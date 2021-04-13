using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager_Game : MonoBehaviour
{
    public static SoundManager_Game instance;

    public AudioSource inGame;
    public AudioSource BasicAttack;
    public AudioSource UseSkill;
    // Start is called before the first frame update
    void Start()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame

    public void PlayAttackSound(AudioClip clip)
    {
        BasicAttack.Stop();
        BasicAttack.clip = clip;
        BasicAttack.Play();
    }
    public void PlaySkillSound(AudioClip clip)
    {
        UseSkill.Stop();
        UseSkill.clip = clip;
        UseSkill.Play();
    }
}
