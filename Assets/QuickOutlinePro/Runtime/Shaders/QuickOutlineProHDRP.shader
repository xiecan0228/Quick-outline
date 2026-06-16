Shader "QuickOutlinePro/HDRP/Outline"
{
    Properties { [HDR]_OutlineColor ("Outline Color", Color) = (0,0.65,1,1) _OutlineWidth ("Outline Width", Range(0,0.25)) = 0.025 _GlowIntensity ("Glow Intensity", Range(0,8)) = 1 }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Cull Front ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor; float _OutlineWidth; float _GlowIntensity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 expanded = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(expanded);
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return half4(_OutlineColor.rgb * max(1, _GlowIntensity), _OutlineColor.a); }
            ENDHLSL
        }
    }
}
