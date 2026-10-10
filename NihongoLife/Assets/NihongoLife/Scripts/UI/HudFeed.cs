using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The one place for short notifications (priority 5 in the HUD policy): rewards, station announcements,
    /// microphone status, job progress. Top centre, under the zone bar, at most three cards, each fading after a
    /// few seconds — never over the interaction prompt (bottom centre) or the windows (centre / right).
    /// </summary>
    public sealed class HudFeed : MonoBehaviour
    {
        public enum Kind { Info, Reward, Warning }

        private static HudFeed _instance;
        private RectTransform _stack;
        private TMP_FontAsset _font;
        private readonly List<(RectTransform card, CanvasGroup group, float until)> _cards = new();
        private string _lastText;
        private float _lastAt;

        private static HudFeed Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("HudFeedCanvas", 140);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<HudFeed>();
                _instance._font = NLUi.ResolveFont();
                _instance._stack = NLUi.Group(canvas.transform, "Feed", true, 8f, TextAnchor.UpperCenter, false);
                NLUi.Anchor(_instance._stack, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(640f, 0f));
                _instance._stack.pivot = new Vector2(0.5f, 1f);
                NLUi.FitContent(_instance._stack);
                return _instance;
            }
        }

        public static void Post(string text, Kind kind = Kind.Info, float seconds = 4.5f)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var feed = Instance;
            // The same message posted again within a moment just refreshes the existing card.
            if (text == feed._lastText && Time.unscaledTime - feed._lastAt < 2f && feed._cards.Count > 0)
            {
                var last = feed._cards[feed._cards.Count - 1];
                feed._cards[feed._cards.Count - 1] = (last.card, last.group, Time.unscaledTime + seconds);
                return;
            }
            feed._lastText = text; feed._lastAt = Time.unscaledTime;
            Color accent = kind == Kind.Reward ? NLUi.Gold : kind == Kind.Warning ? new Color(0.95f, 0.42f, 0.36f) : new Color(0.45f, 0.75f, 0.95f);
            var card = NLUi.Panel(feed._stack, "FeedCard", new Color(0.05f, 0.07f, 0.09f, 0.94f), new RectOffset(18, 18, 10, 11), 0f, vertical: false);
            ((HorizontalLayoutGroup)card.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.FitContent(card, width: true, height: true);
            var dot = NLUi.Label(card, "Dot", kind == Kind.Reward ? "¥" : kind == Kind.Warning ? "!" : "●", 17f, accent, feed._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Size(dot, 24f);
            var label = NLUi.Label(card, "Text", text, 18f, NLUi.Text, feed._font);
            label.textWrappingMode = TextWrappingModes.Normal;
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = Mathf.Min(560f, label.GetPreferredValues(text, 2000f, 40f).x + 4f);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            feed._cards.Add((card, group, Time.unscaledTime + seconds));
            while (feed._cards.Count > 3) { Destroy(feed._cards[0].card.gameObject); feed._cards.RemoveAt(0); }
        }

        public static int VisibleCount => _instance == null ? 0 : _instance._cards.Count;
        public static string LatestText => _instance == null || _instance._cards.Count == 0 ? null : _instance._lastText;

        private void Update()
        {
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                var (card, group, until) = _cards[i];
                if (card == null) { _cards.RemoveAt(i); continue; }
                float left = until - Time.unscaledTime;
                group.alpha = Mathf.Clamp01(left / 0.4f);
                if (left <= 0f) { Destroy(card.gameObject); _cards.RemoveAt(i); }
            }
            // Full-screen activities (exam, mini-game) hide the feed.
            if (_stack != null) _stack.gameObject.SetActive(!UiModalStack.ImmersiveOpen);
        }
    }
}
