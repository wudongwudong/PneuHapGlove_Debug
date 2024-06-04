using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BTHighHzTest : MonoBehaviour
{
    private bool startSend = false;
    public float hz;
    private float timer = 0;

    private byte[] clutchState = { 2, 2 };
    private byte[] valveTiming = { 10, 10 };

    void Start()
    {
        
    }

    void Update()
    {
        if (startSend & (hz != 0))
        {
            timer += Time.deltaTime;
            if (timer >= 1/hz)
            {
                Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
                timer = 0;
            }
        }
    }

    public void OnButtonClick()
    {
        if (startSend == false)
        {
            startSend = true;
        }
        else
        {
            startSend = false;
            timer = 0;
        }
    }
}
