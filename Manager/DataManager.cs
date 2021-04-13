using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataManager : MonoBehaviour
{
    public static DataManager instance; //singleton patterns

    public bool gameOver = false;
    public bool result;
    public int kill;
    public int death;
    public int cs;

    #region Mono CallBacks
    private void Start()
    {
        //singleton pattern
        if (instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }
    #endregion
}