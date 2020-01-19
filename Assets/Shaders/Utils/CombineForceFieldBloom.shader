///
/// CombineForceFieldBloom.shader
///
/// This shader simply combine the render texture and the camera color buffer, plus adding the bloom render texture.
///

Shader "Hidden/MonsterClash/CombineForceFieldBloom"
{
    Properties
    {
        _MainTex("CameraColorTexture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}

        Pass
        {
            Name "CombineTextures"
            ZTest Always ZWrite Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);              SAMPLER(sampler_MainTex);
            TEXTURE2D(_ForceFieldTexture);    SAMPLER(sampler_ForceFieldTexture);
            TEXTURE2D(_BloomTex);             SAMPLER(sampler_BloomTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = input.uv;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 blendColor = SAMPLE_TEXTURE2D(_ForceFieldTexture, sampler_ForceFieldTexture, input.uv);
                half4 addColor = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, input.uv);

                half4 color = lerp(baseColor, blendColor, blendColor.a);
                color += addColor;

                return color;
            }

            ENDHLSL
        }
    }
}