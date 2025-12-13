// Minimal DOTS-compatible version of FoliageWind shader
// Uses simple CBUFFER, no complex instancing
Shader "Custom/FoliageWind_SimpleDOTS"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.2
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.1
        _WindSpeed("Wind Speed", Range(0, 3)) = 0.5
        _WindStrength("Wind Strength", Range(0, 1.0)) = 0.15
        _ObjectScale("Object Scale", Range(1, 1000)) = 1.0
        _WiggleSpeed("Wiggle Speed", Range(0, 10)) = 3.0
        _WiggleAmount("Wiggle Amount", Range(0, 0.05)) = 0.01
        _WiggleScale("Wiggle Scale", Range(0.1, 50)) = 15.0
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color : COLOR;
                half fogCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            float3 _GlobalWindDirection;
            float _GlobalWindStrength;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _Smoothness;
                float _Metallic;
                float _WindSpeed;
                float _WindStrength;
                float _ObjectScale;
                float _WiggleSpeed;
                float _WiggleAmount;
                float _WiggleScale;
            CBUFFER_END
            
            float smoothWave(float t)
            {
                return sin(t) * 0.5 + sin(t * 0.5) * 0.3 + sin(t * 0.25) * 0.2;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float swayAmount = input.color.r;
                float wiggleAmountVertex = input.color.g;
                float swayFactor = swayAmount * swayAmount;
                
                float swayTime = _Time.y * _WindSpeed * 0.5;
                float windSway = smoothWave(swayTime) * _WindStrength * swayFactor;
                
                float3 globalWindDir = normalize(_GlobalWindDirection);
                float3 windOffsetWS = globalWindDir * windSway * _GlobalWindStrength;
                
                #if defined(DOTS_INSTANCING_ON)
                    float3 windOffset = mul((float3x3)UNITY_MATRIX_I_M, windOffsetWS);
                #else
                    float3 windOffset = mul((float3x3)unity_WorldToObject, windOffsetWS);
                #endif
                
                float wiggleTime = _Time.y * _WiggleSpeed;
                float uvSeed = (input.uv.x * 17.3 + input.uv.y * 29.7) * _WiggleScale;
                float posSeed = (input.positionOS.x + input.positionOS.y * 2.1 + input.positionOS.z * 3.7) * 0.5;
                float normalSeed = (input.normalOS.x * 5.3 + input.normalOS.y * 7.1) * 2.0;
                float seed = uvSeed + posSeed + normalSeed;
                
                float wiggle1 = sin(wiggleTime + seed);
                float wiggle2 = sin(wiggleTime * 1.4 + seed + 2.5);
                
                float wiggleX = (wiggle1 + wiggle2 * 0.3) * _WiggleAmount * wiggleAmountVertex;
                float wiggleY = (wiggle2 * 0.5) * _WiggleAmount * wiggleAmountVertex;
                float wiggleZ = (wiggle1 * 0.4 + wiggle2 * 0.3) * _WiggleAmount * wiggleAmountVertex;
                
                float3 wiggleOffset = float3(wiggleX, wiggleY, wiggleZ);
                float3 totalOffset = (windOffset + wiggleOffset) * _ObjectScale;
                input.positionOS.xyz += totalOffset;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseMap * _BaseColor;
                clip(color.a - _Cutoff);
                
                half3 normalWS = normalize(input.normalWS);
                
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogCoord;
                inputData.bakedGI = SAMPLE_GI(0, 0, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = color.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = 0;
                surfaceData.occlusion = 1;
                surfaceData.alpha = color.a;
                
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
