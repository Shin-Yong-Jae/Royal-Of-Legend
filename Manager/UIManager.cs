using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class UIManager : MonoBehaviourPunCallbacks
{
    public CharacterControl ch;
    public static UIManager instance;
    [Header("Time")]
    public Text timeText;
    private float PlayingTime;
    private int PlayingMinute = 0;
    [Header("Kill/Death")]
    public Text KDText;
    [Header("Skill")]
    public Image img_Q;
    public Image img_F;
    public Image img_E;
    public Image img_EF;


    public bool qCooldone = true;
    public bool eCooldone = true;
    //public PhotonView PV;
    public Status status;

    [Header("Status")]
    //public Status status;
    public GameObject player;
    public Text AD_Text;
    public Text AP_Text;
    public Text MS_Text;
    public Text AS_Text;
    public Text HP_Text;
    public Image HpGage;
    public Text MP_Text;
    public Image MpGage;
    public Text Level;
    public Image ExpGage;

    void Awake()
    {
        if (!(PhotonNetwork.IsMasterClient)) return;
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        //status = player.GetComponent<Status>();
        //status.currHp = status.MaxHp;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        //Skill CoolTime UI
        if (player != null && player.GetComponent<CharacterControl>().qCoolTimeDone == true)
        {
            qCooldone = true;
        }
        if (player != null && player.GetComponent<CharacterControl>().qCoolTimeDone == false && qCooldone == true)
        {
            Debug.Log("코루틴 제대로 들어옴@@@@@@@@@@");
            StartCoroutine(CoolTime(GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>().qSkillCoolTime));
            qCooldone = false;
            Debug.Log(GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>().qSkillCoolTime);
        }

        if (player != null && player.GetComponent<CharacterControl>().eCoolTimeDone == true)
        {
            eCooldone = true;
        }
        if (player != null && player.GetComponent<CharacterControl>().eCoolTimeDone == false && eCooldone == true)
        {
            Debug.Log("코루틴 제대로 들어옴@@@@@@@@@@");
            StartCoroutine(ECoolTime(GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>().eSkillCoolTime));
            eCooldone = false;
            Debug.Log(GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>().eSkillCoolTime);
        }

        //Status UI
        AD_Text.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().AD.ToString();
        AP_Text.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().AP.ToString();
        MS_Text.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().moveSpeed.ToString();
        AS_Text.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().attackSpeed.ToString();
        HP_Text.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().CurrHp.ToString() + " / " +
                       GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().MaxHp.ToString();
        MP_Text.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().CurrMp.ToString() + " / " +
                       GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().MaxMp.ToString();
        HpGage.GetComponent<Image>().fillAmount = (GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().CurrHp /
                                                   GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().MaxHp);
        MpGage.GetComponent<Image>().fillAmount = (GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().CurrMp /
                                                  GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().MaxMp);
        Level.text = GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().Level.ToString();
        ExpGage.GetComponent<Image>().fillAmount = (GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().CurrExp /
                                                   GameObject.FindGameObjectWithTag("Player").GetComponent<Status>().MaxExp);

        //Timer UI
        if (PhotonNetwork.IsMasterClient)
        {
            PlayingTime += Time.deltaTime;
            timeText.text = string.Concat(PlayingMinute.ToString() + ":") + Mathf.Ceil(PlayingTime - 1.0f).ToString();
            if (PlayingTime >= 59.9f)
            {
                PlayingTime = 0f;
                PlayingMinute++;
            }
        }
        else
        {
            PlayingTime += Time.deltaTime;
            timeText.text = string.Concat(PlayingMinute.ToString() + ":") + Mathf.Ceil(PlayingTime).ToString();
            if (PlayingTime >= 59.9f)
            {
                PlayingTime = 0f;
                PlayingMinute++;
            }
        }
        timeText.text = string.Format("{0:00}:{1:00}", PlayingMinute, PlayingTime);
        KDText.text = string.Concat(player.GetComponent<Status>().Kill.ToString() + " / " + player.GetComponent<Status>().Death.ToString());

    }
    IEnumerator CoolTime(float cool)
    {
        while (img_Q.fillAmount > 0)
        {
            img_Q.fillAmount -= 1 * Time.deltaTime / cool;
            yield return new WaitForFixedUpdate();
        }
        if (img_Q.fillAmount <= 0)
        {
            img_Q.fillAmount = 1.0f;
        }
    }
    IEnumerator ECoolTime(float cool)
    {
        while (img_E.fillAmount > 0)
        {
            img_E.fillAmount -= 1 * Time.deltaTime / cool;
            yield return new WaitForFixedUpdate();
        }
        if (img_E.fillAmount <= 0)
        {
            img_E.fillAmount = 1.0f;
        }
    }
}