using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SkillHit : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    public float EDamage = 20.0f;

    private void OnTriggerEnter(Collider col)
    {
        if (PV.IsMine && col.CompareTag("EnemyHero") || PV.IsMine && col.CompareTag("EnemyMinion"))
        {
            int targetId;
            targetId = col.GetComponent<PhotonView>().ViewID;
            int b = GameObject.FindGameObjectWithTag("Player").GetComponent<PhotonView>().ViewID;
            PV.RPC("HitFunc", RpcTarget.All, targetId, EDamage + GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().AP);
            if (col.GetComponent<Status>().CurrHp <= 0)
            {
                Debug.Log(col);
                Debug.Log(b);
                PV.RPC("RemoteKill", RpcTarget.AllBuffered, b, col.GetComponent<Status>().DieExp);
            }
        }
    }
    public void PlusE()
    {
        EDamage += 20;
        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(false);
    }

    [PunRPC]
    void HitFunc(int viewid, float damage)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrHp -= damage;
    }
    [PunRPC]
    void RemoteKill(int viewid, float dieexp)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrExp += dieexp;
    }
}