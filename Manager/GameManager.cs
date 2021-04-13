using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;
using UnityEngine.AI;

public class GameManager : MonoBehaviourPunCallbacks
{
    string LogoutUserURL = "http://dinootoko.dothome.co.kr/logout.php";
    string VictoryURL = "http://dinootoko.dothome.co.kr/Victory.php";
    string DefeatURL = "http://dinootoko.dothome.co.kr/Defeat.php";
    string RankingURL = "http://dinootoko.dothome.co.kr/Ranking.php";
    public static GameManager instance;
    public GameObject player_pref;
    public bool redTeam = false;
    object isRed;
    object champ;
    public Photonmanager pt;

    public GameObject EndGame_Canvas;
    public GameObject Victory;
    public GameObject Lose;
    public string[] Rank;
    public string MyRank;
    public PhotonView PV;

    GameObject mCamera;
    GameObject skillManager;

    #region Mono CallBacks
    private void Awake()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        if (instance == null || PhotonNetwork.IsMasterClient) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CreatePlayer();
        if (DataManager.instance && DataManager.instance.gameOver == true)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
        }
    }

    public void Update()
    {
        if (player_pref.activeSelf == false && DataManager.instance.gameOver == false)
        {
            RespawnPlayer();
        }
    }

    private void CreatePlayer()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_CHAMPION, out champ))
            {
                if ((bool)isRed)
                {
                    player_pref = PhotonNetwork.Instantiate((string)champ, new Vector3(140, 6.5f, 140), Quaternion.identity) as GameObject;
                    player_pref.layer = LayerMask.NameToLayer("RedHero");

                    if (player_pref.transform.childCount == 11)
                    {
                        for (int i = 2; i <= 4; i++)
                        {
                            player_pref.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("RedHero");
                        }
                    }
                    if (player_pref.transform.childCount == 7)
                    {
                        player_pref.transform.GetChild(2).gameObject.layer = LayerMask.NameToLayer("RedHero");
                    }

                    redTeam = true;
                    if ((mCamera == null))
                    {
                        mCamera = PhotonNetwork.Instantiate("Main Camera", new Vector3(130, 45, 115), Quaternion.Euler(60, 0, 0)) as GameObject;
                        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).gameObject.SetActive(true);
                        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(false);
                        //GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).gameObject.transform.SetParent(null);
                    }
                    else return;
                }

                if (!(bool)isRed)
                {
                    player_pref = PhotonNetwork.Instantiate((string)champ, new Vector3(19, 7f, 19), Quaternion.identity) as GameObject;
                    player_pref.layer = LayerMask.NameToLayer("BlueHero");

                    if (player_pref.transform.childCount == 11)
                    {
                        for (int i = 2; i <= 4; i++)
                        {
                            player_pref.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("BlueHero");
                        }
                    }
                    if (player_pref.transform.childCount == 7)
                    {
                        player_pref.transform.GetChild(2).gameObject.layer = LayerMask.NameToLayer("BlueHero");
                    }

                    redTeam = false;
                    if ((mCamera == null))
                    {
                        mCamera = PhotonNetwork.Instantiate("Main Camera", new Vector3(20, 45, 0), Quaternion.Euler(60, 0, 0)) as GameObject;
                        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).gameObject.SetActive(true);
                        GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(false);
                        //GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).gameObject.transform.SetParent(null);
                    }
                    else return;
                }
            }
        }
        if (redTeam) player_pref.GetComponent<PhotonView>().RPC("SetRemotePlayerTag", RpcTarget.All, true);
        else player_pref.GetComponent<PhotonView>().RPC("SetRemotePlayerTag", RpcTarget.All, false);
    }

    private void RespawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
        {
            if ((bool)isRed)
            {
                player_pref.transform.position = new Vector3(140, 6.5f, 140);
                Debug.Log(player_pref.transform.position);
            }
            if (!(bool)isRed)
            {
                player_pref.transform.position = new Vector3(19, 6.5f, 19);
                Debug.Log(player_pref.transform.position);
            }
        }
        player_pref.GetComponent<CharacterControl>().death = false;
        player_pref.SetActive(true);
        player_pref.GetComponent<CharacterControl>().RespawnInit();
    }

    public void GameOver()
    {
        DataManager.instance.gameOver = true;
        Debug.Log("GameOver");
        EndGame_Canvas.SetActive(true);
        if (GameObject.FindGameObjectWithTag("TeamNexus").GetComponent<Nexus>().status.CurrHp > 0)
        {
            if(Victory.activeSelf == false)
            {
                Debug.Log(PhotonNetwork.LocalPlayer.NickName);
                StartCoroutine(VictoryUser(PhotonNetwork.LocalPlayer.NickName));
            }
            Victory.SetActive(true);
            DataManager.instance.result = true;
        }
        else
        {
            if (Lose.activeSelf == false)
            {
                Debug.Log(PhotonNetwork.LocalPlayer.NickName);
                StartCoroutine(DefeatUser(PhotonNetwork.LocalPlayer.NickName));
            }
            Lose.SetActive(true);
            DataManager.instance.result = false;
        }
        StartCoroutine(RankingUpdate(PhotonNetwork.LocalPlayer.NickName));
    }

    public void OnCheckButtonClicked()
    {
        PhotonNetwork.LoadLevel("Main");
    }

    private void OnApplicationQuit() => StartCoroutine(LogOutUser(PhotonNetwork.LocalPlayer.NickName));    

    IEnumerator LogOutUser(string ID)
    {
        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        WWW www = new WWW(LogoutUserURL, form);

        yield return www;
        Debug.Log("www.text :" + www.text);
    }

    IEnumerator VictoryUser(string ID)
    {
        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        WWW www = new WWW(VictoryURL, form);

        yield return www;
        Debug.Log("www.text :" + www.text);
    }
    IEnumerator DefeatUser(string ID)
    {
        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        WWW www = new WWW(DefeatURL, form);

        yield return www;
        Debug.Log("www.text :" + www.text);
    }

    IEnumerator RankingUpdate(string ID)
    {
        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        WWW www = new WWW(RankingURL, form);

        yield return www;
        Rank = www.text.Split(',');
        Debug.Log(PhotonNetwork.LocalPlayer.NickName);
        for (int i = 0; i < Rank.Length; i++)
        {
            if (Rank[i] == PhotonNetwork.LocalPlayer.NickName)
            {
                MyRank = Rank[i + 2];
            }
        }
        PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_RANKING);
        PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_RANKING, MyRank);
        PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
        Debug.Log("내 랭킹은"+MyRank);
        Debug.Log("www.text :" + www.text);
    }


    [PunRPC]
    void NoticeLeavePlayer()
    {
        //Debug.Log("소환사가 게임을 쳐 나갔습니다.");
    }
    #endregion
}