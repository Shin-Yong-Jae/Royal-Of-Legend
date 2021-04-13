using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using System.Runtime.InteropServices; //window 최소화 위함
using Hashtable = ExitGames.Client.Photon.Hashtable; //CustomProperties를 사용하기 위함

public class Photonmanager : MonoBehaviourPunCallbacks
{
    public enum UserState { offline, Master, Lobby, Room };
    public UserState userState = UserState.offline;
    public bool onLeftRoom = false;

    public static Photonmanager instance;

    //panel전환할 때 카메라 전환하기 위함
    public GameObject canvas;

    public GameObject MessageBox;
    public GameObject MessageBox_Text;

    [Header("Login Window")]
    public GameObject LoginPanel;
    public GameObject LoginButton_false;
    public GameObject LoginButton_true;
    public GameObject IDInputField;
    public GameObject PWInputField;
    public GameObject SelectRoomPanel;
    public GameObject CreateRoomPanel;
    public GameObject CreateRoomButton;
    public GameObject CreateButton;
    public GameObject RoomName;
    public GameObject Room;
    public Transform GridTr;
    public GameObject LoadingObject;
    public GameObject SignUpButton;
    public GameObject DropOutButton;
    public GameObject Membership_Panel;
    public GameObject Membership_Text;
    public GameObject Membership_ID_InputField;
    public GameObject Membership_PW_InputField;
    public GameObject Membership_OK_btn;
    private bool signup;

    [Header("Room Window")]
    public GameObject RoomPanel;
    public GameObject[] RedPlayer;
    public GameObject[] BluePlayer;
    public GameObject[] RedPlayer_Ready;
    public GameObject[] BluePlayer_Ready;
    public GameObject[] Champ_all;
    public GameObject ReadyButton;
    private string roomnametext;
    public GameObject RoomName_RoomPanel;
    public GameObject OutRoomButton_RoomPanel;

    [Header("Result Window")]
    public GameObject EndPanel;
    public GameObject RestartButton;
    public GameObject GoRoomButton;
    public GameObject ResultText;
    public GameObject[] RedPlayer_ResultPanel;
    public GameObject[] BluePlayer_ResultPanel;
    public GameObject[] RedPlayer_KillDeath;
    public GameObject[] BluePlayer_KillDeath;
    public GameObject[] RedPlayer_Ranking;
    public GameObject[] BluePlayer_Ranking;

    [Header("Effect Sounds")]
    public AudioClip LoginButton;
    public AudioClip LogoutButton;
    public AudioClip JoinLobby;
    public AudioClip JoinRoomButton;
    public AudioClip EnterRoom;
    public AudioClip InRoom;
    public AudioClip OutRoom;
    public AudioClip Ready;
    public AudioClip SelectIcon;

    [Header("Champ Sounds")]
    public AudioClip[] Kisses;
    public AudioClip[] Cabulma;

    private int redPlayerCount = 0;
    private int BluePlayerCount = 0;

    private bool isPlayerRedTeam; // true면 RedTeam, false면 BlueTeam
    private bool isPlayerReady = false;
    private bool gameStart = true;

    List<RoomInfo> _roomList = new List<RoomInfo>();

    [Header("Login DB")]
    string LoginUserURL = "http://dinootoko.dothome.co.kr/login.php";
    string LogoutUserURL = "http://dinootoko.dothome.co.kr/logout.php";
    string SignUpURL = "http://dinootoko.dothome.co.kr/Signup.php";
    string SignOutURL = "http://dinootoko.dothome.co.kr/Signout.php";

    //window 최소화 위함
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    #region Mono CallBacks
    void Awake()
    {
        Screen.SetResolution(800, 450, false);
    }

    void Start()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        //마스터 클라이언트와 일반 클라이언트들이 레벨을 동기화할지 결정 
        //(true로 설정하면 마스터에서 LoadLevel()로 레벨을 변경하면 모든 클라이언트들이 자동으로 동일한 레벨을 로드)
        //PhotonNetwork.AutomaticallySyncScene = true;

