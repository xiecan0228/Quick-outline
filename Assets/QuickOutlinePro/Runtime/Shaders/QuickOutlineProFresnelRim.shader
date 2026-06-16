Shader "QuickOutlinePro/FresnelRim"
{
    Properties
    {
        [HDR]_RimColor ("Fresnel Rim Color", Color) = (0, 0.65, 1, 1)
        _RimWidth ("Fresnel Rim Width", Range(0, 1)) = 0.35
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _RimColor;
            float _RimWidth;
            float _GlowIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = UnityWorldSpaceViewDir(worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normalWS = normalize(i.normalWS);
                float3 viewDirWS = normalize(i.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float power = lerp(8.0, 0.35, saturate(_RimWidth));
                float rim = pow(fresnel, power) * _GlowIntensity;
                return fixed4(_RimColor.rgb * rim, saturate(rim) * _RimColor.a);
            }
            ENDCG
        }
    }
    FallBack Off
}
