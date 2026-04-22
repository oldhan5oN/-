using UnityEngine;

public class BallCatcher : MonoBehaviour
{
    [Header("挡板引用")]
    public Transform frontGuard;
    public Transform backGuard;
    public Transform leftGuard;
    public Transform rightGuard;

    [Header("设置参数")]
    public float openAngle = -90f; // 打开角度
    public float closedAngle = 90f; // 关闭角度
    public float rotationSpeed = 8f; // 旋转速度
    public string ballTag = "Ball"; // 球标签

    [Header("状态")]
    public bool isCatching; // 是否在接球

    // 目标旋转
    private Quaternion frontTargetRot;
    private Quaternion backTargetRot;
    private Quaternion leftTargetRot;
    private Quaternion rightTargetRot;

    private void Start()
    {
        // 初始化打开状态
        Debug.Log("111");
        // 初始化打开状态
        OpenGuards();
        Debug.Log("BallCatcher: 启动！挡板已打开");
    }

    private void Update()
    {
        LerpGuards();
    }

    private void LerpGuards()
    {
        if (frontGuard != null)
        {
            frontGuard.rotation = Quaternion.Lerp(frontGuard.rotation, frontTargetRot, rotationSpeed * Time.deltaTime);
        }
        if (backGuard != null)
        {
            backGuard.rotation = Quaternion.Lerp(backGuard.rotation, backTargetRot, rotationSpeed * Time.deltaTime);
        }
        if (leftGuard != null)
        {
            leftGuard.rotation = Quaternion.Lerp(leftGuard.rotation, leftTargetRot, rotationSpeed * Time.deltaTime);
        }
        if (rightGuard != null)
        {
            rightGuard.rotation = Quaternion.Lerp(rightGuard.rotation, rightTargetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(ballTag))
        {
            isCatching = true;
            CloseGuards();
            Debug.Log("BallCatcher: 球进入！关闭挡板");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(ballTag))
        {
            isCatching = false;
            OpenGuards();
            Debug.Log("BallCatcher: 球离开！打开挡板");
        }
    }

    public void OpenGuards()
    {
        // 挡板向外打开
        frontTargetRot = Quaternion.Euler(openAngle, 0f, 0f);
        backTargetRot = Quaternion.Euler(-openAngle, 0f, 0f);
        leftTargetRot = Quaternion.Euler(0f, 0f, -openAngle);
        rightTargetRot = Quaternion.Euler(0f, 0f, openAngle);
    }

    public void CloseGuards()
    {
        frontTargetRot = Quaternion.Euler(0f, 0f, 0f);
        backTargetRot = Quaternion.Euler(closedAngle, 0f, 0f);
        leftTargetRot = Quaternion.Euler(0f, 0f, -closedAngle);
        rightTargetRot = Quaternion.Euler(0f, 0f, -closedAngle);
        // 挡板垂直关闭
        
    }
}
