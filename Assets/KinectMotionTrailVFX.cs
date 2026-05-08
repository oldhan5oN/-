using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class KinectMotionTrailVFX : MonoBehaviour
{
    public Animator animator;
    public KinectManager kinectManager;

    [Header("Tracked Bones")]
    public HumanBodyBones[] trailBones = {
        HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
        HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
        HumanBodyBones.Head
    };

    [Header("Trail Look")]
    public Color trailColor      = new Color(1f, 0.95f, 0.8f, 0.6f);
    public float baseWidth       = 0.05f;
    public float minLifetime     = 0.05f;
    public float maxLifetime     = 0.8f;
    public float speedToLifetime = 0.18f;
    public float fadeOutSpeed    = 4f;
    public float growSpeed       = 10f;
    public Material trailMaterial;

    class TrailEntry
    {
        public Transform bone;
        public TrailRenderer renderer;
        public Vector3 lastPos;
        public float currentLifetime;
    }
    readonly List<TrailEntry> entries = new List<TrailEntry>();

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (animator == null) { enabled = false; return; }
        Material mat = trailMaterial != null ? trailMaterial : CreateDefaultMaterial();

        foreach (var hb in trailBones)
        {
            Transform t = animator.GetBoneTransform(hb);
            if (t == null) continue;

            var go = new GameObject("Trail_" + hb);
            go.transform.SetParent(t, false);
            go.transform.localPosition = Vector3.zero;

            var tr = go.AddComponent<TrailRenderer>();
            tr.time = minLifetime;
            tr.startWidth = baseWidth;
            tr.endWidth = 0f;
            tr.material = mat;
            tr.minVertexDistance = 0.005f;
            tr.numCornerVertices = 2;
            tr.numCapVertices = 2;
            tr.emitting = true;
            tr.startColor = trailColor;
            tr.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);

            entries.Add(new TrailEntry {
                bone = t, renderer = tr, lastPos = t.position, currentLifetime = minLifetime
            });
        }
    }

    void LateUpdate()
    {
        if (animator == null) return;
        bool active = kinectManager == null || kinectManager.enabled;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            float speed = (e.bone.position - e.lastPos).magnitude / dt;
            e.lastPos = e.bone.position;

            float target = active
                ? Mathf.Clamp(minLifetime + speed * speedToLifetime, minLifetime, maxLifetime)
                : minLifetime;

            float k = (target > e.currentLifetime ? growSpeed : fadeOutSpeed) * dt;
            e.currentLifetime = Mathf.Lerp(e.currentLifetime, target, Mathf.Clamp01(k));
            e.renderer.time = e.currentLifetime;

            float w = baseWidth * Mathf.Lerp(0.6f, 1.6f, Mathf.InverseLerp(0f, 5f, speed));
            e.renderer.startWidth = w;

            float a = trailColor.a * Mathf.Clamp01(speed * 0.6f + 0.15f);
            var c = trailColor; c.a = a;
            e.renderer.startColor = c;
            var endC = c; endC.a = 0f;
            e.renderer.endColor = endC;
        }
    }

    static Material CreateDefaultMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        var m = new Material(sh);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        return m;
    }
}
