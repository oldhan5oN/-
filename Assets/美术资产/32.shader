  Shader "Custom/ShanghaiToonURP"
  {
      Properties
      {
          [Header(Base Color)]
          _BaseMap ("Base Texture", 2D) = "white" {}
          _BaseColor ("Base Color", Color) = (1,1,1,1)

          [Header(Cel Shading)]
          _ShadowColor ("Shadow Color", Color) = (0.55, 0.45, 0.6, 1)
          _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.1
          _ShadowSmooth ("Shadow Edge Softness", Range(0, 0.2)) = 0.005
          _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.85

          [Header(Highlight)]
          _HighlightColor ("Highlight Color", Color) = (1.1, 1.05, 0.9, 1)
          _HighlightThreshold ("Highlight Threshold", Range(0,1)) = 0.85
          _HighlightStrength ("Highlight Strength", Range(0,1)) = 0.3

          [Header(Rim)]
          _RimColor ("Rim Color", Color) = (1, 0.9, 0.7, 1)
          _RimPower ("Rim Power", Range(0.1, 10)) = 4
          _RimStrength ("Rim Strength", Range(0,1)) = 0.0

          [Header(Outline)]
          _OutlineColor ("Outline Color", Color) = (0.12, 0.08, 0.05, 1)
          _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.008
          _OutlineNoiseScale ("Hand-Drawn Noise Scale", Range(0, 50)) = 12
          _OutlineNoiseStrength ("Hand-Drawn Noise Strength", Range(0, 1)) = 0.45
          _OutlineWobble ("Outline Wobble", Range(0, 1)) = 0.25
      }

      SubShader
      {
          Tags
          {
              "RenderType" = "Opaque"
              "RenderPipeline" = "UniversalPipeline"
              "Queue" = "Geometry"
          }

          // ============================================================
          // PASS 1 : OUTLINE (inverted hull + hand-drawn noise wobble)
          // ============================================================
          Pass
          {
              Name "Outline"
              Cull Front
              ZWrite On

              HLSLPROGRAM
              #pragma vertex OutlineVert
              #pragma fragment OutlineFrag

              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

              CBUFFER_START(UnityPerMaterial)
                  float4 _BaseMap_ST;
                  float4 _BaseColor;
                  float4 _ShadowColor;
                  float  _ShadowThreshold;
                  float  _ShadowSmooth;
                  float  _ShadowStrength;
                  float4 _HighlightColor;
                  float  _HighlightThreshold;
                  float  _HighlightStrength;
                  float4 _RimColor;
                  float  _RimPower;
                  float  _RimStrength;
                  float4 _OutlineColor;
                  float  _OutlineWidth;
                  float  _OutlineNoiseScale;
                  float  _OutlineNoiseStrength;
                  float  _OutlineWobble;
              CBUFFER_END

              struct Attributes
              {
                  float4 positionOS : POSITION;
                  float3 normalOS   : NORMAL;
                  float2 uv         : TEXCOORD0;
              };

              struct Varyings
              {
                  float4 positionCS : SV_POSITION;
              };

              // Cheap hash noise for hand-drawn jitter
              float hash31(float3 p)
              {
                  p = frac(p * float3(443.897, 441.423, 437.195));
                  p += dot(p, p.yzx + 19.19);
                  return frac((p.x + p.y) * p.z);
              }

              float valueNoise(float3 p)
              {
                  float3 i = floor(p);
                  float3 f = frac(p);
                  f = f * f * (3.0 - 2.0 * f);
                  float n000 = hash31(i + float3(0,0,0));
                  float n100 = hash31(i + float3(1,0,0));
                  float n010 = hash31(i + float3(0,1,0));
                  float n110 = hash31(i + float3(1,1,0));
                  float n001 = hash31(i + float3(0,0,1));
                  float n101 = hash31(i + float3(1,0,1));
                  float n011 = hash31(i + float3(0,1,1));
                  float n111 = hash31(i + float3(1,1,1));
                  float nx00 = lerp(n000, n100, f.x);
                  float nx10 = lerp(n010, n110, f.x);
                  float nx01 = lerp(n001, n101, f.x);
                  float nx11 = lerp(n011, n111, f.x);
                  float nxy0 = lerp(nx00, nx10, f.y);
                  float nxy1 = lerp(nx01, nx11, f.y);
                  return lerp(nxy0, nxy1, f.z);
              }

              Varyings OutlineVert(Attributes IN)
              {
                  Varyings OUT;

                  // Hand-drawn jitter: per-vertex noise modulates outline thickness
                  float3 nPos = IN.positionOS.xyz * _OutlineNoiseScale;
                  float n = valueNoise(nPos) * 2.0 - 1.0;          // -1..1
                  float n2 = valueNoise(nPos * 2.37 + 7.13) * 2.0 - 1.0;

                  // Per-vertex thickness variation (rough ink line)
                  float widthMul = 1.0 + n * _OutlineNoiseStrength;
                  widthMul = max(widthMul, 0.0);

                  // Wobble: jitter the normal direction slightly so the hull isn't smooth
                  float3 wobble = float3(n, n2, n * n2) * _OutlineWobble * 0.15;
                  float3 normalOS = normalize(IN.normalOS + wobble);

                  float3 posOS = IN.positionOS.xyz + normalOS * _OutlineWidth * widthMul;

                  OUT.positionCS = TransformObjectToHClip(posOS);
                  return OUT;
              }

              half4 OutlineFrag(Varyings IN) : SV_Target
              {
                  return _OutlineColor;
              }
              ENDHLSL
          }

          // ============================================================
          // PASS 2 : MAIN  (flat blocking + hard-edge shadow)
          // ============================================================
          Pass
          {
              Name "ForwardLit"
              Tags { "LightMode" = "UniversalForward" }
              Cull Back
              ZWrite On

              HLSLPROGRAM
              #pragma vertex Vert
              #pragma fragment Frag

              #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
              #pragma multi_compile _ _SHADOWS_SOFT
              #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
              #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
              #pragma multi_compile_fog

              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

              CBUFFER_START(UnityPerMaterial)
                  float4 _BaseMap_ST;
                  float4 _BaseColor;
                  float4 _ShadowColor;
                  float  _ShadowThreshold;
                  float  _ShadowSmooth;
                  float  _ShadowStrength;
                  float4 _HighlightColor;
                  float  _HighlightThreshold;
                  float  _HighlightStrength;
                  float4 _RimColor;
                  float  _RimPower;
                  float  _RimStrength;
                  float4 _OutlineColor;
                  float  _OutlineWidth;
                  float  _OutlineNoiseScale;
                  float  _OutlineNoiseStrength;
                  float  _OutlineWobble;
              CBUFFER_END

              TEXTURE2D(_BaseMap);
              SAMPLER(sampler_BaseMap);

              struct Attributes
              {
                  float4 positionOS : POSITION;
                  float3 normalOS   : NORMAL;
                  float2 uv         : TEXCOORD0;
              };

              struct Varyings
              {
                  float4 positionCS  : SV_POSITION;
                  float2 uv          : TEXCOORD0;
                  float3 normalWS    : TEXCOORD1;
                  float3 positionWS  : TEXCOORD2;
                  float4 shadowCoord : TEXCOORD3;
                  float  fogCoord    : TEXCOORD4;
              };

              Varyings Vert(Attributes IN)
              {
                  Varyings OUT;
                  VertexPositionInputs vInput = GetVertexPositionInputs(IN.positionOS.xyz);
                  VertexNormalInputs   nInput = GetVertexNormalInputs(IN.normalOS);

                  OUT.positionCS  = vInput.positionCS;
                  OUT.positionWS  = vInput.positionWS;
                  OUT.normalWS    = nInput.normalWS;
                  OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                  OUT.shadowCoord = GetShadowCoord(vInput);
                  OUT.fogCoord    = ComputeFogFactor(vInput.positionCS.z);
                  return OUT;
              }

              half4 Frag(Varyings IN) : SV_Target
              {
                  float3 N = normalize(IN.normalWS);

                  Light mainLight = GetMainLight(IN.shadowCoord);
                  float3 L = normalize(mainLight.direction);

                  float NdotL = dot(N, L);
                  float shadowAtten = mainLight.shadowAttenuation;

                  // Hard-edge cartoon shadow (smoothstep gives a clean, controllable ink edge)
                  float lit = smoothstep(_ShadowThreshold - _ShadowSmooth,
                                         _ShadowThreshold + _ShadowSmooth,
                                         NdotL);
                  lit *= shadowAtten;

                  half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                  float3 albedo = baseTex.rgb * _BaseColor.rgb;

                  // Two-tone flat blocking: lit area = albedo, shadow = albedo * shadowColor
                  float3 shadowed = albedo * _ShadowColor.rgb;
                  float3 color    = lerp(shadowed, albedo, lerp(1.0, lit, _ShadowStrength));

                  // Apply main light color/intensity (kept subtle to preserve flat look)
                  color *= lerp(1.0, mainLight.color.rgb, 0.5);

                  // Hard highlight band (optional specular block)
                  if (_HighlightStrength > 0.001)
                  {
                      float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                      float3 H = normalize(L + V);
                      float NdotH = saturate(dot(N, H));
                      float hi = step(_HighlightThreshold, NdotH) * lit;
                      color = lerp(color, _HighlightColor.rgb, hi * _HighlightStrength);
                  }

                  // Soft warm rim (kept off by default - 80s style usually omits it)
                  if (_RimStrength > 0.001)
                  {
                      float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                      float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                      rim = step(0.5, rim);
                      color = lerp(color, _RimColor.rgb, rim * _RimStrength);
                  }

                  color = MixFog(color, IN.fogCoord);
                  return half4(color, 1.0);
              }
              ENDHLSL
          }

          // ============================================================
          // PASS 3 : SHADOW CASTER
          // ============================================================
          Pass
          {
              Name "ShadowCaster"
              Tags { "LightMode" = "ShadowCaster" }

              ZWrite On
              ZTest LEqual
              ColorMask 0
              Cull Back

              HLSLPROGRAM
              #pragma vertex ShadowVert
              #pragma fragment ShadowFrag

              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

              float3 _LightDirection;

              struct Attributes
              {
                  float4 positionOS : POSITION;
                  float3 normalOS   : NORMAL;
              };

              struct Varyings
              {
                  float4 positionCS : SV_POSITION;
              };

              Varyings ShadowVert(Attributes IN)
              {
                  Varyings OUT;
                  float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                  float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                  float4 posCS    = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));
              #if UNITY_REVERSED_Z
                  posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
              #else
                  posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
              #endif
                  OUT.positionCS = posCS;
                  return OUT;
              }

              half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
              ENDHLSL
          }

          // ============================================================
          // PASS 4 : DEPTH ONLY (for SSAO / depth texture)
          // ============================================================
          Pass
          {
              Name "DepthOnly"
              Tags { "LightMode" = "DepthOnly" }
              ZWrite On
              ColorMask 0

              HLSLPROGRAM
              #pragma vertex DepthVert
              #pragma fragment DepthFrag
              #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

              struct Attributes { float4 positionOS : POSITION; };
              struct Varyings   { float4 positionCS : SV_POSITION; };

              Varyings DepthVert(Attributes IN)
              {
                  Varyings OUT;
                  OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                  return OUT;
              }
              half4 DepthFrag(Varyings IN) : SV_Target { return 0; }
              ENDHLSL
          }
      }

      FallBack "Hide"
  }