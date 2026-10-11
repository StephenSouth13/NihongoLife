using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>Procedural, anti-aliased HUD shapes (rings and discs) so the compact status widget needs no art assets.</summary>
    public static class HudGraphics
    {
        private static Sprite _ring, _disc, _gauge, _hairline;

        /// <summary>A ring 128 px wide; use with Image.Type.Filled (Radial360) for need gauges.</summary>
        public static Sprite Ring => _ring != null ? _ring : (_ring = Make("HudRing", 0.70f, 1f));

        /// <summary>A filled disc (portrait backgrounds, badges).</summary>
        public static Sprite Disc => _disc != null ? _disc : (_disc = Make("HudDisc", 0f, 1f));

        /// <summary>A slimmer ring for the need gauges of the status widget.</summary>
        public static Sprite Gauge => _gauge != null ? _gauge : (_gauge = Make("HudGauge", 0.8f, 1f));

        /// <summary>A hairline ring (portrait frame).</summary>
        public static Sprite Hairline => _hairline != null ? _hairline : (_hairline = Make("HudHairline", 0.94f, 1f));

        private static Sprite Make(string name, float inner, float outer)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            float half = size * 0.5f, edge = 1.5f / half;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((outer - r) / edge);
                    if (inner > 0f) a *= Mathf.Clamp01((r - inner) / edge);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }
    }
}
