using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Interaction;
using NihongoLife.Player;

namespace NihongoLife.Home
{
    [Serializable]
    public sealed class StudyWord
    {
        public string japanese;
        public string reading;
        public string meaning;
        public string distractorA;
        public string distractorB;
        public string example;

        /// <summary>The meaning and the two wrong answers in the interface language (kanji forms in the Japanese interface).</summary>
        public string Meaning => Local(meaning, 0);
        public string DistractorA => Local(distractorA, 1);
        public string DistractorB => Local(distractorB, 2);

        private string Local(string vi, int slot)
        {
            if (NihongoLife.Core.Loc.IsVietnamese || !Others.TryGetValue(japanese, out var row)) return vi;
            return NihongoLife.Core.Loc.EnglishData ? NihongoLife.Core.Loc.Data(row.en[slot]) : row.ja[slot];
        }

        private static readonly System.Collections.Generic.Dictionary<string, (string[] en, string[] ja)> Others = new System.Collections.Generic.Dictionary<string, (string[] en, string[] ja)>
        {
            ["へや"] = (new[] { "room", "window", "station" }, new[] { "部屋", "窓", "駅" }),
            ["つくえ"] = (new[] { "desk", "chair", "bed" }, new[] { "机", "いす", "ベッド" }),
            ["いす"] = (new[] { "chair", "desk", "fridge" }, new[] { "いす（椅子）", "机", "冷蔵庫" }),
            ["ベッド"] = (new[] { "bed", "pillow", "desk" }, new[] { "ベッド（寝台）", "まくら", "机" }),
            ["まど"] = (new[] { "window", "door", "wall" }, new[] { "窓", "ドア", "かべ" }),
            ["ドア"] = (new[] { "door", "window", "light" }, new[] { "とびら（扉）", "窓", "電気" }),
            ["でんき"] = (new[] { "light", "telephone", "TV" }, new[] { "電気", "電話", "テレビ" }),
            ["れいぞうこ"] = (new[] { "fridge", "microwave", "washing machine" }, new[] { "冷蔵庫", "電子レンジ", "洗濯機" }),
            ["ほん"] = (new[] { "book", "newspaper", "pen" }, new[] { "本", "新聞", "ペン" }),
            ["ねる"] = (new[] { "go to bed", "wake up", "eat a meal" }, new[] { "寝る", "起きる", "ごはんを食べる" }),
            ["おきる"] = (new[] { "wake up", "go to bed", "go to school" }, new[] { "起きる", "寝る", "学校へ行く" }),
            ["べんきょうする"] = (new[] { "study", "work", "rest" }, new[] { "勉強する", "働く", "休む" }),
            ["ただいま"] = (new[] { "I'm home", "I'm off", "Good night" }, new[] { "帰ったときのあいさつ", "出かけるときのあいさつ", "寝るときのあいさつ" }),
            ["いってきます"] = (new[] { "I'm off", "I'm home", "Sorry" }, new[] { "出かけるときのあいさつ", "帰ったときのあいさつ", "あやまることば" }),
            ["おやすみなさい"] = (new[] { "Good night", "Good morning", "Good evening" }, new[] { "寝るときのあいさつ", "朝のあいさつ", "夜のあいさつ" }),
            ["あさ"] = (new[] { "morning", "evening", "noon" }, new[] { "朝", "夜", "昼" }),
            ["あした"] = (new[] { "tomorrow", "yesterday", "today" }, new[] { "明日", "昨日", "今日" }),
            ["みず"] = (new[] { "water", "tea", "milk" }, new[] { "水", "お茶", "牛乳" }),
        };

        public StudyWord(string japanese, string reading, string meaning, string distractorA, string distractorB, string example)
        {
            this.japanese = japanese;
            this.reading = reading;
            this.meaning = meaning;
            this.distractorA = distractorA;
            this.distractorB = distractorB;
            this.example = example;
        }
    }

