Shader "Custom/DarkFantasyStone"
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
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
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                float4 _BaseColor;
                float4 _DirtColor;
                float4 _MossColor;
                float _Smoothness;
                float _Metallic;
                float _NormalStrength;
                float _Darkness;
                float _Desaturation;
                float _ContrastBoost;
                float _DirtAmount;
                float _DirtContrast;
                float _WeatheringScale;
                float _CrackDepth;
                float _CrackScale;
                float _MossAmount;
                float _MossScale;
                float _AOStrength;
                float _AOScale;
            CBUFFER_END
            
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
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseColor = baseMap.rgb * _BaseColor.rgb;
                
                // Sample and apply normal map
                half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                half3 tangentNormal = UnpackNormalScale(normalMap, _NormalStrength);
                
                // Construct TBN matrix and transform normal to world space
                half3 normalWS = normalize(
                    tangentNormal.x * input.tangentWS +
                    tangentNormal.y * input.bitangentWS +
                    tangentNormal.z * input.normalWS
                );
                
                // === PROCEDURAL DIRT AND GRIME ===
                float dirtNoise = fbm(input.uv, _WeatheringScale);
                dirtNoise = pow(dirtNoise, _DirtContrast); // Increase contrast
                float3 dirtColor = lerp(baseColor, _DirtColor.rgb, dirtNoise * _DirtAmount);
                
                // === CRACKS ===
                float crackPattern = cracks(input.uv, _CrackScale);
                crackPattern = smoothstep(0.0, 0.1, crackPattern);
                float3 crackedColor = lerp(dirtColor * (1.0 - _CrackDepth), dirtColor, crackPattern);
                
                // === MOSS (grows in crevices) ===
                float mossNoise = fbm(input.uv, _MossScale);
                float mossMask = (1.0 - crackPattern) * mossNoise; // Moss in cracks
                mossMask = smoothstep(0.3, 0.7, mossMask);
                float3 mossColor = lerp(crackedColor, _MossColor.rgb, mossMask * _MossAmount);
                
                // === AMBIENT OCCLUSION (procedural) ===
                float ao = fbm(input.uv, _AOScale);
                ao = lerp(1.0, ao, _AOStrength);
                mossColor *= ao;
                
                // === DARK FANTASY ATMOSPHERE ===
                // Desaturate for grim look
                mossColor = desaturate(mossColor, _Desaturation);
                
                // Darken overall
                mossColor *= (1.0 - _Darkness);
                
                // Boost contrast for gritty feel
                mossColor = pow(mossColor, 1.0 - _ContrastBoost * 0.3);
                
                // === LIGHTING ===
                Light mainLight = GetMainLight();
                
                float3 lightDir = normalize(mainLight.direction);
                
                // Diffuse (now using normal-mapped normal)
                float NdotL = saturate(dot(normalWS, lightDir));
                half3 diffuse = mossColor * mainLight.color * (NdotL * 0.6 + 0.4); // Soft lighting
                
                // Ambient (from light probes, using normal-mapped normal)
                half3 ambient = SampleSH(normalWS) * mossColor * 0.4;
                
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
