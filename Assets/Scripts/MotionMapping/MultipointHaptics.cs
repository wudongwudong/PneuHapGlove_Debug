using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MultipointHaptics : MonoBehaviour
{
    public Button multiPointDisplayButton;
    public GameObject[,] multipointButtons = new GameObject[3,3];
    public byte[] valveTiming = new byte[2]{4,60};
    public byte[] functionalList0 = new byte[3];
    public byte[] functionalList1 = new byte[3];
    public byte[] functionalList2 = new byte[3];

    private FileStream fs;
    private StreamWriter sw;

    void Start()
    {
        Button[] buf = gameObject.GetComponentsInChildren<Button>();
        int index = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                multipointButtons[i, j] = buf[index].gameObject;
                index++;

                //Debug.Log(multipointButtons[i, j].name);
            }
        }
        multipointButtons[0, 0].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming); });
        multipointButtons[0, 1].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming); });
        multipointButtons[0, 2].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming); });
        multipointButtons[1, 0].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[1, 0], functionalList1[0], valveTiming); });
        multipointButtons[1, 1].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[1, 1], functionalList1[1], valveTiming); });
        multipointButtons[1, 2].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[1, 2], functionalList1[2], valveTiming); });
        multipointButtons[2, 0].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[2, 0], functionalList2[0], valveTiming); });
        multipointButtons[2, 1].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[2, 1], functionalList2[1], valveTiming); });
        multipointButtons[2, 2].GetComponent<Button>().onClick.AddListener(delegate { MultipointButtonClick(multipointButtons[2, 2], functionalList2[2], valveTiming); });

        multiPointDisplayButton.onClick.AddListener(delegate { StartCoroutine(MultipointDisplayOnClick());});

        string filePath = "C:\\Users\\JM\\Desktop\\" + "ScissorCuttingTiming_" + ".csv"; //scissor cutting
        if (File.Exists(filePath))
        {
            fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        }
        else
        {
            fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        }
        sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

        sw.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
        sw.Flush();
    }

    void MultipointButtonClick(GameObject currentButton, byte pneumaticChannel, byte[] valveTiming)
    {
        if (currentButton.GetComponent<Image>().color == Color.white)
        {
            currentButton.GetComponent<Image>().color = Color.red;

            byte[] clutchState = new byte[] { pneumaticChannel, 0 };
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
        }
        else
        {
            currentButton.GetComponent<Image>().color = Color.white;
            byte[] clutchState = new byte[] { pneumaticChannel, 2 };
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
        }
    }

    void MultipointButtonClick(GameObject[] currentButtons, byte[] pneumaticChannels, byte[][] valveTimings)
    {
        bool hapticsOn = false;
        byte[][] clutchStates = new byte[currentButtons.Length][];
        for (int i = 0; i < currentButtons.Length; i++)
        {
            if (currentButtons[i].GetComponent<Image>().color == Color.white)
            {
                currentButtons[i].GetComponent<Image>().color = Color.red;
                clutchStates[i] = new byte[] { pneumaticChannels[i], 0 };
                hapticsOn = true;
            }
            else
            {
                currentButtons[i].GetComponent<Image>().color = Color.white;
                clutchStates[i] = new byte[] { pneumaticChannels[i], 2 };
                hapticsOn = false;
                
            }
        }

        if (hapticsOn)
        {
            Haptics.ApplyHapticsWithTiming(clutchStates, valveTimings);
        }
        else
        {
            Haptics.ApplyHapticsWithTiming(clutchStates, valveTimings);
        }
    }

    private bool cutaneousStart = false;
    public struct CutHapticsPoints
    {
        public bool isSetPres;
        public float cutPoint;
        public byte frequency;
        public byte[][] clutchStates;
        public byte tarPres;
        public byte[][] valveTimings;

        public CutHapticsPoints(bool isSetPres, float cutPoint, byte frequency, byte[][] clutchStates, byte tarPres, byte[][] valveTimings)
        {
            this.isSetPres = isSetPres;
            this.cutPoint = cutPoint;
            this.frequency = frequency;
            this.clutchStates = clutchStates;
            this.tarPres = tarPres;
            this.valveTimings = valveTimings;
        }
    };

    public string whichHand = "Medium Left";
    public Int64[] tickCount = new Int64[37]{1000,
        6047,
        6859,
        6906,
        6953,
        7015,
        7047,
        7062,
        7156,
        7203,
        7265,
        7297,
        7328,
        7359,
        7437,
        7500,
        7531,
        7562,
        7609,
        7656,
        7719,
        7765,
        7828,
        7859,
        7922,
        7922,
        7984,
        8031,
        8094,
        8109,
        8140,
        8219,
        8281,
        8328,
        8359,
        8390,
        8390

    };
    private int index = 0;
    private Int64 tickStart;

    public Queue cutHapticsQueue;
    public CutHapticsPoints cutHapticsPoint;

    private void ConstructCutHapticsQueue()
    {
        cutHapticsQueue = new Queue();
        //cutHapticsQueue.Enqueue(new CutHapticsPoints(true, 0f, 0, new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 } }, 10, new byte[][]{ new byte[] { 0, 0 }, new byte[] { 0, 0 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(true, 0.05f, 0, new byte[][] { new byte[] { 0, 0 } }, 10, new byte[][] { new byte[] { 0, 0 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(true, 0.05f, 0, new byte[][] { new byte[] { 0, 0 } }, 40, new byte[][] { new byte[] { 0, 0 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.11f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.112f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.115f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.117f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.12f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.122f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.125f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.127f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.130f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.132f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.135f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.137f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.140f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.142f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.145f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.147f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.150f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.152f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.155f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.157f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.160f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.162f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.165f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.167f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.170f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.172f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.175f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));


        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.176f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.177f, 0, new byte[][] { new byte[] { 0, 0 } }, 0, new byte[][] { new byte[] { 8, 5 } }));

        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.185f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.187f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 4, 5 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.190f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 5 } }));

        //cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.22f, 0, new byte[][] { new byte[] { 0, 0 }, new byte[] { 1, 0 } }, 0, new byte[][] { new byte[] { 100, 5 }, new byte[] { 100, 5 } }));
        //cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.23f, 0, new byte[][] { new byte[] { 0, 2 }, new byte[] { 1, 2 } }, 0, new byte[][] { new byte[] { 0, 30 }, new byte[] { 0, 30 } }));

        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.191f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 15 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.192f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 15 } }));
        cutHapticsQueue.Enqueue(new CutHapticsPoints(false, 0.193f, 0, new byte[][] { new byte[] { 0, 2 } }, 0, new byte[][] { new byte[] { 0, 200 } }));
    }

    void FixedUpdate()
    {
        if (cutaneousStart)
        {
            if (index == tickCount.Length)
            {
                cutaneousStart = false;
                return;
            }
            if ((Environment.TickCount - tickStart) > tickCount[index])
            {
                cutHapticsPoint = (CutHapticsPoints)cutHapticsQueue.Dequeue();
                index++;

                if (cutHapticsPoint.isSetPres)
                {
                    Haptics.ApplyHaptics(cutHapticsPoint.frequency, cutHapticsPoint.clutchStates, cutHapticsPoint.tarPres);

                    sw.WriteLine("Indenter Haptics timing" + "," + Environment.TickCount);
                    sw.Flush();
                }
                else
                {
                    Haptics.ApplyHapticsWithTiming(cutHapticsPoint.frequency, cutHapticsPoint.clutchStates, cutHapticsPoint.valveTimings);

                    sw.WriteLine("Indenter Haptics timing" + "," + Environment.TickCount);
                    sw.Flush();
                }
            }
        }
    }
    IEnumerator MultipointDisplayOnClick()
    {
        ConstructCutHapticsQueue();
        yield return new WaitForSeconds(0.5f);
        cutaneousStart = true;
        tickStart = Environment.TickCount;
    }


    //IEnumerator MultipointDisplayOnClick()
    //{
    //    MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming);
    //    yield return new WaitForSeconds(0.1f);
    //    MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    yield return new WaitForSeconds(0.1f);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming);
    //    yield return new WaitForSeconds(0.1f);
    //    MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    yield return new WaitForSeconds(0.1f);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming);
    //    yield return new WaitForSeconds(0.1f);
    //    MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming);

    //}

    //IEnumerator MultipointDisplayOnClick()
    //{
    //    MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming);
    //    yield return new WaitForSeconds(0.2f);
    //    MultipointButtonClick(multipointButtons[0, 0], functionalList0[0], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    yield return new WaitForSeconds(0.2f);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming);
    //    yield return new WaitForSeconds(0.2f);
    //    MultipointButtonClick(multipointButtons[0, 2], functionalList0[2], valveTiming);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    yield return new WaitForSeconds(0.2f);
    //    MultipointButtonClick(multipointButtons[0, 1], functionalList0[1], valveTiming);
    //    MultipointButtonClick(multipointButtons[1, 1], functionalList1[1], valveTiming);
    //    yield return new WaitForSeconds(0.2f);
    //    MultipointButtonClick(multipointButtons[1, 1], functionalList1[1], valveTiming);
    //    MultipointButtonClick(multipointButtons[2, 1], functionalList2[1], valveTiming);
    //    yield return new WaitForSeconds(0.2f);
    //    MultipointButtonClick(multipointButtons[2, 1], functionalList2[1], valveTiming);
    //    yield return new WaitForSeconds(1f);

    //    MultipointButtonClick(new GameObject[]{ multipointButtons[0, 0] , multipointButtons[0, 1] , multipointButtons[0, 2] }, new byte[]{0,1,2}, new byte[][]{ valveTiming , valveTiming, valveTiming });
    //    yield return new WaitForSeconds(0.5f);
    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 0], multipointButtons[0, 1], multipointButtons[0, 2] }, new byte[] { 0, 1, 2 }, new byte[][] { valveTiming, valveTiming, valveTiming });
    //    yield return new WaitForSeconds(0.5f);

    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 1], multipointButtons[1, 1], multipointButtons[2, 1] }, new byte[] { 1, 3, 4 }, new byte[][] { valveTiming, valveTiming, valveTiming });
    //    yield return new WaitForSeconds(0.5f);
    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 1], multipointButtons[1, 1], multipointButtons[2, 1] }, new byte[] { 1, 3, 4 }, new byte[][] { valveTiming, valveTiming, valveTiming });
    //    yield return new WaitForSeconds(1f);

    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 0], multipointButtons[0, 1], multipointButtons[0, 2], multipointButtons[1, 1], multipointButtons[2, 1] }, new byte[] { 0, 1, 2, 3, 4 }, new byte[][] { valveTiming, valveTiming, valveTiming, valveTiming, valveTiming });
    //    yield return new WaitForSeconds(0.5f);
    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 0], multipointButtons[0, 1], multipointButtons[0, 2], multipointButtons[1, 1], multipointButtons[2, 1] }, new byte[] { 0, 1, 2, 3, 4 }, new byte[][] { valveTiming, valveTiming, valveTiming, valveTiming, valveTiming });
    //    yield return new WaitForSeconds(0.5f);

    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 0], multipointButtons[0, 1], multipointButtons[0, 2], multipointButtons[1, 1], multipointButtons[2, 1] }, new byte[] { 0, 1, 2, 3, 4 }, new byte[][] { new byte[]{20,80}, new byte[] { 20, 80 }, new byte[] { 20, 80 }, new byte[] { 20, 80 }, new byte[] { 20, 80 } });
    //    yield return new WaitForSeconds(0.5f);
    //    MultipointButtonClick(new GameObject[] { multipointButtons[0, 0], multipointButtons[0, 1], multipointButtons[0, 2], multipointButtons[1, 1], multipointButtons[2, 1] }, new byte[] { 0, 1, 2, 3, 4 }, new byte[][] { new byte[] { 20, 80 }, new byte[] { 20, 80 }, new byte[] { 20, 80 }, new byte[] { 20, 80 }, new byte[] { 20, 80 } });
    //    yield return new WaitForSeconds(1f);
    //}
}
