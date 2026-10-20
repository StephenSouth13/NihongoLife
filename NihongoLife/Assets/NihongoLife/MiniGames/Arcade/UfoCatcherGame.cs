using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// UFOキャッチャー: the call says which prize to grab, in Japanese only ("「ねこ」を とって！"); the claw swings left
    /// and right over the prize pile — press Space (or the button) to drop it. Grabbing the called prize scores, a
    /// wrong prize costs a life, an empty grab just breaks the combo. Prizes are vocab targets; their art comes from
    /// Resources/MiniGames/Prizes (rendered by Codex) with item sprites and icons as fallbacks.
    /// </summary>
    public sealed class UfoCatcherGame : ArcadeGame
    {
        public sealed class Prize
        {
            public Prize(string id, string ja, string romaji, string vi, string en, params string[] pictures) { Id = id; Ja = ja; Romaji = romaji; Vi = vi; En = en; Pictures = pictures; }
            public string Id { get; }
            public string Ja { get; }
            public string Romaji { get; }
            public string Vi { get; }
            public string En { get; }
            public string[] Pictures { get; }
            public string TargetId => "vocab.prize." + Id;
        }

        public static readonly Prize[] Catalog =
        {
            new Prize("kuma", "くま", "kuma", "con gấu", "bear", "MiniGames/Prizes/kuma", "Items/prize_plush"),
            new Prize("neko", "ねこ", "neko", "con mèo", "cat", "MiniGames/Prizes/neko", "icon:pets"),
            new Prize("inu", "いぬ", "inu", "con chó", "dog", "MiniGames/Prizes/inu", "icon:pets"),
            new Prize("usagi", "うさぎ", "usagi", "con thỏ", "rabbit", "MiniGames/Prizes/usagi", "icon:pets"),
            new Prize("tori", "とり", "tori", "con chim", "bird", "MiniGames/Prizes/tori", "icon:nature_people"),
            new Prize("sakana", "さかな", "sakana", "con cá", "fish", "MiniGames/Prizes/sakana", "MiniGames/Words/sakana"),
            new Prize("ringo", "りんご", "ringo", "quả táo", "apple", "MiniGames/Prizes/ringo", "MiniGames/Words/ringo"),
            new Prize("ichigo", "いちご", "ichigo", "quả dâu", "strawberry", "MiniGames/Prizes/ichigo", "MiniGames/Words/ichigo"),
            new Prize("hoshi", "ほし", "hoshi", "ngôi sao", "star", "MiniGames/Prizes/hoshi", "icon:star"),
            new Prize("hana", "はな", "hana", "bông hoa", "flower", "MiniGames/Prizes/hana", "icon:local_florist"),
            new Prize("kagi", "かぎ", "kagi", "chìa khoá", "key", "MiniGames/Prizes/kagi", "Items/prize_keychain"),
            new Prize("okashi", "おかし", "okashi", "bánh kẹo", "sweets", "MiniGames/Prizes/okashi", "Items/prize_snack"),
        };

        public override string GameId => "ufo_catcher";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(1f, 0.42f, 0.75f);
        protected override (string vi, string en, string ja) HowTo => (
            "Nghe máy gọi tên món bằng tiếng Nhật, canh cái gắp đang chạy rồi bấm Space (hoặc nút Thả) để gắp đúng món. Gắp nhầm mất một mạng.",
            "The machine calls a prize in Japanese. Time the swinging claw and press Space (or Drop) to grab the right one. A wrong prize costs a life.",
            "よばれた けいひんを とろう！ スペース（または ボタン）で アームを おろす。まちがえると ライフが へります。");

        private const int Slots = 7;
        private readonly Prize[] _slot = new Prize[Slots];
        private readonly RectTransform[] _slotRect = new RectTransform[Slots];
        private RectTransform _box, _claw, _cable, _callCard;
        private TextMeshProUGUI _call, _reading;
        private Prize _target;
        private float _clawX, _dir = 1f;
        private bool _dropping;

        public Prize Target => _target;
        public bool Dropping => _dropping;

        protected override void BuildStage(RectTransform stage)
        {
            var cabinet = UiKit.Card(stage, "Cabinet", new Color(0.32f, 0.1f, 0.26f), 26, 0.4f, false);
            UiKit.Place(cabinet.rectTransform, 0f, 0f, 860f, 600f);
            var glass = UiKit.Image(cabinet.transform, "Glass", new Color(0.75f, 0.9f, 1f, 0.12f), UiKit.Rounded(18));
            UiKit.Fill(glass.rectTransform, 24f, 24f, 24f, 24f);
            _box = glass.rectTransform;
            var floor = UiKit.Image(_box, "Pile", new Color(1f, 0.85f, 0.95f, 0.18f), UiKit.Rounded(14));
            UiKit.Strip(floor.rectTransform, false, 0f, 130f);
            var rail = UiKit.Image(_box, "Rail", new Color(0.85f, 0.85f, 0.95f, 0.8f), UiKit.Rounded(3));
            UiKit.Strip(rail.rectTransform, true, 18f, 8f, 10f, 10f);

            _cable = UiKit.Image(_box, "Cable", new Color(0.8f, 0.82f, 0.9f), null).rectTransform;
            _cable.anchorMin = _cable.anchorMax = new Vector2(0f, 1f); _cable.pivot = new Vector2(0.5f, 1f);
            _cable.sizeDelta = new Vector2(4f, 40f);
            // The claw: a hub on the cable with two prongs that open while moving and close on the grab.
            _claw = UiKit.Rect(_box, "Claw");
            _claw.anchorMin = _claw.anchorMax = new Vector2(0f, 1f); _claw.pivot = new Vector2(0.5f, 1f);
            _claw.sizeDelta = new Vector2(90f, 70f);
            var hub = UiKit.Image(_claw, "Hub", UiTheme.Gold, UiKit.Rounded(10));
            UiKit.Pin(hub.rectTransform, new Vector2(0.5f, 1f), 0f, 0f, 46f, 24f);
            var light = UiKit.Image(hub.transform, "Light", new Color(1f, 0.35f, 0.6f), UiKit.Circle);
            UiKit.Pin(light.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 12f, 12f);
            _prongL = Prong(_claw, -1f);
            _prongR = Prong(_claw, 1f);

            var call = UiKit.Card(stage, "Call", UiTheme.Surface, 20, 0.3f, false);
            UiKit.Place(call.rectTransform, 880f, 0f, 280f, 300f);
            _callCard = call.rectTransform;
            UiKit.Place(UiKit.Label(call.transform, "Ask", "Máy gọi:", "The machine calls:", "アナウンス", 15f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 18f, 280f, 22f);
            _call = UiKit.Label(call.transform, "Call", "", 30f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UiKit.Place(_call.rectTransform, 14f, 56f, 252f, 140f);
            _reading = UiKit.Label(call.transform, "Reading", "", 17f, UiTheme.TextSoft, FontStyles.Italic, TextAlignmentOptions.Center);
            UiKit.Place(_reading.rectTransform, 14f, 210f, 252f, 28f);

            var drop = UiKit.Button(stage, "Drop", "Thả gắp (Space)", "Drop (Space)", "おろす（スペース）", DropClaw, UiKit.Kind.Primary, "arrow_downward", 20f, 18);
            UiKit.Place((RectTransform)drop.transform, 880f, 320f, 280f, 70f);
        }

        private RectTransform _prongL, _prongR;

        private static RectTransform Prong(RectTransform claw, float side)
        {
            var arm = UiKit.Image(claw, side < 0f ? "ProngL" : "ProngR", new Color(0.85f, 0.86f, 0.92f), UiKit.Rounded(4));
            var r = arm.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(9f, 50f);
            r.anchoredPosition = new Vector2(side * 14f, -18f);
            var tip = UiKit.Image(r, "Tip", new Color(0.85f, 0.86f, 0.92f), UiKit.Rounded(4));
            tip.rectTransform.anchorMin = tip.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            tip.rectTransform.pivot = new Vector2(side < 0f ? 0f : 1f, 0f);
            tip.rectTransform.sizeDelta = new Vector2(20f, 8f);
            tip.rectTransform.anchoredPosition = new Vector2(side * -4f, 0f);
            return r;
        }

        private void OpenClaw(float open)
        {
            if (_prongL != null) _prongL.localRotation = Quaternion.Euler(0f, 0f, -28f * open);
            if (_prongR != null) _prongR.localRotation = Quaternion.Euler(0f, 0f, 28f * open);
        }

        protected override void OnRoundStart()
        {
            for (int i = 0; i < Slots; i++) FillSlot(i);
            NextTarget();
        }

        private void FillSlot(int i)
        {
            var used = new HashSet<Prize>(_slot.Where(p => p != null));
            var options = Catalog.Where(p => !used.Contains(p)).ToList();
            _slot[i] = options[Random.Range(0, options.Count)];
            if (_slotRect[i] != null) Destroy(_slotRect[i].gameObject);
            var holder = UiKit.Rect(_box, "Prize_" + i);
            holder.anchorMin = holder.anchorMax = new Vector2(0f, 0f); holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(96f, 96f);
            holder.anchoredPosition = new Vector2(SlotX(i), 18f + (i % 2) * 18f);
            var pic = UiKit.Image(holder, "Picture", Color.white, Picture(_slot[i].Pictures));
            pic.preserveAspect = true;
            UiKit.Fill(pic.rectTransform);
            if (pic.sprite != null && pic.sprite.texture != null && pic.sprite.texture.width <= 96) pic.color = Accent; // icon fallback: tint
            _slotRect[i] = holder;
        }

        private float SlotX(int i) => 70f + i * ((_box.rect.width > 10f ? _box.rect.width : 812f) - 140f) / (Slots - 1);

        private void NextTarget()
        {
            var present = _slot.Where(p => p != null && p != _target).ToList();
            _target = present[Random.Range(0, present.Count)];
            _call.text = $"「{_target.Ja}」を\nとって！";
            _reading.text = $"{_target.Romaji} o totte!";
            StartCoroutine(Bounce(_callCard, 0.06f));
        }

        protected override void OnTick(float dt)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) DropClaw();
            if (_dropping) return;
            float width = _box.rect.width;
            _clawX += _dir * Mathf.Lerp(230f, 380f, Mathf.Clamp01(Elapsed / 60f)) * dt;
            if (_clawX > width - 50f) { _clawX = width - 50f; _dir = -1f; }
            if (_clawX < 50f) { _clawX = 50f; _dir = 1f; }
            PlaceClaw(30f);
        }

        private void PlaceClaw(float depth)
        {
            _cable.anchoredPosition = new Vector2(_clawX, -22f);
            _cable.sizeDelta = new Vector2(4f, depth);
            _claw.anchoredPosition = new Vector2(_clawX, -22f - depth);
        }

        /// <summary>Drops the claw where it is (Space, the button, or tests).</summary>
        public void DropClaw()
        {
            if (State != Phase.Playing || _dropping) return;
            StartCoroutine(DropRoutine());
        }

        /// <summary>Tests: moves the claw over the slot holding <paramref name="prize"/>.</summary>
        public void AimAt(Prize prize)
        {
            int i = System.Array.IndexOf(_slot, prize);
            if (i >= 0) { _clawX = SlotX(i); PlaceClaw(30f); }
        }

        /// <summary>Tests: a slot that does not hold the called prize.</summary>
        public Prize AnyOther() => _slot.First(p => p != null && p != _target);

        private IEnumerator DropRoutine()
        {
            _dropping = true;
            float bottom = _box.rect.height - 150f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.55f) { PlaceClaw(Mathf.Lerp(30f, bottom, t)); OpenClaw(1f); yield return null; }
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f) { OpenClaw(1f - t); yield return null; }
            int grabbed = -1;
            float best = 60f;
            for (int i = 0; i < Slots; i++)
            {
                float d = Mathf.Abs(SlotX(i) - _clawX);
                if (d < best) { best = d; grabbed = i; }
            }
            RectTransform carried = grabbed >= 0 ? _slotRect[grabbed] : null;
            Vector2 start = carried != null ? carried.anchoredPosition : Vector2.zero;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                float depth = Mathf.Lerp(bottom, 30f, t);
                PlaceClaw(depth);
                if (carried != null) carried.anchoredPosition = new Vector2(_clawX, _box.rect.height - 22f - depth - 140f);
                yield return null;
            }
            if (grabbed < 0)
            {
                Miss(_target.TargetId, null, null, _claw, costsLife: false);
                Pop(_claw, Loc.T("Trượt!", "Missed!", "からぶり！"), UiTheme.Warn);
            }
            else if (_slot[grabbed] == _target)
            {
                Hit(_target.TargetId, _claw, 120);
                FillSlot(grabbed);
                if (State == Phase.Playing) NextTarget();
            }
            else
            {
                var wrong = _slot[grabbed];
                Miss(_target.TargetId, wrong.TargetId, $"「{_target.Ja}」 = {Loc.Gloss(_target.Vi, _target.En)} " + Loc.F("(bạn gắp 「{0}」 = {1})", "(you grabbed 「{0}」 = {1})", "（「{0}」をとった）", wrong.Ja, Loc.Gloss(wrong.Vi, wrong.En)), _claw);
                if (carried != null) carried.anchoredPosition = start;
            }
            OpenClaw(0.4f);
            _dropping = false;
        }
    }
}
