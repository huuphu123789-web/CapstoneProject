Shader "Custom/URP_FoliageWind"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color Tint", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Wind Settings)]
        _WindSpeed("Wind Speed (Toc Do Gio)", Range(0.1, 8.0)) = 1.8
        _WindStrength("Wind Strength (Do Uon Luon)", Range(0.0, 1.5)) = 0.25
        _WindFrequency("Wind Frequency (Tan So Song)", Range(0.01, 1.0)) = 0.18
        _WindDirection("Wind Direction (X, Z)", Vector) = (1, 0.5, 0, 0)
        _LeafFlutterStrength("Leaf Flutter (Rung Rinh La)", Range(0.0, 0.3)) = 0.05
        _LeafFlutterSpeed("Leaf Flutter Speed", Range(1.0, 15.0)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

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
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : NORMAL;
                float fogFactor     : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindSpeed;
                half _WindStrength;
                half _WindFrequency;
                float4 _WindDirection;
                half _LeafFlutterStrength;
                half _LeafFlutterSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Tính toán sóng gió lan truyền theo tọa độ thế giới (World Space)
                float time = _Time.y * _WindSpeed;
                float spatialPhase = (worldPos.x + worldPos.z) * _WindFrequency;

                // 1. Sóng đung đưa cành chính
                float mainWave = sin(time + spatialPhase);
                float secondaryWave = sin(time * 1.7 + spatialPhase * 0.8) * 0.35;
                float totalSway = (mainWave + secondaryWave) * _WindStrength;

                // Hướng gió
                float2 windDir2D = normalize(_WindDirection.xy);
                float3 windDir = float3(windDir2D.x, 0.1, windDir2D.y);

                // 2. Độ rung rinh chi tiết của từng kẽ lá (Flutter)
                float flutterTime = _Time.y * _LeafFlutterSpeed;
                float leafPhase = input.positionOS.x * 12.0 + input.positionOS.y * 8.0 + input.positionOS.z * 15.0;
                float flutter = sin(flutterTime + leafPhase) * _LeafFlutterStrength;

                // Áp dụng dịch chuyển uốn lượn
                worldPos += windDir * totalSway + float3(flutter, flutter * 0.5, flutter);

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedo.a - _Cutoff);

                float3 normalWS = normalize(input.normalWS);

                // Ánh sáng Directional chính
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                // Half-Lambert giúp mặt sau của lá cây vẫn nhận ánh sáng xuyên qua tự nhiên
                half halfLambert = NdotL * 0.6 + 0.4;
                half3 lightColor = mainLight.color * (halfLambert * mainLight.shadowAttenuation);

                // Ánh sáng môi trường (Ambient)
                half3 ambient = SampleSH(normalWS);

                half3 finalColor = albedo.rgb * (lightColor + ambient);
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindSpeed;
                half _WindStrength;
                half _WindFrequency;
                float4 _WindDirection;
                half _LeafFlutterStrength;
                half _LeafFlutterSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);

                float time = _Time.y * _WindSpeed;
                float spatialPhase = (worldPos.x + worldPos.z) * _WindFrequency;
                float mainWave = sin(time + spatialPhase);
                float secondaryWave = sin(time * 1.7 + spatialPhase * 0.8) * 0.35;
                float totalSway = (mainWave + secondaryWave) * _WindStrength;

                float2 windDir2D = normalize(_WindDirection.xy);
                float3 windDir = float3(windDir2D.x, 0.1, windDir2D.y);

                float flutterTime = _Time.y * _LeafFlutterSpeed;
                float leafPhase = input.positionOS.x * 12.0 + input.positionOS.y * 8.0 + input.positionOS.z * 15.0;
                float flutter = sin(flutterTime + leafPhase) * _LeafFlutterStrength;

                worldPos += windDir * totalSway + float3(flutter, flutter * 0.5, flutter);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, worldNormal, _MainLightPosition.xyz));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedo.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindSpeed;
                half _WindStrength;
                half _WindFrequency;
                float4 _WindDirection;
                half _LeafFlutterStrength;
                half _LeafFlutterSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float time = _Time.y * _WindSpeed;
                float spatialPhase = (worldPos.x + worldPos.z) * _WindFrequency;
                float mainWave = sin(time + spatialPhase);
                float secondaryWave = sin(time * 1.7 + spatialPhase * 0.8) * 0.35;
                float totalSway = (mainWave + secondaryWave) * _WindStrength;

                float2 windDir2D = normalize(_WindDirection.xy);
                float3 windDir = float3(windDir2D.x, 0.1, windDir2D.y);

                float flutterTime = _Time.y * _LeafFlutterSpeed;
                float leafPhase = input.positionOS.x * 12.0 + input.positionOS.y * 8.0 + input.positionOS.z * 15.0;
                float flutter = sin(flutterTime + leafPhase) * _LeafFlutterStrength;

                worldPos += windDir * totalSway + float3(flutter, flutter * 0.5, flutter);

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedo.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
