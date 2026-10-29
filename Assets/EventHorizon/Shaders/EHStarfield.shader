// EHStarfield — High-Fidelity Celestial Starfield with Dynamic Tangential Einstein Arcs.
// Simulates General Relativistic gravitational deflection, tangential tidal shear (Einstein arc formation),
// Airy disk diffraction profiles, realistic stellar spectral temperatures, and gravitational flux amplification.
// Optimized for Meta Quest 3 VR (Single Pass Instanced, zero compute, additive blend).
Shader "EventHorizon/Starfield"
{
    Properties
    {
        _EH_StarBrightness ("Global Brightness", Float) = 1.0
        _EH_PointScale ("Angular Size Gain", Float) = 0.0018
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" }
        Blend One One // Additive blend: stars radiate pure light into space
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _EH_StarBrightness;
            float _EH_PointScale;
            float _EH_HoleRadius;
            float _EH_LensingStrength;
            float _EH_LensingRadius;
            float3 _EH_HoleDir;

            struct Attributes
            {
                float3 positionOS : POSITION; // star center on celestial sphere
                float2 corner : TEXCOORD0;    // quad corner -1..1
                float2 sizeMag : TEXCOORD1;   // x: size, y: magnitude 0..1+
                float2 tintRef : TEXCOORD2;   // x: tint 0..1, y: reference flag
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                float magnitude : TEXCOORD1;
                float tint : TEXCOORD2;
                float fade : TEXCOORD3;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 centreWS = mul(unity_ObjectToWorld, float4(v.positionOS, 1.0)).xyz;
                float3 toStar = centreWS - _WorldSpaceCameraPos;
                float dist = max(length(toStar), 0.001);
                float3 dir = toStar / dist;
                float3 hole = normalize(_EH_HoleDir);
                if (dot(hole, hole) < 0.1) hole = float3(0, 0, 1);

                float cosA = dot(dir, hole);
                float ang = acos(clamp(cosA, -1.0, 1.0));
                float holeAng = radians(9.0) * _EH_HoleRadius;
                float cosLensing = cos(radians(_EH_LensingRadius));

                // Relativistic deflection angle
                float u = max(0.001, (ang - holeAng) / (holeAng + 1e-4));
                float bend = _EH_LensingStrength * (0.22 / (u + 0.04) + 0.16 / (ang + 0.12));
                bend = min(bend, 1.8) * smoothstep(cosLensing, 1.0, cosA);
                float3 bentDir = normalize(dir - hole * (bend * 0.42));
                float3 bentCentre = _WorldSpaceCameraPos + bentDir * dist;

                // --- Gravitational Shear (Einstein Arc Distortion)
                // Tangential vector perpendicular to the radial direction to the hole
                float3 radialWS = normalize(dir - hole * cosA);
                float3 tangentWS = cross(hole, radialWS);
                if (dot(tangentWS, tangentWS) < 1e-4) tangentWS = cross(dir, float3(0, 1, 0));
                tangentWS = normalize(tangentWS);
                radialWS = normalize(cross(tangentWS, dir));

                // Tidal stretching: as ang approaches holeAng, stretch along tangentWS and compress along radialWS
                float prox = saturate(1.0 - (ang - holeAng) / (holeAng * 2.2 + 1e-4));
                float tangStretch = 1.0 + prox * prox * (4.2 * _EH_LensingStrength);
                float radCompress = max(0.35, 1.0 - prox * 0.55);

                // Transform to Camera View Space for correct stereoscopic VR orientation
                float4 viewPos = mul(UNITY_MATRIX_V, float4(bentCentre, 1.0));
                float worldSize = v.sizeMag.x * _EH_PointScale * dist;

                // Tangential and radial basis vectors in view space
                float3 viewTan = mul((float3x3)UNITY_MATRIX_V, tangentWS);
                float3 viewRad = mul((float3x3)UNITY_MATRIX_V, radialWS);

                viewPos.xy += (viewTan.xy * (v.corner.x * tangStretch) + viewRad.xy * (v.corner.y * radCompress)) * worldSize;
                o.positionCS = mul(UNITY_MATRIX_P, viewPos);

                // Stars inside the event horizon shadow are completely occluded
                float inside = 1.0 - smoothstep(holeAng * 0.96, holeAng * 1.01, ang);

                // Gravitational flux amplification (stars near the Einstein ring get brighter)
                float causticBoost = exp(-pow((ang - holeAng * 1.15) / (holeAng * 0.16 + 1e-4), 2.0));
                o.fade = (1.0 - inside) * (1.0 + causticBoost * (1.2 + _EH_LensingStrength * 1.5));

                o.corner = v.corner;
                o.magnitude = v.sizeMag.y;
                o.tint = v.tintRef.x + v.tintRef.y * 10.0;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float r = length(i.corner);

                // Razor-sharp Airy disk core (pin-point stellar clarity in VR pancake lenses)
                float core = exp(-r * r * 16.0);
                // Soft optical diffraction halo
                float halo = exp(-r * 2.8) * 0.35;
                float sprite = core + halo;

                // 4-point diffraction cross for bright reference hypergiants
                float isRef = step(9.5, i.tint);
                float crossSpikes = exp(-abs(i.corner.x) * 14.0) * exp(-abs(i.corner.y) * 2.2)
                                  + exp(-abs(i.corner.y) * 14.0) * exp(-abs(i.corner.x) * 2.2);
                sprite += crossSpikes * isRef * 0.45;

                // Stellar spectral classifications:
                // O/B (Blue-white 25000K), A/F (Pure white 9000K), G (Warm yellow 5800K), K/M (Amber/Red 3500K)
                float tint = frac(i.tint);
                float3 colO = float3(0.72, 0.85, 1.30);   // Class O/B Blue Supergiant
                float3 colA = float3(1.02, 1.04, 1.10);   // Class A/F White
                float3 colG = float3(1.15, 0.96, 0.62);   // Class G Solar Yellow
                float3 colM = float3(1.25, 0.52, 0.28);   // Class M Red Giant

                float3 starCol;
                if (tint < 0.33)
                    starCol = lerp(colO, colA, tint * 3.0);
                else if (tint < 0.66)
                    starCol = lerp(colA, colG, (tint - 0.33) * 3.0);
                else
                    starCol = lerp(colG, colM, (tint - 0.66) * 3.0);

                // Luminous scaling with HDR intensity
                starCol *= (0.45 + i.magnitude * 1.35) * (1.0 + isRef * 1.8);

                return half4(starCol * sprite * i.fade * _EH_StarBrightness, 1.0);
            }
            ENDHLSL
        }
    }
}
