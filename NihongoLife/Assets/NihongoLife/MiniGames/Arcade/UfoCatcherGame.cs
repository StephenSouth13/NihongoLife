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
    /// UFOキャッチャー: a real-looking crane game. The machine calls a plush in Japanese only ("「ねこ」を とって！");
    /// the claw sweeps over the pile — press Space (or the button) to drop it. The right plush is carried to the
    /// chute and drops out ("GET!"), a wrong one slips out of the claw and costs a life, an empty grab breaks the combo.
    /// Prizes are vocab targets. Art (one plush style, the cabinet, the claw): Resources/MiniGames/UfoCatcher, made by
    /// Tools/art/ufo_catcher_art.py.
    /// </summary>
    public sealed class UfoCatcherGame : ArcadeGame
    {
        public sealed class Prize
        {
            public Prize(string id, string ja, string romaji, string vi, string en) { Id = id; Ja = ja; Romaji = romaji; Vi = vi; En = en; }
            public string Id { get; }
            public string Ja { get; }
            public string Romaji { get; }
            public string Vi { get; }
            public string En { get; }
            public string Picture => Art + "plush_" + Id;
            public string TargetId => "vocab.prize." + Id;
        }

        private const string Art = "MiniGames/UfoCatcher/";

        public static readonly Prize[] Catalog =
        {
            new Prize("kuma", "くま", "kuma", "con gấu", "bear"),
            new Prize("neko", "ねこ", "neko", "con mèo", "cat"),
            new Prize("inu", "いぬ", "inu", "con chó", "dog"),
            new Prize("usagi", "うさぎ", "usagi", "con thỏ", "rabbit"),
            new Prize("tori", "とり", "tori", "con chim", "bird"),
            new Prize("sakana", "さかな", "sakana", "con cá", "fish"),
            new Prize("ringo", "りんご", "ringo", "quả táo", "apple"),
            new Prize("ichigo", "いちご", "ichigo", "quả dâu", "strawberry"),
            new Prize("hoshi", "ほし", "hoshi", "ngôi sao", "star"),
            new Prize("hana", "はな", "hana", "bông hoa", "flower"),
            new Prize("kagi", "かぎ", "kagi", "chìa khoá", "key"),
            new Prize("okashi", "おかし", "okashi", "bánh kẹo", "sweets"),
        };

        public override string GameId => "ufo_catcher";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(1f, 0.42f, 0.75f);
        protected override (string vi, string en, string ja) HowTo => (
            "Nghe máy gọi tên thú bông bằng tiếng Nhật, canh cần gắp đang chạy rồi bấm Space (hoặc nút Thả) để gắp đúng con. Gắp nhầm mất một mạng.",
            "The machine calls a plush in Japanese. Time the moving claw and press Space (or Drop) to grab the right one. A wrong plush costs a life.",
            "よばれた ぬいぐるみを とろう！ スペース（または ボタン）で アームを おろす。まちがえると ライフが へります。");

        // Cabinet geometry (stage units; cabinet.png is drawn at 2x): the window and, inside it, the chute and the pile.
        private const float CabinetW = 860f, CabinetH = 600f;
        private const float WindowX = 30f, WindowY = 85f, WindowW = 800f, WindowH = 430f;
        private const float ChuteX = 65f, ChuteTop = 280f;      // in the window, from its left / top
        private const float PileLeft = 205f, PileRight = 750f;
        private const float RailY = 16f, RestDepth = 26f;
        private const float PrizeSize = 118f;

        private const int Slots = 7;
        private readonly Prize[] _slot = new Prize[Slots];
        private readonly RectTransform[] _slotRect = new RectTransform[Slots];
        private RectTransform _box, _claw, _cable, _carriage, _callCard, _prongL, _prongR;
        private TextMeshProUGUI _call, _reading;
        private Prize _target;
        private float _clawX = PileLeft, _dir = 1f, _depth = RestDepth;
        private bool _dropping;

        public Prize Target => _target;
        public bool Dropping => _dropping;

        protected override void BuildStage(RectTransform stage)
        {
            var cabinet = UiKit.Image(stage, "Cabinet", Color.white, Picture(Art + "cabinet"));
            UiKit.Place(cabinet.rectTransform, 0f, 0f, CabinetW, CabinetH);
            var title = UiKit.Label(cabinet.transform, "Marquee", "UFOキャッチャー", 24f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(title.rectTransform, 265f, 31f, 330f, 30f);

            _box = UiKit.Rect(cabinet.transform, "Window");
            UiKit.Place(_box, WindowX, WindowY, WindowW, WindowH);
            _box.gameObject.AddComponent<RectMask2D>();
            var chuteLabel = UiKit.Label(_box, "ChuteSign", "でぐち", 15f, new Color(1f, 0.84f, 0.29f), FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(chuteLabel.rectTransform, ChuteX - 50f, ChuteTop + 26f, 100f, 22f);

            // Gantry: the rail along the top and a carriage that runs on it; the cable hangs from the carriage.
            var rail = UiKit.Image(_box, "Rail", new Color(0.55f, 0.58f, 0.66f), UiKit.Rounded(4));
            UiKit.Place(rail.rectTransform, 14f, RailY - 4f, WindowW - 28f, 10f);
            _carriage = UiKit.Image(_box, "Carriage", new Color(0.42f, 0.45f, 0.53f), UiKit.Rounded(6)).rectTransform;
            TopLeft(_carriage, new Vector2(0.5f, 0.5f), new Vector2(54f, 22f));
            _cable = UiKit.Image(_box, "Cable", new Color(0.62f, 0.65f, 0.72f)).rectTransform;
            TopLeft(_cable, new Vector2(0.5f, 1f), new Vector2(4f, RestDepth));

            // The claw: motor housing and two prongs that open on the way down and close on the grab.
            _claw = UiKit.Rect(_box, "Claw");
            TopLeft(_claw, new Vector2(0.5f, 1f), new Vector2(100f, 120f));
            _prongL = Prong(_claw, -1f);
            _prongR = Prong(_claw, 1f);
            var hub = UiKit.Image(_claw, "Hub", Color.white, Picture(Art + "claw_hub"));
            UiKit.Pin(hub.rectTransform, new Vector2(0.5f, 1f), 0f, 0f, 96f, 56f);

            var glass = UiKit.Image(cabinet.transform, "Glass", Color.white, Picture(Art + "glass"));
            UiKit.Place(glass.rectTransform, WindowX, WindowY, WindowW, WindowH);
            glass.transform.SetAsLastSibling(); // reflections over the prizes and the claw

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
            PlaceClaw(RestDepth);
            OpenClaw(0.35f);
        }


        /// <summary>Anchors at the window's top-left corner, so positions are (x, -y from the top).</summary>
        private static void TopLeft(RectTransform rect, Vector2 pivot, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = pivot;
            rect.sizeDelta = size;
        }

        private static RectTransform Prong(RectTransform claw, float side)
        {
            // The mirror lives on a parent, so both prongs open with the same rotation code.
            var mount = UiKit.Rect(claw, side < 0f ? "ProngMountL" : "ProngMountR");
            mount.anchorMin = mount.anchorMax = new Vector2(0.5f, 1f);
            mount.pivot = new Vector2(0.5f, 1f);
            mount.sizeDelta = Vector2.zero;
            mount.anchoredPosition = new Vector2(side * 24f, -40f);
            mount.localScale = new Vector3(side < 0f ? 1f : -1f, 1f, 1f);
            var prong = UiKit.Image(mount, "Prong", Color.white, Picture(Art + "claw_prong"));
            var r = prong.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 1f - 16f / 220f);
            r.sizeDelta = new Vector2(40f, 73f);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        private void OpenClaw(float open)
        {
            if (_prongL != null) _prongL.localRotation = Quaternion.Euler(0f, 0f, -30f * open);
            if (_prongR != null) _prongR.localRotation = Quaternion.Euler(0f, 0f, -30f * open);
        }

        protected override void OnRoundStart()
        {
            for (int i = 0; i < Slots; i++) FillSlot(i, false);
            NextTarget();
        }

        private void FillSlot(int i, bool popIn)
        {
            var used = new HashSet<Prize>(_slot.Where(p => p != null));
            var options = Catalog.Where(p => !used.Contains(p)).ToList();
            _slot[i] = options[Random.Range(0, options.Count)];
            if (_slotRect[i] != null) Destroy(_slotRect[i].gameObject);
            var holder = UiKit.Rect(_box, "Prize_" + i);
            holder.anchorMin = holder.anchorMax = new Vector2(0f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(PrizeSize, PrizeSize);
            holder.anchoredPosition = SlotPosition(i);
            holder.localRotation = Quaternion.Euler(0f, 0f, ((i * 37) % 13 - 6) * 0.8f); // a heap, not a shelf
            var pic = UiKit.Image(holder, "Picture", Color.white, Picture(_slot[i].Picture));
            pic.preserveAspect = true;
            UiKit.Fill(pic.rectTransform);
            // Back row first: the claw and the front row draw over it.
            holder.SetSiblingIndex(i % 2 == 0 ? Mathf.Min(_box.childCount - 1, 5) : _box.childCount - 1);
            _claw.SetAsLastSibling();
            _slotRect[i] = holder;
            if (popIn) StartCoroutine(PopIn(holder));
        }

        private static float SlotX(int i) => PileLeft + i * (PileRight - PileLeft) / (Slots - 1);
        private static Vector2 SlotPosition(int i) => new Vector2(SlotX(i), i % 2 == 0 ? 34f : 8f);

        private IEnumerator PopIn(RectTransform rect)
        {
            for (float t = 0f; t < 1f && rect != null; t += Time.unscaledDeltaTime / 0.35f)
            {
                float s = t < 0.7f ? Mathf.Lerp(0f, 1.12f, t / 0.7f) : Mathf.Lerp(1.12f, 1f, (t - 0.7f) / 0.3f);
                rect.localScale = Vector3.one * s;
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }

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
            _clawX += _dir * Mathf.Lerp(200f, 340f, Mathf.Clamp01(Elapsed / 60f)) * dt;
            if (_clawX > PileRight + 10f) { _clawX = PileRight + 10f; _dir = -1f; }
            if (_clawX < PileLeft - 10f) { _clawX = PileLeft - 10f; _dir = 1f; }
            PlaceClaw(RestDepth);
        }

        private void PlaceClaw(float depth)
        {
            _depth = depth;
            _carriage.anchoredPosition = new Vector2(_clawX, -RailY);
            _cable.anchoredPosition = new Vector2(_clawX, -RailY);
            _cable.sizeDelta = new Vector2(4f, depth);
            _claw.anchoredPosition = new Vector2(_clawX, -RailY - depth);
        }

        /// <summary>Where a carried prize hangs: its bottom just under the closed prongs.</summary>
        private Vector2 CarryPosition() => new Vector2(_clawX, WindowH - (RailY + _depth + 104f));

        /// <summary>Drops the claw where it is (Space, the button, or tests).</summary>
        public void DropClaw()
        {
            if (State != Phase.Playing || _dropping) return;
            StartCoroutine(DropRoutine());
        }

        /// <summary>Tests: moves the claw over the slot holding <paramref name="prize"/>.</summary>
        public void AimAt(Prize prize)
        {
            if (_dropping) return;
            int i = System.Array.IndexOf(_slot, prize);
            if (i >= 0) { _clawX = SlotX(i); PlaceClaw(RestDepth); }
        }

        /// <summary>Tests: a slot that does not hold the called prize.</summary>
        public Prize AnyOther() => _slot.First(p => p != null && p != _target);

        private IEnumerator DropRoutine()
        {
            _dropping = true;
            float bottom = WindowH - RailY - PrizeSize - 60f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f) { PlaceClaw(Mathf.Lerp(RestDepth, bottom, Smooth(t))); OpenClaw(Mathf.Lerp(0.35f, 1f, t)); yield return null; }
            PlaceClaw(bottom);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f) { OpenClaw(1f - t); yield return null; }
            OpenClaw(0f);

            int grabbed = -1;
            float best = 56f;
            for (int i = 0; i < Slots; i++)
            {
                float d = Mathf.Abs(SlotX(i) - _clawX);
                if (d < best) { best = d; grabbed = i; }
            }
            RectTransform carried = grabbed >= 0 ? _slotRect[grabbed] : null;
            bool right = grabbed >= 0 && _slot[grabbed] == _target;
            Vector2 home = grabbed >= 0 ? SlotPosition(grabbed) : Vector2.zero;
            if (carried != null) { carried.SetAsLastSibling(); _claw.SetAsLastSibling(); carried.localRotation = Quaternion.identity; }

            // Up: a wrong plush slips out halfway; the right one rides to the top.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                PlaceClaw(Mathf.Lerp(bottom, RestDepth, Smooth(t)));
                if (carried != null) carried.anchoredPosition = CarryPosition();
                if (carried != null && !right && t > 0.45f) break;
                yield return null;
            }

            if (grabbed < 0)
            {
                PlaceClaw(RestDepth);
                Miss(_target.TargetId, null, null, _claw, costsLife: false);
                Pop(_claw, Loc.T("Trượt!", "Missed!", "からぶり！"), UiTheme.Warn);
            }
            else if (right)
            {
                PlaceClaw(RestDepth);
                // Over to the chute, open, and the plush drops out.
                float from = _clawX;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
                {
                    _clawX = Mathf.Lerp(from, ChuteX, Smooth(t));
                    PlaceClaw(RestDepth);
                    carried.anchoredPosition = CarryPosition();
                    yield return null;
                }
                OpenClaw(1f);
                Vector2 fallFrom = carried.anchoredPosition;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
                {
                    carried.anchoredPosition = new Vector2(ChuteX, Mathf.Lerp(fallFrom.y, -PrizeSize, t * t));
                    yield return null;
                }
                Hit(_target.TargetId, _claw, 120);
                Pop(_box, "GET！", UiTheme.Gold);
                FillSlot(grabbed, true);
                if (State == Phase.Playing) NextTarget();
                _clawX = PileLeft;
                _dir = 1f;
                PlaceClaw(RestDepth);
            }
            else
            {
                // The wrong plush falls back onto the pile.
                Vector2 slipFrom = carried.anchoredPosition;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
                {
                    carried.anchoredPosition = Vector2.Lerp(slipFrom, home, t * t);
                    PlaceClaw(Mathf.Lerp(_depth, RestDepth, t));
                    yield return null;
                }
                carried.anchoredPosition = home;
                PlaceClaw(RestDepth);
                var wrong = _slot[grabbed];
                Miss(_target.TargetId, wrong.TargetId, $"「{_target.Ja}」 = {Loc.Gloss(_target.Vi, _target.En)} " + Loc.F("(bạn gắp 「{0}」 = {1})", "(you grabbed 「{0}」 = {1})", "（「{0}」をとった）", wrong.Ja, Loc.Gloss(wrong.Vi, wrong.En)), carried);
                StartCoroutine(Shake(carried, 0.25f));
            }
            OpenClaw(0.35f);
            _dropping = false;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
