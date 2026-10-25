// NONATION original-authored Built-in RP physically lit city facade.
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
        fixed4 _Color;
        half _Metallic;
        half _Smoothness;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 sampled=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            o.Albedo=sampled.rgb;
            o.Metallic=saturate(_Metallic);
            o.Smoothness=saturate(_Smoothness);
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
