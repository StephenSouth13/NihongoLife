using System;
using System.Collections;
using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Work that takes time (digging, planting, watering, restocking, scanning…): a progress card in the prompt lane
    /// with the action, the tool used and the time left. Only one action runs at a time, so repeated clicks or key
    /// presses cannot stack or double-count. The player stands still while working; Esc cancels (nothing happens).
    /// </summary>
    public sealed class TimedAction : MonoBehaviour
    {
        private static TimedAction _instance;
        private RectTransform _card;
        private TextMeshProUGUI _label, _detail;
        private Image _fill;
        private Coroutine _running;
        private Action _onCancel;
        private PlayerController _player;
        private bool _wasLocked;
        private CharacterAnimationController _animation;

        public static bool Busy => _instance != null && _instance._running != null;
        /// <summary>Test hook: multiplies every duration (0.05 in fast tests).</summary>
        public static float SpeedScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { SpeedScale = 1f; _instance = null; }

        private static TimedAction Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("TimedActionCanvas", 150);
                DontDestroyOnLoad(canvas.gameObject);
                var ui = canvas.gameObject.AddComponent<TimedAction>();
                var font = NLUi.ResolveFont();
                ui._card = NLUi.Panel(canvas.transform, "ActionCard", new Color(0.04f, 0.055f, 0.07f, 0.94f), new RectOffset(22, 22, 12, 14), 6f);
                NLUi.Anchor(ui._card, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(560f, 0f));
                ui._card.pivot = new Vector2(0.5f, 0f);
                NLUi.FitContent(ui._card);
                var top = NLUi.Group(ui._card, "Top", false, 10f, TextAnchor.MiddleLeft, false);
                ui._label = NLUi.Label(top, "Label", "", 19f, NLUi.Text, font, FontStyles.Bold);
                NLUi.Size(ui._label, flexibleWidth: 1f);
                ui._detail = NLUi.Label(top, "Detail", "", 15f, NLUi.Muted, font, FontStyles.Normal, TextAlignmentOptions.Right);
                ui._detail.textWrappingMode = TextWrappingModes.NoWrap;
                NLUi.Size(ui._detail, 150f);
                var track = new GameObject("Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
                track.SetParent(ui._card, false);
                track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
                track.GetComponent<LayoutElement>().preferredHeight = 10f;
                ui._fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ui._fill.transform.SetParent(track, false);
                ui._fill.color = NLUi.Gold;
                var r = ui._fill.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(0f, 1f); r.offsetMin = r.offsetMax = Vector2.zero;
                NLUi.Label(ui._card, "Hint", "Esc để dừng", 13f, NLUi.Muted, font);
                ui._card.gameObject.SetActive(false);
                UiModalStack.Register(ui, () => Busy, () => Cancel(), "Timed action");
                // The work belongs to the scene it started in (a plot, a shelf): leaving the scene drops it unfinished.
                SceneManager.activeSceneChanged += (_, _) => { if (_instance == ui) ui.Abort(); };
                _instance = ui;
                return ui;
            }
        }

        /// <summary>Starts an action; returns false (and does nothing) while another action is running. <paramref name="pose"/>
        /// is a work state on the player's animator (e.g. "Work_Water"); <paramref name="prop"/> a tool held in the right hand.</summary>
        public static bool Run(string label, string tool, float seconds, Action onDone, Action onCancel = null, string pose = null, GameObject prop = null)
        {
            var ui = Instance;
            if (ui._running != null) return false;
            if (SpeedScale <= 0f) { onDone?.Invoke(); return true; } // instant mode (logic-only tests)
            ui._onCancel = onCancel;
            ui._running = ui.StartCoroutine(ui.Routine(label, tool, Mathf.Max(0.05f, seconds * SpeedScale), onDone, pose, prop));
            return true;
        }

        private void Abort()
        {
            if (_running == null) return;
            StopCoroutine(_running);
            Finish();
            _onCancel = null;
        }

        public static void Cancel()
        {
            if (_instance == null || _instance._running == null) return;
            _instance.StopCoroutine(_instance._running);
            _instance.Finish();
            var cancel = _instance._onCancel;
            _instance._onCancel = null;
            HudFeed.Post("Đã dừng — chưa làm xong nên không tính.", HudFeed.Kind.Warning, 3f);
            cancel?.Invoke();
        }

        private IEnumerator Routine(string label, string tool, float seconds, Action onDone, string pose, GameObject prop)
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null) { _wasLocked = _player.InputLocked; _player.InputLocked = true; }
            var animation = _player != null ? _player.GetComponentInChildren<CharacterAnimationController>() : null;
            _animation = animation;
            bool posed = animation != null && pose != null && animation.PlayWork(pose);
            if (posed && prop != null) animation.HoldProp(prop);
            _label.text = label;
            _card.gameObject.SetActive(true);
            _card.SetAsLastSibling();
            float nextGesture = 0f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                _fill.rectTransform.anchorMax = new Vector2(k, 1f);
                _detail.text = $"{tool}  ·  {Mathf.CeilToInt((seconds - t) * 10f) / 10f:0.0}s";
                if (!posed && animation != null && t >= nextGesture) { animation.TriggerPoint(); nextGesture = t + 1.1f; }
                yield return null;
            }
            _fill.rectTransform.anchorMax = Vector2.one;
            Finish();
            _onCancel = null;
            onDone?.Invoke();
        }

        private void Finish()
        {
            _running = null;
            if (_animation != null) _animation.StopWork();
            _animation = null;
            if (_card != null) _card.gameObject.SetActive(false);
            if (_player != null) _player.InputLocked = _wasLocked;
            _player = null;
        }
    }
}
