using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MinionManager : MonoBehaviourPunCallbacks
{
    public static MinionManager instance;
    private Transform minions;
    private float regenTime = 0f;
    private int count = 0;
    private bool waveOn = true;
    private float waitWaveTime;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        minions = new GameObject("Minions").transform;
        //if (PhotonNetwork.IsMasterClient)
        //{
        //    Invoke("MinionWave", 10.0f);
        //}
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        else
        {
            if (waveOn && DataManager.instance.gameOver == false)
            {
                regenTime += Time.deltaTime;
                StartCoroutine("WaitWaveTime");
            }
            else
            {
                waitWaveTime += Time.deltaTime;
                if(waitWaveTime > 10.0f)
                {
                    waveOn = true;
                    waitWaveTime = 0.0f;
                }
            }
        }
    }

    IEnumerator WaitWaveTime()
    {
        yield return new WaitForSeconds(10.0f);        
        MinionWave();
    }

    private void CreateMinion()
    {
        GameObject rMinion = PhotonNetwork.Instantiate("RedTeamMinion", new Vector3(132,7,135), Quaternion.identity) as GameObject;
        rMinion.transform.SetParent(minions);

        GameObject bMinion = PhotonNetwork.Instantiate("BlueTeamMinion", new Vector3(25,8,23), Quaternion.identity) as GameObject;
        bMinion.transform.SetParent(minions);
    }

    private void CreateRangeMinion()
    {
        GameObject rMinion = PhotonNetwork.Instantiate("RedTeamMinionRange", new Vector3(132,7,135), Quaternion.identity) as GameObject;
        rMinion.transform.SetParent(minions);

        GameObject bMinion = PhotonNetwork.Instantiate("BlueTeamMinionRange", new Vector3(25,8,23), Quaternion.identity) as GameObject;
        bMinion.transform.SetParent(minions);
    }

    private void MinionWave()
    {
        if (regenTime > 1.0f)
        {
            if (count < 2)
            {
                CreateMinion();
                count++;
            }
            else if (count < 3)
            {
                CreateRangeMinion();
                count++;
            }
            else
            {
                waveOn = false;
                count = 0;
                regenTime = 0.0f;
            }
            regenTime = 0.0f;            
        }
    }
}
