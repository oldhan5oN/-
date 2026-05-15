Shader "Hidden/XPostProcessing/EdgeDetectionScharr"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE

    #pragma target 4.5

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X(_MainTex);
    SAMPLER(sampler_MainTex);

    float4 _MainTex_TexelSize;

    half2 _Params;
    half4 _EdgeColor;
    half4 _BackgroundColor;

    #define _EdgeWidth      _Params.x
    #define _BackgroundFade _Params.y

    //================================================
    // Intensity
    //================================================

    float Intensity(float3 color)
    {
        return dot(color, float3(0.299, 0.587, 0.114));
    }

    //================================================
    // Scharr
    //================================================

    float Scharr(float2 uv)
    {
        float2 offset = _MainTex_TexelSize.xy * _EdgeWidth;

        float tl = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(-offset.x,  offset.y)).rgb);
        float ml = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(-offset.x,  0.0)).rgb);
        float bl = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(-offset.x, -offset.y)).rgb);

        float tm = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(0.0,  offset.y)).rgb);
        float bm = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(0.0, -offset.y)).rgb);

        float tr = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(offset.x,  offset.y)).rgb);
        float mr = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(offset.x,  0.0)).rgb);
        float br = Intensity(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv + float2(offset.x, -offset.y)).rgb);

        // Scharr X
        float gx =
              3.0 * tl
            + 10.0 * ml
            + 3.0 * bl
            - 3.0 * tr
            - 10.0 * mr
            - 3.0 * br;

        // Scharr Y
        float gy =
              3.0 * tl
            + 10.0 * tm
            + 3.0 * tr
            - 3.0 * bl
            - 10.0 * bm
            - 3.0 * br;

        float gradient = sqrt(gx * gx + gy * gy);

        return saturate(gradient);
    }

    //================================================
    // Fragment
    //================================================

    half4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;

        half4 sceneColor =
            SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);

        float edge = Scharr(uv);

        // Background fade
        sceneColor.rgb =
            lerp(sceneColor.rgb,
                 _BackgroundColor.rgb,
                 _BackgroundFade);

        // Edge blend
        float3 finalColor =
            lerp(sceneColor.rgb,
                 _EdgeColor.rgb,
                 edge);

        return half4(finalColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "EdgeDetectionScharr"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }

    FallBack Off
}