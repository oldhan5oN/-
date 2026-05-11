  using UnityEngine;
  using UnityEngine.Rendering.Universal;

  [ExecuteAlways]
  [RequireComponent(typeof(Light))]
  [AddComponentMenu("Stage/Stage Spotlight Follower (URP)")]
  public class StageSpotlightFollower : MonoBehaviour
  {
      [Header("跟随目标")]
      [Tooltip("被照亮的角色根节点")]
      public Transform target;
      [Tooltip("照射点相对角色根的高度补偿，一般填角色身高的 60%~80%（胸口位置）")]
      public float aimHeight = 1.1f;

      [Header("位置 / 方位")]
      [Tooltip("水平方位角，0=正前方，正值=向左偏。45 即题目要求的左斜前方")]
      [Range(-180f, 180f)] public float azimuthAngle = 45f;
      [Tooltip("距离角色的水平距离")]
      [Range(0.5f, 20f)] public float distance = 4f;
      [Tooltip("灯位高度偏移，越大俯角越大")]
      [Range(0f, 15f)] public float heightOffset = 5f;
      [Tooltip("跟随阻尼，0=瞬时跟随，越大越拖")]
      [Range(0f, 0.5f)] public float followDamp = 0.05f;

      [Header("灯光 / 色彩")]
      [Tooltip("亮度（URP 透视相机推荐 3~6，避免脸部过曝）")]
      [Range(0f, 20f)] public float intensity = 4.5f;
      [Tooltip("暖色调强度：0=纯白，1=完全暖黄")]
      [Range(0f, 1f)] public float warmSaturation = 0.45f;
      [Tooltip("最暖时的基色（建议 3000K~3500K 的暖白）")]
      [ColorUsage(false, false)] public Color warmTint = new Color(1f, 0.86f, 0.68f);

      [Header("聚光形状 / 羽化")]
      [Tooltip("聚光灯外角（覆盖范围）")]
      [Range(10f, 120f)] public float outerAngle = 55f;
      [Tooltip("外沿柔化范围。0=硬边切；1=整个光锥都是渐变（推荐 0.75~0.95）")]
      [Range(0f, 1f)] public float edgeSoftness = 0.88f;
      [Tooltip("光的最大有效距离")]
      [Range(1f, 100f)] public float range = 25f;

      [Header("阴影")]
      [Tooltip("影子浓度。1=最深，0.5~0.8 更适合舞台感")]
      [Range(0f, 1f)] public float shadowStrength = 0.7f;
      [Tooltip("阴影深度偏移，防止自阴影瑕疵")]
      [Range(0f, 2f)] public float shadowBias = 0.15f;
      [Tooltip("阴影法线偏移，柔化轮廓抖动")]
      [Range(0f, 5f)] public float shadowNormalBias = 0.6f;
      [Tooltip("自定义阴影贴图分辨率，越高轮廓越锐（512/1024/2048/4096）")]
      public int shadowResolution = 2048;

      Light _light;
      UniversalAdditionalLightData _urpData;
      Vector3 _vel;

      void OnEnable()  => Cache();
      void Reset()     => Cache();

      void Cache()
      {
          _light = GetComponent<Light>();
          _light.type = LightType.Spot;
          if (!TryGetComponent(out _urpData))
              _urpData = gameObject.AddComponent<UniversalAdditionalLightData>();
      }

      void LateUpdate()
      {
          if (_light == null) Cache();
          if (target == null) return;

          FollowTarget();
          ApplyLightParams();
      }

      void FollowTarget()
      {
          // 角色面朝方向作为参考，绕 Y 轴旋转 azimuthAngle 得到水平方位
          Vector3 charForward = target.forward; charForward.y = 0f;
          if (charForward.sqrMagnitude < 1e-4f) charForward = Vector3.forward;
          charForward.Normalize();

          Vector3 horizontalDir = Quaternion.AngleAxis(azimuthAngle, Vector3.up) * charForward;
          Vector3 aimPoint  = target.position + Vector3.up * aimHeight;
          Vector3 desiredPos = aimPoint + horizontalDir * distance + Vector3.up * heightOffset;

          transform.position = followDamp <= 0f
              ? desiredPos
              : Vector3.SmoothDamp(transform.position, desiredPos, ref _vel, followDamp);

          transform.rotation = Quaternion.LookRotation((aimPoint - transform.position).normalized, Vector3.up);
      }

      void ApplyLightParams()
      {
          _light.intensity   = intensity;
          _light.range       = range;
          _light.spotAngle   = outerAngle;
          // 内角越小 → 羽化区越大 → 边界越糊
          _light.innerSpotAngle = Mathf.Max(1f, outerAngle * (1f - edgeSoftness));

          _light.color = Color.Lerp(Color.white, warmTint, warmSaturation);

          _light.shadows           = shadowStrength > 0.001f ? LightShadows.Soft : LightShadows.None;
          _light.shadowStrength    = shadowStrength;
          _light.shadowBias        = shadowBias;
          _light.shadowNormalBias  = shadowNormalBias;
          _light.shadowResolution  = UnityEngine.Rendering.LightShadowResolution.FromQualitySettings;
          _light.shadowCustomResolution = Mathf.Max(128, shadowResolution);

          if (_urpData != null)
          {
              _urpData.usePipelineSettings = false;
              _urpData.lightCookieSize = Vector2.one;
          }
      }

  #if UNITY_EDITOR
      void OnDrawGizmosSelected()
      {
          if (target == null) return;
          Gizmos.color = new Color(1f, 0.85f, 0.5f, 0.6f);
          Gizmos.DrawLine(transform.position, target.position + Vector3.up * aimHeight);
      }
  #endif
  }