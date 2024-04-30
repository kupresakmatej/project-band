Shader "Custom/DoubleSidedShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MetallicTex ("Metallic (R)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0 // Slider for metallic
        _GlossinessTex ("Smoothness (R)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5 // Slider for smoothness
    }
    SubShader
    {
        Cull off

        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicTex;
        sampler2D _GlossinessTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_MetallicTex;
            float2 uv_GlossinessTex;
        };

        half _Metallic;
        half _Glossiness;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Sample the metallic and smoothness textures and combine with slider values
            fixed metallicTextureValue = tex2D(_MetallicTex, IN.uv_MetallicTex).r;
            fixed smoothnessTextureValue = tex2D(_GlossinessTex, IN.uv_GlossinessTex).r;

            o.Metallic = metallicTextureValue * _Metallic; // Multiply texture value by slider
            o.Smoothness = smoothnessTextureValue * _Glossiness; // Multiply texture value by slider
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
