// Day/night sky for the outdoor scenes (and the street seen from interiors). Blends the project's painted day and
// night sky panoramas (ZRNAssets skydoom), warms the horizon at dawn/dusk and draws a soft sun/moon disc.
// Driven by DayNightCycle through OutdoorSky (per-scene material instance).
Shader "NihongoLife/SkyDayNight"
{
    Properties
    {
        _DayTex ("Day panorama", 2D) = "white" {}
        _NightTex ("Night panorama", 2D) = "black" {}
        _CloudyTex ("Overcast panorama", 2D) = "gray" {}
        _Night ("Night blend", Range(0, 1)) = 0
        _Overcast ("Overcast blend", Range(0, 1)) = 0
        _Rainbow ("Rainbow", Range(0, 1)) = 0
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _HorizonColor ("Horizon glow", Color) = (1, 0.55, 0.3, 1)
        _HorizonGlow ("Horizon glow strength", Range(0, 1)) = 0
        _Exposure ("Exposure", Range(0, 2)) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _SunDir ("Sun direction (to the sun)", Vector) = (0, 1, 0, 0)
        _SunColor ("Sun disc color", Color) = (1, 0.95, 0.8, 1)
        _SunSize ("Sun disc size", Range(0.9, 1)) = 0.9985
        _MoonPhase ("Moon phase (0 new, 0.5 full)", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _DayTex;
            sampler2D _NightTex;
            sampler2D _CloudyTex;
            float _Night, _Overcast, _Rainbow, _HorizonGlow, _Exposure, _Rotation, _SunSize, _MoonPhase;
            float4 _Tint, _HorizonColor, _SunDir, _SunColor;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float azimuth = atan2(d.x, d.z) / (2.0 * UNITY_PI) + _Rotation / 360.0;
                float elevation = asin(clamp(d.y, -1.0, 1.0)) / UNITY_PI; // -0.5 .. 0.5
                float2 uv = float2(frac(azimuth + 1.0), saturate(0.5 + elevation));
                float3 day = tex2D(_DayTex, uv).rgb;
                float3 night = tex2D(_NightTex, uv).rgb;
                // Overcast: the grey cloud panorama, darkened towards night.
                float3 cloudy = tex2D(_CloudyTex, uv).rgb * lerp(0.95, 0.12, _Night);
                float3 sky = lerp(lerp(day, night, _Night), cloudy, _Overcast) * _Tint.rgb;

                // Warm band along the horizon at dawn and dusk, strongest towards the sun.
                float towardsSun = saturate(dot(normalize(float3(d.x, 0, d.z) + 1e-4), normalize(float3(_SunDir.x, 0, _SunDir.z) + 1e-4)) * 0.5 + 0.5);
                float band = exp(-abs(d.y) * 9.0) * _HorizonGlow * (0.45 + 0.55 * towardsSun);
                sky = lerp(sky, _HorizonColor.rgb, saturate(band));

                // Rainbow: a 42° arc around the point opposite the sun, violet inside, red outside, with a faint
                // secondary bow at 51° (colours reversed). The centre is held a little below the horizon so the arc
                // stands in the sky at any hour (a real midday rainbow would sit under the horizon).
                if (_Rainbow > 0.001)
                {
                    float3 anti = normalize(float3(-_SunDir.x, 0.0, -_SunDir.z) + 1e-4);
                    float3 centre = normalize(anti - float3(0, 0.28, 0));
                    float angle = degrees(acos(clamp(dot(d, centre), -1.0, 1.0)));
                    float t = (angle - 39.6) / 3.2;                       // 0 violet … 1 red
                    float bow = saturate(1.0 - abs(t * 2.0 - 1.0));
                    bow = smoothstep(0.0, 0.35, bow);
                    float h = (1.0 - saturate(t)) * 0.8;                  // hue: red (0) outside … violet (0.8) inside
                    float3 spectrum = saturate(float3(abs(h * 6.0 - 3.0) - 1.0, 2.0 - abs(h * 6.0 - 2.0), 2.0 - abs(h * 6.0 - 4.0)));
                    float t2 = (angle - 50.0) / 3.6;
                    float bow2 = smoothstep(0.0, 0.35, saturate(1.0 - abs(t2 * 2.0 - 1.0))) * 0.35;
                    float h2 = saturate(t2) * 0.8;                        // secondary bow: colours reversed
                    float3 spectrum2 = saturate(float3(abs(h2 * 6.0 - 3.0) - 1.0, 2.0 - abs(h2 * 6.0 - 2.0), 2.0 - abs(h2 * 6.0 - 4.0)));
                    float above = saturate(d.y * 12.0);                  // fades into the horizon
                    sky += (spectrum * bow + spectrum2 * bow2) * 0.42 * _Rainbow * above;
                }

                // Sun by day, moon by night. _MoonPhase is 0 at the new moon, 0.5 at the full moon, 1 at the next new moon.
                // Daytime passes 0.5 so the sun stays a full disc. A new moon hides the disc and its halo.
                float3 sunDir = normalize(_SunDir.xyz);
                float facing = dot(d, sunDir);
                float disc = smoothstep(_SunSize, _SunSize + 0.0008, facing);
                float halo = pow(saturate(facing), 64.0) * 0.25;
                float illum = -cos(_MoonPhase * 6.2831853); // -1 new, 0 half, +1 full
                float3 axisUp = abs(sunDir.y) > 0.92 ? float3(1.0, 0.0, 0.0) : float3(0.0, 1.0, 0.0);
                float3 moonRight = normalize(cross(sunDir, axisUp));
                float limb = max(0.004, sqrt(max(1e-5, 1.0 - _SunSize * _SunSize)));
                float ndc = dot(d - sunDir * facing, moonRight) / limb;
                float bow = sqrt(saturate(1.0 - illum * illum));
                float lit = smoothstep(-0.12, 0.12, ndc * max(bow, 0.08) + illum);
                float visible = smoothstep(-0.97, -0.8, illum);
                sky += _SunColor.rgb * (disc * lit + halo * lit) * visible * step(0.0, d.y + 0.05) * (1.0 - _Overcast * 0.9);

                return fixed4(sky * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
