using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ConsistencyTest : MonoBehaviour
{
    private static bool start = false;
    Thread thread = new Thread(ActuationConsistencyApplyHaptics);
    private static float myTimer = 0;

    void Update()
    {
        //if (start)
        //{
        //    myTimer += Time.deltaTime;
        //    if (myTimer >= 4)
        //    {
        //        ActuationConsistencyApplyHaptics();
        //    }
        //}

    }

    public void OnClick()
    {
        if (start == false)
        {
            thread.IsBackground = true;
            thread.Start();
            start = true;
        }
        else
        {
            thread.Abort();
            start = false;
        }
    }

    private static void ActuationConsistencyApplyHaptics()
    {
        for (byte i = 0; i < 5; i++)                //5手指
        {
            for (byte j = 6; j <= 6; j++)           //10-60kpa
            {
                for (int k = 0; k < 6; k++)         //6次
                {
                    byte[] clutchState = new byte[] {i, 0};
                    byte tarPres = (byte) (j * 10);
                    Haptics.ApplyHaptics(clutchState, tarPres, false);
                    Thread.Sleep(3000);

                    clutchState = new byte[] {i, 2};
                    Haptics.ApplyHaptics(clutchState, tarPres, false);
                    Thread.Sleep(1000);
                }
            }
        }

        //start = false;
    }
}
