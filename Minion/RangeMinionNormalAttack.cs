using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RangeMinionNormalAttack : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView PV;
    private Vector3 curPos;
    private Quaternion curRot;

    private void Start()
    {
        //SetColor();
    }

    private void Update()
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
                transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10.0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, curRot, Time.deltaTime * 10.0f);
            }
        }
    }

    private void SetColor()
    {
        ParticleSystem.MainModule settings = GetComponentInChildren<ParticleSystem>().main;
        if (gameObject.CompareTag("TeamMinion")) settings.startColor = Color.blue ;
        else settings.startColor = Color.red;
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