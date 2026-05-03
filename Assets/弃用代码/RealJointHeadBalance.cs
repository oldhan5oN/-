using UnityEngine;

public class RealJointHeadBalance : MonoBehaviour
{
    [Header("基本设置")]
    public Transform headTransform;
    public Rigidbody headRigidbody; // 头部的Rigidbody（Kinematic）
    public float headRadius = 0.15f;
    public GameObject ball;
    public float ballRadius = 0.5f;
    public bool showDebug = true;

    [Header("关节设置")]
    public float jointBreakForce = 500f; // 关节断裂力（更大，更稳定）
    public float jointBreakTorque = 200f; // 关节断裂扭矩（更大，更稳定）
    public Vector3 jointAnchor = Vector3.zero; // 关节锚点

    [Header("顶起设置")]
    public float minUpwardVelocity = 0.5f; // 触发顶起的最小向上速度

    private FixedJoint currentJoint;
    private Rigidbody ballRb;
    private bool isBallOnHead = false;
    private Vector3 lastHeadPosition;
    private Vector3 headVelocity;
    private Vector3 contactPoint; // 存储实际接触点
    private Vector3 collisionPoint; // 存储物理碰撞点
    private bool hasValidCollision = false; // 是否有有效的碰撞

    private void Start()
    {
        Debug.Log("RealJointHeadBalance: Script starting...");

        if (headTransform == null)
        {
            headTransform = transform;
            Debug.LogWarning("HeadTransform not assigned, using own transform");
        }

        if (headRigidbody == null)
        {
            headRigidbody = headTransform.GetComponent<Rigidbody>();
            if (headRigidbody == null)
            {
                headRigidbody = headTransform.gameObject.AddComponent<Rigidbody>();
                headRigidbody.isKinematic = true;
                headRigidbody.useGravity = false;
                Debug.Log("RealJointHeadBalance: Created Kinematic Rigidbody for head");
            }
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
            CheckJointBreakCondition();
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
        // 只使用物理碰撞检测
        if (hasValidCollision)
        {
            contactPoint = collisionPoint;
            isBallOnHead = true;
        }
        else
        {
            isBallOnHead = false;
        }

        if (Time.frameCount % 60 == 0 && showDebug)
        {
            Debug.Log($"Contact: usingPhysics={hasValidCollision}, onHead={isBallOnHead}, hasJoint={currentJoint != null}");
        }
    }

    private void CreateJointAtContactPoint(Vector3 worldContactPoint)
    {
        if (currentJoint != null) return;

        // 创建FixedJoint
        currentJoint = ball.AddComponent<FixedJoint>();
        currentJoint.connectedBody = headRigidbody;
        currentJoint.breakForce = jointBreakForce;
        currentJoint.breakTorque = jointBreakTorque;
        
        // 关闭自动配置，手动设置关节锚点
        currentJoint.autoConfigureConnectedAnchor = false;
        
        // 将世界坐标的接触点转换为球的局部坐标
        Vector3 localAnchor = ball.transform.InverseTransformPoint(worldContactPoint);
        currentJoint.anchor = localAnchor;
        
        // 计算头部连接点（在头部局部坐标系中）
        Vector3 headTop = headTransform.position + Vector3.up * headRadius;
        Vector3 localHeadTop = headRigidbody.transform.InverseTransformPoint(headTop);
        currentJoint.connectedAnchor = localHeadTop;

        Debug.Log($"RealJointHeadBalance: Joint created at contact point! LocalAnchor={localAnchor}, WorldPoint={worldContactPoint}");
    }

    private void RemoveJoint()
    {
        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
            Debug.Log("RealJointHeadBalance: Joint removed");
        }
    }

