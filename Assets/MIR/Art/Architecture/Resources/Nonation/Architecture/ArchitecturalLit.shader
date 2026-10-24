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
        _NormalMap ("OpenGL Tangent Normal", 2D) = "bump" {}
        _RoughnessMap ("Surface Roughness", 2D) = "white" {}
        _UseNormal ("Normal Map Enabled", Float) = 0
        _UseRoughness ("Roughness Map Enabled", Float) = 0
        _NormalSRGB ("Normal Source Was sRGB On GPU", Float) = 0
        _RoughnessSRGB ("Roughness Source Was sRGB On GPU", Float) = 0
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float _Metallic;
            float _Smoothness;
            float _UseNormal;
            float _UseRoughness;
            float _NormalSRGB;
            float _RoughnessSRGB;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);

            // Unity's default import of JPEG data maps may be sRGB. Recover
            // the original 0..1 encoded normal/roughness values after hardware
            // sRGB sampling. Avoid treating color-space-corrected channels as
            // physical tangent vectors. The bools are set from graphicsFormat.
            half3 DecodeStoredMap(half3 sample, half wasSRGB)
            {
                if(wasSRGB<.5h)return sample;
                half3 lo=sample*12.92h;
                half3 hi=1.055h*pow(max(sample,0.0h),1.0h/2.4h)-.055h;
                return lerp(lo,hi,step(.0031308h,sample));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half3 tangentWS : TEXCOORD4;
                half3 bitangentWS : TEXCOORD5;
            };
            Varyings Vertex(Attributes v)
            {
                Varyings o;
                VertexPositionInputs position=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=position.positionCS;
                o.positionWS=position.positionWS;
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS=normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.bitangentWS=normalize(cross(o.normalWS,o.tangentWS))*
                    (v.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
                o.shadowCoord=GetShadowCoord(position);
                return o;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*_BaseColor;
                half3 normal=normalize(input.normalWS);
                if(_UseNormal>.5h)
                {
                    half3 encNormal=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,input.uv).rgb;
                    encNormal=DecodeStoredMap(encNormal,_NormalSRGB);
                    half3 tangentNormal=normalize(encNormal*2.0h-1.0h);
                    normal=normalize(tangentNormal.x*input.tangentWS+
                        tangentNormal.y*input.bitangentWS+
                        tangentNormal.z*normal);
                }
                half roughness=saturate(1.0h-_Smoothness);
                if(_UseRoughness>.5h)
                {
                    half encodedRough=SAMPLE_TEXTURE2D(_RoughnessMap,
                        sampler_RoughnessMap,input.uv).r;
                    roughness=DecodeStoredMap(
                        half3(encodedRough,encodedRough,encodedRough),
                        _RoughnessSRGB).r;
                }
                half smooth=saturate(1.0h-roughness);
                Light sun=GetMainLight(input.shadowCoord);
                half3 L=normalize(sun.direction);
                half ndl=saturate(dot(normal,L));
                half shadow=sun.shadowAttenuation;
                half3 toEye=normalize(_WorldSpaceCameraPos.xyz-input.positionWS);
                half3 halfway=normalize(L+toEye);
                half shine=pow(saturate(dot(normal,halfway)),
                    lerp(5.0h,96.0h,smooth*smooth));
                half3 diffuse=albedo.rgb*(.38h+sun.color*ndl*shadow*.66h);
                half3 specular=sun.color*shine*shadow*
                    (lerp(.025h,.35h,saturate(_Metallic))*smooth);
                half fogFactor=ComputeFogFactor(input.positionCS.z);
                half3 litColor=MixFog(saturate(diffuse+specular),fogFactor);
                return half4(litColor,albedo.a);
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
            #pragma multi_compile_fog
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
                UNITY_FOG_COORDS(2)
            };
            v2f vert(appdata input)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(input.vertex);
                o.uv=TRANSFORM_TEX(input.uv,_MainTex);
                o.normalWorld=UnityObjectToWorldNormal(input.normal);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 tex=tex2D(_MainTex,input.uv)*_Color;
                float ndl=saturate(dot(normalize(input.normalWorld),
                    normalize(_WorldSpaceLightPos0.xyz)));
                tex.rgb*=.39+.61*ndl;
                UNITY_APPLY_FOG(input.fogCoord,tex);
                return tex;
            }
            ENDCG
        }
        Pass
        {
            Name "NonationBuiltInShadow"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct ShadowOutput { V2F_SHADOW_CASTER; };
            ShadowOutput shadowVert(appdata_base v)
            {
                ShadowOutput o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 shadowFrag(ShadowOutput i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    Fallback Off
}
