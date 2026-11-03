Shader "EcoSim/Ocean"
{
    Properties
    {
        [MainColor] _BaseColor("Shallow Color", Color) = (0.12, 0.42, 0.58, 0.88)
        _DeepColor("Deep Color", Color) = (0.02, 0.12, 0.28, 0.95)
        _ScrollSpeed("Scroll Speed (XY)", Vector) = (0.02, 0.015, 0, 0)
        _WaveScale("Wave Scale", Float) = 0.12
        _WaveStrength("Wave Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "OceanForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepColor;
                float4 _ScrollSpeed;
                float _WaveScale;
                float _WaveStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 scrolled = input.uv + _ScrollSpeed.xy * _Time.y;
                float waveA = sin((scrolled.x + scrolled.y) * (6.2831853 / max(_WaveScale, 0.0001)));
                float waveB = sin((scrolled.x - scrolled.y * 1.3) * (6.2831853 / max(_WaveScale * 1.7, 0.0001)));
                float wave = saturate(0.5 + 0.5 * (waveA * 0.65 + waveB * 0.35));
                wave = lerp(0.5, wave, _WaveStrength);

                half4 color = lerp(_DeepColor, _BaseColor, wave);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
