using UnityEngine;
using UnityEngine.InputSystem;

public class HeadBallSpawner : MonoBehaviour
{
    [Header("球设置")]
    public GameObject ballPrefab; // 球预制体
    public Transform head; // 头部对象
    public float height = 1.5f; // 头顶上方的高度
    public bool setBallTag = true; // 是否自动设置球标签

    [Header("按键设置")]
    public Key spawnKey = Key.Space; // 生成按键
    public Key resetKey = Key.R; // 重置按键
    public Key destroyKey = Key.X; // 销毁按键

    [Header("调试")]
    public bool showDebug = true;

    private GameObject currentBall;
    private Vector3 initialPos;
    private InputAction spawnAction;
    private InputAction resetAction;
    private InputAction destroyAction;

    private void Awake()
    {
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

    private void Update()
    {
        if (spawnAction.WasPressedThisFrame())
        {
            SpawnBall();
        }
        if (resetAction.WasPressedThisFrame())
        {
            ResetBall();
        }
        if (destroyAction.WasPressedThisFrame())
        {
            DestroyBall();
        }
    }

    public void SpawnBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("HeadBallSpawner: 没有设置球预制体！");
            return;
        }
        if (head == null)
        {
            Debug.LogError("HeadBallSpawner: 没有设置头部对象！");
            return;
        }

        DestroyBall();
        initialPos = head.position + Vector3.up * height;
        currentBall = Instantiate(ballPrefab, initialPos, Quaternion.identity);
        
        // 设置球标签
        if (setBallTag)
        {
            currentBall.tag = "Ball";
        }
        
        if (showDebug)
        {
            Debug.Log("HeadBallSpawner: 球已生成！");
        }
    }

    private void ResetBall()
    {
        if (currentBall != null)
        {
            Rigidbody rb = currentBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            currentBall.transform.position = initialPos;
            if (showDebug) Debug.Log("HeadBallSpawner: 球已重置！");
        }
    }

    private void DestroyBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
            if (showDebug) Debug.Log("HeadBallSpawner: 球已销毁！");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || head == null) return;

        // 生成位置标记
        Gizmos.color = Color.cyan;
        Vector3 spawnPos = head.position + Vector3.up * height;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawLine(head.position, spawnPos);
    }
}
