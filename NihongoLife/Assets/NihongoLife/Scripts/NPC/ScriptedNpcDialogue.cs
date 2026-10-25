using System.Collections.Generic;
using NihongoLife.Scenario;

namespace NihongoLife.NPC
{
    /// <summary>One pre-written NPC line: the Japanese being learned plus its reading, romaji and the Vietnamese and
    /// English glosses. Japanese-interface players see the Japanese itself (no gloss), so every supported interface
    /// language (vi / en / ja) is complete for every line.</summary>
    public sealed class ScriptedNpcLine
    {
        public readonly string Ja, Reading, Romaji, Vi, En;
        public ScriptedNpcLine(string ja, string reading, string romaji, string vi, string en)
        { Ja = ja; Reading = reading; Romaji = romaji; Vi = vi; En = en; }
    }

    /// <summary>
    /// Offline, pre-written small talk for NPCs that have no scenario or service to run. Replaces the AI (Gemini)
    /// replies: no network, no API key, and an NPC always answers. Lines rotate per NPC on each talk.
    /// </summary>
    public static class ScriptedNpcDialogue
    {
        private static ScriptedNpcLine L(string ja, string reading, string romaji, string vi, string en) => new ScriptedNpcLine(ja, reading, romaji, vi, en);

        public static readonly ScriptedNpcLine[] Generic =
        {
            L("こんにちは。今日はいい天気ですね。", "こんにちは。きょうはいいてんきですね。", "Konnichiwa. Kyou wa ii tenki desu ne.", "Xin chào. Hôm nay trời đẹp nhỉ.", "Hello. Nice weather today."),
            L("この町はしずかで、いいところですよ。", "このまちはしずかで、いいところですよ。", "Kono machi wa shizuka de, ii tokoro desu yo.", "Thị trấn này yên tĩnh, là nơi tốt lắm đấy.", "This town is quiet. It's a nice place."),
            L("日本語のべんきょう、がんばってくださいね。", "にほんごのべんきょう、がんばってくださいね。", "Nihongo no benkyou, ganbatte kudasai ne.", "Cố gắng học tiếng Nhật nhé.", "Good luck with your Japanese studies."),
            L("またね。気をつけて。", "またね。きをつけて。", "Mata ne. Ki o tsukete.", "Hẹn gặp lại. Đi cẩn thận nhé.", "See you. Take care."),
        };

