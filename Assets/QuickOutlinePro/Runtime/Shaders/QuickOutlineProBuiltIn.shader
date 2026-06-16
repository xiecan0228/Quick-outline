Shader "QuickOutlinePro/BuiltIn/Outline"
{
    Properties
    {
        [HDR]_OutlineColor ("Outline Color", Color) = (0,0.65,1,1)
        _OutlineWidth ("Outline Width", Range(0,0.25)) = 0.025
        _GlowIntensity ("Glow Intensity", Range(0,8)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _GlowIntensity;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 expanded = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(expanded, 1));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return fixed4(_OutlineColor.rgb * max(1, _GlowIntensity), _OutlineColor.a); }
            ENDCG
        }
    }
    FallBack Off
}
