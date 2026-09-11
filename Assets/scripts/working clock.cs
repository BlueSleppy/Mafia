using System;
using UnityEngine;
using UnityEngine.UI;

public class workingclock : MonoBehaviour
{
    [Header("Analog hands (assign Transforms)")]
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;
    public Transform clockCenter;

    [Header("Digital display (optional)")]
    public Text digitalText;
    public bool use12HourFormat = false;

    [Header("Motion")]
    [Tooltip("Enable smooth (interpolated) movement for hands")]
    public bool smoothMovement = true;
    [Tooltip("Higher = faster interpolation smoothing")]
    public float smoothFactor = 10f;

    [Header("Rendering / placement fixes")]
    public bool enforceDepthOffset = true;
    public float depthOffset = -0.01f;
    public bool invertRotation = false;

    private Transform centerTransform;
    private Vector3 hourInitOffset;
    private Vector3 minuteInitOffset;
    private Vector3 secondInitOffset;
    private Quaternion hourInitRot;
    private Quaternion minuteInitRot;
    private Quaternion secondInitRot;

    void Start()
    {
        centerTransform = clockCenter != null ? clockCenter : transform;

        if (hourHand != null)
        {
            hourInitOffset = centerTransform.InverseTransformPoint(hourHand.position);
            hourInitRot = Quaternion.Inverse(centerTransform.rotation) * hourHand.rotation;
        }

        if (minuteHand != null)
        {
            minuteInitOffset = centerTransform.InverseTransformPoint(minuteHand.position);
            minuteInitRot = Quaternion.Inverse(centerTransform.rotation) * minuteHand.rotation;
        }

        if (secondHand != null)
        {
            secondInitOffset = centerTransform.InverseTransformPoint(secondHand.position);
            secondInitRot = Quaternion.Inverse(centerTransform.rotation) * secondHand.rotation;
        }
    }

    void Update()
    {
        DateTime now = DateTime.Now;
        float seconds = now.Second + now.Millisecond / 1000f;
        float minutes = now.Minute + seconds / 60f;
        float hours = (now.Hour % 12) + minutes / 60f;

        float secondAngle = seconds / 60f * 360f;
        float minuteAngle = minutes / 60f * 360f;
        float hourAngle = hours / 12f * 360f;

        if (secondHand != null)
        {
            ApplyRotation(secondHand, secondAngle, secondInitOffset, secondInitRot);
        }

        if (minuteHand != null)
        {
            ApplyRotation(minuteHand, minuteAngle, minuteInitOffset, minuteInitRot);
        }

        if (hourHand != null)
        {
            ApplyRotation(hourHand, hourAngle, hourInitOffset, hourInitRot);
        }

        if (digitalText != null)
        {
            string format = use12HourFormat ? "hh:mm:ss tt" : "HH:mm:ss";
            digitalText.text = now.ToString(format);
        }
    }

    private void ApplyRotation(Transform hand, float angle, Vector3 initOffset, Quaternion initRot)
    {
        float z = invertRotation ? angle : -angle;
        Quaternion spin = Quaternion.Euler(0f, 0f, z);
        Vector3 targetPos = centerTransform.TransformPoint(spin * initOffset);
        Quaternion targetRot = centerTransform.rotation * (spin * initRot);

        if (smoothMovement)
        {
            float tParam = 1f - Mathf.Exp(-smoothFactor * Time.deltaTime);
            hand.position = Vector3.Lerp(hand.position, targetPos, tParam);
            hand.rotation = Quaternion.Slerp(hand.rotation, targetRot, tParam);
        }
        else
        {
            hand.position = targetPos;
            hand.rotation = targetRot;
        }

        if (enforceDepthOffset)
        {
            Vector3 localPos = hand.localPosition;
            localPos.z = depthOffset;
            hand.localPosition = localPos;
        }
    }
}
