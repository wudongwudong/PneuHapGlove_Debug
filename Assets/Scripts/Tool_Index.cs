using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tool_Index : MonoBehaviour
{    
    void Awake()
    {
        //y = 7;
        //forward = new Vector3(x, y, z);
        ////transform.position = forward;
        //objectScale = new Vector3(iniScale, iniScale, iniScale);
        //objectPosition = new Vector3(0, rObject, 0);
    }



    void FixedUpdate()
    {
        transform.position = TCPClient.Instance.positionIndex;
    }


}