using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSmoothFollow : MonoBehaviour
{
    public Transform target;
    public float smoothFactor = 1f;
    private Vector3 targetPos;
    private Vector3 offset;

    void Awake()
    {
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothFactor);
    }
}
