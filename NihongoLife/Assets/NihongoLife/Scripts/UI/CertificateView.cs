using System;
using System.IO;
using NihongoLife.Core;
using NihongoLife.StudyModes.Plan;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Progress bar fill that eases to its target (instant outside Play mode, so edit-mode builds/tests stay exact).</summary>
    public sealed class AnimatedBar : MonoBehaviour
    {
        public RectTransform fill;
        public float target, current, speed = 1.6f;

        public static AnimatedBar Attach(RectTransform fill, float value, bool animate = true)
        {
            var a = fill.parent.gameObject.AddComponent<AnimatedBar>();
            a.fill = fill;
            a.target = Mathf.Clamp01(value);
            a.current = animate && Application.isPlaying ? 0f : a.target;
            UiKit.SetProgress(fill, a.current);
            return a;
        }

        private void Update()
        {
            if (fill == null || Mathf.Approximately(current, target)) return;
            current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * speed * Mathf.Max(0.25f, Mathf.Abs(target - current) * 2f));
            UiKit.SetProgress(fill, current);
        }
    }

    /// <summary>
    /// The NihongoLife in-game certificate for a passed milestone: official logo (Resources/Branding, copied from
    /// Bao_Cao/Logo), player name, level, date, score and skill breakdown, and a clear "not an official
    /// JLPT/TORFL/IELTS certificate" line. Drawn at 1600�1130 and scaled; <see cref="ExportPng"/> renders it to a file.
    /// </summary>
    public static class CertificateView
    {
        public const float CW = 1600f, CH = 1130f;
        private static readonly Color Cream = new Color(0.984f, 0.965f, 0.918f), Ink = new Color(0.16f, 0.14f, 0.13f),
            GoldC = new Color(0.78f, 0.6f, 0.24f), Soft = new Color(0.4f, 0.36f, 0.32f), Red = new Color(0.62f, 0.16f, 0.14f);

        public static Texture2D Logo => Resources.Load<Texture2D>("Branding/nihongolife_logo");
        public static Texture2D Mark => Resources.Load<Texture2D>("Branding/nihongolife_mark");

        public static string TrackName(PlanTrack t) => t switch
        {
            PlanTrack.Japanese => Loc.T("Ti\u1EBFng Nh\u1EADt", "Japanese", "\u65E5\u672C\u8A9E"),
            PlanTrack.English => Loc.T("Ti\u1EBFng Anh", "English", "\u82F1\u8A9E"),
            PlanTrack.Russian => Loc.T("Ti\u1EBFng Nga", "Russian", "\u30ED\u30B7\u30A2\u8A9E"),
            _ => "IELTS",
        };

        public static string LevelName(PlanTrack t, string level) => t == PlanTrack.Ielts ? "Band " + level : level;

        public static string SkillName(string s) => s switch
        {
            "vocab" => Loc.T("T\u1EEB v\u1EF1ng", "Vocabulary", "\u8A9E\u5F59"),
            "grammar" => Loc.T("Ng\u1EEF ph\u00E1p", "Grammar", "\u6587\u6CD5"),
            "reading" => Loc.T("\u0110\u1ECDc hi\u1EC3u", "Reading", "\u8AAD\u89E3"),
            "listening" => Loc.T("Nghe", "Listening", "\u8074\u89E3"),
            _ => Loc.T("Kh\u00E1c", "Other", "\u305D\u306E\u4ED6"),
        };

        public static Color TrackColor(PlanTrack t) => t switch
        {
            PlanTrack.Japanese => UiTheme.Jlpt, PlanTrack.Russian => new Color(0.25f, 0.42f, 0.78f), PlanTrack.Ielts => UiTheme.Ielts, _ => UiTheme.Teal,
        };

        public static string DisclaimerText => Loc.T(
            "Ch\u1EE9ng nh\u1EADn trong game NihongoLife \u2014 kh\u00F4ng ph\u1EA3i ch\u1EE9ng ch\u1EC9 JLPT / TORFL / IELTS ch\u00EDnh th\u1EE9c.",
            "NihongoLife in-game certificate \u2014 not an official JLPT / TORFL / IELTS certificate.",
            "NihongoLife\u30B2\u30FC\u30E0\u5185\u4FEE\u4E86\u8A3C \u2014 JLPT / TORFL / IELTS\u306E\u516C\u5F0F\u8A3C\u660E\u66F8\u3067\u306F\u3042\u308A\u307E\u305B\u3093\u3002");

        /// <summary>Builds the certificate (CW�CH, top-left anchored) under <paramref name="parent"/>.</summary>
        public static RectTransform Build(Transform parent, MilestoneRecord r)
        {
            var root = UiKit.Rect(parent, "Certificate");
            UiKit.Place(root, 0f, 0f, CW, CH);
            var outer = UiKit.Image(root, "Border", GoldC); UiKit.Place(outer.rectTransform, 0f, 0f, CW, CH);
            var paper = UiKit.Image(root, "Paper", Cream); UiKit.Place(paper.rectTransform, 16f, 16f, CW - 32f, CH - 32f);
            var line = UiKit.Image(root, "Line", GoldC); UiKit.Place(line.rectTransform, 40f, 40f, CW - 80f, CH - 80f);
            var inner = UiKit.Image(root, "Inner", Cream); UiKit.Place(inner.rectTransform, 44f, 44f, CW - 88f, CH - 88f);
            var band = UiKit.Image(root, "Band", UiKit.Alpha(TrackColor(r.track), 0.85f)); UiKit.Place(band.rectTransform, 44f, 44f, CW - 88f, 14f);

            Raw(root, "Logo", Logo, CW / 2f - 300f, 80f, 600f, 167f);
            Text(root, "Kind", "NIHONGOLIFE \u00B7 " + Loc.T("CH\u1EE8NG NH\u1EACN TRONG GAME", "IN-GAME CERTIFICATE", "\u30B2\u30FC\u30E0\u5185\u4FEE\u4E86\u8A3C"), 24f, GoldC, FontStyles.Bold, 262f, 40f);
            Text(root, "Title", Loc.T("Ch\u1EE9ng nh\u1EADn ho\u00E0n th\u00E0nh c\u1ED9t m\u1ED1c", "Certificate of Milestone Achievement", "\u30DE\u30A4\u30EB\u30B9\u30C8\u30FC\u30F3\u9054\u6210\u8A3C\u660E"), 58f, Ink, FontStyles.Bold, 312f, 80f);
            Text(root, "Certifies", Loc.T("Ch\u1EE9ng nh\u1EADn r\u1EB1ng", "This certifies that", "\u4EE5\u4E0B\u306E\u8005\u306F"), 26f, Soft, FontStyles.Italic, 408f, 40f);
            Text(root, "Name", string.IsNullOrWhiteSpace(r.playerName) ? "Player" : r.playerName, 64f, Ink, FontStyles.Bold, 452f, 86f);
            var under = UiKit.Image(root, "NameLine", UiKit.Alpha(GoldC, 0.7f)); UiKit.Place(under.rectTransform, CW / 2f - 360f, 544f, 720f, 3f);
            Text(root, "Passed", Loc.F("\u0111\u00E3 v\u01B0\u1EE3t qua b\u00E0i ki\u1EC3m tra c\u1ED9t m\u1ED1c {0} \u00B7 tr\u00ECnh \u0111\u1ED9 {1}", "has passed the {0} milestone checkpoint \u00B7 level {1}", "{0}\u30DE\u30A4\u30EB\u30B9\u30C8\u30FC\u30F3\u30C6\u30B9\u30C8\u306B\u5408\u683C \u00B7 \u30EC\u30D9\u30EB{1}", TrackName(r.track), LevelName(r.track, r.levelId)), 30f, Ink, FontStyles.Normal, 566f, 46f);

            var lvl = UiKit.Image(root, "LevelSeal", TrackColor(r.track), UiKit.Rounded(60));
            UiKit.Place(lvl.rectTransform, CW / 2f - 120f, 630f, 240f, 110f);
            var lt = UiKit.Label(lvl.transform, "Level", LevelName(r.track, r.levelId), 52f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(lt, 0f, 0f, 240f, 110f);

            Text(root, "Score", Loc.F("\u0110i\u1EC3m: {0}/{1} \u00B7 {2}%", "Score: {0}/{1} \u00B7 {2}%", "\u5F97\u70B9:{0}/{1} \u00B7 {2}%", r.correct, r.total, r.Percent), 34f, Ink, FontStyles.Bold, 760f, 48f);
            int n = r.skills.Count;
            float cw = 250f, gap = 18f, x0 = CW / 2f - (n * cw + (n - 1) * gap) / 2f;
            for (int i = 0; i < n; i++)
            {
                var s = r.skills[i];
                var chip = UiKit.Image(root, "Skill_" + s.skill, UiKit.Alpha(TrackColor(r.track), 0.14f), UiKit.Rounded(16));
                UiKit.Place(chip.rectTransform, x0 + i * (cw + gap), 818f, cw, 52f);
                var st = UiKit.Label(chip.transform, "Text", SkillName(s.skill) + "  " + s.correct + "/" + s.total, 22f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(st, 0f, 0f, cw, 52f);
            }

            var date = UiKit.Label(root, "Date", Loc.F("Ng\u00E0y c\u1EA5p: {0:dd/MM/yyyy}", "Issued: {0:dd/MM/yyyy}", "\u767A\u884C\u65E5:{0:yyyy/MM/dd}", r.UtcDate.ToLocalTime()), 24f, Soft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            UiKit.Place(date, 110f, 910f, 600f, 36f);
            var serial = UiKit.Label(root, "Serial", Loc.F("M\u00E3: {0}", "No. {0}", "\u756A\u53F7:{0}", r.Serial), 22f, Soft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            UiKit.Place(serial, 110f, 950f, 600f, 34f);
            Raw(root, "Seal", Mark, CW - 300f, 870f, 170f, 170f);
            Text(root, "Disclaimer", DisclaimerText, 21f, Red, FontStyles.Italic, 1010f, 40f);
            return root;
        }

        private static void Text(RectTransform root, string name, string text, float size, Color c, FontStyles style, float y, float h)
        {
            var l = UiKit.Label(root, name, text, size, c, style, TextAlignmentOptions.Center);
            UiKit.Place(l, 90f, y, CW - 180f, h);
        }

        private static void Raw(RectTransform root, string name, Texture2D tex, float x, float y, float w, float h)
        {
            // fit the texture inside the box, centred, keeping its aspect (no AspectRatioFitter: it sizes to the parent)
            if (tex != null)
            {
                float a = (float)tex.width / tex.height;
                float fw = Mathf.Min(w, h * a), fh = fw / a;
                x += (w - fw) / 2f; y += (h - fh) / 2f; w = fw; h = fh;
            }
            var rt = UiKit.Rect(root, name);
            UiKit.Place(rt, x, y, w, h);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex; img.raycastTarget = false;
            if (tex == null) img.color = Color.clear;
        }

        public static string ExportFolder => Path.Combine(Application.persistentDataPath, "certificates");
        public static string FileNameFor(MilestoneRecord r) => r.Serial + ".png";

        /// <summary>Renders the certificate off-screen and writes a PNG. Returns the path, or null if the device cannot render.</summary>
        public static string ExportPng(MilestoneRecord r, string folder = null, int width = 1600)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return null;
            folder ??= ExportFolder;
            Directory.CreateDirectory(folder);
            int height = Mathf.RoundToInt(width * CH / CW);
            const int layer = 31;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var camGo = new GameObject("CertCam");
            var canvasGo = new GameObject("CertCanvas", typeof(Canvas), typeof(CanvasScaler));
            Texture2D tex = null;
            try
            {
                camGo.transform.position = new Vector3(0f, -5000f, -10f);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false; cam.cullingMask = 1 << layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Cream;
                cam.orthographic = true; cam.orthographicSize = CH / 2f; cam.aspect = CW / CH; cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f; cam.targetTexture = rt;
                // World-space canvas: independent of the (batch-mode) screen size, 1 unit = 1 certificate px
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = cam;
                var crt = (RectTransform)canvasGo.transform;
                crt.position = new Vector3(0f, -5000f, 0f); crt.sizeDelta = new Vector2(CW, CH); crt.localScale = Vector3.one;
                canvasGo.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = (float)width / CW;
                var holder = UiKit.Rect(canvasGo.transform, "Holder");
                holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = new Vector2(CW, CH); holder.anchoredPosition = Vector2.zero;
                Build(holder, r);
                foreach (var t in canvasGo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                string path = Path.Combine(folder, FileNameFor(r));
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return path;
            }
            catch (Exception e) { Debug.LogWarning("[Certificate] export failed: " + e.Message); return null; }
            finally
            {
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(canvasGo);
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
