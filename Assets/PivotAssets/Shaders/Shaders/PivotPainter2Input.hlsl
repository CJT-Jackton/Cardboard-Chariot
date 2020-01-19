#ifndef PIVOT_PAINTER_INPUT_INCLUDED
#define PIVOT_PAINTER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

TEXTURE2D(_PivotPosTex);    SAMPLER(sampler_PivotPosTex);
TEXTURE2D(_XVectorTex);     SAMPLER(sampler_XVectorTex);

#endif