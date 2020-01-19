Shader "Unlit/SinglePivot_Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _XVectorTex("X Vector", 2D) = "white" {}
        _PivotPosTex("Pivot Position", 2D) = "white" {}
        _WindDir("Wind Direction", Vector) = (0.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _XVectorTex;
            sampler2D _PivotPosTex;
            float4 _PivotPosTex_HDR;

            float4 _MainTex_ST;
            float3 _WindDir;

            void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
            {
                float s = sin(Rotation);
                float c = cos(Rotation);
                float one_minus_c = 1.0 - c;

                Axis = normalize(Axis);
                float3x3 rot_mat =
                {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
                    one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
                    one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
                };
                Out = mul(rot_mat, In);
            }

            v2f vert (appdata v)
            {
                v2f o;

                // X vector in world space
                //float3 xVector = tex2Dlod(_XVectorTex, float4(v.uv1.xy, 0, 0)).rgb * 2.0 - 1.0;
                float3 xVector = UnpackNormal(tex2Dlod(_XVectorTex, float4(v.uv1.xy, 0, 0))).xzy;
                xVector = mul(unity_ObjectToWorld, float4(xVector, 0)).xyz;
                //float3 xVector = float3(0, 1, 0);

                // The pivot point position in world space
                float3 pivotPos = DecodeHDR(tex2Dlod(_PivotPosTex, float4(v.uv1.xy, 0, 0)), _PivotPosTex_HDR).xzy;
                pivotPos = float3(-pivotPos.x, pivotPos.y, pivotPos.z);
                // fix FBX import scaling
                pivotPos *= 0.01f;
                pivotPos = mul(unity_ObjectToWorld, float4(pivotPos, 1.0f)).xyz;

                // Rotation axis in world space
                float3 axis = cross(normalize(_WindDir.xyz), xVector);

                if (length(axis) > 0)
                {
                    axis = normalize(axis);

                    //float3 vWorldCenter = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                    float3 vWorldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 offsetPos = vWorldPos - pivotPos;
                    float3 vertexPos;

                    Unity_RotateAboutAxis_Degrees_float(offsetPos, axis, length(_WindDir), vertexPos);

                    vertexPos = vertexPos + pivotPos;

                    o.vertex = mul(UNITY_MATRIX_VP, float4(vertexPos, 1.0f));
                }
                else 
                {
                    o.vertex = UnityObjectToClipPos(v.vertex);
                }
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv1 = v.uv1;
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // sample the texture
                half4 color = tex2D(_MainTex, i.uv);
                //half4 color = float4(DecodeHDR(tex2D(_PivotPosTex, i.uv1), _PivotPosTex_HDR) * 0.04f, 1.0);
                //half4 color = float4(DecodeHDR(tex2D(_PivotPosTex, i.uv1), _PivotPosTex_HDR), 1.0);
                //half4 color = float4(UnpackNormal(tex2D(_XVectorTex, i.uv1)), 1.0);
                //color = float4(color.g, color.b, -color.r, 1.0);

                //color = color * 0.5f + 0.5f;
                //color = mul(unity_ObjectToWorld, color);
                return color;
            }
            ENDCG
        }
    }
}
