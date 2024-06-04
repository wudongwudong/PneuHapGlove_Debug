#define BLE

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using UnityEngine;
using ArduinoBluetoothAPI;
using UnityEngine.UI;

#if BLE
public class BTCommu_Left : MonoBehaviour
{
    //private FileStream fs;
    //private StreamWriter sw;

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

    public string deviceName;
    void Awake()
    {
        _instance = this;
    }

    public bool isMicrotube;

    private Text pressureSource, thumbPres, indexPres, middlePres, ringPres, pinkyPres, palmPres;
    private Text thumbR, indexR, middleR, ringR, pinkyR;

    [Header("BLE Connection")]
    public string targetDeviceName = "HaptGloveAR Right";
    public string serviceUuid = "{000000ff-0000-1000-8000-00805f9b34fb}";
    public string[] characteristicUuids = { "{0000ff01-0000-1000-8000-00805f9b34fb}" };

    BLE ble;
    BLE.BLEScan scan;
    public bool isScanning = false, btConnection = false, isTimerRunning = false, isCalibration = false;
    string deviceId = null;
    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    int devicesCount = 0;

    // BLE Threads 
    Thread scanningThread, connectionThread, readingThread, serialthread, calibrationThread;

    public bool isPause;

    public GraspingLeft graspingScript;
    public FingerMapping_Left fingerMappingLeftScript;
    public Int32[] microtubeData = new Int32[5];
    public float[] pressureData = new float[7];
    public byte sourcePres = 68; //kpa

    public bool flag_MicrotubeDataReady = false;
    public bool flag_BMP280DataReady = false;

    private List<byte> buffer = new List<byte>(1024);
    private byte[] oneFrame = new byte[128];

    private Int32[] tick = new int[2];



    private enum funList : byte
    {
        FI_BMP280 = 0x01,

        FI_MICROTUBE = 0x04,
        FI_CLUTCHGOTACTIVATED = 0x05
    };

    void Start()
    {
        pressureSource = GameObject.Find("TextPresSource").GetComponent<Text>();
        thumbPres = GameObject.Find("TextThumbPres").GetComponent<Text>();
        indexPres = GameObject.Find("TextIndexPres").GetComponent<Text>();
        middlePres = GameObject.Find("TextMiddlePres").GetComponent<Text>();
        ringPres = GameObject.Find("TextRingPres").GetComponent<Text>();
        pinkyPres = GameObject.Find("TextPinkyPres").GetComponent<Text>();
        palmPres = GameObject.Find("TextPalmPres").GetComponent<Text>();

        thumbR = GameObject.Find("TextThumbR").GetComponent<Text>();
        indexR = GameObject.Find("TextIndexR").GetComponent<Text>();
        middleR = GameObject.Find("TextMiddleR").GetComponent<Text>();
        ringR = GameObject.Find("TextRingR").GetComponent<Text>();
        pinkyR = GameObject.Find("TextPinkyR").GetComponent<Text>();

        ble = new BLE();

        readingThread = new Thread(ReadBleData);


        //string filePath = "C:\\Users\\JM\\Desktop\\"+ "ScissorCuttingData_"+deviceName +".csv"; //scissor cutting
        //if (File.Exists(filePath))
        //{
        //    fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        //}
        //else
        //{
        //    fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        //}
        //sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

        //sw.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
        //sw.WriteLine(deviceName + "," + DateTime.Now.ToString("yyyy-MM-dd"));
        //sw.Flush();

        //sw.Close();
        //fs.Close();

        isPause = false;
    }

    void Update()
    {
        thumbPres.text = pressureData[0].ToString();
        indexPres.text = pressureData[1].ToString();
        middlePres.text = pressureData[2].ToString();
        ringPres.text = pressureData[3].ToString();
        pinkyPres.text = pressureData[4].ToString();
        palmPres.text = pressureData[5].ToString();
        pressureSource.text = pressureData[6].ToString();

        thumbR.text = microtubeData[0].ToString();
        indexR.text = microtubeData[1].ToString();
        middleR.text = microtubeData[2].ToString();
        ringR.text = microtubeData[3].ToString();
        pinkyR.text = microtubeData[4].ToString();

        Grapher.Log(microtubeData[0], "CH0", Color.white);
        Grapher.Log(microtubeData[1], "CH1", Color.red);
        Grapher.Log(microtubeData[2], "CH2", Color.green);
        Grapher.Log(microtubeData[3], "CH3", Color.cyan);

        if (!isPause)
        {
            //Scan BLE devices 
            if (isScanning)
            {
                /*if (scene.name != "straightPathsLevel" && scene.name != "shooting")
                {
                    if (ButtonStartScan.enabled)
                        ButtonStartScan.enabled = false;
                }*/

                if (discoveredDevices.Count > devicesCount)
                {
                    foreach (KeyValuePair<string, string> entry in discoveredDevices.ToList())
                    {
                        Debug.Log("Added device: " + entry.Key);
                    }
                    devicesCount = discoveredDevices.Count;
                }
            }

            // The target device was found.
            if (deviceId != null && deviceId != "-1")
            {
                // Target device is connected and GUI knows.
                if (ble.isConnected && btConnection)
                {
                    if (!readingThread.IsAlive)
                    {
                        readingThread = new Thread(ReadBleData);
                        readingThread.Start();
                    }
                }
                // Target device is connected, but GUI hasn't updated yet.
                else if (ble.isConnected && !btConnection)
                {
                    btConnection = true;
                    /*if (scene.name != "straightPathsLevel" && scene.name != "shooting")
                    {
                        ButtonEstablishConnection.enabled = false;
                    }*/
                    Debug.Log("Connected to target device: " + targetDeviceName);
                }

                /*else if (scene.name != "straightPathsLevel" && scene.name != "shooting" && !ButtonEstablishConnection.enabled && !_connected)
                {
                    ButtonEstablishConnection.enabled = true;
                    Debug.Log("Found target device:\n" + targetDeviceName);
                }*/


            }
        }
    }

