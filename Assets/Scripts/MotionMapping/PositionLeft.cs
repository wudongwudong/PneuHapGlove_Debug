using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionLeft : MonoBehaviour
{
    private Vector3 palmPositionLeft = Vector3.zero;
    private float movingSpeed = 0.5f;

    void Start()
    {
        palmPositionLeft = transform.position;
    }

    void FixedUpdate()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (Input.GetKey(KeyCode.W))
        {
            palmPositionLeft.z += movingSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            palmPositionLeft.z -= movingSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            palmPositionLeft.x -= movingSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            palmPositionLeft.x += movingSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            palmPositionLeft.y += movingSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            palmPositionLeft.y -= movingSpeed * Time.deltaTime;
        }

        transform.position = palmPositionLeft;
    }
}
