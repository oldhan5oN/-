using UnityEngine;

/// <summary>
/// 顶缸系统 - 实现杂技演员顶球效果
/// 功能：球接触头顶时自动稳定，头部向上移动时顶起球
/// </summary>
public class AcrobatHeadBalance : MonoBehaviour
{
    [Header("头部设置")]
    [Tooltip("头部Transform")]
    public Transform headTransform;
    [Tooltip("头部半径")]
    public float headRadius = 0.3f;

    [Header("球设置")]
    [Tooltip("球对象")]
    public GameObject ball;
    [Tooltip("球半径")]
    public float ballRadius = 0.5f;

    [Header("物理设置")]
    [Tooltip("磁吸力度 - 越大越容易顶住")]
    public float magneticForce = 50f;
    [Tooltip("顶起力度")]
    public float bounceForce = 15f;
    [Tooltip("头部向上速度阈值")]
    public float minUpwardVelocity = 0.5f;

    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebug = true;

    private Rigidbody ballRb;
    private bool isBallOnHead = false;
    private Vector3 lastHeadPosition;
    private Vector3 headVelocity;

    public void SetBall(GameObject newBall)
    {
        if (ball != newBall)
        {
            ball = newBall;
            if (ball != null)
            {
                ballRb = ball.GetComponent<Rigidbody>();
                if (ballRb == null)
                {
                    ballRb = ball.AddComponent<Rigidbody>();
                    ballRb.constraints = RigidbodyConstraints.None; // 解除旋转锁定
                    ballRb.mass = 2f; // 增加质量，更真实
                    ballRb.useGravity = true;
                    ballRb.linearDamping = 0.1f; // 空气阻力
                    ballRb.angularDamping = 0.5f; // 旋转阻力
                }
                else
                {
                    ballRb.constraints = RigidbodyConstraints.None; // 解除旋转锁定
                    ballRb.mass = 2f; // 增加质量，更真实
                    ballRb.useGravity = true;
                    ballRb.linearDamping = 0.1f; // 空气阻力
                    ballRb.angularDamping = 0.5f; // 旋转阻力
                }
                Debug.Log("Ball updated: " + ball.name);
            }
            else
            {
                ballRb = null;
                isBallOnHead = false;
                Debug.Log("Ball cleared");
            }
        }
    }