    public InputField presInputField;
    public InputField durationInputField;
    public InputField durationInputField_Sec;
    public Toggle toggleDemoForce;
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
        Debug.Log(duration + "\t" + duration_sec);

        Haptics.ApplyHapticsWithTiming(new byte[] { 2, 0 }, new byte[] { pressure, 14 });
        Debug.Log("DemoForceStart: " + DateTime.Now.ToString("HH:mm:ss.fff"));
        //delay
        startTime = Environment.TickCount;
        startDelay = true;


        endDelay = false;
    }

    private bool endDelay = false;

    void FixedUpdate()
    {
        if (startDelay)
        {
            if (Environment.TickCount >= startTime + duration)
            {
                Haptics.ApplyHapticsWithTiming(new byte[] { 2, 2 }, new byte[] { pressure, 255 });
                Debug.Log("DemoForceEnd: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                startDelay = false;
                endTime = Environment.TickCount;
                endDelay = true;
            }
        }

        //if (endDelay)
        //{
        //    if (Environment.TickCount >= endTime + duration_sec)
        //    {
        //        Haptics.ApplyHapticsWithTiming(new byte[] { 2, 2 }, new byte[] { pressure, 255 });
        //        Debug.Log("DemoForceEnd: " + DateTime.Now.ToString("HH:mm:ss.fff"));
        //        endDelay = false;
        //    }

        //}





        //if (startDelay)
        //{
        //    if (Environment.TickCount >= startTime + 10000)
        //    {
        //        if (endDelay == false)
        //        {
        //            endDelay = true;
        //            Haptics.ApplyHapticsWithTiming(new byte[] { 2, 2 }, new byte[] { pressure, 255 });
        //            Debug.Log("5");
        //        }
        //    }
        //    else if (Environment.TickCount >= startTime + 8500)
        //    {
        //        if (endDelay == true)
        //        {
        //            endDelay = false;
        //            Haptics.ApplyHapticsWithTiming(new byte[] { 2, 2 }, new byte[] { 10, 7 });
        //            Debug.Log("4");
        //        }
        //    }
        //    else if (Environment.TickCount >= startTime + 6000)
        //    {
        //        if (endDelay == false)
        //        {
        //            endDelay = true;
        //            Haptics.ApplyHapticsWithTiming(new byte[] { 2, 0 }, new byte[] { 10, 7 });
        //            Debug.Log("3");
        //        }
        //    }
        //    else if (Environment.TickCount >= startTime + 5000)
        //    {
        //        if (endDelay == true)
        //        {
        //            endDelay = false;
        //            Haptics.ApplyHapticsWithTiming(new byte[] { 2, 2 }, new byte[] { 10, 7 });
        //            Debug.Log("2");
        //        }
        //    }
        //    else if (Environment.TickCount >= startTime + 2500)
        //    {
        //        if (endDelay == false)
        //        {
        //            endDelay = true;
        //            Haptics.ApplyHapticsWithTiming(new byte[] { 2, 0 }, new byte[] { 10, 7 });
        //            Debug.Log("1");
        //        }

        //    }


        //}

    }

    // Establish communication
    public void BTConnection()
    {
        if (ble.isConnected)
        {
            ble.Close();
            ble = new BLE();

            Debug.Log("STOP BLE. BLE isConnected: " + ble.isConnected.ToString());
        }
        else
        {
            devicesCount = 0;
            isScanning = true;
            discoveredDevices.Clear();
            deviceId = null;
            scanningThread = new Thread(ScanBleDevices);
            scanningThread.Start();
            Debug.Log("Scanning for..." + targetDeviceName);
        }
    }

    // Start establish BLE connection with
    // target device in dedicated thread.
    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start();
    }

    void ScanBleDevices()
    {
        scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            Debug.Log("found device with name: " + deviceName);
            discoveredDevices.Add(_deviceId, deviceName);

            //if found the target device, immediately stop scan and attempt to connect
            if (deviceId == null && deviceName.Contains(targetDeviceName))
            {
                deviceId = _deviceId;
                //ble.deviceID = deviceId;
                StartConHandler();
            }
        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null)
            Thread.Sleep(500);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;

        if (deviceId == "-1")
        {
            Debug.Log("no device found!");
            return;
        }

    }

    private void ConnectBleDevice()
    {
        if (deviceId != null)
        {
            try
            {
                ble.Connect(deviceId,
                    serviceUuid,
                    characteristicUuids.ToArray());
            }
            catch (Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + deviceId + "\n" + e);
            }
        }
        if (ble.isConnected)
            Debug.Log("Connected to: " + targetDeviceName);
    }

    void ReadBleData()
    {
        byte[] buf = BLE.ReadBytes(); //data input via bytes

        ProcessByteData(buf);

        //Debug.Log("Received data length: " + buf.Length);
    }

    void ProcessByteData(byte[] buf)
    {
        //try
        //{
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
        //}
        //catch (Exception e)
        //{
        //    Debug.Log(e.ToString());
        //}
    }

    // Reset BLE handler
    public void ResetHandler()
    {
        // Reset previous discovered devices
        discoveredDevices.Clear();
        deviceId = null;
        CleanUp();

    }


    public int? BTSend(byte[] data)
    {
        try
        {
            int length = data.Length;
            BLE.WritePackage(deviceId, serviceUuid, characteristicUuids[0], data);

            Debug.Log("发送的数据：" + BitConverter.ToString(data));
            return length;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return null;
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
            case (byte)funList.FI_MICROTUBE:      //Microtube
                decodeMicrotube(frame);
                break;
            case (byte)funList.FI_CLUTCHGOTACTIVATED:
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
        pressureData[6] = BitConverter.ToSingle(frame, 33);

        flag_BMP280DataReady = true;

        //sw.WriteLine( "," + pressureData[0] );

        //Debug.Log("AirPressure:  "+ pressureData[0]+ "\t" + pressureData[1] + "\t" + pressureData[2] + "\t" + pressureData[3] + "\t" + pressureData[4] + "\t" + pressureData[5] + "\t" + pressureData[6]);

    }



    void decodeMicrotube(byte[] frame)
    {
        //Debug.Log(frame.Length);
        //Debug.Log(BitConverter.ToString(frame));
        if (isMicrotube)
        {
            microtubeData[0] = (BitConverter.ToInt16(frame, 3));
            microtubeData[1] = (BitConverter.ToInt16(frame, 6));
            microtubeData[2] = (BitConverter.ToInt16(frame, 9));
            microtubeData[3] = (BitConverter.ToInt16(frame, 12));
            microtubeData[4] = (BitConverter.ToInt16(frame, 15));
        }
        else
        {
            microtubeData[0] = (BitConverter.ToInt32(frame, 3));
            microtubeData[1] = (BitConverter.ToInt32(frame, 8));
            microtubeData[2] = (BitConverter.ToInt32(frame, 13));
            microtubeData[3] = (BitConverter.ToInt32(frame, 18));
            microtubeData[4] = (BitConverter.ToInt32(frame, 23));
        }

        flag_MicrotubeDataReady = true;
        //FingerMapping_Left.Instance.UpdateFingerPosLeft();
        //Debug.Log(microtubeData[0] + "\t" + microtubeData[1] + "\t" + microtubeData[2] + "\t" + microtubeData[3] + "\t" + microtubeData[4]);



        //if (isFirstRotationData)
        //{ 
        //    lastMicrotubeData[0] = microtubeData[0];
        //    convertedCurRotationData[0] = microtubeData[0];
        //    convertedLastRotationData[0] = convertedCurRotationData[0];
        //    pointsAfterJump = pointsIgnoredAfterJump;
        //    isFirstRotationData = false;
        //}
        //else
        //{
        //    pointsAfterJump++;
        //    if (pointsAfterJump >= pointsIgnoredAfterJump)
        //    {
        //        if ((microtubeData[0] - lastMicrotubeData[0]) > multiTurnJumpThreshold)
        //        {
        //            multiTurns[0]--;
        //            pointsAfterJump = 0;
        //        }
        //        else if ((microtubeData[0] - lastMicrotubeData[0]) < -multiTurnJumpThreshold)
        //        {
        //            multiTurns[0]++;
        //            pointsAfterJump = 0;
        //        }
        //    }

        //}

        //convertedCurRotationData[0] = microtubeData[0] + multiTurns[0] * (maxRotationData - minRotationData);
        //convertedRotationData[0] = (convertedCurRotationData[0] + convertedLastRotationData[0]) / 2;

        //convertedLastRotationData[0] = convertedCurRotationData[0];
        //lastMicrotubeData[0] = microtubeData[0];

        //Grapher.Log(convertedRotationData[0], "Converted", Color.green);
        //Grapher.Log((int)microtubeData[0], "Raw", Color.white);

        //Debug.Log(microtubeData[0] + ", "  + convertedRotationData[0]);
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
                BTSend(buf);
                airPresSourceCtrlStarted = true;

                //byte[] buf2 = BLE.ReadBytes(06);
                string buf2 = BLE.ReadPackage();
                //BLE.ReadBytes(deviceId, serviceUuid, characteristicUuids[0],);

                Debug.Log("Data read: " + buf2);
            }
            catch (Exception e) { Debug.Log(e); }
        }
        else
        {
            try
            {
                Encode.Instance.add_u8(0x00);               // Pressure source control stop
                Encode.Instance.add_u8(0);                  // set pressure source to 0
                byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
                Encode.Instance.clear_list();
                BTSend(buf);
                airPresSourceCtrlStarted = false;
            }
            catch (Exception e) { Debug.Log(e); }
        }

    }

    void OnDestroy()
    {
        ResetHandler();
        flag_MicrotubeDataReady = false;

        //sw.Close();
        //fs.Close();
    }
    // Handle Quit Game
    void OnApplicationQuit()
    {
        ResetHandler();
    }

    // Prevent threading issues and free BLE stack.
    // Can cause Unity to freeze and lead
    // to errors when omitted.
    void CleanUp()
    {
        try
        {
            scan.Cancel();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Scan never initialized.\n" + e);
        }


        try
        {
            ble.Close();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("ble never initialized.\n" + e);
        }

        try
        {
            scanningThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Scan thread never initialized.\n" + e);
        }

        try
        {
            connectionThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Connection thread never initialized.\n" + e);
        }

        try
        {
            readingThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Reading thread never initialized.\n" + e);
        }
    }


    void ExitProgram()
    {
        OnApplicationQuit();
        CleanUp();
        Application.Quit();
    }

}
#else
public class BTCommu_Left : MonoBehaviour
{
    //static BTCommu_Left _instance;
    //public static BTCommu_Left Instance
    //{
    //    get
    //    {
    //        if (_instance == null)
    //        {
    //            _instance = new BTCommu_Left();
    //        }
    //        return _instance;
    //    }
    //}

