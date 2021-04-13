using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Werewolf.StatusIndicators.Components;
using Photon.Pun;
using Photon.Realtime;

public class CharacterControl : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables And Objects
    [Header("Player Info")]
    public Text NickNameText;
    public GameObject playerUI;

    [Header("Tower Interaction")]
    public GameObject AggroTarget = null;
    public float t_distance;

    [Header("Character Variables")]
    public LayerMask layermask = 0; // 공격 처리할 레이어들 검출하기위해 선언
    public Animator m_Anim; // 애니메이터
    public NavMeshAgent m_nvAgent; //네비게이션
    public Transform m_weaponTr; //평타가 생성될 위치값
    public Transform playerTr; // 플레이어 위치값을 받을 변수
    public Status status; //챔프 능력치
    public bool isAttackComplete = true; //공격이 완료되었는지를 판단할 bool형 변수
    public GameObject lastClickedEnemyObject = null; // a키+마우스 왼쪽이나 마우스 오른쪽으로 클릭한 적 오브젝트
    protected Ray ray; //광선변수
    private RaycastHit[] hits; // 광선에 충돌한 여러 물체를 담기위해 배열로 충돌변수 하나더 선언
    protected RaycastHit hit; // 광선 충돌 변수
    protected Vector3 destination; // 도착지점 Vector값   
    protected bool isAkeyActivate = false;
    private GameObject currentCursorObject = null; // 현재 마우스 커서가 가리키는 오브젝트
    private int nomalAttack_ID;
    public bool death = false;
    private GameObject normalAttack;
    private GameObject m_AttackTarget;

    [Header("Photon Variables")]
    public PhotonView PV;
    private Vector3 curPos; // 리모트 플레이어 동기화를 위한변수
    private Quaternion curRot; // 리모트 플레이어 동기화를 위한변수
    private float remoteCurrHp;
    private float remoteCurrMp;
    private float remoteMaxHp;
    private float remoteMaxMp;
    private bool remoteDeath;
    public float remoteDieExp;
    public float remoteCurrExp;

    [Header("Cursor")]
    public Texture2D DefaultCursor; // 기본커서
    public Texture2D EnemyAttackCursor; // 적위에 마우스 두었을때 띄울 커서
    public Texture2D BasicAkeyCursor; // A키 눌렀을때 적이 아닐때
    public Texture2D EnemyAkeyCursor; // A키 눌렀을때 적일때
    public Texture2D OnlyEnemyChampionCursor; // 챔피언만 조준 커서 -> 일단 보류    
    public enum CursorType { DefaultCursor, EnemyCursor, BasicAkeyCursor, EnemyAkeyCursor, OnlyEnemyChampionCursor };
    public CursorType cursorType = CursorType.DefaultCursor;
    private Vector2 hotSpot = new Vector2(23.5f, 0); // 모든 커서들에 사용될 핫스팟값

    [Header("CharacterState")]
    public ChampState champState = ChampState.Idle;
    public enum ChampState { Idle, Move, Chase, Attack, UseSkill };

    [Header("UI")]
    public GameObject AttackRangeObject; // a키 누르면 챔프 공격사거리 나타낼 오브젝트
    public GameObject HpBar; // UI로 띄울 HpBar 오브젝트
    public GameObject MpBar; // UI로 띄울 MpBar 오브젝트
    private Transform movePoint; // 이동지점(롤에서 초록색으로 나타났다가 사라지는거)
    private new SpriteRenderer renderer;

    [Header("Skill")]
    public bool qCoolTimeDone = true;
    public bool wCoolTimeDone = true;
    public bool eCoolTimeDone = true;
    public bool rCoolTimeDone = true;
    public float qSkillCoolTime = 1.0f;
    public float wSkillCoolTime = 2.0f;
    public float eSkillCoolTime = 3.0f;
    public float rSkillCoolTime = 4.0f;
    protected bool runQskill = false;
    protected bool runWskill = false;
    protected bool runEskill = false;
    protected bool runRskill = false;
    protected bool isSkillCast = false;
    protected bool isQskillActivate = false;
    protected bool isWskillActivate = false;
    protected bool isEskillActivate = false;
    protected bool isRskillActivate = false;

    protected Vector3 skillPosition;
    public enum SkillCode { wait, q, w, e, r };
    public SkillCode skillCode = SkillCode.wait;
    public SplatManager Splats { get; set; }
    public GameObject[] skillObject;
    public float qSkillUseMana = 10.0f;
    public float wSkillUseMana = 20.0f;
    public float eSkillUseMana = 30.0f;
    public float rSkillUseMana = 40.0f;
    [Header("Audio")]
    public AudioClip BasicAttackSound;
    public AudioClip QSkillSound;
    public AudioClip WSkillSound;
    public AudioClip ESkillSound;
    public AudioClip RSkillSound;

    [Header("아직 사용안한 변수들")]
    private bool outlineActive = false;
    private GameObject outlineObject;
    public GameObject[] charac; // 팀 영웅
    public GameObject[] e_charac; // 적 영웅
    #endregion

    protected void Start()
    {        
        if (PV.IsMine) ChampInit();
    }

    protected void ChampInit()
    {
        SetPlayerNickName();
        Splats = GetComponentInChildren<SplatManager>();
        SetPlayerLayerMask();
        movePoint = GameObject.Find("MovePoint").transform.Find("MovePointImage").transform;
        renderer = movePoint.GetComponent<SpriteRenderer>();
        m_nvAgent.speed = status.moveSpeed;
        status.CurrHp = status.MaxHp;
        status.CurrMp = status.MaxMp;
        status.Kill = 0;
        status.Death = 0;
        champState = ChampState.Idle;
        skillCode = SkillCode.wait;
    }

    private void SetPlayerNickName() => NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;

    protected void Update()
    {
        #region 로컬플레이어처리
        if (PV.IsMine)
        {
            //////////////////////// 스킬 처리 //////////////////////////////////////////////////////////////////////////
            switch (skillCode)
            {
                case SkillCode.wait: Wait(); break;
                case SkillCode.q: Qskill(); break;
                case SkillCode.w: Wskill(); break;
                case SkillCode.e: Eskill(); break;
                case SkillCode.r: Rskill(); break;
                default: break;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                isQskillActivate = true;
                isAkeyActivate = false;
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                isWskillActivate = true;
                isAkeyActivate = false;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                isEskillActivate = true;
                isAkeyActivate = false;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                isRskillActivate = true;
                isAkeyActivate = false;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.A))
            {
                CancleIndicator();
            }

            if (isQskillActivate && Input.GetMouseButtonDown(0) && qCoolTimeDone && status.CurrMp >= qSkillUseMana)
            {
                isQskillActivate = false;
                skillCode = SkillCode.q;
                skillPosition = hit.point;
                CancleIndicator();
            }

            if (isEskillActivate && Input.GetMouseButtonDown(0) && eCoolTimeDone)
            {
                isEskillActivate = false;
                skillCode = SkillCode.e;
                skillPosition = hit.point;
                CancleIndicator();
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

            ////////////////////////임시 포탑우선순위 검출///////////////
            //t_distance = Vector3.Distance(transform.position, GameObject.FindWithTag("EnemyTower").transform.position);
            //if (t_distance > GameObject.FindWithTag("EnemyTower").GetComponent<Turret>().m_range)
            //{
            //    AggroTarget = null;
            //}
            ///////////////////////////////////////////////////////
            m_Anim.SetFloat("AttackSpeed", status.attackSpeed); // 공격속도 애니메이션 스피드를 챔프 공격속도에 맞춰서
            //m_nvAgent.speed = status.moveSpeed;
            //////////////////// 레이캐스트 부분 ////////////////////////////////////////////////////////////////////////////////

            ray = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().ScreenPointToRay(Input.mousePosition); //메인카메라로 부터 마우스 좌표값에 광선을쏜다
            hits = Physics.RaycastAll(ray, 100.0f, layermask); //hits배열안에 레이어마스크에 걸린 적 정보들을

            Debug.DrawRay(ray.origin, ray.direction * 100.0f, Color.green); // 씬 뷰에서 확인용으로 실제 게임에선 안보임
            //////////////////// 레이캐스트 부분 ////////////////////////////////////////////////////////////////////////////////

            //////////////////// 공격 타겟 정하는부분 //////////////////////////////////////////////////////////////////////////
            if (Physics.Raycast(ray, out hit, 100.0f, layermask))
            {
                if (Input.GetMouseButtonDown(1)) lastClickedEnemyObject = hit.collider.gameObject; //광선에 맞은 오브젝트의 Collider를 attackTarget변수에 넣어준다
                if (isAkeyActivate)
                {
                    if (Input.GetMouseButtonDown(0)) lastClickedEnemyObject = hit.collider.gameObject; //광선에 맞은 오브젝트의 Collider를 attackTarget변수에 넣어준다
                }
            }
            if (Physics.Raycast(ray, out hit, 100.0f)) currentCursorObject = hit.collider.gameObject;

            //if (currentCursorObject.layer == LayerMask.NameToLayer("Floor")) outlineActive = false;
            //else outlineActive = true;

            //if (outlineActive)
            //{
            //    currentCursorObject.GetComponent<Outline>().enabled = true;
            //    outlineObject = currentCursorObject;
            //}
            //else
            //{
            //    if(outlineObject != null) outlineObject.GetComponent<Outline>().enabled = false;
            //}
            //////////////////// 공격 타겟 정하는부분 //////////////////////////////////////////////////////////////////////////          

            //////////////////// 마우스 커서 변경 부분 //////////////////////////////////////////////////////////////////////////
            switch (cursorType)
            {
                case CursorType.EnemyCursor: Cursor.SetCursor(EnemyAttackCursor, hotSpot, CursorMode.Auto); break;
                case CursorType.BasicAkeyCursor: Cursor.SetCursor(BasicAkeyCursor, new Vector2(BasicAkeyCursor.width / 2, BasicAkeyCursor.height / 2), CursorMode.Auto); break;
                case CursorType.EnemyAkeyCursor: Cursor.SetCursor(EnemyAkeyCursor, new Vector2(EnemyAkeyCursor.width / 2, EnemyAkeyCursor.height / 2), CursorMode.Auto); break;
                case CursorType.OnlyEnemyChampionCursor: Cursor.SetCursor(OnlyEnemyChampionCursor, hotSpot, CursorMode.Auto); break;
                default: Cursor.SetCursor(DefaultCursor, hotSpot, CursorMode.Auto); break;
            }
            //////////////////// 마우스 커서 변경 부분 //////////////////////////////////////////////////////////////////////////

            //////////////////// 챔프 스테이트 변경 부분 ////////////////////////////////////////////////////////////////////////
            switch (champState)
            {
                case ChampState.Idle: Idle(); break;
                case ChampState.Move: Move(); break;
                case ChampState.Chase: Chase(); break;
                case ChampState.Attack: Attack(); break;
                case ChampState.UseSkill: UseSkill(); break;
                default: Idle(); break;
            }
            //////////////////// 챔프 스테이트 변경 부분 ////////////////////////////////////////////////////////////////////////      

            if (Input.GetKeyDown(KeyCode.A))
            {
                isAkeyActivate = true;
                isQskillActivate = false;
                isEskillActivate = false;
            }

            if (isAkeyActivate) AttackRangeObject.SetActive(true); // 공격 사거리 오브젝트 띄워줌
            else AttackRangeObject.SetActive(false); // 공격 사거리 오브젝트 지워줌

            if (!isSkillCast) // 스킬사용이 끝나고나서
            {
                if (hits.Length == 0) // 레이어마스크에 레이캐스트 히트정보가 없을경우 즉 마우스가 땅위에 있을때
                {
                    cursorType = CursorType.DefaultCursor; // 마우스가 땅위에있을때니까 default 커서로
                    if (Input.GetMouseButtonDown(1) && Physics.Raycast(ray, out hit, 100.0f, 1 << 12))
                    //마우스가 땅위에있고 클릭을 한번했을때
                    {
                        isAkeyActivate = false;
                        renderer.color = new Color(1, 1, 1, 1);
                        destination = hit.point;
                        champState = ChampState.Move;
                        StartCoroutine("CreateAndRemoveImage");
                    }
                    if (Input.GetMouseButton(1) && Physics.Raycast(ray, out hit, 100.0f, 1 << 12))
                    //마우스가 땅위에있고 클릭을 꾸욱하고있을때
                    {
                        isAkeyActivate = false;
                        destination = hit.point; // 마우스로 클릭한 지점의 좌표를 destination 변수에 넣는다
                        champState = ChampState.Move; // 챔프 스테이트를 Move로          
                    }
                    if (isAkeyActivate)
                    {
                        cursorType = CursorType.BasicAkeyCursor;
                        if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out hit, 100.0f, 1 << 12))
                        {
                            isAkeyActivate = false;
                            renderer.color = new Color(1, 0, 0, 1);
                            StartCoroutine("CreateAndRemoveImage");
                            destination = hit.point;
                            champState = ChampState.Move;
                        }
                    }
                }
                else // 반대경우는 마우스가 레이어마스크 즉 적위에 있을때겠지??
                {
                    cursorType = CursorType.EnemyCursor; //마우스가 적위에있으니 공격커서로
                    if (Input.GetMouseButtonDown(1) && !isSkillCast) //마우스 우클릭했을때
                    {
                        if (Vector3.Distance(playerTr.transform.position, lastClickedEnemyObject.transform.position) > status.attackRange)
                        //클릭한 적오브젝트가 공격사거리 보다 멀리있다면
                        {
                            isAkeyActivate = false;
                            champState = ChampState.Chase;
                        }
                        else
                        {
                            isAkeyActivate = false;
                            champState = ChampState.Attack;
                        }
                    }
                    if (isAkeyActivate) // 마우스가 적위에있을때 a키가 활성화 되어있으면
                    {
                        cursorType = CursorType.EnemyAkeyCursor;
                        if (Input.GetMouseButtonDown(0)) // 그상태에서 마우스 왼쪽클릭을하면
                        {
                            if (Vector3.Distance(playerTr.transform.position, lastClickedEnemyObject.transform.position) > status.attackRange)
                            //클릭한 적오브젝트가 공격사거리 보다 멀리있다면
                            {
                                isAkeyActivate = false;
                                champState = ChampState.Chase;
                            }
                            else
                            {
                                isAkeyActivate = false;
                                champState = ChampState.Attack;
                            }
                        }
                    }
                }
            }

            if (m_nvAgent.enabled)
            {
                if (m_nvAgent.remainingDistance <= 0.2f && m_nvAgent.velocity.magnitude >= 0.2f) // 도착지점에 거의 도달했을경우        
                    champState = ChampState.Idle;
            }

            if (Input.GetKeyDown(KeyCode.S)) //s키 눌렀을때      
                champState = ChampState.Idle; //챔프 스테이트 Idle로       

            if (status.CurrHp <= 0 && death == false)
            {
                death = true;
                Dead();
            }
            if (status.CurrExp >= status.MaxExp)
            {
                LevelUp();
            }
            if (m_AttackTarget != null && normalAttack != null)
            {
                if (m_AttackTarget != null)
                {
                    normalAttack.transform.LookAt(m_AttackTarget.transform.position);
                    normalAttack.transform.Translate(Vector3.forward * Time.deltaTime * 20.0f);
                    if (Vector3.Distance(m_AttackTarget.transform.position, normalAttack.transform.position) < 0.2f && PhotonNetwork.IsMasterClient &&
                        !m_AttackTarget.CompareTag("EnemyHero") && !m_AttackTarget.CompareTag("TeamHero") && !m_AttackTarget.CompareTag("TeamTower") &&
                        !m_AttackTarget.CompareTag("EnemyTower") && !m_AttackTarget.CompareTag("TeamNexus") && !m_AttackTarget.CompareTag("EnemyNexus") && PV.IsMine)
                    {
                        m_AttackTarget.GetComponent<Status>().CurrHp -= status.AD;
                        int a = m_AttackTarget.GetComponent<PhotonView>().ViewID;
                        int b = GameObject.FindGameObjectWithTag("Player").GetComponent<PhotonView>().ViewID;
                        if (m_AttackTarget.GetComponent<Status>().CurrHp <= 0)
                        {
                            Debug.Log(b);
                            PV.RPC("RemoteDeadMinion", RpcTarget.AllBuffered, b, m_AttackTarget.GetComponent<Status>().DieExp);
                        }
                        PhotonNetwork.Destroy(normalAttack);
                        m_AttackTarget = null;
                    }
                    else if (Vector3.Distance(m_AttackTarget.transform.position, normalAttack.transform.position) < 0.2f && !PhotonNetwork.IsMasterClient &&
                        !m_AttackTarget.CompareTag("EnemyHero") && !m_AttackTarget.CompareTag("TeamHero") && !m_AttackTarget.CompareTag("TeamTower") &&
                        !m_AttackTarget.CompareTag("EnemyTower") && !m_AttackTarget.CompareTag("TeamNexus") && !m_AttackTarget.CompareTag("EnemyNexus") && PV.IsMine)
                    {
                        int a = m_AttackTarget.GetComponent<PhotonView>().ViewID;
                        int b = GameObject.FindGameObjectWithTag("Player").GetComponent<PhotonView>().ViewID;
                        PV.RPC("RemoteHitMinion", RpcTarget.MasterClient, a, status.AD);
                        if (m_AttackTarget.GetComponent<Status>().CurrHp <= 0)
                        {
                            Debug.Log(b);
                            PV.RPC("RemoteDeadMinion", RpcTarget.AllBuffered, b, m_AttackTarget.GetComponent<Status>().DieExp);
                        }
                        PhotonNetwork.Destroy(normalAttack);
                        m_AttackTarget = null;
                    }
                }
            }
            if (DataManager.instance.gameOver)
            {
                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_CHAMPION);
                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLYAER_KILL);
                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_DEATH);

                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLYAER_KILL, status.Kill);
                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_DEATH, status.Death);

                PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);

                PhotonNetwork.Destroy(gameObject);
            }
        }
        #endregion
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
            status.MaxMp = remoteMaxMp;
            status.CurrMp = remoteCurrMp;
            status.CurrHp = remoteCurrHp;
            status.DieExp = remoteDieExp;
            status.CurrExp = remoteCurrExp;

            if (status.CurrHp <= 0)
            {
                gameObject.GetComponent<Collider>().enabled = false;
                playerUI.gameObject.SetActive(false);
            }
            else
            {
                gameObject.GetComponent<Collider>().enabled = true;
                playerUI.gameObject.SetActive(true);
            }
        }
        ///////////////////////////////////////////// hp 및 mp 감소 부분 //////////////////////////////////////////////////////
        HpBar.GetComponent<Image>().fillAmount = (status.CurrHp / status.MaxHp);
        MpBar.GetComponent<Image>().fillAmount = (status.CurrMp / status.MaxMp);
        ///////////////////////////////////////////// hp 및 mp 감소 부분 //////////////////////////////////////////////////////
    }

    IEnumerator CreateAndRemoveImage() // 이동지점에 띄울 이미지 코루틴
    {
        destination = hit.point; // 마우스로 클릭한 지점의 좌표를 destination 변수에 넣는다
        movePoint.transform.position = destination; // 도착지점에 이미지 생성
        movePoint.gameObject.SetActive(true); // 이동지점에 표시할 마크 표시
        yield return new WaitForSeconds(0.3f); //0.3초를 기다려준후
        movePoint.gameObject.SetActive(false); //이미지 오브젝트를 꺼준다
    }

    private void Wait() => skillCode = default;

    protected void Qskill()
    {
        if (CheckQSkillUse() == true)
        {
            isSkillCast = true;
            runQskill = true;
        }
        else
        {
            skillCode = SkillCode.wait;
        }
    }

    protected void Wskill()
    {
        if (CheckWSkillUse() == true)
        {
            wCoolTimeDone = false;
        }
        else
        {

        }
    }

    protected void Eskill()
    {
        if (CheckESkillUse() == true)
        {
            isSkillCast = true;
            runEskill = true;
        }
        else
        {
            skillCode = SkillCode.wait;
        }
    }

    protected void Rskill()
    {
        if (CheckRSkillUse() == true)
        {

        }
        else
        {

        }
    }

    private bool CheckQSkillUse()
    {
        if (qCoolTimeDone) return true;
        else return false;
    }

    private bool CheckWSkillUse()
    {
        if (wCoolTimeDone) return true;
        else return false;
    }

    private bool CheckESkillUse()
    {
        if (eCoolTimeDone) return true;
        else return false;
    }

    private bool CheckRSkillUse()
    {
        if (rCoolTimeDone) return true;
        else return false;
    }

    #region 스테이트에따른 함수들
    private void Idle() //기본상태함수
    {
        m_Anim.ResetTrigger("Skill");
        m_Anim.ResetTrigger("StartAttack");
        m_Anim.ResetTrigger("Move");
        m_Anim.SetTrigger("StopMove");
        m_nvAgent.SetDestination(playerTr.transform.position);
    }

    private void Move() //이동상태함수
    {
        m_Anim.ResetTrigger("Skill");
        m_Anim.ResetTrigger("StopMove");
        m_Anim.ResetTrigger("StartAttack");
        m_Anim.SetTrigger("Move"); // 이동 애니메이션 실행
        m_nvAgent.SetDestination(destination); // 네비게이션 도착지점을 destination 변수 위치값으로
        if (m_nvAgent.velocity == Vector3.zero)
        {
            champState = ChampState.Idle;
        }
    }

    private void Chase() //추적상태함수
    {
        m_Anim.ResetTrigger("Skill");
        m_Anim.ResetTrigger("StopMove");
        m_Anim.ResetTrigger("StartAttack");
        m_Anim.SetTrigger("Move");
        m_nvAgent.SetDestination(lastClickedEnemyObject.transform.position);

        if (Vector3.Distance(lastClickedEnemyObject.transform.position, playerTr.transform.position) < status.attackRange && isAttackComplete)
        //추적중 공격사거리 안에 들어오고 감지된 적이 공격타겟일때
        {
            champState = ChampState.Attack; // 챔프 스테이트 Attack으로
            return;
        }
    }

    private void Attack() // 공격상태함수
    {
        m_nvAgent.velocity = Vector3.zero;
        m_Anim.ResetTrigger("Skill");
        m_Anim.ResetTrigger("StopMove");
        m_Anim.ResetTrigger("Move");
        m_Anim.SetTrigger("StartAttack");
        AggroTarget = lastClickedEnemyObject;//어그로 바뀌는문 추가
        Vector3 attackTarget;
        attackTarget = new Vector3(lastClickedEnemyObject.transform.position.x, transform.position.y, lastClickedEnemyObject.transform.position.z);
        transform.LookAt(attackTarget); //공격대상을 항상 바라보게
        if ((Vector3.Distance(lastClickedEnemyObject.transform.position, playerTr.transform.position) > status.attackRange) && isAttackComplete)
        //공격중에 적이 챔프 사거리 밖으로 도망가면
        {
            champState = ChampState.Chase; //챔프 스테이트 Chase로
            return;
        }
    }

    protected void CancleIndicator()
    {
        Splats.CancelSpellIndicator();
        Splats.CancelRangeIndicator();
    }

    protected void UseSkill() //스킬 사용했을때 함수
    {
        m_Anim.ResetTrigger("Move");
        m_Anim.ResetTrigger("StartAttack");
        m_Anim.ResetTrigger("StopMove");
    }

    private void Dead() //죽었을때 함수
    {
        if (normalAttack != null)
        {
            if (normalAttack.GetComponent<PhotonView>().ViewID == nomalAttack_ID && PV.IsMine)
            {
                PhotonNetwork.Destroy(normalAttack);
            }
        }
        m_Anim.ResetTrigger("StartAttack");
        m_Anim.ResetTrigger("StopMove");
        m_Anim.ResetTrigger("Move");
        m_Anim.SetTrigger("Dead");
        gameObject.GetComponent<Collider>().enabled = false;
        playerUI.gameObject.SetActive(false);
        m_nvAgent.speed = 0;
        Cursor.SetCursor(DefaultCursor, hotSpot, CursorMode.Auto);
        StartCoroutine("WaitForRespawn");
    }

    private void LevelUp()
    {
        status.Level += 1;
        status.CurrExp = 0;
        status.MaxExp += 20;
        status.CurrHp = 100;
        status.CurrMp = 100;
        if (status.Level % 2 == 0)
        {
            GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
        }
        if (status.Level % 2 == 1)
        {
            GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(true);
        }
    }
    #endregion

    private void CreateNormalAttack()
    {
        if (PV.IsMine)
        {
            m_AttackTarget = lastClickedEnemyObject;
            normalAttack = PhotonNetwork.Instantiate("NormalAttack", m_weaponTr.position, Quaternion.identity);
            nomalAttack_ID = normalAttack.GetComponent<PhotonView>().ViewID;
            SoundManager_Game.instance.PlaySkillSound(BasicAttackSound);
        }
    }

    private void NormalAttackHit()
    {
        if (PV.IsMine)
        {
            if (lastClickedEnemyObject)
            {
                SoundManager_Game.instance.PlaySkillSound(BasicAttackSound);
                int targetID = lastClickedEnemyObject.GetComponent<PhotonView>().ViewID;
                PV.RPC("MeleeChampAttack", RpcTarget.AllBuffered, targetID, status.AD);
                if (lastClickedEnemyObject.GetComponent<Status>().CurrHp <= 0)
                {
                    Debug.Log("들어오나요 ?");
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().CurrExp += lastClickedEnemyObject.GetComponent<Status>().DieExp;
                    if (lastClickedEnemyObject.tag == "EnemyHero")
                    {
                        GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().Kill += 1;
                        lastClickedEnemyObject.GetComponent<Status>().Death += 1;
                    }
                }
            }
        }
    }

    IEnumerator WaitForRespawn()
    {
        yield return new WaitForSeconds(status.respawnTime);
        gameObject.SetActive(false);
    }

    public void RespawnInit()
    {
        m_Anim.SetTrigger("RunIdle");
        status.CurrHp = status.MaxHp;
        status.CurrMp = status.MaxMp;
        gameObject.GetComponent<Collider>().enabled = true;
        playerUI.gameObject.SetActive(true);
        m_nvAgent.speed = status.moveSpeed;
        champState = ChampState.Idle;
        skillCode = SkillCode.wait;
    }
    public void PlusAD()
    {
        status.AD += 5;
        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
    }
    public void PlusAP()
    {
        status.AP += 5;
        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
    }
    public void PlusMS()
    {
        status.moveSpeed += 1;
        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
    }
    public void PlusAS()
    {
        status.attackSpeed += 0.5f;
        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
    }


    private void EndAttack() => isAttackComplete = true;

    private void StartAttack() => isAttackComplete = false;

    protected void SetPlayerLayerMask()
    {
        object isRed;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
        {
            if ((bool)isRed)
            {
                layermask = 1 << 13 | 1 << 14 | 1 << 17;
            }
            if (!(bool)isRed)
            {
                layermask = 1 << 16 | 1 << 15 | 1 << 18;
            }
        }
    }

    [PunRPC]
    protected void RemoteHitMinion(int viewid, float damage)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrHp -= damage;
    }
    [PunRPC]
    void RemoteDeadMinion(int viewid, float dieexp)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrExp += dieexp;
    }

    [PunRPC]
    protected void SetRemotePlayerTag(bool isRed)
    {
        if (!PV.IsMine)
        {
            if (isRed)
            {
                gameObject.layer = LayerMask.NameToLayer("RedHero");
                if (gameObject.transform.childCount == 11)
                {
                    for (int i = 2; i <= 4; i++)
                    {
                        gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("RedHero");
                        gameObject.transform.GetChild(i).GetComponent<FogCoverable>().enabled = true;
                    }
                }
                if (gameObject.transform.childCount == 7)
                {
                    gameObject.transform.GetChild(2).gameObject.layer = LayerMask.NameToLayer("RedHero");
                    gameObject.transform.GetChild(2).GetComponent<FogCoverable>().enabled = true;
                }
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("BlueHero");
                if (gameObject.transform.childCount == 11)
                {
                    for (int i = 2; i <= 4; i++)
                    {
                        gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("BlueHero");
                        gameObject.transform.GetChild(i).GetComponent<FogCoverable>().enabled = true;
                    }
                }
                if (gameObject.transform.childCount == 7)
                {
                    gameObject.transform.GetChild(2).gameObject.layer = LayerMask.NameToLayer("BlueHero");
                    gameObject.transform.GetChild(2).GetComponent<FogCoverable>().enabled = true;
                }
            }

            if (gameObject.layer == GameManager.instance.player_pref.layer)
            {
                gameObject.tag = "TeamHero";
            }
            else
            {
                gameObject.tag = "EnemyHero";
            }
        }
    }
    [PunRPC]
    protected void MeleeChampAttack(int viewid, float damage)
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
            stream.SendNext(status.CurrMp);
            stream.SendNext(status.MaxHp);
            stream.SendNext(status.CurrExp);
            stream.SendNext(status.DieExp);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
            remoteCurrHp = (float)stream.ReceiveNext();
            remoteCurrMp = (float)stream.ReceiveNext();
            remoteMaxHp = (float)stream.ReceiveNext();
            remoteMaxMp = (float)stream.ReceiveNext();
            remoteCurrExp = (float)stream.ReceiveNext();
            remoteDieExp = (float)stream.ReceiveNext();
        }
    }
}