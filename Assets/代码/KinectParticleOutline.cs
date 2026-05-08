using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kinect V2 粒子人形轮廓 - 国风矿物色拖影
/// 自动查找 KinectManager，无需手动绑定
/// </summary>
public class KinectParticleOutline : MonoBehaviour
{
    [Header("Kinect 绑定（留空则自动查找）")]
    public KinectManager kinectManager;

    [Header("粒子密度与拖尾")]
    [Range(50, 2000)]
    public int particleDensity = 600;
    [Range(0.2f, 4f)]
    public float trailLength = 1.2f;

    [Header("跟随平滑")]
    [Range(0.01f, 0.5f)]
    public float followDamping = 0.06f;

    [Header("大闹天宫矿物色")]
    public Color baseColor = new Color(0.96f, 0.93f, 0.87f, 1f);
    [Tooltip("拖尾起始色 - 低饱和石青")]
    public Color trailColorA = new Color(0.45f, 0.68f, 0.65f, 0.8f);
    [Tooltip("拖尾末端色 - 低饱和粉紫")]
    public Color trailColorB = new Color(0.72f, 0.52f, 0.68f, 0.6f);
    [Range(0f, 1f)]
    public float colorSaturation = 0.45f;

    [Header("粒子尺寸")]
    [Range(0.005f, 0.1f)]
    public float particleSize = 0.028f;
    [Range(0.001f, 0.03f)]
    public float particleSizeVariation = 0.008f;

    [Header("距离消散")]
    [Range(1f, 10f)]
    public float fadeNearDistance = 2f;
    [Range(3f, 20f)]
    public float fadeFarDistance = 10f;

    [Header("碰撞体（接缸/碟用）")]
    public bool enableCollision = true;
    [Range(0.03f, 0.25f)]
    public float colliderRadius = 0.07f;
    public PhysicsMaterial colliderMaterial;

    [Header("头部锚点（拖入缸/碟即可生效）")]
    public Transform headAnchorTarget;

    // 内部状态
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private int jointCount;
    private Vector3[] currentPositions;
    private Vector3[] targetPositions;
    private bool[] jointValid;
    private int particlesPerBone;
    private List<SphereCollider> boneColliders = new List<SphereCollider>();
    private Transform headTransform;
    private bool systemReady;
    private float retryTimer;

    void Start()
    {
        TryInitialize();
    }

    void LateUpdate()
    {
        // 未就绪时定期重试
        if (!systemReady)
        {
            retryTimer += Time.deltaTime;
            if (retryTimer > 0.5f)
            {
                retryTimer = 0f;
                TryInitialize();
            }
            return;
        }

        // 确认 KinectManager 仍然有效
        if (kinectManager == null)
        {
            systemReady = false;
            return;
        }

        if (!kinectManager.IsInitialized() || !kinectManager.IsUserDetected())
            return;

        long userId = kinectManager.GetPrimaryUserID();
        if (userId == 0) return;

        SampleJoints(userId);
        EmitParticles();
        MoveColliders();
        FollowHead();
    }

    private void TryInitialize()
    {
        // 多路查找 KinectManager
        if (kinectManager == null)
            kinectManager = KinectManager.Instance;
        if (kinectManager == null)
            kinectManager = FindObjectOfType<KinectManager>();
        if (kinectManager == null)
        {
            GameObject go = GameObject.Find("KinectController");
            if (go != null) kinectManager = go.GetComponent<KinectManager>();
        }
        if (kinectManager == null)
        {
            GameObject go = GameObject.Find("KinectManager");
            if (go != null) kinectManager = go.GetComponent<KinectManager>();
        }

        if (kinectManager == null) return;
        if (!kinectManager.IsInitialized()) return;

        jointCount = kinectManager.GetJointCount();
        if (jointCount <= 0) return;

        currentPositions = new Vector3[jointCount];
        targetPositions = new Vector3[jointCount];
        jointValid = new bool[jointCount];

        BuildParticleSystem();
        BuildColliders();

        systemReady = true;
        Debug.Log("KinectParticleOutline: 初始化完成，骨骼数=" + jointCount);
    }

    private void BuildParticleSystem()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
            ps = gameObject.AddComponent<ParticleSystem>();

