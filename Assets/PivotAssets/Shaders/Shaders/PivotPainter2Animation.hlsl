#ifndef PIVOT_PAINTER2_BASIC_INCLUDED
#define PIVOT_PAINTER2_BASIC_INCLUDED

//#include "PivotPainter2LitInput.hlsl"
#include "PivotPainter2Utils.hlsl"

float GetRotationDotProduct(float3 ForceDir, float3 XAxis, float MaxRotation) {
    float dotProduct = dot(XAxis, SafeNormalize(ForceDir));
    return MaxRotation * (1 - dotProduct);
}

float GetRotationOSFalloff(float3 PositionOS) {
    float force = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ForcePower);
    return clamp(PositionOS.y * force * 0.25, 0.0, 1.0);
}

float Damping(float Time, float DampingRatio, float Frequency) {
    float t = Frequency * sqrt(1.0 - DampingRatio * DampingRatio) * Time;
    float rad = radians(t);
    float value = exp(-DampingRatio * Time) * cos(t);

    return value;
}

float GetDampingOverTime(float3 PivotPosOS, float TimeSinceLastTouch, float DampingRatio) {
    float time = _Time.y - TimeSinceLastTouch;
    time = max(time, 0);

    float frequency = length(PivotPosOS);
    frequency = sqrt(frequency / 9.8) * TWO_PI;
    frequency *= time;

    return Damping(time, DampingRatio, frequency);
}

float GetDampingOverTime2(float TimeSinceLastTouch, float totalTime, float frequency, float falloff) {
    float time = _Time.y - TimeSinceLastTouch;
    time = clamp(time, 0, totalTime);

    float k = (totalTime - time) / totalTime;
    float damping = cos(frequency * time);
    damping *= pow(k, falloff);

    return damping;
}

void FoliageForce(float2 Texcoord, inout float3 PositionWS, inout float3 NormalWS,
    float3 PositionOS, float LayerMask, float3 ForceDir, float MaxRotation) {
    float3 pivotPos = SAMPLE_TEXTURE2D_LOD(_PivotPosTex, sampler_PivotPosTex, Texcoord, 0).xyz;
    float pivotPosScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _PivotPosScale);
    pivotPos = DecodePosition(pivotPos, pivotPosScale);

    float3 xAxis = UnpackNormal(SAMPLE_TEXTURE2D_LOD(_XVectorTex, sampler_XVectorTex, Texcoord, 0)).xyz;
    xAxis = DecodeAxisVector(xAxis);

    ForceDir = SafeNormalize(ForceDir);
    // Protects cross product from returning 0
    ForceDir = lerp(float3(0, 0.01, 0), ForceDir, clamp(distance(ForceDir, float3(0, 0, 0)) * 100, 0, 1));

    float3 rotationAxis = cross(ForceDir, xAxis);
    rotationAxis = normalize(rotationAxis);

    float dampingRatio = 0.94;
    float3 pivotPosOS = TransformWorldToObject(pivotPos);

    float rotationAlign = GetRotationDotProduct(ForceDir, xAxis, MaxRotation);
    float rotationFalloff = GetRotationOSFalloff(PositionOS);

    float timeSinceLastTouch = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _TimeSinceLastTouch);
    //float damping = GetDampingOverTime(pivotPosOS, timeSinceLastTouch, dampingRatio);
    float damping = GetDampingOverTime2(timeSinceLastTouch, 2.5, 10, 3);

    float rotation = rotationAlign * rotationFalloff * damping * 360.0;

    float3 rotatedPosition;
    RotateAboutAxisAndPivot_Degrees_float(PositionWS, rotationAxis, pivotPos, rotation, rotatedPosition);

    float3 rotatedNormal;
    FixRotateAboutAxisNormal(NormalWS, rotationAxis, rotation, rotatedNormal);

    PositionWS = lerp(PositionWS, rotatedPosition, LayerMask);
    NormalWS = lerp(NormalWS, rotatedNormal, LayerMask);
}

void PivotPainter2VertexAnimation(float2 Texcoord, float3 PositionOS, inout float3 PositionWS, inout float3 NormalWS) {
    float3 forceDir = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ForceDir);

    float maxRotation1 = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MaxRotation1);
    float maxRotation2 = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MaxRotation2);
    float maxRotation3 = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MaxRotation3);
    float maxRotation4 = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MaxRotation4);

    float2 Level1Texcoord;
    float2 Level2Texcoord;
    float2 Level3Texcoord;
    float2 Level4Texcoord;
    float Level1LayerMask;
    float Level2LayerMask;
    float Level3LayerMask;
    float Level4LayerMask;

    RebuildHierarchy(Texcoord,
        Level1Texcoord, Level1LayerMask,
        Level2Texcoord, Level2LayerMask,
        Level3Texcoord, Level3LayerMask,
        Level4Texcoord, Level4LayerMask
    );

    // Level 4
#ifdef _ENABLE_LEVEL4_ON
    FoliageForce(Level4Texcoord, PositionWS, NormalWS, PositionOS,
        Level4LayerMask, forceDir, maxRotation4);
#endif

    // Level 3
#ifdef _ENABLE_LEVEL3_ON
    FoliageForce(Level3Texcoord, PositionWS, NormalWS, PositionOS,
        Level3LayerMask, forceDir, maxRotation3);
#endif

    // Level 2
#ifdef _ENABLE_LEVEL2_ON
    FoliageForce(Level2Texcoord, PositionWS, NormalWS, PositionOS,
        Level2LayerMask, forceDir, maxRotation2);
#endif

    // Level 1
#ifdef _ENABLE_LEVEL1_ON
    FoliageForce(Level1Texcoord, PositionWS, NormalWS, PositionOS,
        Level1LayerMask, forceDir, maxRotation1);
#endif
}

#endif