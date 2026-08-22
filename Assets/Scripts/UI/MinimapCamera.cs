using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform target;
    public float cameraDepth = -30f;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
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
