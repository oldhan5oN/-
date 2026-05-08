Shader "Custom/ShanghaiToonURP_Master"
{
    Properties
    {
        // ============================================================
        // 1. BASE
        // ============================================================
        [Header(_____ 1 BASE _____)]
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _OverallBrightness ("Overall Brightness", Range(0, 2)) = 1.0

        // ============================================================
        // 2. THREE TONE CEL SHADING (Light / Mid / Shadow)
        // ============================================================
        [Header(_____ 2 THREE TONE CEL _____)]
        _LightColor ("Light Tone Color", Color) = (1.00, 0.98, 0.92, 1)
        _MidColor   ("Mid Tone Color",   Color) = (0.78, 0.72, 0.70, 1)
        _ShadowColor("Shadow Tone Color",Color) = (0.45, 0.38, 0.50, 1)
        _ThresholdLight ("Light Threshold (Mid -> Light)", Range(-1, 1)) = 0.55
        _ThresholdShadow ("Shadow Threshold (Shadow -> Mid)", Range(-1, 1)) = 0.05
        _ToneSmooth ("Tone Edge Softness", Range(0, 0.2)) = 0.008
        _ToneStrength ("Three Tone Strength", Range(0, 1)) = 1.0

        // ============================================================
        // 3. RAMP MAP (optional gradient lookup)
        // ============================================================
        [Header(_____ 3 RAMP MAP _____)]
        _UseRampMap ("Use Ramp Map 0 Off 1 On", Range(0, 1)) = 0
        _RampTex ("Ramp Texture", 2D) = "white" {}
        _RampStrength ("Ramp Blend Strength", Range(0, 1)) = 1.0

        // ============================================================
        // 4. WARM / COOL KEY LIGHT
        // ============================================================
        [Header(_____ 4 WARM COOL KEY LIGHT _____)]
        _WarmTint ("Warm Tint (lit side)", Color) = (1.10, 0.95, 0.80, 1)
        _CoolTint ("Cool Tint (shadow side)", Color) = (0.78, 0.85, 1.05, 1)
        _WarmCoolStrength ("Warm Cool Strength", Range(0, 1)) = 0.5

        // ============================================================
        // 5. OUTLINE
        // ============================================================
        [Header(_____ 5 OUTLINE _____)]
        _OutlineColor ("Outline Color", Color) = (0.10, 0.07, 0.05, 1)
        _OutlineWarmCool ("Outline Warm Cool Bias", Range(-1, 1)) = 0.0
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.008
        _OutlineNoiseScale ("Outline Noise Scale", Range(0, 50)) = 12
        _OutlineNoiseStrength ("Outline Noise Strength", Range(0, 1)) = 0.45

        // ============================================================
        // 6. SOFT OUTLINE HALO  (silhouette glow)
        // ============================================================
        [Header(_____ 6 SOFT OUTLINE HALO _____)]
        _HaloEnable ("Halo Enable 0 Off 1 On", Range(0, 1)) = 1
        _HaloColor ("Halo Color", Color) = (1.0, 0.92, 0.7, 1)
        _HaloStrength ("Halo Strength", Range(0, 3)) = 0.8
        _HaloPower ("Halo Fresnel Power", Range(0.5, 8)) = 2.2
        _HaloSoftness ("Halo Softness", Range(0.01, 1)) = 0.55

        // ============================================================
        // 7. HALFTONE  (two independent textures)
        // ============================================================
        [Header(_____ 7 HALFTONE MASTER _____)]
        _HalftoneEnable ("Halftone Enable 0 Off 1 On", Range(0, 1)) = 1
        _HalftoneOpacity ("Halftone Opacity", Range(0, 1)) = 0.85
        _HalftoneDarken ("Halftone Darken Factor", Range(0, 1)) = 0.55
        _HalftoneShadowCoverage ("Shadow Dot Coverage", Range(0, 1)) = 0.9
        _HalftoneMidCoverage ("Mid Dot Coverage", Range(0, 1)) = 0.5
        _HalftoneHighlightCoverage ("Highlight Dot Coverage", Range(0, 1)) = 0.05
        _HalftoneEdgeSoftness ("Halftone Edge Softness", Range(0.001, 0.3)) = 0.06
        _HalftoneRotation ("Halftone Grid Rotation Deg", Range(0, 90)) = 27

        [Header(_____ 7a SKIN MASK _____)]
        _SkinMask ("Skin Mask R Channel", 2D) = "black" {}

        [Header(_____ 7b SKIN HALFTONE TEX _____)]
        _SkinHalftoneTex ("Skin Halftone Pattern", 2D) = "white" {}
        _SkinHalftoneStrength ("Skin Halftone Strength", Range(0, 1)) = 0.30
        _SkinHalftoneScale ("Skin Halftone Scale", Range(20, 600)) = 260
        _SkinHalftoneHueShift ("Skin Halftone Hue Shift", Range(-0.5, 0.5)) = 0.0

        [Header(_____ 7c CLOTH HALFTONE TEX _____)]
        _ClothHalftoneTex ("Cloth Halftone Pattern", 2D) = "white" {}
        _ClothHalftoneStrength ("Cloth Halftone Strength", Range(0, 1)) = 0.85
        _ClothHalftoneScale ("Cloth Halftone Scale", Range(20, 400)) = 120
        _ClothHalftoneHueShift ("Cloth Halftone Hue Shift", Range(-0.5, 0.5)) = 0.0

        // ============================================================
        // 8. PAPER GRAIN
        // ============================================================
        [Header(_____ 8 PAPER GRAIN _____)]
        _PaperGrainStrength ("Paper Grain Strength", Range(0, 1)) = 0.0
        _PaperGrainScale ("Paper Grain Scale", Range(1, 20)) = 6.0
        _PaperGrainTint ("Paper Grain Tint", Color) = (0.92, 0.85, 0.68, 1)

        // ============================================================
        // 9. SHANGHAI RETRO FILTER
        // ============================================================
        [Header(_____ 9 SHANGHAI RETRO FILTER _____)]
        _RetroFilterStrength ("Retro Filter Strength", Range(0, 1)) = 0.0
        _RetroSatBoost ("Retro Saturation Boost", Range(1, 2)) = 1.5
        _RetroWarmShift ("Retro Warm Shift", Color) = (1.12, 0.95, 0.78, 1)
        _RetroShadowTint ("Retro Shadow Tint", Color) = (0.7, 0.4, 0.55, 1)

        // ============================================================
        // 10. BACK SELF EMISSION
        // ============================================================
        [Header(_____ 10 BACK SELF EMISSION _____)]
        _BackEmissionStrength ("Back Emission Strength", Range(0, 4)) = 0.0
        _BackEmissionColor ("Back Emission Color", Color) = (1.0, 0.85, 0.55, 1)
        _BackEmissionMask ("Back Emission Mask R Channel", 2D) = "white" {}
        _BackEmissionFalloff ("Back Emission Backside Falloff", Range(0.5, 8)) = 2.5
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
        // PASS 1 : OUTLINE (warm/cool tinted)
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
                float  _OverallBrightness;
                float4 _LightColor;
                float4 _MidColor;
                float4 _ShadowColor;
                float  _ThresholdLight;
                float  _ThresholdShadow;
                float  _ToneSmooth;
                float  _ToneStrength;
                float  _UseRampMap;
                float4 _RampTex_ST;
                float  _RampStrength;
                float4 _WarmTint;
                float4 _CoolTint;
                float  _WarmCoolStrength;
                float4 _OutlineColor;
                float  _OutlineWarmCool;
                float  _OutlineWidth;
                float  _OutlineNoiseScale;
                float  _OutlineNoiseStrength;
                float  _HaloEnable;
                float4 _HaloColor;
                float  _HaloStrength;
                float  _HaloPower;
                float  _HaloSoftness;
                float  _HalftoneEnable;
                float  _HalftoneOpacity;
                float  _HalftoneDarken;
                float  _HalftoneShadowCoverage;
                float  _HalftoneMidCoverage;
                float  _HalftoneHighlightCoverage;
                float  _HalftoneEdgeSoftness;
                float  _HalftoneRotation;
                float4 _SkinMask_ST;
                float4 _SkinHalftoneTex_ST;
                float  _SkinHalftoneStrength;
                float  _SkinHalftoneScale;
                float  _SkinHalftoneHueShift;
                float4 _ClothHalftoneTex_ST;
                float  _ClothHalftoneStrength;
                float  _ClothHalftoneScale;
                float  _ClothHalftoneHueShift;
                float  _PaperGrainStrength;
                float  _PaperGrainScale;
                float4 _PaperGrainTint;
                float  _RetroFilterStrength;
                float  _RetroSatBoost;
                float4 _RetroWarmShift;
                float4 _RetroShadowTint;
                float  _BackEmissionStrength;
                float4 _BackEmissionColor;
                float4 _BackEmissionMask_ST;
                float  _BackEmissionFalloff;
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
                float3 col = _OutlineColor.rgb;
                // Warm/cool bias: -1 = cool (toward _CoolTint), +1 = warm (toward _WarmTint)
                if (_OutlineWarmCool > 0.001)
                    col = lerp(col, col * _WarmTint.rgb, _OutlineWarmCool);
                else if (_OutlineWarmCool < -0.001)
                    col = lerp(col, col * _CoolTint.rgb, -_OutlineWarmCool);
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
                float  _OverallBrightness;
                float4 _LightColor;
                float4 _MidColor;
                float4 _ShadowColor;
                float  _ThresholdLight;
                float  _ThresholdShadow;
                float  _ToneSmooth;
                float  _ToneStrength;
                float  _UseRampMap;
                float4 _RampTex_ST;
                float  _RampStrength;
                float4 _WarmTint;
                float4 _CoolTint;
                float  _WarmCoolStrength;
                float4 _OutlineColor;
                float  _OutlineWarmCool;
                float  _OutlineWidth;
                float  _OutlineNoiseScale;
                float  _OutlineNoiseStrength;
                float  _HaloEnable;
                float4 _HaloColor;
                float  _HaloStrength;
                float  _HaloPower;
                float  _HaloSoftness;
                float  _HalftoneEnable;
                float  _HalftoneOpacity;
                float  _HalftoneDarken;
                float  _HalftoneShadowCoverage;
                float  _HalftoneMidCoverage;
                float  _HalftoneHighlightCoverage;
                float  _HalftoneEdgeSoftness;
                float  _HalftoneRotation;
                float4 _SkinMask_ST;
                float4 _SkinHalftoneTex_ST;
                float  _SkinHalftoneStrength;
                float  _SkinHalftoneScale;
                float  _SkinHalftoneHueShift;
                float4 _ClothHalftoneTex_ST;
                float  _ClothHalftoneStrength;
                float  _ClothHalftoneScale;
                float  _ClothHalftoneHueShift;
                float  _PaperGrainStrength;
                float  _PaperGrainScale;
                float4 _PaperGrainTint;
                float  _RetroFilterStrength;
                float  _RetroSatBoost;
                float4 _RetroWarmShift;
                float4 _RetroShadowTint;
                float  _BackEmissionStrength;
                float4 _BackEmissionColor;
                float4 _BackEmissionMask_ST;
                float  _BackEmissionFalloff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex);          SAMPLER(sampler_RampTex);
            TEXTURE2D(_SkinMask);         SAMPLER(sampler_SkinMask);
            TEXTURE2D(_SkinHalftoneTex);  SAMPLER(sampler_SkinHalftoneTex);
            TEXTURE2D(_ClothHalftoneTex); SAMPLER(sampler_ClothHalftoneTex);
            TEXTURE2D(_BackEmissionMask); SAMPLER(sampler_BackEmissionMask);

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
                float3 normalOS    : TEXCOORD5;
            };

            // ---------- HSV / RGB ----------
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                              d / (q.x + e),
                              q.x);
            }

            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 ApplyHueShift(float3 rgb, float shift)
            {
                if (abs(shift) < 0.0001) return rgb;
                float3 hsv = RGBtoHSV(rgb);
                hsv.x = frac(hsv.x + shift);
                return HSVtoRGB(hsv);
            }

            // ---------- Noise (paper grain) ----------
            float hash21(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }
            float valueNoise2D(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float paperGrain(float2 uv)
            {
                float n = 0.0, amp = 0.5, freq = 1.0;
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    n += valueNoise2D(uv * freq) * amp;
                    freq *= 2.17; amp *= 0.48;
                }
                float fiber = valueNoise2D(float2(uv.x * 0.3, uv.y * 32.0));
                return n * 0.65 + fiber * 0.35;
            }

            // ---------- Halftone helpers ----------
            float2 RotateUV(float2 uv, float rad)
            {
                float c = cos(rad), s = sin(rad);
                return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
            }

            // Sample a halftone pattern texture, then re-shape its values into
            // dot coverage that responds to lighting.
            float HalftoneFromTex(TEXTURE2D_PARAM(tex, smp), float2 uv, float scale,
                                  float rotationRad, float coverage, float edgeSoftness)
            {
                float2 ruv = RotateUV(uv, rotationRad) * scale * 0.01;
                float pat = SAMPLE_TEXTURE2D(tex, smp, ruv).r;
                // Map coverage->threshold: more coverage = lower threshold = more dots
                float thr = 1.0 - saturate(coverage);
                float soft = max(edgeSoftness, 0.001);
                return 1.0 - smoothstep(thr - soft, thr + soft, pat);
            }

            // ---------- Three-tone cel ----------
            float3 ThreeToneCel(float NdotL, float shadowAtten, float3 albedo, out float toneNorm)
            {
                float litMid    = smoothstep(_ThresholdShadow - _ToneSmooth,
                                             _ThresholdShadow + _ToneSmooth, NdotL);
                float litLight  = smoothstep(_ThresholdLight  - _ToneSmooth,
                                             _ThresholdLight  + _ToneSmooth, NdotL);

                litMid   *= shadowAtten;
                litLight *= shadowAtten;

                float3 c = lerp(_ShadowColor.rgb, _MidColor.rgb,  litMid);
                c = lerp(c, _LightColor.rgb, litLight);

                float3 base = albedo * c;
                float3 plain = albedo;
                toneNorm = saturate(litMid * 0.5 + litLight * 0.5);
                return lerp(plain, base, _ToneStrength);
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
                OUT.normalOS    = IN.normalOS;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 L = normalize(mainLight.direction);
                float NdotL = dot(N, L);
                float shadowAtten = mainLight.shadowAttenuation;

                // ---- Albedo ----
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;

                // ============================================================
                // 2. THREE TONE CEL
                // ============================================================
                float toneNorm;
                float3 color = ThreeToneCel(NdotL, shadowAtten, albedo, toneNorm);

                // ============================================================
                // 3. RAMP MAP override (optional)
                // ============================================================
                if (_UseRampMap > 0.5)
                {
                    float rampU = saturate(NdotL * 0.5 + 0.5) * shadowAtten;
                    float3 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampU, 0.5)).rgb;
                    color = lerp(color, albedo * rampCol, _RampStrength);
                }

                // ============================================================
                // 4. WARM / COOL KEY LIGHT
                // ============================================================
                if (_WarmCoolStrength > 0.001)
                {
                    float warmW = saturate(NdotL * 0.5 + 0.5);
                    float3 wc = lerp(_CoolTint.rgb, _WarmTint.rgb, warmW);
                    color = lerp(color, color * wc, _WarmCoolStrength);
                }

                // Apply main light color
                color *= lerp(float3(1,1,1), mainLight.color.rgb, 0.5);

                // ============================================================
                // 7. HALFTONE  (two independent textures, lighting-driven coverage)
                // ============================================================
                if (_HalftoneEnable > 0.5)
                {
                    float lightingNorm = saturate(NdotL * 0.5 + 0.5) * shadowAtten;

                    float coverage;
                    if (lightingNorm < 0.5)
                        coverage = lerp(_HalftoneShadowCoverage, _HalftoneMidCoverage, lightingNorm * 2.0);
                    else
                        coverage = lerp(_HalftoneMidCoverage, _HalftoneHighlightCoverage, (lightingNorm - 0.5) * 2.0);

                    float skinMask = SAMPLE_TEXTURE2D(_SkinMask, sampler_SkinMask,
                                                     TRANSFORM_TEX(IN.uv, _SkinMask)).r;
                    float rotRad = _HalftoneRotation * 0.01745329;

                    float dotSkin = HalftoneFromTex(TEXTURE2D_ARGS(_SkinHalftoneTex, sampler_SkinHalftoneTex),
                                                    IN.uv, _SkinHalftoneScale, rotRad,
                                                    coverage, _HalftoneEdgeSoftness);
                    float dotCloth = HalftoneFromTex(TEXTURE2D_ARGS(_ClothHalftoneTex, sampler_ClothHalftoneTex),
                                                     IN.uv, _ClothHalftoneScale, rotRad,
                                                     coverage, _HalftoneEdgeSoftness);

                    float3 skinDotColor  = ApplyHueShift(color, _SkinHalftoneHueShift)
                                         * (1.0 - _HalftoneDarken * 0.7);
                    float3 clothDotColor = ApplyHueShift(color, _ClothHalftoneHueShift)
                                         * (1.0 - _HalftoneDarken);

                    float skinAlpha  = dotSkin  * _SkinHalftoneStrength  * skinMask         * _HalftoneOpacity;
                    float clothAlpha = dotCloth * _ClothHalftoneStrength * (1.0 - skinMask) * _HalftoneOpacity;

                    color = lerp(color, skinDotColor,  saturate(skinAlpha));
                    color = lerp(color, clothDotColor, saturate(clothAlpha));
                }

                // ============================================================
                // 8. PAPER GRAIN
                // ============================================================
                if (_PaperGrainStrength > 0.001)
                {
                    float g = paperGrain(IN.uv * _PaperGrainScale);
                    float grainMul = lerp(1.0, g * 0.55 + 0.25, _PaperGrainStrength);
                    color *= grainMul;
                    float tintAmount = (1.0 - grainMul) * _PaperGrainStrength * 0.7;
                    color = lerp(color, color * _PaperGrainTint.rgb, tintAmount);
                }

                // ============================================================
                // 9. SHANGHAI RETRO FILTER
                // ============================================================
                if (_RetroFilterStrength > 0.001)
                {
                    float t = _RetroFilterStrength;
                    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                    float3 chroma = color - luma;
                    float satScale = lerp(1.0, _RetroSatBoost, t);
                    float3 boosted = max(luma + chroma * satScale, 0.0);
                    float3 warmed = boosted * lerp(float3(1,1,1), _RetroWarmShift.rgb, t * 0.8);
                    float3 contrasted = smoothstep(lerp(0.0, 0.08, t),
                                                   lerp(1.0, 0.92, t), warmed);
                    float lumaFinal = dot(contrasted, float3(0.2126, 0.7152, 0.0722));
                    float shadowW = smoothstep(0.4, 0.0, lumaFinal) * t * 0.6;
                    contrasted = lerp(contrasted, contrasted * _RetroShadowTint.rgb * 2.0, shadowW);
                    color = lerp(color, contrasted, t);
                }

                // ============================================================
                // 6. SOFT OUTLINE HALO  (additive fresnel halo at silhouette)
                // ============================================================
                if (_HaloEnable > 0.5 && _HaloStrength > 0.001)
                {
                    float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                    float NdotV = saturate(dot(N, V));
                    float fres = pow(1.0 - NdotV, _HaloPower);
                    float halo = smoothstep(1.0 - _HaloSoftness, 1.0, fres);
                    color += _HaloColor.rgb * halo * _HaloStrength;
                }

                // ============================================================
                // 10. BACK SELF EMISSION  (only on faces facing away from light)
                // ============================================================
                if (_BackEmissionStrength > 0.001)
                {
                    float backFactor = pow(saturate(-NdotL), _BackEmissionFalloff);
                    float emissionMask = SAMPLE_TEXTURE2D(_BackEmissionMask, sampler_BackEmissionMask,
                                                         TRANSFORM_TEX(IN.uv, _BackEmissionMask)).r;
                    color += _BackEmissionColor.rgb * backFactor * emissionMask * _BackEmissionStrength;
                }

                // ============================================================
                // 1. OVERALL BRIGHTNESS
                // ============================================================
                color *= _OverallBrightness;

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
