Shader "Custom/RealisticClay"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1,1,1,1)

        _RoughnessMap ("Roughness Map Optional", 2D) = "white" {}
        _UseRoughnessMap ("Use Roughness Map", Range(0,1)) = 0
        _Roughness ("Fallback Roughness", Range(0,1)) = 0.86

        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.16

        _NoiseScale ("Clay Color Grain Scale", Range(1,100)) = 32
        _NoiseStrength ("Clay Color Grain Strength", Range(0,1)) = 0.12

        _BumpScale ("Clay Surface Bump Scale", Range(1,150)) = 70
        _BumpStrength ("Clay Surface Bump Strength", Range(0,0.2)) = 0.04

        _FresnelColor ("Soft Edge Color", Color) = (0.9,0.62,0.45,1)
        _FresnelStrength ("Soft Edge Strength", Range(0,1)) = 0.12
        _FresnelPower ("Soft Edge Power", Range(1,8)) = 3.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _RoughnessMap_ST;

                float4 _BaseColor;

                float _UseRoughnessMap;
                float _Roughness;
                float _SpecularStrength;

                float _NoiseScale;
                float _NoiseStrength;

                float _BumpScale;
                float _BumpStrength;

                float4 _FresnelColor;
                float _FresnelStrength;
                float _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x)
                     + (c - a) * u.y * (1.0 - u.x)
                     + (d - b) * u.x * u.y;
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                for (int i = 0; i < 4; i++)
                {
                    value += noise(p) * amplitude;
                    p *= 2.03;
                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.tangentWS = normalize(normalInputs.tangentWS);
                OUT.bitangentWS = normalize(normalInputs.bitangentWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(posInputs);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseSample.rgb * _BaseColor.rgb;

                float colorNoise = fbm(IN.uv * _NoiseScale);
                float clayColorMul = 0.82 + colorNoise * 0.36;
                albedo *= lerp(1.0, clayColorMul, _NoiseStrength);

                float2 roughUV = TRANSFORM_TEX(IN.uv, _RoughnessMap);
                float roughTex = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, roughUV).r;
                float roughness = lerp(_Roughness, roughTex, saturate(_UseRoughnessMap));
                roughness = saturate(roughness);

                float smoothness = 1.0 - roughness;

                float bumpNoise = fbm(IN.uv * _BumpScale);

                float2 e = float2(0.001, 0);
                float bumpX = fbm((IN.uv + e.xy) * _BumpScale) - bumpNoise;
                float bumpY = fbm((IN.uv + e.yx) * _BumpScale) - bumpNoise;

                float3 normalTS = normalize(float3(
                    bumpX * _BumpStrength * 100.0,
                    bumpY * _BumpStrength * 100.0,
                    1.0
                ));

                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS)
                );

                float3 normalWS = normalize(mul(normalTS, TBN));

                Light mainLight = GetMainLight(IN.shadowCoord);

                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = albedo * mainLight.color * NdotL * mainLight.shadowAttenuation;

                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));

                float specPower = lerp(8.0, 96.0, smoothness);
                float spec = pow(NdotH, specPower) * _SpecularStrength * smoothness;
                float3 specular = spec * mainLight.color * mainLight.shadowAttenuation;

                float3 ambient = SampleSH(normalWS) * albedo * 0.65;

                #ifdef _ADDITIONAL_LIGHTS
                uint additionalLightsCount = GetAdditionalLightsCount();

                for (uint i = 0; i < additionalLightsCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);

                    float ndl = saturate(dot(normalWS, light.direction));
                    diffuse += albedo * light.color * ndl * light.distanceAttenuation * light.shadowAttenuation;

                    float3 h = normalize(light.direction + viewDir);
                    float ndh = saturate(dot(normalWS, h));
                    float s = pow(ndh, specPower) * _SpecularStrength * smoothness;

                    specular += s * light.color * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                float3 edgeGlow = _FresnelColor.rgb * fresnel * _FresnelStrength;

                float3 finalColor = ambient + diffuse + specular + edgeGlow;

                return half4(finalColor, baseSample.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}