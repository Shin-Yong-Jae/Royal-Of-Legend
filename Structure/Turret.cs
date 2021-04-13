using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class Turret : MonoBehaviourPunCallbacks, IPunObservable
{
    #region 포탑변수들
    [SerializeField] Transform Tower_Top = null; // 포탑 회전하는 몸체 트랜스폼 변수 선언
    public float m_range = 0f; //포탑 사정거리
    [SerializeField] LayerMask m_layerMask = 0; //특정 레이어를 가진 대상만 검출
    [SerializeField] float m_spinSpeed = 0f; //적을 발견하면 포구 회전시키는 속도
    [SerializeField] float m_fireRate = 0f; //터렛의 연사속도 변수
    //[SerializeField] GameObject laser = null;
    [SerializeField] Transform Tower_Barrel = null;
    float m_currentFireRate;//실제 연산에 쓸 변수
    public float d_distance;
    public Transform m_tfTarget = null; //공격할 대상의 트랜스폼 값.
    public Transform t_tfTarget = null; //우선 검출된 대상의 트랜스폼 값.

    public GameObject Laser;

    public PhotonView PV;
    private Vector3 curPos;
    private Quaternion curRot;
    private float remoteCurrHp;
    private float remoteMaxHp;
    public GameObject HpBar; // UI로 띄울 HpBar 오브젝트
    public Status status;
    public Animator m_Anim;
    private GameObject remoteLastClickedEnemyObject;
    private float timer;
    private int waitingTime;

    #endregion
    void SearchEnemy()
    {
        Collider[] t_cols = Physics.OverlapSphere(transform.position, m_range, m_layerMask);//포탑 사정거리의 적 검출
        Transform t_shortestTarget = null; //포탑과 가장 가까운 적

        if (t_cols.Length > 0) //검출된 collider 하나라도 있을시 실행
        {
            float t_shortestDistance = Mathf.Infinity; //검출 범위 우선 무한대로 설정.

            foreach (Collider t_colTarget in t_cols) //검출된 collider만큼 반복문 실행
            {
                float t_distance = Vector3.SqrMagnitude(transform.position - t_colTarget.transform.position);
                //Vertor3.SqrMagnitude=실제 거리*실제거리 제곱반환. Vector3.Distance = 루트 연산 후 실제 거리 반환 즉 SqrMagnitude
                if (t_shortestDistance > t_distance) //거리비교위한 검출 범위보다 실제 거리가 더 작다면
                {
                    t_shortestDistance = t_distance;  //그 실제 거리를 다시 기준으로 삼아주고
                    t_shortestTarget = t_colTarget.transform; //가장 가까이 있는 대상이라 도출하여 해당 대상에 트랜스폼를 넣어줌
                }
            }
        }

        m_tfTarget = t_shortestTarget; // 최종 타겟에 가장 가까운 대상 설정.
    }

    // Start is called before the first frame update
    void Start()
    {
        status = gameObject.GetComponent<Status>();
        object isRed;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
        {
            if ((bool)isRed)
            {
                if (gameObject.layer == LayerMask.NameToLayer("RedStructure"))
                {
                    gameObject.tag = "TeamTower";
                    HpBar.GetComponent<Image>().color = Color.blue;
                }
                if (gameObject.layer == LayerMask.NameToLayer("BlueStructure"))
                {
                    gameObject.tag = "EnemyTower";
                    HpBar.GetComponent<Image>().color = Color.red;
                }
            }
            else
            {
                if (gameObject.layer == LayerMask.NameToLayer("RedStructure"))
                {
                    gameObject.tag = "EnemyTower";
                    HpBar.GetComponent<Image>().color = Color.red;
                }
                if (gameObject.layer == LayerMask.NameToLayer("BlueStructure"))
                {
                    gameObject.tag = "TeamTower";
                    HpBar.GetComponent<Image>().color = Color.blue;
                }
            }
        }

        m_currentFireRate = m_fireRate;//시작과 동시에 연사 변수에 연사속도 변수를 넣어줌
        InvokeRepeating("SearchEnemy", 0f, 0.5f); //위의 SearchEnemy 함수를 시작과동시에 0.5초마다 반복
        status.CurrHp = status.MaxHp;
        timer = 0.0f;
        waitingTime = 30;
    }

    // Update is called once per frame
    void Update()
    {
        if (PV.IsMine)
        {
            timer += Time.deltaTime;
            if (timer > waitingTime)
            {
                gameObject.GetComponent<Status>().AD += 5;
                //gameObject.GetComponent<Status>().currHp += 10;
                Debug.Log("AD상승@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                timer = 0;
            }
            //d_distance = Vector3.Distance(transform.position, GameObject.FindWithTag("Player").transform.position);

            //Debug.Log(GameObject.Find("player").transform.position);

            if (m_tfTarget == null) //검출된 타겟이 없을시
            {
                //Debug.Log("적 없음");
                Tower_Top.Rotate(new Vector3(0, 45, 0) * Time.deltaTime); //포신이 계속 돌게 만듬
            }
            //else if (m_tfTarget.tag == ("EnemyMinion") &&   //챔피언 타워안에서 공격시 우선검출 코드
            //    GameObject.FindWithTag("EnemyHero").GetComponent<CharacterControl>().AggroTarget == GameObject.FindWithTag("TeamHero")||
            //    GameObject.FindWithTag("EnemyHero").GetComponent<CharacterControl>().AggroTarget == GameObject.FindWithTag("Player"))
            //{
            //    Quaternion t_lookRotation = Quaternion.LookRotation(GameObject.FindWithTag("EnemyHero").transform.position - transform.position);//특정 좌표값(타겟 좌표값)을 바라보게 만드는 회전값 리턴
            //                                                                                                                                     //Debug.Log(t_lookRotation);
            //    Vector3 t_euler = Quaternion.RotateTowards(Tower_Top.rotation,//a지점에서,   
            //                                               t_lookRotation,     //b지점까지,
            //                                               m_spinSpeed * Time.deltaTime).eulerAngles;//c의 속도로 회전.
            //    Tower_Top.rotation = Quaternion.Euler(0, t_euler.y, 0);//오일러 값에서 y축만 반영되도록 설정.

            //    Quaternion t_fireRotation = Quaternion.Euler(0, t_lookRotation.eulerAngles.y, 0);//터렛이 최종적으로 조준해야하는 방향
            //                                                                                     //Debug.Log(GameObject.Find("Tower_Top2").transform.rotation);
            //    if (Quaternion.Angle(Tower_Top.rotation, t_fireRotation) < 5f)//현재 포진의 방향과 조준해야할 방향의 각도차를 계산 약간의 여유위에 < 5도 설정.
            //    {

            //        m_currentFireRate -= Time.deltaTime;//해당 조건을 만족하면 연산 변수가 1씩 감소하다가
            //        if (m_currentFireRate <= 0) //연산 변수가 0보다 작아지면
            //        {
            //            m_currentFireRate = m_fireRate;//다시 연산 변수를 본래 값으로 되돌리고 공격.
            //            Shoot();
            //            Debug.Log("Attack");
            //        }
            //    }
            //}
            else
            {
                //Debug.Log("적 발견");
                Quaternion t_lookRotation = Quaternion.LookRotation(m_tfTarget.position - transform.position);//특정 좌표값(타겟 좌표값)을 바라보게 만드는 회전값 리턴
                                                                                                              //Debug.Log(t_lookRotation);
                Vector3 t_euler = Quaternion.RotateTowards(Tower_Top.rotation,//a지점에서,   
                                                           t_lookRotation,     //b지점까지,
                                                           m_spinSpeed * Time.deltaTime).eulerAngles;//c의 속도로 회전.
                Tower_Top.rotation = Quaternion.Euler(0, t_euler.y, 0);//오일러 값에서 y축만 반영되도록 설정.

                Quaternion t_fireRotation = Quaternion.Euler(0, t_lookRotation.eulerAngles.y, 0);//터렛이 최종적으로 조준해야하는 방향
                                                                                                 //Debug.Log(GameObject.Find("Tower_Top2").transform.rotation);

                if (Quaternion.Angle(Tower_Top.rotation, t_fireRotation) < 5f)//현재 포진의 방향과 조준해야할 방향의 각도차를 계산 약간의 여유위에 < 5도 설정.
                {
                    m_currentFireRate -= Time.deltaTime;//해당 조건을 만족하면 연산 변수가 1씩 감소하다가

                    if (m_currentFireRate <= 0) //연산 변수가 0보다 작아지면
                    {
                        if (m_tfTarget.tag == ("EnemyHero"))
                        {
                            m_currentFireRate = m_fireRate;//다시 연산 변수를 본래 값으로 되돌리고 공격.
                            Shoot();
                        }
                        if (m_tfTarget.tag == ("TeamHero"))
                        {
                            m_currentFireRate = m_fireRate;//다시 연산 변수를 본래 값으로 되돌리고 공격.
                            Shoot();
                        }

                        if (m_tfTarget.tag == ("EnemyMinion"))
                        {
                            m_currentFireRate = m_fireRate;//다시 연산 변수를 본래 값으로 되돌리고 공격.
                            Shoot();
                        }
                        if (m_tfTarget.tag == ("TeamMinion"))
                        {
                            m_currentFireRate = m_fireRate;//다시 연산 변수를 본래 값으로 되돌리고 공격.
                            Shoot();
                        }
                        if (m_tfTarget.tag == ("Player"))
                        {
                            m_currentFireRate = m_fireRate;//다시 연산 변수를 본래 값으로 되돌리고 공격.
                            Shoot();
                        }
                    }
                }
            }

            if (status.CurrHp <= 0) //터렛 파괴
            {                
                PV.RPC("DestroyTurret", RpcTarget.AllBuffered);
                DestroyTurret();
            }

            if (t_tfTarget != null && Laser != null)
            {
                if (t_tfTarget != null)
                {
                    Laser.transform.LookAt(t_tfTarget.transform.position);
                    Laser.transform.Translate(Vector3.forward * Time.deltaTime * 30.0f);

                    if (Vector3.Distance(t_tfTarget.transform.position, Laser.transform.position) < 0.2f && PhotonNetwork.IsMasterClient 
                        && t_tfTarget.tag != "Player" && t_tfTarget.tag != "EnemyHero" && t_tfTarget.tag != "TeamHero")
                    {
                        t_tfTarget.GetComponent<Status>().CurrHp -= status.AD;
                        PhotonNetwork.Destroy(Laser);
                        t_tfTarget = null;
                    }
                    else if (Vector3.Distance(t_tfTarget.transform.position, Laser.transform.position) < 0.2f 
                        && !PhotonNetwork.IsMasterClient && t_tfTarget.tag != "Player" && t_tfTarget.tag != "EnemyHero" && t_tfTarget.tag != "TeamHero")
                    {
                        int a = t_tfTarget.GetComponent<PhotonView>().ViewID;
                        PV.RPC("RemoteHitMinion", RpcTarget.MasterClient, a, status.AD);
                        PhotonNetwork.Destroy(Laser);
                    }
                }
            }
        }
        //isMine이 아닌 것들은 동기화
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
        }
        /////////////////////////////////////////////////// HP 감소 부분 //////////////////////////////////////////////////////
        HpBar.GetComponent<Image>().fillAmount = (status.CurrHp / status.MaxHp);
        /////////////////////////////////////////////////// HP 감소 부분 //////////////////////////////////////////////////////
    }
    [PunRPC]
    public void Shoot()
    {
        Laser = PhotonNetwork.Instantiate("TowerLaser", Tower_Barrel.transform.position, Quaternion.identity) as GameObject;
        t_tfTarget = m_tfTarget;
    }

    [PunRPC]
    public void DestroyTurret()
    {
        if(Laser!=null) PhotonNetwork.Destroy(Laser);
        m_Anim.SetTrigger("IsDestroy");
        gameObject.GetComponent<Collider>().enabled = false;
        gameObject.GetComponent<Turret>().enabled = false;
    }
   

    [PunRPC]
    void RemoteHitMinion(int viewid, float damage)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrHp -= damage;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(status.CurrHp);
            stream.SendNext(status.MaxHp);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
            remoteCurrHp = (float)stream.ReceiveNext();
            remoteMaxHp = (float)stream.ReceiveNext();
        }
    }
}
