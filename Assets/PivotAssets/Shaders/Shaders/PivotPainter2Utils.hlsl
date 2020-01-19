#ifndef PIVOT_PAINTER2_UTILS_INCLUDED
#define PIVOT_PAINTER2_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"


///////////////////////////////////////////////////////////////////////////////
//                           Helper functions                                //
///////////////////////////////////////////////////////////////////////////////

half2 TwoDimensionalArrayLookupByIndex(half Index, half2 ArrayDimensions) {
    half2 texcoord = half2(Index % ArrayDimensions.x, floor(Index / ArrayDimensions.x));
    texcoord += 0.5;
    texcoord /= ArrayDimensions;

    // Invert Y
    texcoord = half2(texcoord.x, 1.0 - texcoord.y);

    return texcoord;
}

half CalculateMeshElementIndex(half2 DataTextureDimensions, float2 UVCoordinate) {
    // Invert Y
    half2 uv = half2(UVCoordinate.x, 1.0 - UVCoordinate.y);
    uv = floor(uv * DataTextureDimensions);

    half index = uv.x + uv.y * DataTextureDimensions.x;
    return index;
}

half Decode8BitAlphaAxisExtent(half AlphaExtentValue) {
    return max(8, AlphaExtentValue * 2048);
}

float3 DecodeAxisVector(float3 AxisVector) {
    float3 vec = AxisVector.xzy;
    vec = TransformObjectToWorldDir(vec);
    vec = normalize(vec);

    return vec;
}

float3 DecodePosition(float3 Position, float Scale) {
    // idk why, but it is what it is.
    float3 pos = float3(-Position.x, Position.z, Position.y - 1);
    pos *= Scale;
    pos = TransformObjectToWorld(pos);

    return pos;
}

float2 LerpFourFloat2(float2 A, float2 B, float2 C, float2 D, float T) {
    float k = T * 3;
    float2 output = lerp(A, B, clamp(k, 0, 1));

    k--;
    output = lerp(output, C, clamp(k, 0, 1));

    k--;
    output = lerp(output, D, clamp(k, 0, 1));

    return output;
}

float2 LerpThreeFloat2(float2 A, float2 B, float2 C, float T) {
    float k = T * 2;
    float2 output = lerp(A, B, clamp(k, 0, 1));

    k--;
    output = lerp(output, C, clamp(k, 0, 1));

    return output;
}

void ReturnParentTextureInfo(half ParentIndex, half CurrentIndex, half2 TextureDimensions, out float2 ParentUV, out half IsChild) {
    ParentUV = TwoDimensionalArrayLookupByIndex(ParentIndex, TextureDimensions);

    if (ParentIndex != CurrentIndex) {
        IsChild = 1;
    }
    else {
        IsChild = 0;
    }
}

void RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    Rotation = radians(Rotation);
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);

    float3x3 rot_mat =
    {
        one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };

    Out = mul(rot_mat, In);
}

void RotateAboutAxisAndPivot_Degrees_float(float3 In, float3 Axis, float3 PivotPoint, float Rotation, out float3 Out) {
    RotateAboutAxis_Degrees_float(In - PivotPoint, Axis, Rotation, Out);
    Out += PivotPoint;
}

void FixRotateAboutAxisNormal(float3 Normal, float3 Axis, float Rotation, out float3 RotatedNormal) {
    RotateAboutAxis_Degrees_float(Normal, Axis, Rotation, RotatedNormal);
    RotatedNormal += Normal;
}

void UnpackFloat(inout float Float) {
    floor(Float);
}

void UnpackIntegerAsFloat(float In, out float Out) {
    uint uRes32 = asuint(In);

    const uint sign2 = ((uRes32 >> 16) & 0x8000);
    const uint exp2 = (((const int)((uRes32 >> 23) & 0xff)) - 127 + 15 << 10);
    const uint mant2 = ((uRes32 >> 13) & 0x3ff);
    const uint bits = (sign2 | exp2 | mant2);
    const uint result = bits - 1024;

    Out = float(result);
}

///////////////////////////////////////////////////////////////////////////////
//                        Basic Utility functions                            //
///////////////////////////////////////////////////////////////////////////////

void RebuildHierarchy(float2 TextureCoordinate,
    out float2 Level1Texcoord, out float Level1LayerMask,
    out float2 Level2Texcoord, out float Level2LayerMask,
    out float2 Level3Texcoord, out float Level3LayerMask,
    out float2 Level4Texcoord, out float Level4LayerMask) {
    // Total depth in the hierarchy
    half totalDepth = 0;
    // Whether the vertex is a child in the hierarchy
    half isChild;

    float2 TextureDimension = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _PivotPosTex_TexelSize).zw;

    // Level 1
    float2 level1UV = TextureCoordinate;
    half firstIndex = CalculateMeshElementIndex(TextureDimension, level1UV);

    float level1ParentIndex = SAMPLE_TEXTURE2D_LOD(_PivotPosTex, sampler_PivotPosTex, level1UV, 0).a;
    UnpackIntegerAsFloat(level1ParentIndex, level1ParentIndex);
    //UnpackFloat(level1ParentIndex);

    // Level 2
    float2 level2UV;
    ReturnParentTextureInfo(level1ParentIndex, firstIndex, TextureDimension, level2UV, isChild);
    totalDepth += isChild;

    float level2ParentIndex = SAMPLE_TEXTURE2D_LOD(_PivotPosTex, sampler_PivotPosTex, level2UV, 0).a;
    UnpackIntegerAsFloat(level2ParentIndex, level2ParentIndex);
    //UnpackFloat(level2ParentIndex);

    // Level 3
    float2 level3UV;
    ReturnParentTextureInfo(level2ParentIndex, level1ParentIndex, TextureDimension, level3UV, isChild);
    totalDepth += isChild;

    float level3ParentIndex = SAMPLE_TEXTURE2D_LOD(_PivotPosTex, sampler_PivotPosTex, level3UV, 0).a;
    UnpackIntegerAsFloat(level3ParentIndex, level3ParentIndex);
    //UnpackFloat(level3ParentIndex);

    // Level 4
    float2 level4UV;
    ReturnParentTextureInfo(level3ParentIndex, level2ParentIndex, TextureDimension, level4UV, isChild);
    totalDepth += isChild;

    // Return hierarchy total depth
    totalDepth = ceil(totalDepth);

    Level1Texcoord = LerpFourFloat2(level1UV, level2UV, level3UV, level4UV, totalDepth / 3.0);
    Level1LayerMask = 1.0;

    Level2Texcoord = LerpThreeFloat2(level1UV, level2UV, level3UV, clamp((totalDepth - 1.0) / 2.0, 0.0, 1.0));
    Level2LayerMask = clamp(totalDepth, 0.0, 1.0);

    Level3Texcoord = lerp(level1UV, level2UV, clamp(totalDepth - 2.0, 0.0, 1.0));
    Level3LayerMask = clamp(totalDepth - 1.0, 0.0, 1.0);

    Level4Texcoord = level1UV;
    Level4LayerMask = clamp(totalDepth - 2.0, 0.0, 1.0);
}

#endif