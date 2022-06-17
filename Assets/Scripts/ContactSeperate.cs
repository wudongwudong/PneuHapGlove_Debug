using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactSeperate
{
    static ContactSeperate _instance;
    public static ContactSeperate Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ContactSeperate();
            }
            return _instance;
        }
    }

    UInt16 vOpen = 0; //ms
    UInt16 vDelay = 0; //ms


    public void ContactSend(byte objStiffness)
    {
        switch(objStiffness)
        {
            case 10:
                vOpen = 40;
                break;
            case 20:
                vOpen = 60;
                break;
            case 30:
                vOpen = 90;
                break;
            default:
                break;
        }
        
        Encode.Instance.add_u16(objStiffness);
        Encode.Instance.add_u16(vOpen);
        Encode.Instance.add_u16(vDelay);
        byte[] buff = Encode.Instance.add_fun(0x0f);
        Encode.Instance.clear_list();
        TCPClient.Instance.SerialSend(buff);

        //TCPClient.Instance.send(buff);
        Debug.Log("Contact! Air Pressure: " + objStiffness.ToString() + " kPa");
    }

    public void SeperateSend()
    {
        Encode.Instance.add_u16(0);
        Encode.Instance.add_u16(1000);
        Encode.Instance.add_u16(vDelay);
        byte[] buff = Encode.Instance.add_fun(0x0f);
        Encode.Instance.clear_list();
        TCPClient.Instance.SerialSend(buff);

        //TCPClient.Instance.send(buff);
        Debug.Log("Seperate! Air Pressure: 0 kPa");
    }


    


}