    void Start()
    {
        Debug.Log("AcrobatHeadBalance Start() called");
        
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.Log("headTransform set to self: " + transform.name);
        }
        else
        {
            Debug.Log("headTransform assigned: " + headTransform.name);
        }

        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                ballRb = ball.AddComponent<Rigidbody>();
                Debug.Log("Added Rigidbody to ball");
            }
            else
            {
                Debug.Log("Ball has Rigidbody");
            }
        }
        else
        {
            Debug.Log("ball is null, will be set later");
        }

        lastHeadPosition = headTransform.position;
        headVelocity = Vector3.zero;
        Debug.Log("Initial position: " + headTransform.position);
    }

    void FixedUpdate()
    {
        if (headTransform == null)
        {
            return;
        }

        if (ball != null && ballRb == null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                ballRb = ball.AddComponent<Rigidbody>();
                ballRb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("Added Rigidbody to ball in FixedUpdate");
            }
            else
            {
                ballRb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("Ball has Rigidbody, initialized in FixedUpdate");
            }
        }

        if (ball != null && ballRb != null)
        {
            UpdateHeadVelocity();
            CheckBallHeadContact();
            ApplyPhysics();
        }
    }

    private void UpdateHeadVelocity()
    {
        Vector3 currentHeadPosition = headTransform.position;
        headVelocity = (currentHeadPosition - lastHeadPosition) / Time.fixedDeltaTime;
        lastHeadPosition = currentHeadPosition;
    }

    private void CheckBallHeadContact()
    {
        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        Vector3 ballBottom = ball.transform.position - Vector3.up * ballRadius;

        float verticalDistance = headTop.y - ballBottom.y;
        float horizontalDistance = Vector3.Distance(
            new Vector3(headTransform.position.x, 0, headTransform.position.z),
            new Vector3(ball.transform.position.x, 0, ball.transform.position.z)
        );

        bool isVerticallyClose = verticalDistance > -0.5f && verticalDistance < 1.0f;
        bool isHorizontallyClose = horizontalDistance < (headRadius + ballRadius * 2.0f);

        isBallOnHead = isVerticallyClose && isHorizontallyClose;

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Contact Check: headTop={headTop.y:F2}, ballBottom={ballBottom.y:F2}, verticalDistance={verticalDistance:F2}, horizontalDistance={horizontalDistance:F2}, isBallOnHead={isBallOnHead}");
        }
    }

    [Header("速度控制设置")]
    [Tooltip("速度跟随因子 - 越大越跟手")]
    public float velocityFollowFactor = 8f;
    [Tooltip("最小速度阈值 - 确保微小移动也能响应")]
    public float minVelocity = 0.1f;
    [Tooltip("最大速度限制")]
    public float maxVelocity = 2f;
    [Tooltip("速度阻尼 - 消耗多余速度")]
    public float velocityDamping = 0.6f;
    [Tooltip("平滑因子 - 越大过渡越快")]
    public float smoothFactor = 8f;
    [Tooltip("水平惯性因子 - 越大惯性越大")]
    public float horizontalInertiaFactor = 0.8f;

    [Header("定向设置")]
    [Tooltip("是否保持缸的方向稳定")]
    public bool stabilizeOrientation = true;
    [Tooltip("方向稳定系数")]
    public float orientationStability = 5f;
    [Tooltip("方向阻尼 - 减少抖动")]
    public float orientationDamping = 0.5f;

    private void ApplyPhysics()
    {
        if (!isBallOnHead)
        {
            return;
        }

        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        Vector3 targetPosition = headTop + Vector3.up * (ballRadius + 0.05f);

        // 计算目标速度（基于位置差）
        Vector3 positionError = targetPosition - ball.transform.position;
        
        // 分离水平和垂直分量
        Vector3 horizontalError = new Vector3(positionError.x, 0, positionError.z);
        Vector3 verticalError = new Vector3(0, positionError.y, 0);
        
        // 水平方向：添加惯性效果
        Vector3 horizontalTargetVelocity = horizontalError * velocityFollowFactor;
        
        // 垂直方向：保持稳定
        Vector3 verticalTargetVelocity = verticalError * (velocityFollowFactor * 1.5f); // 垂直方向响应更快
        
        // 组合目标速度
        Vector3 targetVelocity = horizontalTargetVelocity + verticalTargetVelocity;
        
        // 确保微小移动也能响应
        if (targetVelocity.magnitude < minVelocity && positionError.magnitude > 0.01f)
        {
            targetVelocity = positionError.normalized * minVelocity;
        }
        
        // 限制最大速度
        if (targetVelocity.magnitude > maxVelocity)
        {
            targetVelocity = targetVelocity.normalized * maxVelocity;
        }

        // 速度平滑过渡（使用更小的平滑因子，增加惯性感）
        Vector3 currentVelocity = ballRb.linearVelocity;
        
        // 水平方向添加惯性
        Vector3 horizontalCurrentVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        Vector3 horizontalNewVelocity = Vector3.Lerp(horizontalCurrentVelocity, horizontalTargetVelocity, Time.fixedDeltaTime * smoothFactor * horizontalInertiaFactor);
        
        // 垂直方向保持稳定
        Vector3 verticalCurrentVelocity = new Vector3(0, currentVelocity.y, 0);
        Vector3 verticalNewVelocity = Vector3.Lerp(verticalCurrentVelocity, verticalTargetVelocity, Time.fixedDeltaTime * smoothFactor * 1.5f);
        
        // 组合新速度
        Vector3 newVelocity = horizontalNewVelocity + verticalNewVelocity;
        
        // 应用速度阻尼
        newVelocity *= (1f - velocityDamping * Time.fixedDeltaTime);
        
        // 直接设置速度（消除弹性感）
        ballRb.linearVelocity = newVelocity;

        // 方向稳定 - 保持缸的方向，确保接触点稳定
        if (stabilizeOrientation)
        {
            // 计算目标方向（保持缸的底部朝向头部）
            Quaternion targetRotation = Quaternion.LookRotation(headTransform.forward, Vector3.up);
            
            // 应用旋转力使缸保持稳定方向
            Quaternion rotationError = targetRotation * Quaternion.Inverse(ball.transform.rotation);
            Vector3 rotationAxis;
            float rotationAngle;
            rotationError.ToAngleAxis(out rotationAngle, out rotationAxis);
            
            if (rotationAngle > 0.1f)
            {
                // 减少旋转力并添加阻尼，减少抖动
                Vector3 torque = rotationAxis * rotationAngle * orientationStability * ballRb.mass;
                torque *= (1f - orientationDamping * Time.fixedDeltaTime);
                ballRb.AddTorque(torque, ForceMode.Force);
            }
        }

        // 保留头部向上移动时的顶起力
        if (headVelocity.y > minUpwardVelocity)
        {
            float bounceMagnitude = bounceForce * headVelocity.y;
            Vector3 bounce = Vector3.up * bounceMagnitude;
            ballRb.AddForce(bounce, ForceMode.Impulse);
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || headTransform == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        Gizmos.DrawWireSphere(headTransform.position, headRadius);
        Gizmos.DrawLine(headTransform.position, headTop);

        if (ball != null)
        {
            Gizmos.color = isBallOnHead ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(ball.transform.position, ballRadius);
        }
    }
}