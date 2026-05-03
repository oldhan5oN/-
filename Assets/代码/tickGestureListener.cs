using UnityEngine;

public class StickGestureListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
    [Header("Player")]
    public int playerIndex = 0;

    [Header("Avatar Bones")]
    [Tooltip("Avatar 的右手骨骼（从 Hierarchy 里拖）")]
    public Transform rightHandBone;

    [Tooltip("Avatar 的左手骨骼（从 Hierarchy 里拖）")]
    public Transform leftHandBone;

    [Tooltip("Avatar 的右手肘骨骼（用于计算小臂方向）")]
    public Transform rightElbowBone;

    [Tooltip("Avatar 的左手肘骨骼")]
    public Transform leftElbowBone;

    [Header("Stick")]
    public GameObject stickRootObject;
    public Rigidbody stickRb;

    [Tooltip("棍子相对手掌的位置偏移")]
    public Vector3 gripOffset = new Vector3(0f, -0.1f, 0f);

    [Tooltip("棍子模型默认朝向的本地轴")]
    public Vector3 stickLocalUpAxis = Vector3.up;

    [Tooltip("额外旋转微调")]
    public Vector3 extraRotationEuler = Vector3.zero;

    [Header("Follow")]
    public float followSpeed = 20f;
    public float rotateSpeed = 15f;

    [Header("Arm Direction")]
    [Tooltip("是否用小臂方向旋转棍子")]
    public bool followArmDirection = true;

    [Tooltip("小臂方向过短时不更新旋转")]
    public float minArmLength = 0.05f;

    [Header("Debug")]
    public bool hasStick = false;
    public bool useRightHand = true;

    private long trackedUserId = 0;
    private Transform currentHandBone;
    private Transform currentElbowBone;

    private void Start()
    {
        if (stickRootObject != null)
            stickRootObject.SetActive(false);

        if (stickRb == null && stickRootObject != null)
            stickRb = stickRootObject.GetComponent<Rigidbody>();
    }

    public void UserDetected(long userId, int userIndex)
    {
        if (userIndex != playerIndex)
            return;

        trackedUserId = userId;

        KinectManager kinectManager = KinectManager.Instance;
        if (kinectManager != null)
        {
            kinectManager.DetectGesture(userId, KinectGestures.Gestures.RaiseRightHand);
            kinectManager.DetectGesture(userId, KinectGestures.Gestures.RaiseLeftHand);
        }

        Debug.Log("玩家进入，开始监听举手。");
    }

    public void UserLost(long userId, int userIndex)
    {
        if (userIndex != playerIndex)
            return;

        trackedUserId = 0;
        hasStick = false;

        if (stickRootObject != null)
            stickRootObject.SetActive(false);

        Debug.Log("玩家丢失，棍子收回。");
    }

    public void GestureInProgress(
        long userId, int userIndex,
        KinectGestures.Gestures gesture,
        float progress,
        KinectInterop.JointType joint,
        Vector3 screenPos)
    {
    }

    public bool GestureCompleted(
        long userId, int userIndex,
        KinectGestures.Gestures gesture,
        KinectInterop.JointType joint,
        Vector3 screenPos)
    {
        if (userIndex != playerIndex)
            return false;

        // 已经拿到棍子了，就不再切换
        if (hasStick)
            return true;

        if (gesture == KinectGestures.Gestures.RaiseRightHand)
        {
            GrabStick(true);
            return true;
        }

        if (gesture == KinectGestures.Gestures.RaiseLeftHand)
        {
            GrabStick(false);
            return true;
        }

        return false;
    }

    public bool GestureCancelled(
        long userId, int userIndex,
        KinectGestures.Gestures gesture,
        KinectInterop.JointType joint)
    {
        return userIndex == playerIndex;
    }

    private void GrabStick(bool right)
    {
        useRightHand = right;

        if (right)
        {
            currentHandBone = rightHandBone;
            currentElbowBone = rightElbowBone;
        }
        else
        {
            currentHandBone = leftHandBone;
            currentElbowBone = leftElbowBone;
        }

        if (currentHandBone == null)
        {
            Debug.LogError("手部骨骼未设置！请在 Inspector 里拖入 Avatar 的手部 Transform。");
            return;
        }

        hasStick = true;

        if (stickRootObject != null)
            stickRootObject.SetActive(true);

        Debug.Log("棍子出现，跟随：" + (right ? "右手" : "左手"));
    }

    private void FixedUpdate()
    {
        if (!hasStick || stickRootObject == null || currentHandBone == null)
            return;

        Vector3 handPos = currentHandBone.position;

        // 计算目标旋转
        Quaternion targetRot = stickRootObject.transform.rotation;

        if (followArmDirection && currentElbowBone != null)
        {
            Vector3 elbowPos = currentElbowBone.position;
            Vector3 forearmDir = handPos - elbowPos;

            if (forearmDir.magnitude > minArmLength)
            {
                targetRot = Quaternion.FromToRotation(stickLocalUpAxis, forearmDir.normalized);
                targetRot *= Quaternion.Euler(extraRotationEuler);
            }
        }
        else
        {
            targetRot = Quaternion.Euler(extraRotationEuler);
        }

        // 握点偏移
        Vector3 worldOffset = targetRot * gripOffset;
        Vector3 targetPos = handPos - worldOffset;

        // 平滑跟随
        if (stickRb != null && stickRb.isKinematic)
        {
            Vector3 newPos = Vector3.Lerp(stickRb.position, targetPos,
                followSpeed * Time.fixedDeltaTime);
            Quaternion newRot = Quaternion.Slerp(stickRb.rotation, targetRot,
                rotateSpeed * Time.fixedDeltaTime);

            stickRb.MovePosition(newPos);
            stickRb.MoveRotation(newRot);
        }
        else
        {
            stickRootObject.transform.position = Vector3.Lerp(
                stickRootObject.transform.position, targetPos,
                followSpeed * Time.fixedDeltaTime);

            stickRootObject.transform.rotation = Quaternion.Slerp(
                stickRootObject.transform.rotation, targetRot,
                rotateSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!hasStick || currentHandBone == null)
            return;

        // 画手部位置
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentHandBone.position, 0.03f);

        // 画棍子原点
        if (stickRootObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stickRootObject.transform.position, 0.03f);

            // 画连线
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentHandBone.position, stickRootObject.transform.position);
        }

        // 画小臂方向
        if (followArmDirection && currentElbowBone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(currentElbowBone.position, currentHandBone.position);
        }
    }
}