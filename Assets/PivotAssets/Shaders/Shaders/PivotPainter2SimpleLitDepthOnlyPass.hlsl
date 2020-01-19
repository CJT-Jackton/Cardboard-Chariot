#ifndef LIGHTWEIGHT_PIVOT_PAINTER_SIMPLE_LIT_DEPTH_ONLY_PASS_INCLUDED
#define LIGHTWEIGHT_PIVOT_PAINTER_SIMPLE_LIT_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "PivotPainter2Animation.hlsl"

struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = input.texcoord;// TRANSFORM_TEX(input.texcoord, _BaseMap);

    // Pivot Vertex Animation
    float3 rotatedPositionWS = TransformObjectToWorld(input.position.xyz);
    float3 rotatedNormalWS = float3(0, 1, 0);

    PivotPainter2VertexAnimation(input.uv1, input.position.xyz, rotatedPositionWS, rotatedNormalWS);

    output.positionCS = TransformWorldToHClip(rotatedPositionWS);
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor), UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    return 0;
}
#endif // LIGHTWEIGHT_PIVOT_PAINTER_SIMPLE_LIT_DEPTH_ONLY_PASS_INCLUDED
