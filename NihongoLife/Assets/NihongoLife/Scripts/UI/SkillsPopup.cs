using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.UI
{
    /// <summary>F2: the skills handbook (key X): each skill with its tier, a progress line and what the tier unlocks.</summary>
    public sealed class SkillsPopup : MenuPopupBase
    {
        private const float W = 860f, H = 640f;
        private RectTransform _rows;
        public string RowsText { get; private set; } = "";

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "KỸ NĂNG"; en = "SKILLS"; ja = "スキル"; }

        public static SkillsPopup GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var ui = host.GetComponent<SkillsPopup>();
            if (ui == null) ui = host.AddComponent<SkillsPopup>();
            ui.Initialize(font);
            return ui;
        }

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle", "Kỹ năng lớn dần theo những gì bạn thật sự làm. Bấm Y để đóng.", "Skills grow from what you actually do. Press Y to close.", "スキルは実際にやったことで育ちます。Yで閉じます。", 15f, UiTheme.Muted, wrap: true);
            UiKit.Place(sub.rectTransform, 36f, 68f, W - 72f, 30f);
            _rows = UiKit.Rect(card, "Rows");
            UiKit.Place(_rows, 32f, 108f, W - 64f, H - 140f);
        }

        protected override void OnOpened() => Rebuild();
        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        protected override void Update()
        {
            base.Update();
            if (Keyboard.current == null || !Keyboard.current.xKey.wasPressedThisFrame) return;
            if (IsOpen) { Hide(); return; }
            if (!UiModalStack.BlocksHotkeys) Show();
        }

        private void Rebuild()
        {
            if (_rows == null) return;
            UiKit.Clear(_rows);
            var sb = new System.Text.StringBuilder();
            float y = 0f; const float RowH = 86f;
            foreach (var s in SkillTiers.All)
            {
                int tier = s.Tier;
                string perk = SkillTiers.Perk(s.Id, tier);
                var head = UiKit.Label(_rows, "Head_" + s.Id, s.Name + "   " + SkillTiers.Dots(tier, s.Tiers) + "   " + SkillTiers.TierName(s.Id, tier), 21f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
                UiKit.Place(head.rectTransform, 4f, y, W - 72f, 30f);
                var prog = UiKit.Label(_rows, "Prog_" + s.Id, SkillTiers.Progress(s), 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false);
                UiKit.Place(prog.rectTransform, 4f, y + 30f, W - 72f, 24f);
                if (perk.Length > 0)
                {
                    var p = UiKit.Label(_rows, "Perk_" + s.Id, "★ " + perk, 14f, UiTheme.Gold, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false);
                    UiKit.Place(p.rectTransform, 4f, y + 54f, W - 72f, 22f);
                }
                sb.AppendLine(head.text).AppendLine(prog.text);
                if (perk.Length > 0) sb.AppendLine(perk);
                y += RowH;
            }
            RowsText = sb.ToString();
        }
    }
}