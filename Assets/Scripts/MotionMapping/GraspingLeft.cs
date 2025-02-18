using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;



public class GraspingLeft : MonoBehaviour
{
    [Header("Pressure intensity")]
    [Tooltip("Range 0.1-1.")]
    [Range(0.1f, 1f)]
    public float pressureIntensity = 0;

    [Header("Pressure speed")]
    [Tooltip("Range 0.1-1.")]
    [Range(0.1f, 1f)]
    public float targetSpeed = 0;

    [Header("Frequency Hz")]
    [Tooltip("Range 0.1Hz-40Hz.")]
    [Range(0.1f, 40f)]
    public float targetFrequency = 0;

    [Header("Vibration peak ratio")]
    [Tooltip("Range 0.1-1.")]
    [Range(0.1f, 1f)]
    public float vibrationPeakRatio = 0;

    [Header("Pulse count")]
    [Tooltip("Range 1-1000.")]
    [Range(1, 1000)]
    public UInt16 pulseCount = 0;


    private List<String> colNameList = new List<String>();
    private Rigidbody targetRigidbody;
    private GameObject[] realFingertip;
    private GameObject[] ghostFingertip;
    private DeformMesh[] deformer = new DeformMesh[5];
    private Collider[] collider = new Collider[5];
    private bool holdState = false;
    private bool startDeform = false;
    private Vector3[] distance = new Vector3[5];
    private bool[] firstContactDeform = new bool[5];

    private Text textSliderOn, textSliderOff, textPeakRatio, textPulseCount;
    private Slider sliderOn, sliderOff, sliderPeakRatio, sliderPulseCount;
    public Button OnMinusFive, OnMinusOne, OffMinusFive, OffMinusOne, OnPlusFive, OnPlusOne, OffPlusFive, OffPlusOne;
    public InputField setPressure, setFrequency;
    public Toggle togglePulse, toggleFloat;

    private FingerMapping_Left fingerMappingLeft;
    private float[] hapticStartPosition = new float[5];
    private float[] hapticModifiedPosition = new float[5];

    public BTCommu_Left btCommu;

    void Start()
    {
        ghostFingertip = GameObject.FindGameObjectsWithTag("GhostHand").OrderBy(g => g.transform.GetSiblingIndex()).ToArray();
        foreach (var bufObject in ghostFingertip)
        {
            bufObject.AddComponent<CollisionDetection>();
        }

        realFingertip = GameObject.FindGameObjectsWithTag("RealFingertip").OrderBy(g => g.transform.GetSiblingIndex())
            .ToArray();

        fingerMappingLeft = gameObject.GetComponent<FingerMapping_Left>();

        textSliderOn = GameObject.Find("Text Slider On").GetComponent<Text>();
        textSliderOff = GameObject.Find("Text Slider Off").GetComponent<Text>();
        sliderOn = GameObject.Find("Slider On Duration").GetComponent<Slider>();
        sliderOff = GameObject.Find("Slider Off Duration").GetComponent<Slider>();

        textPeakRatio = GameObject.Find("Text Slider Peak Ratio").GetComponent<Text>();
        sliderPeakRatio = GameObject.Find("Slider Peak Ratio").GetComponent<Slider>();
        textPulseCount = GameObject.Find("Text Slider Pulse").GetComponent<Text>();
        sliderPulseCount = GameObject.Find("Slider Pulse").GetComponent<Slider>();

        OnMinusFive.onClick.AddListener(delegate { sliderOn.value -= 5; });
        OnMinusOne.onClick.AddListener(delegate { sliderOn.value -= 1; });
        OnPlusFive.onClick.AddListener(delegate { sliderOn.value += 5; });
        OnPlusOne.onClick.AddListener(delegate { sliderOn.value += 1; });
        OffMinusFive.onClick.AddListener(delegate { sliderOff.value -= 5; });
        OffMinusOne.onClick.AddListener(delegate { sliderOff.value -= 1; });
        OffPlusFive.onClick.AddListener(delegate { sliderOff.value += 5; });
        OffPlusOne.onClick.AddListener(delegate { sliderOff.value += 1; });
    }

