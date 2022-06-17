using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    GraspingLeft graspLeftScript;
    Int64 enterTime = 0;
    Int64 exitTime = 0;
    private int delayColEnterTime = 500; //ms
    private int delayColExitTime = 500;  //ms
    private bool delayColExit = false;
    private bool delayColEnter = false;
    private bool colEnterSend = false;
    private bool colEntered = false;

    private Collider bufCol;

    void Start()
    {
        graspLeftScript = GetComponentInParent<GraspingLeft>();
    }

    void FixedUpdate()
    {
        if (delayColExit == true)
        {
            if (System.Environment.TickCount > enterTime + delayColExitTime)
            {
                delayColExit = false;
                if ((colEntered == false) & (colEnterSend == true))
                {
                    graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Exit");
                    exitTime = System.Environment.TickCount;
                    delayColEnter = true;
                    colEnterSend = false;
                    Debug.Log("haptics off");
                }
            }
        }

        if (delayColEnter == true)
        {
            if (Environment.TickCount > exitTime + delayColEnterTime)
            {
                delayColEnter = false;
                if ((colEntered == true) & (colEnterSend == false))
                {
                    graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Enter");
                    enterTime = System.Environment.TickCount;
                    colEnterSend = true;
                    delayColExit = true;
                    Debug.Log("haptics on");
                }
            }
        }

    }

    void OnTriggerEnter(Collider collider)
    {
        colEntered = true;
        bufCol = collider;
        // 加防抖
        if ((delayColEnter == false) & (colEnterSend == false))
        {
            graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Enter");
            enterTime = System.Environment.TickCount;
            colEnterSend = true;
            delayColExit = true;
            Debug.Log("haptics on");
        }

    }

    void OnTriggerExit(Collider collider)
    {
        colEntered = false;

        bufCol = collider;
        // 加防抖
        if ((delayColExit == false) & (colEnterSend == true))
        {
            graspLeftScript.ChildColliderState(bufCol, gameObject.name, "Exit");
            exitTime = System.Environment.TickCount;
            delayColEnter = true;
            colEnterSend = false;
            Debug.Log("haptics off");
        }

    }


}
