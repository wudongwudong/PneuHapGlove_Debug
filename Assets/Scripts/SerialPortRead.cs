using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.IO.Ports;

public class SerialPortRead : MonoBehaviour
{
    static SerialPortRead _instance;
    public static SerialPortRead Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SerialPortRead();
            }
            return _instance;
        }
    }

    private FileStream file;
    private StreamWriter writer;

    // private SerialPort sP;
    private SerialPort sP = new SerialPort("COM4", 115200, Parity.None);
    private string[] serialData = new String[2];

    void Awake()
    {
        _instance = this;
    }

    public Text sourcePressureText;
    public Text clutchPressureText;
    public Text mtResistanceText;

    public Socket SocketClient;

    private bool connection = false;
    public bool Connection
    {
        get { return connection; }
        set { connection = value; }
    }

    private static string IpAddress = "172.20.10.5";

    public Thread thread_receive;
    private int index = 0;

    private BMP280 bmp280 = new BMP280();
    double[] buffBMP280Data = new Double[8];///////////////////////////////////////
    double[] buffMicrotubeData = new Double[2];///////////////////////////////////////

    //public TCPClient()
    //{
    //}

    public void connection_Click()
    {
        if (Connection == false)
        {
            /////////////////////////////////////////////
            //Connection = true;
            /////////////////////////////////////////////

            Connect(IpAddress, 3333);
            if (Connection == true)
            {
                Debug.Log("connection success");
                ReceiveData();
            }
            else
            {
                Debug.Log("connection failed");
            }
        }
        else
        {
            Close();
            Debug.Log("connection closed");
        }
    }

    public void ReceiveData()
    {
        thread_receive = new Thread(ReceiveDataThread);
        thread_receive.IsBackground = true;
        thread_receive.Start();
    }

    void FixedUpdate()
    {
        sourcePressureText.text = buffBMP280Data[0].ToString();
        clutchPressureText.text = buffBMP280Data[1].ToString();
        //mtResistanceText.text = buffMicrotubeData[1].ToString();
        //mtResistanceText.text = serialData[1];
    }

    private void ReceiveDataThread()
    {
        int length = 0;
        byte[] tem_data = new byte[1024];
        Queue DataQueue = new Queue();
        while (true)
        {
            try
            {
                length = SocketClient.Receive(tem_data);

                if (length > 0)
                {
                    Decode decode = new Decode();
                    Queue FrameQueue = decode.GetFrameQueue(tem_data, length);
                    while (FrameQueue.Count > 0)
                    {
                        byte[] frame = (byte[])FrameQueue.Dequeue();
                        int func = decode.GetFunc(frame);

                        switch (func)
                        {
                            case 0x01:
                                DataQueue = decode.GetDataQueue(frame);
                                buffBMP280Data = bmp280.decode_bmp280_data(DataQueue);
                                DataQueue.Clear();
                                break;
                            //    case 0x02:
                            //        if (frame[0] == 20)
                            //        {
                            //            //MessageBox.Show(BitConverter.ToString(frame));
                            //            DataQueue = decode.GetDataQueue(frame);
                            //            Detail = bmp280.solve_v(DataQueue);
                            //            DataQueue.Clear();
                            //            this.Invoke(showVolumeDetail);
                            //        }
                            //        break;
                            case 0x04:
                                DataQueue = decode.GetDataQueue(frame);
                                buffMicrotubeData = Microtube.Instance.MicrotubeResistance(DataQueue);
                                DataQueue.Clear();
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {

                }
            }
            catch
            {
                //connection.Text = "点击连接";
                //this.Invoke(changeConnectionText);
                //client.Connection = false;
                //MessageBox.Show("连接断开");
                //return;
            }
            Thread.Sleep(10);
        }
    }

    public void Connect(string ip, int port)
    {
        try
        {
            SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            EndPoint point = new IPEndPoint(IPAddress.Parse(ip), port);
            SocketClient.Connect(point);
            Connection = true;
            //MessageBox.Show("连接成功");
        }
        catch (SocketException e)
        {
            //MessageBox.Show(e.ToString());
            Connection = false;
        }

    }


    public void Close()
    {
        try
        {
            SocketClient.Close();
            Connection = false;
        }
        catch (SocketException e)
        {
            Connection = false;
        }
    }

    public int? send(byte[] data)//发送编码好的一组数据
    {
        try
        {
            Debug.Log(BitConverter.ToString(data));
            int length = 4;


            //int length = SocketClient.Send(data);
            return length;
        }
        catch (SocketException e)
        {
            Connection = false;
            //MessageBox.Show("连接断开");
            return null;
        }
    }


    ////////////////////Serial Port Test/////////////////////////////
    public void SerialConnection()
    {
        file = new FileStream("C:/Users/65110/Desktop/aaa.csv", FileMode.Create, FileAccess.ReadWrite);
        writer = new StreamWriter(file);


        if (sP.IsOpen)
        {
            sP.Close();
            Debug.Log("Serial Conmmu Stop");
        }
        else
        {
            sP = new SerialPort("COM4", 115200, Parity.None);
            sP.NewLine = "\r\n";
            sP.ReadTimeout = 1;
            sP.Open();
            Thread threadSerial;
            threadSerial = new Thread(SerialData);
            threadSerial.IsBackground = true;
            threadSerial.Start();
            Debug.Log("Serial Conmmu Start");
        }
        
    }

    private void SerialData()
    {
        serialData[0] = sP.ReadLine();
        while (true)
        {
            try
            {
                serialData[1] = sP.ReadLine();
                if (Math.Abs(Convert.ToDouble(serialData[1]) - Convert.ToDouble(serialData[0])) < 20)
                {
                    writer.WriteLine(serialData[1]);
                    mtResistanceText.text = serialData[1];
                    serialData[0] = serialData[1];
                    //Debug.Log(tmp);
                }

            }
            catch {}
            //Thread.Sleep(5);
        }
    }


}
