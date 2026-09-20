using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    public enum HudVitalKind { Health, Energy, Hunger, Thirst }

    /// <summary>
    /// Compact always-visible player card: level + EXP, wallet, and one slim bar per vital stat.
    /// Every element is authored in the scene (no runtime UI building); this component only feeds
    /// values and localized labels into the serialized references. To add a stat (for example a
    /// toilet need) duplicate a row in the scene, add it to the rows list and extend ReadValue.
    /// </summary>
    public class HudVitalsCard : MonoBehaviour
    {
        [Serializable]
        public class VitalRow
        {
            public HudVitalKind kind;
            public TMP_Text label;
            public TMP_Text value;
            public RectTransform fill;
            public Image fillImage;
            public Color color = Color.white;
        }

        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text walletText;
        [SerializeField] private RectTransform expFill;
        [SerializeField] private VitalRow[] rows = Array.Empty<VitalRow>();
        [SerializeField, Range(0.05f, 0.5f)] private float lowThreshold = 0.25f;
        [SerializeField] private Color lowColor = new Color(0.95f, 0.30f, 0.28f, 1f);
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.2f;

        private float _nextRefresh;
        private GameLanguage _language = (GameLanguage)(-1);

        private void OnEnable()
        {
            _nextRefresh = 0f;
            _language = (GameLanguage)(-1);
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + refreshInterval;
                Refresh();
            }

            ApplyLowPulse();
        }

        private void Refresh()
        {
            GameLanguage language = GameServices.TryGet(out GameSettingsService settings) ? settings.Language : GameLanguage.Vietnamese;
            bool relabel = language != _language;
            _language = language;

            PlayerStatus status = PlayerStatus.Instance;
            PlayerInventory inventory = PlayerInventory.Instance;

            if (levelText != null) levelText.text = status != null ? $"Lv {status.Level}" : "Lv -";
            if (walletText != null) walletText.text = inventory != null ? $"¥ {inventory.Yen:N0}" : "¥ -";
            SetFill(expFill, status != null && status.MaxExp > 0 ? (float)status.CurrentExp / status.MaxExp : 0f);

            foreach (var row in rows)
            {
                if (row == null) continue;

                float fraction = ReadValue(row.kind, status, out float shown);
                SetFill(row.fill, fraction);
                if (row.value != null) row.value.text = status != null ? Mathf.RoundToInt(shown).ToString() : "-";
                if (relabel && row.label != null) row.label.text = LabelFor(row.kind, language);

                bool low = status != null && fraction <= lowThreshold;
                if (row.fillImage != null) row.fillImage.color = low ? lowColor : row.color;
                if (row.value != null) row.value.color = low ? lowColor : new Color(0.93f, 0.95f, 0.97f, 1f);
            }
        }

        private void ApplyLowPulse()
        {
            PlayerStatus status = PlayerStatus.Instance;
            if (status == null) return;

            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
            foreach (var row in rows)
            {
                if (row == null || row.fillImage == null) continue;
                if (ReadValue(row.kind, status, out _) > lowThreshold) continue;

                Color c = lowColor;
                c.a = pulse;
                row.fillImage.color = c;
            }
        }

        private static float ReadValue(HudVitalKind kind, PlayerStatus status, out float shown)
        {
            shown = 0f;
            if (status == null) return 0f;

            switch (kind)
            {
                case HudVitalKind.Health:
                    shown = status.CurrentHealth;
                    return status.MaxHealth > 0f ? status.CurrentHealth / status.MaxHealth : 0f;
                case HudVitalKind.Energy:
                    shown = status.CurrentEnergy;
                    return status.MaxEnergy > 0f ? status.CurrentEnergy / status.MaxEnergy : 0f;
                case HudVitalKind.Hunger:
                    shown = status.Hunger;
                    return status.Hunger / 100f;
                default:
                    shown = status.Thirst;
                    return status.Thirst / 100f;
            }
        }

        private static string LabelFor(HudVitalKind kind, GameLanguage language)
        {
            bool ja = language == GameLanguage.Japanese;
            bool en = language == GameLanguage.English;
            switch (kind)
            {
                case HudVitalKind.Health: return ja ? "体力" : en ? "Health" : "Máu";
                case HudVitalKind.Energy: return ja ? "元気" : en ? "Energy" : "Năng lượng";
                case HudVitalKind.Hunger: return ja ? "満腹" : en ? "Hunger" : "No";
                default: return ja ? "水分" : en ? "Thirst" : "Khát";
            }
        }

        private static void SetFill(RectTransform fill, float fraction)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }
}
