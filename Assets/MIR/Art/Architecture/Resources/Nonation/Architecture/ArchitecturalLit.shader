// NONATION original-authored Built-in RP lit city facade with ambientCG CC0
// albedo, tangent normals and linear roughness when actually present.
// Dedicated WebGL shader, deliberately no URP/HDRP includes: the project
// uses Built-in render pipeline and its glTFast integration is pinned to it.
// Located inside Resources so the player cannot strip this material path.
Shader "NONATION/ArchitecturalLit"
{
    Properties
    {
        [MainTexture] _MainTex ("Facade albedo", 2D) = "white" {}
        [MainColor] _Color ("Facade tint", Color) = (1,1,1,1)
        _BaseMap ("Compatibility albedo", 2D) = "white" {}
        _BaseColor ("Compatibility tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = .24
        [Normal] _NormalMap ("Tangent-space normal", 2D) = "bump" {}
        _RoughnessMap ("Linear roughness", 2D) = "white" {}
        _UseNormal ("Enable tangent-space normal", Range(0,1)) = 0
        _UseRoughness ("Enable measured roughness", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _RoughnessMap;
        fixed4 _Color;
        half _Metallic;
        half _Smoothness;
        half _UseNormal;
        half _UseRoughness;
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_RoughnessMap;
        };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 sampled=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            o.Albedo=sampled.rgb;
            o.Metallic=saturate(_Metallic);
            // Actual source scan texture is imported as a linear roughness
            // map. Never multiply its gamma twice or use a glossy fallback.
            half rough=tex2D(_RoughnessMap,IN.uv_RoughnessMap).r;
            o.Smoothness=saturate(lerp(_Smoothness,1.0h-rough,_UseRoughness));
            if(_UseNormal>0.5h)
                o.Normal=UnpackNormal(tex2D(_NormalMap,IN.uv_NormalMap));
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
