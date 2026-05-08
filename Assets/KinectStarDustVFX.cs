using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class KinectStarDustVFX : MonoBehaviour
{
    public Animator animator;
    public KinectManager kinectManager;

    [Header("Disturbance Bones")]
    public HumanBodyBones[] disturbBones = {
        HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
        HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot
    };

    [Header("Volume (around character root)")]
    public Vector3 volumeSize   = new Vector3(3f, 2.8f, 3f);
    public Vector3 volumeOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Look")]
    public Color   dustColor     = new Color(1f, 0.95f, 0.7f, 0.9f);
    public Vector2 sizeRange     = new Vector2(0.012f, 0.045f);
    public Vector2 lifetimeRange = new Vector2(3f, 6f);
    public int     maxParticles  = 300;
    public float   emissionRate  = 40f;

    [Header("Motion")]
    public float driftSpeed    = 0.08f;
    public float disturbRadius = 0.35f;
    public float disturbForce  = 1.5f;

    public Material dustMaterial;

    ParticleSystem ps;
    ParticleSystem.Particle[] buffer;
    Transform[] disturbTransforms;
    Vector3[] disturbVelocities;
    Vector3[] disturbLastPos;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (animator == null) { enabled = false; return; }

        var list = new List<Transform>();
        foreach (var b in disturbBones)
        {
            var t = animator.GetBoneTransform(b);
            if (t != null) list.Add(t);
        }
        disturbTransforms = list.ToArray();
        disturbVelocities = new Vector3[disturbTransforms.Length];
        disturbLastPos    = new Vector3[disturbTransforms.Length];
        for (int i = 0; i < disturbTransforms.Length; i++)
            disturbLastPos[i] = disturbTransforms[i].position;

        var go = new GameObject("StarDust");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = volumeOffset;
        ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = 0.02f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = dustColor;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = volumeSize;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.5f, driftSpeed);
        velocity.x = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.3f, driftSpeed * 0.3f);
        velocity.z = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.3f, driftSpeed * 0.3f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.3f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(dustColor, 0f), new GradientColorKey(dustColor, 1f) },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(dustColor.a, 0.2f),
                new GradientAlphaKey(dustColor.a, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f),
            new Keyframe(0.7f, 1f), new Keyframe(1f, 0.3f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = dustMaterial != null ? dustMaterial : CreateDefaultMaterial();

        buffer = new ParticleSystem.Particle[maxParticles];
    }

    void LateUpdate()
    {
        if (ps == null) return;
        bool active = kinectManager == null || kinectManager.enabled;

        var emission = ps.emission;
        emission.rateOverTime = active ? emissionRate : 0f;

        ps.transform.position = transform.position + volumeOffset;

        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        for (int i = 0; i < disturbTransforms.Length; i++)
        {
            disturbVelocities[i] = (disturbTransforms[i].position - disturbLastPos[i]) / dt;
            disturbLastPos[i] = disturbTransforms[i].position;
        }

        int count = ps.GetParticles(buffer);
        float r = disturbRadius;
        float r2 = r * r;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = buffer[i].position;
            for (int b = 0; b < disturbTransforms.Length; b++)
            {
                Vector3 d = p - disturbTransforms[b].position;
                float sq = d.sqrMagnitude;
                if (sq < r2 && sq > 1e-5f)
                {
                    float dist = Mathf.Sqrt(sq);
                    float falloff = 1f - dist / r;
                    Vector3 boneVel = disturbVelocities[b];
                    Vector3 boneVelDir = boneVel.sqrMagnitude > 1e-4f ? boneVel.normalized : Vector3.zero;
                    Vector3 push = (d / dist * 0.3f + boneVelDir * 0.7f) * disturbForce * falloff;
                    buffer[i].velocity += push * dt * 5f;
                }
            }
        }
        ps.SetParticles(buffer, count);
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