    //private FileStream fs;
    //private StreamWriter sw;

    void Awake()
    {
        //_instance = this;

        //string filePath = "C:\\Users\\65110\\Desktop\\UserStudyData.csv";
        //string filePath = "C:\\Users\\JM\\Desktop\\ScissorCuttingData.csv"; //scissor cutting
        //if (File.Exists(filePath))
        //{
        //    fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        //}
        //else
        //{
        //    fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        //}
        //sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

        //sw.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
        //sw.WriteLine(deviceName + "," + DateTime.Now.ToString("yyyy-MM-dd"));
        //sw.Flush();

        //sw.Close();
        //fs.Close();
    }

    public BluetoothHelper btHelper;
    public bool btConnection = false;
    public string deviceName = "PneuHapGlove L";
    public byte sourcePres = 68; //kpa

    public GraspingLeft graspingScript;
    public FingerMapping_Left fingerMappingLeftScript;
    public Int16[] microtubeData = new Int16[5];
    private Quaternion rawRotation = Quaternion.identity;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 acceleration = Vector3.zero;
    public float[] pressureData = new float[6];

    public bool flag_PositionDataReady = false;
    public bool flag_RotationDataReady = false;
    public bool flag_MicrotubeDataReady = false;
    public bool flag_BMP280DataReady = false;

