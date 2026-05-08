using UnityEngine;
using System.Collections.Generic;

public class KinectParticleOutline : MonoBehaviour
{
    [Header("Kinect 绑定（留空自动查找）")]
    public KinectManager kinectManager;

    [Header("粒子密度与拖尾")]
    [Range(100, 4000)] public int particleDensity = 1200;
    [Range(0.3f, 5f)] public float particleLifetime = 1.6f;
    [Range(0.3f, 5f)] public float trailLifetime = 1.2f;
    [Range(0.005f, 0.12f)] public float trailWidth = 0.025f;

    [Header("跟随平滑")]
    [Range(0.01f, 0.4f)] public float followDamping = 0.05f;

    [Header("矿物国风色")]
    public Color baseColor = new Color(1f, 0.96f, 0.88f, 1f);
    public Color trailColorA = new Color(0.42f, 0.74f, 0.72f, 1f);
    public Color trailColorB = new Color(0.82f, 0.55f, 0.78f, 1f);
    [Range(0f, 2f)] public float emissionIntensity = 1.6f;

    [Header("粒子尺寸")]
    [Range(0.01f, 0.2f)] public float particleSize = 0.06f;
    [Range(0f, 0.05f)] public float sizeVariation = 0.015f;

    [Header("距离消散")]
    [Range(1f, 20f)] public float fadeNearDistance = 3f;
    [Range(3f, 30f)] public float fadeFarDistance = 15f;

    [Header("碰撞体（接缸/碟，不可见）")]
    public bool enableCollision = true;
    [Range(0.03f, 0.25f)] public float colliderRadius = 0.08f;

    [Header("头部锚点（拖入缸/碟即生效）")]
    public Transform headAnchorTarget;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private ParticleSystem.Particle[] particles;
    private int jointCount;
    private Vector3[] currentPositions;
    private bool[] jointValid;
    private List<SphereCollider> boneColliders = new List<SphereCollider>();
    private Transform headTransform;
    private bool systemReady;
    private float retryTimer;
    private Material particleMat;
    private Texture2D softTex;

    void Start() { TryInitialize(); }

    void LateUpdate()
    {
        if (!systemReady)
        {
            retryTimer += Time.deltaTime;
            if (retryTimer > 0.4f) { retryTimer = 0f; TryInitialize(); }
            return;
        }
        if (kinectManager == null) { systemReady = false; return; }
        if (!kinectManager.IsInitialized() || !kinectManager.IsUserDetected()) return;
        long userId = kinectManager.GetPrimaryUserID();
        if (userId == 0) return;

        SampleJoints(userId);
        EmitParticles();
        MoveColliders();
        FollowHead();
    }

    private void TryInitialize()
    {
        if (kinectManager == null) kinectManager = KinectManager.Instance;
        if (kinectManager == null) kinectManager = FindObjectOfType<KinectManager>();
        if (kinectManager == null)
        {
            var go = GameObject.Find("KinectController");
            if (go != null) kinectManager = go.GetComponent<KinectManager>();
        }
        if (kinectManager == null)
        {
            var go = GameObject.Find("KinectManager");
            if (go != null) kinectManager = go.GetComponent<KinectManager>();
        }
        if (kinectManager == null) return;
        if (!kinectManager.IsInitialized()) return;

        jointCount = kinectManager.GetJointCount();
        if (jointCount <= 0) return;

        currentPositions = new Vector3[jointCount];
        jointValid = new bool[jointCount];

        BuildParticleAssets();
        BuildParticleSystem();
        BuildColliders();
        systemReady = true;
        Debug.Log("KinectParticleOutline 初始化完成，骨骼数=" + jointCount);
    }

    private void BuildParticleAssets()
    {
        // 运行时生成软圆贴图，避免缺失贴图导致品红
        int size = 64;
        softTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        softTex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c) / maxR;
            float a = Mathf.Clamp01(1f - d);
            a = Mathf.Pow(a, 2f);
            softTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        softTex.Apply();

        // 依次尝试多个 Shader 兼容 URP/HDRP/Built-in
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("HDRP/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        particleMat = new Material(sh);
        particleMat.mainTexture = softTex;

        // 兼容各 shader 的属性
        if (particleMat.HasProperty("_BaseColor")) particleMat.SetColor("_BaseColor", Color.white);
        if (particleMat.HasProperty("_Color")) particleMat.SetColor("_Color", Color.white);
        if (particleMat.HasProperty("_BaseMap")) particleMat.SetTexture("_BaseMap", softTex);
        if (particleMat.HasProperty("_Surface")) particleMat.SetFloat("_Surface", 1f);       // Transparent
        if (particleMat.HasProperty("_Blend")) particleMat.SetFloat("_Blend", 1f);            // Additive
        if (particleMat.HasProperty("_SrcBlend")) particleMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (particleMat.HasProperty("_DstBlend")) particleMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (particleMat.HasProperty("_ZWrite")) particleMat.SetFloat("_ZWrite", 0f);
        particleMat.renderQueue = 3000;
    }

