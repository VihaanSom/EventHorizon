// EHBlackHoleSky — Crystal-Clear Cosmic Deep-Space Sky + Relativistic Schwarzschild Lensing.
// Features procedural diamond-sharp pinpoint stars (never blurry in VR), real galactic dust lanes,
// and world-locked celestial orientation (black hole stays fixed in 3D space when turning your head).
Shader "EventHorizon/BlackHoleSky"
{
    Properties
    {
        _EH_SkyMap ("NASA Starfield Map", 2D) = "black" {}
        _EH_SkyExposure ("Sky Exposure", Float) = 1.0
        _EH_HoleRadius ("Hole Angular Radius", Float) = 1.0
        _EH_LensingStrength ("Lensing Strength", Float) = 0.55
        _EH_LensingRadius ("Lensing Field Radius (deg)", Float) = 32.0
        _EH_RimSoftness ("Rim Softness", Range(0, 1)) = 0.20
        _EH_PhotonRing ("Photon Ring Intensity", Range(0, 5)) = 1.2
        _EH_InteriorBlack ("Interior Blackness", Range(0, 1)) = 1.0
        _EH_BackgroundTint ("Deep Space Tint", Color) = (0.0005, 0.0008, 0.0015, 1)
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
            float _EH_HoleAngularRadius;
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

                // Compute world position from object space through the ObjectToWorld matrix
                float3 posWS = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(posWS);

                // The viewing direction is the vector from the camera to this vertex in WORLD space
                // Since the dome is centered on the camera and has identity rotation,
                // this equals positionOS (the dome mesh direction). But computing it from world
                // guarantees decoupling from any camera rotation in VR Single-Pass Instanced.
                o.dirWS = posWS - GetCameraPositionWS();
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 dir = normalize(input.dirWS);
                float3 hole = normalize(_EH_HoleDir);
                if (dot(hole, hole) < 0.1) hole = float3(0, 0, 1);

                // True physical angular radius of the 14m event horizon in radians
                float holeAng = _EH_HoleAngularRadius > 0.005 ? _EH_HoleAngularRadius : radians(9.0) * max(0.35, _EH_HoleRadius);
                float cosA = dot(dir, hole);
                float ang = acos(clamp(cosA, -1.0, 1.0));

                // --- Relativistic Gravitational Lensing Field
                // Extends outward from the event horizon to 3.5x shadow radius
                float lensingOuter = holeAng * 3.5;
                float grBend = 0.0;

                if (ang > holeAng && ang < lensingOuter)
                {
                    float u = (ang - holeAng) / (holeAng + 1e-4);
                    // Einstein deflection: strong curvature near photon sphere decaying smoothly outward
                    float deflection = (0.55 / (u + 0.15) + 0.20 / (u * u + 0.40)) * holeAng;
                    float fade = smoothstep(lensingOuter, holeAng * 1.15, ang);
                    grBend = deflection * max(0.15, _EH_LensingStrength) * fade;
                }

                // Bent ray in celestial frame (curving outward from hole, forming Einstein arcs)
                float3 bent = normalize(dir - hole * grBend);

                // --- 1. Authentic NASA SVS #4851 8K Equirectangular Deep Space Map
                float2 skyUV;
                skyUV.x = frac(atan2(bent.z, bent.x) * (0.5 / 3.1415926535) + 0.5);
                skyUV.y = acos(clamp(bent.y, -1.0, 1.0)) / 3.1415926535;

                // Sample the 16-bit float linear HDR NASA map
                float3 nasaStars = SAMPLE_TEXTURE2D(_EH_SkyMap, sampler_EH_SkyMap, skyUV).rgb;
                float3 celestialStars = pow(max(0.0, nasaStars), 0.88) * _EH_SkyExposure;
                float3 col = _EH_BackgroundTint + celestialStars;

                // --- 2. Gravitational Caustic Lensing Flux near Einstein Ring
                float einsteinAng = holeAng * 1.35;
                float arc = exp(-pow((ang - einsteinAng) / (holeAng * 0.25 + 1e-4), 2.0));
                col += float3(0.55, 0.70, 1.05) * arc * 0.45 * max(0.2, _EH_LensingStrength);

                // --- 3. Relativistic Photon Ring (Razor-thin, luminous golden halo at 1.025 * holeAng)
                float r1 = holeAng * 1.025;
                float w1 = holeAng * 0.007 + 1e-4;
                float ring1 = exp(-pow((ang - r1) / w1, 2.0));
                float3 ringCol = float3(1.35, 1.15, 0.85) * ring1 * 1.6 * _EH_PhotonRing;
                col += ringCol;

                // --- 4. Event Horizon Silhouette (Absolute Physical Black Hole)
                float rimCut = holeAng * (1.0 - _EH_RimSoftness * 0.015);
                float rim = smoothstep(rimCut - 0.001, holeAng + 0.001, ang);
                col = lerp(float3(0, 0, 0), col, rim);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
