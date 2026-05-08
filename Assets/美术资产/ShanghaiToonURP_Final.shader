Shader "Custom/ShanghaiToonURP_Final"
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
        // 2. THREE TONE CEL  (rotatable virtual sun)
        // ============================================================
        [Header(_____ 2 THREE TONE CEL _____)]
        _LightColor ("Light Tone Color",  Color) = (1.00, 0.96, 0.86, 1)
        _MidColor   ("Mid Tone Color",    Color) = (0.82, 0.65, 0.58, 1)
        _ShadowColor("Shadow Tone Color", Color) = (0.42, 0.30, 0.50, 1)
        _ThresholdLight  ("Light Threshold (Mid -> Light)",  Range(-1, 1)) = 0.45
        _ThresholdShadow ("Shadow Threshold (Shadow -> Mid)", Range(-1, 1)) = -0.05
        _ToneSmooth   ("Tone Edge Softness", Range(0, 0.3)) = 0.06
        _ToneStrength ("Three Tone Strength", Range(0, 1)) = 1.0

        [Header(_____ 2a VIRTUAL SUN ROTATION _____)]
        _UseVirtualSun ("Use Virtual Sun 0 Off 1 On", Range(0, 1)) = 1
        _SunYaw   ("Sun Yaw (horizontal degrees)",   Range(0, 360)) = 35
        _SunPitch ("Sun Pitch (vertical degrees)",   Range(-90, 90)) = 25
        _SunFollowsView ("Sun Follows Camera Yaw 0 Off 1 On", Range(0, 1)) = 1
        _SunWrap ("Sun Wrap (soft fill)", Range(0, 1)) = 0.35

        // ============================================================
        // 3. CONTRAST GRADIENT MAP
        // ============================================================
        [Header(_____ 3 CONTRAST GRADIENT _____)]
        _UseGradient ("Use Gradient 0 Off 1 On", Range(0, 1)) = 0
        _GradientShadow   ("Gradient Shadow Color",   Color) = (0.20, 0.15, 0.55, 1)
        _GradientMid      ("Gradient Mid Color",      Color) = (0.85, 0.55, 0.45, 1)
        _GradientHighlight("Gradient Highlight Color",Color) = (1.00, 0.92, 0.55, 1)
        _GradientStrength ("Gradient Strength",       Range(0, 1)) = 0.65
        _GradientContrast ("Gradient Contrast Boost", Range(0, 2)) = 1.0

        // ============================================================
        // 4. WARM / COOL KEY LIGHT
        // ============================================================
        [Header(_____ 4 WARM COOL KEY LIGHT _____)]
        _WarmTint ("Warm Tint (lit side)",   Color) = (1.10, 0.95, 0.80, 1)
        _CoolTint ("Cool Tint (shadow side)",Color) = (0.78, 0.85, 1.05, 1)
        _WarmCoolStrength ("Warm Cool Strength", Range(0, 1)) = 0.5

        // ============================================================
        // 5. OUTLINE
        // ============================================================
        [Header(_____ 5 OUTLINE _____)]
        _OutlineColor ("Outline Color", Color) = (0.10, 0.06, 0.05, 1)
        _OutlineWarmCool ("Outline Warm Cool Bias", Range(-1, 1)) = 0.0
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.012
        _OutlineNoiseScale ("Outline Noise Scale", Range(0, 50)) = 12
        _OutlineNoiseStrength ("Outline Noise Strength", Range(0, 1)) = 0.45

        // ============================================================
        // 6. SOFT OUTLINE HALO
        // ============================================================
        [Header(_____ 6 SOFT OUTLINE HALO _____)]
        _HaloEnable ("Halo Enable 0 Off 1 On", Range(0, 1)) = 1
        _HaloColor ("Halo Color", Color) = (1.0, 0.92, 0.7, 1)
        _HaloStrength ("Halo Strength", Range(0, 3)) = 0.8
        _HaloPower ("Halo Fresnel Power", Range(0.5, 8)) = 2.2
        _HaloSoftness ("Halo Softness", Range(0.01, 1)) = 0.55

        // ============================================================
        // 7. HALFTONE  (multiply blend, two textures)
        // ============================================================
        [Header(_____ 7 HALFTONE MASTER _____)]
        _HalftoneEnable ("Halftone Enable 0 Off 1 On", Range(0, 1)) = 1
        _HalftoneOpacity ("Halftone Opacity", Range(0, 1)) = 1.0
        _HalftoneDarken ("Halftone Multiply Darken", Range(0, 1)) = 0.75
        _HalftoneShadowCoverage ("Shadow Dot Coverage", Range(0, 1)) = 0.95
        _HalftoneMidCoverage ("Mid Dot Coverage", Range(0, 1)) = 0.65
        _HalftoneHighlightCoverage ("Highlight Dot Coverage", Range(0, 1)) = 0.10
        _HalftoneEdgeSoftness ("Halftone Edge Softness", Range(0.001, 0.3)) = 0.04
        _HalftoneRotation ("Halftone Grid Rotation Deg", Range(0, 90)) = 27

        [Header(_____ 7a SKIN MASK _____)]
        _SkinMask ("Skin Mask R Channel", 2D) = "black" {}

        [Header(_____ 7b SKIN HALFTONE _____)]
        _SkinHalftoneTex ("Skin Halftone Pattern", 2D) = "white" {}
        _SkinHalftoneStrength ("Skin Halftone Strength", Range(0, 1)) = 0.55
        _SkinHalftoneScale ("Skin Halftone Scale", Range(20, 600)) = 280

        [Header(_____ 7c CLOTH HALFTONE _____)]
        _ClothHalftoneTex ("Cloth Halftone Pattern", 2D) = "white" {}
        _ClothHalftoneStrength ("Cloth Halftone Strength", Range(0, 1)) = 1.0
        _ClothHalftoneScale ("Cloth Halftone Scale", Range(20, 400)) = 140

        // ============================================================
        // 8. INK WASH BRUSH (replaces paper grain)
        // ============================================================
        [Header(_____ 8 INK WASH BRUSH _____)]
        _InkWashStrength ("Ink Wash Strength", Range(0, 1)) = 0.6
        _InkWashScale ("Ink Wash Scale", Range(0.5, 12)) = 3.5
        _InkWashDarken ("Ink Wash Shadow Darken", Range(0, 1)) = 0.7
        _InkWashLightFade ("Ink Wash Highlight Fade", Range(0, 1)) = 0.85
        _InkBrushDirection ("Ink Brush Direction Deg", Range(0, 180)) = 35
        _InkBleedAmount ("Ink Edge Bleed", Range(0, 1)) = 0.5

        // ============================================================
        // 9. SHANGHAI 1980 PALETTE  (Da Nao Tian Gong style)
        // ============================================================
        [Header(_____ 9 SHANGHAI 1980 PALETTE _____)]
        _ShanghaiPaletteStrength ("Shanghai Palette Strength", Range(0, 1)) = 0.0
        _PaletteShadow ("Palette Shadow (deep blue green)",  Color) = (0.18, 0.30, 0.42, 1)
        _PaletteMidA   ("Palette Mid A (cinnabar red)",     Color) = (0.85, 0.30, 0.22, 1)
        _PaletteMidB   ("Palette Mid B (mineral green)",    Color) = (0.35, 0.55, 0.42, 1)
        _PaletteHigh   ("Palette Highlight (peking gold)",  Color) = (0.98, 0.85, 0.42, 1)
        _PaletteSatBoost ("Palette Saturation Boost", Range(1, 2.2)) = 1.4
        _PaletteContrast ("Palette Contrast", Range(0.5, 2)) = 1.15

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
                float  _OverallBrightness;
                float4 _LightColor;
                float4 _MidColor;
                float4 _ShadowColor;
                float  _ThresholdLight;
                float  _ThresholdShadow;
                float  _ToneSmooth;
                float  _ToneStrength;
                float  _UseVirtualSun;
                float  _SunYaw;
                float  _SunPitch;
                float  _SunFollowsView;
                float  _SunWrap;
                float  _UseGradient;
                float4 _GradientShadow;
                float4 _GradientMid;
                float4 _GradientHighlight;
                float  _GradientStrength;
                float  _GradientContrast;
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
                float4 _ClothHalftoneTex_ST;
                float  _ClothHalftoneStrength;
                float  _ClothHalftoneScale;
                float  _InkWashStrength;
                float  _InkWashScale;
                float  _InkWashDarken;
                float  _InkWashLightFade;
                float  _InkBrushDirection;
                float  _InkBleedAmount;
                float  _ShanghaiPaletteStrength;
                float4 _PaletteShadow;
                float4 _PaletteMidA;
                float4 _PaletteMidB;
                float4 _PaletteHigh;
                float  _PaletteSatBoost;
                float  _PaletteContrast;
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
                float  _UseVirtualSun;
                float  _SunYaw;
                float  _SunPitch;
                float  _SunFollowsView;
                float  _SunWrap;
                float  _UseGradient;
                float4 _GradientShadow;
                float4 _GradientMid;
                float4 _GradientHighlight;
                float  _GradientStrength;
                float  _GradientContrast;
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
                float4 _ClothHalftoneTex_ST;
                float  _ClothHalftoneStrength;
                float  _ClothHalftoneScale;
                float  _InkWashStrength;
                float  _InkWashScale;
                float  _InkWashDarken;
                float  _InkWashLightFade;
                float  _InkBrushDirection;
                float  _InkBleedAmount;
                float  _ShanghaiPaletteStrength;
                float4 _PaletteShadow;
                float4 _PaletteMidA;
                float4 _PaletteMidB;
                float4 _PaletteHigh;
                float  _PaletteSatBoost;
                float  _PaletteContrast;
                float  _BackEmissionStrength;
                float4 _BackEmissionColor;
                float4 _BackEmissionMask_ST;
                float  _BackEmissionFalloff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
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
            };

            // ---------- noise (ink wash) ----------
            float hash21(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // FBM stretched along brush direction -> brush-stroke streaks
            float inkWashFBM(float2 uv, float dirRad)
            {
                float c = cos(dirRad), s = sin(dirRad);
                float2 dirUV = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
                float2 stretched = float2(dirUV.x * 0.35, dirUV.y * 1.6);

                float n = 0.0, amp = 0.55, freq = 1.0;
                [unroll]
                for (int k = 0; k < 5; k++)
                {
                    n += vnoise(stretched * freq) * amp;
                    freq *= 2.07; amp *= 0.55;
                }
                // edge-bleed component
                float bleed = vnoise(uv * 6.3 + 13.7);
                return saturate(n * 0.7 + bleed * 0.3);
            }

            // ---------- 3D rotation matrix from yaw/pitch ----------
            float3 SunDirectionWS()
            {
                float yaw   = _SunYaw   * 0.01745329;
                float pitch = _SunPitch * 0.01745329;

                if (_SunFollowsView > 0.5)
                {
                    // Add camera yaw so the sun stays at the same screen-side as the camera spins
                    float3 camFwd = -UNITY_MATRIX_V[2].xyz; // view forward in world
                    float camYaw = atan2(camFwd.x, camFwd.z);
                    yaw += camYaw;
                }

                float cy = cos(yaw),   sy = sin(yaw);
                float cp = cos(pitch), sp = sin(pitch);
                // direction the sun shines TO; we negate when computing NdotL with surface normal
                float3 dir = normalize(float3(sy * cp, sp, cy * cp));
                return dir;
            }

            // ---------- Three-tone cel ----------
            float3 ThreeToneCel(float NdotL, float shadowAtten, float3 albedo, out float toneNorm)
            {
                // Apply wrap (soft fill) so the terminator is more natural than a flat 0.5
                float wrapped = (NdotL + _SunWrap) / (1.0 + _SunWrap);
                wrapped = saturate(wrapped) * 2.0 - 1.0;

                float litMid   = smoothstep(_ThresholdShadow - _ToneSmooth,
                                            _ThresholdShadow + _ToneSmooth, wrapped);
                float litLight = smoothstep(_ThresholdLight  - _ToneSmooth,
                                            _ThresholdLight  + _ToneSmooth, wrapped);
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
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight(IN.shadowCoord);

                // ---- Light direction: virtual sun OR scene main light ----
                float3 L;
                if (_UseVirtualSun > 0.5)
                    L = SunDirectionWS();
                else
                    L = normalize(mainLight.direction);

                float NdotL = dot(N, L);
                float shadowAtten = mainLight.shadowAttenuation;

                // ---- Albedo ----
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;

                // ============================================================
                // 2. THREE TONE CEL (rotatable sun, soft wrap)
                // ============================================================
                float toneNorm;
                float3 color = ThreeToneCel(NdotL, shadowAtten, albedo, toneNorm);

                // ============================================================
                // 3. CONTRAST GRADIENT (3-stop: shadow / mid / highlight)
                // ============================================================
                if (_UseGradient > 0.5)
                {
                    float t = saturate(NdotL * 0.5 + 0.5) * shadowAtten;
                    t = saturate(pow(t, 1.0 / max(_GradientContrast, 0.01)));
                    float3 g = (t < 0.5)
                        ? lerp(_GradientShadow.rgb, _GradientMid.rgb,       t * 2.0)
                        : lerp(_GradientMid.rgb,    _GradientHighlight.rgb, (t - 0.5) * 2.0);
                    color = lerp(color, albedo * g * 1.6, _GradientStrength);
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
                color *= lerp(float3(1,1,1), mainLight.color.rgb, 0.5);

                // ============================================================
                // 9. SHANGHAI 1980 PALETTE (multi-stop period palette, NOT mono yellow)
                // ============================================================
                if (_ShanghaiPaletteStrength > 0.001)
                {
                    float t = saturate(NdotL * 0.5 + 0.5) * shadowAtten;

                    // 4-stop palette: shadow -> midA (cinnabar) -> midB (mineral green) -> highlight (gold)
                    float3 pCol;
                    if (t < 0.33)
                        pCol = lerp(_PaletteShadow.rgb, _PaletteMidA.rgb, t / 0.33);
                    else if (t < 0.66)
                        pCol = lerp(_PaletteMidA.rgb, _PaletteMidB.rgb, (t - 0.33) / 0.33);
                    else
                        pCol = lerp(_PaletteMidB.rgb, _PaletteHigh.rgb, (t - 0.66) / 0.34);

                    // saturation boost
                    float lumaP = dot(pCol, float3(0.2126, 0.7152, 0.0722));
                    pCol = lumaP + (pCol - lumaP) * _PaletteSatBoost;

                    // contrast around 0.5
                    pCol = saturate((pCol - 0.5) * _PaletteContrast + 0.5);

                    // tint the original color, do not replace it -> keeps texture/shading detail
                    float3 tinted = color * pCol * 1.5;
                    color = lerp(color, tinted, _ShanghaiPaletteStrength);
                }

                // ============================================================
                // 7. HALFTONE  (multiply blend, two textures, light-driven coverage)
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
                    float c = cos(rotRad), s = sin(rotRad);
                    float2 ruv = float2(IN.uv.x * c - IN.uv.y * s, IN.uv.x * s + IN.uv.y * c);

                    float2 skinUV  = ruv * _SkinHalftoneScale  * 0.01;
                    float2 clothUV = ruv * _ClothHalftoneScale * 0.01;

                    float skinPat  = SAMPLE_TEXTURE2D(_SkinHalftoneTex,  sampler_SkinHalftoneTex,  skinUV).r;
                    float clothPat = SAMPLE_TEXTURE2D(_ClothHalftoneTex, sampler_ClothHalftoneTex, clothUV).r;

                    float thr = 1.0 - saturate(coverage);
                    float soft = max(_HalftoneEdgeSoftness, 0.001);

                    float dotSkin  = 1.0 - smoothstep(thr - soft, thr + soft, skinPat);
                    float dotCloth = 1.0 - smoothstep(thr - soft, thr + soft, clothPat);

                    // MULTIPLY blend: dot multiplies the underlying color by (1 - darken),
                    // so dots are always the same hue family, just deeper.
                    float skinAlpha  = dotSkin  * _SkinHalftoneStrength  * skinMask         * _HalftoneOpacity;
                    float clothAlpha = dotCloth * _ClothHalftoneStrength * (1.0 - skinMask) * _HalftoneOpacity;
                    float totalAlpha = saturate(skinAlpha + clothAlpha);

                    float3 multiplied = color * (1.0 - _HalftoneDarken);
                    color = lerp(color, multiplied, totalAlpha);
                }

                // ============================================================
                // 8. INK WASH (replaces paper grain) - brush-streak multiply
                // ============================================================
                if (_InkWashStrength > 0.001)
                {
                    float dirRad = _InkBrushDirection * 0.01745329;
                    float ink = inkWashFBM(IN.uv * _InkWashScale, dirRad);

                    // shading luma drives darkening: dark areas get heavier wash
                    float colLuma = dot(color, float3(0.2126, 0.7152, 0.0722));
                    float shadowMask = 1.0 - smoothstep(0.15, 0.7, colLuma);
                    float lightFade = lerp(1.0, smoothstep(0.7, 0.95, colLuma), _InkWashLightFade);

                    // ink amount: scale by strength * (more in shadows) * (less in highlights)
                    float inkAmt = ink * _InkWashStrength * (0.4 + 0.9 * shadowMask) * lightFade;

                    // wash color = same color, darker (multiply blend)
                    float3 wash = color * (1.0 - _InkWashDarken);
                    color = lerp(color, wash, saturate(inkAmt));

                    // edge bleed: a second softer streak that darkens contour transitions
                    if (_InkBleedAmount > 0.001)
                    {
                        float bleed = vnoise(IN.uv * _InkWashScale * 2.7 + 5.5);
                        float bleedAmt = saturate(bleed * shadowMask) * _InkBleedAmount * 0.4;
                        color = lerp(color, color * 0.75, bleedAmt);
                    }
                }

                // ============================================================
                // 6. SOFT OUTLINE HALO
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
                // 10. BACK SELF EMISSION
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
