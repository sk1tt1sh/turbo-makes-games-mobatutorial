Shader "TLB/RealisticFire_SimpleDOTS"
{
    Properties
    {
        [Header(Texture)]
        _MainTex ("Particle Texture (Alpha Mask)", 2D) = "white" {}
        
        [Header(Fire Colors)]
        _ColorCore ("Core Color (White-Yellow)", Color) = (1, 1, 0.8, 1)
        _ColorMid ("Mid Color (Orange)", Color) = (1, 0.5, 0, 1)
        _ColorEdge ("Edge Color (Red)", Color) = (1, 0.1, 0, 1)
        _ColorTip ("Tip Color (Dark)", Color) = (0.1, 0.05, 0, 0)
        
        [Header(Fire Shape)]
        _FireHeight ("Fire Height", Range(0.5, 3)) = 1.5
        _FireWidth ("Fire Width", Range(0.5, 2)) = 1.0
        _Flickering ("Flickering Intensity", Range(0, 1)) = 0.3
        
        [Header(Animation)]
        _RiseSpeed ("Rise Speed", Range(0.5, 3)) = 1.5
        _FlickerSpeed ("Flicker Speed", Range(1, 10)) = 3.0
        _TurbulenceScale ("Turbulence Scale", Range(0.5, 5)) = 2.0
        _TurbulenceStrength ("Turbulence Strength", Range(0, 1)) = 0.4
        
        [Header(Distortion)]
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.15
        _DistortionSpeed ("Distortion Speed", Range(0.5, 3)) = 1.2
        
        [Header(Emission)]
        _EmissionStrength ("Emission Strength", Range(1, 20)) = 5.0
        _GlowFalloff ("Glow Falloff", Range(0.1, 3)) = 1.0
        
        [Header(Style)]
        _DetailAmount ("Detail Amount", Range(0, 1)) = 0.7
        _ColorBands ("Color Bands (Painterly)", Range(3, 20)) = 12
        _Contrast ("Contrast", Range(0.5, 2)) = 1.2
        
        [Header(Alpha)]
        _AlphaFalloff ("Alpha Falloff", Range(0.5, 5)) = 2.0
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.3
        _SoftParticleDistance ("Soft Particle Distance", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ColorCore;
                float4 _ColorMid;
                float4 _ColorEdge;
                float4 _ColorTip;
                float _FireHeight;
                float _FireWidth;
                float _Flickering;
                float _RiseSpeed;
                float _FlickerSpeed;
                float _TurbulenceScale;
                float _TurbulenceStrength;
                float _DistortionStrength;
                float _DistortionSpeed;
                float _EmissionStrength;
                float _GlowFalloff;
                float _DetailAmount;
                float _ColorBands;
                float _Contrast;
                float _AlphaFalloff;
                float _EdgeSoftness;
                float _SoftParticleDistance;
            CBUFFER_END
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }
            
            float fireShape(float2 uv, float time, float detailAmount)
            {
                float2 centered = uv - 0.5;
                float dist = length(centered * float2(_FireWidth, 1.0));
                float baseShape = 1.0 - smoothstep(0.0, 0.5, dist);
                float heightFactor = pow(1.0 - uv.y, 0.7);
                baseShape *= heightFactor;
                
                float flicker1 = noise(float2(atan2(centered.y, centered.x) * 3.0, time * _FlickerSpeed)) * _Flickering;
                float flicker2 = noise(float2(atan2(centered.y, centered.x) * 5.0, time * _FlickerSpeed * 1.3)) * _Flickering * 0.5;
                float combinedFlicker = (flicker1 + flicker2);
                baseShape += combinedFlicker * (1.0 - uv.y) * 0.3;
                
                float2 distortionUV1 = uv * float2(3.0, 2.0) - float2(0, time * _RiseSpeed);
                float2 distortionUV2 = uv * float2(2.0, 3.0) - float2(time * 0.3, time * _RiseSpeed * 0.7);
                
                float distortion1 = fbm(distortionUV1, 4) * _DistortionStrength;
                float distortion2 = fbm(distortionUV2, 3) * _DistortionStrength * 0.5;
                float distortion = (distortion1 + distortion2) * detailAmount;
                
                float2 distortedUV = uv;
                distortedUV.x += distortion * (1.0 - uv.y);
                distortedUV.y += distortion * 0.5;
                
                float turbulence = fbm(distortedUV * _TurbulenceScale - float2(0, time * _RiseSpeed * 0.5), 5);
                turbulence = (turbulence * 2.0 - 1.0) * _TurbulenceStrength * detailAmount;
                
                float fire = baseShape + turbulence * heightFactor;
                fire = saturate(fire * _FireHeight);
                
                return fire;
            }
            
            float3 posterize(float3 color, float bands)
            {
                return floor(color * bands) / bands;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float time = _Time.y;
                float2 uv = input.uv;
                
                float4 textureSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float textureMask = textureSample.a;
                
                uv.x = (uv.x - 0.5) / _FireWidth + 0.5;
                
                float fireIntensity = fireShape(uv, time, _DetailAmount);
                
                if(fireIntensity < 0.01)
                    discard;
                
                float heightGradient = uv.y;
                
                float colorNoise = fbm(uv * 5.0 - float2(0, time * _RiseSpeed), 2);
                heightGradient = saturate(heightGradient + colorNoise * 0.2 - 0.1);
                
                float3 fireColor;
                
                if(heightGradient < 0.3)
                {
                    fireColor = lerp(_ColorCore.rgb, _ColorMid.rgb, heightGradient / 0.3);
                }
                else if(heightGradient < 0.6)
                {
                    fireColor = lerp(_ColorMid.rgb, _ColorEdge.rgb, (heightGradient - 0.3) / 0.3);
                }
                else
                {
                    fireColor = lerp(_ColorEdge.rgb, _ColorTip.rgb, (heightGradient - 0.6) / 0.4);
                }
                
                fireColor *= lerp(0.5, 1.5, pow(fireIntensity, 1.0 / _Contrast));
                fireColor = posterize(fireColor, _ColorBands);
                
                float3 emission = fireColor * _EmissionStrength;
                
                float coreGlow = saturate((1.0 - heightGradient * 2.0) * fireIntensity);
                coreGlow = pow(coreGlow, _GlowFalloff);
                emission += _ColorCore.rgb * coreGlow * 3.0;
                
                float alpha = fireIntensity;
                alpha *= pow(1.0 - heightGradient, _AlphaFalloff);
                
                float edgeFade = 1.0 - abs(uv.x - 0.5) * 2.0;
                edgeFade = smoothstep(0.0, _EdgeSoftness, edgeFade);
                alpha *= edgeFade;
                
                emission *= input.color.rgb;
                alpha *= input.color.a;
                alpha *= textureMask;
                
                emission = MixFog(emission, input.fogFactor);
                
                return half4(emission, saturate(alpha));
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}
