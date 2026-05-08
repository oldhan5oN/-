Shader "Custom/ShanghaiToonURP_V3"
{
    Properties
    {
        // ========== BASE CEL SHADING ==========
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
        _OutlineNoiseScale ("Outline Noise Scale", Range(0, 50)) = 12
        _OutlineNoiseStrength ("Outline Noise Strength", Range(0, 1)) = 0.45

        // ========== 1. PAPER GRAIN ==========
        [Header(Hand Painted Paper Grain)]
        _PaperGrainStrength ("Paper Grain Strength", Range(0, 1)) = 0.0
        _PaperGrainScale ("Paper Grain Scale", Range(1, 20)) = 6.0
        _PaperGrainTint ("Paper Grain Tint", Color) = (0.92, 0.85, 0.68, 1)

        // ========== 2. RETRO FILTER ==========
        [Header(Shanghai Retro Filter)]
        _RetroFilterStrength ("Retro Filter Strength", Range(0, 1)) = 0.0
        _RetroSatBoost ("Retro Saturation Boost", Range(1, 2)) = 1.5
        _RetroWarmShift ("Retro Warm Shift", Color) = (1.12, 0.95, 0.78, 1)
        _RetroShadowTint ("Retro Shadow Tint", Color) = (0.7, 0.4, 0.55, 1)

        // ========== 3. EDGE GLOW ==========
        [Header(Edge Glow Halo)]
        _GlowStrength ("Glow Strength", Range(0, 3)) = 0.0
        _GlowColor ("Glow Color", Color) = (1.0, 0.85, 0.5, 1)
        _GlowPower ("Glow Fresnel Power", Range(0.5, 8)) = 2.5
        _GlowSoftness ("Glow Softness", Range(0.01, 1)) = 0.4
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
                float  _PaperGrainStrength;
                float  _PaperGrainScale;
                float4 _PaperGrainTint;
                float  _RetroFilterStrength;
                float  _RetroSatBoost;
                float4 _RetroWarmShift;
                float4 _RetroShadowTint;
                float  _GlowStrength;
                float4 _GlowColor;
                float  _GlowPower;
                float  _GlowSoftness;
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
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        // ============================================================
        // PASS 2 : MAIN FORWARD LIT
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
                float  _PaperGrainStrength;
                float  _PaperGrainScale;
                float4 _PaperGrainTint;
                float  _RetroFilterStrength;
                float  _RetroSatBoost;
                float4 _RetroWarmShift;
                float4 _RetroShadowTint;
                float  _GlowStrength;
                float4 _GlowColor;
                float  _GlowPower;
                float  _GlowSoftness;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

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

            // ---- Noise helpers ----
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
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 4-octave paper grain with horizontal fiber streaks
            float paperGrain(float2 uv)
            {
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    n += valueNoise2D(uv * freq) * amp;
                    freq *= 2.17;
                    amp *= 0.48;
                }
                // Horizontal fiber streaks (rice paper / xuan paper feel)
                float fiber = valueNoise2D(float2(uv.x * 0.3, uv.y * 32.0));
                n = n * 0.65 + fiber * 0.35;
                return n;
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

                // ---- Base albedo ----
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;

                // ---- Hard-edge cel lighting ----
                float lit = smoothstep(_ShadowThreshold - _ShadowSmooth,
                                       _ShadowThreshold + _ShadowSmooth, NdotL);
                lit *= mainLight.shadowAttenuation;

                float3 shadowed = albedo * _ShadowColor.rgb;
                float3 color = lerp(shadowed, albedo, lerp(1.0, lit, _ShadowStrength));
                color *= lerp(float3(1,1,1), mainLight.color.rgb, 0.5);

                // ============================================================
                // EFFECT 1 : PAPER GRAIN (aggressive range: 0.3 ~ 1.0 multiply)
                // ============================================================
                if (_PaperGrainStrength > 0.001)
                {
                    float2 grainUV = IN.uv * _PaperGrainScale;
                    float g = paperGrain(grainUV);

                    // Wide modulation range: at full strength, darkest spots go to 30% brightness
                    // This makes the grain VERY visible, not subtle
                    float grainMul = lerp(1.0, g * 0.55 + 0.25, _PaperGrainStrength);

                    // Apply grain
                    color *= grainMul;

                    // Tint toward paper color (cream/aged yellow) in the dark grain areas
                    float tintAmount = (1.0 - grainMul) * _PaperGrainStrength * 0.7;
                    color = lerp(color, color * _PaperGrainTint.rgb, tintAmount);
                }

                // ============================================================
                // EFFECT 2 : RETRO FILTER (Shanghai Animation Studio 1980s)
                // ============================================================
                if (_RetroFilterStrength > 0.001)
                {
                    float t = _RetroFilterStrength;

                    // Step A: Boost saturation aggressively (gamma on saturation channel)
                    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                    float3 chroma = color - luma;
                    float satScale = lerp(1.0, _RetroSatBoost, t);
                    float3 boosted = luma + chroma * satScale;
                    boosted = max(boosted, 0.0);

                    // Step B: Warm shift (multiply by warm tint color)
                    float3 warmed = boosted * lerp(float3(1,1,1), _RetroWarmShift.rgb, t * 0.8);

                    // Step C: Contrast enhancement (S-curve via smoothstep)
                    float3 contrasted = smoothstep(
                        lerp(0.0, 0.08, t),
                        lerp(1.0, 0.92, t),
                        warmed
                    );

                    // Step D: Shadow tinting (dark areas get purple-red push)
                    float lumaFinal = dot(contrasted, float3(0.2126, 0.7152, 0.0722));
                    float shadowW = smoothstep(0.4, 0.0, lumaFinal) * t * 0.6;
                    contrasted = lerp(contrasted, contrasted * _RetroShadowTint.rgb * 2.0, shadowW);

                    color = lerp(color, contrasted, t);
                }

                // ============================================================
                // EFFECT 3 : EDGE GLOW HALO (additive fresnel, very visible)
                // ============================================================
                if (_GlowStrength > 0.001)
                {
                    float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                    float NdotV = saturate(dot(N, V));

                    // Fresnel with adjustable power
                    float fresnel = pow(1.0 - NdotV, _GlowPower);

                    // Softness: smoothstep to control the glow falloff width
                    float glowMask = smoothstep(1.0 - _GlowSoftness, 1.0, fresnel);

                    // ADDITIVE blend (not lerp!) — this guarantees visible brightness
                    float3 glow = _GlowColor.rgb * glowMask * _GlowStrength;
                    color += glow;
                }

                color = MixFog(color, IN.fogCoord);
                return half4(saturate(color), 1.0);
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
