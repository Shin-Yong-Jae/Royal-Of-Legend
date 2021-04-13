using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VolumetricLines;
using Photon.Pun;
using Photon.Realtime;

public class LaserAttack : MonoBehaviourPunCallbacks, IPunObservable
{
    public Turret tr;
    public Turret e_tr;
    CharacterControl Ch;
    public PhotonView PV; //네트워크 포톤 뷰 변수

    [Header("target")]
    public Transform targetTr1;
    public Transform targetTr2;
    public Transform targetTr3;
    public Transform targetTr4;

    [Header("e_target")]
    public Transform e_targetTr1;
    public Transform e_targetTr2;
    public Transform e_targetTr3;
    public Transform e_targetTr4;

    [Header("tower")]
    public GameObject[] towers;
    public GameObject[] e_towers;
    private Vector3 curPos;
    private Quaternion curRot;

    void Start()
    {
        tr = GameObject.FindWithTag("TeamTower").GetComponent<Turret>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!PV.IsMine)
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
                transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime* 10.0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, curRot, Time.deltaTime* 10.0f);
            }
        }
    }

    public void OnTriggerEnter(Collider col)
    {
        if (!PV.IsMine && col.tag == "TeamHero" || col.tag == "Player" || !PV.IsMine && col.tag == "EnemyHero")
        {
            Debug.Log("1");
            Debug.Log("OnTarget!");
            col.GetComponent<Status>().CurrHp -= tr.GetComponent<Status>().AD;
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void DestroyRPC()
    {
        Destroy(gameObject);
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
