using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapMaterial : MonoBehaviour
{
    public byte targetPressure = 0;
    public bool isGrasped = false;

    private static byte[] valveCali10kPa = { 15, 15, 13, 13, 12 };
    private static byte[] valveCali20kPa = { 23, 22, 20, 20, 18 };
    private static byte[] valveCali30kPa = { 31, 30, 28, 28, 26 };
    private static byte[] valveCali40kPa = { 50, 49, 48, 50, 48 };

    private static byte[,,] valveCaliOn = new byte[5, 5, 6];
    private static byte[,] valveCaliOff = new byte[5, 6];

    private static byte[,,] valveCaliOn_L = new byte[5, 5, 6]
    {
        {
            {13, 20, 27, 35, 47, 255},//under 69kpa
            {13, 21, 28, 37, 52, 255},
            {14, 21, 28, 37, 52, 255},
            {12, 17, 23, 30, 42, 255},
            {12, 18, 24, 32, 44, 255}
        },
        {
            {14, 21, 29, 38, 53, 255},//under 67kpa
            {13, 21, 29, 39, 56, 255},
            {14, 22, 29, 39, 56, 255},
            {12, 17, 24, 32, 46, 255},
            {12, 19, 25, 34, 49, 255}
        },
        {
            {14, 21, 30, 39, 55, 255},//under 65kpa
            {13, 22, 30, 41, 60, 255},
            {14, 23, 30, 41, 60, 255},
            {12, 18, 25, 33, 49, 255},
            {13, 20, 26, 34, 50, 255}
        },
        {
            {15, 22, 31, 40, 65, 255},//under 63kpa
            {14, 23, 31, 42, 75, 255},
            {15, 23, 31, 42, 75, 255},
            {13, 19, 26, 35, 60, 255},
            {13, 20, 27, 36, 65, 255}
        },
        {
            {15, 23, 32, 42, 92, 255},//under 61kpa
            {14, 23, 32, 43, 100, 255},
            {15, 24, 32, 43, 100, 255},
            {13, 20, 27, 37, 80, 255},
            {13, 20, 28, 38, 85, 255}
        }
    };

    private static byte[,] valveCaliOff_L = new byte[5, 6]
    {
        {100, 130, 160, 180, 190, 220},
        { 80, 130, 150, 170, 180, 220},
        {100, 130, 160, 180, 210, 220},
        {100, 130, 160, 180, 200, 220},
        {100, 150, 170, 190, 210, 220}
    };


    private static byte[,,] valveCaliOn_M = new byte[5, 5, 6]
    {
        {
            {13, 20, 27, 35, 47, 230},//under 69kpa
            {11, 18, 24, 32, 44, 220},
            {12, 20, 27, 35, 48, 230},
            {11, 17, 23, 30, 41, 210},
            {12, 19, 26, 34, 46, 220}
        },
        {
            {13, 21, 28, 37, 50, 255},//under 67kpa
            {11, 19, 26, 34, 47, 255},
            {13, 21, 29, 38, 51, 255},
            {11, 18, 24, 31, 43, 255},
            {12, 20, 27, 35, 49, 255}
        },
        {
            {14, 21, 29, 38, 54, 255},//under 65kpa
            {12, 20, 27, 36, 51, 255},
            {13, 22, 30, 39, 56, 255},
            {11, 18, 25, 33, 47, 255},
            {12, 20, 28, 36, 53, 255}
        },
        {
            {14, 21, 30, 39, 62, 255},//under 63kpa
            {13, 20, 28, 37, 59, 255},
            {14, 22, 30, 40, 65, 255},
            {12, 19, 26, 34, 56, 255},
            {13, 21, 29, 38, 62, 255}
        },
        {
            {14, 22, 31, 41, 90, 255},//under 61kpa
            {13, 21, 28, 38, 86, 255},
            {14, 23, 32, 42, 95, 255},
            {12, 19, 26, 36, 85, 255},
            {13, 21, 29, 40, 90, 255}
        }
    };

    private static byte[,] valveCaliOff_M = new byte[5, 6]
        {
        { 80, 120, 160, 180, 200, 210,},
        {100, 150, 180, 200, 220, 230},
        {110, 160, 200, 220, 230, 250},
        { 90, 140, 170, 180, 190, 220},
        { 90, 140, 170, 180, 190, 220}
        };


    //private static Vector4[,] valveCaliOn_ = new Vector4[5, 5]
    //{
    //    {
    //        new Vector4(13, 24, 40, 150), new Vector4(13, 23, 36, 150), new Vector4(14, 26, 42, 150),
    //        new Vector4(13, 20, 34, 150), new Vector4(12, 23, 37, 150)
    //    },
    //    {
    //        new Vector4(13, 25, 42, 175), new Vector4(13, 23, 37, 175), new Vector4(14, 27, 43, 175),
    //        new Vector4(13, 21, 35, 175), new Vector4(12, 23, 38, 175)
    //    },
    //    {
    //        new Vector4(14, 27, 46, 190), new Vector4(14, 24, 39, 190), new Vector4(15, 29, 46, 190),
    //        new Vector4(14, 22, 37, 190), new Vector4(13, 25, 41, 190)
    //    },
    //    {
    //        new Vector4(15, 28, 50, 250), new Vector4(14, 25, 42, 250), new Vector4(16, 30, 50, 250),
    //        new Vector4(13, 23, 40, 250), new Vector4(14, 26, 44, 250)
    //    },
    //    {
    //        new Vector4(16, 30, 55, 250), new Vector4(15, 27, 46, 250), new Vector4(17, 32, 55, 250),
    //        new Vector4(14, 25, 44, 250), new Vector4(15, 28, 48, 250)
    //    }
    //};

    //private static Vector4[] valveCaliOff_ = new Vector4[5]
    //{
    //    new Vector4(70, 115, 145, 170), new Vector4(60, 92, 120, 150), new Vector4(75, 115, 145, 160),
    //    new Vector4(70, 100, 130, 160), new Vector4(75, 120, 150, 180)
    //};

    void Start()
    {
        if (BTCommu_Left.Instance.deviceName == "PneuHapGlove L")
        {
            valveCaliOn = valveCaliOn_L;
            valveCaliOff = valveCaliOff_L;
        }
        else if (BTCommu_Left.Instance.deviceName == "PneuHapGlove M")
        {
            valveCaliOn = valveCaliOn_M;
            valveCaliOff = valveCaliOff_M;
        }

    }

    public static byte[] CalculateValveTiming(byte tarPres, byte fingerID, int presSource)
    {
        if (BTCommu_Left.Instance.deviceName == "PneuHapGlove L")
        {
            valveCaliOn = valveCaliOn_L;
            valveCaliOff = valveCaliOff_L;
        }
        else if (BTCommu_Left.Instance.deviceName == "PneuHapGlove M")
        {
            valveCaliOn = valveCaliOn_M;
            valveCaliOff = valveCaliOff_M;
        }
        else
        {
            Debug.Log("Invalid Device");
            return null;
        }

        byte[] valveOnOff = new byte[2];

        int presDif = (170000 - presSource) / 2000;
        if (presDif < 0) { presDif = 0; }
        else if (presDif > 4) { presDif = 4; }

        byte[] valveSelectedOn = new byte[6];
        byte[] valveSelectedOff = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            valveSelectedOn[i] = valveCaliOn[presDif, fingerID, i];
            valveSelectedOff[i] = valveCaliOff[fingerID, i];
        }
        //byte[] valveSelectedOn = GetArrayFromVector4(valveCaliOn[presDif, fingerID]);
        //byte[] valveSelectedOff = GetArrayFromVector4(valveCaliOff[fingerID]);

        valveOnOff = GetValveTiming(tarPres, valveSelectedOn, valveSelectedOff);


        return valveOnOff;
    }

    //static byte[] GetArrayFromVector4(Vector4 bufVector4)
    //{
    //    byte[] buf = new byte[4];
    //    buf[0] = Convert.ToByte(bufVector4.x);
    //    buf[1] = Convert.ToByte(bufVector4.y);
    //    buf[2] = Convert.ToByte(bufVector4.z);
    //    buf[3] = Convert.ToByte(bufVector4.w);

    //    return buf;
    //}

    static byte[] GetValveTiming(byte tarPres, byte[] valveSelectedOn, byte[] valveSelectedOff)
    {
        byte[] valveOnOff = new byte[2];

        if (tarPres == 0)
        {
            valveOnOff[0] = 0;
            valveOnOff[1] = 0;

            //valveOnOff[0] = valveSelectedOn[0];
            //valveOnOff[1] = valveSelectedOff[0];
        }
        else if (tarPres < 10)
        {
            valveOnOff[0] = 20; // vib motor on duration
            valveOnOff[1] = tarPres; //pwm duty
        }
        else if (tarPres < 20)
        {
            valveOnOff[0] = (byte)((tarPres - 10) / 10 * (valveSelectedOn[1] - valveSelectedOn[0]) +
                                    valveSelectedOn[0]);
            valveOnOff[1] = (byte)((tarPres - 10) / 10 * (valveSelectedOff[1] - valveSelectedOff[0]) +
                                    valveSelectedOff[0]);
        }
        else if (tarPres < 30)
        {
            valveOnOff[0] = (byte)((tarPres - 20) / 10 * (valveSelectedOn[2] - valveSelectedOn[1]) +
                                    valveSelectedOn[1]);
            valveOnOff[1] = (byte)((tarPres - 20) / 10 * (valveSelectedOff[2] - valveSelectedOff[1]) +
                                    valveSelectedOff[1]);
        }
        else if (tarPres < 40)
        {
            valveOnOff[0] = (byte)((tarPres - 30) / 10 * (valveSelectedOn[3] - valveSelectedOn[2]) +
                                    valveSelectedOn[2]);
            valveOnOff[1] = (byte)((tarPres - 30) / 10 * (valveSelectedOff[3] - valveSelectedOff[2]) +
                                    valveSelectedOff[2]);
        }
        else if (tarPres < 50)
        {
            valveOnOff[0] = (byte)((tarPres - 40) / 10 * (valveSelectedOn[4] - valveSelectedOn[3]) +
                                   valveSelectedOn[3]);
            valveOnOff[1] = (byte)((tarPres - 40) / 10 * (valveSelectedOff[4] - valveSelectedOff[3]) +
                                   valveSelectedOff[3]);
        }
        else if (tarPres < 60)
        {
            valveOnOff[0] = (byte)((tarPres - 50) / 10 * (valveSelectedOn[5] - valveSelectedOn[4]) +
                                   valveSelectedOn[4]);
            valveOnOff[1] = (byte)((tarPres - 50) / 10 * (valveSelectedOff[5] - valveSelectedOff[4]) +
                                   valveSelectedOff[4]);
        }
        else
        {
            valveOnOff[0] = valveSelectedOn[5];
            valveOnOff[1] = valveSelectedOff[5];
        }

        return valveOnOff;
    }


    //public byte[] CalculateValveTiming(byte tarPres)
    //{
    //    byte[] valveOnOff = new byte[2];
    //    byte vOpen = 0;
    //    byte vDelay = 0;

    //    switch (tarPres)
    //    {
    //        case 10:
    //            vOpen = 9;
    //            vDelay = 100;
    //            break;
    //        case 20:
    //            vOpen = 15;
    //            vDelay = 100;
    //            break;
    //        case 30:
    //            vOpen = 27;
    //            vDelay = 100;
    //            break;
    //        case 40:
    //            vOpen = 52;
    //            vDelay = 100;
    //            break;
    //        default:
    //            break;
    //    }

    //    valveOnOff[0] = vOpen;
    //    valveOnOff[1] = vDelay;
    //    return valveOnOff;
    //}

}