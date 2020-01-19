Shader "Lightweight Render Pipeline/Pivot Painter2/Debug"
{
    Properties
    {
        // Pivot Properties
        [NoScaleOffset] _XVectorTex("X-Vector and X-Extent Texture", 2D) = "bump" {}
        [NoScaleOffset] _PivotPosTex("Pivot Position and Index Texture", 2D) = "black" {}
        _PivotPosScale("Pivot Position Scale", Float) = 1.0

        // Force Properties
        [HideInInspector] _ForceDir("Force Direction", Vector) = (1.0, 0.0, 0.0, 0.0)
        [HideInInspector] _ForcePower("Force Power", Float) = 1.0

        // Hierarchy Level Settings
        [Toggle] _Enable_Level1("Enable Level 1", Float) = 1.0
        [Toggle] _Enable_Level2("Enable Level 2", Float) = 1.0
        [Toggle] _Enable_Level3("Enable Level 3", Float) = 1.0
        [Toggle] _Enable_Level4("Enable Level 4", Float) = 1.0

        _MaxRotation1("Max Rotation 1", Range(0.0, 1.0)) = 0.25
        _MaxRotation2("Max Rotation 2", Range(0.0, 1.0)) = 0.25
        _MaxRotation3("Max Rotation 3", Range(0.0, 1.0)) = 0.25
        _MaxRotation4("Max Rotation 4", Range(0.0, 1.0)) = 0.25

        // Custom Pivot Animation Properties
        [HideInInspector] _TimeSinceLastTouch("Time Since Last Touch", Float) = -10.0

        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5

            // BlendMode
            [HideInInspector] _Surface("__surface", Float) = 0.0
            [HideInInspector] _Blend("__blend", Float) = 0.0
            [HideInInspector] _AlphaClip("__clip", Float) = 0.0
            [HideInInspector] _SrcBlend("Src", Float) = 1.0
            [HideInInspector] _DstBlend("Dst", Float) = 0.0
            [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
            [HideInInspector] _Cull("__cull", Float) = 2.0

            // Editmode props
            [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

            // ObsoleteProperties
            [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
            [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)
            [HideInInspector] _SampleGI("SampleGI", float) = 0.0 // needed from bakedlit
    }
        SubShader
            {
                Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "LightweightPipeline" }
                LOD 100

                Blend[_SrcBlend][_DstBlend]
                ZWrite[_ZWrite]
                Cull[_Cull]

                Pass
                {
                    Name "Unlit"
                    HLSLPROGRAM
                // Required to compile gles 2.0 with standard srp library
                #pragma prefer_hlslcc gles
                #pragma exclude_renderers d3d11_9x

                #pragma vertex vert
                #pragma fragment frag
                #pragma shader_feature _ALPHATEST_ON
                #pragma shader_feature _ALPHAPREMULTIPLY_ON

                // -------------------------------------
                // Unity defined keywords
                #pragma multi_compile_instancing
                
                // -------------------------------------
                // Pivot Painter2 defined keywords
                #pragma shader_feature _ENABLE_LEVEL1_ON
                #pragma shader_feature _ENABLE_LEVEL2_ON
                #pragma shader_feature _ENABLE_LEVEL3_ON
                #pragma shader_feature _ENABLE_LEVEL4_ON

                #include "PivotPainter2UnlitInput.hlsl"
                #include "PivotPainter2Animation.hlsl"

                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float3 normalOS     : NORMAL;
                    float4 tangentOS    : TANGENT;
                    float2 texcoord     : TEXCOORD0;
                    float2 uv1          : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float2 uv        : TEXCOORD0;
                    float2 uv1       : TEXCOORD1;
                    float4 color     : TEXCOORD2;
                    float4 vertex    : SV_POSITION;

                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                Varyings vert(Attributes input)
                {
                    Varyings output = (Varyings)0;

                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                    //output.vertex = vertexInput.positionCS;
                    output.uv = input.texcoord;
                    output.uv1 = input.uv1;

                    float3 rotatedPositionWS = vertexInput.positionWS;
                    float3 rotatedNormalWS = normalInput.normalWS;

                    PivotPainter2VertexAnimation(input.uv1, input.positionOS.xyz, rotatedPositionWS, rotatedNormalWS);

                    output.vertex = TransformWorldToHClip(rotatedPositionWS);

                    float2 Level1Texcoord;
                    float2 Level2Texcoord;
                    float2 Level3Texcoord;
                    float2 Level4Texcoord;
                    float Level1LayerMask;
                    float Level2LayerMask;
                    float Level3LayerMask;
                    float Level4LayerMask;

                    RebuildHierarchy(input.uv1,
                        Level1Texcoord, Level1LayerMask,
                        Level2Texcoord, Level2LayerMask,
                        Level3Texcoord, Level3LayerMask,
                        Level4Texcoord, Level4LayerMask
                    );

                    output.color = float4(Level1LayerMask, Level2LayerMask, Level3LayerMask, Level4LayerMask);

                    return output;
                }

                half4 frag(Varyings input) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    //half2 uv = input.uv1;
                    //half4 color = SAMPLE_TEXTURE2D(_XVectorTex, sampler_XVectorTex, input.uv1);
                    //half3 color = UnpackNormal(SAMPLE_TEXTURE2D(_XVectorTex, sampler_XVectorTex, input.uv1)).xyz;
                    
                    //return half4(color.rgb, 1.0);
                    return input.color;
                }
                ENDHLSL
            }
        }
        FallBack "Hidden/InternalErrorShader"
        CustomEditor "UnityEditor.Rendering.LWRP.ShaderGUI.PivotPainter2UnlitShader"
}
