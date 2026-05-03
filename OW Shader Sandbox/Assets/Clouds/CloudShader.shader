// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/CloudShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}

        _NormalTex ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Scale", Float) = 1.0

        _AmbientStrength ("Ambient Strength", Range(0,1)) = 0.3
        _AmbientColor ("Ambient Color", Color) = (1,1,1,1)

        _Glossiness ("Smoothness", Range(0,1)) = 0.0
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _FresnelPower ("Fresnel Power", Float) = 1.0
        _FresnelFade ("Fresnel Fade", Float) = 10.0
        _AlphaBoost ("Alpha Boost", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalTex;
            float3 viewDir;
            float3 worldPos;
        };

        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        float _NormalStrength;
        float _FresnelPower;
        float _FresnelFade;
        float _AlphaBoost;
        float _AmbientStrength;
        fixed4 _AmbientColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Alpha = min(c.a * _AlphaBoost, 1.0);

            // Add ambient light
            o.Emission = o.Albedo * _AmbientStrength * _AmbientColor.rgb;

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Normal map
            fixed3 normal = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex));
            normal = normalize(lerp(float3(0.0, 0.0, 1.0), normal, _NormalStrength));
            o.Normal = normal;

            // View direction (world space)
            float3 viewDir = normalize(IN.viewDir);
            float NdotV = saturate(dot(viewDir, float3(0.0, 0.0, 1.0)));

            float fresnel = pow(1.0 - NdotV, _FresnelPower);
            float cloudVisibility = saturate(1.0 - fresnel * _FresnelFade);

            o.Alpha *= cloudVisibility;
            o.Emission *= cloudVisibility;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
