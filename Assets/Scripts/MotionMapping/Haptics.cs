using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Haptics : MonoBehaviour
{
    public enum FunIndex
    {
        FI_AIR_PRESSURE = 0x01,
        FI_STABLE_PRESSURE_CTRL = 0x02,
        FI_SET_PRESSURE_DEPRECATED = 0x03,
        FI_SET_PRESSURE = 0x04,
        FI_SET_PID = 0x05,
        FI_SET_BATTERY_LED = 0x06,
        FI_SET_VIBRATION = 0x07,
        FI_SET_PULSE = 0x08,
        FI_SET_VIB_SPEED = 0x09,
        FI_SET_PULSE_SPEED = 0x0a,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="finger">finger: Array of Thumb, Index, Middle, Ring, Pinky or Palm.</param>
    /// <param name="applyHaptics">applyHaptics: true means apply haptics, false means remove haptics.</param>
    /// <returns></returns>
    public static byte[][] SetClutchState(string[] finger, bool applyHaptics)
    {
        byte state = 0xff;
        byte[][] clutchState = new byte[finger.Length][];

        switch (applyHaptics)
        {
            case true:
                state = 0;
                break;
            case false:
                state = 2;
                break;
        }

        if (state == 0xff)
        {
            //Debug.Log("Invalid parameter");
            return null;
        }

        for (int i = 0; i < finger.Length; i++)
        {
            switch (finger[i])
            {
                case "Thumb":
                    clutchState[i] = new byte[] { 0, state };
                    break;
                case "Index":
                    clutchState[i] = new byte[] { 1, state };
                    break;
                case "Middle":
                    clutchState[i] = new byte[] { 2, state };
                    break;
                case "Ring":
                    clutchState[i] = new byte[] { 3, state };
                    break;
                case "Pinky":
                    clutchState[i] = new byte[] { 4, state };
                    break;
                case "Palm":
                    clutchState[i] = new byte[] { 5, state };
                    break;
                default:
                    //Debug.Log("Invalid parameter");
                    return null;
            }
        }

        return clutchState;
    }
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


    public enum Finger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
        Palm
    }

    private static byte[] SetHapticsState(Finger finger, bool start)
    {
        byte[] hapticsState = new byte[2];
        if (start)
            hapticsState = new byte[] { (byte)finger, 0 };
        else
            hapticsState = new byte[] { (byte)finger, 2 };

        return hapticsState;
    }

    private static byte[][] SetHapticsState(Finger[] fingers, bool[] states)
    {
        byte[][] hapticsStates = new byte[fingers.Length][];

        for (int i = 0; i < fingers.Length; i++)
        {
            if (states[i])
                hapticsStates[i] = new byte[] { (byte)fingers[i], 0 };
            else
                hapticsStates[i] = new byte[] { (byte)fingers[i], 2 };
        }

        return hapticsStates;
    }

    private static byte[] SetPID(float kp, float ki, float kd)
    {
        Encode.Instance.add_f32(kp);
        Encode.Instance.add_f32(ki);
        Encode.Instance.add_f32(kd);
        byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PID);       // FI = 5
        Encode.Instance.clear_list();

        return data;
    }

    public static byte[] HEXRPressure(Finger finger, bool state, float intensity, float speed)
    {
        byte[] hapticsState = SetHapticsState(finger, state);

        float frequency = 0;
        Encode.Instance.add_f32(frequency);
        Encode.Instance.add_u8(hapticsState[0]);             // which finger
        Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
        float pressure = LinerMapping(0.1f, 1f, intensity, 15, 50);
        Encode.Instance.add_f32(pressure);
        speed = Clamp(0.1f, 1f, speed);
        speed = LinerMapping(0.1f, 1f, speed, 0.1f, 1);
        speed *= 100;
        Encode.Instance.add_u8((byte)speed);
        byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PRESSURE);       // FI = 4
        Encode.Instance.clear_list();

        Debug.Log("Intensity: " + intensity + "Pressure: " + pressure + "Speed: " + speed);

        return data;
    }

    public static byte[] HEXRPressure(Finger[] fingers, bool[] states, float[] intensities, float[] speeds)
    {
        byte[][] hapticsState = SetHapticsState(fingers, states);

        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < fingers.Length; i++)
        {
            HapticsFrame.AddRange(HEXRPressure(fingers[i], states[i], intensities[i], speeds[i]));
        }

        return HapticsFrame.ToArray();
    }


    /// <summary>
    /// Set low frequency vibration with tunable intensity and peakRatio
    /// </summary>
    /// <param name="finger">Set haptics channel</param>
    /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
    /// <param name="frequency">Set vibration frequency from 0.1Hz to 2Hz</param>
    /// <param name="intensity">Set vibration intensity from 0.1 to 0.7</param>
    /// <param name="peakRatio">Set peak ratio from 0.2 to 0.8</param>
    /// <param name="speed">Set slop of the pulse wave from 0.1 to 1</param>
    /// <param name="endIntensity">Set the pressure intensity to keep after vibration</param>
    public static byte[] HEXRVibration(Finger finger, bool state, float frequency, float intensity, float peakRatio, float speed, float endIntensity)
    {
        byte[] hapticsState = SetHapticsState(finger, state);

        frequency = Clamp(0.1f, 2, frequency);
        Encode.Instance.add_f32(frequency);
        Encode.Instance.add_u8(hapticsState[0]);             // which finger
        Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
        intensity = Clamp(0.1f, 0.7f, intensity);
        float pressure = LinerMapping(0.1f, 0.7f, intensity, 15, 30);
        peakRatio = Clamp(0.2f, 0.8f, peakRatio);
        peakRatio = peakRatio * 100;
        Encode.Instance.add_f32(pressure);
        Encode.Instance.add_u8((byte)peakRatio);
        speed = Clamp(0.1f, 1f, speed);
        speed = LinerMapping(0.1f, 1f, speed, 0.1f, 1);
        speed *= 100;
        Encode.Instance.add_u8((byte)speed);
        float endPressure = 0;
        if (endIntensity < 0.1f)
        {
            endIntensity = 0;
            endPressure = 0;
        }
        else
        {
            endIntensity = Clamp(0.1f, 1f, endIntensity);
            endPressure = LinerMapping(0.1f, 1f, endIntensity, 15, 50);
        }
        Encode.Instance.add_f32(endPressure);
        byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_VIB_SPEED);       // FI = 9
        Encode.Instance.clear_list();

        Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio + "\tSpeed: " + speed);

        return data;
    }

    public static byte[] HEXRVibration(Finger[] fingers, bool[] states, float[] frequencies, float[] intensities, float[] peakRatios, float[] speeds, float[] endIntensity)
    {
        byte[][] hapticsState = SetHapticsState(fingers, states);

        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < fingers.Length; i++)
        {
            HapticsFrame.AddRange(HEXRVibration(fingers[i], states[i], frequencies[i], intensities[i], peakRatios[i], speeds[i], endIntensity[i]));
        }

        return HapticsFrame.ToArray();
    }


    /// <summary>
    /// Set low frequency pulses with tunable intensity and peakRatio and speed
    /// </summary>
    /// <param name="finger">Set haptics channel</param>
    /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
    /// <param name="frequency">Set vibration frequency from 0.1Hz to 2Hz</param>
    /// <param name="intensity">Set vibration intensity from 0.1 to 0.7</param>
    /// <param name="peakRatio">Set peak ratio from 0.2 to 0.8</param>
    public static byte[] HEXRPulse(Finger finger, bool state, float frequency, float intensity, float peakRatio, float speed, UInt16 pulseCount, float endIntensity)
    {
        byte[] hapticsState = SetHapticsState(finger, state);

        frequency = Clamp(0.1f, 2, frequency);
        Encode.Instance.add_f32(frequency);
        Encode.Instance.add_u8(hapticsState[0]);             // which finger
        Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
        intensity = Clamp(0.1f, 0.7f, intensity);
        float pressure = LinerMapping(0.1f, 0.7f, intensity, 15, 30);
        peakRatio = Clamp(0.2f, 0.8f, peakRatio);
        peakRatio = peakRatio * 100;
        Encode.Instance.add_f32(pressure);
        Encode.Instance.add_u8((byte)peakRatio);
        pulseCount = Convert.ToUInt16(Clamp(1, 1000, pulseCount));
        Encode.Instance.add_u16(pulseCount);
        speed = Clamp(0.1f, 1f, speed);
        speed = LinerMapping(0.1f, 1f, speed, 0.1f, 1);
        speed *= 100;
        Encode.Instance.add_u8((byte)speed);
        float endPressure = 0;
        if (endIntensity < 0.1f)
        {
            endIntensity = 0;
            endPressure = 0;
        }
        else
        {
            endIntensity = Clamp(0.1f, 1f, endIntensity);
            endPressure = LinerMapping(0.1f, 1f, endIntensity, 15, 50);
        }
        Encode.Instance.add_f32(endPressure);
        byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PULSE_SPEED);       // FI = 10
        Encode.Instance.clear_list();

        Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio + "\tPulseCount: " + pulseCount + "\tSpeed: " + speed);

        return data;
    }

    public static byte[] HEXRPulse(Finger[] fingers, bool[] states, float[] frequencies, float[] intensities, float[] peakRatios, float[] speeds, UInt16[] pulseCounts, float[] endIntensities)
    {
        byte[][] hapticsState = SetHapticsState(fingers, states);

        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < fingers.Length; i++)
        {
            HapticsFrame.AddRange(HEXRPulse(fingers[i], states[i], frequencies[i], intensities[i], peakRatios[i], speeds[i], pulseCounts[i], endIntensities[i]));
        }

        return HapticsFrame.ToArray();
    }

    /// <summary>
    /// Set vibration with tunable intensity
    /// </summary>
    /// <param name="finger">Set haptics channel</param>
    /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
    /// <param name="frequency">Set vibration frequency from 0.1Hz to 40Hz</param>
    /// <param name="intensity">Set vibration intensity from 0.1 to 1</param>
    public static byte[] HEXRVibration(Finger finger, bool state, float frequency, float intensity)
    {
        byte[] hapticsState = SetHapticsState(finger, state);

        frequency = Clamp(0.1f, 40, frequency);
        Encode.Instance.add_f32(frequency);
        Encode.Instance.add_u8(hapticsState[0]);             // which finger
        Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
        float[] buf = GetVibIntensity(frequency, intensity);
        float pressure = buf[0];
        Encode.Instance.add_f32(pressure);                   // Pressure
        byte peakRatio = (byte)buf[1];
        Encode.Instance.add_u8(peakRatio);                   // Peak ratio
        byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_VIBRATION);       // FI = 7
        Encode.Instance.clear_list();

        Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio);

        return data;
    }

    public static byte[] HEXRVibration(Finger[] fingers, bool[] states, float[] frequencies, float[] intensities)
    {
        byte[][] hapticsState = SetHapticsState(fingers, states);

        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < fingers.Length; i++)
        {
            HapticsFrame.AddRange(HEXRVibration(fingers[i], states[i], frequencies[i], intensities[i]));
        }

        return HapticsFrame.ToArray();
    }


    /// <summary>
    /// Set vibration with tunable intensity and the number of pulses
    /// </summary>
    /// <param name="finger">Set haptics channel</param>
    /// <param name="state">Set TRUE to generate vibration, set FALSE to stop vibration</param>
    /// <param name="frequency">Set vibration frequency from 0.1Hz to 40Hz</param>
    /// <param name="intensity">Set vibration intensity from 0.1 to 1</param>
    /// <param name="pulseCount">Set pulse count from 1 to 1000</param>
    public static byte[] HEXRPulse(Finger finger, bool state, float frequency, float intensity, UInt16 pulseCount)
    {
        byte[] hapticsState = SetHapticsState(finger, state);

        frequency = Clamp(0.1f, 40, frequency);
        Encode.Instance.add_f32(frequency);
        Encode.Instance.add_u8(hapticsState[0]);             // which finger
        Encode.Instance.add_u8(hapticsState[1]);             // enter, stay or exit
        float[] buf = GetVibIntensity(frequency, intensity);
        float pressure = buf[0];
        Encode.Instance.add_f32(pressure);                   // Pressure
        byte peakRatio = (byte)buf[1];
        Encode.Instance.add_u8(peakRatio);                   // Peak ratio
        pulseCount = Convert.ToUInt16(Clamp(1, 1000, pulseCount));
        Encode.Instance.add_u16(pulseCount);
        byte[] data = Encode.Instance.add_fun((byte)Haptics.FunIndex.FI_SET_PULSE);       // FI = 8
        Encode.Instance.clear_list();

        Debug.Log("Frequency: " + frequency + "\tPressure: " + pressure + "\tPeakRatio: " + peakRatio + "\tPulseCount: " + pulseCount);

        return data;
    }

    public static byte[] HEXRPulse(Finger[] fingers, bool[] states, float[] frequencies, float[] intensities, UInt16[] pulseCounts)
    {
        byte[][] hapticsState = SetHapticsState(fingers, states);

        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < fingers.Length; i++)
        {
            HapticsFrame.AddRange(HEXRPulse(fingers[i], states[i], frequencies[i], intensities[i], pulseCounts[i]));
        }

        return HapticsFrame.ToArray();
    }

    private static float GetVibPressure(float frequency)
    {
        float pressure = 0;
        if (frequency >= 5)
            pressure = 50;
        else if (frequency >= 2)
            pressure = LinerMapping(2, 5, frequency, 20, 50);
        else if (frequency >= 0.1f)
            pressure = 20;

        return pressure;
    }

    private static float[] GetVibIntensity(float frequency, float fakeIntensity)
    {
        float pressure = 0;
        byte peakRatio = 0;
        fakeIntensity = Clamp(0.1f, 1, fakeIntensity);
        if (frequency >= 10)
        {
            pressure = 50;
            peakRatio = (byte)LinerMapping(0.1f, 1, fakeIntensity, 20, 50);
        }
        else if (frequency >= 5)
        {
            pressure = 50;
            byte bound1 = (byte)LinerMapping(5, 10, frequency, 20, 20);
            peakRatio = (byte)LinerMapping(0.1f, 1, fakeIntensity, bound1, 50);
        }
        else if (frequency >= 1)
        {
            peakRatio = 50;
            pressure = LinerMapping(0.1f, 1, fakeIntensity, 15, 50);
        }
        else if (frequency >= 0.1f)
        {
            peakRatio = 50;
            pressure = LinerMapping(0.1f, 1, fakeIntensity, 15, 50);
        }

        float[] buf = new float[] { pressure, peakRatio };
        return buf;
    }

    private static byte GetVibPeakRatio(float frequency)
    {
        byte peakRatio = 0;
        byte defaultRatio = 35;
        byte bound1 = 0;
        byte bound2 = 0;
        if (frequency >= 10)
            peakRatio = defaultRatio;
        else if (frequency >= 5)
        {
            bound1 = (byte)LinerMapping(5, 10, frequency, 10, 20);
            bound2 = 50;
            peakRatio = (byte)LinerMapping(20, 50, defaultRatio, bound1, bound2);
        }
        else if (frequency >= 2)
        {
            bound1 = 50;
            bound2 = 30;
            peakRatio = (byte)LinerMapping(2, 5, frequency, bound1, bound2);
        }
        else if (frequency >= 0.1f)
        {
            peakRatio = 50;
        }

        return peakRatio;
    }

    private static float LinerMapping(float preBound1, float preBound2, float input, float bound1, float bound2)
    {
        if (preBound2 == preBound1)
            return 0;

        float output = (bound2 - bound1) * (input - preBound1) / (preBound2 - preBound1) + bound1;
        return output;
    }

    private static float Clamp(float lower, float upper, float input)
    {
        float output = 0;
        if (input > upper)
            output = upper;
        else if (input < lower)
            output = lower;
        else
            output = input;

        return output;
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////////////

    ///// <summary>
    ///// To one finger
    ///// </summary>
    ///// <param name="clutchState"></param>
    ///// <param name="targetPres"></param>
    //public static void ApplyHaptics(byte[] clutchState, byte targetPres)
    //{
    //    byte frequency = 0;
    //    if ((targetPres > 0) & (targetPres < 10))
    //    {
    //        frequency = 0xff;
    //    }
    //    int presSource = (int)BTCommu_Left.Instance.pressureData[5];
    //    byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

    //    if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
    //    {
    //        byte vOpen = valveTiming[0];
    //        byte vDelay = valveTiming[1];
    //        //±àÂë+BT·¢ËÍ
    //        Encode.Instance.add_u8(frequency);
    //        Encode.Instance.add_u8(clutchState[0]);             // which finger
    //        Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
    //        Encode.Instance.add_u8(targetPres);
    //        Encode.Instance.add_u8(vOpen);
    //        Encode.Instance.add_u8(vDelay);
    //        byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
    //        Encode.Instance.clear_list();
    //        BTCommu_Left.Instance.BTSend(buf);
    //        //Debug.Log(BitConverter.ToString(buf));
    //    }
    //}

    /// <summary>
    /// To one finger
    /// </summary>
    /// <param name="clutchState"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(byte[] clutchState, float targetPres, bool compensateHysteresis)
    {
        byte frequency = 0;
        if ((targetPres > 0) & (targetPres < 10))
        {
            frequency = 0xff;
        }
        int presSource = (int)BTCommu_Left.Instance.pressureData[5];
        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            byte vOpen = 0;//valveTiming[0];
            byte vDelay = 0;//valveTiming[1];
            //±àÂë+BT·¢ËÍ
            Encode.Instance.add_u8(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_f32(targetPres);
            Encode.Instance.add_u8(vOpen);
            Encode.Instance.add_u8(vDelay);
            Encode.Instance.add_b1(compensateHysteresis);
            byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
            Encode.Instance.clear_list();
            BTCommu_Left.Instance.BTSend(buf);
            //Debug.Log(BitConverter.ToString(buf));
        }
    }

    /// <summary>
    /// Apply vibration with peak ratio to one finger
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="clutchState"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(float frequency, byte[] clutchState, float targetPres, byte peakRatio)
    {
        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            //±àÂë+BT·¢ËÍ
            frequency = Clamp(0, 40, frequency);
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_f32(targetPres);
            peakRatio = Convert.ToByte(Clamp(0, 100, peakRatio));
            Encode.Instance.add_u8(peakRatio);
            byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_VIBRATION);       // FI = 7
            Encode.Instance.clear_list();
            BTCommu_Left.Instance.BTSend(buf);
            //Debug.Log(BitConverter.ToString(buf));
        }
    }

    /// <summary>
    /// Apply pulses to one finger
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="clutchState"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(float frequency, byte[] clutchState, float targetPres, byte peakRatio, UInt16 count)
    {
        if ((clutchState[0] != 0xff) & (clutchState[1] != 0xff))
        {
            //±àÂë+BT·¢ËÍ
            frequency = Clamp(0, 40, frequency);
            Encode.Instance.add_f32(frequency);
            Encode.Instance.add_u8(clutchState[0]);             // which finger
            Encode.Instance.add_u8(clutchState[1]);             // enter, stay or exit
            Encode.Instance.add_f32(targetPres);
            peakRatio = Convert.ToByte(Clamp(0, 100, peakRatio));
            Encode.Instance.add_u8(peakRatio);
            count = Convert.ToUInt16(Clamp(0, 100, count));
            Encode.Instance.add_u16(count);
            byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PULSE);       // FI = 8
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
    public static void ApplyHaptics(byte frequency, byte[] clutchState, byte targetPres, bool compensateHysteresis)
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
            Encode.Instance.add_b1(compensateHysteresis);
            byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
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
    public static void ApplyHaptics(byte[][] clutchStates, byte targetPres, bool compensateHysteresis)
    {
        byte frequency = 0;
        if ((targetPres > 0) & (targetPres < 10))
        {
            frequency = 0xff;
        }
        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < clutchStates.Length; i++)
        {
            HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], targetPres, compensateHysteresis));
        }

        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
    }

    /// <summary>
    /// Apply vibration to multiple fingers
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="clutchStates"></param>
    /// <param name="targetPres"></param>
    public static void ApplyHaptics(byte frequency, byte[][] clutchStates, byte targetPres, bool compensateHysteresis)
    {
        List<byte> HapticsFrame = new List<byte>();

        for (int i = 0; i < clutchStates.Length; i++)
        {
            HapticsFrame.AddRange(ConstructHapticsFrame(frequency, clutchStates[i], targetPres, compensateHysteresis));
        }

        BTCommu_Left.Instance.BTSend(HapticsFrame.ToArray());
    }

    private static byte[] ConstructHapticsFrame(byte frequency, byte[] clutchState, byte targetPres, bool compensateHysteresis)
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
        Encode.Instance.add_b1(compensateHysteresis);
        byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED); // FI = 3
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
            byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
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
            byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED);       // FI = 3
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
        byte[] buf = Encode.Instance.add_fun((byte)FunIndex.FI_SET_PRESSURE_DEPRECATED); // FI = 3
        Encode.Instance.clear_list();

        return buf;
    }

}