using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Haptics : MonoBehaviour
{
    public static byte[] SetClutchState(String bufName, String bufState)
    {
        //´¥·¢Clutch
        byte[] clutchState = new byte[2] { 0xff, 0xff };
        switch (bufName)
        {
            case "GhostThumbB":
                clutchState[0] = 0;
                break;
            case "GhostIndexC":
                clutchState[0] = 1;
                break;
            case "GhostMiddleC":
                clutchState[0] = 2;
                break;
            case "GhostRingC":
                clutchState[0] = 3;
                break;
            case "GhostPinkyC":
                clutchState[0] = 4;
                break;
            default:
                break;
        }

        switch (bufState)
        {
            case "Enter":
                clutchState[1] = 0;
                break;
            case "Stay":
                clutchState[1] = 1;
                break;
            case "Exit":
                clutchState[1] = 2;
                break;
            default:
                break;
        }
        return clutchState;
    }

    /// <summary>
    /// To one finger
    /// </summary>
    /// <param name="clutchState"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(byte[] clutchState, byte targetPres)
    {
        byte frequency = 0;
        if ((targetPres > 0) & (targetPres < 10))
        {
            frequency = 0xff;
        }
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
        byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            byte vOpen = valveTiming[0];
            byte vDelay = valveTiming[1];
            //±àÂë+BT·¢ËÍ
            Encode.Instance.add_u8(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_u8(targetPres);
            Encode.Instance.add_u8(vOpen);
            Encode.Instance.add_u8(vDelay);
            byte[] buf = Encode.Instance.add_fun(0x03);       // FI = 3
            Encode.Instance.clear_list();
            BTCommu_Left.Instance.BTSend(buf);
            //Debug.Log(BitConverter.ToString(buf));
        }
    }

    /// <summary>
    /// Apply vibration to one finger
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="clutchState"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(byte frequency, byte[] clutchState, byte targetPres)
    {
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
        byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            byte vOpen = valveTiming[0];
            byte vDelay = valveTiming[1];
            //±àÂë+BT·¢ËÍ
            Encode.Instance.add_u8(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_u8(targetPres);
            Encode.Instance.add_u8(vOpen);
            Encode.Instance.add_u8(vDelay);
            byte[] buf = Encode.Instance.add_fun(0x03);       // FI = 3
            Encode.Instance.clear_list();
            BTCommu_Left.Instance.BTSend(buf);
            //Debug.Log(BitConverter.ToString(buf));
        }
    }

    /// <summary>
    /// To multiple fingers
    /// </summary>
    /// <param name="clutchStates"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(byte[][] clutchStates, byte targetPres)
    {
        byte frequency = 0;
        if ((targetPres > 0) & (targetPres < 10))
        {
            frequency = 0xff;
        }
        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < clutchStates.Length; i++)
        {
            HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], targetPres));
        }

        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
    }

    /// <summary>
    /// Apply vibration to multiple fingers
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="clutchStates"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(byte frequency, byte[][] clutchStates, byte targetPres)
    {
        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < clutchStates.Length; i++)
        {
            HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], targetPres));
        }

        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
    }

    private static byte[] ConstructHapticsFrame(byte frequency, byte[] clutchState, byte targetPres)
    {
        byte fingerID = clutchState[0];
        byte status = clutchState[1];
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];

        byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, fingerID, presSource);
        byte vOpen = valveTiming[0];
        byte vDelay = valveTiming[1];
        //±àÂë+BT·¢ËÍ
        Encode.Instance.add_u8(frequency);
        Encode.Instance.add_u8(fingerID); // which finger
        Encode.Instance.add_u8(status); // enter, stay or exit
        Encode.Instance.add_u8(targetPres);
        Encode.Instance.add_u8(vOpen);
        Encode.Instance.add_u8(vDelay);
        byte[] buf = Encode.Instance.add_fun(0x03); // FI = 3
        Encode.Instance.clear_list();

        return buf;
    }

    /// <summary>
    /// To one finger
    /// </summary>
    /// <param name="clutchState"></param>
    /// <param name="valveTiming"></param>
    public static void ApplyHapticsWithTiming(byte[] clutchState, byte[] valveTiming)
    {
        byte frequency = 0;
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            byte vOpen = valveTiming[0];
            byte vDelay = valveTiming[1];
            //±àÂë+BT·¢ËÍ
            Encode.Instance.add_u8(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_u8(0);
            Encode.Instance.add_u8(vOpen);
            Encode.Instance.add_u8(vDelay);
            byte[] buf = Encode.Instance.add_fun(0x03);       // FI = 3
            Encode.Instance.clear_list();
            BTCommu_Left.Instance.BTSend(buf);
            //Debug.Log(BitConverter.ToString(buf));
        }
    }

    /// <summary>
    /// Apply vibration to one finger
    /// </summary>
    /// <param name="clutchState"></param>
    /// <param name="valveTiming"></param>
    public static void ApplyHapticsWithTiming(byte frequency, byte[] clutchState, byte[] valveTiming)
    {
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            byte vOpen = valveTiming[0];
            byte vDelay = valveTiming[1];
            //±àÂë+BT·¢ËÍ
            Encode.Instance.add_u8(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_u8(0);
            Encode.Instance.add_u8(vOpen);
            Encode.Instance.add_u8(vDelay);
            byte[] buf = Encode.Instance.add_fun(0x03);       // FI = 3
            Encode.Instance.clear_list();
            BTCommu_Left.Instance.BTSend(buf);
            //Debug.Log(BitConverter.ToString(buf));
        }
    }

    /// <summary>
    /// To multiple fingers
    /// </summary>
    /// <param name="clutchStates"></param>
    /// <param name="valveTimings"></param>
    public static void ApplyHapticsWithTiming(byte[][] clutchStates, byte[][] valveTimings)
    {
        byte frequency = 0;
        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < clutchStates.Length; i++)
        {
            HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], valveTimings[i]));
        }

        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
    }

    /// <summary>
    /// Apply vibration to multiple fingers
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="clutchStates"></param>
    /// <param name="valveTimings"></param>
    public static void ApplyHapticsWithTiming(byte frequency, byte[][] clutchStates, byte[][] valveTimings)
    {
        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < clutchStates.Length; i++)
        {
            HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], valveTimings[i]));
        }

        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
    }

    private static byte[] ConstructHapticsFrame(byte frequency, byte[] clutchState, byte[] valveTiming)
    {
        byte fingerID = clutchState[0];
        byte status = clutchState[1];
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];

        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, fingerID, presSource);
        byte vOpen = valveTiming[0];
        byte vDelay = valveTiming[1];
        //±àÂë+BT·¢ËÍ
        Encode.Instance.add_u8(frequency);
        Encode.Instance.add_u8(fingerID); // which finger
        Encode.Instance.add_u8(status); // enter, stay or exit
        Encode.Instance.add_u8(0);
        Encode.Instance.add_u8(vOpen);
        Encode.Instance.add_u8(vDelay);
        byte[] buf = Encode.Instance.add_fun(0x03); // FI = 3
        Encode.Instance.clear_list();

        return buf;
    }

}