    void FixedUpdate()
    {
        textSliderOn.text = "On Duration: " + sliderOn.value;
        textSliderOff.text = "Off Duration: " + sliderOff.value;
        textPeakRatio.text = "Peak Ratio: " + sliderPeakRatio.value;
        textPulseCount.text = "Pulse Count: " + sliderPulseCount.value;

        if ((colNameList.Count >= 2) & colNameList.Contains("GhostThumbB"))
        {
            if (holdState == false)
            {
                gameObject.AddComponent<FixedJoint>();
                gameObject.GetComponent<FixedJoint>().connectedBody = targetRigidbody;

                holdState = true;
                Debug.Log("Create fixed joint");

                for (int i = 0; i < realFingertip.Length; i++)
                {
                    realFingertip[i].GetComponent<Collider>().isTrigger = true;
                }

                foreach (var VARIABLE in colNameList)
                {
                    Debug.Log(VARIABLE);
                }
            }
        }
        else
        {
            if (holdState == true)
            {
                Destroy(gameObject.GetComponent<FixedJoint>());

                holdState = false;
                Debug.Log("Destroy fixed joint");

                for (int i = 0; i < realFingertip.Length; i++)
                {
                    realFingertip[i].GetComponent<Collider>().isTrigger = false;
                }

                foreach (var VARIABLE in colNameList)
                {
                    Debug.Log(VARIABLE);
                }
            }
        }

        if (startDeform)
        {
            for (byte fingerID = 0; fingerID < 5; fingerID++)
            {
                if (collider[fingerID] != null)
                {
                    UpdateDeform(collider[fingerID], fingerID);
                }
            }

            if (colNameList.Count == 0)
            {
                startDeform = false;
            }
        }

    }

    //void OnCollisionStay(Collision collisionInfo)
    //{
    //    // Debug-draw all contact points and normals
    //    foreach (ContactPoint contact in collisionInfo.contacts)
    //    {
    //        Debug.DrawRay(contact.point, contact.normal, Color.white);
    //    }
    //}

