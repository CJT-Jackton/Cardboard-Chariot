#ifndef LIGHTWEIGHT_PIVOT_PAINTER_LIT_INPUT_INCLUDED
#define LIGHTWEIGHT_PIVOT_PAINTER_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(half4, _SpecColor)
UNITY_DEFINE_INSTANCED_PROP(half4, _EmissionColor)
UNITY_DEFINE_INSTANCED_PROP(half, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(half, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(half, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(half, _BumpScale)
UNITY_DEFINE_INSTANCED_PROP(half, _OcclusionStrength)
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
half _Smoothness;
half _Metallic;
half _BumpScale;
half _OcclusionStrength;
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

TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

TEXTURE2D(_PivotPosTex);    SAMPLER(sampler_PivotPosTex);
TEXTURE2D(_XVectorTex);     SAMPLER(sampler_XVectorTex);

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = SAMPLE_METALLICSPECULAR(uv);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
#else
    specGloss.a *= UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
#endif
#else // _METALLICSPECGLOSSMAP
#if _SPECULAR_SETUP
    specGloss.rgb = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecColor).rgb;
#else
    specGloss.rgb = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic).rrr;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
#else
    specGloss.a = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
#endif
#endif

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
    // TODO: Controls things like these by exposing SHADER_QUALITY levels (low, medium, high)
#if defined(SHADER_API_GLES)
    return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
#else
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return LerpWhiteTo(occ, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _OcclusionStrength));
#endif
#else
    return 1.0;
#endif
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor), UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor).rgb;

#if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BumpScale));
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor).rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif // LIGHTWEIGHT_PIVOT_PAINTER_LIT_INPUT_INCLUDED