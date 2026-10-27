// NONATION self-contained Built-in RP material. The old technical alpha used
// a one-pass vertex Lambert shader, making streets, trees and 3D furniture
// appear flat and disconnected from the custom physically lit facades.
// This Resources-pinned shader uses the same Unity Standard lighting response
// as ArchitecturalLit, with real shadow receiving and coherent roughness.
// It is original NONATION source, not a third-party shader asset.
Shader "MIR/RuntimeFallback"
{
    Properties
    {
        [MainColor] _Color ("Base Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = .12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma multi_compile_instancing
        fixed4 _Color;
        half _Metallic;
        half _Smoothness;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = saturate(_Metallic);
            o.Smoothness = saturate(_Smoothness);
            o.Occlusion = 1;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
