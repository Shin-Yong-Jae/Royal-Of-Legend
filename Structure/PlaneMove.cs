using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneMove : MonoBehaviour
{
    GameObject player;
    Vector3 pos;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        pos.x = transform.position.x;
        pos.z = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            pos.y = player.GetComponent<CharacterControl>().playerTr.position.y;
            transform.position = pos;
        }
    }
}
