using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Layout check and capture for screen-space menus at any resolution without touching the window:
    /// the canvas is laid out in world space at the size the CanvasScaler would give that screen, then audited for
    /// text that spills out of its box or the screen, texts drawn over each other, and buttons stacked on each other.
    /// Used by the menu layout PlayMode test and the menu screenshot tool.
    /// </summary>
    public static class UiLayoutProbe
    {
        public const int Layer = 31;

        /// <summary>Reference size and match the shared <see cref="ResponsiveCanvasFit"/> picks for a screen.</summary>
        public static void Reference(int w, int h, bool phone, out Vector2 reference, out float match)
        {
            float aspect = (float)w / h;
            int shortSide = Mathf.Min(w, h);
            if (phone)
            {
                if (aspect < 1f) { reference = new Vector2(Mathf.Clamp(w, 360, 480), Mathf.Clamp(h, 640, 960)); match = 0f; }
                else { reference = new Vector2(Mathf.Clamp(w, 640, 960), Mathf.Clamp(h, 320, 480)); match = 1f; }
            }
            else if (aspect < 0.92f) { reference = new Vector2(720f, 1280f); match = 0f; }
            else if (aspect > 2.05f && shortSide >= 900) { reference = new Vector2(2560f, 1080f); match = 1f; }
            else if (aspect < 1.45f) { reference = new Vector2(1440f, 1080f); match = 0.5f; }
            else { reference = new Vector2(1920f, 1080f); match = 0.5f; }
        }

        /// <summary>CanvasScaler "match width or height" scale factor.</summary>
        public static float ScaleFactor(int w, int h, Vector2 reference, float match)
        {
            float logW = Mathf.Log(w / reference.x, 2f), logH = Mathf.Log(h / reference.y, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logW, logH, match));
        }

        /// <summary>Lays a canvas out as a w x h screen would (world space, drawn by <paramref name="uiCamera"/>).</summary>
        private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1f && inner.xMax <= outer.xMax + 1f && inner.yMin >= outer.yMin - 1f && inner.yMax <= outer.yMax + 1f;

        public static float Fit(Canvas canvas, Camera uiCamera, int w, int h, bool phone)
        {
            Reference(w, h, phone, out var reference, out var match);
            float scale = ScaleFactor(w, h, reference, match);
            UiScreen.Override = new Vector2Int(w, h);
            UiScreen.PhoneOverride = phone;
            UiScreen.CanvasScaleOverride = scale;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = uiCamera;
            var rt = (RectTransform)canvas.transform;
            rt.sizeDelta = new Vector2(w / scale, h / scale);
            rt.localScale = Vector3.one * scale;
            rt.position = uiCamera.transform.position + uiCamera.transform.forward * 10f;
            rt.rotation = uiCamera.transform.rotation;
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = h / 2f;
            uiCamera.aspect = (float)w / h;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            return scale;
        }

        public static void Release()
        {
            UiScreen.Override = null;
            UiScreen.PhoneOverride = null;
            UiScreen.SafeAreaOverride = null;
            UiScreen.CanvasScaleOverride = null;
        }

        /// <summary>The topmost open full-screen layer (a modal backdrop), or the root when none is open.</summary>
        public static RectTransform TopLayer(RectTransform root)
        {
            RectTransform top = null;
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(false))
            {
                if (rect == root || rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one) continue;
                var image = rect.GetComponent<Image>();
                if (image == null || !image.enabled || !image.raycastTarget || image.color.a < 0.3f) continue;
                if (!Visible(rect)) continue;
                if (rect.GetComponentInChildren<TMP_Text>(false) == null) continue;
                top = rect; // later in hierarchy order = drawn on top
            }
            return top != null ? top : root;
        }

        static bool Visible(Transform t)
        {
            foreach (var group in t.GetComponentsInParent<CanvasGroup>(false))
            {
                if (group.alpha < 0.05f) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }

        static Rect Local(RectTransform space, Vector3 a, Vector3 b)
        {
            Vector3 p = space.InverseTransformPoint(a), q = space.InverseTransformPoint(b);
            return Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.y, q.y), Mathf.Max(p.x, q.x), Mathf.Max(p.y, q.y));
        }

        static Rect Box(RectTransform space, RectTransform rect)
        {
            var c = new Vector3[4];
            rect.GetWorldCorners(c);
            return Local(space, c[0], c[2]);
        }

        static bool Clip(RectTransform space, Transform t, ref Rect r)
        {
            foreach (var mask in t.GetComponentsInParent<RectMask2D>(false))
            {
                var m = Box(space, mask.rectTransform);
                r = Rect.MinMaxRect(Mathf.Max(r.xMin, m.xMin), Mathf.Max(r.yMin, m.yMin), Mathf.Min(r.xMax, m.xMax), Mathf.Min(r.yMax, m.yMax));
            }
            foreach (var mask in t.GetComponentsInParent<Mask>(false))
            {
                if (!mask.enabled) continue;
                var m = Box(space, mask.rectTransform);
                r = Rect.MinMaxRect(Mathf.Max(r.xMin, m.xMin), Mathf.Max(r.yMin, m.yMin), Mathf.Min(r.xMax, m.xMax), Mathf.Min(r.yMax, m.yMax));
            }
            return r.width > 1f && r.height > 1f;
        }

        static float Overlap(Rect a, Rect b) =>
            Mathf.Min(Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)), Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin)));

        public static string PathOf(Transform t, Transform root)
        {
            string path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        /// <summary>Problems in the visible layout of <paramref name="layer"/> on a canvas (canvas units, 3 px tolerance).</summary>
        public static List<string> Audit(RectTransform canvasRect, RectTransform layer, float tolerance = 3f)
        {
            var issues = new List<string>();
            Canvas.ForceUpdateCanvases();
            Rect screen = canvasRect.rect;
            var texts = new List<(TMP_Text text, Rect glyphs, Transform owner)>();
            foreach (var text in layer.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.enabled || string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.05f || !Visible(text.transform)) continue;
                text.ForceMeshUpdate();
                var info = text.textInfo;
                if (info == null || info.characterCount == 0) continue;
                Bounds b = text.textBounds;
                if (b.size.x < 0.5f || b.size.y < 0.5f) continue;
                var tr = text.rectTransform;
                Rect glyphs = Local(canvasRect, tr.TransformPoint(b.min), tr.TransformPoint(b.max));
                bool clippedByMask = text.GetComponentInParent<RectMask2D>() != null || text.GetComponentInParent<Mask>() != null;
                Rect own = Box(canvasRect, tr);
                string name = PathOf(text.transform, layer);
                if (!clippedByMask && text.overflowMode == TextOverflowModes.Overflow)
                {
                    float spill = Mathf.Max(own.xMin - glyphs.xMin, glyphs.xMax - own.xMax, own.yMin - glyphs.yMin, glyphs.yMax - own.yMax);
                    if (spill > tolerance + 1f) issues.Add($"spills out of its box by {spill:0}px: {name} \"{Short(text.text)}\"");
                }
                Rect visible = glyphs;
                if (!Clip(canvasRect, text.transform, ref visible)) continue;
                if (visible.xMin < screen.xMin - tolerance || visible.xMax > screen.xMax + tolerance || visible.yMin < screen.yMin - tolerance || visible.yMax > screen.yMax + tolerance)
                    issues.Add($"off screen: {name} \"{Short(text.text)}\"");
                var owner = text.GetComponentInParent<Selectable>() != null ? text.GetComponentInParent<Selectable>().transform : text.transform.parent;
                texts.Add((text, visible, owner));
            }
            for (int i = 0; i < texts.Count; i++)
                for (int j = i + 1; j < texts.Count; j++)
                {
                    var a = texts[i]; var b = texts[j];
                    if (a.text.transform.IsChildOf(b.text.transform) || b.text.transform.IsChildOf(a.text.transform)) continue;
                    if (Overlap(a.glyphs, b.glyphs) > tolerance)
                        issues.Add($"texts overlap: {PathOf(a.text.transform, layer)} \"{Short(a.text.text)}\" x {PathOf(b.text.transform, layer)} \"{Short(b.text.text)}\"");
                }
            var buttons = new List<(Selectable s, Rect r)>();
            foreach (var s in layer.GetComponentsInChildren<Selectable>(false))
            {
                if (!s.enabled || !Visible(s.transform) || s is Scrollbar || s is Slider) continue;
                if (s.targetGraphic == null || !s.targetGraphic.enabled || s.targetGraphic.color.a < 0.02f && s.GetComponentInChildren<TMP_Text>(false) == null) continue;
                var r = Box(canvasRect, (RectTransform)s.transform);
                if (!Clip(canvasRect, s.transform, ref r)) continue;
                if (r.xMin < screen.xMin - tolerance || r.xMax > screen.xMax + tolerance || r.yMin < screen.yMin - tolerance || r.yMax > screen.yMax + tolerance)
                    issues.Add($"button off screen: {PathOf(s.transform, layer)}");
                buttons.Add((s, r));
            }
            for (int i = 0; i < buttons.Count; i++)
                for (int j = i + 1; j < buttons.Count; j++)
                {
                    var a = buttons[i]; var b = buttons[j];
                    if (a.s.transform.IsChildOf(b.s.transform) || b.s.transform.IsChildOf(a.s.transform)) continue;
                    // An overlay control sitting fully inside another (carousel arrows on a picture) is by design.
                    if (Contains(a.r, b.r) || Contains(b.r, a.r)) continue;
                    if (Overlap(a.r, b.r) > tolerance + 1f)
                        issues.Add($"buttons overlap: {PathOf(a.s.transform, layer)} x {PathOf(b.s.transform, layer)}");
                }
            return issues;
        }

        static string Short(string s)
        {
            s = System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", "").Replace("\n", " ");
            return s.Length > 40 ? s.Substring(0, 40) + "..." : s;
        }

        /// <summary>Renders the 3D camera and the UI camera (on a transparent target) and blends the UI over the 3D image.</summary>
        public static byte[] Capture(Camera world, Camera ui, int w, int h)
        {
            var bg = Read(world, w, h, false);
            var fg = Read(ui, w, h, true);
            var outPx = new Color32[w * h];
            for (int i = 0; i < outPx.Length; i++)
            {
                var f = fg[i]; var g = bg != null ? bg[i] : new Color32(12, 14, 18, 255);
                int a = f.a;
                outPx[i] = new Color32((byte)((f.r * a + g.r * (255 - a)) / 255), (byte)((f.g * a + g.g * (255 - a)) / 255), (byte)((f.b * a + g.b * (255 - a)) / 255), 255);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(outPx);
            tex.Apply();
            var png = tex.EncodeToPNG();
            Object.Destroy(tex);
            return png;
        }

        private static Color32[] Read(Camera cam, int w, int h, bool transparent)
        {
            if (cam == null) return null;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevFlags = cam.clearFlags;
            var prevColor = cam.backgroundColor;
            var prev = RenderTexture.active;
            try
            {
                if (transparent)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                var px = tex.GetPixels32();
                Object.Destroy(tex);
                return px;
            }
            finally
            {
                RenderTexture.active = prev;
                cam.targetTexture = prevTarget;
                cam.clearFlags = prevFlags;
                cam.backgroundColor = prevColor;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
