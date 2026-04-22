using UnityEngine;

public class BallCatcher3D : MonoBehaviour
{
    [Header("挡板引用")]
    public Transform frontGuard;
    public Transform backGuard;
    public Transform leftGuard;
    public Transform rightGuard;

    [Header("设置参数")]
    public float openAngle = 45f; // 打开角度
    public float rotationSpeed = 8f; // 旋转速度
    public string ballTag = "Ball"; // 球标签

    [Header("状态")]
    public bool isCatching; // 是否在接球

    [Header("调试")]
    public bool showDebug = true;

    // 目标旋转
    private Quaternion frontTargetRot;
    private Quaternion backTargetRot;
    private Quaternion leftTargetRot;
    private Quaternion rightTargetRot;

    private void Start()
    {
        // 初始状态：挡板不存在（隐藏）
        HideGuards();
        if (showDebug)
        {
            Debug.Log("BallCatcher3D: 启动！挡板已隐藏（等待球进入）");
        }
    }

    private void Update()
    {
        if (isCatching)
        {
            // 只有在接球状态时才更新挡板旋转
            LerpGuards();
        }
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ballTag))
        {
            isCatching = true;
            // 球进入时：显示挡板并立即关闭（垂直状态）
            ShowGuards();
            CloseGuards();
            if (showDebug) Debug.Log("BallCatcher3D: 球进入！显示挡板并关闭");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ballTag))
        {
            isCatching = false;
            // 球离开时：隐藏挡板
            HideGuards();
            if (showDebug) Debug.Log("BallCatcher3D: 球离开！隐藏挡板");
        }
    }

    public void ShowGuards()
    {
        // 显示所有挡板
        if (frontGuard != null) frontGuard.gameObject.SetActive(true);
        if (backGuard != null) backGuard.gameObject.SetActive(true);
        if (leftGuard != null) leftGuard.gameObject.SetActive(true);
        if (rightGuard != null) rightGuard.gameObject.SetActive(true);
    }

    public void HideGuards()
    {
        // 隐藏所有挡板
        if (frontGuard != null) frontGuard.gameObject.SetActive(false);
        if (backGuard != null) backGuard.gameObject.SetActive(false);
        if (leftGuard != null) leftGuard.gameObject.SetActive(false);
        if (rightGuard != null) rightGuard.gameObject.SetActive(false);
    }

    public void OpenGuards()
    {
        // 挡板向外打开 - 3D版本
        // 前后挡板绕X轴旋转
        frontTargetRot = Quaternion.Euler(-openAngle, 0f, 0f);  // 向前倾斜
        backTargetRot = Quaternion.Euler(openAngle, 0f, 0f);    // 向后倾斜
        
        // 左右挡板绕Z轴旋转
        leftTargetRot = Quaternion.Euler(0f, 0f, openAngle);    // 向左倾斜
        rightTargetRot = Quaternion.Euler(0f, 0f, -openAngle);  // 向右倾斜
        
        if (showDebug) Debug.Log($"BallCatcher3D: 打开挡板，角度={openAngle}");
    }

    public void CloseGuards()
    {
        // 挡板垂直关闭（所有挡板垂直向上）
        frontTargetRot = Quaternion.Euler(openAngle, 0f, 0f);  // 前挡板垂直
        backTargetRot = Quaternion.Euler(-openAngle, 0f, 0f);   // 后挡板垂直
        leftTargetRot = Quaternion.Euler(0f, 0f, -openAngle);   // 左挡板垂直
        rightTargetRot = Quaternion.Euler(0f, 0f, openAngle);  // 右挡板垂直
        
        if (showDebug) Debug.Log("BallCatcher3D: 关闭挡板（垂直状态）");
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // 检测区域
        Gizmos.color = isCatching ? Color.green : Color.yellow;
        if (TryGetComponent<Collider>(out Collider col))
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
