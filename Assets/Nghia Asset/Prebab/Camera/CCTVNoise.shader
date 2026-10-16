
Shader "Custom/CCTVNoiseURP"
{
    Properties
    {
        _MainTex ("Camera Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.08
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.18
        _ScanlineSpeed ("Scanline Speed", Range(0,5)) = 1.5
        _ScanlineDensity ("Scanline Density", Range(100,1500)) = 700
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _NoiseStrength;
                float _ScanlineStrength;
                float _ScanlineSpeed;
                float _ScanlineDensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float hashNoise(float2 p)
            {
                return frac(
                    sin(dot(p, float2(12.9898, 78.233)))
                    * 43758.5453
                );
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                half4 col = SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex, uv
                );

                float scanWave = sin(
                    (uv.y + _Time.y * _ScanlineSpeed)
                    * _ScanlineDensity
                );

                float scanline = 1.0
                    - _ScanlineStrength
                    * (0.5 + 0.5 * scanWave);

                float noise = hashNoise(
                    float2(
                        floor(uv.x * 500.0),
                        floor(uv.y * 300.0)
                        + floor(_Time.y * 30.0)
                    )
                );

                col.rgb = saturate(
                    col.rgb * scanline
                    + (noise - 0.5) * _NoiseStrength
                );

                return col;
            }
            ENDHLSL
        }
    }
}