using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FrequencyTest : MonoBehaviour
{
    public byte frequency_Hz = 1;
    public byte fingerID = 1;
    
    private float beatHitInterval = 0;
    private float beatHitInterval_half = 0;
    private float beatStayInterval = 0;
    private float beatStayInterval_buf = 0;
    private System.Random rdm = new System.Random();
    private bool beatOn = false;

    void Start()
    {
        beatHitInterval = (float)1 / frequency_Hz;
        beatHitInterval_half = (float) beatHitInterval / 2;
        beatStayInterval = (float)300 / 1000; //s
        beatStayInterval_buf = beatStayInterval;
        Debug.Log("Beat Hit Interval: " + beatHitInterval);
        Debug.Log("Beat Hit Interval Half: " + beatHitInterval_half);
    }

    public byte duty = 0;
    public void OnButtonClick()
    {
        if (beatOn == false)
        {
            beatOn = true;
            beatHitInterval_half = (float)beatHitInterval / 2;

            byte[] clutchState = { fingerID, 0 };
            byte[] valveTiming = { valveOnTiming, 0 };


            if ((500 / frequency_Hz) > 255)
            {
                valveTiming[1] = 250;
            }
            else
            {
                valveTiming[1] = (byte) (500 / frequency_Hz);
            }

            valveTiming[1] = duty;

            Haptics.ApplyHapticsWithTiming(frequency_Hz, clutchState, valveTiming);
            //Haptics.ApplyHaptics(clutchState, targetPres);
            beatHapticsIsApplied = true;
        }
        else
        {
            beatOn = false;
            if (beatHapticsIsApplied)
            {
                //byte[] clutchState = new byte[] { fingerID, 2 };
                //Haptics.ApplyHaptics(clutchState, targetPres);
                //Debug.Log("Beat Off: " + fingerID + "\t" + targetPres);

                byte[] clutchState = { fingerID, 2};
                Haptics.ApplyHapticsWithTiming(frequency_Hz, clutchState, new byte[] {0, 255});
                //Haptics.ApplyHaptics(clutchState, targetPres);

                //byte[][] clutchStates =
                //    {new byte[] {0, 2}, new byte[] {1, 2}, new byte[] {2, 2}, new byte[] {3, 2}, new byte[] {4, 2}};
                //Haptics.ApplyHaptics(clutchStates, targetPres);

                beatHapticsIsApplied = false;
                beatHitInterval = (float)1 / frequency_Hz;
                //beatStayInterval_buf = beatStayInterval;
            }
        }
    }


    //private void OnTriggerEnter(Collider collider)
    //{
    //    if (collider.name == "L_Palm")
    //    {
    //        beatOn = true;
    //    }
    //}
    //private void OnTriggerExit(Collider collider)
    //{
    //    if (collider.name == "L_Palm")
    //    {
    //        beatOn = false;
    //        if (beatHapticsIsApplied)
    //        {
    //            //byte[] clutchState = new byte[] { fingerID, 2 };
    //            //Haptics.ApplyHaptics(clutchState, targetPres);
    //            //Debug.Log("Beat Off: " + fingerID + "\t" + targetPres);

    //            byte[][] clutchStates =
    //                {new byte[] {0, 2}, new byte[] {1, 2}, new byte[] {2, 2}, new byte[] {3, 2}, new byte[] {4, 2}};
    //            Haptics.ApplyHaptics(clutchStates, targetPres);

    //            beatHapticsIsApplied = false;
    //            beatStayInterval_buf = beatStayInterval;
    //        }
    //    }
    //}

    private bool beatHapticsIsApplied = false;
    private byte targetPres = 20;
    public byte valveOnTiming = 20;
    void Update()
    {
        //if (beatOn)
        //{
        //    beatHitInterval -= Time.deltaTime;

        //    if (beatHitInterval <= beatHitInterval_half)
        //    {
        //        if (beatHapticsIsApplied == false)
        //        {
        //            byte[] clutchState = { 1, 0 };
        //            byte[] valveTiming = {valveOnTiming, 0};
        //            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
        //            //Haptics.ApplyHaptics(clutchState, targetPres);
        //            beatHapticsIsApplied = true;
        //            Debug.Log("Air In: " + Time.time);
        //        }

        //        if (beatHitInterval <= 0)
        //        {
        //            byte[] clutchState = { 1, 2 };
        //            byte[] valveTiming;
        //            if ((500/frequency_Hz-5) > 255)
        //            {
        //                valveTiming = new byte[] {0, 250};
        //                //Debug.Log("ValveOff time: " + valveTiming[1]);
        //                Debug.Log("Exhaust: " + Time.time);
        //            }
        //            else
        //            {
        //                valveTiming = new byte[] {0, (byte) (500 / frequency_Hz - 5)};
        //                //Debug.Log("ValveOff time: " + valveTiming[1]);
        //                Debug.Log("Exhaust: " + Time.time);
        //            }
        //            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
        //            //Haptics.ApplyHaptics(clutchState, targetPres);
        //            beatHapticsIsApplied = false;
        //            beatHitInterval = (float)1 / frequency_Hz;
        //        }
        //    }

        //}
    }
}
