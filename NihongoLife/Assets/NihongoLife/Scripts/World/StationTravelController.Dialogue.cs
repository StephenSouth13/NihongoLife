using System.Collections;
using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NihongoLife.World
{
    /// <summary>Partial of StationTravelController: ticket-clerk and passenger conversations. The MonoBehaviour entry point stays StationTravelController.cs.</summary>
    public sealed partial class StationTravelController : MonoBehaviour
    {
        // ─────────── Conversations ───────────

        private void TalkStationStaff()
        {
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) return;
            string staff = Staff;
            if (HasTicket)
            {
                dm.StartConversation(new List<ScenarioNode>
                {
                    Line_("st_has", staff, $"{TicketStop.Ja}ゆきの きっぷ ですね。かいさつは あちら、{Platform}ばんせん です。", $"{TicketStop.Ja}ゆきの きっぷ ですね。かいさつは あちら、にばんせん です。",
                        ($"Vé đi {TicketStop.Vi} nhỉ. Cổng soát vé ở đằng kia, sân ga số {Platform}.", $"A ticket to {TicketStop.Vi}. The ticket gate is over there, platform {Platform}."), null),
                }, "st_has", null);
                return;
            }

            var nodes = new List<ScenarioNode>
            {
                Line_("st_hello", staff, "いらっしゃいませ。どちらまで ですか。", "いらっしゃいませ。どちらまで ですか。", ("Xin chào quý khách. Quý khách đi đến đâu ạ?", "Hello. Where are you travelling to?"), null,
                    Choice("ミナトえきへ いきたいです。", ("Tôi muốn đến ga Minato.", "I'd like to go to Minato Station."), "st_fare_minato"),
                    Choice("がくえんまえへ いきたいです。", ("Tôi muốn đến ga Gakuen-mae.", "I'd like to go to Gakuen-mae Station."), "st_fare_gakuen"),
                    Choice("みどりじまへ いきたいです。", ("Tôi muốn đến Đảo Xanh (Midori).", "I'd like to go to Midori Island."), "st_fare_midori"),
                    Choice("くうこうへ いきたいです。", ("Tôi muốn ra sân bay (bay đi Phú Quốc).", "I'd like to go to the airport (flight to Phú Quốc)."), "st_fare_airport"),
                    Choice("ミナト、どこ？", ("Minato, đâu? (cộc lốc)", "Minato, where? (blunt)"), "st_polite"),
                    Choice("すみません、だいじょうぶです。", ("Xin lỗi, không cần đâu ạ.", "Sorry, I'm fine, thanks."), "st_bye")),
                Line_("st_polite", staff, "「〜へ いきたいです」と いうと ていねいですよ。", "「〜へ いきたいです」と いうと ていねいですよ。", ("Nói 「〜へ いきたいです」 sẽ lịch sự hơn đó.", "Saying 「〜へ いきたいです」 is more polite."), "st_hello"),
                FareLine("minato", staff), FareLine("gakuen", staff), FareLine("midori", staff), FareLine("airport", staff),
                Line_("st_wrong", staff, $"いいえ、{Platform}ばんせん です。3ばんせんは はんたいほうこう ですよ。", "いいえ、にばんせん です。", ($"Không, là sân ga số {Platform}. Số 3 là chiều ngược lại đó.", $"No, it's platform {Platform}. Platform 3 goes the other way."), "st_hello"),
                Line_("st_machine", staff, "では、あちらの けんばいきで どうぞ。", "では、あちらの けんばいきで どうぞ。", ("Vậy mời quý khách mua ở máy bán vé đằng kia.", "Then please buy one at the ticket machine over there."), null),
                Line_("st_bye", staff, "はい、どうぞ おきをつけて。", "はい、どうぞ おきをつけて。", ("Vâng, quý khách đi cẩn thận.", "Certainly. Have a safe trip."), null),
            };
            dm.StartConversation(nodes, "st_hello", ended =>
            {
                if (string.IsNullOrEmpty(ended) || ended == "cancel") return;
                bool buying = ended.StartsWith("buy:");
                if (ended == "st_machine" || buying) CompleteObjective("obj_ask");
                PlayerStatus.Instance?.AddKnowledge(2);
                if (buying) StartCoroutine(SellAtCounter(ended.Substring("buy:".Length)));
            });
        }

        private ScenarioNode FareLine(string stopId, string staff)
        {
            Stop stop = System.Array.Find(Line, s => s.Id == stopId);
            string yenReading = stop.Price switch { 320 => "さんびゃくにじゅうえん", 450 => "よんひゃくごじゅうえん", 1500 => "せんごひゃくえん", _ => "ひゃくはちじゅうえん" };
            return Line_("st_fare_" + stopId, staff, $"{stop.Ja}までは {stop.Price}えん、{Platform}ばんせん です。きっぷを おかいに なりますか。",
                $"{stop.Ja}までは {yenReading}、にばんせん です。", ($"Đến {stop.Vi} là {stop.Price} yên, sân ga số {Platform}. Quý khách mua vé luôn không ạ?", $"To {stop.Vi} it's {stop.Price} yen, platform {Platform}. Would you like a ticket now?"), null,
                Choice("きっぷを いちまい ください。", ($"Cho tôi một vé ạ. (¥{stop.Price})", $"One ticket, please. (¥{stop.Price})"), "buy:" + stopId),
                Choice("けんばいきで かいます。", ("Tôi sẽ mua ở máy bán vé.", "I'll buy one at the machine."), "st_machine"),
                Choice("3ばんせん ですか。", ("Sân ga số 3 ạ?", "Platform 3?"), "st_wrong"));
        }

        private IEnumerator SellAtCounter(string stopId)
        {
            yield return null; // let the first conversation close
            Stop stop = System.Array.Find(Line, s => s.Id == stopId);
            string staff = Staff;
            bool sold = PurchaseTicket(stopId);
            var nodes = sold
                ? new List<ScenarioNode>
                {
                    Line_("st_sold", staff, $"{stop.Price}えん ちょうだい します。はい、{stop.Ja}ゆきの きっぷ です。{Platform}ばんせんへ どうぞ。",
                        $"{stop.Price}えん ちょうだい します。", ($"Xin nhận {stop.Price} yên. Đây là vé đi {stop.Vi}. Mời quý khách ra sân ga số {Platform}.", $"That's {stop.Price} yen. Here is your ticket to {stop.Vi}. Please go to platform {Platform}."), null,
                        Choice("ありがとうございます。", ("Cảm ơn anh ạ.", "Thank you."), "st_thanks")),
                    Line_("st_thanks", staff, "いってらっしゃいませ。", "いってらっしゃいませ。", ("Chúc quý khách đi vui vẻ.", "Have a nice trip."), null),
                }
                : new List<ScenarioNode>
                {
                    Line_("st_nomoney", staff, "もうしわけありません、おかねが たりない ようです。", "もうしわけありません、おかねが たりない ようです。",
                        ($"Xin lỗi, có vẻ quý khách không đủ tiền (vé ¥{stop.Price}).", $"I'm sorry, it seems you don't have enough money (the ticket is ¥{stop.Price})."), null),
                };
            DialogueManager.Instance?.StartConversation(nodes, nodes[0].id, null);
        }

        public void TalkToPassenger(string passengerId)
        {
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) return;
            SetWindowView(false);
            List<ScenarioNode> nodes = passengerId switch
            {
                "grandma" => new List<ScenarioNode>
                {
                    Line_("ps_q", Speaker("Bà cụ", "Elderly woman", "おばあさん"), "ミナトの さかなは おいしいですよ。あなたも ミナトへ いきますか。", "ミナトの さかなは おいしいですよ。",
                        ("Cá ở Minato ngon lắm đó. Cháu cũng đi Minato à?", "The fish in Minato is delicious. Are you going to Minato too?"), null,
                        Choice("はい、すしを たべに いきます。", ("Vâng, cháu đi ăn sushi ạ.", "Yes, I'm going to eat sushi."), "ps_ok"),
                        Choice("いいえ、がくえんまえで おります。", ("Không, cháu xuống ở Gakuen-mae ạ.", "No, I'm getting off at Gakuen-mae."), "ps_school")),
                    Line_("ps_ok", Speaker("Bà cụ", "Elderly woman", "おばあさん"), "いいですね！ひばりずしが おすすめ ですよ。", "いいですね！", ("Hay quá! Bà gợi ý quán Sushi Hibari đó.", "How nice! I recommend Sushi Hibari."), null),
                    Line_("ps_school", Speaker("Bà cụ", "Elderly woman", "おばあさん"), "べんきょう、がんばって くださいね。", "べんきょう、がんばって くださいね。", ("Học hành chăm chỉ nhé cháu.", "Good luck with your studies."), null),
                },
                "worker" => new List<ScenarioNode>
                {
                    Line_("ps_q", Speaker("Nhân viên văn phòng", "Office worker", "かいしゃいん"), "すみません、いま なんじ ですか。", "すみません、いま なんじ ですか。", ("Xin lỗi, bây giờ là mấy giờ ạ?", "Excuse me, what time is it now?"), null,
                        Choice("ちょっと まって ください…くじ です。", ("Chờ chút ạ… 9 giờ.", "Just a moment… it's 9 o'clock."), "ps_ok"),
                        Choice("しりません。", ("Không biết.", "Don't know."), "ps_meh")),
                    Line_("ps_ok", Speaker("Nhân viên văn phòng", "Office worker", "かいしゃいん"), "ありがとうございます。まにあいそう です。", "ありがとうございます。", ("Cảm ơn bạn. Chắc là kịp giờ rồi.", "Thank you. Looks like I'll make it in time."), null),
                    Line_("ps_meh", Speaker("Nhân viên văn phòng", "Office worker", "かいしゃいん"), "そうですか…。", "そうですか…。", ("Vậy à…", "I see…"), null),
                },
                _ => _riding || _atStop
                    ? new List<ScenarioNode>
                    {
                        Line_("ps_q", Speaker("Học sinh", "Student", "がくせい"), $"すみません、つぎは {NextStop.Ja} ですか。", $"すみません、つぎは {NextStop.Ja} ですか。",
                            ($"Xin lỗi, ga tiếp theo là {NextStop.Vi} phải không?", $"Excuse me, is the next stop {NextStop.Vi}?"), null,
                            Choice($"はい、つぎは {NextStop.Ja} です。", ($"Vâng, ga tiếp theo là {NextStop.Vi}.", $"Yes, the next stop is {NextStop.Vi}."), "ps_ok"),
                            Choice("わかりません。", ("Tôi không biết.", "I don't know."), "ps_meh")),
                        Line_("ps_ok", Speaker("Học sinh", "Student", "がくせい"), "よかった！ありがとうございます。", "よかった！", ("May quá! Cảm ơn bạn.", "Phew! Thank you."), null),
                        Line_("ps_meh", Speaker("Học sinh", "Student", "がくせい"), "アナウンスを きいて みましょう。", "アナウンスを きいて みましょう。", ("Thử nghe thông báo trên tàu xem.", "Let's listen to the train announcement."), null),
                    }
                    : new List<ScenarioNode>
                    {
                        Line_("ps_q", Speaker("Hành khách", "Passenger", "じょうきゃく"), $"この でんしゃは ミナトへ いきますか。", "この でんしゃは ミナトへ いきますか。", ("Tàu này có đi Minato không?", "Does this train go to Minato?"), null,
                            Choice($"はい、{Platform}ばんせんの でんしゃです。", ($"Có, là tàu ở sân ga số {Platform}.", $"Yes, it's the train on platform {Platform}."), "ps_ok"),
                            Choice("いいえ。", ("Không.", "No."), "ps_meh")),
                        Line_("ps_ok", Speaker("Hành khách", "Passenger", "じょうきゃく"), "たすかりました。ありがとう！", "たすかりました。ありがとう！", ("May quá. Cảm ơn nhé!", "What a relief. Thanks!"), null),
                        Line_("ps_meh", Speaker("Hành khách", "Passenger", "じょうきゃく"), "えっ、そうですか…。えきいんさんに きいて みます。", "えっ、そうですか…。", ("Ơ, vậy sao… Tôi sẽ hỏi nhân viên ga.", "Oh, really… I'll ask the station staff."), null),
                    },
            };
            dm.StartConversation(nodes, "ps_q", ended => { if (ended == "ps_ok") PlayerStatus.Instance?.AddKnowledge(2); });
        }

        private static ScenarioNode Line_(string id, string speaker, string ja, string reading, (string vi, string en) meaning, string next, params DialogueChoice[] choices) => new ScenarioNode
        {
            id = id, nodeType = ScenarioNodeType.Dialogue, speakerId = "station_" + id, speakerName = speaker,
            textJa = ja, textReading = reading, textVi = meaning.vi, textEn = meaning.en, nextNodeId = next,
            choices = new List<DialogueChoice>(choices)
        };

        private static DialogueChoice Choice(string ja, (string vi, string en) meaning, string next) => new DialogueChoice { textJa = ja, textVi = meaning.vi, textEn = meaning.en, nextNodeId = next };

        private static string Staff => Speaker("Kimura (nhân viên ga)", "Kimura (station staff)", "えきいん・きむら");

        /// <summary>A speaker label in the interface language (the Japanese role word is part of the Japanese name).</summary>
        private static string Speaker(string vi, string en, string ja) => NihongoLife.Core.Loc.Pick(vi, en, ja);

        private static void CompleteObjective(string objectiveId)
        {
            var scenario = ScenarioManager.Instance;
            if (scenario != null && scenario.CurrentScenario != null && scenario.CurrentScenario.id == ScenarioId)
                scenario.CompleteObjective(objectiveId);
        }

        private float SecondsUntilArrival() => IsTrainBoarding ? 0f : serviceInterval - Phase;
    }
}