    private List<byte> buffer = new List<byte>(1024);
    private byte[] oneFrame = new byte[128];

    private Int32[] tick = new int[2];

    public FixObjectPosition scissors;


    private enum funList : byte
    {
        FI_BMP280 = 0x01,
        FI_MICROTUBE = 0x04,
        FI_CLUTCHGOTACTIVATED = 0x05
    };

    void Start()
    {
        btHelper = BluetoothHelper.GetNewInstance(deviceName);
        btHelper.OnConnected += BtHelper_OnConnected;
        btHelper.OnConnectionFailed += BtHelper_OnConnectionFailed;
        btHelper.OnDataReceived += BtHelper_OnDataReceived;
        btHelper.setFixedLengthBasedStream(51);

        GameObject buf = GameObject.Find("HandPosition");
        if (buf != null)
        {
            scissors = buf.GetComponent<FixObjectPosition>();
        }

        //string filePath = "C:\\Users\\JM\\Desktop\\"+ "ScissorCuttingData_"+deviceName +".csv"; //scissor cutting
        //if (File.Exists(filePath))
        //{
        //    fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        //}
        //else
        //{
        //    fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        //}
        //sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

        //sw.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
        //sw.WriteLine(deviceName + "," + DateTime.Now.ToString("yyyy-MM-dd"));
        //sw.Flush();

        //sw.Close();
        //fs.Close();
    }



    void Update()
    {
        if (btHelper.isConnected())
        {
            btConnection = true;
        }
        else
        {
            btConnection = false;
        }
    }

    // Establish communication
    public void BTConnection()
    {
        //if (btHelper == null)
        //{
        //    btHelper = BluetoothHelper.GetInstance(deviceName);
        //    btHelper.OnConnected += BtHelper_OnConnected;
        //    btHelper.OnConnectionFailed += BtHelper_OnConnectionFailed;
        //    btHelper.OnDataReceived += BtHelper_OnDataReceived;
        //    btHelper.setFixedLengthBasedStream(51);
        //}

        if (btHelper.isConnected())
        {
            btHelper.Disconnect();
            flag_PositionDataReady = false;
            flag_RotationDataReady = false;
            flag_MicrotubeDataReady = false;

            //btHelper.OnConnected -= BtHelper_OnConnected;
            //btHelper.OnConnectionFailed -= BtHelper_OnConnectionFailed;
            //btHelper.OnDataReceived -= BtHelper_OnDataReceived;
            //btHelper = null;

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
            Debug.Log("StartListening 1");
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }


    public int? BTSend(byte[] data)
    {
        try
        {
            int length = data.Length;
            btHelper.SendData(data);

            Debug.Log("Sent：" + BitConverter.ToString(data));
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
        Debug.Log("fail 1");
        //throw new NotImplementedException();
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
            case (byte)funList.FI_MICROTUBE:      //Microtube
                decodeMicrotube(frame);
                break;
            case (byte)funList.FI_CLUTCHGOTACTIVATED:
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

        //sw.WriteLine( "," + pressureData[0] );

        //Debug.Log("AirPressure:  "+ pressureData[0]+ "\t" + pressureData[1] + "\t" + pressureData[2] + "\t" + pressureData[3] + "\t" + pressureData[4] + "\t" + pressureData[5]);

        //Grapher.Log(pressureData[1], "Pressure Source", Color.white);
    }


    void decodeMicrotube(byte[] frame)
    {
        microtubeData[0] = (BitConverter.ToInt16(frame, 3));
        microtubeData[1] = (BitConverter.ToInt16(frame, 6));
        microtubeData[2] = (BitConverter.ToInt16(frame, 9));
        microtubeData[3] = (BitConverter.ToInt16(frame, 12));
        microtubeData[4] = (BitConverter.ToInt16(frame, 15));
        flag_MicrotubeDataReady = true;
        fingerMappingLeftScript.UpdateFingerPosLeft();
        graspingScript.GetCurrentMicrotubeData(fingerMappingLeftScript.normalizedData);

        if (scissors != null)
        {
            scissors.Scissors(fingerMappingLeftScript.normalizedData);
        }

        //sw.Write(Environment.TickCount + "," + microtubeData[0]);
        //Debug.Log(DateTime.Now.ToString("HH:mm:ss.fff"));
        //sw.Flush();

        //Debug.Log(fingerMappingLeftScript.normalizedData[0] + "\t" + fingerMappingLeftScript.normalizedData[1] + "\t" + fingerMappingLeftScript.normalizedData[2] + "\t" + fingerMappingLeftScript.normalizedData[3] + "\t" + fingerMappingLeftScript.normalizedData[4]);

        if (frame[1] == (byte)funList.FI_CLUTCHGOTACTIVATED)
        {
            byte[] buf = { frame[2], frame[5], frame[8], frame[11], frame[14] };

            for (int i = 0; i < 5; i++)
            {
                if (buf[i] == (byte)0xff)
                {
                    //graspingScript.hapticStartPosition[i] = microtubeData[i];
                    graspingScript.hapticStartPosition[i] = fingerMappingLeftScript.normalizedData[i];
                    graspingScript.hapticGraspingIsStart[i] = true;
                }
            }



            //int clutchID = Array.IndexOf(buf, (byte)0xff);

            //graspingScript.hapticStartPosition[clutchID] = microtubeData[clutchID];
        }
    }


    void OnDestroy()
    {
        if (btHelper != null)
            btHelper.Disconnect();
        flag_PositionDataReady = false;
        flag_RotationDataReady = false;
        flag_MicrotubeDataReady = false;

        //sw.Close();
        //fs.Close();
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
                BTSend(buf);
                airPresSourceCtrlStarted = true;
            }
            catch (Exception e) { Debug.Log(e); }
        }
        else
        {
            try
            {
                Encode.Instance.add_u8(0x00);               // Pressure source control stop
                Encode.Instance.add_u8(0);                  // set pressure source to 0
                byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
                Encode.Instance.clear_list();
                BTSend(buf);
                airPresSourceCtrlStarted = false;
            }
            catch (Exception e) { Debug.Log(e); }
        }

    }

