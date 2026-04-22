using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 关节式头部球生成器 - 适配 JointBasedHeadBalance 系统
/// 功能：按指定按键生成球，按另一按键复位球位置
/// </summary>
public class JointHeadBallSpawner : MonoBehaviour
{
    [Header("头部设置")]
    [Tooltip("头部Transform")]
    public Transform headTransform;
    [Tooltip("生成高度（头部上方距离）")]
    public float spawnHeight = 2.0f;

    [Header("球设置")]
    [Tooltip("球预制体")]
    public GameObject ballPrefab;
    [Tooltip("球生成后是否自动激活顶缸系统")]
    public bool autoActivateJointSystem = true;

    [Header("按键设置")]
    [Tooltip("生成球的按键")]
    public Key spawnKey = Key.Space;
    [Tooltip("复位球的按键")]
    public Key resetKey = Key.R;

    [Header("引用")]
    [Tooltip("关节式顶缸系统脚本")]
    public JointBasedHeadBalance jointSystem;

    private GameObject currentBall;
    private Vector3 initialBallPosition;

    private void Awake()
    {
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.Log("JointHeadBallSpawner: headTransform set to self.");
        }

        if (ballPrefab == null)
        {
            Debug.LogError("JointHeadBallSpawner: ballPrefab not assigned.");
            enabled = false;
        }
    }

    private void Update()
    {
        // 生成球
        if (Keyboard.current[spawnKey].wasPressedThisFrame)
        {
            SpawnBall();
        }

        // 复位球
        if (Keyboard.current[resetKey].wasPressedThisFrame)
        {
            ResetBall();
        }
    }

    /// <summary>
    /// 在头部上方生成球
    /// </summary>
    public void SpawnBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("JointHeadBallSpawner: Cannot spawn ball - ballPrefab is null.");
            return;
        }

        // 如果已有球，先销毁
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // 计算生成位置
        Vector3 spawnPosition = headTransform.position + Vector3.up * spawnHeight;
        
        // 实例化球
        currentBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        currentBall.name = "JointBall";
        
        // 记录初始位置
        initialBallPosition = spawnPosition;

        // 自动激活顶缸系统
        if (autoActivateJointSystem && jointSystem != null)
        {
            jointSystem.SetBall(currentBall);
            Debug.Log("JointHeadBallSpawner: Ball spawned and joint system activated.");
        }
        else if (jointSystem == null)
        {
            Debug.LogWarning("JointHeadBallSpawner: jointSystem not assigned, ball spawned but no joint control.");
        }
        else
        {
            Debug.Log("JointHeadBallSpawner: Ball spawned, joint system not auto-activated.");
        }
    }

    /// <summary>
    /// 复位球到初始位置
    /// </summary>
    public void ResetBall()
    {
        if (currentBall == null)
        {
            Debug.LogWarning("JointHeadBallSpawner: Cannot reset - no ball exists.");
            return;
        }

        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // 重置位置和速度
            currentBall.transform.position = initialBallPosition;
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            
            Debug.Log("JointHeadBallSpawner: Ball reset to initial position.");
        }
        else
        {
            Debug.LogError("JointHeadBallSpawner: Ball has no Rigidbody, cannot reset properly.");
        }
    }

    /// <summary>
    /// 清空当前球
    /// </summary>
    public void ClearBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
            
            if (jointSystem != null)
            {
                jointSystem.SetBall(null);
            }
            
            Debug.Log("JointHeadBallSpawner: Ball cleared.");
        }
    }

    void OnDrawGizmos()
    {
        if (headTransform == null) return;

        // 绘制生成位置
        Gizmos.color = Color.yellow;
        Vector3 spawnPos = headTransform.position + Vector3.up * spawnHeight;
        Gizmos.DrawWireSphere(spawnPos, 0.1f);
        Gizmos.DrawLine(headTransform.position, spawnPos);

        // 绘制当前球位置
        if (currentBall != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentBall.transform.position, 0.15f);
            Gizmos.DrawLine(spawnPos, currentBall.transform.position);
        }
    }
}
