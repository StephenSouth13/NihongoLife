using System;
using System.Collections;
using System.Text;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// On the HUD yen chip: every wallet change rises from the chip as "+¥600 Thưởng nhiệm vụ" (green) or
    /// "−¥160 Mua ở Hibari Mart" (red), and the chip pulses. A click on the chip opens the last transactions.
    /// </summary>
    public sealed class WalletFeedback : MonoBehaviour, IPointerClickHandler
    {
        private TMP_FontAsset _font;
        private RectTransform _history;
        private TextMeshProUGUI _historyText;
        public static int Floating { get; private set; }

        public static bool HistoryOpen { get; private set; }

        public void Configure(TMP_FontAsset font) => _font = font;

        private void OnEnable() => WalletLedger.Recorded += Show;
        private void OnDisable() => WalletLedger.Recorded -= Show;

        private void Show(WalletLedger.Entry e)
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(Float(e));
            StartCoroutine(Pulse(e.amount > 0));
            if (HistoryOpen) RefreshHistory();
        }

        private IEnumerator Float(WalletLedger.Entry e)
        {
            Floating++;
            float lane = (Floating - 1) * 26f; // several changes at once stack instead of overlapping
            bool gain = e.amount > 0;
            var label = NLUi.Label(transform, "Delta", $"<b>{(gain ? "+" : "−")}¥{Math.Abs(e.amount):N0}</b>  <size=75%>{e.reason}</size>", 20f,
                gain ? new Color(0.45f, 0.95f, 0.55f) : new Color(1f, 0.5f, 0.42f), _font, FontStyles.Normal, TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 200);
            label.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(360f, 30f);
            for (float t = 0f; t < 1.8f; t += Time.unscaledDeltaTime)
            {
                rect.anchoredPosition = new Vector2(4f, 6f + lane + t * 34f);
                label.alpha = t < 1.2f ? 1f : 1f - (t - 1.2f) / 0.6f;
                yield return null;
            }
            Destroy(label.gameObject);
            Floating--;
        }

        private IEnumerator Pulse(bool gain)
        {
            var image = GetComponent<Image>();
            Color baseColor = image != null ? image.color : Color.clear;
            Color flash = gain ? new Color(0.15f, 0.42f, 0.2f, 0.95f) : new Color(0.5f, 0.15f, 0.12f, 0.95f);
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / 0.6f * Mathf.PI);
                transform.localScale = Vector3.one * (1f + 0.08f * k);
                if (image != null) image.color = Color.Lerp(baseColor, flash, k);
                yield return null;
            }
            transform.localScale = Vector3.one;
            if (image != null) image.color = baseColor;
        }

        public void OnPointerClick(PointerEventData eventData) => ToggleHistory();

        public void ToggleHistory()
        {
            if (_history == null) BuildHistory();
            HistoryOpen = !_history.gameObject.activeSelf;
            _history.gameObject.SetActive(HistoryOpen);
            if (HistoryOpen) RefreshHistory();
        }

        private void BuildHistory()
        {
            var canvas = NLUi.CreateCanvas("WalletHistoryCanvas", 560, transform.root);
            _history = NLUi.Panel(canvas.transform, "WalletHistory", NLUi.Ink, new RectOffset(18, 18, 14, 16), 8f);
            NLUi.Anchor(_history, new Vector2(0f, 0f), new Vector2(24f, 220f), new Vector2(460f, 10f));
            NLUi.FitContent(_history);
            NLUi.Label(_history, "Title", "Lịch sử ví  <size=70%><color=#A8B4C4>さいふ · 30 giao dịch gần nhất</color></size>", 22f, NLUi.Gold, _font, FontStyles.Bold);
            _historyText = NLUi.Label(_history, "Lines", "", 16f, NLUi.Text, _font);
            NLUi.CloseButton(_history, _font, () => { HistoryOpen = false; _history.gameObject.SetActive(false); }, 34f, 8f);
            UiModalStack.Register(this, () => HistoryOpen, () => { HistoryOpen = false; _history.gameObject.SetActive(false); }, "WalletHistory");
            _history.gameObject.SetActive(false);
        }

        private void RefreshHistory()
        {
            var sb = new StringBuilder();
            var entries = WalletLedger.Entries;
            if (entries.Count == 0) sb.Append("<color=#A8B4C4>Chưa có giao dịch. Làm thêm, nhiệm vụ và bán nông sản để có tiền.</color>");
            for (int i = 0; i < entries.Count && i < 12; i++)
            {
                var e = entries[i];
                string when = new DateTime(e.ticks).ToString("dd/MM HH:mm");
                string amount = e.amount > 0 ? $"<color=#7BE38E>+¥{e.amount:N0}</color>" : $"<color=#FF8A75>−¥{-e.amount:N0}</color>";
                if (i > 0) sb.Append('\n');
                sb.Append($"<color=#8A96A3>{when}</color>  {amount}  {e.reason}  <color=#8A96A3>→ ¥{e.balance:N0}</color>");
            }
            _historyText.text = sb.ToString();
        }
    }
}