        if (DataManager.instance && DataManager.instance.gameOver == true)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);

            Room room = PhotonNetwork.CurrentRoom;
            if (room.IsOpen != true)
            {
                room.IsOpen = true;
                room.SetCustomProperties(room.CustomProperties);
            }
            LoginPanel.SetActive(false);
            RoomPanel.SetActive(false);
            EndPanel.SetActive(true);
            ResultGame();
        }
    }

    public void ResultGame()
    {
        if (DataManager.instance.result == true)
            ResultText.GetComponent<Text>().text = "승리";
        else
            ResultText.GetComponent<Text>().text = "패배";
    }

    void Update()
    {
        if (LoginPanel.activeInHierarchy)
        {
            //id를 입력했거나 SelectRoomPanel이 켜져있으면 LoginButton_true을 비활성화 시키기 위함
            if ((IDInputField.GetComponent<InputField>().text == "" || PWInputField.GetComponent<InputField>().text == "") || SelectRoomPanel.activeInHierarchy || Membership_Panel.activeInHierarchy)
            {
                LoginButton_false.SetActive(true);
                LoginButton_true.SetActive(false);
            }
            else
            {
                LoginButton_false.SetActive(false);
                LoginButton_true.SetActive(true);
            }

            if (LoginButton_true.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Return)) OnLoginButtonClicked();
                if (Input.GetKeyDown(KeyCode.KeypadEnter)) OnLoginButtonClicked();
            }

            if (RoomName.GetComponent<InputField>().text != "") CreateButton.GetComponent<Button>().interactable = true;
            else CreateButton.GetComponent<Button>().interactable = false;

            if(Membership_Panel.activeInHierarchy)
            {
                SignUpButton.GetComponent<Button>().interactable = false;
                DropOutButton.GetComponent<Button>().interactable = false;
                IDInputField.GetComponent<InputField>().interactable = false;
                PWInputField.GetComponent<InputField>().interactable = false;

                if (signup) Membership_Text.GetComponent<Text>().text = "회원가입";
                else Membership_Text.GetComponent<Text>().text = "회원탈퇴";

                if (Membership_ID_InputField.GetComponent<InputField>().text != "" && Membership_PW_InputField.GetComponent<InputField>().text != "") Membership_OK_btn.GetComponent<Button>().interactable = true;
                else Membership_OK_btn.GetComponent<Button>().interactable = false;
            }
            else
            {
                SignUpButton.GetComponent<Button>().interactable = true;
                DropOutButton.GetComponent<Button>().interactable = true;
                IDInputField.GetComponent<InputField>().interactable = true;
                PWInputField.GetComponent<InputField>().interactable = true;
                Membership_ID_InputField.GetComponent<InputField>().text = "";
                Membership_PW_InputField.GetComponent<InputField>().text = "";
            }
        }

        if (RoomPanel.activeInHierarchy)
        {
            object isplayerReady;
            object isplayerRed;

            int readyCount = 0;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue(PlayerInformation.PLAYER_READY, out isplayerReady))
                    if ((bool)isplayerReady) readyCount++;
            }

            if (readyCount == PhotonNetwork.PlayerList.Length)
            {
                int redplayer = 0, blueplayer = 0;

                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (player.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isplayerRed))
                    {
                        if ((bool)isplayerRed) redplayer++;
                        if (!(bool)isplayerRed) blueplayer++;
                    }
                }

                if (redplayer == blueplayer && redplayer + blueplayer >= 2 && gameStart)
                {
                    Room room = PhotonNetwork.CurrentRoom;
                    //Hashtable cp = room.CustomProperties;
                    room.IsOpen = false;
                    room.SetCustomProperties(room.CustomProperties);

                    gameStart = false;
                    ReadyButton.GetComponent<Button>().interactable = false;
                    OutRoomButton_RoomPanel.GetComponent<Button>().interactable = false;
                    Debug.Log("gameStart");
                    LoadingObject.SetActive(true);
                }
            }
        }

        if (userState == UserState.Master && onLeftRoom == true)
        {
            onLeftRoom = false;
            IDInputField.GetComponent<InputField>().text = PhotonNetwork.LocalPlayer.NickName;
            PhotonNetwork.JoinLobby();
        }
    }
    #endregion

    #region Photon CallBacks
    public override void OnConnectedToMaster() //Login Button 클릭 -> ConnectUsingSettings -> OnConnectedToMaster
    {
        Debug.Log("connect To Master");
        userState = UserState.Master;
        //PhotonNetwork.JoinRandomRoom();
    }

    //public override void OnJoinRandomFailed(short returnCode, string message) //JoinRandomRoom입장 실패 시 room생성
    //{
    //    Debug.Log("Failed Join room, Creating One...");
    //    PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 10 });
    //}

    //public override void OnCreateRoomFailed(short returnCode, string message)
    //{

    //}

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        SoundManager_Main.instance.PlayEffectSound(JoinLobby);
        userState = UserState.Lobby;

        IDInputField.GetComponent<InputField>().interactable = false;
        EndPanel.SetActive(false);
        RoomPanel.SetActive(false);
        LoginPanel.SetActive(true);
        SelectRoomPanel.SetActive(true);
        _roomList.Clear();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) //Lobby에 들어올때, room의 정보가 Update될때, 룸의 프로퍼티가 업데이트 될때
    {
        Debug.Log("RoomList Updated _Update");
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Room"))
        {
            Destroy(obj);
        }

        foreach (RoomInfo roomInfo in roomList)
        {
            Debug.Log(roomInfo.Name + " 리스트에서 받아온 roomname");
            if (/*!roomInfo.IsOpen ||*/ !roomInfo.IsVisible || roomInfo.RemovedFromList)
            {
                if (_roomList.IndexOf(roomInfo) != -1)
                    _roomList.RemoveAt(_roomList.IndexOf(roomInfo));
            }
            else
            {
                if (!_roomList.Contains(roomInfo)) _roomList.Add(roomInfo);
                else _roomList[_roomList.IndexOf(roomInfo)] = roomInfo;
            }
        }

        foreach (RoomInfo roomInfo in _roomList)
        {
            GameObject _room = Instantiate(Room, GridTr);
            RoomData roomData = _room.GetComponent<RoomData>();
            roomData.roomName = roomInfo.Name;
            roomData.maxPlayer = roomInfo.MaxPlayers;
            roomData.playerCount = roomInfo.PlayerCount;
            roomData.isOpen = roomInfo.IsOpen;
            roomData.UpdateInfo();

            if (roomData.isOpen == false)
                _room.GetComponent<Button>().interactable = false;
            else
            {
                roomData.GetComponent<Button>().onClick.AddListener
                (
                    delegate //메소드를 직접 호출하는 것 대신에 delegate로 메소드를 호출 -> 포인터개념과 비슷, 대리자
                    {
                        SoundManager_Main.instance.PlayEffectSound(JoinRoomButton);
                        LoadingObject.SetActive(true);
                        roomnametext = roomData.roomName;
                        PhotonNetwork.JoinRoom(roomData.roomName, null);
                    }
                );
            }
        }
        Debug.Log("RoomList Updated _End");
    }

    //room에 들어오고 기본 설정 시작
    public override void OnJoinedRoom() //클라이언트가 룸을 생성하고 들어갔을 때 호출되는 제공되는 메소드
    {
        Debug.Log("Joined room");
        userState = UserState.Room;
        RoomName_RoomPanel.GetComponent<Text>().text = roomnametext;

        //현재 room에 있는 player를 전부 확인하여 data받아오고 뿌림
        for (int playerInRoom = 0; playerInRoom < PhotonNetwork.PlayerList.Length; playerInRoom++)
        {
            object isRed;
            object isPlayerReady;
            object isPlayerRoomnuber;

            //CustomProperties -> 룸이나 플레이어에 관련시킬 수 있는 해시테이블 형식의 일시적 파라미터
            //TryGetValue -> 지정된 키와 연결된 값을 가져옴
            //https://docs.microsoft.com/ko-kr/dotnet/api/system.collections.generic.dictionary-2.trygetvalue?view=netframework-4.8 꼭 볼것
            if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
            {
                if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                {
                    if ((bool)isRed) //RedTeam일 경우 
                    {
                        RedPlayer[(int)isPlayerRoomnuber].GetComponent<Text>().text = PhotonNetwork.PlayerList[playerInRoom].NickName;
                        if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_READY, out isPlayerReady))
                            if ((bool)isPlayerReady) RedPlayer_Ready[(int)isPlayerRoomnuber].SetActive(true);

                        redPlayerCount++;
                    }

                    if (!(bool)isRed)//BlueTeam일 경우 
                    {
                        BluePlayer[(int)isPlayerRoomnuber].GetComponent<Text>().text = PhotonNetwork.PlayerList[playerInRoom].NickName;
                        if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_READY, out isPlayerReady))
                            if ((bool)isPlayerReady) BluePlayer_Ready[(int)isPlayerRoomnuber].SetActive(true);

                        BluePlayerCount++;
                    }
                }
            }
        }

        //모든 player의 data를 받아왔으니 내가 자동으로 들어가질 team 선택되도록
        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
        {
            if (RedPlayer[playerInRoom].GetComponent<Text>().text == "")
            {
                isPlayerRedTeam = true;
                Hashtable initialProps = new Hashtable() { { PlayerInformation.PLAYER_TEAM, isPlayerRedTeam }, { PlayerInformation.PLAYER_ROOMNUMBER, playerInRoom } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps); //LocalPlayer의 커스텀 프로퍼티를 신규/갱신된 key-value 로 갱신
                RedPlayer[playerInRoom].GetComponent<Text>().text = PhotonNetwork.LocalPlayer.NickName;
                RedPlayer[playerInRoom].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f); //본인 팀창 색상 다르게
                break;
            }

            if (BluePlayer[playerInRoom].GetComponent<Text>().text == "")
            {
                isPlayerRedTeam = false;
                Hashtable initialProps = new Hashtable() { { PlayerInformation.PLAYER_TEAM, isPlayerRedTeam }, { PlayerInformation.PLAYER_ROOMNUMBER, playerInRoom } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
                BluePlayer[playerInRoom].GetComponent<Text>().text = PhotonNetwork.LocalPlayer.NickName;
                BluePlayer[playerInRoom].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
                break;
            }
        }
        Debug.Log("Joined room complete");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) //player의 Property가 업데이트 되었을 때 호출 (중요 포인트 -> 자신것도 포함됨) 
    {
        Debug.Log("Property Updated _start");

        object isPlayerRed;
        object isPlayerRoomnuber;
        object isPlayerReady;
        object isPlayerKill;
        object isPlayerDeath;
        object isPlayerRanking;

        Debug.Log("Property Updated _delete");
        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
        {
            if (BluePlayer[playerInRoom].GetComponent<Text>().text == targetPlayer.NickName)
            {
                BluePlayer[playerInRoom].GetComponent<Text>().text = "";
                break;
            }

            if (RedPlayer[playerInRoom].GetComponent<Text>().text == targetPlayer.NickName)
            {
                RedPlayer[playerInRoom].GetComponent<Text>().text = "";
                break;
            }
        }

        Debug.Log("Property Updated _plus");
        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
        {
            if (changedProps.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
            {
                if ((int)isPlayerRoomnuber == playerInRoom)
                {
                    if (changedProps.TryGetValue(PlayerInformation.PLAYER_TEAM, out isPlayerRed))
                    {
                        if ((bool)isPlayerRed) RedPlayer[playerInRoom].GetComponent<Text>().text = targetPlayer.NickName;

                        if (!(bool)isPlayerRed) BluePlayer[playerInRoom].GetComponent<Text>().text = targetPlayer.NickName;
                    }
                }
            }
        }

        if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_READY, out isPlayerReady))
        {
            if ((bool)isPlayerReady)
            {
                if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isPlayerRed))
                {
                    if ((bool)isPlayerRed)
                    {
                        if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                            RedPlayer_Ready[(int)isPlayerRoomnuber].SetActive(true);
                    }

                    if (!(bool)isPlayerRed)
                    {
                        if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                            BluePlayer_Ready[(int)isPlayerRoomnuber].SetActive(true);
                    }
                }
            }

            if (!(bool)isPlayerReady)
            {
                if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isPlayerRed))
                {
                    if ((bool)isPlayerRed)
                    {
                        if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                            RedPlayer_Ready[(int)isPlayerRoomnuber].SetActive(false);
                    }

                    if (!(bool)isPlayerRed)
                    {
                        if (targetPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                            BluePlayer_Ready[(int)isPlayerRoomnuber].SetActive(false);
                    }
                }
            }
        }

        Debug.Log("Property Updated _Complete");

        if (EndPanel.activeInHierarchy && DataManager.instance.gameOver)
        {
            DataManager.instance.gameOver = false;
            Room room = PhotonNetwork.CurrentRoom;

            for (int playerInRoom = 0; playerInRoom < room.PlayerCount; playerInRoom++)
            {
                if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                {
                    if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isPlayerRed))
                    {
                        PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLYAER_KILL, out isPlayerKill);
                        PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_DEATH, out isPlayerDeath);
                        PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_RANKING, out isPlayerRanking);
                        if ((bool)isPlayerRed)
                        {
                            Debug.Log("red KD");
                            RedPlayer_ResultPanel[(int)isPlayerRoomnuber].GetComponent<Text>().text = PhotonNetwork.PlayerList[playerInRoom].NickName;
                            RedPlayer_KillDeath[(int)isPlayerRoomnuber].GetComponent<Text>().text = (int)isPlayerKill + " / " + (int)isPlayerDeath;
                            RedPlayer_Ranking[(int)isPlayerRoomnuber].GetComponent<Text>().text = isPlayerRanking +" 위 ";
                        }
                        if (!(bool)isPlayerRed)
                        {
                            Debug.Log("blue KD");
                            BluePlayer_ResultPanel[(int)isPlayerRoomnuber].GetComponent<Text>().text = PhotonNetwork.PlayerList[playerInRoom].NickName;
                            BluePlayer_KillDeath[(int)isPlayerRoomnuber].GetComponent<Text>().text = (int)isPlayerKill + " / " + (int)isPlayerDeath;
                            BluePlayer_Ranking[(int)isPlayerRoomnuber].GetComponent<Text>().text = isPlayerRanking + " 위 ";
                        }
                    }
                }
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        object isPlayerRed;
        object isPlayerNumber;

        if (otherPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerNumber))
        {
            if (otherPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isPlayerRed))
            {
                if ((bool)isPlayerRed)
                {
                    RedPlayer[(int)isPlayerNumber].GetComponent<Text>().text = "";
                    RedPlayer_Ready[(int)isPlayerNumber].SetActive(false);
                }

                if (!(bool)isPlayerRed)
                {
                    BluePlayer[(int)isPlayerNumber].GetComponent<Text>().text = "";
                    BluePlayer_Ready[(int)isPlayerNumber].SetActive(false);
                }
            }
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
        SoundManager_Main.instance.PlayEffectSound(OutRoom);

        for (int i = 0; i < 5; i++)
        {
            RedPlayer[i].GetComponent<Text>().text = "";
            RedPlayer_Ready[i].SetActive(false);
            BluePlayer[i].GetComponent<Text>().text = "";
            BluePlayer_Ready[i].SetActive(false);
            RedPlayer[i].transform.parent.transform.parent.GetComponent<Button>().interactable = true;
            BluePlayer[i].transform.parent.transform.parent.GetComponent<Button>().interactable = true;
        }
        foreach (GameObject Champ_array in Champ_all)
        {
            Champ_array.GetComponent<Button>().interactable = true;
            Champ_array.GetComponent<Image>().color = new Color(1, 1, 1, 0.392f);
        }
        ReadyButton.SetActive(false);
        ChangeColor();
        PhotonNetwork.LocalPlayer.CustomProperties.Clear();
        onLeftRoom = true;
    }

    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    Debug.Log("OnLogoutButtonClicked -> OnDisconnected");
    //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    //}
    #endregion

    #region UI CallBacks
    public void OnLoginButtonClicked()
    {
        Debug.Log("Login");
        SoundManager_Main.instance.PlayEffectSound(LoginButton);

        if (IDInputField.GetComponent<InputField>().text.Length <= 2)
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "아이디는 3글자 이상입니다.";
        } 
        else StartCoroutine(LoginUser(IDInputField.GetComponent<InputField>().text, PWInputField.GetComponent<InputField>().text));
    }

    public void OnMembershipSignUpButtonClicked()
    {
        signup = true;
        Membership_Panel.SetActive(true);
    }

    public void OnMembershipDropOutButtonClicked()
    {
        signup = false;
        Membership_Panel.SetActive(true);
    }

    public void OnMembershipOKButtonClicked()
    {
        if (Membership_ID_InputField.GetComponent<InputField>().text.Length <= 2)
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "아이디는 3글자 이상입니다.";
            return;
        }
        if (signup) StartCoroutine(SignUpUser(Membership_ID_InputField.GetComponent<InputField>().text, Membership_PW_InputField.GetComponent<InputField>().text));
        else StartCoroutine(SignOutUser(Membership_ID_InputField.GetComponent<InputField>().text, Membership_PW_InputField.GetComponent<InputField>().text));
    }

    public void OnMembershipCancleButtonClicked() => Membership_Panel.SetActive(false);

    public void OnLoadCompleate_EnterLobby() //Loading Bar 스크립트에서 호출. 
    {
        PhotonNetwork.JoinLobby();  //OnRoomListUpdate사용을 위해 lobby로 입장 -> OnJoinedLobby
        Debug.Log("Load Complete_EnterLobby");
    }

    public void OnCreateRoomButtonClicked()
    {
        Debug.Log("CreateRoom");
        SoundManager_Main.instance.PlayEffectSound(SelectIcon);
        CreateRoomPanel.SetActive(true);
        CreateRoomButton.GetComponent<Button>().interactable = false;
    }

    public void OnCreateButtonClicked()
    {
        SoundManager_Main.instance.PlayEffectSound(JoinRoomButton);
        foreach (RoomInfo roomInfo in _roomList)
        {
            if (roomInfo.Name == RoomName.GetComponent<InputField>().text)
            {
                MessageBox.SetActive(true);
                MessageBox_Text.GetComponent<Text>().text = "\"" + RoomName.GetComponent<InputField>().text + "\"" + "은 이미 생성되어 있는 방입니다.";
                return;
            }
        }

        LoadingObject.SetActive(true);
        roomnametext = RoomName.GetComponent<InputField>().text;
        CreateRoomButton.GetComponent<Button>().interactable = true;
        CreateRoomPanel.SetActive(false);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        roomOptions.MaxPlayers = 10;
        //roomOptions.CustomRoomProperties = new Hashtable() { { "CustomProperties", "커스텀 프로퍼티" } };
        //roomOptions.CustomRoomPropertiesForLobby = new string[] { "CustomProperties" };
        PhotonNetwork.CreateRoom(roomnametext, roomOptions);
        RoomName.GetComponent<InputField>().text = "";
    }

    public void OnCancleButtonClicked()
    {
        RoomName.GetComponent<InputField>().text = "";
        CreateRoomPanel.SetActive(false);
        CreateRoomButton.GetComponent<Button>().interactable = true;
    }

    public void OnGoRestartButtonClicked()
    {
        object champ;
        object isRed;
        object isPlayerRoomnuber;
        redPlayerCount = 0; BluePlayerCount = 0;
        OnLoadComplete_EnterRoom();
        Room room = PhotonNetwork.CurrentRoom;
        RoomName_RoomPanel.GetComponent<Text>().text = room.Name;
        for (int playerInRoom = 0; playerInRoom < PhotonNetwork.PlayerList.Length; playerInRoom++)
        {
            if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
            {
                if (PhotonNetwork.PlayerList[playerInRoom].CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
                {
                    if ((bool)isRed) //RedTeam일 경우 
                    {
                        RedPlayer[(int)isPlayerRoomnuber].GetComponent<Text>().text = PhotonNetwork.PlayerList[playerInRoom].NickName;
                        redPlayerCount++;
                    }
                    if (!(bool)isRed)//BlueTeam일 경우 
                    {
                        BluePlayer[(int)isPlayerRoomnuber].GetComponent<Text>().text = PhotonNetwork.PlayerList[playerInRoom].NickName;
                        BluePlayerCount++;
                    }
                }
            }
        }
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_CHAMPION, out champ))
            OnChampionClicked(champ.ToString());
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_ROOMNUMBER, out isPlayerRoomnuber))
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
            {
                if ((bool)isRed)
                    RedPlayer[(int)isPlayerRoomnuber].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
                if (!(bool)isRed)
                    BluePlayer[(int)isPlayerRoomnuber].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
            }
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
    }

    public void OnLoadComplete_EnterRoom() //Loading Bar 스크립트에서 호출. 
    {
        Debug.Log("Load Complete_EnterRoom");
        SoundManager_Main.instance.PlayEffectSound(EnterRoom);
        EndPanel.SetActive(false);
        LoginPanel.SetActive(false);
        RoomPanel.SetActive(true);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        canvas.GetComponent<Canvas>().worldCamera = Camera.main;
    }

    public void OnLoadComplete_GameStart() //Loading Bar 스크립트에서 호출. 
    {
        Debug.Log("Load Complete_GameStart");
        isPlayerReady = !isPlayerReady;
        PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_READY);
        PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_READY, isPlayerReady);
        PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
        PhotonNetwork.IsMessageQueueRunning = false;

        if (PhotonNetwork.IsMasterClient) EnterGame();
        else Invoke("EnterGame", 0.5f);
    }

    void EnterGame() => PhotonNetwork.LoadLevel("Game");

    public void OnCloseErrorBoxButtonClicked()
    {
        SoundManager_Main.instance.PlayEffectSound(SelectIcon);
        MessageBox_Text.GetComponent<Text>().text = "";
        MessageBox.SetActive(false);
    }

    public void OnChangeTeamButtonClicked()
    {
        Debug.Log("OnChangeTeamButtonClicked");
        SoundManager_Main.instance.PlayRoomSound(SelectIcon);

        //비어있는 팀 선택창 클릭시
        if (EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Text>().text == "")
        {
            ChangeColor(); //본인 팀창 색상 원상 복구

            object isRed;

            //RedTeam 창을 선택했을 경우
            if (EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.name == "RedTeam")
            {
                Debug.Log("Select_RedTeam");
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
                {
                    if ((bool)isRed) //LocalPlayer가 RedTeam이였을 경우 -> RedTeam에서 자리만 이동
                    {
                        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
                        {
                            if (EventSystem.current.currentSelectedGameObject.name == "Red_Information_" + (playerInRoom + 1).ToString())
                            {
                                Debug.Log("Select_RedTeam_red->red");
                                RedPlayer[playerInRoom].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
                                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_ROOMNUMBER);
                                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_ROOMNUMBER, playerInRoom);
                                PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
                            }
                        }
                    }

                    if (!(bool)isRed) //LocalPlayer가 BlueTeam이였을 경우 -> team이동
                    {
                        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
                        {
                            if (EventSystem.current.currentSelectedGameObject.name == "Red_Information_" + (playerInRoom + 1).ToString())
                            {
                                Debug.Log("Select_RedTeam_blue->red");
                                RedPlayer[playerInRoom].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
                                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_ROOMNUMBER);
                                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_ROOMNUMBER, playerInRoom);
                                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_TEAM);
                                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_TEAM, true);
                                PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
                            }
                        }
                    }
                }
            }

            //BlueTeam 창을 선택했을 경우
            if (EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.name == "BlueTeam")
            {
                Debug.Log("Select_BlueTeam");
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerInformation.PLAYER_TEAM, out isRed))
                {
                    if ((bool)isRed)  //LocalPlayer가 RedTeam이였을 경우 -> team이동
                    {
                        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
                        {
                            if (EventSystem.current.currentSelectedGameObject.name == "Blue_Information_" + (playerInRoom + 1).ToString())
                            {
                                Debug.Log("Select_BlueTeam_red->blue");
                                BluePlayer[playerInRoom].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
                                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_ROOMNUMBER);
                                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_ROOMNUMBER, playerInRoom);
                                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_TEAM);
                                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_TEAM, false);
                                PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
                            }
                        }
                    }

                    if (!(bool)isRed) //LocalPlayer가 BlueTeam이였을 경우 -> BlueTeam에서 자리만 이동
                    {
                        for (int playerInRoom = 0; playerInRoom < 5; playerInRoom++)
                        {
                            if (EventSystem.current.currentSelectedGameObject.name == "Blue_Information_" + (playerInRoom + 1).ToString())
                            {
                                Debug.Log("Select_BlueTeam_blue->blue");
                                BluePlayer[playerInRoom].transform.parent.transform.parent.GetComponent<Image>().color = new Color(0.667f, 0.667f, 0.667f, 1.0f);
                                PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_ROOMNUMBER);
                                PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_ROOMNUMBER, playerInRoom);
                                PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
                            }
                        }
                    }
                }
            }
        }
    }

    void ChangeColor()
    {
        for (int i = 0; i < 5; i++)
        {
            RedPlayer[i].transform.parent.transform.parent.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            BluePlayer[i].transform.parent.transform.parent.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
    }

    public void OnChampionClicked(string champ)
    {
        ReadyButton.SetActive(true);

        foreach (GameObject Champ_array in Champ_all)
        {
            if (!(Champ_array.name == champ))
                Champ_array.GetComponent<Image>().color = new Color(1, 1, 1, 0.392f);
            else
                SelectedChamp(Champ_array);
        }

        if (champ == "Commingsoon")
            ReadyButton.GetComponent<Button>().interactable = false;
        else
            ReadyButton.GetComponent<Button>().interactable = true;

        PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_CHAMPION);
        PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_CHAMPION, champ);
    }

    void SelectedChamp(GameObject champname)
    {
        Debug.Log("Champion selected: " + champname);
        champname.GetComponent<Image>().color = new Color(0, 0, 0, 0.392f);

        if (champname.name == "Kisses")
        {
            SoundManager_Main.instance.PlayChampSound(Kisses);
        }
        if(champname.name == "Cabulma")
        {
            SoundManager_Main.instance.PlayChampSound(Cabulma);
        }
    }

    public void OnReadyButtonClicked()
    {
        if (isPlayerReady == false)
            SoundManager_Main.instance.PlayEffectSound(Ready);
        if (isPlayerReady == true)
            SoundManager_Main.instance.PlayEffectSound(SelectIcon);

        isPlayerReady = !isPlayerReady;

        PhotonNetwork.LocalPlayer.CustomProperties.Remove(PlayerInformation.PLAYER_READY);
        PhotonNetwork.LocalPlayer.CustomProperties.Add(PlayerInformation.PLAYER_READY, isPlayerReady);
        PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);

        if (isPlayerReady)
        {
            foreach (GameObject Champ_array in Champ_all)
            {
                Champ_array.GetComponent<Button>().interactable = false;
            }

            for (int i = 0; i < 5; i++)
            {
                RedPlayer[i].transform.parent.transform.parent.GetComponent<Button>().interactable = false;
                BluePlayer[i].transform.parent.transform.parent.GetComponent<Button>().interactable = false;
            }
        }
        else
        {
            foreach (GameObject Champ_array in Champ_all)
            {
                Champ_array.GetComponent<Button>().interactable = true;
            }

            for (int i = 0; i < 5; i++)
            {
                RedPlayer[i].transform.parent.transform.parent.GetComponent<Button>().interactable = true;
                BluePlayer[i].transform.parent.transform.parent.GetComponent<Button>().interactable = true;
            }
        }
    }

    public void OnLogoutButtonClicked()
    {
        StartCoroutine(LogOutUser(IDInputField.GetComponent<InputField>().text));
        SoundManager_Main.instance.PlayEffectSound(LogoutButton);
        PhotonNetwork.Disconnect();
        StartCoroutine("LoadScene_delay");
    }

    IEnumerator LoadScene_delay()
    {
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    IEnumerator LoginUser(string ID, string PW)
    {
        Membership_Panel.SetActive(false);

        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        form.AddField("PwPost", PW);
        WWW www = new WWW(LoginUserURL, form);

        yield return www;
        Debug.Log("www.text :" + www.text);
        if (www.text == "회원정보가 틀렸습니다.")
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "\"" + IDInputField.GetComponent<InputField>().text + "\"" + "은 가입되지 않은 회원입니다.";
        }
        else if(www.text == "이미 접속중.")
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "\"" + IDInputField.GetComponent<InputField>().text + "\"" + "은 이미 접속중인 회원입니다.";
        }
        else if(www.text == "로그인 성공")
        {
            LoadingObject.SetActive(true);
            PhotonNetwork.LocalPlayer.NickName = IDInputField.GetComponent<InputField>().text;  //입력한 ID받아옴
            if (userState == UserState.offline) PhotonNetwork.ConnectUsingSettings();   //photon연결 시작
        }
    }
    IEnumerator LogOutUser(string ID)
    {
        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        WWW www = new WWW(LogoutUserURL, form);

        yield return www;
        Debug.Log("www.text :" + www.text);
    }

    IEnumerator SignUpUser(string ID, string PW)
    {
        Membership_Panel.SetActive(false);

        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        form.AddField("PwPost", PW);
        WWW www = new WWW(SignUpURL, form);

        yield return www;
        Debug.Log(www.text);

        if (www.text == "저장 성공")
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "회원가입이 되었습니다.";
        }
        else if(www.text =="ID 중복")
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "이미 존재하는 아이디입니다.";
        }
    }

    IEnumerator SignOutUser(string ID, string PW)
    {
        Membership_Panel.SetActive(false);

        WWWForm form = new WWWForm();
        form.AddField("IdPost", ID);
        form.AddField("PwPost", PW);
        WWW www = new WWW(SignOutURL, form);

        yield return www;
        Debug.Log(www.text);

        if (www.text == "저장성공")
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "회원탈퇴가 되었습니다.";
        }
        else if(www.text == "회원 정보가 일치하지 않습니다.")
        {
            MessageBox.SetActive(true);
            MessageBox_Text.GetComponent<Text>().text = "회원 정보가 일치하지 않습니다.";
        }
    }
    private void OnApplicationQuit() => StartCoroutine(LogOutUser(PhotonNetwork.LocalPlayer.NickName));
    public void OnOutRoomButtonClicked() => PhotonNetwork.LeaveRoom();
    public void OnExitButtonClicked() => Application.Quit();
    public void OnHideButtonClicked() => ShowWindow(GetActiveWindow(), 2);
    #endregion
}