    //public void ExRemainedPres()
    //{
    //    byte[][] clutchStates =
    //        {new byte[] {0, 2}, new byte[] {1, 2}, new byte[] {2, 2}, new byte[] {3, 2}, new byte[] {4, 2}};
    //    Haptics.ApplyHapticsToMultipleFingers(clutchStates, 40);

    //    List<byte> arrowHandHapticsFrame = new List<byte>();

    //    arrowHandHapticsFrame.AddRange(ConstructArrowHandHapticsFrame(0, 2));
    //    arrowHandHapticsFrame.AddRange(ConstructArrowHandHapticsFrame(1, 2));
    //    arrowHandHapticsFrame.AddRange(ConstructArrowHandHapticsFrame(2, 2));
    //    arrowHandHapticsFrame.AddRange(ConstructArrowHandHapticsFrame(3, 2));
    //    arrowHandHapticsFrame.AddRange(ConstructArrowHandHapticsFrame(4, 2));

    //    BTCommu_Left.Instance.BTSend(arrowHandHapticsFrame.ToArray());
    //}

    //byte[] ConstructArrowHandHapticsFrame(byte fingerID, byte status)
    //{
    //    byte tarPres = 20;
    //    byte[] valveTiming = HapMaterial.CalculateValveTiming(tarPres, fingerID, (int)pressureData[5]);
    //    byte vOpen = valveTiming[0];
    //    byte vDelay = valveTiming[1];
    //    //编码+BT发送
    //    Encode.Instance.add_u8(fingerID); // which finger
    //    Encode.Instance.add_u8(status); // enter, stay or exit
    //    Encode.Instance.add_u8(tarPres);
    //    Encode.Instance.add_u8(vOpen);
    //    Encode.Instance.add_u8(vDelay);
    //    byte[] buf = Encode.Instance.add_fun(0x03); // FI = 3
    //    Encode.Instance.clear_list();

    //    return buf;
    //}


}
#endif


























//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using System.Linq;
//using System.Runtime.InteropServices;
//using UnityEngine;
//using ArduinoBluetoothAPI;
//using UnityEngine.UI;


//public class BTCommu_Left : MonoBehaviour
//{
//    static BTCommu_Left _instance;
//    public static BTCommu_Left Instance
//    {
//        get
//        {
//            if (_instance == null)
//            {
//                _instance = new BTCommu_Left();
//            }
//            return _instance;
//        }
//    }

//    void Awake()
//    {
//        _instance = this;
//    }

//    public bool isMicrotube;

//    public byte sourcePres = 50;

//    private BluetoothHelper btHelper;
//    public string deviceName = "PneuHapGlove L";

//    public Int32[] microtubeData = new Int32[5];
//    private Quaternion rawRotation = Quaternion.identity;
//    public Quaternion rotation = Quaternion.identity;
//    public Vector3 acceleration = Vector3.zero;
//    public float[] pressureData = new float[7];

//    public bool flag_PositionDataReady = false;
//    public bool flag_RotationDataReady = false;
//    public bool flag_MicrotubeDataReady = false;
//    public bool flag_BMP280DataReady = false;

//    private List<byte> buffer = new List<byte>(1024);
//    private byte[] oneFrame = new byte[128];

//    private Int32[] tick = new int[2];

//    private Text pressureSource, thumbPres, indexPres, middlePres, ringPres, pinkyPres, palmPres;
//    private Text thumbR, indexR, middleR, ringR, pinkyR;

//    private DateTime btStartTime;
//    private DateTime btEndTime;

//    private FileStream fs;
//    private StreamWriter sw;
//    private long milliseconds_Start;

//    public Toggle lifetimeTestStart;
//    public Slider totalTimeSlider;

//    public InputField presInputField;
//    public InputField durationInputField;
//    public InputField durationInputField_Sec;
//    public Toggle toggleDemoForce;

//    private enum funList : byte
//    {
//        FI_BMP280 = 0x01,
//        FI_POSITION = 0x02,
//        FI_ROTATION = 0x03,
//        FI_MICROTUBE = 0x04
//    };

//    void Start()
//    {
//        btHelper = BluetoothHelper.GetInstance(deviceName);
//        btHelper.OnConnected += BtHelper_OnConnected;
//        btHelper.OnConnectionFailed += BtHelper_OnConnectionFailed;
//        btHelper.OnDataReceived += BtHelper_OnDataReceived;
//        btHelper.setFixedLengthBasedStream(66);
//        //btHelper.setLengthBasedStream();

