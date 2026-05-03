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

    [Header("Debug")]
    [Range(0f, 1f)]
    public float debugShakeAmount = 0f;

    public PlateState state = PlateState.Hidden;

    private float unstableTimer;
    private Vector3 edgeAnchor;
    private Vector3 centerAnchor;

    /// <summary>
    /// 初始化方法 - 获取组件引用，设置锚点，初始化关节参数
    /// </summary>
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

    /// <summary>
    /// 物理更新方法 - 根据当前状态执行相应的更新逻辑
    /// </summary>
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
                break;
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

        // 限制 Y 轴旋转，保持盘子稳定
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
            // 先设置速度，再设置 Kinematic
            plateRb.linearVelocity = Vector3.zero;
            plateRb.angularVelocity = Vector3.zero;
            plateRb.useGravity = false;
            plateRb.isKinematic = true;
        }

        gameObject.SetActive(false);
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

        state = PlateState.FallingOntoStick;

        Debug.Log("盘子从上方掉下。");
    }

    public void UpdateFallingOntoStick()
    {
        if (!IsStickUpright())
            return;

        // 盘子只在 X、Z 方向跟随杆子，Y 方向保持不变（让它自然下落）
        transform.position = new Vector3(stickTip.position.x, transform.position.y, stickTip.position.z);
        Debug.Log("掉落");
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

    /// <summary>
    /// 更新盘子在杆子上的状态 - 处理抖动检测、旋转加速、稳定性计算
    /// </summary>
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
                Debug.Log("盘子掉下去喽");
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
    /// 获取抖动量 - 返回当前的抖动检测值（调试用）
    /// </summary>
    /// <returns>抖动量（0-1）</returns>
    private float GetShakeAmount()
    {
        return debugShakeAmount;
    }

    /// <summary>
    /// 设置抖动量 - 外部调用接口，用于传递手部抖动信号
    /// </summary>
    /// <param name="amount">抖动强度（0-1）</param>
    public void SetShakeAmount(float amount)
    {
        debugShakeAmount = Mathf.Clamp01(amount);
    }

    /// <summary>
    /// 更新关节锚点位置 
    /// </summary>
    private void UpdateJointAnchor()
    {
        if (joint == null)
            return;

        if (joint.connectedBody == null)
            return;

        edgeAnchor = new Vector3(0f, 0f, 0f);

        joint.anchor = edgeAnchor;
        joint.connectedAnchor = stickRoot.InverseTransformPoint(stickTip.position);
    }

    /// <summary>
    /// 应用旋转力矩 - 根据目标转速与当前转速的差值施加扭矩
    /// </summary>
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

    /// <summary>
    /// 应用扶正辅助力矩 - 根据稳定性将盘子朝向杆子竖直方向修正
    /// </summary>
    private void ApplyUprightAssist()//扶正盘子
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

    /// <summary>
    /// 检测杆子是否竖直 - 通过点积计算杆子与垂直方向的夹角
    /// </summary>
    /// <returns>杆子是否足够竖直</returns>
    public bool IsStickUpright()
    {
        if (stickTip == null)
            return false;

        float upright = Vector3.Dot(stickTip.up, Vector3.up);
        return upright >= minStickUprightDot;
    }

    /// <summary>
    /// 分离盘子 - 断开关节连接，让盘子自由掉落
    /// </summary>
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
    /// 调试方法：从菜单生成盘子 - 在 Inspector 右键菜单中调用
    /// </summary>
    [ContextMenu("Debug Spawn Plate Above Stick")]
    private void DebugSpawnPlateAboveStick()
    {
        SpawnPlateAboveStick();
    }

    /// <summary>
    /// 调试方法：分离盘子 - 在 Inspector 右键菜单中调用
    /// </summary>
    [ContextMenu("Debug Detach Plate")]
    private void DebugDetachPlate()
    {
        DetachPlate();
    }
}
