using UnityEngine;

public class StickGestureListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
    [Header("Player")]
    public int playerIndex = 0;

    [Header("Stick")]
    public GameObject stickRootObject;
    public Rigidbody stickRb;

    [Tooltip("棍子相对手掌的位置偏移（本地空间，例如握点）")]
    public Vector3 gripOffset = new Vector3(0f, -0.1f, 0f);

    [Tooltip("棍子模型默认朝向的本地轴。如果棍子竖着是Y轴=Y，如果前向是Z轴=Z")]
    public Vector3 stickLocalUpAxis = Vector3.up;

    [Tooltip("额外旋转微调（度），调不正的时候用")]
    public Vector3 extraRotationEuler = Vector3.zero;

    [Header("Follow")]
    public float followSpeed = 20f;
    public float rotateSpeed = 15f;

    [Header("Arm Direction")]
    [Tooltip("是否用小臂方向旋转棍子")]
    public bool followArmDirection = true;

    [Tooltip("小臂方向过短时不更新旋转，避免抖动")]
    public float minArmLength = 0.05f;

    [Header("Debug")]
    public bool hasStick = false;
    public bool useRightHand = true;

    private long trackedUserId = 0;
    private KinectInterop.JointType trackedHandJoint = KinectInterop.JointType.HandRight;
    private KinectInterop.JointType trackedElbowJoint = KinectInterop.JointType.ElbowRight;

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
        // 不需要
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
            GrabStick(userId, true);
            return true;
        }

        if (gesture == KinectGestures.Gestures.RaiseLeftHand)
        {
            GrabStick(userId, false);
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

    private void GrabStick(long userId, bool right)
    {
        trackedUserId = userId;
        useRightHand = right;

        if (right)
        {
            trackedHandJoint = KinectInterop.JointType.HandRight;
            trackedElbowJoint = KinectInterop.JointType.ElbowRight;
        }
        else
        {
            trackedHandJoint = KinectInterop.JointType.HandLeft;
            trackedElbowJoint = KinectInterop.JointType.ElbowLeft;
        }

        hasStick = true;

        if (stickRootObject != null)
            stickRootObject.SetActive(true);

        Debug.Log("棍子出现，跟随：" + (right ? "右手" : "左手"));
    }

    private void FixedUpdate()
    {
        if (!hasStick || stickRootObject == null)
            return;

        KinectManager kinectManager = KinectManager.Instance;
        if (kinectManager == null || trackedUserId == 0)
            return;

        int handIndex = (int)trackedHandJoint;
        int elbowIndex = (int)trackedElbowJoint;

        if (!kinectManager.IsJointTracked(trackedUserId, handIndex))
            return;

        Vector3 handPos = kinectManager.GetJointPosition(trackedUserId, handIndex);

        // 计算目标旋转
        Quaternion targetRot = stickRootObject.transform.rotation;

        if (followArmDirection &&
            kinectManager.IsJointTracked(trackedUserId, elbowIndex))
        {
            Vector3 elbowPos = kinectManager.GetJointPosition(trackedUserId, elbowIndex);
            Vector3 forearmDir = handPos - elbowPos;

            if (forearmDir.magnitude > minArmLength)
            {
                // 把棍子的本地轴对齐到小臂方向
                targetRot = Quaternion.FromToRotation(stickLocalUpAxis, forearmDir.normalized);
                targetRot *= Quaternion.Euler(extraRotationEuler);
            }
        }
        else
        {
            targetRot = Quaternion.Euler(extraRotationEuler);
        }

        // 握点偏移（让棍子握把正好在手上，不是棍子中心在手上）
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
}