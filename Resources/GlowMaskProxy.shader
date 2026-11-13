Shader "Hidden/Nox/GlowMaskProxy"
{
    // Invisible to the normal camera: the glow mask copies must not draw anything by themselves.
    // The OutlineGlow renderer feature draws them with its own mask material (overrideMaterial).
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+2" }

        Pass
        {
            Name "Proxy"
            Tags { "LightMode" = "UniversalForward" }

            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
