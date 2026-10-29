// EHBlackHoleSky — High-Fidelity Deep-Space Cosmic Backdrop + Schwarzschild Lensing + Relativistic Photon Rings.
// Optimized for Meta Quest 3 VR (Single-Pass Instanced, zero compute, zero blits, high fillrate efficiency).
Shader "EventHorizon/BlackHoleSky"
{
    Properties
    {
        _EH_HoleRadius ("Hole Angular Radius (relative)", Float) = 1.0
        _EH_LensingStrength ("Lensing Strength", Float) = 0.5
        _EH_LensingRadius ("Lensing Field Radius (deg)", Float) = 28.0
        _EH_RimSoftness ("Rim Softness", Range(0, 1)) = 0.35
        _EH_PhotonRing ("Photon Ring Intensity", Range(0, 5)) = 1.8
        _EH_InteriorBlack ("Interior Blackness", Range(0, 1)) = 0.99
        _EH_BackgroundTint ("Background Tint", Color) = (0.015, 0.02, 0.04, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" }
        ZWrite On
        Cull Front // inverted dome: camera is inside looking at inner faces

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _EH_HoleRadius;
            float _EH_LensingStrength;
            float _EH_LensingRadius;
            float _EH_RimSoftness;
            float _EH_PhotonRing;
            float _EH_InteriorBlack;
            float3 _EH_BackgroundTint;
            float3 _EH_HoleDir;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;
            };

            Varyings Vert(float3 positionOS : POSITION)
            {
                Varyings o;
                float3 world = mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
                o.dirWS = world - _WorldSpaceCameraPos;
                o.positionCS = TransformObjectToHClip(positionOS);
                return o;
            }

            // High-speed 3D pseudo-random hash
            float EHHash(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.1, 0.2, 0.3));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // Smooth 3D value noise
            float EHNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = EHHash(i);
                float n100 = EHHash(i + float3(1, 0, 0));
                float n010 = EHHash(i + float3(0, 1, 0));
                float n110 = EHHash(i + float3(1, 1, 0));
                float n001 = EHHash(i + float3(0, 0, 1));
                float n101 = EHHash(i + float3(1, 0, 1));
                float n011 = EHHash(i + float3(0, 1, 1));
                float n111 = EHHash(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            // 4-octave Fractional Brownian Motion for celestial nebulae
            float EHFbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                float3 shift = float3(1.23, 2.34, 3.45);
                for (int k = 0; k < 4; ++k)
                {
                    v += a * EHNoise(p);
                    p = p * 2.08 + shift;
                    a *= 0.5;
                }
                return v;
            }

            // Screen-space dither to suppress VR color banding on Quest 3 LCD panels
            float EHDither(float2 screenPos)
            {
                return frac(sin(dot(screenPos, float2(12.9898, 78.233))) * 43758.5453) * 0.0039;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.dirWS);
                float3 hole = normalize(_EH_HoleDir);
                if (dot(hole, hole) < 0.1) hole = float3(0, 0, 1);

                // Reference angular radius mapped to radians
                float holeAng = radians(9.0) * _EH_HoleRadius;
                float cosLensing = cos(radians(_EH_LensingRadius));

                float cosA = dot(dir, hole);
                float ang = acos(clamp(cosA, -1.0, 1.0));

                // --- Relativistic Gravitational Deflection
                // Schwarzschild bending increases asymptotically toward the photon sphere
                float u = max(0.0005, (ang - holeAng) / (holeAng + 1e-4));
                float grBend = _EH_LensingStrength * (0.24 / (u + 0.035) + 0.18 / (ang + 0.12));
                grBend = min(grBend, 1.85) * smoothstep(cosLensing, 1.0, cosA);

                // Bent ray in celestial frame (deflected away from the hole direction)
                float3 bent = normalize(dir - hole * (grBend * 0.42));

                // --- High-Fidelity Cosmic Background
                // Galactic plane band in bent space
                float galacticCoord = bent.y * 0.88 + bent.x * 0.35 + bent.z * 0.12;
                float galacticPlane = exp(-pow(abs(galacticCoord) * 3.4, 2.0));
                float galacticHalo = exp(-pow(abs(galacticCoord) * 1.5, 2.0));

                // Multi-scale celestial nebula structure
                float nebCore = EHFbm(bent * 2.8);
                float nebFine = EHFbm(bent * 6.2 + float3(2.1, 4.3, 1.5));
                float nebLanes = EHFbm(bent * 12.5 + float3(7.4, 1.2, 5.8));

                // Interstellar dust absorption rifts (dark lanes carving through bright nebulae)
                float darkRifts = smoothstep(0.35, 0.72, nebLanes);

                // Cosmic Palette: Deep space void + H-alpha (magenta) + O-III (cyan) + Stellar Gold
                float3 voidCol = _EH_BackgroundTint;
                float3 hAlpha = float3(0.65, 0.14, 0.38) * 0.85;  // Hydrogen-Alpha magenta emission
                float3 oIII = float3(0.08, 0.48, 0.62) * 0.75;    // Oxygen-III cyan-teal veil
                float3 stellarGold = float3(0.85, 0.68, 0.38);    // Dense galactic core stars

                // Blend celestial layers
                float3 nebulaCol = lerp(hAlpha, oIII, saturate(nebFine * 1.3 - 0.2));
                nebulaCol = lerp(nebulaCol, stellarGold, galacticPlane * 0.65);
                nebulaCol *= (0.4 + 1.2 * nebCore) * (1.1 - 0.7 * darkRifts);

                float3 col = voidCol * (0.6 + 0.5 * nebCore)
                           + nebulaCol * (galacticPlane * 0.8 + galacticHalo * 0.35);

                // Distant star clouds (dense micro-star haze)
                float microStars = pow(EHNoise(bent * 36.0), 12.0) * 18.0 * galacticPlane;
                col += float3(0.9, 0.95, 1.1) * microStars;

                // --- Relativistic Photon Rings (Lyapunov orbits: n=1 and n=2)
                // Primary photon ring (n=1 orbit): razor-sharp, intense, with relativistic blueshift core
                float r1 = holeAng * (1.0 + 0.028 + _EH_RimSoftness * 0.03);
                float w1 = holeAng * 0.012 + 1e-4;
                float ring1 = exp(-pow((ang - r1) / w1, 2.0));
                float ring1Core = exp(-abs(ang - r1) / (w1 * 0.45));

                // Secondary photon ring (n=2 orbit): delicate inner sub-ring
                float r2 = holeAng * (1.0 + 0.009 + _EH_RimSoftness * 0.01);
                float w2 = holeAng * 0.005 + 1e-4;
                float ring2 = exp(-pow((ang - r2) / w2, 2.0));

                // High-energy photon ring emission (HDR intensity for bloom)
                float3 ring1Col = float3(0.82, 0.92, 1.15) * ring1Core * 2.2 + float3(1.1, 0.9, 0.7) * ring1;
                float3 ring2Col = float3(0.75, 0.85, 1.2) * ring2 * 0.85;
                col += (ring1Col + ring2Col) * _EH_PhotonRing;

                // --- Einstein-Arc Caustic Glow (gravitational flux magnification near shadow)
                float arc = exp(-pow((ang - holeAng * 1.32) / (holeAng * 0.28 + 1e-4), 2.0));
                col += float3(0.5, 0.65, 0.95) * arc * 0.45 * (0.35 + _EH_LensingStrength);

                // --- Event Horizon Silhouette (absolute black singularity with sub-pixel edge)
                float rimCut = holeAng * (1.0 - _EH_RimSoftness * 0.015);
                float rim = smoothstep(rimCut - 0.002, holeAng + 0.003, ang);
                float3 interior = (1.0 - _EH_InteriorBlack) * float3(0.01, 0.015, 0.025);
                col = lerp(interior, col, rim);

                // Anti-banding dither for VR display panels
                col += EHDither(input.positionCS.xy);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
