#ifndef PIXEL_OUTLINE_COMMON
#define PIXEL_OUTLINE_COMMON

// Shared code for the PixelOutline family of shaders.
// Any shader that includes this must declare the matching Properties
// (ShaderLab Properties blocks cannot be included) and put
// `#include "PixelOutlineCommon.cginc"` inside CGINCLUDE or CGPROGRAM.

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

float2 PixelSnapUV(float2 uv, float density)
{
    return (floor(uv * density) + 0.5) / density;
}

float StepLight(float ndl, float steps)
{
    ndl = (steps <= 1.0) ? 1.0 : floor(ndl * steps) / (steps - 1.0);
    return saturate(ndl);
}

// Inverted-hull outline: pushes the vertex along its normal in clip space
// so the width stays constant on screen.
float4 OutlineClipPos(float4 vertex, float3 normalOS, float width)
{
    float4 posCS = UnityObjectToClipPos(vertex);
    float3 normalWS = UnityObjectToWorldNormal(normalOS);
    float2 normalCS = normalize(mul((float3x3)UNITY_MATRIX_VP, normalWS).xy);

    float aspect = _ScreenParams.x / _ScreenParams.y;
    normalCS.x /= aspect;

    posCS.xy += normalCS * (width * 0.002) * posCS.w;
    return posCS;
}

#endif
