using System.Collections;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Player
{
    /// <summary>
    /// What happens when needs are neglected, after the HUD has warned (rings flash red under 15 %):
    /// · health 0 → you faint: the screen goes dark, a passer-by calls the clinic, you wake 3 in-game hours later
    ///   patched up (health 45 %, energy 50 %, some food and water) and pay the bill (10 % of your yen, at most ¥800);
    /// · sleepiness 100 % for 40 s → you nod off where you stand and wake 6 hours later, rested but late.
    /// Hunger/thirst at 0 already drain health and slow you (PlayerStatus); this is the end of that road.
    /// </summary>
    public sealed class StatusConsequences : MonoBehaviour
    {
        public const int MaxClinicFee = 800;

        private PlayerStatus _status;
        private bool _busy;
        private CanvasGroup _overlay;
        private TextMeshProUGUI _title, _body;

        public bool IsBusy => _busy;
        public int LastFee { get; private set; }
        public int CollapseCount { get; private set; }

        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
            if (_status != null) _status.OnCollapsed += HandleCollapse;
        }

        private void OnDestroy()
        {
            if (_status != null) _status.OnCollapsed -= HandleCollapse;
            if (_overlay != null) Destroy(_overlay.transform.root.gameObject);
        }

        private void Update()
        {
            if (!_busy && _status != null && _status.NoddingOffSeconds >= PlayerStatus.FallAsleepAfterSeconds)
                StartCoroutine(Blackout(false));
        }

        private void HandleCollapse()
        {
            if (!_busy) StartCoroutine(Blackout(true));
        }

        private IEnumerator Blackout(bool faint)
        {
            _busy = true;
            var controller = FindFirstObjectByType<PlayerController>();
            var animation = controller != null ? controller.GetComponentInChildren<CharacterAnimationController>() : null;
            bool wasLocked = controller != null && controller.InputLocked;
            if (controller != null) controller.InputLocked = true;
            animation?.SetResting(true);
            EnsureOverlay();
            _title.text = faint ? "Bạn đã ngất…" : "Bạn ngủ gục giữa đường…";
            _body.text = faint ? "Cơ thể kiệt sức vì đói, khát hoặc bệnh. Người qua đường đã gọi phòng khám Hibari." : "Thức quá lâu nên cơ thể tự tắt nguồn.";
            yield return Fade(1f, 1.4f);
            yield return new WaitForSecondsRealtime(1.2f);

            var cycle = FindFirstObjectByType<DayNightCycle>();
            if (faint)
            {
                var inventory = controller != null ? controller.GetComponent<PlayerInventory>() : FindFirstObjectByType<PlayerInventory>();
                int yen = inventory != null ? inventory.Yen : 0;
                LastFee = Mathf.Min(MaxClinicFee, Mathf.RoundToInt(yen * 0.1f));
                if (LastFee > 0 && inventory != null) inventory.SpendYen(LastFee, "Phí phòng khám");
                cycle?.SetHour(cycle.Hour + 3f);
                _status.RecoverAfterCollapse();
                CollapseCount++;
                _title.text = "Tỉnh lại sau 3 giờ";
                _body.text = $"Phòng khám đã chữa cho bạn. Viện phí: ¥{LastFee}.\nHãy ăn uống và ngủ đủ — các vòng chỉ số nhấp nháy đỏ là lúc phải lo ngay.";
            }
            else
            {
                cycle?.SetHour(cycle.Hour + 6f);
                _status.Sleep(6f);
                _title.text = "Tỉnh dậy sau 6 giờ";
                _body.text = "Bạn đã ngủ bù, nhưng mất nửa ngày. Lần sau hãy về phòng ngủ khi vòng Tỉnh táo chuyển đỏ.";
            }
            yield return new WaitForSecondsRealtime(2.2f);
            animation?.SetResting(false);
            yield return Fade(0f, 1f);
            if (controller != null) controller.InputLocked = wasLocked;
            NihongoLife.UI.HudFeed.Post(faint ? $"Đã trả viện phí ¥{LastFee}." : "Bạn đã ngủ 6 giờ ngoài đường.", NihongoLife.UI.HudFeed.Kind.Warning, 5f);
            _busy = false;
        }

        private IEnumerator Fade(float to, float seconds)
        {
            float from = _overlay.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _overlay.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            _overlay.alpha = to;
            _overlay.blocksRaycasts = to > 0.5f;
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            var canvas = NihongoLife.UI.NLUi.CreateCanvas("BlackoutCanvas", 900);
            var root = new GameObject("Blackout", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            NihongoLife.UI.NLUi.Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 1f);
            _overlay = root.GetComponent<CanvasGroup>();
            _overlay.alpha = 0f;
            var font = NihongoLife.UI.NLUi.ResolveFont();
            _title = NihongoLife.UI.NLUi.Label(root.transform, "Title", "", 40f, new Color(1f, 0.85f, 0.55f), font, FontStyles.Bold, TextAlignmentOptions.Center);
            NihongoLife.UI.NLUi.Anchor(_title.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(1100f, 60f));
            _body = NihongoLife.UI.NLUi.Label(root.transform, "Body", "", 20f, new Color(0.85f, 0.88f, 0.92f), font, FontStyles.Normal, TextAlignmentOptions.Center);
            NihongoLife.UI.NLUi.Anchor(_body.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(1000f, 90f));
        }
    }
}
