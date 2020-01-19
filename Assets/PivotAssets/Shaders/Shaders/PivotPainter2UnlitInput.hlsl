#ifndef LIGHTWEIGHT_PIVOT_PAINTER_UNLIT_INPUT_INCLUDED
#define LIGHTWEIGHT_PIVOT_PAINTER_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
half _Glossiness;
half _Metallic;
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

TEXTURE2D(_PivotPosTex);    SAMPLER(sampler_PivotPosTex);
TEXTURE2D(_XVectorTex);     SAMPLER(sampler_XVectorTex);

#endif // LIGHTWEIGHT_PIVOT_PAINTER_UNLIT_INPUT_INCLUDED
