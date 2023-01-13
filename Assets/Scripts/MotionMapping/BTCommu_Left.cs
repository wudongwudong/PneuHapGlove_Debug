using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using UnityEngine;
using ArduinoBluetoothAPI;
using UnityEngine.UI;


public class BTCommu_Left : MonoBehaviour
{
    static BTCommu_Left _instance;
    public static BTCommu_Left Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BTCommu_Left();
            }
            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;
    }

    public byte sourcePres = 50;

    private BluetoothHelper btHelper;
    public string deviceName = "PneuHapGlove L";

    public Int16[] microtubeData = new Int16[5];
    private Quaternion rawRotation = Quaternion.identity;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 acceleration = Vector3.zero;
    public double[] pressureData = new double[6];

    public bool flag_PositionDataReady = false;
    public bool flag_RotationDataReady = false;
    public bool flag_MicrotubeDataReady = false;
    public bool flag_BMP280DataReady = false;

    private List<byte> buffer = new List<byte>(1024);
    private byte[] oneFrame = new byte[128];

    private Int32[] tick = new int[2];

    private Text pressureSource, thumbPres, indexPres, middlePres, ringPres, pinkyPres;
    private Text thumbR, indexR, middleR, ringR, pinkyR;

    private DateTime btStartTime;
    private DateTime btEndTime;

    private FileStream fs;
    private StreamWriter sw;
    private long milliseconds_Start;

    public Toggle lifetimeTestStart;
    public Slider totalTimeSlider;

    public InputField presInputField;
    public InputField durationInputField;
    public InputField durationInputField_Sec;
    public Toggle toggleDemoForce;

    private enum funList : byte
    {
        FI_BMP280 = 0x01,
        FI_POSITION = 0x02,
        FI_ROTATION = 0x03,
        FI_MICROTUBE = 0x04
    };

    void Start()
    {
        btHelper = BluetoothHelper.GetInstance(deviceName);
        btHelper.OnConnected += BtHelper_OnConnected;
        btHelper.OnConnectionFailed += BtHelper_OnConnectionFailed;
        btHelper.OnDataReceived += BtHelper_OnDataReceived;
        btHelper.setFixedLengthBasedStream(97);
        //btHelper.setLengthBasedStream();

        /////////////////////////////////////////////////////////////////////////////////////////////
        pressureSource = GameObject.Find("TextPresSource").GetComponent<Text>();
        thumbPres = GameObject.Find("TextThumbPres").GetComponent<Text>();
        indexPres = GameObject.Find("TextIndexPres").GetComponent<Text>();
        middlePres = GameObject.Find("TextMiddlePres").GetComponent<Text>();
        ringPres = GameObject.Find("TextRingPres").GetComponent<Text>();
        pinkyPres = GameObject.Find("TextPinkyPres").GetComponent<Text>();

        thumbR = GameObject.Find("TextThumbR").GetComponent<Text>();
        indexR = GameObject.Find("TextIndexR").GetComponent<Text>();
        middleR = GameObject.Find("TextMiddleR").GetComponent<Text>();
        ringR = GameObject.Find("TextRingR").GetComponent<Text>();
        pinkyR = GameObject.Find("TextPinkyR").GetComponent<Text>();

        //存Pressure source和index的气压
        string filePath = "C:\\Users\\65110\\Desktop\\PressureData " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") +
                          ".csv";
        fs = new FileStream(filePath,FileMode.Create,FileAccess.ReadWrite);
        sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        sw.WriteLine("Time" + "," + "PressureSource(Pa)" + "," + "ThumbChannel(Pa)" + "," + "IndexChannel(Pa)" + "," + "MiddleChannel(Pa)" + "," + "RingChannel(Pa)" + "," + "PinkyChannel(Pa)");
        sw.Flush();
        milliseconds_Start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private byte pressure;
    private int duration;
    private int duration_sec;
    private bool startDelay = false;
    private long startTime;
    private long endTime;

    public void SendHapticsWithDuration()
    {
        pressure = Convert.ToByte(presInputField.text);
        duration = Convert.ToInt32(durationInputField.text);
        duration_sec = Convert.ToInt32(durationInputField_Sec.text);

        Haptics.ApplyHapticsWithTiming(new byte[] {1, 0}, new byte[]{pressure,14});
        Debug.Log("DemoForceStart: " + DateTime.Now.ToString("HH:mm:ss.fff"));
        //delay
        startTime = Environment.TickCount;
        startDelay = true;
        
        
        
    }

    private bool endDelay = false;
    void FixedUpdate()
    {
        if (startDelay)
        {
            if (Environment.TickCount >= startTime + duration)
            {
                Haptics.ApplyHapticsWithTiming(new byte[] { 1, 2 }, new byte[] { pressure, 14 });
                Debug.Log("DemoForceEnd: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                startDelay = false;
                endTime = Environment.TickCount;
                endDelay = true;
            }
        }

        if (endDelay)
        {
            if (Environment.TickCount >= endTime + duration_sec)
            {
                Haptics.ApplyHapticsWithTiming(new byte[] { 1, 2 }, new byte[] { pressure, 255 });
                Debug.Log("DemoForceEnd: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                endDelay = false;
            }
            
        }

    }

    private float timerOn = 0f;
    private float timerOff = 2f;//3
    private byte fingerID = 0;
    private bool btStart = false;
    private bool btEndTimeIsPrint = false;

    //持续单指充气30kpa
    void Update()
    {
        if (btHelper.isConnected())
        {
            if (lifetimeTestStart.isOn)
            {
                timerOn -= Time.deltaTime;
                if (timerOn <= 0)
                {
                    byte[] clutchState = { fingerID, 0 };
                    byte[] clutchTiming = { 27, 150 };
                    Haptics.ApplyHapticsWithTiming(clutchState, clutchTiming);
                    timerOn = totalTimeSlider.value;//6
                    //Debug.Log("Haptic Loop On:\t" + clutchState[0] + "\t" + clutchState[1]);
                }

                timerOff -= Time.deltaTime;
                if (timerOff <= 0)
                {
                    byte[] clutchState = { fingerID, 2 };
                    byte[] clutchTiming = { 27, 150 };
                    Haptics.ApplyHapticsWithTiming(clutchState, clutchTiming);
                    timerOff = totalTimeSlider.value;//6
                    //fingerID++;
                    //if (fingerID == 5)
                    //{
                    //    fingerID = 0;
                    //}
                    //Debug.Log("Haptic Loop Off:\t" + clutchState[0] + "\t" + clutchState[1]);
                }
            }
            else
            {
                timerOn = 0f;
                timerOff = 2f;//3
            }

        }
        else
        {
            if ((btStart == true) & (btEndTimeIsPrint == false))
            {
                btEndTime = DateTime.Now;
                Debug.Log("Haptic Glove End: " + btEndTime);
                btEndTimeIsPrint = true;
            }

        }

    }

    // Establish communication
    public void BTConnection()
    {
        if (btHelper.isConnected())
        {
            btHelper.Disconnect();
            flag_PositionDataReady = false;
            flag_RotationDataReady = false;
            flag_MicrotubeDataReady = false;
        }
        else
        {
            btHelper.Connect();
        }
        
    }

    private void BtHelper_OnConnected(BluetoothHelper helper)
    {
        BluetoothDevice btServer = btHelper.getBluetoothDevice();
        Debug.Log("Name: " + btServer.DeviceName);

        try
        {
            btHelper.StartListening();
            Debug.Log("StartListening");
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        btStart = true;
        btStartTime = DateTime.Now;
        Debug.Log("Haptic Glove Start: " + btStartTime);
    }

    public int? BTSend(byte[] data)
    {
        try
        {
            int length = data.Length;
            btHelper.SendData(data);
            
            Debug.Log("发送的数据：" + BitConverter.ToString(data));
            return length;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return null;
        }
    }

    private void BtHelper_OnConnectionFailed(BluetoothHelper helper)
    {
        throw new NotImplementedException();
    }

    private void BtHelper_OnDataReceived(BluetoothHelper helper)
    {
        //tick[0] = tick[1];
        //tick[1] = System.Environment.TickCount;
        //Debug.Log("Frequency =" + (float) (1000 / (tick[1] - tick[0] + 1)));
        //Debug.Log("DataReceived");
        try
        {
            byte[] buf = btHelper.ReadBytes();
            buffer.AddRange(buf);
            //Debug.Log(buf.Length + "\t" + buffer.Count);
            //Debug.Log(BitConverter.ToString(buf));
            while (buffer.Count >= 5)
            {
                if (Enum.IsDefined(typeof(funList), buffer[1]))
                {
                    int len = buffer[0];//帧长度
                    if (buffer.Count < len) break;//数据不够直接跳出

                    byte checkSum = 0;
                    for (int i = 0; i < len - 1; i++)
                    {
                        checkSum ^= buffer[i];
                    }

                    if (checkSum != buffer[len - 1])
                    {
                        buffer.RemoveRange(0, len);
                        continue;
                    }

                    buffer.CopyTo(0, oneFrame, 0, len);
                    buffer.RemoveRange(0, len);
                    FrameDataAnalysis(oneFrame);
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void FrameDataAnalysis(byte[] frame)
    {
        //Debug.Log("frame[1]:  " + frame[1]);
        switch (frame[1])
        {
            case (byte)funList.FI_BMP280:         //BMP280
                decodePressure(frame);
                break;
            case (byte)funList.FI_POSITION:            //IMU
                //decodePosition(frame);
                break;
            case (byte)funList.FI_ROTATION:            //IMU
                decodeOritation(frame);
                break;
            case (byte)funList.FI_MICROTUBE:      //Microtube
                decodeMicrotube(frame);
                break;
            default:
                break;
        }
    }

    void decodePressure(byte[] frame)
    {
        pressureData[0] = BitConverter.ToSingle(frame, 3);
        pressureData[1] = BitConverter.ToSingle(frame, 8);
        pressureData[2] = BitConverter.ToSingle(frame, 13);
        pressureData[3] = BitConverter.ToSingle(frame, 18);
        pressureData[4] = BitConverter.ToSingle(frame, 23);
        pressureData[5] = BitConverter.ToSingle(frame, 28);

        flag_BMP280DataReady = true;
        //Debug.Log("AirPressure:  "+ pressureData[0]+ "\t" + pressureData[1] + "\t" + pressureData[2] + "\t" + pressureData[3] + "\t" + pressureData[4] + "\t" + pressureData[5]);
        
        thumbPres.text = pressureData[0].ToString();
        indexPres.text = pressureData[1].ToString();
        middlePres.text = pressureData[2].ToString();
        ringPres.text = pressureData[3].ToString();
        pinkyPres.text = pressureData[4].ToString();
        pressureSource.text = pressureData[5].ToString();


        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        sw.WriteLine(milliseconds - milliseconds_Start + "," + pressureData[5].ToString() + "," +
                     pressureData[0].ToString() + "," +
                     pressureData[1].ToString() + "," +
                     pressureData[2].ToString() + "," +
                     pressureData[3].ToString() + "," +
                     pressureData[4].ToString());
        sw.Flush();
    }

    void decodePosition(byte[] frame)
    {
        acceleration.x = (BitConverter.ToInt16(frame, 2)) / 32768.0f * 16;
        acceleration.y = (BitConverter.ToInt16(frame, 4)) / 32768.0f * 16;
        acceleration.z = (BitConverter.ToInt16(frame, 6)) / 32768.0f * 16;

        flag_PositionDataReady = true;
        IMULeft.Instance.UpdatePositionLeft(acceleration);
        //Debug.Log(acceleration.x.ToString() + "\t" + acceleration.y.ToString() + "\t" + acceleration.z.ToString() + "\t" + acceleration.magnitude);
    }

    void decodeOritation(byte[] frame)
    {
        rawRotation.w = (BitConverter.ToInt16(frame, 2));
        rawRotation.z = -(BitConverter.ToInt16(frame, 4));
        rawRotation.x = -(BitConverter.ToInt16(frame, 6));
        rawRotation.y = (BitConverter.ToInt16(frame, 8));
        rotation = rawRotation;
        flag_RotationDataReady = true;
        IMULeft.Instance.UpdateRotationLeft();
        //Debug.Log(rotation.w.ToString() + "\t" + rotation.x.ToString() + "\t" + rotation.y.ToString() + "\t" + rotation.z.ToString());
    }


    void decodeMicrotube(byte[] frame)
    {
        microtubeData[0] = (BitConverter.ToInt16(frame, 3));
        microtubeData[1] = (BitConverter.ToInt16(frame, 6));
        microtubeData[2] = (BitConverter.ToInt16(frame, 9));
        microtubeData[3] = (BitConverter.ToInt16(frame, 12));
        microtubeData[4] = (BitConverter.ToInt16(frame, 15));
        flag_MicrotubeDataReady = true;
        FingerMapping_Left.Instance.UpdateFingerPosLeft();
        //Debug.Log(microtubeData[0] + "\t" + microtubeData[1] + "\t" + microtubeData[2] + "\t" + microtubeData[3] + "\t" + microtubeData[4]);

        thumbR.text = microtubeData[0].ToString();
        indexR.text = microtubeData[1].ToString();
        middleR.text = microtubeData[2].ToString();
        ringR.text = microtubeData[3].ToString();
        pinkyR.text = microtubeData[4].ToString();
    }


    void OnDestroy()
    {
        if (btHelper != null)
            btHelper.Disconnect();
        flag_PositionDataReady = false;
        flag_RotationDataReady = false;
        flag_MicrotubeDataReady = false;

        sw.Close();
        fs.Close(); 
    }

    public bool airPresSourceCtrlStarted = false;
    public void AirPressureSourceControl()
    {
        if (airPresSourceCtrlStarted == false) 
        {
            try
            {
                Encode.Instance.add_u8(0x01);               // Pressure source control start
                Encode.Instance.add_u8(sourcePres);                 // set pressure source to 50kPa
                byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
                Encode.Instance.clear_list();
                BTCommu_Left.Instance.BTSend(buf);
                airPresSourceCtrlStarted = true;
            }
            catch (Exception e) {Debug.Log(e);}
        }
        else
        {
            try
            {
                Encode.Instance.add_u8(0x00);               // Pressure source control stop
                Encode.Instance.add_u8(0);                  // set pressure source to 0
                byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
                Encode.Instance.clear_list();
                BTCommu_Left.Instance.BTSend(buf);
                airPresSourceCtrlStarted = false;
            }
            catch (Exception e) { Debug.Log(e); }
        }
        
    }


}