//        /////////////////////////////////////////////////////////////////////////////////////////////
//        pressureSource = GameObject.Find("TextPresSource").GetComponent<Text>();
//        thumbPres = GameObject.Find("TextThumbPres").GetComponent<Text>();
//        indexPres = GameObject.Find("TextIndexPres").GetComponent<Text>();
//        middlePres = GameObject.Find("TextMiddlePres").GetComponent<Text>();
//        ringPres = GameObject.Find("TextRingPres").GetComponent<Text>();
//        pinkyPres = GameObject.Find("TextPinkyPres").GetComponent<Text>();
//        palmPres = GameObject.Find("TextPinkyPres").GetComponent<Text>();

//        thumbR = GameObject.Find("TextThumbR").GetComponent<Text>();
//        indexR = GameObject.Find("TextIndexR").GetComponent<Text>();
//        middleR = GameObject.Find("TextMiddleR").GetComponent<Text>();
//        ringR = GameObject.Find("TextRingR").GetComponent<Text>();
//        pinkyR = GameObject.Find("TextPinkyR").GetComponent<Text>();

//        //存Pressure source和index的气压
//        string filePath = "C:\\Users\\65110\\Desktop\\PressureData " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") +
//                          ".csv";
//        fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
//        sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
//        sw.WriteLine("Time" + "," + "PressureSource(Pa)" + "," + "ThumbChannel(Pa)" + "," + "IndexChannel(Pa)" + "," + "MiddleChannel(Pa)" + "," + "RingChannel(Pa)" + "," + "PinkyChannel(Pa)");
//        sw.Flush();
//        milliseconds_Start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
//    }

//    private byte pressure;
//    private int duration;
//    private int duration_sec;
//    private bool startDelay = false;
//    private long startTime;
//    private long endTime;

//    public void SendHapticsWithDuration()
//    {
//        pressure = Convert.ToByte(presInputField.text);
//        duration = Convert.ToInt32(durationInputField.text);
//        duration_sec = Convert.ToInt32(durationInputField_Sec.text);

//        Haptics.ApplyHapticsWithTiming(new byte[] { 1, 0 }, new byte[] { pressure, 14 });
//        Debug.Log("DemoForceStart: " + DateTime.Now.ToString("HH:mm:ss.fff"));
//        //delay
//        startTime = Environment.TickCount;
//        startDelay = true;



//    }

//    private bool endDelay = false;
//    void FixedUpdate()
//    {
//        if (startDelay)
//        {
//            if (Environment.TickCount >= startTime + duration)
//            {
//                Haptics.ApplyHapticsWithTiming(new byte[] { 1, 2 }, new byte[] { pressure, 14 });
//                Debug.Log("DemoForceEnd: " + DateTime.Now.ToString("HH:mm:ss.fff"));
//                startDelay = false;
//                endTime = Environment.TickCount;
//                endDelay = true;
//            }
//        }

//        if (endDelay)
//        {
//            if (Environment.TickCount >= endTime + duration_sec)
//            {
//                Haptics.ApplyHapticsWithTiming(new byte[] { 1, 2 }, new byte[] { pressure, 255 });
//                Debug.Log("DemoForceEnd: " + DateTime.Now.ToString("HH:mm:ss.fff"));
//                endDelay = false;
//            }

//        }

//    }

//    private float timerOn = 0f;
//    private float timerOff = 2f;//3
//    private byte fingerID = 0;
//    private bool btStart = false;
//    private bool btEndTimeIsPrint = false;

//    //持续单指充气30kpa
//    void Update()
//    {
//        //if (btHelper.isConnected())
//        //{
//        //    if (lifetimeTestStart.isOn)
//        //    {
//        //        timerOn -= Time.deltaTime;
//        //        if (timerOn <= 0)
//        //        {
//        //            byte[] clutchState = { fingerID, 0 };
//        //            byte[] clutchTiming = { 27, 150 };
//        //            Haptics.ApplyHapticsWithTiming(clutchState, clutchTiming);
//        //            timerOn = totalTimeSlider.value;//6
//        //            //Debug.Log("Haptic Loop On:\t" + clutchState[0] + "\t" + clutchState[1]);
//        //        }

//        //        timerOff -= Time.deltaTime;
//        //        if (timerOff <= 0)
//        //        {
//        //            byte[] clutchState = { fingerID, 2 };
//        //            byte[] clutchTiming = { 27, 150 };
//        //            Haptics.ApplyHapticsWithTiming(clutchState, clutchTiming);
//        //            timerOff = totalTimeSlider.value;//6
//        //            //fingerID++;
//        //            //if (fingerID == 5)
//        //            //{
//        //            //    fingerID = 0;
//        //            //}
//        //            //Debug.Log("Haptic Loop Off:\t" + clutchState[0] + "\t" + clutchState[1]);
//        //        }
//        //    }
//        //    else
//        //    {
//        //        timerOn = 0f;
//        //        timerOff = 2f;//3
//        //    }

//        //}
//        //else
//        //{
//        //    if ((btStart == true) & (btEndTimeIsPrint == false))
//        //    {
//        //        btEndTime = DateTime.Now;
//        //        Debug.Log("Haptic Glove End: " + btEndTime);
//        //        btEndTimeIsPrint = true;
//        //    }

//        //}

//    }

//    // Establish communication
//    public void BTConnection()
//    {
//        if (btHelper.isConnected())
//        {
//            btHelper.Disconnect();
//            flag_PositionDataReady = false;
//            flag_RotationDataReady = false;
//            flag_MicrotubeDataReady = false;
//        }
//        else
//        {
//            btHelper.Connect();
//        }

//    }