        // 停止自动播放以便手动控制
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.maxParticles = particleDensity;
        main.startLifetime = trailLength;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSize = new ParticleSystem.MinMaxCurve(
            particleSize - particleSizeVariation,
            particleSize + particleSizeVariation);
        main.startColor = baseColor;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(Color.Lerp(trailColorA, trailColorB, 0.5f), 0.4f),
                new GradientColorKey(trailColorB, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.4f, 0.7f),
            new Keyframe(1f, 0f)
        ));

        var psr = GetComponent<ParticleSystemRenderer>();
        if (psr == null)
            psr = gameObject.AddComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;

        particles = new ParticleSystem.Particle[particleDensity];
        particlesPerBone = Mathf.Max(2, particleDensity / Mathf.Max(1, jointCount));
    }

    private void BuildColliders()
    {
        if (!enableCollision) return;

        for (int i = 0; i < jointCount; i++)
        {
            GameObject obj = new GameObject("BoneCol" + i);
            obj.transform.SetParent(transform);
            obj.layer = gameObject.layer;
            obj.SetActive(false);

            SphereCollider sc = obj.AddComponent<SphereCollider>();
            sc.radius = colliderRadius;
            if (colliderMaterial != null)
                sc.material = colliderMaterial;

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            boneColliders.Add(sc);

            // joint index 3 = Head
            if (i == 3)
                headTransform = obj.transform;
        }
    }

    private void SampleJoints(long userId)
    {
        float dt = Time.deltaTime;
        // 指数平滑系数，避免除零
        float alpha = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, followDamping));

        for (int i = 0; i < jointCount; i++)
        {
            if (!kinectManager.IsJointTracked(userId, i))
            {
                jointValid[i] = false;
                continue;
            }

            Vector3 raw = kinectManager.GetJointPosition(userId, i);

            // 过滤无效零点（Kinect 未就绪时返回零）
            if (raw.x == 0f && raw.y == 0f && raw.z == 0f)
            {
                jointValid[i] = false;
                continue;
            }

            jointValid[i] = true;
            targetPositions[i] = raw;

            // 首次采样直接赋值，后续做指数平滑
            if (currentPositions[i].sqrMagnitude < 0.0001f)
                currentPositions[i] = raw;
            else
                currentPositions[i] = Vector3.Lerp(currentPositions[i], raw, alpha);
        }
    }

    private void EmitParticles()
    {
        if (ps == null || particles == null) return;

        // 距离衰减
        float fade = 1f;
        Camera cam = Camera.main;
        if (cam != null)
        {
            float d = Vector3.Distance(cam.transform.position, transform.position);
            fade = 1f - Mathf.InverseLerp(fadeNearDistance, fadeFarDistance, d);
        }
        fade = Mathf.Max(0.05f, fade);

        int budget = Mathf.RoundToInt(particleDensity * fade);
        int idx = 0;

        for (int j = 0; j < jointCount && idx < budget; j++)
        {
            if (!jointValid[j]) continue;

            // 获取父骨骼用于连线插值
            int parent = (int)kinectManager.GetParentJoint((KinectInterop.JointType)j);
            bool hasParent = (parent != j) && (parent >= 0) && (parent < jointCount) && jointValid[parent];

            Vector3 posA = currentPositions[j];
            Vector3 posB = hasParent ? currentPositions[parent] : posA;

            // 跳过两点重合的骨骼段
            float boneLen = Vector3.Distance(posA, posB);
            if (hasParent && boneLen < 0.001f)
                hasParent = false;

            int count = Mathf.Min(particlesPerBone, budget - idx);
            for (int p = 0; p < count; p++)
            {
                if (idx >= particles.Length) break;

                float t = (float)p / count;

                // 沿骨骼段分布
                Vector3 pos = hasParent ? Vector3.Lerp(posA, posB, t) : posA;

                // 微量随机偏移产生体积感
                pos.x += Random.Range(-0.012f, 0.012f);
                pos.y += Random.Range(-0.012f, 0.012f);
                pos.z += Random.Range(-0.008f, 0.008f);

                // 国风渐变色
                Color c = Color.Lerp(baseColor, Color.Lerp(trailColorA, trailColorB, t), colorSaturation);
                c.a = (1f - t * 0.7f) * fade;

                particles[idx].position = pos;
                particles[idx].startLifetime = trailLength;
                particles[idx].remainingLifetime = trailLength * (1f - t * 0.15f);
                particles[idx].startSize = particleSize + Random.Range(-particleSizeVariation, particleSizeVariation);
                particles[idx].startColor = c;
                idx++;
            }
        }

        ps.SetParticles(particles, idx);
    }

    private void MoveColliders()
    {
        if (!enableCollision) return;

        for (int i = 0; i < boneColliders.Count && i < jointCount; i++)
        {
            if (jointValid[i])
            {
                boneColliders[i].gameObject.SetActive(true);
                boneColliders[i].transform.position = currentPositions[i];
            }
            else
            {
                boneColliders[i].gameObject.SetActive(false);
            }
        }
    }

    private void FollowHead()
    {
        if (headAnchorTarget == null || headTransform == null) return;
        if (!jointValid[3]) return;

        Vector3 target = currentPositions[3] + Vector3.up * 0.18f;
        float alpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, followDamping));
        headAnchorTarget.position = Vector3.Lerp(headAnchorTarget.position, target, alpha);
    }

    void OnValidate()
    {
        if (ps == null) return;
        var main = ps.main;
        main.maxParticles = particleDensity;
        main.startLifetime = trailLength;
        main.startSize = new ParticleSystem.MinMaxCurve(
            particleSize - particleSizeVariation,
            particleSize + particleSizeVariation);
        main.startColor = baseColor;
    }

    void OnDestroy()
    {
        for (int i = boneColliders.Count - 1; i >= 0; i--)
        {
            if (boneColliders[i] != null)
                Destroy(boneColliders[i].gameObject);
        }
        boneColliders.Clear();
    }
}
