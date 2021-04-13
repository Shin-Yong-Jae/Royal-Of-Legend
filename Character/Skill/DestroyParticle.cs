using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DestroyParticle : MonoBehaviourPunCallbacks
{
    public float delayTime;
    void Start()
    {
        DelayDestroy();
    }

    private void DelayDestroy() => Destroy(gameObject, delayTime);

}
