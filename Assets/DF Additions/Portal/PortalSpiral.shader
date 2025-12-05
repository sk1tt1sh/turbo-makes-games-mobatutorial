Shader "Custom/UltimatePortalCombined"
{
    Properties
    {
        [Header(Background Painterly Image)]
        _BackgroundTex("Background Portal Image", 2D) = "white" {}
        _BackgroundColor("Background Tint", Color) = (1, 1, 1, 1)
        _BackgroundBrightness("Background Brightness", Range(0, 5)) = 1.0
        _BackgroundGlowSpeed("Background Glow Pulse Speed", Range(0, 5)) = 1.0
        _BackgroundGlowAmount("Background Glow Pulse Amount", Range(0, 1)) = 0.2
        _BackgroundOpacity("Background Opacity", Range(0, 1)) = 0.8
        
        [Header(Spinning Spiral Texture)]
        _MainTex("Spiral Texture Asset", 2D) = "white" {}
        _SpiralColor("Spiral Tint", Color) = (1, 1, 1, 1)
        _RotationSpeed("Spiral Rotation Speed", Range(-5, 5)) = 1.0
        _SpiralZoom("Spiral Zoom", Range(0.1, 5.0)) = 0.3
        _SpiralOpacity("Spiral Opacity", Range(0, 1)) = 0.7
        _UseTexture("Use Spiral Texture", Float) = 1.0
        
        [Header(Energy Ripples)]
        _RippleSpeed("Ripple Speed", Range(0, 5)) = 2.0
        _RippleScale("Ripple Scale", Range(0.5, 10)) = 4.0
        _RippleIntensity("Ripple Intensity", Range(0, 5)) = 1.5
        _NoiseScale("Noise Detail", Range(1, 10)) = 4.0
        _RippleOpacity("Ripple Opacity", Range(0, 1)) = 0.5
        
        [Header(Painterly Colors)]
        _Color1("Color 1", Color) = (0.2, 0.4, 1.0, 1)      // Blue
        _Color2("Color 2", Color) = (0.6, 0.2, 1.0, 1)      // Purple
        _Color3("Color 3", Color) = (1.0, 0.4, 0.6, 1)      // Pink
        _ColorCycleSpeed("Color Cycle Speed", Range(0, 5)) = 1.0
        _ColorSaturation("Color Saturation", Range(0, 2)) = 1.2
        _ColorBrightness("Color Brightness", Range(0, 5)) = 2.0
        
        [Header(Electric Edge Glow)]
        _EdgeGlowColor("Edge Glow Color", Color) = (0.5, 0.8, 1.0, 1)
        _EdgeGlowWidth("Edge Glow Width", Range(0, 0.5)) = 0.15
        _EdgeGlowIntensity("Edge Glow Intensity", Range(0, 10)) = 3.0
        _EdgeFlickerSpeed("Edge Flicker Speed", Range(0, 20)) = 8.0
        _EdgeFlickerAmount("Edge Flicker Amount", Range(0, 1)) = 0.3
        
        [Header(Portal Shape)]
        _PortalShape("Portal Shape (0=Rectangle, 1=Circle)", Range(0, 1)) = 0.0
        _Softness("Edge Softness", Range(0, 1)) = 0.3
        _Brightness("Overall Brightness", Range(0, 5)) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_BackgroundTex);
            SAMPLER(sampler_BackgroundTex);
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BackgroundTex_ST;
                float4 _MainTex_ST;
                float4 _BackgroundColor;
                float4 _SpiralColor;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _EdgeGlowColor;
                float _BackgroundBrightness;
                float _BackgroundGlowSpeed;
                float _BackgroundGlowAmount;
                float _BackgroundOpacity;
                float _RotationSpeed;
                float _SpiralZoom;
                float _SpiralOpacity;
                float _UseTexture;
                float _RippleSpeed;
                float _RippleScale;
                float _RippleIntensity;
                float _NoiseScale;
                float _RippleOpacity;
                float _ColorCycleSpeed;
                float _ColorSaturation;
                float _ColorBrightness;
                float _EdgeGlowWidth;
                float _EdgeGlowIntensity;
                float _EdgeFlickerSpeed;
                float _EdgeFlickerAmount;
                float _PortalShape;
                float _Softness;
                float _Brightness;
            CBUFFER_END
            
            #define PI 3.14159265359
            
            // === NOISE FUNCTIONS FROM PORTALSPIRAL2 ===
            
            // Random function
            float random(float p)
            {
                return frac(sin(p) * 10000.0);
            }
            
            // 2D Noise
            float noise(float2 p)
            {
                float t = _Time.y / 20.0;
                t = frac(t);
                return random(p.x * 14.0 + p.y * sin(t) * 0.5);
            }
            
            float2 sw(float2 p) { return float2(floor(p.x), floor(p.y)); }
            float2 se(float2 p) { return float2(ceil(p.x), floor(p.y)); }
            float2 nw(float2 p) { return float2(floor(p.x), ceil(p.y)); }
            float2 ne(float2 p) { return float2(ceil(p.x), ceil(p.y)); }
            
            // Smooth noise
            float smoothNoise(float2 p)
            {
                float2 inter = smoothstep(0.0, 1.0, frac(p));
                float s = lerp(noise(sw(p)), noise(se(p)), inter.x);
                float n = lerp(noise(nw(p)), noise(ne(p)), inter.x);
                return lerp(s, n, inter.y);
            }
            
            // Circular ripple function
            float circ(float2 p)
            {
                float r = length(p);
                r = log(sqrt(r));
                return abs(fmod(4.0 * r, PI * 2.0) - PI) * 3.0 + 0.2;
            }
            
            // Fractal Brownian Motion for detail
            float fbm(float2 p)
            {
                float z = 2.0;
                float rz = 0.0;
                for(float i = 1.0; i < 6.0; i++)
                {
                    rz += abs((smoothNoise(p) - 0.5) * 2.0) / z;
                    z *= 2.0;
                    p *= 2.0;
                }
                return rz;
            }
            
            // === COLOR CYCLING FROM PORTALSPIRAL1 ===
            
            // Smooth color cycling
            float3 smoothColorCycle(float value, float3 col1, float3 col2, float3 col3)
            {
                value = frac(value);
                value *= 3.0; // 3 colors
                
                if (value < 1.0)
                    return lerp(col1, col2, value);
                else if (value < 2.0)
                    return lerp(col2, col3, value - 1.0);
                else
                    return lerp(col3, col1, value - 2.0);
            }
            
            // Rotation
            float2 rotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(
                    p.x * c - p.y * s,
                    p.x * s + p.y * c
                );
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // === LAYER 1: BACKGROUND IMAGE ===
                half4 backgroundTex = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, input.uv);
                
                // Pulsating glow on background
                float bgPulse = sin(_Time.y * _BackgroundGlowSpeed) * _BackgroundGlowAmount + 1.0;
                float3 backgroundLayer = backgroundTex.rgb * _BackgroundColor.rgb * _BackgroundBrightness * bgPulse;
                
                // === SETUP FOR EFFECTS ===
                float2 uv = input.uv - 0.5;
                float dist = length(uv);
                
                // Rotation
                float angle = _Time.y * _RotationSpeed;
                float2 rotatedUV = rotate(uv, angle);
                
                // === LAYER 2: SPINNING SPIRAL TEXTURE ===
                // Scale UV for zoom control
                float2 scaledUV = rotatedUV / _SpiralZoom;
                half4 spiralTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scaledUV + 0.5);
                float3 spiralLayer = spiralTex.rgb * _SpiralColor.rgb * _UseTexture;
                
                // === LAYER 3: ENERGY RIPPLES ===
                float2 p = rotatedUV * _RippleScale;
                
                // Fractal noise for turbulence
                float rz = fbm(p * _NoiseScale);
                
                // Animated expansion/contraction - SLOWED DOWN for portal effect
                // Use a slower time multiplier for meandering effect instead of frantic
                float rippleTime = _Time.y * _RippleSpeed * 0.5; // Halved for slower control
                p /= (1.0 + sin(rippleTime) * 0.3); // Gentler oscillation instead of exp()
                
                // Circular ripples
                rz *= pow(abs(0.1 - circ(p)), 0.9);
                rz *= _RippleIntensity;
                
                // Color the ripples with painterly color cycling
                float colorValue = dist * 2.0 - frac(_Time.y * _ColorCycleSpeed);
                float3 rippleColor = smoothColorCycle(colorValue, _Color1.rgb, _Color2.rgb, _Color3.rgb);
                rippleColor *= rz * _ColorBrightness * _RippleOpacity;
                rippleColor = pow(rippleColor, _ColorSaturation);
                
                // === LAYER 4: ELECTRIC EDGE GLOW (FROM PORTALSPIRAL1) ===
                float edgeDist = abs(dist - 0.5);
                
                // Flickering effect (from PortalSpiral1 - the good one!)
                float flicker = sin(_Time.y * _EdgeFlickerSpeed + noise(input.uv * 20.0) * 10.0) * _EdgeFlickerAmount + 1.0;
                
                // Edge glow intensity
                float edgeGlow = 1.0 - smoothstep(0.0, _EdgeGlowWidth, edgeDist);
                edgeGlow = pow(edgeGlow, 2.0) * _EdgeGlowIntensity * flicker;
                
                // Add electric noise to edge
                float electricNoise = noise(input.uv * 50.0 + _Time.y * 2.0);
                edgeGlow *= (0.7 + electricNoise * 0.3);
                
                float3 edgeColor = _EdgeGlowColor.rgb * edgeGlow;
                
                // === COMBINE ALL LAYERS ===
                float3 finalColor = backgroundLayer * _BackgroundOpacity;
                finalColor += spiralLayer * _SpiralOpacity;
                finalColor += rippleColor;
                finalColor += edgeColor;
                
                // Overall brightness
                finalColor *= _Brightness;
                
                // === PORTAL SHAPE (RECTANGLE OR CIRCLE) ===
                float softEdge;
                
                if (_PortalShape > 0.5)
                {
                    // Circular portal
                    softEdge = 1.0 - smoothstep(0.5 - _Softness, 0.5, dist);
                }
                else
                {
                    // Rectangular portal
                    float2 edgeDist2 = abs(input.uv - 0.5);
                    float maxDist = max(edgeDist2.x, edgeDist2.y);
                    softEdge = 1.0 - smoothstep(0.5 - _Softness, 0.5, maxDist);
                }
                
                // === ALPHA ===
                float alpha = softEdge * max(_BackgroundOpacity, backgroundTex.a);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