    private void LeaveTrail(Vector3 point, float scale)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * scale;
        sphere.transform.position = point;
        //sphere.transform.parent = transform.parent;
        sphere.GetComponent<Collider>().enabled = false;
        Destroy(sphere, 1);
    }

    Vector3[] closestPointOnObjectCollider = new Vector3[5];
    Vector3[] normalDir = new Vector3[5];
    GameObject[] touchPointGameObject = new GameObject[5];
    GameObject[] forcePointGameObject = new GameObject[5];
    private int slidingThreshold = 20;  //滑动偏移角度阈值
    Ray[] ray_UpdateTouchPoint = new Ray[5];

    void CreateTouchPointGameObject(byte fingerID, Vector3 position, float scale, Collider col)
    {
        touchPointGameObject[fingerID] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        touchPointGameObject[fingerID].transform.localScale = Vector3.one * scale;
        touchPointGameObject[fingerID].transform.position = position;
        touchPointGameObject[fingerID].transform.parent = col.transform;
        touchPointGameObject[fingerID].GetComponent<Collider>().enabled = false;
        touchPointGameObject[fingerID].GetComponent<MeshRenderer>().enabled = false;
    }

    void CreateForcePointGameObject(byte fingerID, Vector3 position, float scale)
    {
        forcePointGameObject[fingerID] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        forcePointGameObject[fingerID].transform.localScale = Vector3.one * scale;
        forcePointGameObject[fingerID].transform.position = position;
        forcePointGameObject[fingerID].transform.parent = ghostFingertip[fingerID].transform;
        forcePointGameObject[fingerID].GetComponent<Collider>().enabled = false;
        forcePointGameObject[fingerID].GetComponent<MeshRenderer>().enabled = false;
    }

    void UpdateDeform(Collider col, byte fingerID)
    {
        if (firstContactDeform[fingerID] == true)
        {
            closestPointOnObjectCollider[fingerID] = col.ClosestPoint(ghostFingertip[fingerID].transform.position);

            //在近似碰撞点创建小球，附着在手指和碰撞物体上，作为标记，计算手指移动距离和角度
            CreateTouchPointGameObject(fingerID, closestPointOnObjectCollider[fingerID], 0.003f, col);
            CreateForcePointGameObject(fingerID, closestPointOnObjectCollider[fingerID], 0.003f);

            //手指尖中心位置到接触点向量，作为初始滑动圆锥轴线
            normalDir[fingerID] =
                -(forcePointGameObject[fingerID].transform.localPosition - ghostFingertip[fingerID].transform.localPosition).normalized;
            firstContactDeform[fingerID] = false;
            return;
        }

        distance[fingerID] = forcePointGameObject[fingerID].transform.position + 0.001f * normalDir[fingerID] -
                             touchPointGameObject[fingerID].transform.position;
        deformer[fingerID].AddDeformingForce(touchPointGameObject[fingerID].transform.position, distance[fingerID], fingerID);

        //判断手指滑动太大后，更新接触点位置，并更新滑动圆锥轴线为新接触点法线
        float angle = Vector3.Angle(normalDir[fingerID], distance[fingerID]);
        if (angle > slidingThreshold)
        {
            //更新ClosestPointOnObjectCollider的位置和法线
            UpdateTouchPoint(fingerID);
        }

        Debug.DrawRay(touchPointGameObject[fingerID].transform.position, distance[fingerID].normalized, Color.red);
        Debug.DrawRay(touchPointGameObject[fingerID].transform.position, normalDir[fingerID].normalized, Color.blue);
        //Debug.Log("Angle: " + angle);
        //Debug.Log("Distance: " + distance[fingerID]);

    }

    //理解为：原滑动圆锥轴线在平面内，绕过原接触点的平面法线旋转α角，获得Ray角度向量。过力点沿Ray角度做偏移，使Ray其实点在物体外，做Ray，与物体交点作为新接触点。新轴线为接触点法线。
    void UpdateTouchPoint(byte fingerID)
    {
        Vector3 rotAxis = Vector3.Cross(normalDir[fingerID], distance[fingerID].normalized).normalized;
        Vector3 rayDir = normalDir[fingerID] * (float)Math.Cos((double)slidingThreshold / 180 * Math.PI) + Vector3.Cross(rotAxis, normalDir[fingerID]) * (float)Math.Sin((double)slidingThreshold / 180 * Math.PI) + rotAxis * Vector3.Dot(rotAxis, normalDir[fingerID]) * (1 - (float)Math.Cos((double)slidingThreshold / 180 * Math.PI));
        rayDir = rayDir.normalized;
        Debug.DrawRay(touchPointGameObject[fingerID].transform.position, rotAxis, Color.yellow);
        Debug.DrawRay(touchPointGameObject[fingerID].transform.position, rayDir, Color.black);

        ray_UpdateTouchPoint[fingerID] = new Ray(forcePointGameObject[fingerID].transform.position - 0.1f * rayDir, rayDir);
        RaycastHit hitInfo;
        int layerMask = LayerMask.GetMask("PickUpAble");
        if (Physics.Raycast(ray_UpdateTouchPoint[fingerID], out hitInfo, 100, layerMask, QueryTriggerInteraction.Collide))
        {
            //Debug.Log("击中");
            //LeaveTrail(hitInfo.point, 0.003f);
            Debug.DrawLine(ray_UpdateTouchPoint[fingerID].origin, hitInfo.point, Color.black);
            touchPointGameObject[fingerID].transform.position = hitInfo.point;
            normalDir[fingerID] = -hitInfo.normal;
        }
        else
        {
            Debug.Log("未击中");
        }
    }

    public void ChildColliderState(Collider col, String bufName, String bufState)
    {
        targetRigidbody = col.GetComponent<Rigidbody>();
        var hapticMaterial = col.gameObject.GetComponent<HapMaterial>();

        if (hapticMaterial == null)
        {
            Debug.Log("No Haptic Material Assigned");
            return;
        }

        byte[] clutchState = Haptics.SetClutchState(bufName, bufState);// clutchState[0] = fingerID, clutchState[1] = Enter or Stay or Exit
        byte fingerID = clutchState[0];

        byte targetPres = hapticMaterial.targetPressure;

        //int presSource = (int)BTCommu_Left.Instance.pressureData[5];
        //byte[] valveTiming = HapMaterial.CalculateValveTiming(targetPres, clutchState[0], presSource);

        if (bufState == "Enter")
        {
            colNameList.Add(bufName);
            Haptics.ApplyHaptics(clutchState, targetPres, false);
            Debug.Log("Enter");

            collider[fingerID] = col;
            deformer[fingerID] = col.gameObject.GetComponent<DeformMesh>();
            if (deformer[fingerID] == null)
            {
                Debug.Log("It's a Rigid Object");
            }
            else
            {
                Debug.Log("It's a Soft Object");
                startDeform = true;
                firstContactDeform[fingerID] = true;
            }
        }
        else if (bufState == "Stay")
        {
            Debug.Log("Stay");
            return;
        }
        else if (bufState == "Exit")
        {
            colNameList.Remove(bufName);
            Haptics.ApplyHaptics(clutchState, targetPres, false);
            collider[fingerID] = null;
            deformer[fingerID] = null;
            Destroy(touchPointGameObject[fingerID]);
            Destroy(forcePointGameObject[fingerID]);
            Debug.Log("Exit");
        }
        else
        {
            return;
        }

    }

    byte[] SetValveTimingFromSlider()
    {
        byte[] valveTiming = new byte[2] { 0xff, 0xff };
        //byte vOpen = 50;
        //byte vDelay = 50;
        byte vOpen = (byte)sliderOn.value;
        byte vDelay = (byte)sliderOff.value;
        valveTiming[0] = vOpen;
        valveTiming[1] = vDelay;
        return valveTiming;
    }

    public void ThumbInClick()
    {
        byte[] clutchState = {0, 0};
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToSingle(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), Convert.ToByte(sliderPeakRatio.value), Convert.ToUInt16(sliderPulseCount.value));
        else if((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToSingle(setFrequency.text), clutchState, Convert.ToByte(setPressure.text),Convert.ToByte(sliderPeakRatio.value));
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);

    }
    public void ThumbExClick()
    {
        byte[] clutchState = { 0, 2 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToSingle(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), Convert.ToByte(sliderPeakRatio.value), Convert.ToUInt16(sliderPulseCount.value));
        else if((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToSingle(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), Convert.ToByte(sliderPeakRatio.value));
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void IndexInClick()
    {
        byte[] clutchState = { 1, 0 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void IndexExClick()
    {
        byte[] clutchState = { 1, 2 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void MiddleInClick()
    {
        byte[] clutchState = { 2, 0 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text),false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void MiddleExClick()
    {
        byte[] clutchState = { 2, 2 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void RingInClick()
    {
        byte[] clutchState = { 3, 0 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void RingExClick()
    {
        byte[] clutchState = { 3, 2 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void PinkyInClick()
    {
        byte[] clutchState = { 4, 0 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void PinkyExClick()
    {
        byte[] clutchState = { 4, 2 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }

    public void PalmInClick()
    {
        byte[] clutchState = { 5, 0 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }
    public void PalmExClick()
    {
        byte[] clutchState = { 5, 2 };
        byte[] valveTiming = SetValveTimingFromSlider();

        if (togglePulse.isOn & (setPressure.text != ""))
            Haptics.ApplyHaptics(0xff, clutchState, Convert.ToByte(setPressure.text), false);
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else if (setPressure.text != "")
        {
            if (toggleFloat.isOn)
                Haptics.ApplyHaptics(clutchState, Convert.ToSingle(setPressure.text), false);
            else
                Haptics.ApplyHaptics(clutchState, Convert.ToByte(setPressure.text), false);
        }
        else if ((setFrequency.text != "") & (setPressure.text != ""))
            Haptics.ApplyHaptics(Convert.ToByte(setFrequency.text), clutchState, Convert.ToByte(setPressure.text), false);
        else
            Haptics.ApplyHapticsWithTiming(clutchState, valveTiming);
    }

    public void AllInClick()
    {
        byte[][] clutchStates = new byte[6][]
        {
            new Byte[] { 0x00, 0x00 }, new Byte[] { 0x01, 0x00 }, new Byte[] { 0x02, 0x00 }, 
            new Byte[] { 0x03, 0x00 }, new Byte[] { 0x04, 0x00 }, new Byte[] { 0x05, 0x00 }
        };
        byte[][] valveTimings = new byte[6][]
        {
            SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider(),
            SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider()
        };
        if (setPressure.text != "")
            Haptics.ApplyHaptics(clutchStates, Convert.ToByte(setPressure.text), false);
        else
            Haptics.ApplyHapticsWithTiming(clutchStates, valveTimings);
    }
    public void AllExClick()
    {
        byte[][] clutchStates = new byte[6][]
        {
            new Byte[] { 0x00, 0x02 }, new Byte[] { 0x01, 0x02 }, new Byte[] { 0x02, 0x02 },
            new Byte[] { 0x03, 0x02 }, new Byte[] { 0x04, 0x02 }, new Byte[] { 0x05, 0x02 }
        };
        byte[][] valveTimings = new byte[6][]
        {
            SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider(),
            SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider()
        };
        if (setPressure.text != "")
            Haptics.ApplyHaptics(clutchStates, Convert.ToByte(setPressure.text), false);
        else
            Haptics.ApplyHapticsWithTiming(clutchStates, valveTimings);
    }

    public void TestClick(float interval_ms)
    {
        StartCoroutine(DelayTestSend(interval_ms));
    }

    IEnumerator DelayTestSend(float interval_ms)
    {
        int count = Convert.ToInt32(GameObject.Find("TextTestSendCount").GetComponent<InputField>().text);

        if (count > 0)
        {
            
        }
        else
        {
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            byte[][] clutchStates = new byte[6][]
            {
                new Byte[] { 0x00, 0x00 }, new Byte[] { 0x01, 0x00 }, new Byte[] { 0x02, 0x00 },
                new Byte[] { 0x03, 0x00 }, new Byte[] { 0x04, 0x00 }, new Byte[] { 0x05, 0x00 }
            };

            if (((i+1) % 2) == 0)
            {
                clutchStates = new byte[6][]
                {
                    new Byte[] { 0x00, 0x02 }, new Byte[] { 0x01, 0x02 }, new Byte[] { 0x02, 0x02 },
                    new Byte[] { 0x03, 0x02 }, new Byte[] { 0x04, 0x02 }, new Byte[] { 0x05, 0x02 }
                };
            }

            byte[][] valveTimings = new byte[6][]
            {
                SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider(),
                SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider()
            };
            if (setPressure.text != "")
                Haptics.ApplyHaptics(clutchStates, Convert.ToByte(setPressure.text), false);

            yield return new WaitForSeconds(interval_ms/1000f);
        }
        
    }

    private Text startPositionText, curPositionText;
    private int startPosition, curPosition;
    public void PalpationInClick()
    {
        startPositionText = GameObject.Find("TexStartForce").GetComponent<Text>();
        startPosition = (int)btCommu.microtubeData[1] + (int)btCommu.microtubeData[2] + (int)btCommu.microtubeData[3];
        startPositionText.text = startPosition.ToString();

        byte[][] clutchStates = new byte[][]
        {
            new Byte[] { 0x01, 0x00 }, new Byte[] { 0x02, 0x00 },
            new Byte[] { 0x03, 0x00 }
        };
        byte[][] valveTimings = new byte[][]
        {
            SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider()
        };
        if (setPressure.text != "")
            Haptics.ApplyHaptics(clutchStates, Convert.ToByte(setPressure.text), false);
        else
            Haptics.ApplyHapticsWithTiming(clutchStates, valveTimings);
    }
    public void PalpationExClick()
    {
        byte[][] clutchStates = new byte[][]
        {
            new Byte[] { 0x01, 0x02 }, new Byte[] { 0x02, 0x02 },
            new Byte[] { 0x03, 0x02 }
        };
        byte[][] valveTimings = new byte[][]
        {
            SetValveTimingFromSlider(), SetValveTimingFromSlider(), SetValveTimingFromSlider()
        };
        if (setPressure.text != "")
            Haptics.ApplyHaptics(clutchStates, Convert.ToByte(setPressure.text), false);
        else
            Haptics.ApplyHapticsWithTiming(clutchStates, valveTimings);
    }

    void Update()
    {
        curPositionText = GameObject.Find("TextCurrentForce").GetComponent<Text>();
        curPosition = (int)(btCommu.microtubeData[1] + btCommu.microtubeData[2] + btCommu.microtubeData[3] - startPosition);
        curPositionText.text = curPosition.ToString();
    }

    

    public void OnTestingVibButtonStartClicked()
    {
        //byte[] data = HEXRVibration(Finger.Thumb, true, targetFrequency);
        //byte[] data = HEXRVibration(Finger.Thumb, true, targetFrequency, pressureIntensity);
        //byte[] data = HEXRVibration(Finger.Thumb, true, targetFrequency, pressureIntensity, vibrationPeakRatio, targetSpeed);

        //byte[] data = HEXRPulse(Finger.Thumb, true, targetFrequency, pulseCount);
        //byte[] data = HEXRPulse(Finger.Thumb, true, targetFrequency, pressureIntensity, pulseCount);
        //byte[] data = HEXRPulse(Finger.Thumb, true, targetFrequency, pressureIntensity, vibrationPeakRatio, pulseCount);

        //byte[] data = HEXRPressure(Finger.Thumb, true, pressureIntensity, targetSpeed);

        //BTCommu_Left.Instance.BTSend(data);
    }

    public void OnTestingVibButtonStopClicked()
    {
        //byte[] data = HEXRVibration(Finger.Thumb, false, targetFrequency);
        //byte[] data = HEXRVibration(Finger.Thumb, false, targetFrequency, pressureIntensity);
        //byte[] data = HEXRVibration(Finger.Thumb, false, targetFrequency, pressureIntensity, vibrationPeakRatio, targetSpeed);

        //byte[] data = HEXRPulse(Finger.Thumb, false, targetFrequency, pulseCount);
        //byte[] data = HEXRPulse(Finger.Thumb, false, targetFrequency, pressureIntensity, pulseCount);
        //byte[] data = HEXRPulse(Finger.Thumb, false, targetFrequency, pressureIntensity, vibrationPeakRatio, pulseCount);

        //byte[] data = HEXRPressure(Finger.Thumb, false, pressureIntensity, targetSpeed);

        //BTCommu_Left.Instance.BTSend(data);
    }


    
}
