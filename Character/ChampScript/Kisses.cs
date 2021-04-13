using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Kisses : CharacterControl
{
    new void Start()
    {
        if (PV.IsMine) base.Start();
    }

    new void Update()
    {
        base.Update();
        if (PV.IsMine)
        {
            if (champState == ChampState.UseSkill) KissesUseSkill();

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Splats.SelectSpellIndicator("PointIndicator");
            }

            if (runQskill) KissesQskill();
        }
        else
        {
            if (gameObject.transform.GetChild(2).GetComponent<FogCoverable>().meActive == true)
            {
                //Debug.Log("meActive == true일때");
                gameObject.transform.GetChild(1).gameObject.SetActive(true);
                gameObject.transform.GetChild(8).gameObject.SetActive(true);
            }

            if (gameObject.transform.GetChild(2).GetComponent<FogCoverable>().meActive == false)
            {
                //Debug.Log("meActive == false일때");
                gameObject.transform.GetChild(1).gameObject.SetActive(false);
                gameObject.transform.GetChild(8).gameObject.SetActive(false);
            }
        }
    }

    private void KissesQskill()
    {
        if (Vector3.Distance(skillPosition, playerTr.transform.position) < 20.0f)
        {
            runQskill = false;
            qCoolTimeDone = false;
            status.CurrMp -= qSkillUseMana;
            PhotonNetwork.Instantiate("KissesQSkillExplosionRange", skillPosition, Quaternion.identity);
            champState = ChampState.UseSkill;
            StartCoroutine("RunQskill");
        }
        else
        {
            destination = skillPosition;
            champState = ChampState.Move;
            if (Input.GetMouseButtonDown(1) && Physics.Raycast(ray, out hit, 100.0f, 1 << 12))
            {
                runQskill = false;
                isSkillCast = false;
                skillCode = SkillCode.wait;
            }
            if (Vector3.Distance(skillPosition, playerTr.transform.position) < 20.0f)
            {
                runQskill = false;
                qCoolTimeDone = false;
                status.CurrMp -= qSkillUseMana;
                PhotonNetwork.Instantiate("KissesQSkillExplosionRange", skillPosition, Quaternion.identity);
                champState = ChampState.UseSkill;
                StartCoroutine("RunQskill");
            }
        }
    }

    IEnumerator RunQskill()
    {
        m_Anim.SetTrigger("Skill");
        yield return new WaitForSeconds(0.5f);
        Sound(QSkillSound);
        PhotonNetwork.Instantiate("KissesQSkill", skillPosition, Quaternion.identity);
        m_Anim.ResetTrigger("Skill");
        m_Anim.SetTrigger("RunIdle");
        isSkillCast = false;
        champState = ChampState.Idle;
        skillCode = SkillCode.wait;
        yield return new WaitForSeconds(qSkillCoolTime - 0.5f);
        qCoolTimeDone = true;
    }

    private void KissesUseSkill()
    {
        m_nvAgent.velocity = Vector3.zero;
        Vector3 dir = skillPosition - transform.position;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10.0f);
    }

    public void Sound(AudioClip ac)
    {
        SoundManager_Game.instance.PlaySkillSound(ac);
    }
}