// Charter 6.3 hit-flash shader. Hand-written ShaderLab surface shader (not Shader Graph,
// per Step 6 Research: this project is Built-in RP, and .shadergraph is GUI-authored JSON,
// unreliable to hand-write) based on Unity's standard surface shader template. Lerps the
// base albedo toward white as _FlashIntensity rises. Placeholder-art project — no lighting
// showcase intended, just enough to read as a "flash" on a grey placeholder mesh.
Shader "Custom/HitFlash"
{
    Properties
    {
        _Color ("Color", Color) = (0.5,0.5,0.5,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _FlashIntensity ("Flash Intensity", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _FlashIntensity;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = lerp(baseColor.rgb, float3(1,1,1), _FlashIntensity);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = baseColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
