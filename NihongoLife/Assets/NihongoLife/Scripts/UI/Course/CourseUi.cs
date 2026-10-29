using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Course;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Small layout helpers shared by the school/tenses screens (top-left coordinates, measured paragraphs, scrolling bodies).</summary>
    public static class CourseUi
    {
        public static string Stars(int n) => new string('★', Mathf.Clamp(n, 0, 3)) + new string('☆', 3 - Mathf.Clamp(n, 0, 3));

        /// <summary>A wrapping label placed at (x,y) with the width given; its height is measured and returned.</summary>
        public static TextMeshProUGUI Para(Transform parent, string name, string text, float size, Color color, float x, float y, float w, out float h,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = UiKit.Label(parent, name, text ?? "", size, color, style, align, true);
            UiKit.Place(t.rectTransform, x, y, w, 10f);
            h = Mathf.Ceil(t.GetPreferredValues(t.text, w, 0f).y) + 2f;
            t.rectTransform.sizeDelta = new Vector2(w, h);
            return t;
        }

        /// <summary>A scrolling area filling <paramref name="host"/>; returns the content column.</summary>
        public static RectTransform ScrollIn(RectTransform host, float spacing = 0f, RectOffset padding = null)
        {
            var content = UiKit.Scroll(host, "Scroll", out _, spacing, padding ?? new RectOffset(0, 0, 0, 0));
            UiKit.Fill((RectTransform)content.parent.parent);
            return content;
        }

        /// <summary>A free-layout body inside a scroll content (children are placed by coordinates); call <see cref="EndBody"/> with the final height.</summary>
        public static RectTransform Body(RectTransform scrollContent) => UiKit.Rect(scrollContent, "Body");

        public static void EndBody(RectTransform body, float height) => UiKit.Size(body, -1f, Mathf.Max(8f, height));

        public static void SpeakButton(Transform parent, string text, string locale, float x, float y)
        {
            if (!PatternSpeech.Available || string.IsNullOrEmpty(text)) return;
            var speak = UiKit.IconButton(parent, "Speak", "volume_up", () => PatternSpeech.Speak(text, locale), UiKit.Kind.Quiet, 40f, 22f);
            UiKit.Place((RectTransform)speak.transform, x, y, 40f, 40f);
        }

        public static string LocaleOf(string target) => target == "ja" ? "ja-JP" : target == "en" ? "en-US" : target == "ru" ? "ru-RU" : "vi-VN";

        /// <summary>Deterministic shuffle (the same question always shows the same order).</summary>
        public static List<T> Shuffled<T>(IList<T> source, string seed)
        {
            var list = new List<T>(source);
            unchecked
            {
                int h = 17;
                foreach (char c in seed ?? "") h = h * 31 + c;
                var rng = new System.Random(h);
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
            return list;
        }

        public static Color LevelColor(string cefr)
        {
            switch (cefr)
            {
                case "A1": return UiTheme.Good;
                case "A2": return UiTheme.Teal;
                case "B1": return UiTheme.Ielts;
                case "B2": return UiTheme.Gold;
                case "C1": return UiTheme.Jlpt;
                default: return UiTheme.Muted;
            }
        }

        public static Color TargetColor(string target)
        {
            switch (target)
            {
                case "ja": return UiTheme.Jlpt;
                case "en": return UiTheme.Ielts;
                case "ru": return UiTheme.Teal;
                default: return UiTheme.Gold;
            }
        }

        /// <summary>A clickable row card with a highlight bar when selected; returns the card.</summary>
        public static Image Row(RectTransform parent, string name, bool selected, Color accent, float height, Action click)
        {
            var row = UiKit.Card(parent, name, selected ? UiTheme.Raised : UiTheme.Surface, 14);
            UiKit.Size(row, -1f, height);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.onClick.AddListener(() => click?.Invoke());
            if (selected)
            {
                var bar = UiKit.Image(row.transform, "Sel", accent, UiKit.Rounded(3));
                UiKit.Place(bar.rectTransform, 0f, 12f, 5f, height - 24f);
            }
            return row;
        }

        public static string Mmss(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}