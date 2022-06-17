using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BMP280
{
    private double amount_o2 = 0;
    private double amount_h2 = 0;

    public double[] data_pres_temp;


    public BMP280()
    {
    }

    public double[] decode_bmp280_data(Queue q_data)
    {
        data_pres_temp = new Double[q_data.Count];
        for (int i = 0; i < data_pres_temp.Length; i++)
        {
            data_pres_temp[i] = (double)q_data.Dequeue();
        }

        return data_pres_temp;

        //data_pres_temp[0] = 100;
        //Debug.Log("我在这" + data_pres_temp[0].ToString());
    }

    public double[] solve_v(Queue q_data)
    {
        amount_o2 = (double)q_data.Dequeue() * 1000000;
        amount_h2 = (double)q_data.Dequeue() * 1000000;

        double[] s = new double[2];
        s[0] = amount_h2;
        s[1] = amount_o2;

        return s;

        //MessageBox.Show(amount_o2 + "\n" + amount_h2 + "\n" + total_v);

    }

}
