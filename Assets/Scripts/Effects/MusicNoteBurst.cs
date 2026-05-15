using UnityEngine;

namespace MusicFX
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Effects/Music Note Burst")]
    public class MusicNoteBurst : MonoBehaviour
    {
        [Header("贴图（多张手绘音符 PNG，运行时随机抽取）")]
        [SerializeField] private Sprite[] noteSprites;

        [Header("粒子材质（留空自动创建 URP 透明粒子材质）")]
        [SerializeField] private Material particleMaterial;

        [Header("迸发参数")]
        [SerializeField, Min(1)] private int burstCount = 30;
        [SerializeField] private Vector2 speedRange = new Vector2(3f, 7f);
        [SerializeField] private Vector2 lifetimeRange = new Vector2(1.2f, 2.2f);
        [SerializeField] private Vector2 sizeRange = new Vector2(0.4f, 1.0f);
        [SerializeField] private Vector2 startRotationDegRange = new Vector2(-180f, 180f);
        [SerializeField] private Vector2 rotationSpeedDegRange = new Vector2(-180f, 180f);

        [Header("彩带飘散感")]
        [Tooltip("重力倍率：负=上浮，正=下坠")]
        [SerializeField] private float gravityModifier = -0.25f;
        [Tooltip("速度阻尼 0~1：越大越快减速")]
        [SerializeField, Range(0f, 1f)] private float velocityDamping = 0.35f;
        [Tooltip("末段持续向上的微力（让音符飘）")]
        [SerializeField] private float lateUpwardForce = 1.2f;

        [Header("视觉")]
        [SerializeField] private bool useAdditive = false;
        [SerializeField] private string sortingLayer = "Default";
        [SerializeField] private int orderInLayer = 100;

        [Header("自动播放（调试用）")]
        [SerializeField] private bool playOnStart = false;

        private ParticleSystem ps;
        private ParticleSystemRenderer psr;

        private void Awake() { BuildParticleSystem(); }
        private void Start() { if (playOnStart) Play(); }

        public void Play()
        {
            if (ps == null) BuildParticleSystem();
            ps.Emit(burstCount);
        }

        public void PlayAt(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            Play();
        }

        public void Rebuild() { BuildParticleSystem(); }

        private void BuildParticleSystem()
        {
            ps = GetComponent<ParticleSystem>();
            if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
            psr = GetComponent<ParticleSystemRenderer>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(
                startRotationDegRange.x * Mathf.Deg2Rad,
                startRotationDegRange.y * Mathf.Deg2Rad);
            main.gravityModifier = gravityModifier;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            shape.randomDirectionAmount = 1f;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = velocityDamping > 0f;
            limit.dampen = velocityDamping;
            limit.limit = new ParticleSystem.MinMaxCurve(Mathf.Max(0.01f, speedRange.y));

            var force = ps.forceOverLifetime;
            force.enabled = Mathf.Abs(lateUpwardForce) > 0.001f;
            var ramp = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.4f, 0.3f),
                new Keyframe(1f, 1f));
            force.y = new ParticleSystem.MinMaxCurve(lateUpwardForce, ramp);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(
                rotationSpeedDegRange.x * Mathf.Deg2Rad,
                rotationSpeedDegRange.y * Mathf.Deg2Rad);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(1f, 0.65f),
                    new GradientAlphaKey(0f, 1f),
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.15f, 1f),
                new Keyframe(1f, 0.85f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;
            for (int i = tsa.spriteCount - 1; i >= 0; i--) tsa.RemoveSprite(i);
            int valid = 0;
            if (noteSprites != null)
            {
                for (int i = 0; i < noteSprites.Length; i++)
                {
                    if (noteSprites[i] == null) continue;
                    tsa.AddSprite(noteSprites[i]);
                    valid++;
                }
            }
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            float maxFrame = Mathf.Max(0f, valid - 0.0001f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, maxFrame);

            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.alignment = ParticleSystemRenderSpace.View;
            psr.sortingLayerName = sortingLayer;
            psr.sortingOrder = orderInLayer;
            psr.material = particleMaterial != null ? particleMaterial : CreateDefaultMaterial();
        }

        private Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "MusicNote_Particles_Auto" };
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend"))   m.SetFloat("_Blend", useAdditive ? 1f : 0f);
            if (m.HasProperty("_ZWrite"))  m.SetFloat("_ZWrite", 0f);
            return m;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Play")]
        private void TestPlay()
        {
            if (!Application.isPlaying) BuildParticleSystem();
            Play();
        }
#endif
    }
}
