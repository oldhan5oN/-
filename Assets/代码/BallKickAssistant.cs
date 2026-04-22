using UnityEngine;

public class BallKickAssistant : MonoBehaviour
{
    [Header("头部引用")]
    public Transform headTransform; // 头部Transform

    [Header("设置参数")]
    public string ballTag = "Ball"; // 球标签
    public float headUpThreshold = 0.5f; // 头部向上速度阈值
    public float ballForceMultiplier = 5f; // 球的力倍数

    [Header("调试")]
    public bool showDebug = true;

    // 头部位置跟踪
    private Vector3 lastHeadPosition;
    private Vector3 currentHeadVelocity;
    
    // 球引用
    private Rigidbody currentBall;

    private void Start()
    {
        if (headTransform != null)
        {
            lastHeadPosition = headTransform.position;
        }
        if (showDebug)
        {
            Debug.Log("BallKickAssistant: 启动！准备辅助顶球");
        }
    }

    private void Update()
    {
        // 计算头部速度
        UpdateHeadVelocity();
        
        // 检测头部向上运动并辅助顶球
        CheckHeadUpMotion();
    }

    private void UpdateHeadVelocity()
    {
        if (headTransform != null)
        {
            currentHeadVelocity = (headTransform.position - lastHeadPosition) / Time.deltaTime;
            lastHeadPosition = headTransform.position;
        }
    }

    private void CheckHeadUpMotion()
    {
        // 检测头部向上运动
        if (currentHeadVelocity.y > headUpThreshold && currentBall != null)
        {
            // 计算顶球力（基于头部向上速度）
            float force = currentHeadVelocity.y * ballForceMultiplier;
            currentBall.AddForce(Vector3.up * force, ForceMode.Impulse);
            
            if (showDebug)
            {
                Debug.Log($"BallKickAssistant: 辅助顶球！力={force}");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ballTag))
        {
            // 保存球的引用
            currentBall = other.GetComponent<Rigidbody>();
            if (showDebug)
            {
                Debug.Log("BallKickAssistant: 球进入检测区域");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ballTag))
        {
            // 清除球的引用
            currentBall = null;
            if (showDebug)
            {
                Debug.Log("BallKickAssistant: 球离开检测区域");
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // 检测区域
        Gizmos.color = currentBall != null ? Color.green : Color.yellow;
        if (TryGetComponent<Collider>(out Collider col))
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        
        // 头部速度可视化
        if (headTransform != null && currentHeadVelocity != Vector3.zero)
        {
            Gizmos.color = currentHeadVelocity.y > headUpThreshold ? Color.green : Color.blue;
            Gizmos.DrawRay(headTransform.position, currentHeadVelocity * 0.5f);
        }
    }
}
