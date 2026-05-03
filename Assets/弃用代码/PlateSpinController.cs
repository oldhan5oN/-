using UnityEngine;

public class PlateSpinController : MonoBehaviour
{
    [Header("References")]
    public Transform stickTip;

    [Header("Plate Settings")]
    public float plateRadius = 0.5f;
    public Vector3 localSpinAxis = Vector3.up;

    [Header("Spin Settings")]
    public float spinSpeed = 0f;
    public float maxSpinSpeed = 900f;
    public float spinAcceleration = 500f;
    public float spinDecay = 120f;

    [Header("Stability")]
    [Range(0f, 1f)]
    public float spinStability = 0f;

    public float stabilityGain = 0.8f;
    public float stabilityLoss = 0.25f;

    [Header("Pose")]
    public float hangingTiltAngle = 55f;
    public float alignSpeed = 8f;
    public float positionFollowSpeed = 20f;

    [Header("Debug / Input")]
    public bool debugUseKeyboard = true;
    public KeyCode debugShakeKey = KeyCode.Space;

    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
    }

    void Update()
    {
        float shakeAmount = GetShakeAmount();

        UpdateSpin(shakeAmount);
        UpdatePlatePose();
    }

    float GetShakeAmount()
    {
        if (debugUseKeyboard)
        {
            return Input.GetKey(debugShakeKey) ? 1f : 0f;
        }

        // 后面把这里换成 Kinect 手部抖动检测值
        return 0f;
    }

    void UpdateSpin(float shakeAmount)
    {
        if (shakeAmount > 0.2f)
        {
            spinSpeed += spinAcceleration * shakeAmount * Time.deltaTime;
            spinStability += stabilityGain * shakeAmount * Time.deltaTime;
        }
        else
        {
            spinSpeed -= spinDecay * Time.deltaTime;
            spinStability -= stabilityLoss * Time.deltaTime;
        }

        spinSpeed = Mathf.Clamp(spinSpeed, 0f, maxSpinSpeed);
        spinStability = Mathf.Clamp01(spinStability);
    }

    void UpdatePlatePose()
    {
        if (stickTip == null)
            return;

        // 0 = 杆子顶边缘，1 = 杆子顶中心
        Vector3 edgeLocalPoint = new Vector3(plateRadius, 0f, 0f);
        Vector3 centerLocalPoint = Vector3.zero;

        Vector3 localContactPoint = Vector3.Lerp(edgeLocalPoint, centerLocalPoint, spinStability);

        // 盘子从倾斜逐渐变正
        Quaternion hangingRotation =
            baseRotation * Quaternion.Euler(0f, 0f, hangingTiltAngle);

        Quaternion uprightRotation = baseRotation;

        Quaternion targetBaseRotation =
            Quaternion.Slerp(hangingRotation, uprightRotation, spinStability);

        // 在目标姿态基础上自转
        Quaternion spinRotation =
            Quaternion.AngleAxis(spinSpeed * Time.time, localSpinAxis);

        Quaternion targetRotation = targetBaseRotation * spinRotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            alignSpeed * Time.deltaTime
        );

        // 让盘子上的接触点贴到杆尖
        Vector3 worldContactOffset =
            transform.TransformVector(localContactPoint);

        Vector3 targetPosition =
            stickTip.position - worldContactOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            positionFollowSpeed * Time.deltaTime
        );
    }
}