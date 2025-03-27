using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepUpTesti : MonoBehaviour
{
    public float interval_seconds = 5;
    public float intensityStep = 0.1f;
    
    private bool start = false;
    private float time = 0;
    private float intensity = 0f;

    void Update()
    {
        if (start)
        {
            time += Time.deltaTime;

            if (time >= interval_seconds)
            {
                byte[] data;
                if (intensity >= 1)
                {
                    start = false;
                    data = Haptics.HEXRPressure(Haptics.Finger.Thumb, true, 0, 1);
                    BTCommu_Left.Instance.BTSend(data);
                }
                else
                {
                    intensity += intensityStep;
                    data = Haptics.HEXRPressure(Haptics.Finger.Thumb, true, intensity, 1);
                    BTCommu_Left.Instance.BTSend(data);
                }

                time = 0;

                
            }
        }
        else
        {
            intensity = 0;
            time = 0;
        }
    }


    public void EnableTest()
    {
        start = true;
    }
}