    private void CheckJointBreakCondition()
    {
        if (currentJoint == null) return;

        // 检查头部向上速度，超过阈值时断裂关节
        if (headVelocity.y > minUpwardVelocity)
        {
            Debug.Log($"RealJointHeadBalance: Breaking joint! headVelocity={headVelocity.y:F2}");
            RemoveJoint();
            
            // 施加较小的顶起力
            Vector3 bounceForce = Vector3.up * ballRb.mass * Mathf.Max(headVelocity.y, 1f);
            ballRb.AddForce(bounceForce, ForceMode.Impulse);
        }
    }

    public void SetBall(GameObject newBall)
    {
        if (ball != newBall)
        {
            // 移除旧关节
            RemoveJoint();

            ball = newBall;
            if (ball != null)
            {
                ballRb = ball.GetComponent<Rigidbody>();
                if (ballRb == null)
                {
                    ballRb = ball.AddComponent<Rigidbody>();
                    ballRb.constraints = RigidbodyConstraints.None;
                    ballRb.mass = 2f;
                    ballRb.useGravity = true;
                    ballRb.linearDamping = 0.1f;
                    ballRb.angularDamping = 0.5f;
                }
                else
                {
                    ballRb.constraints = RigidbodyConstraints.None;
                    ballRb.mass = 2f;
                    ballRb.useGravity = true;
                    ballRb.linearDamping = 0.1f;
                    ballRb.angularDamping = 0.5f;
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

    private void OnCollisionEnter(Collision collision)
    {
        // 检查碰撞对象是否是头部
        if (collision.gameObject == headTransform.gameObject || 
            collision.transform.IsChildOf(headTransform))
        {
            // 获取第一个接触点
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                collisionPoint = contact.point;
                hasValidCollision = true;
                
                Debug.Log($"RealJointHeadBalance: Collision detected! Point={collisionPoint}");
                
                // 如果还没有关节，就在碰撞点创建关节
                if (currentJoint == null)
                {
                    CreateJointAtContactPoint(collisionPoint);
                    isBallOnHead = true;
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 持续保持碰撞状态
        if (collision.gameObject == headTransform.gameObject || 
            collision.transform.IsChildOf(headTransform))
        {
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                collisionPoint = contact.point;
                hasValidCollision = true;
                isBallOnHead = true;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 离开碰撞
        if (collision.gameObject == headTransform.gameObject || 
            collision.transform.IsChildOf(headTransform))
        {
            hasValidCollision = false;
            isBallOnHead = false;
            
            Debug.Log("RealJointHeadBalance: Collision exited");
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
        Gizmos.DrawWireSphere(headTop, 0.03f); // 头顶点标记

        // 绘制缸
        if (ball != null)
        {
            Gizmos.color = currentJoint != null ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(ball.transform.position, ballRadius);
            
            // 绘制物理碰撞点（如果有）
            if (hasValidCollision)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(collisionPoint, 0.08f); // 大圆圈
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(collisionPoint, 0.04f); // 小实球
            }
            
            // 绘制预测的接触点（如果没有物理碰撞）
            else
            {
                Vector3 ballCenter = ball.transform.position;
                Vector3 directionToHeadTop = headTop - ballCenter;
                Vector3 predictedContact = ballCenter + directionToHeadTop.normalized * ballRadius;
                
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(predictedContact, 0.05f);
                Gizmos.DrawLine(ballCenter, predictedContact);
            }
            
            // 绘制关节连接线
            if (currentJoint != null)
            {
                // 计算关节位置
                Vector3 jointPosition = ball.transform.position + ball.transform.rotation * currentJoint.anchor;
                Vector3 connectedPosition = currentJoint.connectedBody != null 
                    ? currentJoint.connectedBody.position + currentJoint.connectedBody.rotation * currentJoint.connectedAnchor 
                    : headTop;
                
                // 关节连接线
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(headTop, jointPosition);
                Gizmos.DrawLine(jointPosition, connectedPosition);
                
                // 关节锚点
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(jointPosition, 0.06f); // 球上的关节锚点
                Gizmos.DrawWireSphere(connectedPosition, 0.03f); // 头部上的连接点
            }
        }
    }
}
