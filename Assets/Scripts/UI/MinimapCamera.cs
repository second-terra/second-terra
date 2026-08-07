using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform target;
    public float height = 30f;

    void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = new Vector3(
            target.position.x,
            height,
            target.position.z
        );
    }
}