//    private void BtHelper_OnConnected(BluetoothHelper helper)
//    {
//        BluetoothDevice btServer = btHelper.getBluetoothDevice();
//        Debug.Log("Name: " + btServer.DeviceName);

//        try
//        {
//            btHelper.StartListening();
//            Debug.Log("StartListening");
//        }
//        catch (Exception e)
//        {
//            Debug.Log(e.ToString());
//        }

//        btStart = true;
//        btStartTime = DateTime.Now;
//        Debug.Log("Haptic Glove Start: " + btStartTime);
//    }

//    public int? BTSend(byte[] data)
//    {
//        try
//        {
//            int length = data.Length;
//            btHelper.SendData(data);

//            Debug.Log("发送的数据：" + BitConverter.ToString(data));
//            return length;
//        }
//        catch (Exception e)
//        {
//            Debug.Log(e.ToString());
//            return null;
//        }
//    }

//    private void BtHelper_OnConnectionFailed(BluetoothHelper helper)
//    {
//        throw new NotImplementedException();
//    }

//    private void BtHelper_OnDataReceived(BluetoothHelper helper)
//    {
//        //tick[0] = tick[1];
//        //tick[1] = System.Environment.TickCount;
//        //Debug.Log("Frequency =" + (float) (1000 / (tick[1] - tick[0] + 1)));
//        //Debug.Log("DataReceived");
//        //try
//        //{
//        byte[] buf = btHelper.ReadBytes();
//        buffer.AddRange(buf);
//        //Debug.Log(buf.Length + "\t" + buffer.Count);
//        //Debug.Log(BitConverter.ToString(buf));
//        while (buffer.Count >= 5)
//        {
//            if (Enum.IsDefined(typeof(funList), buffer[1]))
//            {
//                int len = buffer[0];//帧长度
//                if (buffer.Count < len) break;//数据不够直接跳出

//                byte checkSum = 0;
//                for (int i = 0; i < len - 1; i++)
//                {
//                    checkSum ^= buffer[i];
//                }

//                if (checkSum != buffer[len - 1])
//                {
//                    buffer.RemoveRange(0, len);
//                    continue;
//                }

//                buffer.CopyTo(0, oneFrame, 0, len);
//                buffer.RemoveRange(0, len);
//                FrameDataAnalysis(oneFrame);
//            }
//            else
//            {
//                buffer.RemoveAt(0);
//            }
//        }
//        //}
//        //catch (Exception e)
//        //{
//        //    Debug.Log(e.ToString());
//        //}
//    }

//    void FrameDataAnalysis(byte[] frame)
//    {
//        //Debug.Log("frame[1]:  " + frame[1]);
//        switch (frame[1])
//        {
//            case (byte)funList.FI_BMP280:         //BMP280
//                decodePressure(frame);
//                break;
//            case (byte)funList.FI_POSITION:            //IMU
//                //decodePosition(frame);
//                break;
//            case (byte)funList.FI_ROTATION:            //IMU
//                decodeOritation(frame);
//                break;
//            case (byte)funList.FI_MICROTUBE:      //Microtube
//                decodeMicrotube(frame);
//                break;
//            default:
//                break;
//        }
//    }

//    void decodePressure(byte[] frame)
//    {
//        pressureData[0] = BitConverter.ToSingle(frame, 3);
//        pressureData[1] = BitConverter.ToSingle(frame, 8);
//        pressureData[2] = BitConverter.ToSingle(frame, 13);
//        pressureData[3] = BitConverter.ToSingle(frame, 18);
//        pressureData[4] = BitConverter.ToSingle(frame, 23);
//        pressureData[5] = BitConverter.ToSingle(frame, 28);
//        pressureData[6] = BitConverter.ToSingle(frame, 33);


//        flag_BMP280DataReady = true;
//        //Debug.Log("AirPressure:  "+ pressureData[0]+ "\t" + pressureData[1] + "\t" + pressureData[2] + "\t" + pressureData[3] + "\t" + pressureData[4] + "\t" + pressureData[5]);

//        thumbPres.text = pressureData[0].ToString();
//        indexPres.text = pressureData[1].ToString();
//        middlePres.text = pressureData[2].ToString();
//        ringPres.text = pressureData[3].ToString();
//        pinkyPres.text = pressureData[4].ToString();
//        palmPres.text = pressureData[5].ToString();
//        pressureSource.text = pressureData[6].ToString();


//        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
//        sw.WriteLine(milliseconds - milliseconds_Start + "," + pressureData[5].ToString() + "," +
//                     pressureData[0].ToString() + "," +
//                     pressureData[1].ToString() + "," +
//                     pressureData[2].ToString() + "," +
//                     pressureData[3].ToString() + "," +
//                     pressureData[4].ToString());
//        sw.Flush();
//    }

//    void decodePosition(byte[] frame)
//    {
//        acceleration.x = (BitConverter.ToInt16(frame, 2)) / 32768.0f * 16;
//        acceleration.y = (BitConverter.ToInt16(frame, 4)) / 32768.0f * 16;
//        acceleration.z = (BitConverter.ToInt16(frame, 6)) / 32768.0f * 16;

//        flag_PositionDataReady = true;
//        //Debug.Log(acceleration.x.ToString() + "\t" + acceleration.y.ToString() + "\t" + acceleration.z.ToString() + "\t" + acceleration.magnitude);
//    }

//    void decodeOritation(byte[] frame)
//    {
//        rawRotation.w = (BitConverter.ToInt16(frame, 2));
//        rawRotation.z = -(BitConverter.ToInt16(frame, 4));
//        rawRotation.x = -(BitConverter.ToInt16(frame, 6));
//        rawRotation.y = (BitConverter.ToInt16(frame, 8));
//        rotation = rawRotation;
//        flag_RotationDataReady = true;
//        //Debug.Log(rotation.w.ToString() + "\t" + rotation.x.ToString() + "\t" + rotation.y.ToString() + "\t" + rotation.z.ToString());
//    }

