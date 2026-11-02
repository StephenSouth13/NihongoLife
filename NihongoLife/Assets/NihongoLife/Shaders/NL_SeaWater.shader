// Stylised tropical sea for 65_PhuQuocIsland (URP, opaque, WebGL2-safe).
// World-space so one material works on the 1:1 beach, the resort and the 1:100 overview sea.
// Two scrolling sine wave sets perturb the normal; colour blends shallow turquoise -> deep blue by view angle (fresnel),
// plus the main light's specular glint and soft foam streaks. No depth texture needed.
Shader "NihongoLife/SeaWater"
{
    Properties
    {
        _Shallow ("Shallow colour", Color) = (0.25, 0.78, 0.80, 1)
        _Deep ("Deep colour", Color) = (0.05, 0.33, 0.55, 1)
        _Foam ("Foam colour", Color) = (0.95, 0.98, 1, 1)
        _WaveScale ("Wave scale (1/m)", Float) = 0.35
        _WaveSpeed ("Wave speed", Float) = 0.6
        _WaveStrength ("Wave strength", Range(0, 1)) = 0.35
        _FoamAmount ("Foam amount", Range(0, 1)) = 0.25
        _Gloss ("Gloss", Range(8, 512)) = 160
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow, _Deep, _Foam;
                float _WaveScale, _WaveSpeed, _WaveStrength, _FoamAmount, _Gloss;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            float2 Slope(float2 xz, float t)
            {
                float2 d1 = normalize(float2(1.0, 0.35)), d2 = normalize(float2(-0.4, 1.0)), d3 = normalize(float2(0.7, -0.8));
                float k = _WaveScale;
                float a = dot(xz, d1) * k + t * 1.1, b = dot(xz, d2) * k * 1.7 + t * 1.6, c = dot(xz, d3) * k * 3.1 + t * 2.3;
                return d1 * cos(a) + d2 * cos(b) * 0.6 + d3 * cos(c) * 0.3;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 s = Slope(i.positionWS.xz, t) * _WaveStrength;
                float3 n = normalize(float3(-s.x, 1.0, -s.y));
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fres = pow(1.0 - saturate(dot(n, v)), 3.0);
                half3 col = lerp(_Shallow.rgb, _Deep.rgb, saturate(0.35 + fres * 0.9));
                Light l = GetMainLight();
                float3 h = normalize(l.direction + v);
                float spec = pow(saturate(dot(n, h)), _Gloss) * 1.4;
                float diff = 0.55 + 0.45 * saturate(dot(n, l.direction));
                float streak = sin(i.positionWS.x * 0.9 + t * 1.3 + sin(i.positionWS.z * 0.6 - t)) * sin(i.positionWS.z * 1.3 - t * 0.8);
                float foam = smoothstep(1.0 - _FoamAmount * 0.5, 1.0, streak) * _FoamAmount;
                // Only the brightness of the sun lights the water: its full colour turned the sea brown at sunrise and sunset.
                half sun = dot(l.color, half3(0.3, 0.59, 0.11));
                // The sky's ambient keeps the water lit (and follows night and day) even when no main light reaches this pass.
                half3 ambient = SampleSH(half3(0, 1, 0));
                col = col * (ambient + diff * lerp(sun.xxx, l.color, 0.25)) + spec * l.color + _Foam.rgb * foam;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
