// EHDust — High-Fidelity Cosmic Dust Motes & Relativistic Plunge Needles.
// Optimized for Meta Quest 3 VR: Crisp micro-glints with distance-fade and zero camera clipping.
Shader "EventHorizon/Dust"
{
    Properties
    {
        _EH_DustAlpha ("Alpha", Range(0, 1)) = 0.5
        _EH_DustTint ("Tint", Color) = (0.75, 0.88, 1.0, 1)
        _EH_StreakStretch ("Streak Stretch", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" }
        Blend One OneMinusSrcAlpha // Premultiplied alpha for intense, clean HDR sparks
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
                float camDist : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = mul(unity_ObjectToWorld, float4(v.positionOS, 1.0)).xyz;
                o.camDist = distance(worldPos, GetCameraPositionWS());
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Near-plane fade: smoothly fade out particles closer than 3.5m so they never blob in VR
                float nearFade = smoothstep(2.0, 4.5, i.camDist);
                if (nearFade < 0.005) discard;

                if (_EH_StreakStretch > 1.2)
                {
                    // Relativistic ionization needle: razor-thin, aerodynamic optical streamline
                    float2 centered = i.uv - float2(0.5, 0.5);
                    float xDist = abs(centered.x) * 2.0;
                    float yPos = saturate(i.uv.y);

                    // Exponential needle profile: ultra-sharp cross-section
                    float widthFalloff = exp(-xDist * xDist * 32.0);
                    // Glowing tail wake
                    float trail = pow(yPos, 2.5);
                    // Head spark
                    float headSpark = exp(-distance(i.uv, float2(0.5, 0.96)) * 28.0) * 3.0;

                    // Cherenkov blueshift at relativistic velocities
                    float speedFrac = saturate((_EH_StreakStretch - 1.2) / 4.0);
                    float3 streakCol = lerp(_EH_DustTint, float3(0.65, 0.90, 1.45), speedFrac);

                    float alpha = saturate((trail * widthFalloff + headSpark) * _EH_DustAlpha * nearFade);
                    float3 emission = streakCol * (1.5 + headSpark * 2.5) * alpha;

                    return half4(emission, alpha);
                }
                else
                {
                    // Crystalline cosmic dust mote: pinpoint Airy core + subtle twinkle
                    float d = length(i.uv - 0.5) * 2.0;
                    float core = exp(-d * d * 32.0);
                    float glow = exp(-d * 4.5) * 0.25;
                    float alpha = saturate((core + glow) * _EH_DustAlpha * nearFade);

                    float3 emission = _EH_DustTint * (core * 2.2 + glow) * alpha;
                    return half4(emission, alpha);
                }
            }
            ENDHLSL
        }
    }
}
