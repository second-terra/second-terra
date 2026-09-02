using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform target;
    public float cameraDepth = -30f;

    void Start()
    {
        if (target != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning(
                "[MinimapCamera] Player를 찾지 못했습니다. target을 직접 연결해주세요."
            );
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            cameraDepth
        );
    }
}
