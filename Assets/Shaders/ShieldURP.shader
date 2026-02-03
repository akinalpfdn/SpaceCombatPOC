Shader "SpaceCombat/ShieldURP"
{
    Properties
    {
        [Header(Shield Color)]
        [HDR] _ShieldColor ("Shield Color", Color) = (0.4, 0.8, 1.0, 1.0)

        [Header(Fresnel Edge Glow)]
        _FresnelPower ("Fresnel Power", Range(1, 8)) = 4.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.5

        [Header(Hexagon Pattern)]
        _HexagonScale ("Hexagon Scale", Range(1, 30)) = 10.0
        _HexagonVisibility ("Hexagon Visibility", Range(0, 1)) = 0.0
        _HexagonLineWidth ("Hexagon Line Width", Range(0.01, 0.2)) = 0.05

        [Header(Ripple Settings)]
        _RippleSpeed ("Ripple Speed", Range(1, 10)) = 3.0
        _RippleWidth ("Ripple Width", Range(0.05, 0.5)) = 0.15
        _RippleMaxRadius ("Ripple Max Radius", Range(0.5, 5)) = 2.0

        [Header(Idle Animation)]
        _IdlePulse ("Idle Pulse", Range(0, 1)) = 0.0

        [Header(Hit Points)]
        _HitPoint0 ("Hit Point 0", Vector) = (0, 0, 0, -1)
        _HitPoint1 ("Hit Point 1", Vector) = (0, 0, 0, -1)
        _HitPoint2 ("Hit Point 2", Vector) = (0, 0, 0, -1)
        _HitPoint3 ("Hit Point 3", Vector) = (0, 0, 0, -1)
        _HitPoint4 ("Hit Point 4", Vector) = (0, 0, 0, -1)
        _HitPoint5 ("Hit Point 5", Vector) = (0, 0, 0, -1)
        _HitPoint6 ("Hit Point 6", Vector) = (0, 0, 0, -1)
        _HitPoint7 ("Hit Point 7", Vector) = (0, 0, 0, -1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ShieldPass"

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShieldColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float _HexagonScale;
                float _HexagonVisibility;
                float _HexagonLineWidth;
                float _RippleSpeed;
                float _RippleWidth;
                float _RippleMaxRadius;
                float _IdlePulse;
                float4 _HitPoint0;
                float4 _HitPoint1;
                float4 _HitPoint2;
                float4 _HitPoint3;
                float4 _HitPoint4;
                float4 _HitPoint5;
                float4 _HitPoint6;
                float4 _HitPoint7;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.uv = input.uv;

                return output;
            }

            float HexagonPattern(float2 uv, float scale, float lineWidth)
            {
                float2 p = uv * scale;
                float2 h = float2(1.0, sqrt(3.0));
                float2 a = fmod(p, h) - h * 0.5;
                float2 b = fmod(p - h * 0.5, h) - h * 0.5;
                float2 gv = length(a) < length(b) ? a : b;
                float d = abs(max(dot(gv, normalize(float2(1.0, 1.73))), gv.x));
                float hexLine = smoothstep(lineWidth, lineWidth * 0.5, abs(d - 0.5));
                return hexLine;
            }

            // Returns: x = ripple ring intensity, y = glow area (for localized hexagon)
            // Uses angular distance on sphere surface for truly localized effect
            float2 CalculateRippleAndGlow(float3 localPos, float4 hitPoint, float rippleWidth, float maxRadius)
            {
                if (hitPoint.w < 0.0) return float2(0.0, 0.0);

                float3 hitPos = hitPoint.xyz;
                float normalizedTime = hitPoint.w;

                // Use angular distance on sphere surface instead of 3D distance
                // This keeps the effect localized to the hit area
                float3 hitDir = normalize(hitPos);
                float3 fragDir = normalize(localPos);

                // Dot product gives cosine of angle between directions
                // Convert to angular distance (0 = same point, PI = opposite side)
                float cosAngle = dot(hitDir, fragDir);
                float angularDist = acos(saturate(cosAngle)); // 0 to PI radians

                // Scale angular distance to approximate surface distance
                // Smaller multiplier = tighter/more localized effect
                float surfaceDist = angularDist * 0.25; // Halved for tighter ripple

                float currentRadius = normalizedTime * maxRadius * 0.25; // Halved for tighter spread

                // Ripple ring effect on surface
                float innerEdge = smoothstep(currentRadius - rippleWidth, currentRadius, surfaceDist);
                float outerEdge = smoothstep(currentRadius + rippleWidth, currentRadius, surfaceDist);
                float ring = innerEdge * outerEdge;

                float fadeOut = 1.0 - normalizedTime;
                float ripple = ring * fadeOut;

                // Glow area - localized around hit point
                // Only visible on the hemisphere facing the hit
                float glowArea = smoothstep(currentRadius + rippleWidth * 2.0, 0.0, surfaceDist) * fadeOut;

                // Very tight angular falloff - only show effect within ~30 degree cone from hit point
                // cosAngle = 1.0 means same direction as hit, 0.87 = ~30 degrees, 0.7 = ~45 degrees
                float angularFalloff = smoothstep(0.7, 0.98, cosAngle);
                ripple *= angularFalloff;
                glowArea *= angularFalloff;

                return float2(ripple, glowArea);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;

                float2 hexUV = input.positionOS.xy + input.positionOS.yz * 0.5;
                float hexPattern = HexagonPattern(hexUV, _HexagonScale, _HexagonLineWidth);

                // Calculate ripples and localized glow areas for each hit point
                float rippleSum = 0.0;
                float glowSum = 0.0;
                float2 result;

                result = CalculateRippleAndGlow(input.positionOS, _HitPoint0, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint1, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint2, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint3, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint4, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint5, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint6, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;
                result = CalculateRippleAndGlow(input.positionOS, _HitPoint7, _RippleWidth, _RippleMaxRadius);
                rippleSum += result.x; glowSum += result.y;

                rippleSum = saturate(rippleSum);
                glowSum = saturate(glowSum);

                // Hexagon only visible in the localized glow area around hits
                // Also factor in the global _HexagonVisibility for fade-out control
                float localizedHexagon = hexPattern * glowSum * _HexagonVisibility;

                float baseVisibility = fresnel;
                float hitVisibility = localizedHexagon + rippleSum;
                float totalIntensity = saturate(baseVisibility + hitVisibility * 2.0);

                half4 finalColor = half4(_ShieldColor.rgb * totalIntensity, totalIntensity * _ShieldColor.a);
                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
