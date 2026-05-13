Shader "Hidden/X-PostProcessing/EdgeDetectionScharr"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // URP Blit 使用的纹理
    TEXTURE2D_X(_MainTex);
    SAMPLER(sampler_MainTex);

    half2 _Params;
    half4 _EdgeColor;
    half4 _BackgroundColor;

    #define _EdgeWidth _Params.x
    #define _BackgroundFade _Params.y

    // ===== 顶点/片元结构体（替代原 PPv2 的 VaryingsDefault）=====

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
    };

    // ===== 顶点着色器（替代原 PPv2 的 VertDefault）=====

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullscreenTriangleVertexPosition(input.vertexID);
        output.texcoord   = GetNormalizedScreenSpaceUV(output.positionCS);
        return output;
    }

    // ===== Scharr 算法（与原版完全一致）=====

    float intensity(float4 color)
    {
        return sqrt((color.x * color.x) + (color.y * color.y) + (color.z * color.z));
    }

    float scharr(float stepx, float stepy, float2 center)
    {
        // 采样周围 8 个像素
        float topLeft     = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2(-stepx,  stepy)));
        float midLeft     = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2(-stepx,  0)));
        float bottomLeft  = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2(-stepx, -stepy)));
        float midTop      = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2( 0,       stepy)));
        float midBottom   = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2( 0,      -stepy)));
        float topRight    = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2( stepx,   stepy)));
        float midRight    = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2( stepx,   0)));
        float bottomRight = intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, center + float2( stepx,  -stepy)));

        // Scharr 卷积核
        // Gx:  3  0 -3        Gy:  3  10   3
        //     10  0 -10             0   0   0
        //      3  0 -3            -3 -10  -3

        float Gx = 3.0 * topLeft + 10.0 * midLeft + 3.0 * bottomLeft
                 - 3.0 * topRight - 10.0 * midRight - 3.0 * bottomRight;

        float Gy = 3.0 * topLeft + 10.0 * midTop + 3.0 * topRight
                 - 3.0 * bottomLeft - 10.0 * midBottom - 3.0 * bottomRight;

        float scharrGradient = sqrt((Gx * Gx) + (Gy * Gy));
        return scharrGradient;
    }

    // ===== 片元着色器 =====

    half4 Frag(Varyings i) : SV_Target
    {
        half4 sceneColor = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.texcoord);

        float scharrGradient = scharr(
            _EdgeWidth / _ScreenParams.x,
            _EdgeWidth / _ScreenParams.y,
            i.texcoord
        );

        // 背景淡化
        sceneColor = lerp(sceneColor, _BackgroundColor, _BackgroundFade);

        // 边缘叠加
        float3 edgeColor = lerp(sceneColor.rgb, _EdgeColor.rgb, scharrGradient);

        return float4(edgeColor, 1);
    }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "EdgeDetectionScharr"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}