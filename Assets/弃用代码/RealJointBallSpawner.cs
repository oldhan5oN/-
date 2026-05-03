using UnityEngine;
using UnityEngine.InputSystem;

public class RealJointBallSpawner : MonoBehaviour
{
    [Header("球设置")]
    public GameObject ballPrefab; // 球的预制体
    public Transform headTransform; // 头部位置
    public float spawnHeight = 1f; // 生成高度（头顶上方）

    [Header("自动激活")]
    public RealJointHeadBalance jointSystem; // 关节系统
    public bool autoActivateJointSystem = true; // 自动激活关节系统

    [Header("按键设置")]
    public Key spawnKey = Key.Space; // 生成按键
    public Key resetKey = Key.R; // 重置按键
    public Key destroyKey = Key.X; // 销毁按键

    private GameObject currentBall; // 当前的球
    private Vector3 initialBallPosition; // 初始位置
    private InputAction spawnAction;
    private InputAction resetAction;
    private InputAction destroyAction;

    private void Awake()
    {
        // 创建输入动作
        spawnAction = new InputAction(binding: "<Keyboard>/" + spawnKey);
        resetAction = new InputAction(binding: "<Keyboard>/" + resetKey);
        destroyAction = new InputAction(binding: "<Keyboard>/" + destroyKey);
    }

    private void OnEnable()
    {
        spawnAction.Enable();
        resetAction.Enable();
        destroyAction.Enable();
    }

    private void OnDisable()
    {
        spawnAction.Disable();
        resetAction.Disable();
        destroyAction.Disable();
    }

    private void Start()
    {
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.LogWarning("HeadTransform not assigned, using own transform");
        }

        if (autoActivateJointSystem && jointSystem == null)
        {
            jointSystem = GetComponent<RealJointHeadBalance>();
            if (jointSystem == null)
            {
                jointSystem = FindObjectOfType<RealJointHeadBalance>();
            }
        }

        Debug.Log("RealJointBallSpawner: Ready!");
        Debug.Log($"  - Spawn: {spawnKey}");
        Debug.Log($"  - Reset: {resetKey}");
        Debug.Log($"  - Destroy: {destroyKey}");
    }

    private void Update()
    {
        if (spawnAction.WasPressedThisFrame())
        {
            SpawnBall();
        }
        else if (resetAction.WasPressedThisFrame())
        {
            ResetBall();
        }
        else if (destroyAction.WasPressedThisFrame())
        {
            DestroyBall();
        }
    }

    public void SpawnBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("RealJointBallSpawner: Cannot spawn ball - ballPrefab is null!");
            return;
        }

        // 如果已有球，先销毁
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // 计算生成位置
        Vector3 headTop = headTransform.position + Vector3.up * spawnHeight;
        initialBallPosition = headTop;

        // 实例化球
        currentBall = Instantiate(ballPrefab, headTop, Quaternion.identity);
        currentBall.name = "RealJointBall";

        Debug.Log($"RealJointBallSpawner: Ball spawned at {headTop}");

        // 自动激活关节系统
        if (autoActivateJointSystem && jointSystem != null)
        {
            jointSystem.SetBall(currentBall);
            Debug.Log("RealJointBallSpawner: Joint system activated with new ball");
        }
    }

    public void ResetBall()
    {
        if (currentBall == null)
        {
            Debug.LogWarning("RealJointBallSpawner: No ball to reset!");
            SpawnBall();
            return;
        }

        // 获取球的Rigidbody
        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // 停止球
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }

        // 重置位置
        currentBall.transform.position = initialBallPosition;
        currentBall.transform.rotation = Quaternion.identity;

        Debug.Log("RealJointBallSpawner: Ball reset to initial position");

        // 重新激活关节系统
        if (autoActivateJointSystem && jointSystem != null)
        {
            jointSystem.SetBall(currentBall);
        }
    }

    public void DestroyBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
            Debug.Log("RealJointBallSpawner: Ball destroyed");
        }
        else
        {
            Debug.LogWarning("RealJointBallSpawner: No ball to destroy!");
        }
    }

    void OnDrawGizmos()
    {
        if (headTransform != null)
        {
            // 绘制生成位置
            Gizmos.color = Color.cyan;
            Vector3 spawnPosition = headTransform.position + Vector3.up * spawnHeight;
            Gizmos.DrawWireSphere(spawnPosition, 0.1f);
            Gizmos.DrawLine(headTransform.position, spawnPosition);
            
            // 绘制标记
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPosition, 0.05f);
        }
    }
}
