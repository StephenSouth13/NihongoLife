// Recolours the hair of a Kids-pack atlas (flat colour blocks) on the GPU: CharacterLook blits the kid's texture
// through this with the hair mask that CharacterLookBuilder bakes (white = hair). Texels inside the mask take the new
// colour, keeping their brightness relative to the original hair colour; everything else is copied as is.
Shader "Hidden/NihongoLife/HairRecolor"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _Mask ("Hair mask", 2D) = "black" {}
        _From ("Original hair", Color) = (0.5, 0.35, 0.2, 1)
        _To ("New hair", Color) = (0.1, 0.08, 0.07, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Mask;
            float4 _From;
            float4 _To;

            float4 frag(v2f_img i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float m = tex2D(_Mask, i.uv).r;
                float shade = Luminance(c.rgb) / max(Luminance(_From.rgb), 0.04);
                float3 hair = saturate(_To.rgb * clamp(shade, 0.4, 1.6));
                return float4(lerp(c.rgb, hair, step(0.5, m)), c.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
