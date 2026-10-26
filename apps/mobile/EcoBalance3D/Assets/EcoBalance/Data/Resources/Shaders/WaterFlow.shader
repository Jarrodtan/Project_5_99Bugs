// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
// Flowing water for streams and lakes. UV.x runs along the stream (in metres / width), UV.y across it,
// so ripples and foam streaks travel downstream. Lakes use a low flow speed for a gentle shimmer.
// Lives in Resources so it is included in builds (loaded with Shader.Find).
Shader "EcoBalance/WaterFlow"
{
    Properties
    {
        _Shallow ("Edge colour", Color) = (0.55, 0.82, 0.92, 0.75)
        _Deep ("Middle colour", Color) = (0.12, 0.40, 0.62, 0.92)
        _FlowSpeed ("Flow speed", Float) = 0.8
        _RippleScale ("Ripple scale", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow;
                half4 _Deep;
                float _FlowSpeed;
                float _RippleScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;
                float along = i.uv.x * _RippleScale - t;
                float across = i.uv.y;

                // Wobbly ripples moving downstream, plus thin bright foam streaks.
                float r1 = sin((along + sin(across * 6.2831 + i.uv.x * 0.7) * 0.25) * 6.2831) * 0.5 + 0.5;
                float r2 = sin((along * 2.3 + across * 1.7) * 6.2831 + 1.3) * 0.5 + 0.5;
                float ripple = r1 * 0.6 + r2 * 0.4;
                float foam = pow(saturate(ripple), 10);

                float middle = 1 - abs(across * 2 - 1);            // 0 at the banks, 1 mid-stream
                half4 col = lerp(_Shallow, _Deep, middle);
                Light sun = GetMainLight();
                col.rgb = col.rgb * (0.65 + 0.35 * sun.color) + ripple * 0.08 + foam * 0.35;
                col.a = lerp(_Shallow.a, _Deep.a, middle);
                col.rgb = MixFog(col.rgb, i.fog);
                return col;
            }
            ENDHLSL
        }
    }
}
