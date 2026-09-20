using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// "How to play" popup: four numbered step cards and a shortcut strip whose key labels are read from
    /// the live input bindings, so the guide stays correct after the player rebinds keys.
    /// </summary>
    public class GuidePopup : MenuPopupBase
    {
        private class Shortcut
        {
            public GameInputId? id;
            public string fixedKey;
            public string vi;
            public string en;
            public string ja;
            public TextMeshProUGUI keyText;
        }

        private class Step
        {
            public string titleVi, titleEn, titleJa;
            public string bodyVi, bodyEn, bodyJa;
            public TextMeshProUGUI body;
        }

        private readonly List<Shortcut> _shortcuts = new List<Shortcut>();
        private readonly List<Step> _steps = new List<Step>();

        protected override Vector2 CardSize => new Vector2(1100f, 660f);

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Cách chơi";
            en = "How to play";
            ja = "遊び方";
        }

        protected override void Build(RectTransform card)
        {
            AddLocalizedText(card, "Bắt đầu chỉ trong 4 bước", "Get started in 4 steps", "4ステップで始めよう", 18f, 38f, 88f, 700f, 30f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false, Muted);

            _steps.Add(new Step
            {
                titleVi = "Khám phá khu phố", titleEn = "Explore the town", titleJa = "町を歩く",
                bodyVi = "Đi quanh khu phố để tìm NPC, cửa hàng hoặc điểm nhiệm vụ. Mũi tên và bảng nhiệm vụ sẽ chỉ hướng.",
                bodyEn = "Walk around town to find NPCs, shops and mission markers. The arrow and the mission panel point the way.",
                bodyJa = "町を歩いて NPC・お店・ミッション地点を探します。矢印とミッション欄が道案内します。"
            });
            _steps.Add(new Step
            {
                titleVi = "Trò chuyện bằng tiếng Nhật", titleEn = "Talk in Japanese", titleJa = "日本語で話す",
                bodyVi = "Nhấn {Interact} để nói chuyện, rồi chọn câu đáp N5 phù hợp. Chọn sai thì NPC sẽ nhẹ nhàng sửa và bạn thử lại.",
                bodyEn = "Press {Interact} to talk, then pick a fitting N5 reply. If you pick a wrong one, the NPC gently corrects you and you try again.",
                bodyJa = "{Interact} で話しかけ、N5 の返事を選びます。間違えても NPC が優しく直してくれるので、もう一度挑戦できます。"
            });
            _steps.Add(new Step
            {
                titleVi = "Luyện tập và quản lý", titleEn = "Practice and manage", titleJa = "練習と管理",
                bodyVi = "Mở balo bằng {Inventory}, xem nhân vật bằng {Character}, giữ {Voice} để luyện phát âm. Nhớ ăn uống để không kiệt sức.",
                bodyEn = "Open your bag with {Inventory}, your profile with {Character}, hold {Voice} to practise speaking. Remember to eat and drink.",
                bodyJa = "{Inventory} でバッグ、{Character} でプロフィール、{Voice} で発音練習。食事と水分も忘れずに。"
            });
            _steps.Add(new Step
            {
                titleVi = "Học, nhận XP, lên cấp", titleEn = "Learn, earn XP, level up", titleJa = "学んで、XP をもらう",
                bodyVi = "Nghe trước, đọc gợi ý khi cần, hoàn thành tình huống để nhận XP, mở khóa nhiệm vụ mới và ghé nhà hàng, ga tàu, trường học.",
                bodyEn = "Listen first, read hints when needed, finish scenes to earn XP, unlock new missions and visit the restaurant, station and school.",
                bodyJa = "まず聞いて、必要ならヒントを読み、場面をクリアして XP を獲得。新しいミッションやレストラン・駅・学校も開きます。"
            });

            for (int i = 0; i < _steps.Count; i++)
            {
                int column = i % 2;
                int row = i / 2;
                float x = 38f + column * 530f;
                float y = 128f + row * 186f;
                var step = _steps[i];

                AddRounded(card, "StepCard", x, y, 500f, 170f, Surface);
                AddCircle(card, "Badge", x + 18f, y + 18f, 46f, Gold);
                AddText(card, (i + 1).ToString(), 26f, x + 18f, y + 18f, 46f, 46f, TextAlignmentOptions.Center, FontStyles.Bold, false, DarkText);
                AddLocalizedText(card, step.titleVi, step.titleEn, step.titleJa, 22f, x + 78f, y + 20f, 402f, 36f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
                step.body = AddText(card, string.Empty, 17f, x + 78f, y + 62f, 404f, 100f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
            }

            AddLocalizedText(card, "Phím tắt", "Shortcuts", "ショートカット", 17f, 38f, 506f, 300f, 26f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);

            _shortcuts.Add(new Shortcut { fixedKey = "W A S D", vi = "Di chuyển", en = "Move", ja = "移動" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Sprint, vi = "Chạy", en = "Sprint", ja = "走る" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Interact, vi = "Tương tác", en = "Interact", ja = "話す・調べる" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Inventory, vi = "Balo", en = "Bag", ja = "バッグ" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Character, vi = "Nhân vật", en = "Profile", ja = "プロフィール" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Map, vi = "Bản đồ", en = "Map", ja = "地図" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Chat, vi = "Chat", en = "Chat", ja = "チャット" });
            _shortcuts.Add(new Shortcut { id = GameInputId.Voice, vi = "Luyện nói", en = "Speak", ja = "発音練習" });

            for (int i = 0; i < _shortcuts.Count; i++)
            {
                var shortcut = _shortcuts[i];
                float x = 38f + (i % 4) * 264f;
                float y = 538f + (i / 4) * 54f;
                AddRounded(card, "Chip", x, y, 248f, 44f, Surface);
                AddRounded(card, "KeyBox", x + 6f, y + 6f, 96f, 32f, new Color(0.18f, 0.22f, 0.26f, 1f));
                shortcut.keyText = AddText(card, "-", 15f, x + 6f, y + 6f, 96f, 32f, TextAlignmentOptions.Center, FontStyles.Bold, false, Gold);
                AddLocalizedText(card, shortcut.vi, shortcut.en, shortcut.ja, 16f, x + 112f, y + 4f, 130f, 36f, TextAlignmentOptions.MidlineLeft);
            }
        }

        protected override void OnLanguageApplied()
        {
            var keys = new Dictionary<string, string>();
            foreach (var pair in new[]
            {
                ("{Interact}", GameInputId.Interact), ("{Inventory}", GameInputId.Inventory),
                ("{Character}", GameInputId.Character), ("{Voice}", GameInputId.Voice)
            })
            {
                keys[pair.Item1] = KeyLabel(pair.Item2);
            }

            foreach (var step in _steps)
            {
                if (step.body == null) continue;
                string text = Pick(step.bodyVi, step.bodyEn, step.bodyJa);
                foreach (var pair in keys) text = text.Replace(pair.Key, $"<b><color=#F2B840>{pair.Value}</color></b>");
                step.body.text = text;
            }

            foreach (var shortcut in _shortcuts)
            {
                if (shortcut.keyText == null) continue;
                shortcut.keyText.text = shortcut.id.HasValue ? KeyLabel(shortcut.id.Value) : shortcut.fixedKey;
            }
        }

        private static string KeyLabel(GameInputId id)
        {
            try
            {
                var input = GameInputService.GetOrCreate();
                string label = input != null ? input.GetBindingLabel(id) : null;
                return string.IsNullOrWhiteSpace(label) ? "-" : label;
            }
            catch (Exception)
            {
                return "-";
            }
        }
    }
}