    /// <summary>
    /// The white desk from the intro scene (「白い机とベッドがあります」). F → a 5-question review of
    /// N5 home-life vocabulary. Each session costs energy and gives Knowledge per correct answer;
    /// the desk can be used a limited number of times per day (reset when the player sleeps).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class StudyDeskInteractable : MonoBehaviour, IInteractable, IConditionalInteractable
    {
        [SerializeField] private int questionsPerSession = 5;
        [SerializeField] private int sessionsPerDay = 3;
        [SerializeField] private float energyCost = 15f;
        [SerializeField] private int knowledgePerCorrect = 2;
        [SerializeField] private int perfectBonus = 3;

        private int _sessionsToday;
        private bool _busy;

        public static readonly StudyWord[] Words =
        {
            new StudyWord("へや", "heya · 部屋", "căn phòng", "cửa sổ", "nhà ga", "ここは わたしの へやです。"),
            new StudyWord("つくえ", "tsukue · 机", "cái bàn", "cái ghế", "cái giường", "つくえの うえに ほんが あります。"),
            new StudyWord("いす", "isu", "cái ghế", "cái bàn", "tủ lạnh", "いすに すわります。"),
            new StudyWord("ベッド", "beddo", "cái giường", "cái gối", "cái bàn", "ベッドで ねます。"),
            new StudyWord("まど", "mado · 窓", "cửa sổ", "cửa ra vào", "bức tường", "まどを あけて ください。"),
            new StudyWord("ドア", "doa", "cửa ra vào", "cửa sổ", "đèn", "ドアを しめます。"),
            new StudyWord("でんき", "denki · 電気", "đèn điện", "điện thoại", "tivi", "でんきを つけます。"),
            new StudyWord("れいぞうこ", "reizōko · 冷蔵庫", "tủ lạnh", "lò vi sóng", "máy giặt", "れいぞうこに おにぎりが あります。"),
            new StudyWord("ほん", "hon · 本", "quyển sách", "tờ báo", "cây bút", "ほんを よみます。"),
            new StudyWord("ねる", "neru · 寝る", "đi ngủ", "thức dậy", "ăn cơm", "まいばん 11じに ねます。"),
            new StudyWord("おきる", "okiru · 起きる", "thức dậy", "đi ngủ", "đi học", "まいあさ 7じに おきます。"),
            new StudyWord("べんきょうする", "benkyō suru · 勉強する", "học bài", "làm việc", "nghỉ ngơi", "まいにち にほんごを べんきょうします。"),
            new StudyWord("ただいま", "tadaima", "Tôi về rồi đây", "Tôi đi đây", "Chúc ngủ ngon", "うちに かえって 「ただいま」。"),
            new StudyWord("いってきます", "ittekimasu", "Tôi đi đây", "Tôi về rồi đây", "Xin lỗi", "がっこうへ いくとき 「いってきます」。"),
            new StudyWord("おやすみなさい", "oyasuminasai", "Chúc ngủ ngon", "Chào buổi sáng", "Chào buổi tối", "ねる まえに 「おやすみなさい」。"),
            new StudyWord("あさ", "asa · 朝", "buổi sáng", "buổi tối", "buổi trưa", "あさ ごはんを たべます。"),
            new StudyWord("あした", "ashita · 明日", "ngày mai", "hôm qua", "hôm nay", "あした がっこうへ いきます。"),
            new StudyWord("みず", "mizu · 水", "nước", "trà", "sữa", "みずを のみます。"),
        };

        public bool IsInteractionAvailable => !_busy && (HomeBedroomRuntime.Instance == null || !HomeBedroomRuntime.Instance.IsStudyOpen);
        public int SessionsLeft => Mathf.Max(0, sessionsPerDay - _sessionsToday);
        public string GetPromptJa() => "べんきょうする";
        public string GetpromptEn() => NihongoLife.Core.Loc.F("Học bài (còn {0} lượt hôm nay)", "Study ({0} sessions left today)", "勉強する（今日はあと{0}回）", SessionsLeft);
        public Transform GetTransform() => transform;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnEnable()
        {
            if (HomeBedroomRuntime.Instance != null) HomeBedroomRuntime.Instance.OnNewDay += ResetDay;
        }

        private void Start()
        {
            if (HomeBedroomRuntime.Instance != null)
            {
                HomeBedroomRuntime.Instance.OnNewDay -= ResetDay;
                HomeBedroomRuntime.Instance.OnNewDay += ResetDay;
            }
        }

        private void OnDisable()
        {
            if (HomeBedroomRuntime.Instance != null) HomeBedroomRuntime.Instance.OnNewDay -= ResetDay;
        }

        private void ResetDay() => _sessionsToday = 0;

        public void Interact(GameObject player)
        {
            if (_busy) return;
            NihongoLife.UI.StudyDeskMenuScreen.Open(this, player); // multi-menu: quiz, flashcards, album, exam prep, wardrobe
        }

        /// <summary>The word quiz (the desk's original session), chosen from the desk menu.</summary>
        public void StartQuiz(GameObject player)
        {
            if (_busy) return;
            var room = HomeBedroomRuntime.Instance;
            var status = PlayerStatus.Instance;
            if (SessionsLeft <= 0)
            {
                room?.ShowToast("きょうは もう つかれました", NihongoLife.Core.Loc.Triple("Hôm nay học đủ rồi — đi ngủ để có ngày mới.", "That's enough study for today — sleep to start a new day.", "今日はもう十分勉強しました。寝て新しい一日を始めましょう。"));
                return;
            }
            if (status != null && status.CurrentEnergy < energyCost)
            {
                room?.ShowToast("つかれて います", NihongoLife.Core.Loc.TripleF("Cần {0:0} năng lượng để học. Hãy ăn uống hoặc ngủ.", "You need {0:0} energy to study. Eat, drink or sleep.", "勉強するには元気が{0:0}必要です。食べるか寝ましょう。", energyCost));
                return;
            }
            if (room == null) return;
            StartCoroutine(Session(room, status));
        }

        private IEnumerator Session(HomeBedroomRuntime room, PlayerStatus status)
        {
            _busy = true;
            var pool = new List<StudyWord>(Words);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            var questions = pool.GetRange(0, Mathf.Min(questionsPerSession, pool.Count));

            int correct = 0;
            bool finished = false;
            yield return room.RunStudySession(questions, c => { correct = c; finished = true; });
            if (!finished)
            {
                _busy = false;
                yield break;
            }

            _sessionsToday++;
            NihongoLife.Learning.StudyProgressStore.AddWordsSeen(questions.Count);
            int gained = correct * knowledgePerCorrect + (correct == questions.Count ? perfectBonus : 0);
            if (status != null)
            {
                status.TrySpendEnergy(energyCost);
                status.AddKnowledge(gained);
            }
            room.ShowToast(correct == questions.Count ? "まんてん！" : "おつかれさまでした",
                NihongoLife.Core.Loc.TripleF("Đúng {0}/{1} · Trí tuệ +{2} · Năng lượng −{3:0}", "Correct {0}/{1} · Knowledge +{2} · Energy −{3:0}", "正解 {0}/{1} · 知識 +{2} · 元気 −{3:0}", correct, questions.Count, gained, energyCost));
            _busy = false;
        }
    }
}
