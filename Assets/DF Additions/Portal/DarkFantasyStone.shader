Shader "Custom/DarkFantasyStone_DOTS"
{
    Properties
    {
        [Header(Base Texture)]
        _BaseMap("Stone Albedo Texture", 2D) = "white" {}
        _BaseColor("Base Tint", Color) = (0.5, 0.5, 0.5, 1)
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 1.0
        
        [Header(Surface Properties)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.15
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        
        [Header(Dark Fantasy Atmosphere)]
        _Darkness("Overall Darkness", Range(0, 1)) = 0.3
        _Desaturation("Desaturation", Range(0, 1)) = 0.4
        _ContrastBoost("Contrast Boost", Range(0, 1)) = 0.2
        
        [Header(Grit and Weathering)]
        _DirtColor("Dirt/Grime Color", Color) = (0.15, 0.12, 0.1, 1)
        _DirtAmount("Dirt Amount", Range(0, 1)) = 0.3
        _DirtContrast("Dirt Contrast", Range(0, 5)) = 2.0
        _WeatheringScale("Weathering Detail Scale", Range(1, 50)) = 15.0
        
        [Header(Cracks and Detail)]
        _CrackDepth("Crack Darkness", Range(0, 1)) = 0.5
        _CrackScale("Crack Scale", Range(1, 100)) = 25.0
        
        [Header(Moss and Organic Growth)]
        _MossColor("Moss Color", Color) = (0.15, 0.2, 0.1, 1)
        _MossAmount("Moss Amount", Range(0, 1)) = 0.2
        _MossScale("Moss Scale", Range(1, 50)) = 20.0
        
        [Header(Ambient Occlusion)]
        _AOStrength("AO Strength", Range(0, 1)) = 0.5
        _AOScale("AO Scale", Range(1, 100)) = 30.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogCoord : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
                UNITY_DEFINE_INSTANCED_PROP(float4, _NormalMap_ST)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _DirtColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MossColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _Darkness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Desaturation)
                UNITY_DEFINE_INSTANCED_PROP(float, _ContrastBoost)
                UNITY_DEFINE_INSTANCED_PROP(float, _DirtAmount)
                UNITY_DEFINE_INSTANCED_PROP(float, _DirtContrast)
                UNITY_DEFINE_INSTANCED_PROP(float, _WeatheringScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _CrackDepth)
                UNITY_DEFINE_INSTANCED_PROP(float, _CrackScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _MossAmount)
                UNITY_DEFINE_INSTANCED_PROP(float, _MossScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _AOStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _AOScale)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
            
            // Simple hash function for procedural noise
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.13);
                p3 += dot(p3, p3.yzx + 3.333);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            // 2D Noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal noise for detailed variation
            float fbm(float2 p, float scale)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency * scale);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            // Voronoi-like cracks
            float cracks(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 i = floor(p);
                float2 f = frac(p);
                
                float minDist = 1.0;
                
                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 cellPoint = hash(i + neighbor) * float2(1.0, 1.0);
                        float2 diff = neighbor + cellPoint - f;
                        float dist = length(diff);
                        minDist = min(minDist, dist);
                    }
                }
                
                return minDist;
            }
            
            // Desaturate color
            float3 desaturate(float3 color, float amount)
            {
                float gray = dot(color, float3(0.299, 0.587, 0.114));
                return lerp(color, float3(gray, gray, gray), amount);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                
                float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
                output.uv = input.uv * baseST.xy + baseST.zw;
                
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
                half3 baseColorRGB = baseMap.rgb * baseColor.rgb;
                
                // Sample and apply normal map
                float normalStrength = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalStrength);
                half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                half3 tangentNormal = UnpackNormalScale(normalMap, normalStrength);
                
                // Construct TBN matrix and transform normal to world space
                half3 normalWS = normalize(
                    tangentNormal.x * input.tangentWS +
                    tangentNormal.y * input.bitangentWS +
                    tangentNormal.z * input.normalWS
                );
                
                // Get instanced properties
                float weatheringScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _WeatheringScale);
                float dirtAmount = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DirtAmount);
                float dirtContrast = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DirtContrast);
                float4 dirtColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DirtColor);
                float crackScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CrackScale);
                float crackDepth = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CrackDepth);
                float mossScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MossScale);
                float mossAmount = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MossAmount);
                float4 mossColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MossColor);
                float aoScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AOScale);
                float aoStrength = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AOStrength);
                float desaturation = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Desaturation);
                float darkness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Darkness);
                float contrastBoost = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ContrastBoost);
                
                // === PROCEDURAL DIRT AND GRIME ===
                float dirtNoise = fbm(input.uv, weatheringScale);
                dirtNoise = pow(dirtNoise, dirtContrast); // Increase contrast
                float3 dirtColorResult = lerp(baseColorRGB, dirtColor.rgb, dirtNoise * dirtAmount);
                
                // === CRACKS ===
                float crackPattern = cracks(input.uv, crackScale);
                crackPattern = smoothstep(0.0, 0.1, crackPattern);
                float3 crackedColor = lerp(dirtColorResult * (1.0 - crackDepth), dirtColorResult, crackPattern);
                
                // === MOSS (grows in crevices) ===
                float mossNoise = fbm(input.uv, mossScale);
                float mossMask = (1.0 - crackPattern) * mossNoise; // Moss in cracks
                mossMask = smoothstep(0.3, 0.7, mossMask);
                float3 mossColorResult = lerp(crackedColor, mossColor.rgb, mossMask * mossAmount);
                
                // === AMBIENT OCCLUSION (procedural) ===
                float ao = fbm(input.uv, aoScale);
                ao = lerp(1.0, ao, aoStrength);
                mossColorResult *= ao;
                
                // === DARK FANTASY ATMOSPHERE ===
                // Desaturate for grim look
                mossColorResult = desaturate(mossColorResult, desaturation);
                
                // Darken overall
                mossColorResult *= (1.0 - darkness);
                
                // Boost contrast for gritty feel
                mossColorResult = pow(mossColorResult, 1.0 - contrastBoost * 0.3);
                
                // === LIGHTING ===
                Light mainLight = GetMainLight();
                
                float3 lightDir = normalize(mainLight.direction);
                
                // Diffuse (now using normal-mapped normal)
                float NdotL = saturate(dot(normalWS, lightDir));
                half3 diffuse = mossColorResult * mainLight.color * (NdotL * 0.6 + 0.4); // Soft lighting
                
                // Ambient (from light probes, using normal-mapped normal)
                half3 ambient = SampleSH(normalWS) * mossColorResult * 0.4;
                
                // Combine
                half3 finalColor = diffuse + ambient;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogCoord);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
        
        // DepthOnly pass (required for DOTS rendering)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask R
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
