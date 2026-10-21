using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Scenario;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// The けいひん (prize) counter: Aoi swaps the チケット earned in mini-games for prizes, in Japanese
    /// ("〜を ください", "〜まい です"), so the arcade loop ends in one more short conversation.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PrizeCounter : MonoBehaviour, IInteractable, IInteractionPriority
    {
        public sealed class Prize
        {
            public Prize(string id, string ja, string vi, string en, int cost) { Id = id; Ja = ja; Vi = vi; En = en; Cost = cost; }
            public string Id { get; }
            public string Ja { get; }
            public string Vi { get; }
            public string En { get; }
            public int Cost { get; }
            public string Name => Loc.Pick(Vi, En, Ja);
        }

        public static readonly Prize[] Prizes =
        {
            new Prize("prize_plush_big", "おおきい くまの ぬいぐるみ", "Gấu bông khổng lồ", "Giant teddy bear", 40),
            new Prize("prize_plush", "くまの ぬいぐるみ", "Gấu bông", "Teddy bear", 12),
            new Prize("prize_neko_plush", "ねこの ぬいぐるみ", "Mèo bông", "Cat plush", 12),
            new Prize("prize_daruma", "だるま", "Búp bê Daruma", "Daruma doll", 18),
            new Prize("prize_furin", "ふうりん", "Chuông gió Fūrin", "Fūrin wind chime", 15),
            new Prize("prize_keychain", "キーホルダー", "Móc khoá", "Keychain", 6),
            new Prize("prize_sticker", "シール", "Hình dán", "Stickers", 4),
            new Prize("prize_snack", "おかしの はこ", "Hộp bánh kẹo", "Box of sweets", 3),
        };

        private static string Staff => Loc.T("Aoi (quầy quà)", "Aoi (prize counter)", "あおいさん");
        public float InteractionPriority => 0.3f;
        public string GetPromptJa() => "けいひんと こうかん";
        public string GetpromptEn() => Loc.T("Đổi vé thưởng lấy quà", "Swap tickets for prizes", "けいひんと こうかん");
        public Transform GetTransform() => transform;

        public static int Tickets => PlayerInventory.Instance != null ? PlayerInventory.Instance.GetItemQuantity(KanaMatchGame.TicketItemId) : 0;

        public void Interact(GameObject player)
        {
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) return;
            var choices = new List<DialogueChoice>();
            foreach (var prize in Prizes)
                choices.Add(new DialogueChoice { textJa = $"{prize.Ja}を ください。（{prize.Cost}まい）", textVi = $"{prize.Vi} — {prize.Cost} vé", textEn = $"{prize.En} — {prize.Cost} tickets", nextNodeId = "buy:" + prize.Id });
            choices.Add(new DialogueChoice { textJa = "けっこうです。", textVi = "Thôi, không cần ạ.", textEn = "No, thank you.", nextNodeId = "bye" });
            dm.StartConversation(new[]
            {
                Line("hello", $"いらっしゃいませ！チケットは {Tickets}まい ですね。どれに しますか。", $"Xin chào! Bạn đang có {Tickets} vé. Bạn muốn đổi gì?", $"Welcome! You have {Tickets} tickets. What would you like?", choices),
                Line("bye", "また あそびに きてね！", "Lần sau lại ghé chơi nhé!", "Come and play again!", null),
            }, "hello", ended =>
            {
                if (string.IsNullOrEmpty(ended) || !ended.StartsWith("buy:")) return;
                Exchange(ended.Substring(4));
            });
        }

        /// <summary>Swaps tickets for a prize. Returns false when the player does not have enough.</summary>
        public static bool Exchange(string prizeId)
        {
            var prize = System.Array.Find(Prizes, p => p.Id == prizeId);
            var inventory = PlayerInventory.Instance;
            if (prize == null || inventory == null) return false;
            bool enough = Tickets >= prize.Cost;
            if (enough)
            {
                inventory.RemoveItem(KanaMatchGame.TicketItemId, prize.Cost);
                inventory.AddItem(prize.Id, prize.Ja, Loc.Triple(prize.Vi + " (quà Game Center)", prize.En + " (Game Center prize)", prize.Ja + "（けいひん）"), 0, 1);
            }
            DialogueManager.Instance?.StartConversation(new[]
            {
                enough
                    ? Line("done", $"はい、{prize.Ja} です。おめでとう！", $"Đây là {prize.Vi} của bạn. Chúc mừng! (−{prize.Cost} vé)", $"Here is your {prize.En}. Congratulations! (−{prize.Cost} tickets)", null)
                    : Line("short", $"ごめんなさい、チケットが {prize.Cost - Tickets}まい たりません。", $"Xin lỗi, bạn còn thiếu {prize.Cost - Tickets} vé. Chơi các máy game để kiếm thêm nhé!", $"Sorry, you are {prize.Cost - Tickets} tickets short. Play the machines to earn more!", null)
            }, enough ? "done" : "short", null);
            return enough;
        }

        private static ScenarioNode Line(string id, string ja, string vi, string en, List<DialogueChoice> choices) => new ScenarioNode
        {
            id = id, nodeType = ScenarioNodeType.Dialogue, speakerName = Staff,
            textJa = ja, textReading = ja, textVi = vi, textEn = en, choices = choices ?? new List<DialogueChoice>()
        };
    }
}