//    //private short[] lastMicrotubeData = new short[5];
//    //private int[] convertedRotationData = new int[5];
//    //private int[] convertedCurRotationData = new int[5];
//    //private int[] convertedLastRotationData = new int[5];
//    //public int[] multiTurns = new int[5];
//    //public bool isFirstRotationData = true;
//    //public int multiTurnJumpThreshold = 1500;
//    //private int minRotationData = 62;
//    //private int maxRotationData = 3884;
//    //private int pointsIgnoredAfterJump = 3; // ensure only one jump at a time
//    //private int pointsDelayAfterJump = 3;
//    //private int pointsAfterJump = 0;

//    void decodeMicrotube(byte[] frame)
//    {
//        //Debug.Log(frame.Length);
//        //Debug.Log(BitConverter.ToString(frame));
//        if (isMicrotube)
//        {
//            microtubeData[0] = (BitConverter.ToInt16(frame, 3));
//            microtubeData[1] = (BitConverter.ToInt16(frame, 6));
//            microtubeData[2] = (BitConverter.ToInt16(frame, 9));
//            microtubeData[3] = (BitConverter.ToInt16(frame, 12));
//            microtubeData[4] = (BitConverter.ToInt16(frame, 15));
//        }
//        else
//        {
//            microtubeData[0] = (BitConverter.ToInt32(frame, 3));
//            microtubeData[1] = (BitConverter.ToInt32(frame, 8));
//            microtubeData[2] = (BitConverter.ToInt32(frame, 13));
//            microtubeData[3] = (BitConverter.ToInt32(frame, 18));
//            microtubeData[4] = (BitConverter.ToInt32(frame, 23));
//        }

//        flag_MicrotubeDataReady = true;
//        //FingerMapping_Left.Instance.UpdateFingerPosLeft();
//        //Debug.Log(microtubeData[0] + "\t" + microtubeData[1] + "\t" + microtubeData[2] + "\t" + microtubeData[3] + "\t" + microtubeData[4]);

//        thumbR.text = microtubeData[0].ToString();
//        indexR.text = microtubeData[1].ToString();
//        middleR.text = microtubeData[2].ToString();
//        ringR.text = microtubeData[3].ToString();
//        pinkyR.text = microtubeData[4].ToString();

//        Grapher.Log(microtubeData[0], "CH0", Color.white);
//        Grapher.Log(microtubeData[1], "CH1", Color.red);
//        Grapher.Log(microtubeData[2], "CH2", Color.green);
//        Grapher.Log(microtubeData[3], "CH3", Color.cyan);

//        //if (isFirstRotationData)
//        //{ 
//        //    lastMicrotubeData[0] = microtubeData[0];
//        //    convertedCurRotationData[0] = microtubeData[0];
//        //    convertedLastRotationData[0] = convertedCurRotationData[0];
//        //    pointsAfterJump = pointsIgnoredAfterJump;
//        //    isFirstRotationData = false;
//        //}
//        //else
//        //{
//        //    pointsAfterJump++;
//        //    if (pointsAfterJump >= pointsIgnoredAfterJump)
//        //    {
//        //        if ((microtubeData[0] - lastMicrotubeData[0]) > multiTurnJumpThreshold)
//        //        {
//        //            multiTurns[0]--;
//        //            pointsAfterJump = 0;
//        //        }
//        //        else if ((microtubeData[0] - lastMicrotubeData[0]) < -multiTurnJumpThreshold)
//        //        {
//        //            multiTurns[0]++;
//        //            pointsAfterJump = 0;
//        //        }
//        //    }

//        //}

//        //convertedCurRotationData[0] = microtubeData[0] + multiTurns[0] * (maxRotationData - minRotationData);
//        //convertedRotationData[0] = (convertedCurRotationData[0] + convertedLastRotationData[0]) / 2;

//        //convertedLastRotationData[0] = convertedCurRotationData[0];
//        //lastMicrotubeData[0] = microtubeData[0];

//        //Grapher.Log(convertedRotationData[0], "Converted", Color.green);
//        //Grapher.Log((int)microtubeData[0], "Raw", Color.white);

//        //Debug.Log(microtubeData[0] + ", "  + convertedRotationData[0]);
//    }


//    void OnDestroy()
//    {
//        if (btHelper != null)
//            btHelper.Disconnect();
//        flag_PositionDataReady = false;
//        flag_RotationDataReady = false;
//        flag_MicrotubeDataReady = false;

//        sw.Close();
//        fs.Close();
//    }

//    public bool airPresSourceCtrlStarted = false;
//    public void AirPressureSourceControl()
//    {
//        if (airPresSourceCtrlStarted == false)
//        {
//            try
//            {
//                Encode.Instance.add_u8(0x01);               // Pressure source control start
//                Encode.Instance.add_u8(sourcePres);                 // set pressure source to 50kPa
//                byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
//                Encode.Instance.clear_list();
//                BTCommu_Left.Instance.BTSend(buf);
//                airPresSourceCtrlStarted = true;
//            }
//            catch (Exception e) { Debug.Log(e); }
//        }
//        else
//        {
//            try
//            {
//                Encode.Instance.add_u8(0x00);               // Pressure source control stop
//                Encode.Instance.add_u8(0);                  // set pressure source to 0
//                byte[] buf = Encode.Instance.add_fun(0x02); // FI_STABLE_PRESSURE_CTRL
//                Encode.Instance.clear_list();
//                BTCommu_Left.Instance.BTSend(buf);
//                airPresSourceCtrlStarted = false;
//            }
//            catch (Exception e) { Debug.Log(e); }
//        }

//    }


//}
