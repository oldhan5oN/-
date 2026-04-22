using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BallJointController : MonoBehaviour
{
    [Header("设置")]
    public Transform headTransform; // 头部对象
    public Rigidbody headRigidbody; // 头部的Rigidbody
    public bool showDebug = true;

    [Header("关节设置")]
    public float jointBreakForce = 500f;
    public float jointBreakTorque = 200f;
    public float minUpwardVelocity = 0.5f;

    private FixedJoint currentJoint;
    private Rigidbody ballRb;
    private Vector3 lastHeadPosition;
    private Vector3 headVelocity;
    private Vector3 collisionPoint;
    private bool hasValidCollision;

    private void Start()
    {
        ballRb = GetComponent<Rigidbody>();
        if (headTransform != null)
        {
            lastHeadPosition = headTransform.position;
        }
        Debug.Log($"BallJointController: Started! ball={name}, hasHeadRigidbody={headRigidbody != null}");
    }

    private void FixedUpdate()
    {
        if (headTransform == null || headRigidbody == null) return;

        // 更新头部速度
        Vector3 currentHeadPosition = headTransform.position;
        headVelocity = (currentHeadPosition - lastHeadPosition) / Time.fixedDeltaTime;
        lastHeadPosition = currentHeadPosition;

        // 检查是否需要断开关节
        CheckJointBreakCondition();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == headTransform.gameObject || 
            collision.transform.IsChildOf(headTransform))
        {
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                collisionPoint = contact.point;
                hasValidCollision = true;
                
                Debug.Log($"BallJointController: Collision! Point={collisionPoint}");
                
                if (currentJoint == null)
                {
                    CreateJointAtContactPoint(collisionPoint);
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject == headTransform.gameObject || 
            collision.transform.IsChildOf(headTransform))
        {
            hasValidCollision = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == headTransform.gameObject || 
            collision.transform.IsChildOf(headTransform))
        {
            hasValidCollision = false;
            RemoveJoint();
            Debug.Log("BallJointController: Collision exited");
        }
    }

    private void CreateJointAtContactPoint(Vector3 worldContactPoint)
    {
        if (currentJoint != null) return;

        currentJoint = gameObject.AddComponent<FixedJoint>();
        currentJoint.connectedBody = headRigidbody;
        currentJoint.breakForce = jointBreakForce;
        currentJoint.breakTorque = jointBreakTorque;
        currentJoint.autoConfigureConnectedAnchor = false;

        Vector3 localAnchor = transform.InverseTransformPoint(worldContactPoint);
        currentJoint.anchor = localAnchor;

        Vector3 headTop = headTransform.position + Vector3.up * 0.15f;
        Vector3 localHeadTop = headRigidbody.transform.InverseTransformPoint(headTop);
        currentJoint.connectedAnchor = localHeadTop;

        Debug.Log($"BallJointController: Joint created! Anchor={localAnchor}");
    }

    private void RemoveJoint()
    {
        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
            Debug.Log("BallJointController: Joint removed");
        }
    }

    private void CheckJointBreakCondition()
    {
        if (currentJoint == null) return;

        if (headVelocity.y > minUpwardVelocity)
        {
            Debug.Log($"BallJointController: Breaking joint! Velocity={headVelocity.y:F2}");
            RemoveJoint();
            Vector3 bounceForce = Vector3.up * ballRb.mass * Mathf.Max(headVelocity.y, 1f);
            ballRb.AddForce(bounceForce, ForceMode.Impulse);
        }
    }

    private void OnJointBreak(float breakForce)
    {
        Debug.Log($"BallJointController: Joint broke! Force={breakForce}");
        currentJoint = null;
    }

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        if (currentJoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            Vector3 jointPos = transform.position + transform.rotation * currentJoint.anchor;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(jointPos, 0.06f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        if (hasValidCollision)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(collisionPoint, 0.08f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(collisionPoint, 0.04f);
        }
    }
}
