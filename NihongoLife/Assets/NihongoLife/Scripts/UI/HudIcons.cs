using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Procedural, anti-aliased white HUD icons (tinted by the Image colour): heart, runner, fork &amp; knife,
    /// droplet, crescent moon, map pin and wallet. Each is a signed-distance shape sampled 4× per pixel, built once
    /// and cached, so the status widget needs no icon art.
    /// </summary>
    public static class HudIcons
    {
        private const int Size = 96;
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Heart => Get("heart", HeartShape);
        public static Sprite Runner => Get("runner", RunnerShape);
        public static Sprite ForkKnife => Get("forkknife", ForkKnifeShape);
        public static Sprite Drop => Get("drop", DropShape);
        public static Sprite Moon => Get("moon", MoonShape);
        public static Sprite Pin => Get("pin", PinShape);
        public static Sprite Wallet => Get("wallet", WalletShape);

        private static Sprite Get(string name, Func<Vector2, float> shape)
        {
            if (Cache.TryGetValue(name, out var sprite) && sprite != null) return sprite;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "HudIcon_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                        {
                            // p in [-1, 1], y up.
                            var p = new Vector2((x + (sx + 0.5f) / 4f) / Size * 2f - 1f, (y + (sy + 0.5f) / 4f) / Size * 2f - 1f);
                            if (shape(p) <= 0f) inside++;
                        }
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(inside * 255 / 16));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            Cache[name] = sprite;
            return sprite;
        }

        // ─────────── Signed-distance helpers (negative = inside) ───────────

        private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        private static float Box(Vector2 p, Vector2 c, Vector2 half, float round = 0f)
        {
            var d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + Vector2.one * round;
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        private static float Union(params float[] d) { float m = float.MaxValue; foreach (var v in d) m = Mathf.Min(m, v); return m; }

        // ─────────── Shapes ───────────

        private static float HeartShape(Vector2 p)
        {
            // Inigo Quilez's exact heart SDF, fitted to the icon box.
            var q = new Vector2(Mathf.Abs(p.x) * 0.72f, (p.y + 0.78f) * 0.72f);
            float d;
            if (q.y + q.x > 1f) d = (q - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2f) / 4f;
            else
            {
                float a2 = (q - new Vector2(0f, 1f)).sqrMagnitude;
                float m = Mathf.Max(q.x + q.y, 0f) * 0.5f;
                float b2 = (q - new Vector2(m, m)).sqrMagnitude;
                d = Mathf.Sqrt(Mathf.Min(a2, b2)) * Mathf.Sign(q.x - q.y);
            }
            return d;
        }

        private static float RunnerShape(Vector2 p)
        {
            const float w = 0.085f;
            return Union(
                Circle(p, new Vector2(0.2f, 0.62f), 0.15f),
                Segment(p, new Vector2(0.12f, 0.36f), new Vector2(-0.06f, -0.08f), w * 1.25f), // torso
                Segment(p, new Vector2(0.1f, 0.3f), new Vector2(-0.24f, 0.2f), w),             // back arm
                Segment(p, new Vector2(-0.24f, 0.2f), new Vector2(-0.4f, 0.0f), w),
                Segment(p, new Vector2(0.12f, 0.3f), new Vector2(0.36f, 0.18f), w),            // front arm
                Segment(p, new Vector2(0.36f, 0.18f), new Vector2(0.5f, 0.32f), w),
                Segment(p, new Vector2(-0.06f, -0.08f), new Vector2(0.24f, -0.3f), w * 1.1f),  // front leg
                Segment(p, new Vector2(0.24f, -0.3f), new Vector2(0.18f, -0.7f), w * 1.1f),
                Segment(p, new Vector2(-0.06f, -0.08f), new Vector2(-0.28f, -0.38f), w * 1.1f), // back leg
                Segment(p, new Vector2(-0.28f, -0.38f), new Vector2(-0.62f, -0.44f), w * 1.1f));
        }

        private static float ForkKnifeShape(Vector2 p)
        {
            float fork = Union(
                Segment(p, new Vector2(-0.3f, 0.05f), new Vector2(-0.3f, -0.72f), 0.08f),
                Box(p, new Vector2(-0.3f, 0.2f), new Vector2(0.2f, 0.12f), 0.08f),
                Segment(p, new Vector2(-0.46f, 0.2f), new Vector2(-0.46f, 0.7f), 0.045f),
                Segment(p, new Vector2(-0.3f, 0.2f), new Vector2(-0.3f, 0.7f), 0.045f),
                Segment(p, new Vector2(-0.14f, 0.2f), new Vector2(-0.14f, 0.7f), 0.045f));
            float blade = Union(
                Segment(p, new Vector2(0.32f, -0.05f), new Vector2(0.32f, -0.72f), 0.08f),
                Mathf.Max(Circle(p, new Vector2(0.18f, 0.25f), 0.45f), -(p.x - 0.22f), p.y - 0.72f, -(p.y + 0.06f)));
            return Union(fork, blade);
        }

        private static float DropShape(Vector2 p)
        {
            p.y += 0.12f;
            float body = Circle(p, Vector2.zero, 0.5f);
            // Cone towards the tip at (0, 0.85).
            var q = new Vector2(Mathf.Abs(p.x), p.y);
            float cone = Vector2.Dot(q - new Vector2(0f, 0.85f), new Vector2(0.86f, 0.51f).normalized * 1f);
            float coneCap = Mathf.Max(cone, -p.y);
            return Union(body, coneCap);
        }

        private static float MoonShape(Vector2 p)
        {
            float moon = Mathf.Max(Circle(p, new Vector2(-0.08f, -0.04f), 0.66f), -Circle(p, new Vector2(0.28f, 0.24f), 0.52f));
            var s = new Vector2(Mathf.Abs(p.x - 0.52f), Mathf.Abs(p.y - 0.5f));
            float star = Mathf.Min(s.x + s.y * 3f - 0.17f, s.y + s.x * 3f - 0.17f);
            return Mathf.Min(moon, star);
        }

        private static float PinShape(Vector2 p)
        {
            float head = Circle(p, new Vector2(0f, 0.22f), 0.5f);
            var q = new Vector2(Mathf.Abs(p.x), p.y);
            float tail = Mathf.Max(Vector2.Dot(q - new Vector2(0f, -0.82f), new Vector2(0.9f, -0.44f).normalized), p.y - 0.22f);
            float hole = Circle(p, new Vector2(0f, 0.22f), 0.2f);
            return Mathf.Max(Union(head, tail), -hole);
        }

        private static float WalletShape(Vector2 p)
        {
            float body = Box(p, new Vector2(-0.04f, -0.08f), new Vector2(0.72f, 0.52f), 0.14f);
            float flap = Box(p, new Vector2(-0.1f, 0.5f), new Vector2(0.56f, 0.12f), 0.08f);
            float clasp = Box(p, new Vector2(0.56f, -0.08f), new Vector2(0.24f, 0.18f), 0.08f);
            float dot = Circle(p, new Vector2(0.52f, -0.08f), 0.07f);
            float outline = Mathf.Max(body, -Box(p, new Vector2(-0.04f, -0.08f), new Vector2(0.6f, 0.4f), 0.06f));
            return Union(Mathf.Max(Union(outline, flap, clasp), -dot));
        }
    }
}
