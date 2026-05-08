using UnityEngine;

[RequireComponent(typeof(Animator))]
public class KinectAuraMistVFX : MonoBehaviour
{
    public Animator animator;
    public KinectManager kinectManager;

    [Header("Anchor")]
    public HumanBodyBones anchorBone = HumanBodyBones.Spine;

    [Header("Look (1980s Shanghai pale palette)")]
    public Color   mistColor     = new Color(0.78f, 0.82f, 0.88f, 0.32f);
    public Vector2 sizeRange     = new Vector2(0.4f, 1.1f);
    public Vector2 lifetimeRange = new Vector2(2.5f, 4.5f);

    [Header("Volume")]
    public float radius = 0.7f;
    public float height = 1.8f;

    [Header("Motion")]
    public float baseEmission        = 25f;
    public float motionEmissionBoost = 60f;
    public float upwardSpeed         = 0.25f;
    public float swirlStrength       = 0.3f;

    public Material mistMaterial;

    ParticleSystem ps;
    Transform anchor;
    Vector3 lastAnchorPos;
    float smoothedSpeed;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (animator == null) { enabled = false; return; }
        anchor = animator.GetBoneTransform(anchorBone);
        if (anchor == null) anchor = transform;
        lastAnchorPos = anchor.position;

        var go = new GameObject("AuraMist");
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = Vector3.zero;

        ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = mistColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;

        var emission = ps.emission;
        emission.rateOverTime = baseEmission;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 2f, height, radius * 2f);
        shape.rotation = Vector3.zero;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(upwardSpeed * 0.6f, upwardSpeed * 1.4f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = swirlStrength;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(mistColor, 0f), new GradientColorKey(mistColor, 1f) },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(mistColor.a, 0.3f),
                new GradientAlphaKey(mistColor.a * 0.6f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.2f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mistMaterial != null ? mistMaterial : CreateDefaultMaterial();
        renderer.sortingFudge = 1f;
    }

    void LateUpdate()
    {
        if (anchor == null || ps == null) return;
        bool active = kinectManager == null || kinectManager.enabled;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        float v = (anchor.position - lastAnchorPos).magnitude / dt;
        lastAnchorPos = anchor.position;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, v, 4f * dt);

        var emission = ps.emission;
        emission.rateOverTime = active
            ? baseEmission + Mathf.Clamp01(smoothedSpeed / 2f) * motionEmissionBoost
            : 0f;
    }

    static Material CreateDefaultMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var m = new Material(sh);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        return m;
    }
}
