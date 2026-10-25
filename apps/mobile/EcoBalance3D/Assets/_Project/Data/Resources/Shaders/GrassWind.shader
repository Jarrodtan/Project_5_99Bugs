// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
// Grass blades that sway in the wind: tips move most, roots stay put, and neighbouring
// clumps sway slightly out of step so the meadow ripples. Dark at the root, light at the tip.
// Lives in Resources so it is included in builds (loaded with Shader.Find).
Shader "EcoBalance/GrassWind"
{
    Properties
    {
        _BaseColor ("Root colour", Color) = (0.16, 0.38, 0.12, 1)
        _TipColor ("Tip colour", Color) = (0.58, 0.80, 0.32, 1)
        _BladeHeight ("Blade height (object units)", Float) = 1.4
        _WindStrength ("Wind strength", Float) = 0.35
        _WindSpeed ("Wind speed", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
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
                half4 _BaseColor;
                half4 _TipColor;
                float _BladeHeight;
                float _WindStrength;
                float _WindSpeed;
            CBUFFER_END
            float _EcoDryness;   // global, set by SeasonWeather: 0 lush green .. 1 dry brown

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float height : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float h = saturate(i.positionOS.y / max(_BladeHeight, 0.001));
                float3 w = TransformObjectToWorld(i.positionOS.xyz);

                // Two waves: a slow gust rolling across the meadow and a quicker flutter.
                float t = _Time.y * _WindSpeed;
                float gust = sin(t + w.x * 0.12 + w.z * 0.08);
                float flutter = 0.35 * sin(t * 2.7 + w.x * 0.9 + w.z * 0.7);
                float sway = (gust + flutter) * _WindStrength * h * h;   // bend grows towards the tip
                w.xz += float2(sway, sway * 0.4);

                o.positionCS = TransformWorldToHClip(w);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.height = h;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                Light sun = GetMainLight();
                half lit = saturate(dot(normalize(i.normalWS), sun.direction)) * 0.55 + 0.45;
                half3 green = lerp(_BaseColor.rgb, _TipColor.rgb, i.height);
                half3 dry = lerp(half3(0.36, 0.28, 0.14), half3(0.78, 0.66, 0.38), i.height);
                half3 albedo = lerp(green, dry, saturate(_EcoDryness));
                half3 col = albedo * sun.color * lit + SampleSH(half3(0, 1, 0)) * 0.25 * albedo;
                return half4(MixFog(col, i.fog), 1);
            }
            ENDHLSL
        }
    }
}