        public static readonly Dictionary<string, ScriptedNpcLine[]> ByNpc = new Dictionary<string, ScriptedNpcLine[]>
        {
            ["npc_neighbor_1"] = new[]
            {
                L("おはようございます。きょうもげんきですね。", "おはようございます。きょうもげんきですね。", "Ohayou gozaimasu. Kyou mo genki desu ne.", "Chào buổi sáng. Hôm nay cháu vẫn khỏe nhỉ.", "Good morning. You look well today."),
                L("こまったことがあったら、いってくださいね。", "こまったことがあったら、いってくださいね。", "Komatta koto ga attara, itte kudasai ne.", "Nếu có gì khó khăn thì cứ nói nhé.", "If you have any trouble, just tell me."),
            },
            ["npc_neighbor_2"] = new[]
            {
                L("うちのねこ、見ませんでしたか。", "うちのねこ、みませんでしたか。", "Uchi no neko, mimasen deshita ka.", "Cháu có thấy con mèo nhà cô không?", "Have you seen my cat?"),
                L("いつもありがとうございます。", "いつもありがとうございます。", "Itsumo arigatou gozaimasu.", "Lúc nào cũng cảm ơn cháu.", "Thank you, as always."),
            },
            ["npc_neighbor_3"] = new[]
            {
                L("ゴミは月曜日と木曜日ですよ。", "ごみはげつようびともくようびですよ。", "Gomi wa getsuyoubi to mokuyoubi desu yo.", "Rác được đổ vào thứ Hai và thứ Năm đấy.", "Garbage day is Monday and Thursday."),
                L("町のルール、もうなれましたか。", "まちのるーる、もうなれましたか。", "Machi no ruuru, mou naremashita ka.", "Cháu đã quen với quy định của khu phố chưa?", "Are you used to the town rules yet?"),
            },
            ["npc_cashier"] = new[]
            {
                L("いらっしゃいませ。", "いらっしゃいませ。", "Irasshaimase.", "Xin mời quý khách.", "Welcome!"),
                L("おべんとう、あたためますか。", "おべんとう、あたためますか。", "Obentou, atatamemasu ka.", "Quý khách có muốn hâm nóng cơm hộp không?", "Shall I heat up your bento?"),
            },
            ["npc_teacher_morita"] = new[]
            {
                L("しゅくだいはおわりましたか。", "しゅくだいはおわりましたか。", "Shukudai wa owarimashita ka.", "Em làm xong bài tập chưa?", "Have you finished your homework?"),
                L("わからないことは、しつもんしてください。", "わからないことは、しつもんしてください。", "Wakaranai koto wa, shitsumon shite kudasai.", "Chỗ nào không hiểu thì hãy hỏi nhé.", "Please ask about anything you don't understand."),
            },
            ["npc_classmate_kim"] = new[]
            {
                L("いっしょにべんきょうしませんか。", "いっしょにべんきょうしませんか。", "Issho ni benkyou shimasen ka.", "Cùng học với mình không?", "Want to study together?"),
                L("きのうのテスト、むずかしかったね。", "きのうのてすと、むずかしかったね。", "Kinou no tesuto, muzukashikatta ne.", "Bài kiểm tra hôm qua khó nhỉ.", "Yesterday's test was hard, wasn't it?"),
            },
            ["npc_sushi_staff"] = new[]
            {
                L("いらっしゃいませ。なんめいさまですか。", "いらっしゃいませ。なんめいさまですか。", "Irasshaimase. Nanmei-sama desu ka.", "Xin mời. Quý khách đi mấy người ạ?", "Welcome. How many in your party?"),
                L("おちゃはセルフサービスです。", "おちゃはせるふさーびすです。", "Ocha wa serufu saabisu desu.", "Trà là tự phục vụ ạ.", "Tea is self-service."),
            },
            ["npc_sushi_chef"] = new[]
            {
                L("きょうのマグロはおいしいですよ。", "きょうのまぐろはおいしいですよ。", "Kyou no maguro wa oishii desu yo.", "Cá ngừ hôm nay ngon lắm đấy.", "Today's tuna is delicious."),
                L("わさびはだいじょうぶですか。", "わさびはだいじょうぶですか。", "Wasabi wa daijoubu desu ka.", "Quý khách ăn được wasabi không?", "Is wasabi okay for you?"),
            },
            ["npc_ramen_owner"] = new[]
            {
                L("ラーメン、いかがですか。", "らーめん、いかがですか。", "Raamen, ikaga desu ka.", "Dùng một bát ramen không?", "How about some ramen?"),
                L("おおもりもできますよ。", "おおもりもできますよ。", "Oomori mo dekimasu yo.", "Có thể lấy suất lớn đấy.", "We can do a large portion too."),
            },
            ["npc_station_staff"] = new[]
            {
                L("きっぷはあちらのきかいでかえます。", "きっぷはあちらのきかいでかえます。", "Kippu wa achira no kikai de kaemasu.", "Vé có thể mua ở máy đằng kia.", "You can buy tickets at the machine over there."),
                L("つぎのでんしゃは五分後です。", "つぎのでんしゃはごふんごです。", "Tsugi no densha wa go-fun go desu.", "Chuyến tàu tiếp theo sau năm phút nữa.", "The next train is in five minutes."),
            },
            ["npc_guide"] = new[]
            {
                L("わからないことがあったら、きいてね。", "わからないことがあったら、きいてね。", "Wakaranai koto ga attara, kiite ne.", "Nếu có gì không hiểu thì cứ hỏi mình nhé.", "If there's anything you don't understand, ask me."),
                L("まず、ちずをみてみましょう。", "まず、ちずをみてみましょう。", "Mazu, chizu o mite mimashou.", "Trước tiên, hãy thử xem bản đồ nào.", "First, let's take a look at the map."),
            },
            ["npc_farm_manager"] = new[]
            {
                L("やさいのせわ、てつだってくれる?", "やさいのせわ、てつだってくれる?", "Yasai no sewa, tetsudatte kureru?", "Giúp cô chăm rau được không?", "Can you help me look after the vegetables?"),
                L("きょうはいいてんきだから、よくそだつね。", "きょうはいいてんきだから、よくそだつね。", "Kyou wa ii tenki dakara, yoku sodatsu ne.", "Hôm nay trời đẹp nên rau lớn nhanh nhỉ.", "The weather is nice today, so things grow well."),
            },
        };

        private static readonly Dictionary<string, int> _turn = new Dictionary<string, int>();

        /// <summary>The lines for an NPC (its own, or the generic pool). Never null or empty.</summary>
        public static ScriptedNpcLine[] LinesFor(string npcId) =>
            !string.IsNullOrEmpty(npcId) && ByNpc.TryGetValue(npcId, out var lines) && lines != null && lines.Length > 0 ? lines : Generic;

        /// <summary>The next line for this NPC (rotates on each call). Always returns a line.</summary>
        public static ScriptedNpcLine Next(string npcId)
        {
            var lines = LinesFor(npcId);
            string key = npcId ?? string.Empty;
            _turn.TryGetValue(key, out int i);
            _turn[key] = i + 1;
            return lines[((i % lines.Length) + lines.Length) % lines.Length];
        }

        /// <summary>A dialogue node for the next line. Glosses keep both vi and en, so the dialogue box re-renders
        /// in whichever language is chosen (Japanese interface: Japanese only).</summary>
        public static ScenarioNode NextNode(string npcId, string speakerName)
        {
            var line = Next(npcId);
            return new ScenarioNode
            {
                id = "scripted_" + (npcId ?? "npc"),
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = speakerName,
                speakerId = npcId,
                textJa = line.Ja,
                textReading = line.Reading,
                textRomaji = line.Romaji,
                textVi = line.Vi,
                textEn = line.En,
                animationCue = "talk"
            };
        }
    }
}
