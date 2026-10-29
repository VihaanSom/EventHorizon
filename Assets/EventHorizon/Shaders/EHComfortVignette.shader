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

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(positionOS);
                o.uv = uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float d = length((i.uv - 0.5) * 2.0);
                float mask = smoothstep(0.55, 1.15, d);
                return half4(_EH_VignetteColor, mask * _EH_VignetteAlpha);
            }
            ENDHLSL
        }
    }
}
