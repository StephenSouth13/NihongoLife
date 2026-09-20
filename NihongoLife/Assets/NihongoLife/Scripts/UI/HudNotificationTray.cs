using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    public enum HudNoticeTone { Info, Warning, Alert }

    /// <summary>
    /// Fixed HUD notification slots (authored in the scene under the vitals card). Any system can
    /// post a persistent notice, for example an unpaid restaurant bill, and clear it when done.
    /// Notices are idempotent per id: posting the same id again just updates it.
    /// </summary>
    public class HudNotificationTray : MonoBehaviour
    {
        [Serializable]
        public class Chip
        {
            public GameObject root;
            public Image background;
            public Image badge;
            public TMP_Text title;
            public TMP_Text subtitle;
            public Button button;
        }

        private class Notice
        {
            public string id;
            public string title;
            public string subtitle;
            public Action onClick;
            public HudNoticeTone tone;
            public bool pulse;
            public float expiresAt;
        }

        [SerializeField] private Chip[] chips = Array.Empty<Chip>();

        private static HudNotificationTray _instance;
        private readonly List<Notice> _notices = new List<Notice>();
        private bool _dirty = true;

        public static HudNotificationTray Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<HudNotificationTray>();
                return _instance;
            }
        }

        private void OnEnable()
        {
            _instance = this;
            for (int i = 0; i < chips.Length; i++)
            {
                int captured = i;
                if (chips[i] != null && chips[i].button != null)
                {
                    chips[i].button.onClick.RemoveAllListeners();
                    chips[i].button.onClick.AddListener(() => OnChipClicked(captured));
                }
            }

            _dirty = true;
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
        }

        /// <param name="seconds">0 keeps the notice until Clear is called.</param>
        public void Post(string id, string title, string subtitle, Action onClick = null, HudNoticeTone tone = HudNoticeTone.Info, bool pulse = false, float seconds = 0f)
        {
            if (string.IsNullOrEmpty(id)) return;

            Notice notice = _notices.Find(n => n.id == id);
            bool isNew = notice == null;
            if (isNew)
            {
                notice = new Notice { id = id };
                _notices.Add(notice);
            }

            bool changed = isNew || notice.title != title || notice.subtitle != subtitle || notice.tone != tone || notice.pulse != pulse;
            notice.title = title;
            notice.subtitle = subtitle;
            notice.onClick = onClick;
            notice.tone = tone;
            notice.pulse = pulse;
            notice.expiresAt = seconds > 0f ? Time.unscaledTime + seconds : 0f;
            if (changed) _dirty = true;
        }

        public void Clear(string id)
        {
            if (_notices.RemoveAll(n => n.id == id) > 0) _dirty = true;
        }

        private void Update()
        {
            for (int i = _notices.Count - 1; i >= 0; i--)
            {
                if (_notices[i].expiresAt > 0f && Time.unscaledTime >= _notices[i].expiresAt)
                {
                    _notices.RemoveAt(i);
                    _dirty = true;
                }
            }

            if (_dirty) Render();

            for (int i = 0; i < chips.Length && i < _notices.Count; i++)
            {
                if (!_notices[i].pulse || chips[i] == null || chips[i].background == null) continue;
                Color baseColor = BackgroundFor(_notices[i].tone);
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f);
                chips[i].background.color = new Color(baseColor.r * pulse + 0.04f, baseColor.g * pulse + 0.02f, baseColor.b * pulse, baseColor.a);
            }
        }

        private void Render()
        {
            _dirty = false;
            for (int i = 0; i < chips.Length; i++)
            {
                var chip = chips[i];
                if (chip == null || chip.root == null) continue;

                if (i >= _notices.Count)
                {
                    chip.root.SetActive(false);
                    continue;
                }

                var notice = _notices[i];
                bool lastSlot = i == chips.Length - 1;
                int hidden = _notices.Count - chips.Length;
                string subtitle = lastSlot && hidden > 0 ? $"{notice.subtitle}  (+{hidden})" : notice.subtitle;

                chip.root.SetActive(true);
                if (chip.title != null) chip.title.text = notice.title;
                if (chip.subtitle != null) chip.subtitle.text = subtitle;
                if (chip.background != null) chip.background.color = BackgroundFor(notice.tone);
                if (chip.badge != null) chip.badge.color = BadgeFor(notice.tone);
            }
        }

        private void OnChipClicked(int index)
        {
            if (index >= 0 && index < _notices.Count) _notices[index].onClick?.Invoke();
        }

        private static Color BackgroundFor(HudNoticeTone tone)
        {
            switch (tone)
            {
                case HudNoticeTone.Warning: return new Color(0.30f, 0.22f, 0.06f, 0.94f);
                case HudNoticeTone.Alert: return new Color(0.36f, 0.10f, 0.10f, 0.94f);
                default: return new Color(0.06f, 0.11f, 0.16f, 0.94f);
            }
        }

        private static Color BadgeFor(HudNoticeTone tone)
        {
            switch (tone)
            {
                case HudNoticeTone.Warning: return new Color(0.95f, 0.72f, 0.25f, 1f);
                case HudNoticeTone.Alert: return new Color(0.95f, 0.36f, 0.32f, 1f);
                default: return new Color(0.42f, 0.72f, 0.95f, 1f);
            }
        }
    }
}
