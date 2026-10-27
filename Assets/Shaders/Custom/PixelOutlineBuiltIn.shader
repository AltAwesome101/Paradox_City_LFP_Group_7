Shader "Custom/PixelOutlineBuiltIn"
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

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Lighting.cginc"

    Texture2D _BaseMap;
    SamplerState sampler_point_repeat;   // inline point-filter sampler

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
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // ---------- Pass 1: outline (inverted hull, constant screen-space width) ----------
        Pass
        {
            Name "Outline"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                float4 posCS = UnityObjectToClipPos(v.vertex);
                float3 normalWS = UnityObjectToWorldNormal(v.normal);
                float2 normalCS = normalize(mul((float3x3)UNITY_MATRIX_VP, normalWS).xy);

                float aspect = _ScreenParams.x / _ScreenParams.y;
                normalCS.x /= aspect;

                posCS.xy += normalCS * (_OutlineWidth * 0.002) * posCS.w;
                o.pos = posCS;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_OutlineColor.rgb * (1.0 - _OutlineDarken), 1);
            }
            ENDCG
        }

        // ---------- Pass 2: pixelated, stepped-lit surface with colour control ----------
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // snap UVs to a grid -> blocky pixels
                float2 uv = (floor(i.uv * _PixelDensity) + 0.5) / _PixelDensity;
                float3 albedo = _BaseMap.Sample(sampler_point_repeat, uv).rgb * _BaseColor.rgb;

                // stepped (toon) lighting
                float3 N = normalize(i.normalWS);
                float ndl = saturate(dot(N, normalize(_WorldSpaceLightPos0.xyz)));
                ndl = (_LightSteps <= 1.0) ? 1.0 : floor(ndl * _LightSteps) / (_LightSteps - 1.0);
                ndl = saturate(ndl);

                float3 ambient = ShadeSH9(float4(N, 1)) * _AmbientStrength;
                float3 lit = albedo * (ambient + _LightColor0.rgb * ndl);

                return fixed4(ColourControl(lit), 1);
            }
            ENDCG
        }
    }
}
