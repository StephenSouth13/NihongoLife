using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The look panel: hair colour, beard (opens with age, boys' models), "grey with age", and what age changes
    /// (wrinkles, walking and speaking pace). Docked on the right without dimming, so the character stays in view
    /// (the player in town, the preview in the menu's character select); every change shows at once. Opened from
    /// the HUD portrait and the character select ("Ngoại hình").
    /// </summary>
    public sealed class CharacterLookUI : MonoBehaviour
    {
        private static CharacterLookUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _body;
        private GameObject _subject;
        private string _message;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeInHierarchy;

        private static CharacterLookUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("CharacterLookCanvas", 140);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<CharacterLookUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Rebuild(); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "CharacterLook");
                CharacterLook.Changed += () => { if (IsOpen) _instance.Rebuild(); };
                return _instance;
            }
        }

        /// <summary>Opens the panel for <paramref name="subject"/> (the model whose sex decides beards; null = the player).</summary>
        public static void Open(GameObject subject = null)
        {
            var ui = Instance;
            ui._subject = subject;
            ui._message = null;
            ui._root.gameObject.SetActive(true);
            ui._root.SetAsLastSibling();
            ui.Rebuild();
        }

        public static void Close()
        {
            if (_instance == null || _instance._root == null) return;
            _instance._root.gameObject.SetActive(false);
        }

        private void Build(RectTransform canvas)
        {
            _root = NLUi.Panel(canvas, "CharacterLook", NLUi.Ink);
            _root.anchorMin = new Vector2(1f, 0.5f); _root.anchorMax = new Vector2(1f, 0.5f); _root.pivot = new Vector2(1f, 0.5f);
            _root.anchoredPosition = new Vector2(-24f, 0f);
            _root.sizeDelta = new Vector2(520f, 760f);
            _root.gameObject.AddComponent<HudFitRect>().Configure(0.45f, 0.94f, new Vector2(-24f, 0f), 0.4f);
            var scrollRoot = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollRoot.SetParent(_root, false);
            NLUi.Stretch(scrollRoot);
            scrollRoot.offsetMin = new Vector2(22f, 18f); scrollRoot.offsetMax = new Vector2(-12f, -18f);
            _body = UiKit.Scroll(scrollRoot, "Content", out var scroll, 10f, new RectOffset(2, 14, 2, 12));
            NLUi.Stretch((RectTransform)scroll.transform);
            NLUi.CloseButton(_root, _font, Close, 42f, 12f);
            _root.gameObject.SetActive(false);
        }

        private bool SubjectIsBoy()
        {
            var subject = _subject;
            if (subject == null)
            {
                var player = SceneRefs.Find<PlayerController>();
                subject = player != null ? player.gameObject : null;
            }
            var visual = subject != null ? subject.transform.Find("Visual") : null;
            var model = CharacterLook.ModelOf(visual != null ? visual.gameObject : subject);
            if (model != null) return model.boy;
            return PlayableCharacterCatalog.GetSelected().Id is "kid_haruto" or "kid_sota" or "kid_riku" or "kid_yuto";
        }

        private void Rebuild()
        {
            UiKit.Clear(_body);
            var options = CharacterLook.Options;
            var state = CharacterLook.State;
            int age = CharacterLook.PlayerAge;
            bool boy = SubjectIsBoy();

            NLUi.Label(_body, "Title", Loc.T("Ngoại hình", "Appearance", "見た目"), 28f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Label(_body, "Age", Loc.F("{0} tuổi · tuổi tăng mỗi sinh nhật trong game", "Age {0} · a year older each in-game birthday", "{0}歳 · ゲーム内の誕生日ごとに一つ年をとる", age), 15f, NLUi.Muted, _font);
            if (!string.IsNullOrEmpty(_message)) NLUi.Label(_body, "Message", _message, 15f, NLUi.Bad, _font);

            NLUi.Label(_body, "HairTitle", Loc.T("Màu tóc", "Hair colour", "髪の色"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            var grid = new GameObject("Hairs", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            grid.SetParent(_body, false);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(144f, 44f); layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 3;
            NLUi.FitContent(grid);
            foreach (var hair in options.hairs)
            {
                string id = hair.id;
                bool on = state.hair == id;
                var swatch = string.IsNullOrEmpty(hair.hex) ? NLUi.Card : CharacterLook.Hex(hair.hex);
                var button = NLUi.Button(grid, "Hair_" + id, (on ? "✓ " : "") + hair.Name, _font, () => { _message = null; CharacterLook.SetHair(id); },
                    swatch, 14f, swatch.grayscale > 0.55f ? NLUi.Ink : Color.white, 44f);
                if (on) button.gameObject.AddComponent<Outline>().effectColor = NLUi.Gold;
            }

            NLUi.Label(_body, "BeardTitle", Loc.T("Râu", "Beard", "ひげ"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            foreach (var beard in options.beards)
            {
                string id = beard.id;
                string why = CharacterLook.BeardBlocker(id, age, boy);
                bool on = state.beard == id;
                string label = (on ? "✓ " : "") + beard.Name + (why != null ? "  <size=80%><color=#A8B4C4>" + why + "</color></size>" : "");
                var button = NLUi.Button(_body, "Beard_" + id, label, _font, () => { _message = CharacterLook.SetBeard(id, boy); Rebuild(); },
                    on ? NLUi.Gold : why != null ? new Color(1f, 1f, 1f, 0.05f) : NLUi.Card, 15f, on ? NLUi.Ink : why != null ? NLUi.Muted : NLUi.Text, 42f);
                button.interactable = why == null;
            }

            NLUi.Label(_body, "AgeTitle", Loc.T("Theo tuổi", "With age", "年齢とともに"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Button(_body, "GrayWithAge", (state.grayWithAge ? "☑ " : "☐ ") + Loc.F("Tóc bạc dần từ {0} tuổi", "Hair greys from age {0}", "{0}歳から白髪に", options.grayFromAge), _font,
                () => CharacterLook.SetGrayWithAge(!state.grayWithAge), NLUi.Card, 15f, NLUi.Text, 42f);
            int lines = 0;
            foreach (var set in options.wrinkles) if (age >= set.minAge) lines++;
            int firstLines = options.wrinkles.Length > 0 ? options.wrinkles[0].minAge : 0;
            NLUi.Label(_body, "Wrinkles", lines > 0 ? Loc.F("Nếp nhăn: mức {0}", "Wrinkles: level {0}", "しわ：レベル{0}", lines)
                : Loc.F("Nếp nhăn xuất hiện từ {0} tuổi", "Wrinkles appear from age {0}", "{0}歳からしわが出ます", firstLines), 15f, NLUi.Text, _font);
            NLUi.Label(_body, "Pace", Loc.F("Tốc độ đi ×{0:0.00} · tốc độ nói ×{1:0.00} (chậm dần từ {2} tuổi)", "Walking ×{0:0.00} · speaking ×{1:0.00} (slower from age {2})", "歩く速さ ×{0:0.00} · 話す速さ ×{1:0.00}（{2}歳からゆっくり）",
                CharacterLook.WalkFactor(age), CharacterLook.TalkFactor(age), options.slowFromAge), 15f, NLUi.Muted, _font);

            var done = NLUi.Button(_body, "Done", Loc.T("Xong", "Done", "完了"), _font, Close, NLUi.Gold, 17f, NLUi.Ink, 46f);
            NLUi.Size(done, -1f, 46f);
        }
    }
}
