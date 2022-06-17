using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViveTrackerLeft : MonoBehaviour
{
    private Transform masterTransform;
    private Rigidbody rb;
    private Vector3 posDir = Vector3.zero;
    public float posSpeed = 1000f;
    public float rotSpeed = 100f;

    void Start()
    {
        masterTransform = GameObject.Find("ViveTracker_Left").transform;
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //Follow position
        posDir = masterTransform.position - transform.position; 
        rb.velocity = posDir * posSpeed * Time.deltaTime;

        //Follow rotation
        Quaternion deltaRotation = masterTransform.rotation * Quaternion.Inverse(rb.rotation);
        float angle = 0.0f;
        Vector3 axis = Vector3.zero;

        deltaRotation.ToAngleAxis(out angle, out axis);
        if (float.IsInfinity(axis.x)) { return; }
        if (angle > 180f) { angle -= 360f; };

        //angle *= Mathf.Deg2Rad;
        Vector3 angularVelocity = angle * axis * rotSpeed * Time.deltaTime;
        rb.angularVelocity = angularVelocity;

        //Debug.Log(rb.angularVelocity.magnitude);
        //rb.MoveRotation(masterTransform.rotation);
    }
}
