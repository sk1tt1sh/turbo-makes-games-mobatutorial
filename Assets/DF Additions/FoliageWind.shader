Shader "Custom/FoliageWindURP_GlobalWind"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Surface Properties)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.2
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.1
        
        [Header(Wind Settings)]
        _WindSpeed("Trunk Sway Speed", Range(0, 2)) = 0.3
        _WindStrength("Trunk Sway Strength", Range(0, 0.3)) = 0.05
        // REMOVED: _WindDirection - now comes from global wind system
        _ObjectScale("Object Scale", Range(1, 1000)) = 1.0
        
        [Header(Leaf Wiggle)]
        _WiggleSpeed("Leaf Wiggle Speed", Range(0, 5)) = 2.0
        _WiggleAmount("Leaf Wiggle Amount", Range(0, 0.01)) = 0.003
        _WiggleScale("Wiggle Variation Scale", Range(0.1, 50)) = 10.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color : COLOR;
                half fogCoord : TEXCOORD3;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            // === GLOBAL WIND PROPERTIES ===
            // These are set by GlobalWindManager and shared across all materials
            // They default to safe values if GlobalWindManager isn't running
            float3 _GlobalWindDirection = float3(1, 0, 0);
            float _GlobalWindStrength = 1.0;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _Smoothness;
                float _Metallic;
                float _WindSpeed;
                float _WindStrength;
                // REMOVED: float3 _WindDirection;
                float _ObjectScale;
                float _WiggleSpeed;
                float _WiggleAmount;
                float _WiggleScale;
            CBUFFER_END
            
            // Smooth easing function for natural motion
            float smoothWave(float t)
            {
                return sin(t) * 0.5 + sin(t * 0.5) * 0.3 + sin(t * 0.25) * 0.2;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Get vertex color channels
                float swayAmount = input.color.r;
                float wiggleAmount = input.color.g;
                
                // SQUARED sway amount - base (red=0) NEVER moves
                float swayFactor = swayAmount * swayAmount;
                
                // === MAIN WIND SWAY - USES GLOBAL WIND ===
                float swayTime = _Time.y * _WindSpeed * 0.5;
                float windSway = smoothWave(swayTime) * _WindStrength * swayFactor;
                
                // Use global wind direction and apply global wind strength multiplier
                float3 globalWindDir = normalize(_GlobalWindDirection);
                float3 windOffset = globalWindDir * windSway * _GlobalWindStrength;
                
                // === LEAF WIGGLE - UV+POSITION+NORMAL VARIATION ===
                float wiggleTime = _Time.y * _WiggleSpeed;
                
                // Combine UV coordinates, object position, and normal for unique per-face seeds
                float uvSeed = (input.uv.x * 17.3 + input.uv.y * 29.7) * _WiggleScale;
                float posSeed = (input.positionOS.x + input.positionOS.y * 2.1 + input.positionOS.z * 3.7) * 0.5;
                float normalSeed = (input.normalOS.x * 5.3 + input.normalOS.y * 7.1) * 2.0;
                
                // Combine all seeds for maximum variation
                float seed = uvSeed + posSeed + normalSeed;
                
                // Two sine waves with different frequencies for natural motion
                float wiggle1 = sin(wiggleTime + seed);
                float wiggle2 = sin(wiggleTime * 1.4 + seed + 2.5);
                
                // Combine into gentle directional flutter
                float wiggleX = (wiggle1 + wiggle2 * 0.3) * _WiggleAmount * wiggleAmount;
                float wiggleY = (wiggle2 * 0.5) * _WiggleAmount * wiggleAmount;
                float wiggleZ = (wiggle1 * 0.4 + wiggle2 * 0.3) * _WiggleAmount * wiggleAmount;
                
                float3 wiggleOffset = float3(wiggleX, wiggleY, wiggleZ);
                
                // Combine wind and wiggle, scale for large objects
                float3 totalOffset = (windOffset + wiggleOffset) * _ObjectScale;
                
                // Apply offset
                input.positionOS.xyz += totalOffset;
                
                // Transform to world and clip space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                
                // Calculate fog
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseMap * _BaseColor;
                
                // Alpha cutoff
                clip(color.a - _Cutoff);
                
                // Get main light
                Light mainLight = GetMainLight();
                
                // Vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 lightDir = normalize(mainLight.direction);
                
                // Diffuse lighting
                float NdotL = saturate(dot(normalWS, lightDir));
                half3 diffuse = color.rgb * mainLight.color * (NdotL * 0.7 + 0.3);
                
                // Ambient
                half3 ambient = SampleSH(normalWS) * color.rgb * 0.3;
                
                // Specular reflection (Blinn-Phong)
                float3 halfVector = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specPower = exp2(10.0 * _Smoothness + 1.0);
                float specular = pow(NdotH, specPower) * _Smoothness;
                
                // Apply metallic
                half3 specularColor = lerp(mainLight.color, color.rgb * mainLight.color, _Metallic);
                half3 specularFinal = specular * specularColor * NdotL;
                
                // Combine
                half3 finalColor = diffuse + ambient + specularFinal;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogCoord);
                
                return half4(finalColor, color.a);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            // Global wind properties with safe defaults
            float3 _GlobalWindDirection = float3(1, 0, 0);
            float _GlobalWindStrength = 1.0;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Cutoff;
                float _WindSpeed;
                float _WindStrength;
                float _ObjectScale;
                float _WiggleSpeed;
                float _WiggleAmount;
                float _WiggleScale;
            CBUFFER_END
            
            // Same smooth wave function
            float smoothWave(float t)
            {
                return sin(t) * 0.5 + sin(t * 0.5) * 0.3 + sin(t * 0.25) * 0.2;
            }
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                float swayAmount = input.color.r;
                float wiggleAmount = input.color.g;
                
                float swayFactor = swayAmount * swayAmount;
                
                // Smoothed sway with global wind
                float swayTime = _Time.y * _WindSpeed * 0.5;
                float windSway = smoothWave(swayTime) * _WindStrength * swayFactor;
                float3 globalWindDir = normalize(_GlobalWindDirection);
                float3 windOffset = globalWindDir * windSway * _GlobalWindStrength;
                
                float wiggleTime = _Time.y * _WiggleSpeed;
                
                float uvSeed = (input.uv.x * 17.3 + input.uv.y * 29.7) * _WiggleScale;
                float posSeed = (input.positionOS.x + input.positionOS.y * 2.1 + input.positionOS.z * 3.7) * 0.5;
                float normalSeed = (input.normalOS.x * 5.3 + input.normalOS.y * 7.1) * 2.0;
                float seed = uvSeed + posSeed + normalSeed;
                
                float wiggle1 = sin(wiggleTime + seed);
                float wiggle2 = sin(wiggleTime * 1.4 + seed + 2.5);
                
                float wiggleX = (wiggle1 + wiggle2 * 0.3) * _WiggleAmount * wiggleAmount;
                float wiggleY = (wiggle2 * 0.5) * _WiggleAmount * wiggleAmount;
                float wiggleZ = (wiggle1 * 0.4 + wiggle2 * 0.3) * _WiggleAmount * wiggleAmount;
                
                float3 totalOffset = (windOffset + float3(wiggleX, wiggleY, wiggleZ)) * _ObjectScale;
                
                input.positionOS.xyz += totalOffset;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
