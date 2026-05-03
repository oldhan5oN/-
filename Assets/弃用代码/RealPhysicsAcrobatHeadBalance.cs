using UnityEngine;

public class RealPhysicsAcrobatHeadBalance : MonoBehaviour
{
    [Header("基本设置")]
    [Tooltip("头部的Transform")]
    public Transform headTransform;
    [Tooltip("头部半径")]
    public float headRadius = 0.15f;
    [Tooltip("球对象")]
    public GameObject ball;
    [Tooltip("球半径")]
    public float ballRadius = 0.5f;
    [Tooltip("显示调试信息")]
    public bool showDebug = true;

    [Header("物理参数")]
    [Tooltip("头部碰撞力系数")]
    public float collisionForce = 10f;
    [Tooltip("头部摩擦力系数")]
    public float frictionCoefficient = 0.8f;
    [Tooltip("头部稳定性系数")]
    public float stabilityFactor = 5f;
    [Tooltip("最大碰撞力限制")]
    public float maxCollisionForce = 20f;
    [Tooltip("头部向上移动时的顶起力")]
    public float bounceForce = 10f;
    [Tooltip("触发顶起的最小头部向上速度")]
    public float minUpwardVelocity = 0.5f;

    private Rigidbody ballRb;
    private bool isBallOnHead = false;
    private Vector3 lastHeadPosition;
    private Vector3 headVelocity;

    private void Start()
    {
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.LogWarning("HeadTransform not assigned, using own transform");
        }

        lastHeadPosition = headTransform.position;

        if (ball != null)
        {
            SetBall(ball);
        }
        else
        {
            Debug.LogWarning("Ball not assigned");
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
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

        if (Time.frameCount % 60 == 0 && showDebug)
        {
            Debug.Log($"Contact Check: headTop={headTop.y:F2}, ballBottom={ballBottom.y:F2}, verticalDistance={verticalDistance:F2}, horizontalDistance={horizontalDistance:F2}, isBallOnHead={isBallOnHead}");
        }
    }

    private void ApplyPhysics()
    {
        if (!isBallOnHead)
        {
            return;
        }

        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        
        // 计算头部与球的相对位置
        Vector3 headToBall = ball.transform.position - headTop;
        float distance = headToBall.magnitude;
        
        // 计算碰撞法线（从头部指向球）
        Vector3 normal = headToBall.normalized;
        
        // 1. 碰撞力 - 基于距离
        float forceMagnitude = Mathf.Clamp(collisionForce / (distance + 0.1f), 0, maxCollisionForce);
        Vector3 collisionForceVector = normal * forceMagnitude;
        ballRb.AddForce(collisionForceVector, ForceMode.Force);
        
        // 2. 摩擦力 - 防止球滑动
        Vector3 relativeVelocity = ballRb.linearVelocity - headVelocity;
        Vector3 frictionForce = -relativeVelocity * frictionCoefficient * ballRb.mass;
        ballRb.AddForce(frictionForce, ForceMode.Force);
        
        // 3. 稳定性力 - 保持球在头顶中心
        Vector3 horizontalError = new Vector3(headToBall.x, 0, headToBall.z);
        if (horizontalError.magnitude > 0.1f)
        {
            Vector3 stabilityForce = -horizontalError * stabilityFactor;
            ballRb.AddForce(stabilityForce, ForceMode.Force);
        }
        
        // 4. 头部向上移动时的顶起力
        if (headVelocity.y > minUpwardVelocity)
        {
            float bounceMagnitude = bounceForce * headVelocity.y;
            Vector3 bounce = Vector3.up * bounceMagnitude;
            ballRb.AddForce(bounce, ForceMode.Impulse);
        }
    }

    // 外部设置球的方法
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

        // 绘制球
        if (ball != null)
        {
            Gizmos.color = isBallOnHead ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(ball.transform.position, ballRadius);
        }
    }
}