//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Haptics : MonoBehaviour
//{
//    public static byte[] SetClutchState(String bufName, String bufState)
//    {
//        //´¥·¢Clutch
//        byte[] clutchState = new byte[2] { 0xff, 0xff };
//        switch (bufName)
//        {
//            case "GhostThumbB":
//                clutchState[0] = 0;
//                break;
//            case "GhostIndexC":
//                clutchState[0] = 1;
//                break;
//            case "GhostMiddleC":
//                clutchState[0] = 2;
//                break;
//            case "GhostRingC":
//                clutchState[0] = 3;
//                break;
//            case "GhostPinkyC":
//                clutchState[0] = 4;
//                break;
//            default:
//                break;
//        }

//        switch (bufState)
//        {
//            case "Enter":
//                clutchState[1] = 0;
//                break;
//            case "Stay":
//                clutchState[1] = 1;
//                break;
//            case "Exit":
//                clutchState[1] = 2;
//                break;
//            default:
//                break;
//        }
//        return clutchState;
//    }

//    /// <summary>
//    /// To one finger
//    /// </summary>
//    /// <param name="clutchState"></param>
//    /// <param name="targetPres"></param>
//    public static void ApplyHaptics(byte[] clutchState, byte targetPres)
//    {
//        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
//        byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

//        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
//        {
//            byte vOpen = valveTiming[0];
//            byte vDelay = valveTiming[1];
//            //±àÂë+BT·¢ËÍ
//            Encode.Instance.add_u8(clutchState[0]);             // which finger
//            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
//            Encode.Instance.add_u8(targetPres);
//            Encode.Instance.add_u8(vOpen);
//            Encode.Instance.add_u8(vDelay);
//            byte[] buf = Encode.Instance.add_fun(0x03);       // FI = 3
//            Encode.Instance.clear_list();
//            BTCommu_Left.Instance.BTSend(buf);
//            //Debug.Log(BitConverter.ToString(buf));
//        }
//    }

