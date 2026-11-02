// EHAccretionDisk — Authentic Interstellar (Gargantua) Relativistic Accretion Rings.
// Features fine concentric ring striations, Keplerian shear flow, Shakura-Sunyaev thermal color,
// and relativistic Doppler asymmetry, calibrated to prevent blowout and fireballing in VR.
Shader "EventHorizon/AccretionDisk"
{
    Properties
    {
        _EH_DiskEmission ("Emission Gain", Float) = 1.0
        _EH_FlowTime ("Flow Time", Float) = 0.0
        _EH_InnerBoost ("Inner Rim Boost", Float) = 1.0
        _EH_Doppler ("Doppler Asymmetry", Range(0, 1)) = 0.40
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend One OneMinusSrcAlpha // Premultiplied alpha for rich HDR plasma
        ZWrite Off
        ZTest LEqual
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

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r = saturate(i.uv.y);

                // Keplerian differential rotation: inner plasma orbits faster
                float keplerianOrbit = pow(max(0.08, 1.0 - r * 0.70), -1.35);
                float flowAngle = frac(i.uv.x + _EH_FlowTime * 0.020 * keplerianOrbit);
                float a = flowAngle * 6.2831853;

                // --- Authentic Interstellar Concentric Ring Striations
                // Fine Saturn-like plasma density waves across the disk
                float ringF1 = sin(r * 55.0);
                float ringF2 = sin(r * 135.0);
                float ringF3 = sin(r * 290.0 + flowAngle * 10.0);
                float striations = 0.72 + 0.16 * ringF1 + 0.08 * ringF2 + 0.04 * ringF3;

                // Delicate azimuthal plasma wisps and spiral density waves
                float spiral = sin(a * 3.0 - log(r + 0.08) * 4.5 - _EH_FlowTime * 1.2);
                float wisps = 0.88 + 0.12 * sin(a * 7.0 + spiral * 1.2 - _EH_FlowTime * 1.8);

                // Radial Density Falloff:
                // Searing inner ISCO edge, rich luminous body, smooth outer fade
                float innerEdge = exp(-r * 12.0) * 0.70 * _EH_InnerBoost;
                float body = exp(-r * 2.2) * striations * wisps;
                float outerFade = smoothstep(1.0, 0.75, r);
                float innerFade = smoothstep(0.0, 0.035, r);

                float density = (innerEdge + body * 0.85) * outerFade * innerFade * _EH_DiskEmission;

                // --- Authentic Interstellar Color Gradient (Calibrated to prevent blowout):
                // Inner ISCO: Incandescent white-gold core (Peak HDR ~1.35, gentle bloom)
                // Hot body: Blazing radiant amber & golden copper
                // Mid body: Deep glowing bronze & burnt orange
                // Outer edge: Deep smoky crimson & charred sienna
                float3 colCore   = float3(1.15, 1.05, 0.90) * 1.35;
                float3 colHot    = float3(1.20, 0.85, 0.38) * 1.10;
                float3 colMid    = float3(1.05, 0.52, 0.14) * 0.90;
                float3 colRed    = float3(0.75, 0.22, 0.05) * 0.70;
                float3 colSmoke  = float3(0.12, 0.03, 0.01) * 0.45;

                float3 col;
                if (r < 0.10)
                    col = lerp(colCore, colHot, r / 0.10);
                else if (r < 0.35)
                    col = lerp(colHot, colMid, (r - 0.10) / 0.25);
                else if (r < 0.70)
                    col = lerp(colMid, colRed, (r - 0.35) / 0.35);
                else
                    col = lerp(colRed, colSmoke, (r - 0.70) / 0.30);

                // --- Relativistic Doppler Beaming
                // Approaching side boosted gently, receding side dimmed
                float cosOrbit = cos(a);
                float dopplerFactor = 1.0 + _EH_Doppler * 0.35 * cosOrbit;
                float dopplerBoost = clamp(pow(max(0.2, dopplerFactor), 1.8), 0.65, 1.35);

                float3 blueShift = float3(0.96, 1.02, 1.10);
                float3 redShift  = float3(1.08, 0.85, 0.65);
                float3 dopplerTint = lerp(redShift, blueShift, saturate((cosOrbit + 1.0) * 0.5));
                col *= dopplerBoost * dopplerTint;

                // Gravitational Redshift near horizon
                float gravRedshift = sqrt(saturate(1.0 - 0.06 / (r + 0.08)));
                col *= gravRedshift;

                // Total plasma emission
                float3 emission = col * density;

                // Safe HDR clamp to physically prevent ANY blinding whiteout/fireball
                emission = min(emission, float3(1.6, 1.4, 1.2));

                // Alpha with soft boundary
                float alpha = saturate(density * 1.0);

                // Premultiplied alpha output
                return half4(emission, alpha * 0.90);
            }
            ENDHLSL
        }
    }
}
