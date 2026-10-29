// Re-tints the flat colour blocks of a Kids-pack atlas (skin, top, bottom) on the GPU. CharacterLook blits a kid's
// texture through this before the hair pass: texels near one of the three "from" colours (the model's own skin, top
// and bottom, measured by CharacterVariantBuilder) take the matching "to" colour, keeping their brightness relative to
// the original block. A "to" colour with zero alpha leaves that block alone. Everything else is copied as is.
Shader "Hidden/NihongoLife/PaletteRecolor"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _Mask ("Hair mask", 2D) = "black" {}
        _From0 ("Skin from", Vector) = (0, 0, 0, 0)
        _To0 ("Skin to", Vector) = (0, 0, 0, 0)
        _From1 ("Top from", Vector) = (0, 0, 0, 0)
        _To1 ("Top to", Vector) = (0, 0, 0, 0)
        _From2 ("Bottom from", Vector) = (0, 0, 0, 0)
        _To2 ("Bottom to", Vector) = (0, 0, 0, 0)
        _Tolerance ("Tolerance", Float) = 0.045
        _ChromaTolerance ("Hue tolerance", Float) = 0.03
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
            float4 _From0, _To0, _From1, _To1, _From2, _To2;
            float _Tolerance, _ChromaTolerance;

            // (Vector, not Color: Unity would convert Color properties to linear on upload.) The colours of the blocks are given as authored (sRGB bytes / 255, set with SetVector): the match is
            // done in that space so dark and light blocks get the same tolerance in both colour spaces.
            float3 ToAuthored(float3 c)
            {
            #ifdef UNITY_COLORSPACE_GAMMA
                return c;
            #else
                return LinearToGammaSpace(c);
            #endif
            }

            float3 FromAuthored(float3 c)
            {
            #ifdef UNITY_COLORSPACE_GAMMA
                return c;
            #else
                return GammaToLinearSpace(c);
            #endif
            }

            float Lum(float3 c) { return dot(c, float3(0.3, 0.59, 0.11)); }

            // A texel belongs to a block when it is that colour, or the same hue at another brightness (the atlases
            // shade their blocks with gradients; only done for skin, where the face shading needs it, so a light detail of the face texture that happens to share the hue of a top is not turned into the top) - never black outlines or the white of the eyes.
            float Hit(float3 g, float4 from, float4 to, float hueMatch)
            {
                float near = step(distance(g, from.rgb), _Tolerance);
                float ratio = Lum(g) / max(Lum(from.rgb), 0.02);
                float3 chromaG = g / max(g.r + g.g + g.b, 0.02);
                float3 chromaFrom = from.rgb / max(from.r + from.g + from.b, 0.02);
                float sameHue = step(distance(chromaG, chromaFrom), _ChromaTolerance) * step(0.55, ratio) * step(ratio, 1.7) * hueMatch;
                return max(near, sameHue) * step(0.5, to.a) * step(0.5, from.a);
            }

            float3 Swapped(float3 g, float4 from, float4 to)
            {
                float shade = clamp(Lum(g) / max(Lum(from.rgb), 0.02), 0.5, 1.35);
                return saturate(to.rgb * shade);
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float3 g = ToAuthored(c.rgb);
                float notHair = 1.0 - step(0.5, tex2D(_Mask, i.uv).r); // hair is recoloured by its own mask afterwards
                float h0 = Hit(g, _From0, _To0, 1.0) * notHair;
                float h1 = Hit(g, _From1, _To1, 0.0) * notHair * (1.0 - h0);
                float h2 = Hit(g, _From2, _To2, 0.0) * notHair * (1.0 - h0) * (1.0 - h1);
                float3 o = g;
                o = lerp(o, Swapped(g, _From0, _To0), h0);
                o = lerp(o, Swapped(g, _From1, _To1), h1);
                o = lerp(o, Swapped(g, _From2, _To2), h2);
                return float4(FromAuthored(o), c.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
