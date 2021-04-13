using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager_Main : MonoBehaviour
{
    public static SoundManager_Main instance;

    public AudioSource effectPlayer;
    public AudioSource inRoom;
    public AudioSource champPlayer;

    private void Start()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void PlayEffectSound(AudioClip clip)
    {
        champPlayer.Stop();
        effectPlayer.Stop();
        effectPlayer.clip = clip;
        effectPlayer.Play();
    }

    public void PlayRoomSound(AudioClip clip)
    {
        inRoom.Stop();
        inRoom.clip = clip;
        inRoom.Play();
    }

    public void PlayChampSound(AudioClip[] champclip)
    {
        champPlayer.Stop();
        champPlayer.PlayOneShot(champclip[Random.Range(0, 2)]);
    }
}