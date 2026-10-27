Shader "Custom/PixelOutlineURP"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Map (optional)", 2D) = "white" {}
        _BaseColor ("Base Colour / Tint", Color) = (1,1,1,1)

        [Header(Pixelation)]
        _PixelDensity ("Pixel Density (texels per UV unit)", Range(4, 256)) = 48
        _LightSteps ("Light Steps (1 = flat)", Range(1, 8)) = 3
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35

        [Header(Colour Control)]
        _Hue ("Hue Shift (degrees)", Range(-180, 180)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 2)) = 1
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Posterize ("Posterize Levels (0 = off)", Range(0, 32)) = 0

        [Header(Outline)]
        _OutlineColor ("Outline Colour", Color) = (0.1, 0.02, 0.02, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2
        _OutlineDarken ("Outline Darken", Range(0, 1)) = 0.5
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    TEXTURE2D(_BaseMap);

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor;
        float _PixelDensity;
        float _LightSteps;
        float _AmbientStrength;
        float _Hue;
        float _Saturation;
        float _Contrast;
        float _Brightness;
        float _Posterize;
        float4 _OutlineColor;
        float _OutlineWidth;
        float _OutlineDarken;
    CBUFFER_END

    float3 HueShift(float3 c, float degrees)
    {
        float a = radians(degrees);
        float3 k = float3(0.57735, 0.57735, 0.57735);
        float ca = cos(a);
        return c * ca + cross(k, c) * sin(a) + k * dot(k, c) * (1.0 - ca);
    }

    float3 ColourControl(float3 c)
    {
        c = HueShift(c, _Hue);
        float luma = dot(c, float3(0.299, 0.587, 0.114));
        c = lerp(luma.xxx, c, _Saturation);
        c = (c - 0.5) * _Contrast + 0.5;
        c *= _Brightness;
        if (_Posterize > 0.5)
            c = floor(saturate(c) * _Posterize + 0.5) / _Posterize;
        return saturate(c);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ---------- Pass 1: outline (inverted hull, constant screen-space width) ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float2 normalCS = normalize(TransformWorldToHClipDir(normalWS).xy);

                float aspect = _ScreenParams.x / _ScreenParams.y;
                normalCS.x /= aspect;

                // width is in roughly "pixels at 1080p"; multiplied by w to stay constant on screen
                posCS.xy += normalCS * (_OutlineWidth * 0.002) * posCS.w;
                OUT.positionCS = posCS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 col = _OutlineColor.rgb * (1.0 - _OutlineDarken);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // ---------- Pass 2: pixelated, stepped-lit surface with colour control ----------
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // snap UVs to a grid -> blocky pixels
                float2 uv = (floor(IN.uv * _PixelDensity) + 0.5) / _PixelDensity;
                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_PointClamp, uv).rgb * _BaseColor.rgb;

                // stepped (toon) lighting
                float3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                float ndl = saturate(dot(N, mainLight.direction));
                ndl = (_LightSteps <= 1.0) ? 1.0 : floor(ndl * _LightSteps) / (_LightSteps - 1.0);
                ndl = saturate(ndl);

                float3 ambient = SampleSH(N) * _AmbientStrength;
                float3 lit = albedo * (ambient + mainLight.color * ndl);

                return half4(ColourControl(lit), 1);
            }
            ENDHLSL
        }
    }
}
