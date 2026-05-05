using UnityEngine;

public class PlateJugglingController : MonoBehaviour
{
    public enum PlateState
    {
        Hidden,
        FallingOntoStick,
        HangingOnEdge,
        SpinningUp,
        StableSpinning,
        FallingOff
    }

    [Header("References")]
    public Transform stickRoot;
    public Transform stickTip;
    public Rigidbody stickRb;

    public Rigidbody plateRb;
    public ConfigurableJoint joint;

    [Header("Plate Settings")]
    public float plateRadius = 0.25f;

    [Tooltip("盘子自转轴。大多数平放在 XZ 平面的盘子用 Y 轴。")]
    public Vector3 localSpinAxis = Vector3.up;

    [Header("Spawn / Catch")]
    public float spawnHeight = 1.5f;
    public float catchDistance = 0.25f;
    public float minStickUprightDot = 0.65f;

    [Header("Spin")]
    public float spinSpeed;
    public float maxSpinSpeed = 1200f;
    public float spinAcceleration = 800f;
    public float spinDecay = 120f;

    [Header("Stability")]
    [Range(0f, 1f)]
    public float stability;

    public float stabilityGain = 0.7f;
    public float stabilityLoss = 0.35f;
    public float uprightTorque = 20f;

    [Header("Joint")]
    public float linearLimit = 0.15f;
    public float jointPositionSpring = 250f;
    public float jointPositionDamper = 25f;

    [Header("Angular Limit")]
    public float angularXLowLimit = -60f;
    public float angularXHighLimit = 60f;
    public float angularZLimit = 177f;

    [Header("Fall")]
    public float fallDelay = 0.4f;

    [Header("Reset Settings")]
    [Tooltip("盘子掉落后是否自动复位")]
    public bool autoReset = true;

    [Tooltip("掉落后多久自动复位（秒）")]
    public float resetDelay = 2f;

    [Tooltip("复位时盘子的高度阈值（低于这个高度视为掉地上了）")]
    public float groundThreshold = -1f;

    [Header("Debug")]
    [Range(0f, 1f)]
    public float debugShakeAmount = 0f;

    public PlateState state = PlateState.Hidden;

    private float unstableTimer;
    private Vector3 edgeAnchor;
    private Vector3 centerAnchor;

    // 复位相关
    private float fallTimer;
    private bool hasFallen;

