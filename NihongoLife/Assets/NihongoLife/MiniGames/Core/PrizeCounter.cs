using System.Collections.Generic;
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
            public Prize(string id, string ja, string vi, int cost) { Id = id; Ja = ja; Vi = vi; Cost = cost; }
            public string Id { get; }
            public string Ja { get; }
            public string Vi { get; }
            public int Cost { get; }
        }

        public static readonly Prize[] Prizes =
        {
            new Prize("prize_plush", "くまの ぬいぐるみ", "Gấu bông", 12),
            new Prize("prize_keychain", "キーホルダー", "Móc khoá", 6),
            new Prize("prize_snack", "おかしの はこ", "Hộp bánh kẹo", 3),
        };

        private const string Staff = "あおい · Aoi (quầy quà)";
        public float InteractionPriority => 0.3f;
        public string GetPromptJa() => "けいひんと こうかん";
        public string GetpromptEn() => "Đổi vé thưởng lấy quà";
        public Transform GetTransform() => transform;

        public static int Tickets => PlayerInventory.Instance != null ? PlayerInventory.Instance.GetItemQuantity(KanaMatchGame.TicketItemId) : 0;

        public void Interact(GameObject player)
        {
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) return;
            var choices = new List<DialogueChoice>();
            foreach (var prize in Prizes)
                choices.Add(new DialogueChoice { textJa = $"{prize.Ja}を ください。", textEn = $"{prize.Vi} — {prize.Cost} vé", nextNodeId = "buy:" + prize.Id });
            choices.Add(new DialogueChoice { textJa = "けっこうです。", textEn = "Thôi, không cần ạ.", nextNodeId = "bye" });
            dm.StartConversation(new[]
            {
                Line("hello", $"いらっしゃいませ！チケットは {Tickets}まい ですね。どれに しますか。", $"Xin chào! Bạn đang có {Tickets} vé. Bạn muốn đổi gì?", choices),
                Line("bye", "また あそびに きてね！", "Lần sau lại ghé chơi nhé!", null),
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
                inventory.AddItem(prize.Id, prize.Ja, prize.Vi + " (quà Game Center)", 0, 1);
            }
            DialogueManager.Instance?.StartConversation(new[]
            {
                enough
                    ? Line("done", $"はい、{prize.Ja} です。おめでとう！", $"Đây là {prize.Vi} của bạn. Chúc mừng! (−{prize.Cost} vé)", null)
                    : Line("short", $"ごめんなさい、チケットが {prize.Cost - Tickets}まい たりません。", $"Xin lỗi, bạn còn thiếu {prize.Cost - Tickets} vé. Chơi Kana Match để kiếm thêm nhé!", null)
            }, enough ? "done" : "short", null);
            return enough;
        }

        private static ScenarioNode Line(string id, string ja, string vi, List<DialogueChoice> choices) => new ScenarioNode
        {
            id = id, nodeType = ScenarioNodeType.Dialogue, speakerName = Staff,
            textJa = ja, textReading = ja, textEn = vi, choices = choices ?? new List<DialogueChoice>()
        };
    }
}