//    /// <summary>
//    /// To multiple fingers
//    /// </summary>
//    /// <param name="clutchStates"></param>
//    /// <param name="targetPres"></param>
//    public static void ApplyHaptics(byte[][] clutchStates, byte targetPres)
//    {
//        List<byte> HapticsFrame = new List<byte>();

//        for (int i = 0; i < clutchStates.Length; i++)
//        {
//            HapticsFrame.AddRange(ConstructHapticsFrame(clutchStates[i], targetPres));
//        }

//        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
//    }

//    private static byte[] ConstructHapticsFrame(byte[] clutchState, byte targetPres)
//    {
//        byte fingerID = clutchState[0];
//        byte status = clutchState[1];
//        int presSource = (int)BTCommu_Left.Instance.pressureData[5];

//        byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, fingerID, presSource);
//        byte vOpen = valveTiming[0];
//        byte vDelay = valveTiming[1];
//        //±àÂë+BT·¢ËÍ
//        Encode.Instance.add_u8(fingerID); // which finger
//        Encode.Instance.add_u8(status); // enter, stay or exit
//        Encode.Instance.add_u8(targetPres);
//        Encode.Instance.add_u8(vOpen);
//        Encode.Instance.add_u8(vDelay);
//        byte[] buf = Encode.Instance.add_fun(0x03); // FI = 3
//        Encode.Instance.clear_list();

//        return buf;
//    }

//    /// <summary>
//    /// To one finger
//    /// </summary>
//    /// <param name="clutchState"></param>
//    /// <param name="targetPres"></param>
//    /// <param name="valveTiming"></param>
//    public static void ApplyHapticsWithTiming(byte[] clutchState, byte[] valveTiming)
//    {
//        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
//        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

//        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
//        {
//            byte vOpen = valveTiming[0];
//            byte vDelay = valveTiming[1];
//            //±àÂë+BT·¢ËÍ
//            Encode.Instance.add_u8(clutchState[0]);             // which finger
//            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
//            Encode.Instance.add_u8(0);
//            Encode.Instance.add_u8(vOpen);
//            Encode.Instance.add_u8(vDelay);
//            byte[] buf = Encode.Instance.add_fun(0x03);       // FI = 3
//            Encode.Instance.clear_list();
//            BTCommu_Left.Instance.BTSend(buf);
//            //Debug.Log(BitConverter.ToString(buf));
//        }
//    }

//    /// <summary>
//    /// To multiple finger
//    /// </summary>
//    /// <param name="clutchState"></param>
//    /// <param name="targetPres"></param>
//    /// <param name="valveTiming"></param>
//    public static void ApplyHapticsWithTiming(byte[][] clutchStates, byte[][] valveTimings)
//    {
//        List<byte> HapticsFrame = new List<byte>();

//        for (int i = 0; i < clutchStates.Length; i++)
//        {
//            HapticsFrame.AddRange(ConstructHapticsFrame(clutchStates[i], valveTimings[i]));
//        }

//        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
//    }

//    private static byte[] ConstructHapticsFrame(byte[] clutchState, byte[] valveTiming)
//    {
//        byte fingerID = clutchState[0];
//        byte status = clutchState[1];
//        int presSource = (int)BTCommu_Left.Instance.pressureData[5];

//        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, fingerID, presSource);
//        byte vOpen = valveTiming[0];
//        byte vDelay = valveTiming[1];
//        //±àÂë+BT·¢ËÍ
//        Encode.Instance.add_u8(fingerID); // which finger
//        Encode.Instance.add_u8(status); // enter, stay or exit
//        Encode.Instance.add_u8(0);
//        Encode.Instance.add_u8(vOpen);
//        Encode.Instance.add_u8(vDelay);
//        byte[] buf = Encode.Instance.add_fun(0x03); // FI = 3
//        Encode.Instance.clear_list();

//        return buf;
//    }

//}
