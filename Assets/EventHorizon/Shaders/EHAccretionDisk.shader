// EHAccretionDisk — High-Fidelity Relativistic Accretion Disk & Plasma Flow.
// Simulates Keplerian differential rotation, MHD plasma turbulence, Shakura-Sunyaev thermal gradient,
// relativistic Doppler beaming, and gravitational redshift.
// Optimized for Meta Quest 3 VR (Single Pass Instanced, HDR emissive, URP Bloom compatible).
Shader "EventHorizon/AccretionDisk"
{
    Properties
    {
        _EH_DiskEmission ("Emission Gain", Float) = 1.2
        _EH_FlowTime ("Flow Time", Float) = 0.0
        _EH_InnerBoost ("Inner ISCO Boost", Float) = 2.8
        _EH_Doppler ("Doppler Asymmetry", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend One OneMinusSrcAlpha // Premultiplied alpha for intense HDR plasma glow and clean transparency
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _EH_DiskEmission;
            float _EH_FlowTime;
            float _EH_InnerBoost;
            float _EH_Doppler;

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
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                return o;
            }

            // High-performance shearing turbulence simulating magneto-hydrodynamic (MHD) plasma flow
            float EHMHDTurbulence(float angleTurns, float radius01, float t)
            {
                float a = angleTurns * 6.2831853;
                float r = radius01;

                // Logarithmic accretion spiral arms
                float spiral = sin(a * 3.0 - log(r + 0.06) * 4.8 - t * 1.5);

                // Multi-scale plasma density waves
                float w1 = sin(a * 5.0 - t * 2.1 + sin(a * 2.5 + t * 0.8) * 1.4);
                float w2 = sin(a * 11.0 + t * 1.6 + r * 12.0 + spiral * 1.1);
                float w3 = sin(a * 23.0 - t * 3.2 - r * 24.0);
                float w4 = sin(a * 47.0 + t * 4.8 + r * 42.0);

                return 0.50 + 0.25 * w1 + 0.14 * w2 + 0.07 * w3 + 0.04 * w4;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r = saturate(i.uv.y);

                // Keplerian differential rotation: inner plasma completes orbits significantly faster
                float keplerianOrbit = pow(max(0.06, 1.0 - r * 0.72), -1.35);
                float flowAngle = frac(i.uv.x + _EH_FlowTime * 0.028 * keplerianOrbit);

                // Multi-scale plasma turbulence and fine filamentary streaks
                float turb = EHMHDTurbulence(flowAngle, r, _EH_FlowTime);
                float streaks = smoothstep(0.38, 0.90, turb);
                float fineFilaments = smoothstep(0.52, 0.86, sin(flowAngle * 56.54 + r * 32.0 - _EH_FlowTime * 3.6));

                // Radial Density Profile:
                // Searing inner ISCO edge, rich luminous body, silky interstellar outer fade
                float innerEdge = exp(-r * 7.5) * _EH_InnerBoost;
                float body = exp(-r * 2.4);
                float outerFade = smoothstep(1.0, 0.75, r);
                float innerFade = smoothstep(0.0, 0.035, r);

                float density = (innerEdge * 1.6 + body * (0.45 + 0.45 * streaks + 0.2 * fineFilaments)) * _EH_DiskEmission;

                // Relativistic Shakura-Sunyaev Temperature Gradient:
                // From 10,000,000K (incandescent blue-white core) to 2,500K (deep crimson-gold dust)
                float3 colCore   = float3(1.10, 1.25, 1.85) * 4.2;  // Ultra-hot inner ionizing core (HDR bloom trigger)
                float3 colHot    = float3(1.35, 1.15, 0.80) * 2.4;  // Incandescent white-gold
                float3 colMid    = float3(1.25, 0.65, 0.18) * 1.6;  // Radiant amber plasma
                float3 colRed    = float3(0.95, 0.22, 0.05) * 1.1;  // Deep crimson plasma
                float3 colSmoke  = float3(0.20, 0.04, 0.01);        // Cool outer dust

                float3 col;
                if (r < 0.10)
                    col = lerp(colCore, colHot, r / 0.10);
                else if (r < 0.35)
                    col = lerp(colHot, colMid, (r - 0.10) / 0.25);
                else if (r < 0.72)
                    col = lerp(colMid, colRed, (r - 0.35) / 0.37);
                else
                    col = lerp(colRed, colSmoke, (r - 0.72) / 0.28);

                // Relativistic Doppler Beaming:
                // Approaching limb (counter-clockwise orbit -> left side) is boosted and blueshifted.
                // Receding limb (right side) is dimmed and redshifted.
                float cosOrbit = cos(flowAngle * 6.2831853);
                float dopplerFactor = 1.0 + _EH_Doppler * cosOrbit;
                float dopplerBoost = pow(max(0.12, dopplerFactor), 2.5);

                float3 blueShift = float3(0.82, 0.94, 1.25);
                float3 redShift  = float3(1.12, 0.60, 0.40);
                float3 dopplerTint = lerp(redShift, blueShift, saturate((cosOrbit + 1.0) * 0.5));
                col *= dopplerBoost * dopplerTint;

                // Gravitational Redshift (photons climbing out of horizon well lose energy)
                float gravRedshift = sqrt(saturate(1.0 - 0.10 / (r + 0.12)));
                col *= gravRedshift;

                // Total plasma emission
                float3 emission = col * density;

                // Silky alpha with soft boundary fade
                float alpha = saturate((body * 1.6 + innerEdge * 0.7) * outerFade * innerFade);

                // Output in premultiplied format
                return half4(emission * alpha, alpha * 0.92);
            }
            ENDHLSL
        }
    }
}
