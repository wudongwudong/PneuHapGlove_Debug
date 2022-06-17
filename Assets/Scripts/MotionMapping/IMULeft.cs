#define BT_Commu

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

public class IMULeft : MonoBehaviour
{
    static IMULeft _instance;
    public static IMULeft Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new IMULeft();
            }
            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;
    }

    Quaternion initialRotation;
    Quaternion imuInitialRotation;
    bool flag_InitialRotation = true;

    Vector3 initialPosition;
    Vector3 imuinitialAcceleration;
    bool flag_firstAcceleration = true;

    float speedFactor = 0.1f;

    Rigidbody palmRigidbody;

    void Start()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;

        palmRigidbody = GetComponentInParent<Rigidbody>();
    }


    


    public void UpdateRotationLeft()
    {
#if BT_Commu
        if (BTCommu_Left.Instance.flag_RotationDataReady == false)
        {
            return;
        }
        if (flag_InitialRotation == true)
        {
            imuInitialRotation = BTCommu_Left.Instance.rotation;
            flag_InitialRotation = false;
            return;
        }

        Quaternion offsetRotation = Quaternion.Inverse(imuInitialRotation) * BTCommu_Left.Instance.rotation;
        //transform.localRotation = initialRotation * offsetRotation;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, initialRotation * offsetRotation, Time.deltaTime * speedFactor);

#else
        if (SerialCommu_Left.Instance.flag_RotationDataReady == false)
        {
            return;
        }
        if (flag_InitialRotation == true)
        {
            gyroInitialRotation = SerialCommu_Left.Instance.rotation;
            flag_InitialRotation = false;
        }

        Quaternion offsetRotation = Quaternion.Inverse(gyroInitialRotation) * SerialCommu_Left.Instance.rotation;
        //transform.localRotation = initialRotation * offsetRotation;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, initialRotation * offsetRotation, Time.deltaTime * speedFactor);
#endif
    }

    private float accOffset = 0.01f;
    public void UpdatePositionLeft(Vector3 imuThisAcceleration)
    {
        if (BTCommu_Left.Instance.flag_PositionDataReady == false)
        {
            return;
        }
        if (flag_firstAcceleration == true)
        {
            imuinitialAcceleration = imuThisAcceleration;
            flag_firstAcceleration = false;
            return;
        }

        float buf_a = imuThisAcceleration.magnitude - imuinitialAcceleration.magnitude;
        //Debug.Log(buf_a);
        if (Math.Abs(buf_a) < accOffset)
        {
            return;
        }
        v += (imuThisAcceleration - imuinitialAcceleration) * Time.deltaTime;
        transform.position += v * Time.deltaTime;
        Debug.Log(v.x + "\t" + v.y + "\t" + v.z);
        //Debug.Log(transform.position.x + "\t" + transform.position.y + "\t" + transform.position.z);

        //transform.localPosition += new Vector3((imuThisAcceleration.x - imuinitialAcceleration.x) * Time.deltaTime, transform.localPosition.y, transform.localPosition.z);
    }

    private static Vector3 v = Vector3.zero;
    //palmRigidbody.velocity = (acceleration - imuInitialAcceleration) * Time.deltaTime *1000;
    //Debug.Log(palmRigidbody.velocity.x + "\t" + palmRigidbody.velocity.y + "\t" + palmRigidbody.velocity.z);

}
