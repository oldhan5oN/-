using UnityEngine;

public class JointBasedHeadBalance : MonoBehaviour
{
    [Header("基本设置")]
    [Tooltip("头部的Transform")]
    public Transform headTransform;
    [Tooltip("头部半径")]
    public float headRadius = 0.15f;
    [Tooltip("缸对象")]
    public GameObject ball;
    [Tooltip("缸半径")]
    public float ballRadius = 0.5f;
    [Tooltip("显示调试信息")]
    public bool showDebug = true;

    [Header("关节设置")]
    [Tooltip("关节弹性系数 - 越大越有弹性")]
    public float jointElasticity = 50f;
    [Tooltip("关节阻尼系数 - 越大越稳定")]
    public float jointDamping = 5f;
    [Tooltip("最大关节力限制")]
    public float maxJointForce = 100f;

    [Header("惯性设置")]
    [Tooltip("水平惯性因子 - 越大惯性越大")]
    public float horizontalInertia = 0.7f;
    [Tooltip("垂直惯性因子 - 越大惯性越大")]
    public float verticalInertia = 0.3f;

    [Header("顶起设置")]
    [Tooltip("顶起力系数")]
    public float bounceForce = 15f;
    [Tooltip("触发顶起的最小头部向上速度")]
    public float minUpwardVelocity = 0.5f;

    private Rigidbody ballRb;
    private bool isBallOnHead = false;
    private bool isJointActive = false;
    private Vector3 lastHeadPosition;
    private Vector3 headVelocity;
    private Vector3 jointOffset; // 关节相对于头部的偏移

    private void Start()
    {
        Debug.Log("JointBasedHeadBalance: Script starting...");
        
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.LogWarning("HeadTransform not assigned, using own transform");
        }

        lastHeadPosition = headTransform.position;

        if (ball != null)
        {
            SetBall(ball);
            Debug.Log("JointBasedHeadBalance: Ball assigned successfully");
        }
        else
        {
            Debug.LogWarning("JointBasedHeadBalance: Ball not assigned, script disabled");
            enabled = false;
        }
        
