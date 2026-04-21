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
                    ballRb.constraints = RigidbodyConstraints.FreezeRotation;
                    ballRb.mass = 0.5f;
                    ballRb.useGravity = true;
                }
                else
                {
                    ballRb.constraints = RigidbodyConstraints.FreezeRotation;
                    ballRb.mass = 0.5f;
                    ballRb.useGravity = true;
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
    public float velocityFollowFactor = 15f;
    [Tooltip("最小速度阈值 - 确保微小移动也能响应")]
    public float minVelocity = 0.2f;
    [Tooltip("最大速度限制")]
    public float maxVelocity = 3f;
    [Tooltip("速度阻尼 - 消耗多余速度")]
    public float velocityDamping = 0.3f;
    [Tooltip("平滑因子 - 越大过渡越快")]
    public float smoothFactor = 15f;

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
        Vector3 targetVelocity = positionError * velocityFollowFactor;
        
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

        // 速度平滑过渡（使用更大的平滑因子）
        Vector3 currentVelocity = ballRb.linearVelocity;
        Vector3 newVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * smoothFactor);
        
        // 应用速度阻尼
        newVelocity *= (1f - velocityDamping * Time.fixedDeltaTime);
        
        // 直接设置速度（消除弹性感）
        ballRb.linearVelocity = newVelocity;

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