Shader "Custom/ShanghaiToonURP_Wear"
{
    Properties
    {
        // ========== BASE / CEL (沿用之前的核心控件) ==========
        [Header(Base Color)]
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Cel Shading)]
        _ShadowColor ("Shadow Color", Color) = (0.55, 0.45, 0.6, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.1
        _ShadowSmooth ("Shadow Edge Softness", Range(0, 0.2)) = 0.005
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.85

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.12, 0.08, 0.05, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.008
        _OutlineNoiseScale ("Hand Drawn Noise Scale", Range(0, 50)) = 12
        _OutlineNoiseStrength ("Hand Drawn Noise Strength", Range(0, 1)) = 0.45

        // ========== 三个全局滑杆 (只作用于服装) ==========
        [Header(Global Clothing Aging)]
        _ClothingMask ("Clothing Mask R Channel", 2D) = "white" {}
        _VintageTextureIntensity ("Vintage Texture Intensity", Range(0, 1)) = 0.0
        _AgingTint ("Aging Tint", Range(0, 1)) = 0.0
        _WearAmount ("Wear Amount", Range(0, 1)) = 0.0

        [Header(Aging Style Tuning)]
        _AgingTintColor ("Aging Tint Color", Color) = (0.86, 0.74, 0.52, 1)
        _WearDirtColor ("Wear Dirt Color", Color) = (0.32, 0.26, 0.20, 1)
        _TextureScale ("Texture Grain Scale", Range(0.5, 12)) = 4.0
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
        // PASS 1 : OUTLINE
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
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineNoiseScale;
                float  _OutlineNoiseStrength;
                float4 _ClothingMask_ST;
                float  _VintageTextureIntensity;
                float  _AgingTint;
                float  _WearAmount;
                float4 _AgingTintColor;
                float4 _WearDirtColor;
                float  _TextureScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            float hash31(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                float n = hash31(IN.positionOS.xyz * _OutlineNoiseScale) * 2.0 - 1.0;
                float widthMul = max(1.0 + n * _OutlineNoiseStrength, 0.0);
                float3 posOS = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth * widthMul;
                OUT.positionCS = TransformObjectToHClip(posOS);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                // 描边随旧化色调一起轻微变暖
                float3 col = lerp(_OutlineColor.rgb, _OutlineColor.rgb * _AgingTintColor.rgb, _AgingTint * 0.5);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ============================================================
        // PASS 2 : MAIN
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
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineNoiseScale;
                float  _OutlineNoiseStrength;
                float4 _ClothingMask_ST;
                float  _VintageTextureIntensity;
                float  _AgingTint;
                float  _WearAmount;
                float4 _AgingTintColor;
                float4 _WearDirtColor;
                float  _TextureScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ClothingMask);  SAMPLER(sampler_ClothingMask);

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

            float hash21(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }

            float valueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 多倍频纸纹 + 横向纤维（大闹天宫赛璐璐底纸感）
            float clothGrain(float2 uv)
            {
                float n = 0.0, amp = 0.5, freq = 1.0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    n += valueNoise2D(uv * freq) * amp;
                    freq *= 2.13; amp *= 0.5;
                }
                float fib = valueNoise2D(float2(uv.x * 0.4, uv.y * 26.0));
                return saturate(n * 0.7 + fib * 0.3);
            }

            // 大斑块脏污噪声（磨损用）
            float dirtNoise(float2 uv)
            {
                float a = valueNoise2D(uv * 1.7);
                float b = valueNoise2D(uv * 4.3 + 13.0);
                return saturate(a * 0.6 + b * 0.4);
            }

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

                // ----- 基础色 -----
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;

                // ----- 服装遮罩（白=服装，黑=皮肤/配饰，保持原貌） -----
                float clothMask = SAMPLE_TEXTURE2D(_ClothingMask, sampler_ClothingMask,
                                                   TRANSFORM_TEX(IN.uv, _ClothingMask)).r;

                // ===== ① 复古肌理强度：纸纹 / 布纹颗粒 =====
                if (_VintageTextureIntensity > 0.001)
                {
                    float g = clothGrain(IN.uv * _TextureScale);
                    float grainMul = lerp(1.0, g * 0.55 + 0.7, _VintageTextureIntensity * clothMask);
                    albedo *= grainMul;
                }

                // ===== ② 旧化色调：暖黄/牛皮纸偏色 + 微降饱和 =====
                if (_AgingTint > 0.001)
                {
                    float luma = dot(albedo, float3(0.2126, 0.7152, 0.0722));
                    float3 desat = lerp(albedo, luma.xxx, 0.35);
                    float3 tinted = desat * _AgingTintColor.rgb;
                    albedo = lerp(albedo, tinted, _AgingTint * clothMask);
                }

                // ===== ③ 磨损程度：脏斑 + 边缘掉色 + 整体褪色 =====
                if (_WearAmount > 0.001)
                {
                    float dirt = dirtNoise(IN.uv * _TextureScale * 0.6);
                    // 把脏斑做成阈值斑块，避免均匀变脏（更接近老胶片的污渍）
                    float dirtMask = smoothstep(0.55, 0.85, dirt) * _WearAmount * clothMask;
                    albedo = lerp(albedo, _WearDirtColor.rgb, dirtMask * 0.6);

                    // 边缘菲涅尔掉色（衣物折角处磨白）
                    float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                    float fres = pow(1.0 - saturate(dot(N, V)), 2.5);
                    float fade = fres * _WearAmount * clothMask;
                    float luma2 = dot(albedo, float3(0.2126, 0.7152, 0.0722));
                    albedo = lerp(albedo, lerp(albedo, luma2.xxx * 0.9, 0.7), fade);
                }

                // ----- 硬边赛璐璐光照（保持原风格）-----
                float lit = smoothstep(_ShadowThreshold - _ShadowSmooth,
                                       _ShadowThreshold + _ShadowSmooth, NdotL);
                lit *= mainLight.shadowAttenuation;

                float3 shadowed = albedo * _ShadowColor.rgb;
                float3 color    = lerp(shadowed, albedo, lerp(1.0, lit, _ShadowStrength));
                color *= lerp(1.0, mainLight.color.rgb, 0.5);

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
            ZWrite On  ZTest LEqual  ColorMask 0  Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

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
        // PASS 4 : DEPTH ONLY
        // ============================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On  ColorMask 0

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
