using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Microtube
{
    static Microtube _instance;
    public static Microtube Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Microtube();
            }
            return _instance;
        }
    }

    private double[] mtResistance = { 0, 0, 0};

    public double[] MicrotubeResistance(Queue q_data)
    {
        mtResistance[0] = (uint)q_data.Dequeue();
        mtResistance[1] = (uint)q_data.Dequeue();
        mtResistance[2] = (uint)q_data.Dequeue();
        return mtResistance;
    }
}
