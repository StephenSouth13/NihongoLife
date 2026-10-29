using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Progression;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Music
{
    /// <summary>
    /// The Hibiki music shop (ひびき楽器店, scene 55_MusicShop): try the pianos and guitars (MusicStudioUI), read the
    /// theory books, look at the gallery, play a gig on the stage (the character performs; 80–125 % tempo pleases — MusicStudioUI.Perform),
    /// mix a DJ set (choose the next record by tempo and key — harmonic mixing on the Camelot wheel) and paint by numbers
    /// at the easel (PaintingUI). The careers that work here: musician (job_music_gig), producer / DJ (job_dj_set),
    /// painter (job_art_commission); the owner, Mr Hibiki, takes the reports at the counter.
    /// </summary>
    public static class MusicShop
    {
        public const string OwnerId = "npc_music_owner";
        public const string GigQuest = "job_music_gig";
        public const string DjQuest = "job_dj_set";
        public const string ArtQuest = "job_art_commission";
        private static readonly string[] Careers = { "musician", "producer", "painter" };
        private const string Owner = "ひびき てんちょう";

        public static string PromptJa(MusicSpotKind kind) => kind switch
        {
            MusicSpotKind.Piano => "ピアノを ひく",
            MusicSpotKind.Guitar => "ギターを ひく",
            MusicSpotKind.Theory => "がくふを よむ",
            MusicSpotKind.Gallery => "えを みる",
            MusicSpotKind.Stage => "ステージで えんそう",
            MusicSpotKind.DjBooth => "DJ を する",
            MusicSpotKind.Easel => "えを かく",
            _ => "てんちょうと はなす",
        };

        public static string Prompt(MusicSpotKind kind) => kind switch
        {
            MusicSpotKind.Piano => Loc.T("Chơi piano", "Play the piano", "ピアノを ひく"),
            MusicSpotKind.Guitar => Loc.T("Chơi guitar", "Play a guitar", "ギターを ひく"),
            MusicSpotKind.Theory => Loc.T("Sách nhạc lý", "Music theory books", "音楽理論の本"),
            MusicSpotKind.Gallery => Loc.T("Xem phòng tranh", "Look at the gallery", "ギャラリーを 見る"),
            MusicSpotKind.Stage => Loc.T("Biểu diễn trên sân khấu", "Perform on the stage", "ステージで 演奏"),
            MusicSpotKind.DjBooth => Loc.T("Chơi DJ", "Play a DJ set", "DJ を する"),
            MusicSpotKind.Easel => Loc.T("Vẽ tranh", "Paint", "絵を かく"),
            _ => Loc.T("Nói chuyện với chủ tiệm", "Talk to the owner", "店長と 話す"),
        };

        public static void Use(MusicSpotKind kind, MusicSpot spot = null)
        {
            switch (kind)
            {
                case MusicSpotKind.Piano: TakeSeat(spot, false); MusicStudioUI.Open(MusicStudioUI.Tab.Songs); break;
                case MusicSpotKind.Guitar: TakeSeat(spot, true); MusicStudioUI.Open(MusicStudioUI.Tab.Songs); break;
                case MusicSpotKind.Theory: MusicStudioUI.Open(MusicStudioUI.Tab.Theory); break;
                case MusicSpotKind.Gallery: MusicStudioUI.Open(MusicStudioUI.Tab.Gallery); break;
                case MusicSpotKind.Stage: TakeSeat(spot, true); MusicStudioUI.OpenStage(); break;
                case MusicSpotKind.DjBooth: NextMix(); break;
                case MusicSpotKind.Easel: PaintingUI.Open(); break;
                default: TalkToOwner(); break;
            }
        }

        /// <summary>The player goes to the instrument (the piano bench, the stage's microphone) and becomes the performer:
        /// the songs picked next are played by the character, hands following the beat, the camera turned to watch.</summary>
        private static void TakeSeat(MusicSpot spot, bool guitar)
        {
            var player = SceneRefs.Find<NihongoLife.Player.PlayerController>();
            if (player == null) return;
            if (spot != null && spot.HasSeat)
            {
                var controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.transform.SetPositionAndRotation(spot.Seat, Quaternion.Euler(0f, spot.SeatYaw, 0f));
                if (controller != null) controller.enabled = true;
                var camera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
                // At the piano: from the right, the hands and the keys in view. On the stage: from the audience, a little to the side.
                if (camera != null) camera.SetOrbit(spot.SeatYaw + (spot.Kind == MusicSpotKind.Stage ? 165f : -70f), 16f, spot.Kind == MusicSpotKind.Stage ? 4.2f : 3.2f);
            }
            MusicStudioUI.SetPerformer(player.gameObject, guitar);
        }

        /// <summary>The owner: takes the report of a finished gig / set / commission (pay), otherwise says what the shop offers.</summary>
        public static void TalkToOwner()
        {
            foreach (var quest in QuestService.Active.ToList())
            {
                if (quest == null || !(quest.id == GigQuest || quest.id == DjQuest || quest.id == ArtQuest || Careers.Contains(quest.career))) continue;
                if (!QuestService.ReadyToReport(quest.id)) continue;
                QuestService.Raise("talk", OwnerId);
                HudFeed.Post("「おつかれさま！ いい しごとでした。」 — " + Loc.T("Chủ tiệm Hibiki trả công cho bạn.", "Mr Hibiki pays you for the work.", "店長がお礼を言いました。"), HudFeed.Kind.Reward, 5f, "music");
                return;
            }
            HudFeed.Post("「いらっしゃいませ！」 — " + Loc.T(
                "Cứ thử đàn thoải mái: chọn bài là nhân vật tự đàn. Lên sân khấu diễn với nhịp 80–125%; bàn DJ và giá vẽ ở bên kia. Việc làm thêm ở bảng cạnh cửa.",
                "Try any instrument: pick a song and your character plays it. Perform on the stage at 80–125% tempo; the DJ booth and the easel are over there. Jobs are on the boards by the door.",
                "楽器は 自由に どうぞ。曲を えらぶと 演奏します。ステージは テンポ 80〜125% で。DJブースと イーゼルも あります。"), HudFeed.Kind.Info, 7f, "music");
        }

        // ─────────── The DJ booth: harmonic mixing ───────────

        /// <summary>Camelot wheel: number 1–12, A = minor, B = major. Neighbours mix smoothly: the same number (relative
        /// major / minor), or ±1 with the same letter. The tempo must stay within about ±6 % to beat-match.</summary>
        private static readonly string[] MinorKeys = { "A♭m", "E♭m", "B♭m", "Fm", "Cm", "Gm", "Dm", "Am", "Em", "Bm", "F♯m", "D♭m" };
        private static readonly string[] MajorKeys = { "B", "F♯", "D♭", "A♭", "E♭", "B♭", "F", "C", "G", "D", "A", "E" };
        private static readonly string[] Titles = { "Sakura Drive", "Neon Hibari", "Last Train Home", "Rainy Crossing", "Kissa Groove", "Harbour Lights", "Midnight Konbini", "Summer Matsuri", "Paper Lanterns", "Tokyo Bay Breeze", "Morning Tram", "Shrine Steps" };

        public readonly struct Track
        {
            public readonly string Title; public readonly int Number; public readonly bool Major; public readonly int Bpm;
            public Track(string title, int number, bool major, int bpm) { Title = title; Number = number; Major = major; Bpm = bpm; }
            public string Key => (Major ? MajorKeys : MinorKeys)[Number - 1];
            public string Camelot => Number + (Major ? "B" : "A");
            public string Label => $"{Title} · {Bpm} BPM · {Key} ({Camelot})";
        }

        private static int Wrap(int n) => ((n - 1) % 12 + 12) % 12 + 1;

        /// <summary>True when the next record mixes smoothly into the current one (key and tempo).</summary>
        public static bool Compatible(Track from, Track to)
        {
            bool key = (from.Number == to.Number) || (from.Major == to.Major && (Wrap(from.Number + 1) == to.Number || Wrap(from.Number - 1) == to.Number));
            return key && Mathf.Abs(to.Bpm - from.Bpm) <= from.Bpm * 0.06f;
        }

        /// <summary>Tests and the booth: a playing record and three candidates, exactly one of which mixes in.</summary>
        public static (Track playing, Track[] options, int right) MakeMix(System.Random random)
        {
            var titles = Titles.OrderBy(_ => random.Next()).ToArray();
            var playing = new Track(titles[0], random.Next(1, 13), random.Next(2) == 0, random.Next(110, 129));
            int step = random.Next(3); // same number (relative key), +1 or −1 on the wheel
            var good = step == 0 ? new Track(titles[1], playing.Number, !playing.Major, playing.Bpm + random.Next(-3, 4))
                : new Track(titles[1], Wrap(playing.Number + (step == 1 ? 1 : -1)), playing.Major, playing.Bpm + random.Next(-3, 4));
            var clash = new Track(titles[2], Wrap(playing.Number + 5 + random.Next(3)), playing.Major, playing.Bpm + random.Next(-2, 3)); // far round the wheel
            var tempo = new Track(titles[3], playing.Number, playing.Major, playing.Bpm + (random.Next(2) == 0 ? -30 : 34)); // right key, wrong speed
            var options = new[] { good, clash, tempo }.OrderBy(_ => random.Next()).ToArray();
            return (playing, options, System.Array.IndexOf(options, good));
        }

        private static System.Random _random = new System.Random();

        /// <summary>One DJ decision: which record goes next? The right one raises "mix" (the DJ set's objective).</summary>
        public static void NextMix()
        {
            var (playing, options, _) = MakeMix(_random);
            var choices = new List<JobQuizCard.Choice>();
            foreach (var track in options)
            {
                bool ok = Compatible(playing, track);
                string hint = ok ? null
                    : track.Number == playing.Number && track.Major == playing.Major
                        ? Loc.F("Đúng giọng nhưng {0} BPM chênh quá xa {1} BPM — không khớp nhịp được.", "Right key, but {0} BPM is too far from {1} BPM to beat-match.", "キーは合うけど、{0} BPM は {1} BPM から はなれすぎ。", track.Bpm, playing.Bpm)
                        : Loc.F("{0} cách {1} quá xa trên vòng Camelot — hai giọng sẽ chỏi nhau.", "{0} is too far from {1} on the Camelot wheel — the keys clash.", "{0} と {1} は キーが ぶつかります。", track.Camelot, playing.Camelot);
                choices.Add(new JobQuizCard.Choice { Label = track.Label, Correct = ok, WrongHint = hint });
            }
            JobQuizCard.Show("DJ · ミックス", "DJ ソウタ", "つぎの きょくは どれに しますか？", "Tsugi no kyoku wa dore ni shimasu ka?",
                Loc.T("Bài tiếp theo chọn bài nào?", "Which record goes next?", ""),
                Loc.F("Đang phát: {0}. Chọn bài hoà âm được (cùng số, hoặc ±1 cùng chữ trên vòng Camelot) và nhịp chênh ≤ 6%.",
                    "Playing: {0}. Pick the one that mixes: the same number, or ±1 with the same letter on the Camelot wheel, tempo within 6%.",
                    "再生中：{0}。カメロットで同じ数字か ±1（同じ文字）、テンポ差 6% 以内の曲を。", playing.Label),
                choices, () => QuestService.Raise("mix", "music_dj"));
        }
    }
}
