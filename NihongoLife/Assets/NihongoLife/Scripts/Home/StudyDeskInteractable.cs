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
        public string GetpromptEn() => $"Học bài (còn {SessionsLeft} lượt hôm nay)";
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
            var room = HomeBedroomRuntime.Instance;
            var status = PlayerStatus.Instance;
            if (SessionsLeft <= 0)
            {
                room?.ShowToast("きょうは もう つかれました", "Hôm nay học đủ rồi — đi ngủ để có ngày mới.");
                return;
            }
            if (status != null && status.CurrentEnergy < energyCost)
            {
                room?.ShowToast("つかれて います", $"Cần {energyCost:0} năng lượng để học. Hãy ăn uống hoặc ngủ.");
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
            yield return room.RunStudySession(questions, c => correct = c);

            _sessionsToday++;
            int gained = correct * knowledgePerCorrect + (correct == questions.Count ? perfectBonus : 0);
            if (status != null)
            {
                status.TrySpendEnergy(energyCost);
                status.AddKnowledge(gained);
            }
            room.ShowToast(correct == questions.Count ? "まんてん！" : "おつかれさまでした",
                $"Đúng {correct}/{questions.Count} · Kiến thức +{gained} · Năng lượng −{energyCost:0}");
            _busy = false;
        }
    }
}
