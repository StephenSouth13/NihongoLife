using System;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Three small icons that open and close the parts of the HUD: character card, task tracker, [F] hint. On a phone the HUD
    /// took most of the picture, so each part can be put away and brought back with one tap. Top right on a computer, beside
    /// the shortcuts icon on a touch screen, where the corners belong to the thumbs.
    /// </summary>
    public sealed class HudToggleBar : MonoBehaviour
    {
        private const float Size = 40f, Gap = 6f;

        private RectTransform _bar;
        private Image _status, _quest, _prompt;
        private bool _touch;

        public static HudToggleBar Build(RectTransform parent)
        {
            var go = new GameObject("HudToggleBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var bar = go.AddComponent<HudToggleBar>();
            bar._bar = (RectTransform)go.transform;
            bar._status = bar.Toggle("ToggleStatus", "person", 0, () => HudPrefs.StatusHidden = !HudPrefs.StatusHidden);
            bar._quest = bar.Toggle("ToggleQuest", "checklist", 1, () => HudPrefs.QuestHidden = !HudPrefs.QuestHidden);
            bar._prompt = bar.Toggle("TogglePrompt", "info", 2, () => HudPrefs.PromptHidden = !HudPrefs.PromptHidden);
            HudPrefs.Changed += bar.Tint;
            bar.Tint();
            bar.Place();
            return bar;
        }

        private void OnDestroy() => HudPrefs.Changed -= Tint;

        private void LateUpdate()
        {
            if (HudCanvasFitter.IsTouchLayout(false) != _touch) Place();
        }

        private Image Toggle(string name, string icon, int index, Action click)
        {
            var disc = UiKit.Image(transform, name, UiTheme.Raised, UiKit.Circle, true);
            var rect = disc.rectTransform;
            rect.sizeDelta = new Vector2(Size, Size);
            var glyph = UiKit.Icon(rect, icon, UiTheme.Text, 24f);
            UiKit.Pin(glyph.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 24f, 24f);
            glyph.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            button.onClick.AddListener(() => click());
            return disc;
        }

        /// <summary>A part that is shown has a bright ring of colour; a hidden one is dim, so the state reads at a glance.</summary>
        private void Tint()
        {
            Paint(_status, !HudPrefs.StatusHidden);
            Paint(_quest, !HudPrefs.QuestHidden);
            Paint(_prompt, !HudPrefs.PromptHidden);
        }

        private static void Paint(Image disc, bool shown)
        {
            disc.color = shown ? new Color(0.13f, 0.32f, 0.42f, 0.95f) : new Color(0.08f, 0.09f, 0.11f, 0.7f);
        }

        private void Place()
        {
            _touch = HudCanvasFitter.IsTouchLayout(false);
            var anchor = _touch ? new Vector2(0.5f, 1f) : new Vector2(1f, 1f);
            _bar.anchorMin = _bar.anchorMax = _bar.pivot = anchor;
            _bar.anchoredPosition = _touch ? new Vector2(186f, -12f) : new Vector2(-24f, -110f);
            float width = _touch ? Size * 3f + Gap * 2f : Size, height = _touch ? Size : Size * 3f + Gap * 2f;
            _bar.sizeDelta = new Vector2(width, height);
            var discs = new[] { _status, _quest, _prompt };
            for (int i = 0; i < discs.Length; i++)
            {
                var rect = discs[i].rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = _touch ? new Vector2(i * (Size + Gap), 0f) : new Vector2(0f, -i * (Size + Gap));
            }
        }
    }
}
