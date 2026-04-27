using UnityEngine;
using UnityEngine.Events;

public class DaPoseDetector : MonoBehaviour
{
    [Header("触发事件")]
    public UnityEvent onDaPoseDetected;

    [Header("姿势参数")]
    public float holdTime = 0.5f;          // 保持多久才算成功
    public float cooldownTime = 1.5f;      // 触发后冷却，防止一直触发

    [Header("手臂判断")]
    public float handOpenMultiplier = 2.2f; // 双手距离 > 肩宽 * 这个值
    public float handHeightTolerance = 0.35f; // 手和肩膀高度允许误差

    [Header("腿部判断")]
    public float legOpenMultiplier = 1.2f; // 双脚距离 > 肩宽 * 这个值

    private float poseTimer = 0f;
    private float cooldownTimer = 0f;
    private bool hasTriggered = false;

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        KinectManager manager = KinectManager.Instance;

        if (manager == null || !manager.IsInitialized())
            return;

        long userId = manager.GetPrimaryUserID();

        if (userId == 0)
            return;

        bool isDaPose = CheckDaPose(manager, userId);

        if (isDaPose)
        {
            poseTimer += Time.deltaTime;

            if (poseTimer >= holdTime && !hasTriggered)
            {
                hasTriggered = true;
                cooldownTimer = cooldownTime;

                Debug.Log("检测到“大”字姿势！");
                onDaPoseDetected.Invoke();
            }
        }
        else
        {
            poseTimer = 0f;
            hasTriggered = false;
        }
    }

    bool CheckDaPose(KinectManager manager, long userId)
    {
        int head = (int)KinectInterop.JointType.Head;

        int shoulderLeft = (int)KinectInterop.JointType.ShoulderLeft;
        int shoulderRight = (int)KinectInterop.JointType.ShoulderRight;

        int handLeft = (int)KinectInterop.JointType.HandLeft;
        int handRight = (int)KinectInterop.JointType.HandRight;

        int footLeft = (int)KinectInterop.JointType.FootLeft;
        int footRight = (int)KinectInterop.JointType.FootRight;

        if (!manager.IsJointTracked(userId, shoulderLeft) ||
            !manager.IsJointTracked(userId, shoulderRight) ||
            !manager.IsJointTracked(userId, handLeft) ||
            !manager.IsJointTracked(userId, handRight) ||
            !manager.IsJointTracked(userId, footLeft) ||
            !manager.IsJointTracked(userId, footRight))
        {
            return false;
        }

        Vector3 leftShoulder = manager.GetJointPosition(userId, shoulderLeft);
        Vector3 rightShoulder = manager.GetJointPosition(userId, shoulderRight);

        Vector3 leftHand = manager.GetJointPosition(userId, handLeft);
        Vector3 rightHand = manager.GetJointPosition(userId, handRight);

        Vector3 leftFoot = manager.GetJointPosition(userId, footLeft);
        Vector3 rightFoot = manager.GetJointPosition(userId, footRight);

        float shoulderWidth = Mathf.Abs(leftShoulder.x - rightShoulder.x);
        float handWidth = Mathf.Abs(leftHand.x - rightHand.x);
        float footWidth = Mathf.Abs(leftFoot.x - rightFoot.x);

        float shoulderHeight = (leftShoulder.y + rightShoulder.y) * 0.5f;
        float leftHandHeightDiff = Mathf.Abs(leftHand.y - shoulderHeight);
        float rightHandHeightDiff = Mathf.Abs(rightHand.y - shoulderHeight);

        bool handsOpen = handWidth > shoulderWidth * handOpenMultiplier;

        bool handsNearShoulderHeight =
            leftHandHeightDiff < handHeightTolerance &&
            rightHandHeightDiff < handHeightTolerance;

        bool legsOpen = footWidth > shoulderWidth * legOpenMultiplier;

        // 左手应该在左肩左边，右手应该在右肩右边
        bool handsOnCorrectSide =
            leftHand.x < leftShoulder.x &&
            rightHand.x > rightShoulder.x;

        return handsOpen && handsNearShoulderHeight && legsOpen && handsOnCorrectSide;
    }
}