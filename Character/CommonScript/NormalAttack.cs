using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NormalAttack : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView PV;
    CharacterControl cc;
    public GameObject go;
    private Vector3 curPos;
    private Quaternion curRot;

    void Start()
    {
        cc = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>();
        if (PV.IsMine)
        {
            go = cc.lastClickedEnemyObject;
        }
    }

    void Update()
    {
        if(!PV.IsMine)
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
        }
    }

    private void OnTriggerEnter(Collider col) //Collider는 RPC로 못넘겨줌
    {
        if (PV.IsMine && col.CompareTag("EnemyHero"))
        {
            int a = go.GetComponent<PhotonView>().ViewID;
            int b = cc.GetComponent<PhotonView>().ViewID;
            PV.RPC("HitObject", RpcTarget.AllBuffered, a, cc.GetComponent<Status>().AD);
            if (go.GetComponent<Status>().CurrHp <= 0)
            {
                PV.RPC("RemoteDeadChamp", RpcTarget.AllBuffered, b, go.GetComponent<Status>().DieExp);
                PV.RPC("KillChamp", RpcTarget.AllBuffered, b);
                PV.RPC("DeathChamp", RpcTarget.AllBuffered, a);
            }
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }

        if (PV.IsMine && col.CompareTag("EnemyTower"))
        {
            int a = go.GetComponent<PhotonView>().ViewID;
            PV.RPC("HitObject", RpcTarget.AllBuffered, a, cc.GetComponent<Status>().AD);
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }

        if (PV.IsMine && col.CompareTag("EnemyNexus"))
        {
            int a = go.GetComponent<PhotonView>().ViewID;
            PV.RPC("HitObject", RpcTarget.AllBuffered, a, cc.GetComponent<Status>().AD);
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void HitObject(int viewid, float damage)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrHp -= damage;
    }

    [PunRPC]
    void DestroyRPC()
    {
        Destroy(gameObject);
    }
    [PunRPC]
    void RemoteDeadChamp(int viewid,float dieexp)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().CurrExp+=dieexp;
    }
    [PunRPC]
    void KillChamp(int viewid)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().Kill += 1;
    }
    [PunRPC]
    void DeathChamp(int viewid)
    {
        if (PhotonView.Find(viewid).gameObject != null) PhotonView.Find(viewid).gameObject.GetComponent<Status>().Death += 1;
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
        }
    }
}