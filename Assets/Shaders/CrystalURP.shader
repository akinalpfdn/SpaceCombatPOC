Shader "SpaceCombat/CrystalURP"
{
    Properties
    {
        [Header(Crystal Colors)]
        _ColorTint1("Color Tint 1", Color) = (0.5, 0, 0.82, 1)
        _ColorTint2("Color Tint 2", Color) = (0.61, 0.43, 1, 1)
        _FresnelColor("Fresnel Edge Color", Color) = (0.8, 0.5, 1, 1)

        [Header(Surface)]
        _BaseMap("Albedo Mask", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1
        _Metallic("Metallic", Range(0, 1)) = 0.3
        _Smoothness("Smoothness", Range(0, 1)) = 0.95

        [Header(Crystal Effect)]
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _FresnelIntensity("Fresnel Intensity", Range(0, 5)) = 1.5

        [Header(Emission)]
        _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1.5, 0.6, 3, 1)
        _EmissionPower("Emission Power", Range(0, 10)) = 2

        [Header(Inner Glow)]
        _TranslucencyPower("Translucency Power", Range(1, 20)) = 8
        _TranslucencyScale("Translucency Scale", Range(0, 5)) = 1.5
        _TranslucencyColor("Translucency Color", Color) = (0.6, 0.3, 1, 1)
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
            Name "CrystalForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _ColorTint1;
                half4 _ColorTint2;
                half4 _FresnelColor;
                half4 _EmissionColor;
                half4 _TranslucencyColor;
                half _BumpScale;
                half _Metallic;
                half _Smoothness;
                half _FresnelPower;
                half _FresnelIntensity;
                half _EmissionPower;
                half _TranslucencyPower;
                half _TranslucencyScale;
            CBUFFER_END

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
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = normInputs.normalWS;
                o.tangentWS = normInputs.tangentWS;
                o.bitangentWS = normInputs.bitangentWS;
                o.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // View direction
                half3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                // Sample textures
                half4 albedoMask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv);

                // TBN matrix
                half3x3 TBN = half3x3(
                    normalize(i.tangentWS),
                    normalize(i.bitangentWS),
                    normalize(i.normalWS));
                half3 normalWS = normalize(mul(normalTS, TBN));

                // Crystal color blend
                half3 crystalColor = lerp(_ColorTint1.rgb, _ColorTint2.rgb, albedoMask.r);

                // Fresnel - glowing edges like real crystal
                half NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;
                half3 fresnelContrib = _FresnelColor.rgb * fresnel;

                // Main light
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfDir));

                // Diffuse + Specular (strong specular for crystal reflections)
                half3 diffuse = crystalColor * (NdotL * 0.6 + 0.4) * mainLight.color;
                half specPow = exp2(10.0 * _Smoothness + 1.0);
                half3 specular = mainLight.color * pow(NdotH, specPow) * _Metallic * 2.0;

                // Translucency (back-lit inner glow)
                half3 transDir = mainLight.direction + normalWS * 0.3;
                half transDot = pow(saturate(dot(viewDirWS, -transDir)), _TranslucencyPower);
                half3 translucency = _TranslucencyColor.rgb * transDot * _TranslucencyScale * mainLight.color;

                // Ambient
                half3 ambient = SampleSH(normalWS) * crystalColor * 0.6;

                // Emission
                half3 emission = emissionTex.rgb * _EmissionColor.rgb * _EmissionPower;

                // Combine - opaque crystal: solid body + glowing edges + inner glow
                half3 finalColor = ambient + diffuse + specular + fresnelContrib + translucency + emission;

                finalColor = MixFog(finalColor, i.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthOnly pass for proper depth buffer
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
