// EHDust — High-Fidelity Instanced Cosmic Dust Motes & Relativistic Plunge Streaks.
// Simulates micro-crystalline dust glimmer and high-velocity relativistic ionization wakes.
// Optimized for Meta Quest 3 VR (GPU instancing, single pass instanced stereo).
Shader "EventHorizon/Dust"
{
    Properties
    {
        _EH_DustAlpha ("Alpha", Range(0, 1)) = 0.45
        _EH_DustTint ("Tint", Color) = (0.7, 0.82, 1.0, 1)
        _EH_StreakStretch ("Streak Stretch", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" }
        Blend One OneMinusSrcAlpha // Premultiplied alpha for brilliant high-energy sparks
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _EH_DustAlpha;
            float3 _EH_DustTint;
            float _EH_StreakStretch;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                if (_EH_StreakStretch > 1.2)
                {
                    // Relativistic high-velocity plunge streak (ionized comet needle)
                    float2 centered = i.uv - float2(0.5, 0.5);
                    float xDist = abs(centered.x) * 2.0;

                    // Y: 0.0 is trail tail, 1.0 is leading ionization head
                    float yPos = saturate(i.uv.y);

                    // Tapering width: razor-sharp head, expanding slightly into wake
                    float widthFalloff = exp(-xDist * xDist * (10.0 + yPos * 18.0));

                    // Trail intensity: brightest at head, decaying smoothly down the wake
                    float trail = pow(yPos, 2.0);

                    // Incandescent head spark
                    float headSpark = exp(-distance(i.uv, float2(0.5, 0.94)) * 18.0) * 2.5;

                    // Relativistic Cherenkov blueshift at extreme speeds
                    float speedFrac = saturate((_EH_StreakStretch - 1.2) / 5.5);
                    float3 streakCol = lerp(_EH_DustTint, float3(0.55, 0.88, 1.35), speedFrac);

                    float alpha = saturate((trail * widthFalloff + headSpark) * _EH_DustAlpha);
                    float3 emission = streakCol * (1.2 + headSpark * 2.0) * alpha;

                    return half4(emission, alpha);
                }
                else
                {
                    // Crystalline ambient dust mote: sharp glint core + soft diffraction glow
                    float d = length(i.uv - 0.5) * 2.0;
                    float core = exp(-d * d * 12.0);
                    float glow = exp(-d * 2.6) * 0.35;
                    float alpha = saturate((core + glow) * _EH_DustAlpha);

                    float3 emission = _EH_DustTint * (core * 1.5 + glow) * alpha;
                    return half4(emission, alpha);
                }
            }
            ENDHLSL
        }
    }
}
