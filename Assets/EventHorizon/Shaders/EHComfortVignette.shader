// EHComfortVignette — camera-attached quad that gently darkens the frame
// edges during high-motion phases when comfort mode is on. No fullscreen pass.
Shader "EventHorizon/ComfortVignette"
{
    Properties
    {
        _EH_VignetteAlpha ("Alpha", Range(0, 1)) = 0.0
        _EH_VignetteColor ("Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _EH_VignetteAlpha;
            float3 _EH_VignetteColor;

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float d = length((i.uv - 0.5) * 2.0);
                float mask = smoothstep(0.55, 1.15, d);
                return half4(_EH_VignetteColor, mask * _EH_VignetteAlpha);
            }
            ENDHLSL
        }
    }
}
