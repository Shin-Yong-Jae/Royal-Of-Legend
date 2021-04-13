using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; //NavMeshAgent를사용하기위해 using
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class MinionAI : MonoBehaviourPunCallbacks, IPunObservable
{
    private MinionAI instance;
    public PhotonView PV;
    public Animator m_Anim; // 애니메이터
    public NavMeshAgent m_nvAgent; // 네비게이션

    [SerializeField] float searchRange = 0f; // 탐색범위
    public float attackRange = 0f; // 공격범위
    [SerializeField] LayerMask layerMask = 0; //레이어검출용

    public GameObject attackTarget = null; // 공격할 오브젝트
    public GameObject[] destination; // 도착지점 배열
    public Transform currDestination;
    public Transform WeaponTr;
    public bool isAttackComplete = true; //공격이 완료되었는지를 판단할 bool형 변수 
    public GameObject HpBar; // UI로 띄울 HpBar 오브젝트

    public enum MinionState { Rush, Attack, Dead };
    //겜시작할때 Idle 상태로
    public MinionState minionState = MinionState.Rush;
    private Vector3 curPos;
    private Quaternion curRot;
    private float remoteCurrHp;
    private float remoteMaxHp;
    public float remoteDieExp;
    public float remoteCurrExp;
    public Status status;
    private bool isDead;
    object isRed;
    public GameObject[] charac; // 팀 영웅
    public GameObject[] e_charac; // 적 영웅
    public GameObject[] player; //내 영웅
    public GameObject eeee;

    public GameObject rmNormalAttack;
    public GameObject m_AttackTarget;
    private int targetId;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null) instance = this;
        isDead = false;
        status = gameObject.GetComponent<Status>();
        status.CurrHp = status.MaxHp;
        SetMinionTag();
        SetMinionDestination();
        SetCurrDestination();
        InvokeRepeating("SearchTarget", 0f, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (rmNormalAttack) CancelInvoke("SearchTarget");
            switch (minionState)
            {
                case MinionState.Rush: Rush(); break;
                case MinionState.Attack: Attack(); break;
                case MinionState.Dead: Dead(); break;
                default: Rush(); break;
            }
            if (status.CurrHp <= 0) minionState = MinionState.Dead;

            if (currDestination.GetComponent<Collider>().enabled == false)
            {
                SetCurrDestination();
            }

            if (m_AttackTarget != null && rmNormalAttack != null)
            {
                rmNormalAttack.transform.LookAt(m_AttackTarget.transform.position);
                rmNormalAttack.transform.Translate(Vector3.forward * Time.deltaTime * 10.0f);
                if (Vector3.Distance(m_AttackTarget.transform.position, rmNormalAttack.transform.position) < 1.0f && PV.IsMine && instance == this)
                {
                    targetId = m_AttackTarget.GetComponent<PhotonView>().ViewID;
                    PV.RPC("MinionHitClinent", RpcTarget.All, targetId, status.AD);
                    PhotonNetwork.Destroy(rmNormalAttack);
                    m_AttackTarget = null;
                }
            }

            if (DataManager.instance.gameOver) PhotonNetwork.Destroy(gameObject);
        }
        //클라이언트들 동기화
        else
        {
            //끊어진 시간이 너무 길 경우(텔레포트)
            if ((transform.position - curPos).sqrMagnitude >= 10.0f * 10.0f)
            {
                transform.position = curPos;
                transform.rotation = curRot;
            }
            //끊어진 시간이 짧을 경우(자연스럽게 연결 - 데드레커닝)
            else
            {
                transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10.0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, curRot, Time.deltaTime * 10.0f);
            }
            status.MaxHp = remoteMaxHp;
            status.CurrHp = remoteCurrHp;
            status.DieExp = remoteDieExp;
            status.CurrExp = remoteCurrExp;

        }
        /////////////////////////////////////////////////// HP 감소 부분 //////////////////////////////////////////////////////
        HpBar.GetComponent<Image>().fillAmount = (status.CurrHp / status.MaxHp);
        /////////////////////////////////////////////////// HP 감소 부분 //////////////////////////////////////////////////////
    }

    [PunRPC]
    void MinionHitClinent(int viewid, float damage)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrHp -= damage;
    }

    private void Rush()
    {
        m_Anim.ResetTrigger("Attack");
        m_Anim.SetTrigger("Move"); //이동애니메이션 트리거 던져줌 
        if (attackTarget == null) // 돌진하는도중 공격타겟이 없으면
        {
            InvokeRepeating("SearchTarget", 0f, 0.5f);
            m_nvAgent.SetDestination(currDestination.transform.position);
        }
        else // 돌진하는도중 공격타겟이 나타나면
        {
            CancelInvoke("SearchTarget"); // 새로운 공격타겟을 찾는함수를 잠시 꺼주고 
            m_nvAgent.SetDestination(attackTarget.transform.position); // 도착지점을 공격타겟으로 변경

            if (Vector3.Distance(transform.position, attackTarget.transform.position) < attackRange) // 쫓다가 공격타겟이 미니언 공격사거리 내에 들어오면
            {
                minionState = MinionState.Attack;
            }

            if (Vector3.Distance(transform.position, attackTarget.transform.position) > searchRange) // 쫓다가 공격타겟이 탐색범위를 넘어가면
            {
                InvokeRepeating("SearchTarget", 0f, 0.5f); //탐색함수 다시실행
                attackTarget = null;
            }
        }
    }

    private void Attack()
    {
        m_Anim.ResetTrigger("Move");
        m_Anim.SetTrigger("Attack"); // 공격 애니메이션 트리거 던져줌
        m_nvAgent.velocity = Vector3.zero; //미니언 이동 멈춰주고

        if (isAttackComplete) InvokeRepeating("SearchTarget", 0f, 1.0f); //공격도중에는 새로운적을 찾고 
        else CancelInvoke("SearchTarget");

        if (attackTarget != null)
        {
            transform.LookAt(attackTarget.transform.position); //공격대상을 항상 바라보게

            if ((Vector3.Distance(transform.position, attackTarget.transform.position)) > attackRange && isAttackComplete) // 공격하다가 공격대상이 미니언 사거리보다 멀어지면
            {
                attackTarget = null;
                minionState = MinionState.Rush;
                return;
            }
        }
        else
        {
            minionState = MinionState.Rush;
        }
    }

    private void Dead()
    {
        e_charac = GameObject.FindGameObjectsWithTag("EnemyHero");
        charac = GameObject.FindGameObjectsWithTag("TeamHero");
        player = GameObject.FindGameObjectsWithTag("Player");
        if (!isDead)
        {
            if (rmNormalAttack)
            {
                if (rmNormalAttack.GetComponent<PhotonView>().ViewID == rmNormalAttackID && PV.IsMine)
                {
                    PhotonNetwork.Destroy(rmNormalAttack);
                }
            }
            //if (gameObject.tag == "TeamMinion")
            //{
            //    PV.RPC("RemoteDeadMinion", RpcTarget.MasterClient, e_charac, status.DieExp);
            //}
            //else if(gameObject.tag == "EnemyMinion")
            //{

            //    PV.RPC("RemoteDeadMinion", RpcTarget.MasterClient, charac, status.DieExp);
            //    Debug.Log("charac?" + charac);
            //    PV.RPC("RemoteDeadMinion", RpcTarget.MasterClient, player, status.DieExp);
            //    Debug.Log("player?" + player);
            //}
            isDead = true;
            status.AD = 0;
            m_nvAgent.GetComponent<NavMeshAgent>().enabled = false;
            m_Anim.ResetTrigger("Move");
            m_Anim.ResetTrigger("Attack");
            m_Anim.SetTrigger("Dead");
            gameObject.GetComponent<Collider>().enabled = false;
            HpBar.SetActive(false);
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
            //if (gameObject.tag == "TeamMinion")
            //{
            //    for (int i = 0; i < e_charac.Length; i++)
            //    {
            //        if (e_charac[i].GetComponent<Collider>().enabled == true)
            //        {
            //            e_charac[i].GetComponent<CharacterControl>().status.CurrExp += status.DieExp;
            //        }
            //    }
            //}
            //if (PV.IsMine&&gameObject.tag == "EnemyMinion")
            //{
            //    if (PV.IsMine&&GameObject.FindWithTag("Player").GetComponent<Collider>().enabled == true)
            //    {
            //        GameObject.FindWithTag("Player").GetComponent<CharacterControl>().status.CurrExp += status.DieExp;
            //    }
            //    for (int i = 0; i < charac.Length; i++)
            //    {
            //        if (charac[i].GetComponent<Collider>().enabled == true)
            //        {
            //            charac[i].GetComponent<CharacterControl>().status.CurrExp += status.DieExp;
            //        }
            //    }
            //}
        }
    }

    [PunRPC]
    void DestroyRPC() => Destroy(gameObject, 1.5f);

    void SearchTarget() //가장 가까이 있는 적 찾는함수 용재꺼랑 같은방식
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, searchRange, layerMask);
        GameObject t_shortestTarget = null;

        if (cols.Length > 0)
        {
            float t_shortestDistance = Mathf.Infinity;

            foreach (Collider t_colTarget in cols)
            {
                float t_distance = Vector3.SqrMagnitude(transform.position - t_colTarget.transform.position);

                if (t_shortestDistance > t_distance)
                {
                    t_shortestDistance = t_distance;
                    t_shortestTarget = t_colTarget.gameObject;
                }
            }
        }
        attackTarget = t_shortestTarget;
    }

    private void SetMinionTag()
    {
        object isRed;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
        {
            if ((bool)isRed)
            {
                if (gameObject.layer == LayerMask.NameToLayer("RedMinion"))
                {
                    gameObject.tag = "TeamMinion";
                    HpBar.GetComponent<Image>().color = Color.blue;
                }
                if (gameObject.layer == LayerMask.NameToLayer("BlueMinion"))
                {
                    gameObject.tag = "EnemyMinion";
                    HpBar.GetComponent<Image>().color = Color.red;
                }
            }
            if (!(bool)isRed)
            {
                if (gameObject.layer == LayerMask.NameToLayer("RedMinion"))
                {
                    gameObject.tag = "EnemyMinion";
                    HpBar.GetComponent<Image>().color = Color.red;
                }
                if (gameObject.layer == LayerMask.NameToLayer("BlueMinion"))
                {
                    gameObject.tag = "TeamMinion";
                    HpBar.GetComponent<Image>().color = Color.blue;
                }
            }
        }
    }

    private void SetMinionDestination()
    {
        if (gameObject.layer == LayerMask.NameToLayer("RedMinion"))
        {
            destination[0] = GameObject.Find("Blue_Tower1");
            destination[1] = GameObject.Find("Blue_Tower2");
            destination[2] = GameObject.Find("Blue_Tower3");
            destination[3] = GameObject.Find("Blue_Tower4");
            destination[4] = GameObject.Find("Blue_Nexus");
        }
        else if (gameObject.layer == LayerMask.NameToLayer("BlueMinion"))
        {
            destination[0] = GameObject.Find("Red_Tower1");
            destination[1] = GameObject.Find("Red_Tower2");
            destination[2] = GameObject.Find("Red_Tower3");
            destination[3] = GameObject.Find("Red_Tower4");
            destination[4] = GameObject.Find("Red_Nexus");

        }
    }

    private void SetCurrDestination()
    {
        for (int i = 0; i < destination.Length; i++)
        {
            if (destination[i].GetComponent<Collider>().enabled == true)
            {
                currDestination = destination[i].transform;
                break;
            }
        }
    }

    void EndAttack()
    {
        if (attackRange < 10.0f)
        {
            GameObject m_AttackTarget;
            m_AttackTarget = attackTarget;
        }
        isAttackComplete = true;
    }

    void StartAttack()
    {
        isAttackComplete = false;
    }

    int rmNormalAttackID;

    void CreateNormalAttack()
    {
        if (rmNormalAttack) return;
        else if (attackRange > 10 && instance == this && PV.IsMine)
        {
            rmNormalAttack = PhotonNetwork.Instantiate("RangeMinionNormalAttack", WeaponTr.position, Quaternion.identity) as GameObject;
            m_AttackTarget = attackTarget;
            rmNormalAttackID = rmNormalAttack.GetComponent<PhotonView>().ViewID;
            //PV.RPC("SetrmNormalAttackColor", RpcTarget.All);
        }
        else return;
    }
    //[PunRPC]
    //void SetrmNormalAttackColor()
    //{
    //    ParticleSystem.MainModule settings = rmNormalAttack.GetComponentInChildren<ParticleSystem>().main;
    //    if (gameObject.CompareTag("TeamMinion")) settings.startColor = Color.blue;
    //    if (gameObject.CompareTag("EnemyMinion")) settings.startColor = Color.red;
    //}

    //[PunRPC]
    //void RemoteDeadMinion(GameObject[] GoID, float dieexp)
    //{
    //    for (int i = 0; i < GoID.Length; i++)
    //    {
    //        int viewid = GoID[i].GetComponent<PhotonView>().ViewID;
    //        PhotonView.Find(viewid).gameObject.GetComponent<CharacterControl>().status.CurrExp += dieexp;
    //        Debug.Log(GoID[i]);
    //        Debug.Log(PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrExp);
    //    }
    //}

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(status.CurrHp);
            stream.SendNext(status.MaxHp);
            stream.SendNext(status.DieExp);
            stream.SendNext(status.CurrExp);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
            remoteCurrHp = (float)stream.ReceiveNext();
            remoteMaxHp = (float)stream.ReceiveNext();
            remoteDieExp = (float)stream.ReceiveNext();
            remoteCurrExp = (float)stream.ReceiveNext();
        }
    }
}
