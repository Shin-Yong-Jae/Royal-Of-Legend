using UnityEngine;
using System.Collections;

public class Billboard : MonoBehaviour
{
    Transform cam;
    private void Start()
    {
        StartCoroutine("WaitAndFindPlayer");
    }

    private void Update()
    {
        if (cam) transform.LookAt(transform.position + cam.rotation * Vector3.forward, cam.rotation * Vector3.up);
    }

    IEnumerator WaitAndFindPlayer()
    {
        yield return new WaitForSeconds(0.5f); //0.5초를 기다려준후
        cam = GameObject.FindWithTag("MainCamera").transform;
    }
}