        Debug.Log($"JointBasedHeadBalance: showDebug = {showDebug}");
    }

    private void FixedUpdate()
    {
        if (ball != null && ballRb != null)
        {
            UpdateHeadVelocity();
            CheckBallHeadContact();
            ApplyJointPhysics();
        }
        else
        {
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"JointBasedHeadBalance: FixedUpdate - ball={ball != null}, ballRb={ballRb != null}");
            }
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
        Vector3 ballBottom = ball.transform.position - ball.transform.up * ballRadius;

        float verticalDistance = headTop.y - ballBottom.y;
        float horizontalDistance = Vector3.Distance(
            new Vector3(headTransform.position.x, 0, headTransform.position.z),
            new Vector3(ball.transform.position.x, 0, ball.transform.position.z)
        );

        bool isVerticallyClose = verticalDistance > -0.1f && verticalDistance < 0.3f;
        bool isHorizontallyClose = horizontalDistance < (headRadius + ballRadius * 1.2f);

        bool wasOnHead = isBallOnHead;
        isBallOnHead = isVerticallyClose && isHorizontallyClose;

        // 当缸首次接触头部时，记录关节偏移
        if (isBallOnHead && !wasOnHead)
        {
            ActivateJoint();
        }
        // 当缸离开头部时，解除关节
        else if (!isBallOnHead && wasOnHead)
        {
            DeactivateJoint();
        }

        if (Time.frameCount % 60 == 0 && showDebug)
        {
            Debug.Log($"Contact: vertical={verticalDistance:F2}, horizontal={horizontalDistance:F2}, onHead={isBallOnHead}, jointActive={isJointActive}");
        }
    }

    private void ActivateJoint()
    {
        isJointActive = true;
        
        // 计算关节偏移（缸底相对于头顶的偏移）
        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        Vector3 ballBottom = ball.transform.position - Vector3.up * ballRadius;
        
        // 关节偏移应该是缸底到头顶的向量
        jointOffset = Vector3.up * (ballRadius + headRadius + 0.05f); // 缸底在头顶上方
        
        Debug.Log("Joint activated with offset: " + jointOffset);
    }

    private void DeactivateJoint()
    {
        isJointActive = false;
        Debug.Log("Joint deactivated");
    }

    private void ApplyJointPhysics()
    {
        if (!isBallOnHead || !isJointActive)
        {
            return;
        }

        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        
        // 更可靠的缸底部计算：使用世界坐标系下的向下方向
        Vector3 ballBottom = ball.transform.position - ball.transform.up * ballRadius;
        
        // 计算目标位置：缸底应该在头顶上方很小距离
        Vector3 targetBallBottom = headTop + Vector3.up * 0.02f; // 缸底在头顶上方0.02米
        Vector3 targetPosition = targetBallBottom + ball.transform.up * ballRadius; // 缸中心位置
        
        // 计算位置误差
        Vector3 positionError = targetPosition - ball.transform.position;
        
        // 计算关节力（类似弹簧力）
        Vector3 jointForce = positionError * jointElasticity;
        
        // 限制最大力
        if (jointForce.magnitude > maxJointForce)
        {
            jointForce = jointForce.normalized * maxJointForce;
        }
        
        // 应用关节力（使用 Acceleration 模式，更直接）
        ballRb.AddForce(jointForce, ForceMode.Acceleration);
        
        // 应用阻尼力（减少振荡）
        Vector3 dampingForce = -ballRb.linearVelocity * jointDamping;
        ballRb.AddForce(dampingForce, ForceMode.Force);
        
        // 额外：直接对抗重力
        Vector3 antiGravityForce = Vector3.up * ballRb.mass * 9.81f;
        ballRb.AddForce(antiGravityForce, ForceMode.Force);
        
        // 调试信息
        if (Time.frameCount % 120 == 0 && showDebug)
        {
            Debug.Log($"Joint Physics: headTop={headTop.y:F2}, ballBottom={ballBottom.y:F2}, error={positionError.magnitude:F2}, force={jointForce.magnitude:F2}");
        }
        
        // 检查是否需要解除关节（顶起）
        if (headVelocity.y > minUpwardVelocity)
        {
            DeactivateJoint();
            
            // 施加顶起力
            float bounceMagnitude = bounceForce * headVelocity.y;
            Vector3 bounce = Vector3.up * bounceMagnitude;
            ballRb.AddForce(bounce, ForceMode.Impulse);
            
            Debug.Log("Bounce! Force: " + bounceMagnitude);
        }
    }

    // 外部设置缸的方法
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
                    ballRb.constraints = RigidbodyConstraints.None; // 允许自由转动
                    ballRb.mass = 2f; // 增加质量，更真实
                    ballRb.useGravity = true;
                    ballRb.linearDamping = 0.1f; // 空气阻力
                    ballRb.angularDamping = 0.5f; // 旋转阻力
                }
                else
                {
                    ballRb.constraints = RigidbodyConstraints.None; // 允许自由转动
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
                isJointActive = false;
                Debug.Log("Ball cleared");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || headTransform == null)
        {
            return;
        }

        // 绘制头部
        Gizmos.color = Color.green;
        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        Gizmos.DrawWireSphere(headTransform.position, headRadius);
        Gizmos.DrawLine(headTransform.position, headTop);

        // 绘制缸
        if (ball != null)
        {
            Gizmos.color = isJointActive ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(ball.transform.position, ballRadius);
            
            // 绘制关节连接线
            if (isJointActive)
            {
                Gizmos.color = Color.yellow;
                Vector3 jointTarget = headTop + jointOffset;
                Gizmos.DrawLine(headTop, jointTarget);
                Gizmos.DrawWireSphere(jointTarget, 0.05f);
            }
        }
    }
}
