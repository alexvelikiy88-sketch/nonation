// NONATION fallback material shader. Used when built-in/URP lit shaders are
// stripped because the scene creates nearly all renderers dynamically.
// This exact Shader asset is under Resources and is included in WebGL Player.
Shader "MIR/RuntimeFallback"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct Input
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct Output
            {
                float4 pos : SV_POSITION;
                fixed brightness : TEXCOORD0;
            };

            Output vert(Input input)
            {
                Output output;
                output.pos = UnityObjectToClipPos(input.vertex);
                float3 worldNormal = normalize(UnityObjectToWorldNormal(input.normal));
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                output.brightness = 0.38 + 0.62 * saturate(dot(worldNormal, lightDir));
                return output;
            }

            fixed4 frag(Output input) : SV_Target
            {
                return fixed4(_Color.rgb * input.brightness, _Color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
