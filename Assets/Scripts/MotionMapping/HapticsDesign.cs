using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class HapticsDesign : MonoBehaviour
{
    public Toggle thumb, index, middle, ring, pinky, palm;
    public Button selectAll, unselectAll, applyHaptics, removeHaptics;
    public Toggle isVibration, isPulse;
    public InputField intensity, pressureSpeed, vibrationSpeed, frequency, peakRatio, endPressure;
    public Slider pulseCount;

    void Start()
    {
        selectAll.onClick.AddListener(delegate { SelectAllHapticsChannels(true); });
        unselectAll.onClick.AddListener(delegate { SelectAllHapticsChannels(false); });
        applyHaptics.onClick.AddListener(delegate { ApplyHaptics(true); });
        removeHaptics.onClick.AddListener(delegate { ApplyHaptics(false); });
    }

    private void SelectAllHapticsChannels(bool state)
    {
        thumb.isOn = state;
        index.isOn = state;
        middle.isOn = state;
        ring.isOn = state;
        pinky.isOn = state;
        palm.isOn = state;
    }

    private Haptics.Finger[] GetHapticsStates(bool state)
    {
        List<Haptics.Finger> fingers = new List<Haptics.Finger>();

        if (thumb.isOn == true)
            fingers.Add(Haptics.Finger.Thumb);
        if (index.isOn == true)
            fingers.Add(Haptics.Finger.Index);
        if (middle.isOn == true)
            fingers.Add(Haptics.Finger.Middle);
        if (ring.isOn == true)
            fingers.Add(Haptics.Finger.Ring);
        if (pinky.isOn == true)
            fingers.Add(Haptics.Finger.Pinky);
        if (palm.isOn == true)
            fingers.Add(Haptics.Finger.Palm);

        return fingers.ToArray();
    }

    private void ApplyHaptics(bool state)
    {
        Haptics.Finger[] fingers = GetHapticsStates(state);
        byte[] data;

        if (fingers.Length != 0)
        {
            bool[] states = new Boolean [fingers.Length];
            float[] intensities = new float[fingers.Length];

            for (int i = 0; i < fingers.Length; i++)
            {
                states[i] = state;
                intensities[i] = Convert.ToSingle(intensity.text);
            }

            if (isVibration.isOn)
            {
                float[] frequencies = new float[fingers.Length];
                for (int i = 0; i < fingers.Length; i++)
                    frequencies[i] = Convert.ToSingle(frequency.text);
                
                if (isPulse.isOn)
                {
                    UInt16[] pulseCounts = new ushort[fingers.Length];
                    for (int i = 0; i < fingers.Length; i++)
                        pulseCounts[i] = Convert.ToUInt16(pulseCount.value);

                    if (!string.IsNullOrEmpty(peakRatio.text))
                    {
                        float[] peakRatios = new float[fingers.Length];
                        float[] speeds = new float[fingers.Length];
                        float[] endPressures = new float[fingers.Length];
                        for (int i = 0; i < fingers.Length; i++)
                        {
                            peakRatios[i] = Convert.ToSingle(peakRatio.text);
                            speeds[i] = Convert.ToSingle(vibrationSpeed.text);
                            endPressures[i] = Convert.ToSingle(endPressure.text);
                        }
                        data = Haptics.HEXRPulse(fingers, states, frequencies, intensities, peakRatios, speeds, pulseCounts, endPressures);
                    }
                    else
                        data = Haptics.HEXRPulse(fingers, states, frequencies, intensities, pulseCounts);
                }
                else
                {
                    if (!string.IsNullOrEmpty(peakRatio.text))
                    {
                        Debug.Log(peakRatio.text);
                        float[] peakRatios = new float[fingers.Length];
                        float[] speeds = new float[fingers.Length];
                        float[] endPressures = new float[fingers.Length];
                        for (int i = 0; i < fingers.Length; i++)
                        {
                            peakRatios[i] = Convert.ToSingle(peakRatio.text);
                            speeds[i] = Convert.ToSingle(vibrationSpeed.text);
                            endPressures[i] = Convert.ToSingle(endPressure.text);
                        }
                        data = Haptics.HEXRVibration(fingers, states, frequencies, intensities, peakRatios, speeds, endPressures);
                    }
                    else
                        data = Haptics.HEXRVibration(fingers, states, frequencies, intensities);
                    
                }
            }
            else
            {
                float[] speeds = new float[fingers.Length];
                for (int i = 0; i < fingers.Length; i++)
                {
                    speeds[i] = Convert.ToSingle(pressureSpeed.text);
                }

                data = Haptics.HEXRPressure(fingers,states, intensities, speeds);
            }

            BTCommu_Left.Instance.BTSend(data);
        }
        else
        {
            Debug.LogError("No haptics channel is selected.");
        }
    }

}
