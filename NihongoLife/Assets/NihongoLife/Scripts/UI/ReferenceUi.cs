using System.Collections.Generic;
using System.Text;
using NihongoLife.Exam.Ielts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Shared pieces of the reference popups (model answers, self-assessment, Russian path).</summary>
    public static class ReferenceUi
    {
        public const string HostName = "ReferenceCanvas";

        /// <summary>
        /// The GameObject the reference popups live on: an overlay canvas above the IELTS test screen (sorting 800 &gt; 760).
        /// Created on demand, never DontDestroyOnLoad, so it unloads with the scene.
        /// </summary>
        public static GameObject Host()
        {
            var existing = GameObject.Find(HostName);
            if (existing != null) return existing;
            return NLUi.CreateCanvas(HostName, 800).gameObject;
        }

        public static T Open<T>(GameObject host = null) where T : MenuPopupBase
        {
            host ??= Host();
            var popup = host.GetComponent<T>();
            if (popup == null) popup = host.AddComponent<T>();
            popup.Initialize(UiKit.Font);
            return popup;
        }

        public static RectTransform Row(Transform parent, string name, float spacing = 10f, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var row = UiKit.Rect(parent, name);
            UiKit.RowLayout(row, spacing, null, align);
            return row;
        }

        public static RectTransform Panel(Transform parent, string name, Color color, int pad = 18, float spacing = 10f) =>
            NLUi.Panel(parent, name, color, new RectOffset(pad, pad, pad - 4, pad - 4), spacing);

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, FontStyles style = FontStyles.Normal) =>
            UiKit.Label(parent, name, text, size, color, style, TextAlignmentOptions.TopLeft, true);

        public static string Band(float band) => band.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// Rich text of a sample with its highlights marked (green = strength, red = issue), each wrapped in a TMP link
        /// "h{index}" for the hover tooltip and followed by a superscript number matching the notes list.
        /// </summary>
        public static string Highlight(string text, IList<IeltsModelHighlight> highlights)
        {
            text ??= "";
            var spans = new List<(int start, int end, int index)>();
            for (int i = 0; i < (highlights?.Count ?? 0); i++)
            {
                var h = highlights[i];
                if (string.IsNullOrEmpty(h?.phrase)) continue;
                int at = text.IndexOf(h.phrase, System.StringComparison.Ordinal);
                if (at < 0) continue;
                int end = at + h.phrase.Length;
                bool overlaps = spans.Exists(s => at < s.end && end > s.start);
                if (!overlaps) spans.Add((at, end, i));
            }
            spans.Sort((a, b) => a.start.CompareTo(b.start));
            var sb = new StringBuilder();
            int pos = 0;
            foreach (var s in spans)
            {
                sb.Append(Safe(text.Substring(pos, s.start - pos)));
                var h = highlights[s.index];
                string mark = h.Good ? "#3FC78555" : "#EB5C5255";
                string ink = h.Good ? "#7FE0AE" : "#FF9A8F";
                sb.Append($"<link=\"h{s.index}\"><mark={mark}><color={ink}>").Append(Safe(text.Substring(s.start, s.end - s.start)))
                  .Append($"</color></mark></link><sup><color={ink}>{s.index + 1}</color></sup>");
                pos = s.end;
            }
            sb.Append(Safe(text.Substring(pos)));
            return sb.ToString();
        }

        private static string Safe(string s) => s.Replace("<", "\u2039").Replace(">", "\u203A");
    }

    /// <summary>
    /// Hover (or tap) a highlighted phrase in a model answer to see the examiner note in a floating tooltip.
    /// The text needs raycastTarget on; links are "h{index}" into <see cref="Notes"/>.
    /// </summary>
    public sealed class HighlightTooltip : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
    {
        public TextMeshProUGUI Target;
        public List<string> Notes = new();
        public RectTransform Bubble;
        public TextMeshProUGUI BubbleText;
        public int Shown { get; private set; } = -1;

        public static HighlightTooltip Attach(TextMeshProUGUI text, RectTransform tooltipLayer, List<string> notes)
        {
            text.raycastTarget = true;
            var tip = text.gameObject.AddComponent<HighlightTooltip>();
            tip.Target = text;
            tip.Notes = notes ?? new List<string>();
            var card = UiKit.Card(tooltipLayer, "Tooltip", new Color(0.05f, 0.07f, 0.1f, 0.97f), 12, 0.4f, false);
            tip.Bubble = card.rectTransform;
            tip.Bubble.anchorMin = tip.Bubble.anchorMax = new Vector2(0.5f, 0.5f);
            tip.Bubble.pivot = new Vector2(0f, 1f);
            tip.Bubble.sizeDelta = new Vector2(380f, 110f);
            tip.BubbleText = UiKit.Label(card.transform, "Note", "", 15f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Fill(tip.BubbleText.rectTransform, 14f, 10f, 14f, 10f);
            card.gameObject.SetActive(false);
            return tip;
        }

        /// <summary>Shows note <paramref name="index"/> (or hides with -1). Public for tests and taps.</summary>
        public void Show(int index, Vector2 screen)
        {
            if (Bubble == null) return;
            if (index < 0 || index >= Notes.Count) { Bubble.gameObject.SetActive(false); Shown = -1; return; }
            Shown = index;
            BubbleText.text = $"<b>{index + 1}.</b> {Notes[index]}";
            BubbleText.ForceMeshUpdate();
            Bubble.sizeDelta = new Vector2(380f, Mathf.Max(56f, BubbleText.preferredHeight + 24f));
            var parent = (RectTransform)Bubble.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
                Bubble.anchoredPosition = local + new Vector2(14f, -14f);
            Bubble.gameObject.SetActive(true);
            Bubble.SetAsLastSibling();
        }

        private int LinkAt(Vector2 screen)
        {
            if (Target == null) return -1;
            int link = TMP_TextUtilities.FindIntersectingLink(Target, screen, null);
            if (link < 0) return -1;
            string id = Target.textInfo.linkInfo[link].GetLinkID();
            return id.Length > 1 && id[0] == 'h' && int.TryParse(id.Substring(1), out int i) ? i : -1;
        }

        public void OnPointerMove(PointerEventData e) => Show(LinkAt(e.position), e.position);
        public void OnPointerExit(PointerEventData e) => Show(-1, e.position);
        public void OnPointerClick(PointerEventData e) => Show(LinkAt(e.position), e.position);

        private void OnDisable() { if (Bubble != null) Bubble.gameObject.SetActive(false); }
    }
}