    private void BuildParticleSystem()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.maxParticles = particleDensity;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize - sizeVariation, particleSize + sizeVariation);
        main.startColor = baseColor * emissionIntensity;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;

        // 生命周期颜色
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(trailColorA, 0.35f),
                new GradientColorKey(trailColorB, 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // 生命周期尺寸 - 拖尾感
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 0.85f),
            new Keyframe(1f, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Trails 模块 - 真正的拖尾
        var trails = ps.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.lifetime = new ParticleSystem.MinMaxCurve(trailLifetime);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(trailWidth);
        trails.minVertexDistance = 0.02f;
        trails.dieWithParticles = true;
        trails.sizeAffectsWidth = true;
        var trailGrad = new Gradient();
        trailGrad.SetKeys(
            new[] {
                new GradientColorKey(trailColorA, 0f),
                new GradientColorKey(trailColorB, 1f)
            },
            new[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(trailGrad);

        psr = GetComponent<ParticleSystemRenderer>();
        if (psr == null) psr = gameObject.AddComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.alignment = ParticleSystemRenderSpace.View;
        psr.material = particleMat;
        psr.trailMaterial = particleMat;

        particles = new ParticleSystem.Particle[particleDensity];
    }

    private void BuildColliders()
    {
        if (!enableCollision) return;
        for (int i = 0; i < jointCount; i++)
        {
            var obj = new GameObject("BoneCol_" + i);
            obj.transform.SetParent(transform);
            obj.layer = gameObject.layer;
            obj.hideFlags = HideFlags.DontSave;
            obj.SetActive(false);

            var sc = obj.AddComponent<SphereCollider>();
            sc.radius = colliderRadius;

            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            boneColliders.Add(sc);
            if (i == 3) headTransform = obj.transform;
        }
    }

    private void SampleJoints(long userId)
    {
        float dt = Time.deltaTime;
        float alpha = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, followDamping));
        for (int i = 0; i < jointCount; i++)
        {
            if (!kinectManager.IsJointTracked(userId, i)) { jointValid[i] = false; continue; }
            Vector3 raw = kinectManager.GetJointPosition(userId, i);
            if (raw.sqrMagnitude < 0.0001f) { jointValid[i] = false; continue; }
            jointValid[i] = true;
            if (currentPositions[i].sqrMagnitude < 0.0001f) currentPositions[i] = raw;
            else currentPositions[i] = Vector3.Lerp(currentPositions[i], raw, alpha);
        }
    }

    private void EmitParticles()
    {
        if (ps == null || particles == null) return;

        float fade = 1f;
        var cam = Camera.main;
        if (cam != null)
        {
            float d = Vector3.Distance(cam.transform.position, transform.position);
            fade = 1f - Mathf.InverseLerp(fadeNearDistance, fadeFarDistance, d);
        }
        fade = Mathf.Max(0.1f, fade);

        int validJoints = 0;
        for (int i = 0; i < jointCount; i++) if (jointValid[i]) validJoints++;
        if (validJoints == 0) { ps.SetParticles(particles, 0); return; }

        int budget = Mathf.RoundToInt(particleDensity * fade);
        int perBone = Mathf.Max(4, budget / validJoints);
        int idx = 0;

        for (int j = 0; j < jointCount && idx < budget; j++)
        {
            if (!jointValid[j]) continue;

            int parent = (int)kinectManager.GetParentJoint((KinectInterop.JointType)j);
            bool hasParent = (parent != j) && (parent >= 0) && (parent < jointCount) && jointValid[parent];
            Vector3 posA = currentPositions[j];
            Vector3 posB = hasParent ? currentPositions[parent] : posA;
            float boneLen = Vector3.Distance(posA, posB);
            if (hasParent && boneLen < 0.001f) hasParent = false;

            int count = Mathf.Min(perBone, budget - idx);
            for (int p = 0; p < count; p++)
            {
                if (idx >= particles.Length) break;
                float t = (float)p / Mathf.Max(1, count);
                Vector3 pos = hasParent ? Vector3.Lerp(posA, posB, t) : posA;
                pos.x += Random.Range(-0.015f, 0.015f);
                pos.y += Random.Range(-0.015f, 0.015f);
                pos.z += Random.Range(-0.01f, 0.01f);

                Color c = Color.Lerp(baseColor, Color.Lerp(trailColorA, trailColorB, t), 0.55f);
                c *= emissionIntensity;
                c.a = fade;

                particles[idx].position = pos;
                particles[idx].startLifetime = particleLifetime;
                particles[idx].remainingLifetime = particleLifetime * Random.Range(0.6f, 1f);
                particles[idx].startSize = particleSize + Random.Range(-sizeVariation, sizeVariation);
                particles[idx].startColor = c;
                // 细微速度让 PerParticle Trail 拉出轨迹
                particles[idx].velocity = new Vector3(
                    Random.Range(-0.08f, 0.08f),
                    Random.Range(0.02f, 0.15f),
                    Random.Range(-0.08f, 0.08f));
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
            else boneColliders[i].gameObject.SetActive(false);
        }
    }

    private void FollowHead()
    {
        if (headAnchorTarget == null || headTransform == null) return;
        if (jointValid == null || jointValid.Length <= 3 || !jointValid[3]) return;
        Vector3 target = currentPositions[3] + Vector3.up * 0.18f;
        float alpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, followDamping));
        headAnchorTarget.position = Vector3.Lerp(headAnchorTarget.position, target, alpha);
    }

    void OnDestroy()
    {
        for (int i = boneColliders.Count - 1; i >= 0; i--)
            if (boneColliders[i] != null) Destroy(boneColliders[i].gameObject);
        boneColliders.Clear();
        if (particleMat != null) Destroy(particleMat);
        if (softTex != null) Destroy(softTex);
    }
}
