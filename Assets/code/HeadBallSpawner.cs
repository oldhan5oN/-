using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 头部球生成器 - 在头部上方生成球并支持复位
/// 功能：按指定按键生成球，按另一按键复位球位置
/// </summary>
public class HeadBallSpawner : MonoBehaviour
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
    public bool autoActivateAcrobat = true;

    [Header("按键设置")]
    [Tooltip("生成球的按键")]
    public Key spawnKey = Key.Space;
    [Tooltip("复位球的按键")]
    public Key resetKey = Key.R;

    [Header("引用")]
    [Tooltip("顶缸系统脚本")]
    public AcrobatHeadBalance acrobatSystem;

    private GameObject currentBall;
    private Vector3 initialBallPosition;

    private void Awake()
    {
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.Log("HeadBallSpawner: headTransform set to self.");
        }

        if (ballPrefab == null)
        {
            Debug.LogError("HeadBallSpawner: ballPrefab not assigned.");
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
    /// 生成球
    /// </summary>
    private void SpawnBall()
    {
        if (currentBall != null)
        {
            Debug.Log("HeadBallSpawner: Ball already exists.");
            return;
        }

        // 计算生成位置
        Vector3 spawnPosition = headTransform.position + Vector3.up * spawnHeight;
        initialBallPosition = spawnPosition;

        // 生成球
        currentBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        currentBall.name = "HeadBall";

        // 添加Rigidbody（如果没有）
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentBall.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.mass = 0.5f;
            rb.useGravity = true;
        }

        // 自动激活顶缸系统
        if (autoActivateAcrobat && acrobatSystem != null)
        {
            acrobatSystem.SetBall(currentBall);
            Debug.Log("HeadBallSpawner: Acrobat system activated with new ball.");
        }

        Debug.Log($"HeadBallSpawner: Ball spawned at position: {spawnPosition}");
    }

    /// <summary>
    /// 复位球
    /// </summary>
    private void ResetBall()
    {
        if (currentBall == null)
        {
            Debug.Log("HeadBallSpawner: No ball to reset.");
            return;
        }

        // 重置位置
        currentBall.transform.position = initialBallPosition;
        currentBall.transform.rotation = Quaternion.identity;

        // 重置速度
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"HeadBallSpawner: Ball reset to position: {initialBallPosition}");
    }

    /// <summary>
    /// 清理球
    /// </summary>
    public void ClearBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
            Debug.Log("HeadBallSpawner: Ball cleared.");
        }
    }

    private void OnDrawGizmos()
    {
        if (headTransform == null)
            return;

        // 绘制生成位置
        Gizmos.color = Color.yellow;
        Vector3 spawnPosition = headTransform.position + Vector3.up * spawnHeight;
        Gizmos.DrawWireSphere(spawnPosition, 0.2f);
        Gizmos.DrawLine(headTransform.position, spawnPosition);

        // 绘制当前球
        if (currentBall != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentBall.transform.position, 0.3f);
        }
    }
}