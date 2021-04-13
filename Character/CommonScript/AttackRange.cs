using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AttackRange : MonoBehaviour
{
    private float attackRange = 0f; // 캐릭 공격사거리;
    CharacterControl cc;
    [Range(0, 50)]
    public int segments = 50;
    LineRenderer line;

    private void Start()
    {
        cc = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterControl>();
    }

    // Update is called once per frame
    private void Update()
    {
        attackRange = cc.GetComponent<Status>().attackRange;
        line = gameObject.GetComponent<LineRenderer>();
        line.positionCount = segments + 1;
        line.useWorldSpace = false;
        line.material.color = new Color(0f, 0f, 1f, 1f);
        CreatePoints();
    }

    void CreatePoints()
    {
        float x;
        float y;

        float angle = 20f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin(Mathf.Deg2Rad * angle) * attackRange;
            y = Mathf.Cos(Mathf.Deg2Rad * angle) * attackRange;

            line.SetPosition(i, new Vector2(y, x));
            angle += (360.0f / segments);
        }
    }
}
