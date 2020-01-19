#ifndef LIGHTWEIGHT_PIVOT_PAINTER_SIMPLE_LIT_INPUT_INCLUDED
#define LIGHTWEIGHT_PIVOT_PAINTER_SIMPLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(half4, _SpecColor)
UNITY_DEFINE_INSTANCED_PROP(half4, _EmissionColor)
UNITY_DEFINE_INSTANCED_PROP(half, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _PivotPosScale)
UNITY_DEFINE_INSTANCED_PROP(float4, _PivotPosTex_TexelSize)
UNITY_DEFINE_INSTANCED_PROP(float3, _ForceDir)
UNITY_DEFINE_INSTANCED_PROP(float, _ForcePower)
UNITY_DEFINE_INSTANCED_PROP(float, _MaxRotation1)
UNITY_DEFINE_INSTANCED_PROP(float, _MaxRotation2)
UNITY_DEFINE_INSTANCED_PROP(float, _MaxRotation3)
UNITY_DEFINE_INSTANCED_PROP(float, _MaxRotation4)
UNITY_DEFINE_INSTANCED_PROP(float, _TimeSinceLastTouch)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

/*
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
float _PivotPosScale;
float4 _PivotPosTex_TexelSize;
float3 _ForceDir;
float _ForcePower;
float _MaxRotation1;
float _MaxRotation2;
float _MaxRotation3;
float _MaxRotation4;
float _TimeSinceLastTouch;
CBUFFER_END
*/

TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

TEXTURE2D(_PivotPosTex);    SAMPLER(sampler_PivotPosTex);
TEXTURE2D(_XVectorTex);     SAMPLER(sampler_XVectorTex);

half4 SampleSpecularSmoothness(half2 uv, half alpha, half4 specColor, TEXTURE2D_PARAM(specMap, sampler_specMap))
{
    half4 specularSmoothness = half4(0.0h, 0.0h, 0.0h, 1.0h);
#ifdef _SPECGLOSSMAP
    specularSmoothness = SAMPLE_TEXTURE2D(specMap, sampler_specMap, uv) * specColor;
#elif defined(_SPECULAR_COLOR)
    specularSmoothness = specColor;
#endif

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    specularSmoothness.a = exp2(10 * alpha + 1);
#else
    specularSmoothness.a = exp2(10 * specularSmoothness.a + 1);
#endif

    return specularSmoothness;
}

#endif // LIGHTWEIGHT_PIVOT_PAINTER_SIMPLE_LIT_INPUT_INCLUDED
