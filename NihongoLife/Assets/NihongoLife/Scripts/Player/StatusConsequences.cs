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
        private PlayerController _lockedController;              // locked by the running blackout, released if it is cut short
        private CharacterAnimationController _restingAnimation;
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
            if (_busy || _status == null || NihongoLife.Core.IntroTrailer.Playing) return;
            // Health still at 0 (the event fired while a blackout could not run, or a save loaded at 0): faint now.
            if (_status.CurrentHealth <= 0f) StartCoroutine(Blackout(true));
            else if (_status.NoddingOffSeconds >= PlayerStatus.FallAsleepAfterSeconds) StartCoroutine(Blackout(false));
        }

        private void OnDisable()
        {
            // A scene change or a disabled player stops the coroutine half way: never stay "busy" with a black
            // screen, or the player is stuck at 0 health forever (no faint, no clinic).
            if (!_busy) return;
            _busy = false;
            if (_overlay != null) { _overlay.alpha = 0f; _overlay.blocksRaycasts = false; }
            if (_lockedController != null) _lockedController.InputLocked = false; // we locked it: give control back
            if (_restingAnimation != null) _restingAnimation.SetResting(false);
            _lockedController = null;
            _restingAnimation = null;
        }

        private void HandleCollapse()
        {
            if (!_busy && isActiveAndEnabled && !NihongoLife.Core.IntroTrailer.Playing) StartCoroutine(Blackout(true));
        }

        private IEnumerator Blackout(bool faint)
        {
            _busy = true;
            var controller = SceneRefs.Find<PlayerController>();
            var animation = controller != null ? controller.GetComponentInChildren<CharacterAnimationController>() : null;
            bool wasLocked = controller != null && controller.InputLocked;
            if (controller != null) controller.InputLocked = true;
            _lockedController = wasLocked ? null : controller;
            _restingAnimation = animation;
            animation?.SetResting(true);
            EnsureOverlay();
            _title.text = faint ? NihongoLife.Core.Loc.T("Bạn đã ngất…", "You fainted…", "倒れてしまいました…") : NihongoLife.Core.Loc.T("Bạn ngủ gục giữa đường…", "You fell asleep in the street…", "道で眠ってしまいました…");
            _body.text = faint ? NihongoLife.Core.Loc.T("Cơ thể kiệt sức vì đói, khát hoặc bệnh. Người qua đường đã gọi phòng khám Hibari.", "Your body gave out from hunger, thirst or illness. A passer-by called Hibari Clinic.", "空腹やのどの渇き、病気で体が限界でした。通りかかった人がひばりクリニックを呼びました。") : NihongoLife.Core.Loc.T("Thức quá lâu nên cơ thể tự tắt nguồn.", "You stayed up too long and your body shut down.", "起きすぎて、体が勝手に眠ってしまいました。");
            yield return Fade(1f, 1.4f);
            yield return new WaitForSecondsRealtime(1.2f);

            var cycle = SceneRefs.Find<DayNightCycle>();
            if (faint)
            {
                var inventory = controller != null ? controller.GetComponent<PlayerInventory>() : SceneRefs.Find<PlayerInventory>();
                int yen = inventory != null ? inventory.Yen : 0;
                LastFee = Mathf.Min(MaxClinicFee, Mathf.RoundToInt(yen * 0.1f));
                if (LastFee > 0 && inventory != null) inventory.SpendYen(LastFee, NihongoLife.Core.Loc.Triple("Phí phòng khám", "Clinic fee", "診察料"));
                cycle?.SetHour(cycle.Hour + 3f);
                _status.RecoverAfterCollapse();
                CollapseCount++;
                _title.text = NihongoLife.Core.Loc.T("Tỉnh lại sau 3 giờ", "You wake up 3 hours later", "3時間後に目が覚めました");
                _body.text = NihongoLife.Core.Loc.F("Phòng khám đã chữa cho bạn. Viện phí: {0}.\nHãy ăn uống và ngủ đủ — các vòng chỉ số nhấp nháy đỏ là lúc phải lo ngay.", "The clinic treated you. Fee: {0}.\nEat, drink and sleep enough — a status ring flashing red means act now.", "クリニックで治療を受けました。料金：{0}。\nしっかり食べて、飲んで、寝ましょう。リングが赤く点滅したらすぐ対処を。", NihongoLife.Core.Loc.Yen(LastFee));
            }
            else
            {
                cycle?.SetHour(cycle.Hour + 6f);
                _status.Sleep(6f);
                _title.text = NihongoLife.Core.Loc.T("Tỉnh dậy sau 6 giờ", "You wake up 6 hours later", "6時間後に目が覚めました");
                _body.text = NihongoLife.Core.Loc.T("Bạn đã ngủ bù, nhưng mất nửa ngày. Lần sau hãy về phòng ngủ khi vòng Tỉnh táo chuyển đỏ.", "You caught up on sleep, but lost half a day. Next time, go home to bed when the Alertness ring turns red.", "眠りは取り戻せましたが、半日が過ぎました。次は「眠気」のリングが赤くなったら部屋で寝ましょう。");
            }
            yield return new WaitForSecondsRealtime(2.2f);
            animation?.SetResting(false);
            yield return Fade(0f, 1f);
            if (controller != null) controller.InputLocked = wasLocked;
            _lockedController = null;
            _restingAnimation = null;
            NihongoLife.UI.HudFeed.Post(faint ? NihongoLife.Core.Loc.F("Đã trả viện phí {0}.", "Paid the clinic fee of {0}.", "診察料{0}を払いました。", NihongoLife.Core.Loc.Yen(LastFee)) : NihongoLife.Core.Loc.T("Bạn đã ngủ 6 giờ ngoài đường.", "You slept 6 hours in the street.", "道で6時間眠りました。"), NihongoLife.UI.HudFeed.Kind.Warning, 5f);
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
