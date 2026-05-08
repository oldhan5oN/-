Shader "Custom/ShanghaiToonURP_Halftone"
{
    Properties
    {
        // ========== BASE ==========
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

        // ========== HALFTONE MASTER ==========
        [Header(Halftone Master)]
        _HalftoneEnable ("Halftone Enable 0 Off 1 On", Range(0, 1)) = 1
        _HalftoneOpacity ("Halftone Overall Opacity", Range(0, 1)) = 0.85
        _HalftoneDarken ("Halftone Dot Darken Factor", Range(0, 1)) = 0.55
        _HalftoneHueShift ("Halftone Hue Shift", Range(-0.5, 0.5)) = 0.0
        _HalftoneRotation ("Halftone Grid Rotation Degrees", Range(0, 90)) = 27

        // ========== HALFTONE LIGHTING RESPONSE ==========
        [Header(Halftone Lighting Response)]
        _HalftoneShadowCoverage ("Shadow Area Dot Coverage", Range(0, 1)) = 0.85
        _HalftoneMidCoverage ("Mid Tone Dot Coverage", Range(0, 1)) = 0.5
        _HalftoneHighlightCoverage ("Highlight Dot Coverage", Range(0, 1)) = 0.05
        _HalftoneEdgeSoftness ("Dot Edge Softness", Range(0.001, 0.3)) = 0.06

        // ========== SKIN MASK ==========
        [Header(Skin Mask)]
        _SkinMask ("Skin Mask R Channel", 2D) = "black" {}

        // ========== CLOTHING HALFTONE ==========
        [Header(Clothing Halftone)]
        _ClothHalftoneStrength ("Cloth Halftone Strength", Range(0, 1)) = 0.85
        _ClothHalftoneScale ("Cloth Halftone Scale", Range(20, 400)) = 120
        _ClothHueOffset ("Cloth Color Hue Offset", Range(-0.5, 0.5)) = 0.0

        // ========== SKIN HALFTONE ==========
        [Header(Skin Halftone)]
        _SkinHalftoneStrength ("Skin Halftone Strength", Range(0, 1)) = 0.25
        _SkinHalftoneScale ("Skin Halftone Scale", Range(20, 600)) = 260
        _SkinHueOffset ("Skin Color Hue Offset", Range(-0.5, 0.5)) = 0.0
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
                float  _HalftoneEnable;
                float  _HalftoneOpacity;
                float  _HalftoneDarken;
                float  _HalftoneHueShift;
                float  _HalftoneRotation;
                float  _HalftoneShadowCoverage;
                float  _HalftoneMidCoverage;
                float  _HalftoneHighlightCoverage;
                float  _HalftoneEdgeSoftness;
                float4 _SkinMask_ST;
                float  _ClothHalftoneStrength;
                float  _ClothHalftoneScale;
                float  _ClothHueOffset;
                float  _SkinHalftoneStrength;
                float  _SkinHalftoneScale;
                float  _SkinHueOffset;
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
        // PASS 2 : MAIN  (cel + halftone)
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
                float  _HalftoneEnable;
                float  _HalftoneOpacity;
                float  _HalftoneDarken;
                float  _HalftoneHueShift;
                float  _HalftoneRotation;
                float  _HalftoneShadowCoverage;
                float  _HalftoneMidCoverage;
                float  _HalftoneHighlightCoverage;
                float  _HalftoneEdgeSoftness;
                float4 _SkinMask_ST;
                float  _ClothHalftoneStrength;
                float  _ClothHalftoneScale;
                float  _ClothHueOffset;
                float  _SkinHalftoneStrength;
                float  _SkinHalftoneScale;
                float  _SkinHueOffset;
            CBUFFER_END

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SkinMask); SAMPLER(sampler_SkinMask);

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

            // ---- HSV / RGB conversion (for hue offset on dot color) ----
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

            // ---- Halftone dot field ----
            // Returns 0..1 where 1 = inside dot, 0 = outside dot.
            // dotCoverage controls dot radius within each cell (0 = no dots, 1 = fully covered).
            float HalftoneDot(float2 uv, float scale, float rotationRad, float dotCoverage, float edgeSoftness)
            {
                float cs = cos(rotationRad);
                float sn = sin(rotationRad);
                float2 ruv = float2(uv.x * cs - uv.y * sn, uv.x * sn + uv.y * cs);
                float2 grid = ruv * scale;
                float2 cell = frac(grid) - 0.5;
                float dist = length(cell);

                // Dot radius derived from coverage: at coverage=1 the dot fills the cell (radius ~0.5)
                float radius = sqrt(saturate(dotCoverage)) * 0.5;
                float soft = max(edgeSoftness, 0.001);
                return 1.0 - smoothstep(radius - soft, radius + soft, dist);
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
                // HALFTONE SYSTEM
                // ============================================================
                if (_HalftoneEnable > 0.5)
                {
                    // 1) Light intensity 0..1 ( shadow=0, highlight=1 )
                    //    Use NdotL remapped + smoothed lit, mixed with shadowed luma.
                    float lightingNorm = saturate(NdotL * 0.5 + 0.5);
                    lightingNorm = lerp(lightingNorm, lit, 0.5);

                    // 2) Skin mask (R channel). 1 = skin, 0 = clothing/everything else.
                    float skinMask = SAMPLE_TEXTURE2D(_SkinMask, sampler_SkinMask,
                                                     TRANSFORM_TEX(IN.uv, _SkinMask)).r;

                    // 3) Coverage curve : shadows dense, mids middle, highlights sparse
                    //    Two-segment lerp gives an artist-controllable falloff.
                    float coverage;
                    if (lightingNorm < 0.5)
                        coverage = lerp(_HalftoneShadowCoverage, _HalftoneMidCoverage, lightingNorm * 2.0);
                    else
                        coverage = lerp(_HalftoneMidCoverage, _HalftoneHighlightCoverage, (lightingNorm - 0.5) * 2.0);

                    float rotRad = _HalftoneRotation * 0.01745329; // degrees -> radians

                    // 4) Two halftone layers: clothing UV-driven and skin UV-driven
                    float dotCloth = HalftoneDot(IN.uv, _ClothHalftoneScale, rotRad,
                                                 coverage, _HalftoneEdgeSoftness);
                    float dotSkin  = HalftoneDot(IN.uv, _SkinHalftoneScale,  rotRad,
                                                 coverage, _HalftoneEdgeSoftness);

                    // 5) Dot color = same-family deeper tone of the underlying albedo
                    float3 clothDotColor = ApplyHueShift(color, _ClothHueOffset + _HalftoneHueShift);
                    clothDotColor *= (1.0 - _HalftoneDarken);

                    float3 skinDotColor = ApplyHueShift(color, _SkinHueOffset + _HalftoneHueShift);
                    skinDotColor *= (1.0 - _HalftoneDarken * 0.7); // skin slightly lighter

                    // 6) Per-region masked alpha
                    float clothAlpha = dotCloth * _ClothHalftoneStrength * (1.0 - skinMask) * _HalftoneOpacity;
                    float skinAlpha  = dotSkin  * _SkinHalftoneStrength  * skinMask         * _HalftoneOpacity;

                    // 7) Composite (lerp keeps it as a translucent overlay rather than opaque punch)
                    color = lerp(color, clothDotColor, saturate(clothAlpha));
                    color = lerp(color, skinDotColor,  saturate(skinAlpha));
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
