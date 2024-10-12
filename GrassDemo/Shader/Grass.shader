Shader "Custom/URP_DoubleSidedLighting"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ColorDiffHeight("ColorDiffHeight", Range(0.0, 0.99)) = 0.5
        _ColorBlendFact("ColorBlendFact", Range(0.5, 2)) = 1
        _Ambient ("Ambient", Color) = (1,1,1,1)
        _AmbientStrength ("AmbientStrength", Range(0.0, 1)) = 1
        _Thickness ("Thickness", Range(-5, 0)) = 0
        _ThicknessAngle ("ThicknessAngle", Range(0.785, 1.57)) = 1
        _ThicknessAngleFactor ("ThicknessAngleFactor", Range(0, 2)) = 0
        _BezierPos0("BezierPos0", Vector) = (0, 0, 0)
        _BezierPos1("BezierPos1", Vector) = (0, 0, 0)
        _BezierPos2("BezierPos2", Vector) = (0, 0, 0)
        _BezierPos3("BezierPos3", Vector) = (0, 0, 0)
        _WindDir("WindDir", Vector) = (0, 0, 0)
        _WindStrength("WindStrength", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend One Zero
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                float3 lightNormalWS    : TEXCOORD3;
                float3 testColor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BaseColor;
                float4 _Ambient;
                float4 _BaseMap_ST;
                float3 _BezierPos0;
                float3 _BezierPos1;
                float3 _BezierPos2;
                float3 _BezierPos3;
                float3 _WindDir;
                float _WindStrength;
                float _AmbientStrength;
                float _Thickness;
                float _ThicknessAngle;
                float _ThicknessAngleFactor;
                float _ColorDiffHeight;
                float _ColorBlendFact;
                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);
            CBUFFER_END

            float3 linear_bezier(float3 pos0, float3 pos1, float t)
            {
                return pos0 + t*(pos1 - pos0);
            }

            float3 quadratic_bezier(float3 pos0, float3 pos1, float3 pos2, float t)
            {
                return pow(1 - t, 2) * pos0 + 2 * t * (1 - t) * pos1 + pow(t, 2) * pos2;
            }

            float3 cubic_bezier(float3 pos0, float3 pos1, float3 pos2, float3 pos3, float t)
            {
                return pos0 * pow(1 - t, 3) + 3 * pos1 * t * pow(1 - t, 2) + 3 * pos2 * pow(t, 2) * (1 - t) + pos3 * pow(t, 3);
            }

            float3 bezier_derivatives(float3 pos0, float3 pos1, float3 pos2, float3 pos3, float t)
            {
                return -3 * pow(1 - t, 2) * pos0 + 3 * (1 - t)*(1 - 3 * t) * pos1 + 3 * t * (2 - 3 * t) * pos2 + 3 *pow(t, 2) * pos3;
            }

            float3 thickness_normal(float x)
            {
                return normalize(float3(2 * _Thickness * x - _Thickness, 0, 1));
            }

            float sidethick_cal(float angle)
            {
                return -0.5 * angle * (angle - (1.57-_ThicknessAngle + _ThicknessAngleFactor));
            }

            float3 MovePointTowardsDirection(float3 originalPoint, float3 targetDirection, float magnitude)
            {

                if (length(targetDirection) == 0.0)
                {
                    // 如果targetDirection为零向量，则返回原始点
                    return originalPoint;
                }
                
                // 规范化targetDirection为单位向量，确保它是单位向量
                targetDirection = normalize(targetDirection);
            
                // 计算原向量的长度
                float originalLength = length(originalPoint);
            
                // 计算新的方向向量：从原向量向目标方向移动一部分
                float3 newDirection = normalize(lerp(normalize(originalPoint), targetDirection, magnitude));
            
                // 保持向量长度不变，计算新的点
                float3 newPoint = newDirection * originalLength;
            
                return newPoint;
            }

            
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 viewDir = normalize(GetWorldSpaceViewDir(TransformObjectToWorld(input.positionOS)));
                float3 thicknessNormalOS = thickness_normal(input.uv.x);
                float3 viewerPosOS = mul(unity_WorldToObject,float4(_WorldSpaceCameraPos, 1));
                viewerPosOS.y = 0;
                viewerPosOS = normalize(viewerPosOS);

                _BezierPos1 = MovePointTowardsDirection(_BezierPos1, _WindDir, _WindStrength);
                _BezierPos2 = MovePointTowardsDirection(_BezierPos2, _WindDir, _WindStrength);
                _BezierPos3 = MovePointTowardsDirection(_BezierPos3, _WindDir, _WindStrength);
                float3 bezierPos = cubic_bezier(_BezierPos0, _BezierPos1, _BezierPos2, _BezierPos3, input.uv.y);
                input.positionOS.yz = bezierPos.yz;
                input.positionOS.x += bezierPos.x;
                
                if (abs(viewerPosOS.z) < cos(_ThicknessAngle))
                {
                    float tempA = acos(dot(float3(0, 0, 1), viewerPosOS));
                    float angle = tempA <= 1.57 ? sidethick_cal(tempA - _ThicknessAngle) : -1 * sidethick_cal(3.14 - tempA - _ThicknessAngle);
                    angle *= viewerPosOS.x/abs(viewerPosOS.x);
                    float2x2 m_rotate = float2x2(cos(angle), sin(angle), -sin(angle), cos(angle));
                    input.positionOS.xz = mul(m_rotate, input.positionOS.xz);
                }
                
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                float3 derivatives = bezier_derivatives(_BezierPos0, _BezierPos1, _BezierPos2, _BezierPos3, input.uv.y);
                float3 calNormalOS = normalize(float3(0, 1, -1 * derivatives.y/derivatives.z));
                if (dot(mul(unity_ObjectToWorld, float4(calNormalOS, 0.0)), viewDir) < 0.0)
                {
                    calNormalOS *=-1;
                }
                /*if(calNormalOS.z > 0)
                {
                    calNormalOS *=-1;
                    output.testColor = float3(1,1,1);
                }*/
                output.lightNormalWS = TransformObjectToWorldNormal(float3(thicknessNormalOS.x*calNormalOS.z/thicknessNormalOS.z,calNormalOS.y,calNormalOS.z));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.viewDirWS = viewDir;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                /*if(input.testColor.x == 1)
                {
                    return half4(1, 1, 1, 1.0);
                }
                else
                {
                    return half4(0, 0, 0, 1.0);
                }*/
                half3 normalWS = normalize(input.normalWS);
                half3 lightNormalWS = normalize(input.lightNormalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                // 获取主光源
                Light mainLight = GetMainLight();
                half3 lightDirWS = normalize(mainLight.direction);

                // 判断法线方向并反转背面的法线
                if (dot(normalWS, viewDirWS) < 0.0)
                {
                    //lightNormalWS.z = -lightNormalWS.z;
                    _AmbientStrength = _AmbientStrength * 1.5;
                    //return half4(1, 1, 1, 1.0);
                }
                float3 blendColor = lerp(_BaseColor, _TopColor, pow(max(0, input.uv.y - _ColorDiffHeight)/(1 - _ColorDiffHeight), _ColorBlendFact));
                // 计算光照
                half3 diffuse = blendColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half NdotL = max(0.0, dot(lightNormalWS, lightDirWS));
                half3 lighting = NdotL * mainLight.color.rgb * diffuse + _Ambient * _AmbientStrength;
                
                return half4(lighting, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}