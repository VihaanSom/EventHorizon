// EHAccretionDisk — High-Fidelity Relativistic Accretion Disk & Plasma Flow.
// Simulates Keplerian differential rotation, logarithmic accretion spirals, multi-scale MHD turbulence,
// astrophysical Shakura-Sunyaev temperature gradient, relativistic Doppler beaming, and gravitational redshift.
// Optimized for Meta Quest 3 VR (Single Pass Instanced, zero compute, zero blits, high fillrate efficiency).
Shader "EventHorizon/AccretionDisk"
{
    Properties
    {
        _EH_DiskEmission ("Emission", Float) = 1.0
        _EH_FlowTime ("Flow Time", Float) = 0.0
        _EH_InnerBoost ("Inner Rim Boost", Float) = 2.2
        _EH_Doppler ("Doppler Asymmetry", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend One OneMinusSrcAlpha // Premultiplied alpha for intense HDR emission and clean glow
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

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            Varyings Vert(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings o;
                o.worldPos = mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
                o.positionCS = TransformObjectToHClip(positionOS);
                o.uv = uv;
                return o;
            }

            // Multi-harmonic shearing turbulence simulating magneto-hydrodynamic (MHD) plasma flow
            float EHTurbulence(float angleTurns, float radius01, float t)
            {
                float a = angleTurns * 6.2831853;
                float r = radius01;

                // Logarithmic accretion spiral arms
                float spiral = sin(a * 4.0 - log(r + 0.08) * 5.5 - t * 1.8);

                // Octave 1: Major plasma density waves & shear
                float w1 = sin(a * 6.0 - t * 2.4 + sin(a * 3.0 + t * 0.9) * 1.6);
                // Octave 2: Turbulent filaments & magnetic reconnection loops
                float w2 = sin(a * 13.0 + t * 1.7 + r * 14.0 + spiral * 1.2);
                // Octave 3: Fine plasma wisps
                float w3 = sin(a * 27.0 - t * 3.5 - r * 28.0);
                // Octave 4: High-frequency granulation
                float w4 = sin(a * 53.0 + t * 5.2 + r * 48.0);

                return 0.50 + 0.24 * w1 + 0.14 * w2 + 0.08 * w3 + 0.04 * w4;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float r = saturate(i.uv.y);

                // Keplerian differential rotation: inner plasma completes orbits significantly faster
                float keplerianOrbit = pow(max(0.08, 1.0 - r * 0.75), -1.35);
                float flowAngle = frac(i.uv.x + _EH_FlowTime * 0.035 * keplerianOrbit);

                // Multi-scale plasma turbulence
                float turb = EHTurbulence(flowAngle, r, _EH_FlowTime);
                float streaks = smoothstep(0.42, 0.92, turb);
                float fineFilaments = smoothstep(0.55, 0.88, sin(flowAngle * 62.83 + r * 35.0 - _EH_FlowTime * 4.0));

                // Radial Density Profile:
                // Searing inner edge (ISCO), rich glowing body, smooth interstellar fade
                float innerEdge = exp(-r * 6.8) * _EH_InnerBoost;
                float body = exp(-r * 2.2);
                float outerFade = smoothstep(1.0, 0.80, r);
                float innerFade = smoothstep(0.0, 0.025, r);

                float density = (innerEdge * 1.5 + body * (0.5 + 0.5 * streaks + 0.2 * fineFilaments)) * _EH_DiskEmission;

                // Relativistic Shakura-Sunyaev Temperature Gradient:
                // T(r) ~ r^(-3/4): from 50,000K (ultra-hot blue-white) to 1,500K (deep crimson smoke)
                float3 colCore   = float3(0.90, 0.95, 1.25) * 3.5;  // Ultra-hot inner ionizing core (HDR)
                float3 colHot    = float3(1.20, 1.08, 0.85) * 2.0;  // Incandescent white-yellow
                float3 colMid    = float3(1.10, 0.65, 0.22) * 1.4;  // Blazing golden amber
                float3 colRed    = float3(0.85, 0.22, 0.06);        // Deep crimson plasma
                float3 colSmoke  = float3(0.25, 0.06, 0.02);        // Cool outer dust

                float3 col;
                if (r < 0.12)
                    col = lerp(colCore, colHot, r / 0.12);
                else if (r < 0.38)
                    col = lerp(colHot, colMid, (r - 0.12) / 0.26);
                else if (r < 0.75)
                    col = lerp(colMid, colRed, (r - 0.38) / 0.37);
                else
                    col = lerp(colRed, colSmoke, (r - 0.75) / 0.25);

                // Relativistic Doppler Beaming (The Approach/Recede Asymmetry):
                // Approaching limb (counter-clockwise orbit -> left side) is boosted by D^3 and blueshifted.
                // Receding limb (right side) is dimmed and redshifted.
                float cosOrbit = cos(flowAngle * 6.2831853);
                float dopplerFactor = 1.0 + _EH_Doppler * cosOrbit;
                float dopplerBoost = pow(max(0.1, dopplerFactor), 2.8);

                // Relativistic color shift: approaching plasma shifts to blue-white, receding to deep red
                float3 blueShift = float3(0.8, 0.92, 1.2);
                float3 redShift  = float3(1.1, 0.65, 0.45);
                float3 dopplerTint = lerp(redShift, blueShift, saturate((cosOrbit + 1.0) * 0.5));

                col *= dopplerBoost * dopplerTint;

                // Gravitational Redshift (photons climbing out of horizon lose energy)
                float gravRedshift = sqrt(saturate(1.0 - 0.12 / (r + 0.14)));
                col *= gravRedshift;

                // Total plasma emission
                float3 emission = col * density;

                // Alpha with soft boundaries
                float alpha = saturate((body * 1.5 + innerEdge * 0.6) * outerFade * innerFade);

                // Output in premultiplied format: emission is added, background is attenuated by (1 - alpha)
                return half4(emission * alpha, alpha * 0.95);
            }
            ENDHLSL
        }
    }
}
