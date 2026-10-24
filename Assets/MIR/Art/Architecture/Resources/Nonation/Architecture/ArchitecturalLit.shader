// NONATION original-authored architecture shader.
// A Resources-backed shader avoids runtime Shader.Find stripping in WebGL.
// The first SubShader targets URP 17, the second built-in as a safe fallback.
Shader "NONATION/ArchitecturalLit"
{
    Properties
    {
        _BaseMap ("Architectural Albedo", 2D) = "white" {}
        _BaseColor ("Base Tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Surface Smoothness", Range(0,1)) = .26
        _MainTex ("Compatibility Albedo", 2D) = "white" {}
        _Color ("Compatibility Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "NonationArchitectureForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float _Metallic;
            float _Smoothness;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };
            Varyings Vertex(Attributes v)
            {
                Varyings o;
                VertexPositionInputs position=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=position.positionCS;
                o.positionWS=position.positionWS;
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
                o.shadowCoord=GetShadowCoord(position);
                return o;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*_BaseColor;
                half3 normal=normalize(input.normalWS);
                Light sun=GetMainLight(input.shadowCoord);
                half3 L=normalize(sun.direction);
                half ndl=saturate(dot(normal,L));
                half shadow=sun.shadowAttenuation;
                half3 toEye=normalize(_WorldSpaceCameraPos.xyz-input.positionWS);
                half3 halfway=normalize(L+toEye);
                half shine=pow(saturate(dot(normal,halfway)),
                    lerp(5.0h,84.0h,saturate(_Smoothness)));
                half3 diffuse=albedo.rgb*(.39h+sun.color*ndl*shadow*.67h);
                half3 specular=sun.color*shine*shadow*
                    (lerp(.028h,.36h,saturate(_Metallic))*saturate(_Smoothness));
                return half4(saturate(diffuse+specular),albedo.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    // Built-in pipeline rescue pass. Runtime material still retains texture.
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Smoothness;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWorld : TEXCOORD1;
            };
            v2f vert(appdata input)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(input.vertex);
                o.uv=TRANSFORM_TEX(input.uv,_MainTex);
                o.normalWorld=UnityObjectToWorldNormal(input.normal);
                return o;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 tex=tex2D(_MainTex,input.uv)*_Color;
                float ndl=saturate(dot(normalize(input.normalWorld),
                    normalize(_WorldSpaceLightPos0.xyz)));
                tex.rgb*=.39+.61*ndl;
                return tex;
            }
            ENDCG
        }
    }
    Fallback Off
}
