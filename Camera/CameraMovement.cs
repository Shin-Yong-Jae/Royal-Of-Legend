using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CameraMovement : MonoBehaviourPunCallbacks
{
    //화면 이동 속도
    public float panSpeed = 20f;

    //화면테두리
    public float panBorderThickness= 10f;

    //x,z좌표를 활용하여 화면을 이동할때에 제한을 두기위해 Vector2를 사용
    public Vector2 panLimit;

    //휠을 사용하여 화면 zoom in,out을 하기 위함
    public float scrollSpeed = 10;
    public float minY = 5f, maxY = 15f;

    private GameObject player;


    public PhotonView PV;



    void Start()
    {
        //StartCoroutine("WaitAndFindPlayer");
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (PV.IsMine)
        {
            //pos에 현재 위치를 받아옴
            Vector3 pos = transform.position;

            //방향키를 적용했을때 그 방향에 맞게 Input.mousePosition, Screen을 이용하여 화면 이동을 구현
            //Input.mousePosition -> 픽셀 좌표(GameView)에서 마우스 위치를 반환함, 화면 왼쪽 아래가 (0,0), 오른쪽 위 부분은 (Screen.width, Screen.height)를 나타냅니다.
            if (Input.GetKey(KeyCode.UpArrow) || Input.mousePosition.y >= Screen.height - panBorderThickness)
            {
                pos.z += panSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.DownArrow) || Input.mousePosition.y <= panBorderThickness)
            {
                pos.z -= panSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.RightArrow) || Input.mousePosition.x >= Screen.width - panBorderThickness)
            {
                pos.x += panSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.LeftArrow) || Input.mousePosition.x <= panBorderThickness)
            {
                pos.x -= panSpeed * Time.deltaTime;
            }

            if (player != null)
            {
                //space입력시 player위치로 이동
                if (Input.GetKey(KeyCode.Space))
                {
                    pos = player.transform.position;
                    pos.y = 45.0f;
                    pos.z -= 22.0f;
                }
            }
            else
            {
                StartCoroutine("WaitAndFindPlayer");
            }

            //GetAxis는 Unity에서 Edit -> Project settings -> Input 안에있는 값들을 이용할 수 있음
            //마우스 휠을 사용하여 zoom을 구현 -> 마우스 휠의 Name을 찾아서 활용
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            pos.y -= scroll * scrollSpeed * 100f * Time.deltaTime;

            if (scroll > 0 && pos.y > 5) //Zoom-in
            {
                pos.z += scroll * scrollSpeed * 50 * Time.deltaTime;
            }

            if (scroll < 0 && pos.y < 15) //Zoom-out
            {
                pos.z += scroll * scrollSpeed * 50 * Time.deltaTime;
            }

            //Mathf.Clamp메소드를 이용하여 pos.(x,y,z)의 최소값과 최대값을 설정
            //=> 최소값보다 값이 더 낮아질 경우에도 최소값을 반환해준다.
            pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.z = Mathf.Clamp(pos.z, -panLimit.y, panLimit.y);

            //입력받은 pos의 값으로 Camera의 transform을 변경
            transform.position = pos;
        }
        else
        {
            gameObject.SetActive(false);
        }
        
    }
    IEnumerator WaitAndFindPlayer()
    {
        yield return new WaitForSeconds(0.5f); //0.5초를 기다려준후
        player = GameObject.FindGameObjectWithTag("Player");
    }
}
