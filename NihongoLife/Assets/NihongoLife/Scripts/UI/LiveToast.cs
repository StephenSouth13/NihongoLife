using System;
using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Real-time announcements (a teacher accepted the class, a student asked to join, the examiner sent a result…):
    /// cards that slide in at the top right above every screen — exams included — with a coloured stripe, an icon,
    /// title, message, an optional action button and a bar counting down to auto-dismiss. Up to four stack; the
    /// newest is on top. Every toast is also kept in the HUD bell for later.
    /// Texts are given in vi / en / ja and follow a language switch while shown.
    /// </summary>
    public sealed class LiveToast : MonoBehaviour
    {
        public enum Tone { Info, Success, Warning, Live, Error }

        private const float Width = 420f;
        private const int MaxShown = 4;
        private static LiveToast _instance;

        private RectTransform _stack;
        private readonly List<Entry> _entries = new List<Entry>();

        private sealed class Entry
        {
            public string Key;
            public RectTransform Rect;
            public CanvasGroup Group;
            public RectTransform Timer;
            public float Born, Life, Y, TargetY;
            public bool Leaving;
        }

        /// <summary>All toasts shown so far in this session (newest last) — tests and the bell read it.</summary>
        public static readonly List<string> History = new List<string>();
        public static int VisibleCount => _instance == null ? 0 : _instance._entries.Count;

        private static LiveToast Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("LiveToastCanvas", 980);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<LiveToast>();
                _instance._stack = UiKit.Rect(canvas.transform, "Stack");
                UiKit.Pin(_instance._stack, new Vector2(1f, 1f), -24f, -96f, Width, 600f);
                return _instance;
            }
        }

        /// <summary>Shows a toast. <paramref name="key"/> replaces an earlier toast with the same key (e.g. one per room).</summary>
        public static void Show(Tone tone, string icon, string titleVi, string titleEn, string titleJa, string bodyVi, string bodyEn, string bodyJa,
            string actionVi = null, string actionEn = null, string actionJa = null, Action action = null, float seconds = 7f, string key = null)
        {
            var self = Instance;
            if (!string.IsNullOrEmpty(key)) self.Dismiss(key, instant: true);
            History.Add(Loc.T(titleVi, titleEn, titleJa) + " — " + Loc.T(bodyVi, bodyEn, bodyJa));
            if (History.Count > 100) History.RemoveAt(0);
            self.Build(tone, icon, titleVi, titleEn, titleJa, bodyVi, bodyEn, bodyJa, actionVi, actionEn, actionJa, action, seconds, key);
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(tone == Tone.Error ? GameAudioCue.UiError : GameAudioCue.UiConfirm, 0.75f);
            HudNotificationTray.Instance?.Post("live-" + (key ?? Guid.NewGuid().ToString("N")), Loc.T(titleVi, titleEn, titleJa), Loc.T(bodyVi, bodyEn, bodyJa), action,
                tone == Tone.Error || tone == Tone.Warning ? HudNoticeTone.Warning : HudNoticeTone.Info, false, 60f);
        }

        public static void Hide(string key) { if (_instance != null) _instance.Dismiss(key, instant: false); }

        public static Color ToneColor(Tone tone) => tone switch
        {
            Tone.Success => UiTheme.Good,
            Tone.Warning => UiTheme.Warn,
            Tone.Live => UiTheme.Live,
            Tone.Error => UiTheme.Bad,
            _ => UiTheme.Ielts,
        };

        private void Build(Tone tone, string icon, string tVi, string tEn, string tJa, string bVi, string bEn, string bJa,
            string aVi, string aEn, string aJa, Action action, float seconds, string key)
        {
            Color accent = ToneColor(tone);
            bool hasAction = action != null && (aVi != null || aEn != null);
            float height = hasAction ? 132f : 92f;

            var card = UiKit.Card(_stack, "Toast", new Color(0.085f, 0.11f, 0.155f, 0.98f), 16, 0f, true);
            var rect = card.rectTransform;
            UiKit.Pin(rect, new Vector2(1f, 1f), 0f, 0f, Width, height);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            UiKit.Outline(rect, UiKit.Alpha(accent, 0.35f), 1f);

            var stripe = UiKit.Image(rect, "Stripe", accent, UiKit.Rounded(4));
            UiKit.Place(stripe.rectTransform, 0f, 0f, 6f, height);

            var disc = UiKit.Image(rect, "IconDisc", UiKit.Alpha(accent, 0.18f), UiKit.Circle);
            UiKit.Place(disc.rectTransform, 20f, 18f, 44f, 44f);
            var ic = UiKit.Icon(disc.transform, string.IsNullOrEmpty(icon) ? "notifications" : icon, accent, 24f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 24f, 24f);

            var title = UiKit.Label(rect, "Title", tVi, tEn, tJa, 16.5f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 78f, 14f, Width - 120f, 24f);
            var body = UiKit.Label(rect, "Body", bVi, bEn, bJa, 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(body.rectTransform, 78f, 40f, Width - 100f, 44f);
            body.overflowMode = TextOverflowModes.Ellipsis;

            var close = UiKit.IconButton(rect, "Close", "close", null, UiKit.Kind.Ghost, 28f, 15f);
            UiKit.Pin((RectTransform)close.transform, new Vector2(1f, 1f), -10f, -10f, 28f, 28f);

            if (hasAction)
            {
                var act = UiKit.Button(rect, "Action", aVi, aEn, aJa, null, tone == Tone.Error ? UiKit.Kind.Secondary : UiKit.Kind.Primary, "arrow_forward", 14.5f, 10);
                UiKit.Place((RectTransform)act.transform, 78f, 88f, 190f, 34f);
                act.onClick.AddListener(() => { try { action(); } catch (Exception e) { Debug.LogException(e); } Dismiss(key ?? rect.GetInstanceID().ToString(), false); });
            }

            var timerTrack = UiKit.Image(rect, "TimerTrack", new Color(1f, 1f, 1f, 0.05f));
            UiKit.Strip(timerTrack.rectTransform, false, 0f, 3f, 8f, 8f);
            var timer = UiKit.Image(timerTrack.transform, "Timer", UiKit.Alpha(accent, 0.8f)).rectTransform;
            timer.anchorMin = Vector2.zero; timer.anchorMax = Vector2.one; timer.offsetMin = timer.offsetMax = Vector2.zero;

            var entry = new Entry { Key = key ?? rect.GetInstanceID().ToString(), Rect = rect, Group = group, Timer = timer, Born = Time.unscaledTime, Life = Mathf.Max(2f, seconds), Y = 0f };
            close.onClick.AddListener(() => Dismiss(entry.Key, false));
            _entries.Insert(0, entry);
            while (_entries.Count > MaxShown) Dismiss(_entries[_entries.Count - 1].Key, instant: true);
            Relayout();
            entry.Y = entry.TargetY;
            rect.anchoredPosition = new Vector2(Width + 40f, -entry.Y);
        }

        private void Relayout()
        {
            float y = 0f;
            foreach (var e in _entries)
            {
                if (e.Leaving) continue;
                e.TargetY = y;
                y += e.Rect.sizeDelta.y + 12f;
            }
        }

        private void Dismiss(string key, bool instant)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.Key != key) continue;
                if (instant || e.Rect == null) { if (e.Rect != null) Destroy(e.Rect.gameObject); _entries.RemoveAt(i); }
                else { e.Leaving = true; e.Born = Time.unscaledTime; }
            }
            Relayout();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.Rect == null) { _entries.RemoveAt(i); continue; }
                if (e.Leaving)
                {
                    float k = Mathf.Clamp01((now - e.Born) / 0.25f);
                    e.Group.alpha = 1f - k;
                    e.Rect.anchoredPosition = new Vector2(Mathf.Lerp(0f, 60f, k), e.Rect.anchoredPosition.y);
                    if (k >= 1f) { Destroy(e.Rect.gameObject); _entries.RemoveAt(i); Relayout(); }
                    continue;
                }
                float age = now - e.Born;
                // Slide in with a soft overshoot, then settle; follow the stack position.
                float t = Mathf.Clamp01(age / 0.35f);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                e.Y = Mathf.Lerp(e.Y, e.TargetY, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                e.Rect.anchoredPosition = new Vector2(Mathf.Lerp(Width + 40f, 0f, ease), -e.Y);
                e.Group.alpha = Mathf.Clamp01(age / 0.2f);
                e.Timer.anchorMax = new Vector2(Mathf.Clamp01(1f - age / e.Life), 1f);
                if (age >= e.Life) { e.Leaving = true; e.Born = now; }
            }
        }
    }
}
