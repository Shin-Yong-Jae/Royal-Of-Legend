using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class Nexus : MonoBehaviourPunCallbacks,IPunObservable
{
    public GameObject HpBar; // UI로 띄울 HpBar 오브젝트
    public Status status;

    public PhotonView PV;
    private float remoteCurrHp;
    private float remoteMaxHp;

    private float timer = 0;

    void Awake() => status.CurrHp = status.MaxHp;
    
    void Start()
    {
        object isRed;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
        {
            if ((bool)isRed)
            {
                if (gameObject.layer == LayerMask.NameToLayer("RedStructure"))
                {
                    gameObject.tag = "TeamNexus";
                    HpBar.GetComponent<Image>().color = Color.blue;
                }
                if (gameObject.layer == LayerMask.NameToLayer("BlueStructure"))
                {
                    gameObject.tag = "EnemyNexus";
                    HpBar.GetComponent<Image>().color = Color.red;
                }
            }
            if (!(bool)isRed)
            {
                if (gameObject.layer == LayerMask.NameToLayer("RedStructure"))
                {
                    gameObject.tag = "EnemyNexus";
                    HpBar.GetComponent<Image>().color = Color.red;
                }
                if (gameObject.layer == LayerMask.NameToLayer("BlueStructure"))
                {
                    gameObject.tag = "TeamNexus";
                    HpBar.GetComponent<Image>().color = Color.blue;
                }
            }
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (!PV.IsMine)
        {
            status.MaxHp = remoteMaxHp;
            status.CurrHp = remoteCurrHp;
        }
        /////////////////////////////////////////////////// HP 감소 부분 //////////////////////////////////////////////////////
        HpBar.GetComponent<Image>().fillAmount = (status.CurrHp / status.MaxHp);
        /////////////////////////////////////////////////// HP 감소 부분 //////////////////////////////////////////////////////
        if (status.CurrHp <= 0 && timer >= 20) GameManager.instance.GameOver();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(status.CurrHp);
            stream.SendNext(status.MaxHp);
        }
        else
        {
            remoteCurrHp = (float)stream.ReceiveNext();
            remoteMaxHp = (float)stream.ReceiveNext();
        }
    }
}
