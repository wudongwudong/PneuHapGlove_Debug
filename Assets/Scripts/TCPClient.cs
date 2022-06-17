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

public class TCPClient : MonoBehaviour
{
    static TCPClient _instance;
    public static TCPClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TCPClient();
            }
            return _instance;
        }
    }

    private FileStream file;
    private StreamWriter writer;
    Thread threadSerial;

    // private SerialPort sP;
    private SerialPort sP = new SerialPort("COM6", 115200, Parity.None);
    public static string[] serialData = new String[5];
    public static double[] angle = new double[3];

    private long received_count = 0;//接收计数
    private long send_count = 0;//发送计数
    private bool Listening = false;//是否没有执行完invoke相关操作
    private bool Closing = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke
    private List<byte> buffer = new List<byte>(4096);//默认分配1页内存，并始终限制不允许超过
    private byte[] binary_data_1 = new byte[1024];

    private enum funList: byte
    {
        FI_BMP280 = 0x01,
        FI_IMU = 0x02,
        FI_MICROTUBE = 0x04
    };

    void Awake()
    {
        _instance = this;

        positionThumb = new Vector3(5, rObject, 0);
        positionIndex = new Vector3(0, 7, 0);
        scaleObject = new Vector3(iniScale, iniScale, iniScale);
        positionObject = new Vector3(0, rObject, 0);
    }

    public Text sourcePressureText;
    public Text clutchPressureText;
    public Text textDataThumb;
    public Text textDataIndex;

    public Transform objectToTouchTransform;

    public Socket SocketClient;

    private bool connection = false;
    public bool Connection
    {
        get { return connection; }
        set { connection = value; }
    }

    private static string IpAddress = "172.20.10.10";

    public Thread thread_receive;
    private int index = 0;

    private BMP280 bmp280 = new BMP280();
    public static double[] buffBMP280Data = new Double[8] { 0, 0, 0, 0, 0, 0, 0, 0 };///////////////////////////////////////
    public static double[] buffMicrotubeData = new Double[3] { 0, 0, 0 };///////////////////////////////////////

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
        //dataIndex.text = buffMicrotubeData[0].ToString();           //读WiFi数据
        textDataThumb.text = serialData[0];                             //读串口数据
        textDataIndex.text = serialData[1];

        objectToTouchTransform.localScale = scaleObject;
        objectToTouchTransform.localPosition = positionObject;
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

                            case 0x04:
                                Debug.Log(BitConverter.ToString(frame));

                                DataQueue = decode.GetDataQueue(frame);
                                buffMicrotubeData = Microtube.Instance.MicrotubeResistance(DataQueue);
                                DataQueue.Clear();

                                
                                Debug.Log(buffMicrotubeData[1].ToString());
                                DetectContact();
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
            //Debug.Log(BitConverter.ToString(data));
            //int length = 4;


            int length = SocketClient.Send(data);
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
        //file = new FileStream("C:/Users/65110/Desktop/aaa.csv", FileMode.Create, FileAccess.ReadWrite); 
        //writer = new StreamWriter(file);


        if (sP.IsOpen)
        {
            Closing = true;
            sP.Close();
            threadSerial.Abort();
            Debug.Log("Serial Conmmu Stop");
        }
        else
        {
            sP = new SerialPort("COM6", 115200, Parity.None);
            sP.NewLine = "\r\n";
            sP.ReadTimeout = 1;
            //sP.DataReceived += new SerialDataReceivedEventHandler(comm_DataReceived);
            try
            {
                sP.Open();
                threadSerial = new Thread(comm_DataReceived);
                threadSerial.IsBackground = true;
                threadSerial.Start();

                Debug.Log("Serial Conmmu Start");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
            
        }
        
    }

    private void comm_DataReceived()//object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
    {
        while (true)
        {
            if (Closing) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环
            try
            {
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。
                int n = sP.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致
                if (n == 0)
                {
                    continue;
                }
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据
                received_count += n;//增加接收计数
                sP.Read(buf, 0, n);//读取缓冲数据
                //
                //<协议解析>
                bool data_1_catched = false;//缓存记录数据是否捕获到
                //1.缓存数据
                buffer.AddRange(buf);
                //2.完整性判断
                //Debug.Log("n = " + n);
                
                while (buffer.Count >= 5) //长度（1字节）+功能（1字节）+数据（n字节）+校验（1字节）
                {
                    //2.1 查找数据头 buffer[1]是否在功能列表里
                    if (Enum.IsDefined(typeof(funList), buffer[1]))
                    {
                        //2.2 探测缓存数据是否有一条数据的字节，如果不够，就不用费劲的做其他验证了
                        int len = buffer[0];//帧长度
                        //Debug.Log("len = " + len);
                        //Debug.Log("buffer.count = " + buffer.Count);
                        //帧完整判断第一步，长度是否足够
                        //len是帧总长
                        if (buffer.Count < len) break;//数据不够的时候什么都不做
                        //这里确保数据长度足够，数据头标志找到，我们开始计算校验
                        //2.3 校验数据，确认数据正确
                        //异或校验，逐个字节异或得到校验码
                        byte checkSum = 0;
                        for (int i = 0; i < len - 1; i++)//len-1表示校验之前的位置
                        {
                            checkSum ^= buffer[i];
                        }
                        if (checkSum != buffer[len-1]) //如果数据校验失败，丢弃这一包数据
                        {
                            //Debug.Log("校验失败");
                            buffer.RemoveRange(0, len);//从缓存中删除错误数据
                            continue;//继续下一次循环
                        }
                        //Debug.Log("校验成功");
                        //至此，已经被找到了一条完整数据。我们将数据直接分析，或是缓存起来一起分析
                        //我们这里采用的办法是缓存一次，好处就是如果你某种原因，数据堆积在缓存buffer中
                        //已经很多了，那你需要循环的找到最后一组，只分析最新数据，过往数据你已经处理不及时
                        //了，就不要浪费更多时间了，这也是考虑到系统负载能够降低。
                        buffer.CopyTo(0, binary_data_1, 0, len);//复制一条完整数据到具体的数据缓存
                        data_1_catched = true;
                        buffer.RemoveRange(0, len);//正确分析一条数据，从缓存中移除数据。
                        FrameDataAnalysis(binary_data_1, data_1_catched);
                    }
                    else
                    {
                        //这里是很重要的，如果数据开始不是头，则删除数据
                        buffer.RemoveAt(0);
                    }
                }
            }
            catch { }

        }

    }
    void FrameDataAnalysis(byte[] binary_data, bool data_catched)
    {
        
        if (data_catched)
        {
            switch (binary_data[1])
            {
                case (byte)funList.FI_BMP280:         //BMP280
                    break;
                case (byte)funList.FI_IMU:            //IMU
                    //decodeIMU(binary_data);
                    break;
                case (byte)funList.FI_MICROTUBE:      //Microtube
                    decodeMicrotube(binary_data);
                    break;
            }
            
        }
    }
    void decodeIMU(byte[] binary_data)
    {
        angle[0] = (BitConverter.ToInt16(binary_data, 3)) / 32768.0 * 180;
        angle[1] = (BitConverter.ToInt16(binary_data, 6)) / 32768.0 * 180;
        angle[2] = (BitConverter.ToInt16(binary_data, 9)) / 32768.0 * 180;
    }

    void decodeMicrotube(byte[] binary_data)
    {
        serialData[0] = (BitConverter.ToInt16(binary_data, 3)).ToString();
        serialData[1] = (BitConverter.ToInt16(binary_data, 6)).ToString();
        serialData[2] = (BitConverter.ToInt16(binary_data, 9)).ToString();
        serialData[3] = (BitConverter.ToInt16(binary_data, 12)).ToString();
        serialData[4] = (BitConverter.ToInt16(binary_data, 15)).ToString();
        //Debug.Log(serialData[0]);
        DetectContact();
    }

    public int? SerialSend(byte[] data)//串口发送编码好的一组数据
    {
        try
        {
            int length = data.Length;
            sP.Write(data, 0, length);
            Debug.Log("发送的数据：" + BitConverter.ToString(data));
            return length;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return null;
        }
    }


    bool flagPresSocureStart = false;

    public void PresSourceCtrl()
    {
        if (flagPresSocureStart == false)
        {
            byte pSourceTarget = 40; //kPa
            Encode.Instance.add_u8(pSourceTarget);
            byte[] buff = Encode.Instance.add_fun(0x0d);
            Encode.Instance.clear_list();
            TCPClient.Instance.send(buff);
            Debug.Log("Pressure Source Start: 40 kPa");

            flagPresSocureStart = true;
        }
        else
        {
            byte[] buff = Encode.Instance.add_fun(0x0e);
            Encode.Instance.clear_list();
            TCPClient.Instance.send(buff);
            Debug.Log("Pressure Source Stop");

            flagPresSocureStart = false;
        }

    }
    /////////////////////////////////////////////////////////////////////////////////////


    private static byte objStiffness = 10;  //////传入Clutch的控制气压命令 / kPa
    public Vector3 positionThumb = Vector3.zero;
    public Vector3 positionIndex = Vector3.zero;

    private float dataThumb = 0;
    private float dataIndex = 0;
    private float[] dataIndexRange = new float[2] { 500, 1000 };
    private float[] dataThumbRange = new float[2] { 500, 1200 };
    //private float rMicrotube_r2 = 1200;
    static private float rObject = 2.5f;
    static private float rFinger = 0.5f;

    private float[] distanceThumbRange = new float[2] { 1f, 4f };
    private float[] distanceIndexRange = new float[2] { 4.5f, 8f };  //distanceIndexRange[1] = rObject + rFinger;

    private float iniScale = 2 * rObject;
    public Vector3 scaleObject = Vector3.zero;
    public Vector3 positionObject = Vector3.zero;


    bool flagFirstContact = true;
    bool flagFirstSeperate = false;
    int startTime = 0;
    int endTime = 0;

    
    ////////////////////////////////////////////////////////
    float hysteresis = 0;
    bool flagContact = false;
    ////////////////////////////////////////////////////////
    

    public void DetectContact()
    {
        try
        {
            dataThumb = (float)Convert.ToDouble(serialData[0]);   //读串口数据
            dataIndex = (float)Convert.ToDouble(serialData[1]);  
            //rMicrotube = (float)buffMicrotubeData[1];           //读WiFi数据
            positionThumb.x = ((dataThumb - dataThumbRange[1]) / (dataThumbRange[0] - dataThumbRange[1])) * (distanceThumbRange[1] - distanceThumbRange[0]) + distanceThumbRange[0];
            positionIndex.y = ((dataIndex - dataIndexRange[1]) / (dataIndexRange[0] - dataIndexRange[1])) * (distanceIndexRange[1] - distanceIndexRange[0]) + distanceIndexRange[0];
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }


        if (positionIndex.y > (2 * rObject + rFinger))
        {
            //objectToTouchTransform.localScale = new Vector3(2, 2, 2);
            //transform.position = forward;
            if (flagFirstSeperate)
            {
                //Clutch停止
                ContactSeperate.Instance.SeperateSend();
                flagFirstSeperate = false;

                startTime = System.Environment.TickCount;
                endTime = 0;
            }

            if (startTime != 0)
            {
                endTime = System.Environment.TickCount;
            }
            if (endTime - startTime > 500)
            {
                flagFirstContact = true;
                startTime = 0;
                endTime = 0;
            }

        }
        else if ((positionIndex.y >= distanceIndexRange[0]) && (positionIndex.y <= (2 * rObject + rFinger)))
        {
            //transform.position = forward;
            if (flagFirstContact)
            {
                //Clutch启动
                //iniPressure = TCPClient.buffBMP280Data[1];
                ContactSeperate.Instance.ContactSend(objStiffness);
                flagFirstContact = false;

                startTime = System.Environment.TickCount;
                endTime = 0;

            }

            endTime = System.Environment.TickCount;
            if (endTime - startTime > 500)
            {
                flagFirstSeperate = true;
            }

            //物体形变
            scaleObject.y = positionIndex.y - rFinger;
            positionObject.y = scaleObject.y / 2;
            scaleObject.x = scaleObject.z = (0.4f * ((iniScale - scaleObject.y) / scaleObject.y) + 1) * iniScale;
            //objectToTouchTransform.localScale = new Vector3(xScale, yScale, zScale);
            //objectToTouchTransform.localPosition = new Vector3(xPosition, yPosition, zPosition);

        }
        else
        {
            positionIndex = new Vector3(0, distanceIndexRange[0], 0);
            //transform.position = forward;

            //objectToTouchTransform.localScale = new Vector3(5.5f, dFinger_d2 - rFinger, 5.5f);
        }
    }

    public void ChangeObjStiffnessOne()
    {
        objStiffness = 10;
        Debug.Log("Object Stiffness has been changed to: " + objStiffness.ToString());
    }
    public void ChangeObjStiffnessTwo()
    {
        objStiffness = 20;
        Debug.Log("Object Stiffness has been changed to: " + objStiffness.ToString());
    }
    public void ChangeObjStiffnessThree()
    {
        objStiffness = 30;
        Debug.Log("Object Stiffness has been changed to: " + objStiffness.ToString());
    }

}