    private void Awake()
    {
        if (plateRb == null)
            plateRb = GetComponent<Rigidbody>();

        if (joint == null)
            joint = GetComponent<ConfigurableJoint>();

        edgeAnchor = new Vector3(plateRadius, 0f, 0f);
        centerAnchor = Vector3.zero;

        SetupJointBaseParams();
        DeactivateJointConstraint();
        HidePlateAtStart();

        Debug.Log("初始化完成");
        Debug.Log("当前状态：" + state);
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case PlateState.Hidden:
                break;

            case PlateState.FallingOntoStick:
                UpdateFallingOntoStick();
                break;

            case PlateState.HangingOnEdge:
            case PlateState.SpinningUp:
            case PlateState.StableSpinning:
                UpdatePlateOnStick();
                break;

            case PlateState.FallingOff:
                UpdateFallingOff();
                break;
        }
    }

    private void Update()
    {
        // 快捷键：按 R 键复位
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPlate();
        }

        // 快捷键：按 Space 键生成盘子
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnPlateAboveStick();
        }
    }

    private void SetupJointBaseParams()
    {
        if (joint == null)
        {
            Debug.LogWarning("盘子上没有 ConfigurableJoint。");
            return;
        }

        joint.autoConfigureConnectedAnchor = false;

        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = linearLimit;

        joint.linearLimit = limit;
    }

    private void DeactivateJointConstraint()
    {
        if (joint == null)
            return;

        joint.connectedBody = null;

        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
    }

    private void ActivateJointConstraint()
    {
        if (joint == null)
            return;

        joint.connectedBody = stickRb;

        joint.anchor = Vector3.zero;
        joint.connectedAnchor = stickRoot.InverseTransformPoint(stickTip.position);

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
    }

    private void HidePlateAtStart()
    {
        state = PlateState.Hidden;

        DeactivateJointConstraint();

        if (plateRb != null)
        {
            plateRb.linearVelocity = Vector3.zero;
            plateRb.angularVelocity = Vector3.zero;
            plateRb.useGravity = false;
            plateRb.isKinematic = true;
        }

        gameObject.SetActive(false);

        hasFallen = false;
        fallTimer = 0f;
    }

    public void SpawnPlateAboveStick()
    {
        if (stickTip == null || plateRb == null)
        {
            Debug.LogWarning("缺少 stickTip 或 plateRb 引用。");
            return;
        }

        gameObject.SetActive(true);

        DeactivateJointConstraint();

        transform.position = stickTip.position + Vector3.up * spawnHeight;
        transform.rotation = Quaternion.Euler(-90f, 0f, 0);

        plateRb.linearVelocity = Vector3.zero;
        plateRb.angularVelocity = Vector3.zero;
        plateRb.isKinematic = false;
        plateRb.useGravity = true;

        spinSpeed = 0f;
        stability = 0f;
        unstableTimer = 0f;

        hasFallen = false;
        fallTimer = 0f;

        state = PlateState.FallingOntoStick;

        Debug.Log("盘子从上方掉下。");
    }

    public void UpdateFallingOntoStick()
    {
        if (!IsStickUpright())
            return;

        transform.position = new Vector3(stickTip.position.x, transform.position.y, stickTip.position.z);

        float distanceToTip = Vector3.Distance(transform.position, stickTip.position);

        if (distanceToTip <= catchDistance)
        {
            AttachToStickEdge();
        }
    }

    private void AttachToStickEdge()
    {
        if (joint == null || stickRb == null || stickRoot == null || stickTip == null)
        {
            Debug.LogWarning("Joint / StickRb / StickRoot / StickTip 引用不完整。");
            return;
        }

        edgeAnchor = new Vector3(0f, 0f, 0f);

        ActivateJointConstraint();

        plateRb.isKinematic = false;
        plateRb.useGravity = true;

        state = PlateState.HangingOnEdge;

        Debug.Log("盘子挂到杆子边缘。");
    }

    private void UpdatePlateOnStick()
    {
        float shakeAmount = GetShakeAmount();

        if (!IsStickUpright())
        {
            unstableTimer += Time.fixedDeltaTime;

            stability -= stabilityLoss * 2f * Time.fixedDeltaTime;
            spinSpeed -= spinDecay * 2f * Time.fixedDeltaTime;

            if (unstableTimer >= fallDelay)
            {
                DetachPlate();
                return;
            }
        }
        else
        {
            unstableTimer = 0f;
        }

        if (shakeAmount > 0.2f)
        {
            spinSpeed += spinAcceleration * shakeAmount * Time.fixedDeltaTime;
            stability += stabilityGain * shakeAmount * Time.fixedDeltaTime;
        }
        else
        {
            spinSpeed -= spinDecay * Time.fixedDeltaTime;
            stability -= stabilityLoss * Time.fixedDeltaTime;
        }

        spinSpeed = Mathf.Clamp(spinSpeed, 0f, maxSpinSpeed);
        stability = Mathf.Clamp01(stability);

        UpdateJointAnchor();
        ApplySpinTorque();
        ApplyUprightAssist();

        if (stability > 0.8f)
        {
            state = PlateState.StableSpinning;
        }
        else if (spinSpeed > 100f)
        {
            state = PlateState.SpinningUp;
        }
        else
        {
            state = PlateState.HangingOnEdge;
        }
    }

    /// <summary>
    /// 更新掉落状态 - 检测盘子是否落地，触发自动复位
    /// </summary>
    private void UpdateFallingOff()
    {
        if (hasFallen)
            return;

        // 检测盘子是否掉到地面以下
        if (transform.position.y < groundThreshold)
        {
            hasFallen = true;
            fallTimer = 0f;

            Debug.Log("盘子落地。");

            if (autoReset)
            {
                Debug.Log($"将在 {resetDelay} 秒后自动复位。");
            }
        }

        // 自动复位计时
        if (hasFallen && autoReset)
        {
            fallTimer += Time.fixedDeltaTime;

            if (fallTimer >= resetDelay)
            {
                ResetPlate();
            }
        }
    }

    private float GetShakeAmount()
    {
        return debugShakeAmount;
    }

    public void SetShakeAmount(float amount)
    {
        debugShakeAmount = Mathf.Clamp01(amount);
    }

    private void UpdateJointAnchor1()
    {
        if (joint == null)
            return;

        if (joint.connectedBody == null)
            return;

        edgeAnchor = new Vector3(0f, 0f, 0f);

        joint.anchor = edgeAnchor;
        joint.connectedAnchor = stickRoot.InverseTransformPoint(stickTip.position);
    }

    private void ApplySpinTorque()
    {
        if (plateRb == null)
            return;

        Vector3 worldSpinAxis = transform.TransformDirection(localSpinAxis.normalized);

        float currentSpinDegrees =
            Vector3.Dot(plateRb.angularVelocity, worldSpinAxis) * Mathf.Rad2Deg;

        float spinError = spinSpeed - currentSpinDegrees;

        plateRb.AddTorque(
            worldSpinAxis * spinError * 0.02f,
            ForceMode.Acceleration
        );
    }

    private void ApplyUprightAssist()
    {
        if (plateRb == null)
            return;

        if (stability <= 0f)
            return;

        Vector3 plateUp = transform.forward;
        Vector3 targetUp = stickTip.up;

        Vector3 correctionAxis = Vector3.Cross(plateUp, targetUp);

        plateRb.AddTorque(
            correctionAxis * uprightTorque * stability,
            ForceMode.Acceleration
        );
    }

    public bool IsStickUpright()
    {
        if (stickTip == null)
            return false;

        float upright = Vector3.Dot(stickTip.up, Vector3.up);
        return upright >= minStickUprightDot;
    }

    public void DetachPlate()
    {
        Debug.Log("盘子掉落。");

        DeactivateJointConstraint();

        if (plateRb != null)
        {
            plateRb.isKinematic = false;
            plateRb.useGravity = true;
        }

        state = PlateState.FallingOff;
    }

    /// <summary>
    /// 复位盘子 - 将盘子恢复到初始隐藏状态
    /// </summary>
    public void ResetPlate()
    {
        Debug.Log("复位盘子。");

        HidePlateAtStart();
    }

    // ========== Inspector 右键菜单 ==========

    [ContextMenu("生成盘子 (Space)")]
    private void DebugSpawnPlateAboveStick()
    {
        SpawnPlateAboveStick();
    }

    [ContextMenu("分离盘子")]
    private void DebugDetachPlate1()
    {
        DetachPlate();
    }

    [ContextMenu("复位盘子 (R)")]
    private void DebugResetPlate()
    {
        ResetPlate();
    }
}