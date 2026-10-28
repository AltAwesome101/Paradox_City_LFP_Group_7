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

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "OUTLINE"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "PixelOutlineCommon.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = OutlineClipPos(v.vertex, v.normal, _OutlineWidth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_OutlineColor.rgb * (1.0 - _OutlineDarken), 1);
            }
            ENDCG
        }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "PixelOutlineCommon.cginc"

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
                float2 uv = PixelSnapUV(i.uv, _PixelDensity);
                float3 albedo = _BaseMap.Sample(sampler_point_repeat, uv).rgb * _BaseColor.rgb;

                float3 N = normalize(i.normalWS);
                float ndl = StepLight(saturate(dot(N, normalize(_WorldSpaceLightPos0.xyz))), _LightSteps);

                float3 ambient = ShadeSH9(float4(N, 1)) * _AmbientStrength;
                float3 lit = albedo * (ambient + _LightColor0.rgb * ndl);

                return fixed4(ColourControl(lit), 1);
            }
            ENDCG
        }
    }
}
