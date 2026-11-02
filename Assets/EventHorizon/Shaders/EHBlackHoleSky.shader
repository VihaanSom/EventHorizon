// EHBlackHoleSky — High-Fidelity Celestial Sky + Relativistic Schwarzschild Lensing + NASA Starfield.
// Optimized for Meta Quest 3 VR (Single-Pass Instanced, URP compatible, zero compute).
Shader "EventHorizon/BlackHoleSky"
{
    Properties
    {
        _EH_SkyMap ("NASA Starfield Map (Equirectangular)", 2D) = "black" {}
        _EH_SkyExposure ("Sky Exposure", Float) = 1.4
        _EH_HoleRadius ("Hole Angular Radius", Float) = 1.0
        _EH_LensingStrength ("Lensing Strength", Float) = 0.65
        _EH_LensingRadius ("Lensing Field Radius (deg)", Float) = 35.0
        _EH_RimSoftness ("Rim Softness", Range(0, 1)) = 0.25
        _EH_PhotonRing ("Photon Ring Intensity", Range(0, 5)) = 2.2
        _EH_InteriorBlack ("Interior Blackness", Range(0, 1)) = 1.0
        _EH_BackgroundTint ("Deep Space Tint", Color) = (0.002, 0.003, 0.008, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" }
        ZWrite On
        Cull Front // inverted dome: camera is inside looking out

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_EH_SkyMap);
            SAMPLER(sampler_EH_SkyMap);

            float _EH_SkyExposure;
            float _EH_HoleRadius;
            float _EH_LensingStrength;
            float _EH_LensingRadius;
            float _EH_RimSoftness;
            float _EH_PhotonRing;
            float _EH_InteriorBlack;
            float3 _EH_BackgroundTint;
            float3 _EH_HoleDir;

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Celestial world direction at optical infinity (immune to translation jitter)
                o.dirWS = TransformObjectToWorldDir(input.positionOS);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                return o;
            }

            // Screen-space dither to suppress VR color banding
            float EHDither(float2 screenPos)
            {
                return frac(sin(dot(screenPos, float2(12.9898, 78.233))) * 43758.5453) * 0.0025;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 dir = normalize(input.dirWS);
                float3 hole = normalize(_EH_HoleDir);
                if (dot(hole, hole) < 0.1) hole = float3(0, 0, 1);

                // Reference angular radius mapped to radians
                float holeAng = radians(9.0) * _EH_HoleRadius;
                float cosLensing = cos(radians(_EH_LensingRadius * max(1.0, _EH_HoleRadius * 0.5)));

                float cosA = dot(dir, hole);
                float ang = acos(clamp(cosA, -1.0, 1.0));

                // --- General Relativistic Deflection (Null Geodesic bending)
                // Deflection approaches infinity as light ray touches the photon orbit
                float u = max(0.0008, (ang - holeAng) / (holeAng + 1e-4));
                float grBend = _EH_LensingStrength * (0.32 / (u + 0.03) + 0.20 / (ang + 0.15));
                grBend = min(grBend, 2.2) * smoothstep(cosLensing, 1.0, cosA);

                // Bent ray in celestial frame (curving outward from hole)
                float3 bent = normalize(dir - hole * (grBend * 0.48));

                // --- Sample High-Resolution Real NASA Celestial Star Map
                // Plate carrée equirectangular spherical mapping
                float2 skyUV;
                skyUV.x = frac(atan2(bent.z, bent.x) * (0.5 / 3.1415926535) + 0.5);
                skyUV.y = acos(clamp(bent.y, -1.0, 1.0)) / 3.1415926535;

                float3 starColor = SAMPLE_TEXTURE2D(_EH_SkyMap, sampler_EH_SkyMap, skyUV).rgb;
                
                // Enhance stellar contrast: keep deep cosmic void dark, boost bright stars and Milky Way core
                float starLum = dot(starColor, float3(0.299, 0.587, 0.114));
                float3 enhancedStars = starColor * (1.0 + pow(starLum, 1.8) * 2.5) * _EH_SkyExposure;
                float3 col = _EH_BackgroundTint + enhancedStars;

                // --- Einstein-Arc Gravitational Flux Magnification
                // Stars grazing the event horizon get boosted in surface brightness
                float arc = exp(-pow((ang - holeAng * 1.25) / (holeAng * 0.24 + 1e-4), 2.0));
                col += float3(0.65, 0.78, 1.15) * arc * 0.65 * (0.4 + _EH_LensingStrength);

                // --- Relativistic Photon Ring (n=1 and n=2 unstable photon orbits)
                // Intense, razor-sharp ring of trapped light at ~1.03 to 1.08 of shadow radius
                float r1 = holeAng * (1.0 + 0.025 + _EH_RimSoftness * 0.02);
                float w1 = holeAng * 0.009 + 1e-4;
                float ring1Core = exp(-pow((ang - r1) / w1, 2.0));
                float ring1Halo = exp(-abs(ang - r1) / (w1 * 3.5));

                float r2 = holeAng * (1.0 + 0.008);
                float w2 = holeAng * 0.004 + 1e-4;
                float ring2 = exp(-pow((ang - r2) / w2, 2.0));

                // Ultra-hot thermal emission (HDR values to trigger post-processing Bloom)
                float3 ring1Col = float3(1.2, 1.4, 2.5) * ring1Core * 3.8 + float3(1.9, 1.4, 0.8) * ring1Halo * 1.2;
                float3 ring2Col = float3(0.9, 1.2, 1.8) * ring2 * 1.5;
                col += (ring1Col + ring2Col) * _EH_PhotonRing;

                // --- Event Horizon Silhouette (Absolute Physical Black Hole)
                float rimCut = holeAng * (1.0 - _EH_RimSoftness * 0.015);
                float rim = smoothstep(rimCut - 0.003, holeAng + 0.003, ang);
                float3 interior = (1.0 - _EH_InteriorBlack) * float3(0.002, 0.003, 0.005);
                col = lerp(interior, col, rim);

                // Anti-banding dither for VR display panels
                col += EHDither(input.positionCS.xy);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
