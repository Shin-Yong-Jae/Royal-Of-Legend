using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;

public class Cabulma : CharacterControl
{
    private bool isEskillArrived = true;
    private float eSkillArriveDistance = 0.0f;
    private float eSkillCastTime = 0.0f;
    private bool eSkillTimeCheck = false;
    private float eSkillSpeed = 0.0f;
    private float moveTimer = 0.0f;
    private float eSkillHeight = 0.0f;
    private GameObject SkillInfo;
    new void Start()
    {
        if (PV.IsMine) base.Start(); SkillInfo = GameObject.Find("CabulmaSkillInfo");
    }

    new void Update()
    {
        base.Update();
        if (PV.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Splats.SelectSpellIndicator("PointIndicator");
            }

            if (runEskill) CabulmaEskill();

            if (eSkillTimeCheck)
            {
                if (!isEskillArrived)
                {
                    eSkillSpeed = 1.0f / eSkillCastTime;
                    moveTimer += Time.deltaTime * eSkillSpeed;
                    Vector3 currentPos = Vector3.Lerp(transform.position, skillPosition, moveTimer);
                    currentPos.y += eSkillHeight * Mathf.Sin(Mathf.Clamp01(moveTimer) * Mathf.PI);
                    transform.position = currentPos;
                }
                else
                {
                    eSkillTimeCheck = false;
                    eSkillCastTime = 0.0f;
                    isEskillArrived = true;
                    isSkillCast = false;
                    m_Anim.ResetTrigger("Eskill");
                    m_Anim.SetTrigger("RunIdle");
                    champState = ChampState.Idle;
                    skillCode = SkillCode.wait;
                    skillPosition = Vector3.zero;
                    eSkillSpeed = 0.0f;
                    moveTimer = 0.0f;
                    eSkillHeight = 0.0f;
                    Invoke("CoolTime", eSkillCoolTime);
                }

                if (Vector3.Distance(skillPosition, playerTr.position) <= 0.3f)
                {                  
                    isEskillArrived = true;                    
                    playerTr.transform.position = skillPosition;
                    gameObject.GetComponent<NavMeshAgent>().enabled = true;
                    PhotonNetwork.Instantiate("CabulmaEskill", skillPosition, Quaternion.identity);
                }
            }
        }
        else
        {
            if (gameObject.transform.GetChild(2).GetComponent<FogCoverable>().meActive == true)
            {
                //Debug.Log("meActive == true일때");
                gameObject.transform.GetChild(5).gameObject.SetActive(true);
            }

            if (gameObject.transform.GetChild(2).GetComponent<FogCoverable>().meActive == false)
            {
                //Debug.Log("meActive == false일때");
                gameObject.transform.GetChild(5).gameObject.SetActive(false);
            }
        }
    }

    private void CoolTime() => eCoolTimeDone = true;

    private void CabulmaEskill()
    {
        gameObject.GetComponent<NavMeshAgent>().enabled = false;
        eSkillArriveDistance = Vector3.Distance(skillPosition, playerTr.position);
        if (eSkillArriveDistance > 17.0f)
        {
            eSkillArriveDistance = 17.0f;
            skillPosition = SkillInfo.transform.position + Vector3.ClampMagnitude(skillPosition - SkillInfo.transform.position, 17.0f);
        }
        eSkillHeight = eSkillArriveDistance * 0.3f;
        eSkillCastTime = eSkillArriveDistance * 0.05f;
        m_Anim.SetFloat("EskillSpeed", eSkillCastTime);
        eCoolTimeDone = false;
        runEskill = false;
        isEskillArrived = false;
        eSkillTimeCheck = true;
        m_Anim.SetTrigger("Eskill");
        status.CurrMp -= eSkillUseMana;
        champState = ChampState.UseSkill;
        Sound(ESkillSound);
    }
    public void Sound(AudioClip ac)
    {
        SoundManager_Game.instance.PlaySkillSound(ac);
    }
}