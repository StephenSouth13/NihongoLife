using System.Collections;
using System.Collections.Generic;
using NihongoLife.Dialogue;
using NihongoLife.Exam;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.School
{
    /// <summary>
    /// Makes the classroom react: a welcome banner when the player walks in, an exam ceremony when an
    /// attempt starts (banner, classroom lights dim, spotlight on the exam desk), and when it is submitted a
    /// result banner, paper confetti, Morita's comment and the best score chalked on the blackboard.
    /// </summary>
    public sealed class ClassroomRuntime : MonoBehaviour
    {
        public static ClassroomRuntime Instance { get; private set; }

        [SerializeField] private TextMeshPro blackboardScore;
        [SerializeField] private Light examSpotlight;
        [SerializeField] private List<Light> roomLights = new();

        private const string BestScoreKey = "NL.Classroom.BestScore";
        private readonly List<float> _roomIntensity = new();
        private bool _attemptWasActive;
        private bool _welcomed;
        private ExamManager _exams;
        private Canvas _canvas;
        private TMP_FontAsset _font;

        public bool ExamCeremonyActive { get; private set; }
        public int ResultsShown { get; private set; }

        public void Configure(TextMeshPro scoreText, Light spotlight, List<Light> lights)
        {
            blackboardScore = scoreText;
            examSpotlight = spotlight;
            roomLights = lights;
        }

        private void Awake()
        {
            Instance = this;
            foreach (var light in roomLights) _roomIntensity.Add(light != null ? light.intensity : 0f);
            if (examSpotlight != null) examSpotlight.enabled = false;
            RefreshBlackboard();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Unbind();
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void Update()
        {
            if (!_welcomed && SceneManager.GetActiveScene() == gameObject.scene)
            {
                _welcomed = true;
                Banner("ひばり日本語学院 へ ようこそ", "Lớp N5 của cô Morita · bàn thi ở đầu lớp — F để làm đề JLPT / IELTS", new Color(0.55f, 0.85f, 1f), 5f);
            }
            if (_exams == null && ExamManager.Instance != null) Bind(ExamManager.Instance);
            if (_exams == null) return;
            bool active = _exams.IsAttemptActive;
            if (active && !_attemptWasActive) BeginCeremony();
            _attemptWasActive = active;
        }

        public void OnExamDeskUsed() =>
            Banner("しけん の じかん です", "Chọn một đề: JLPT N5 hoặc IELTS — làm bài nghiêm túc nhé!", NLUi.Gold, 3f);

        private void Bind(ExamManager exams)
        {
            _exams = exams;
            _exams.OnExamFinished += HandleFinished;
        }

        private void Unbind()
        {
            if (_exams != null) _exams.OnExamFinished -= HandleFinished;
            _exams = null;
        }

        private void BeginCeremony()
        {
            ExamCeremonyActive = true;
            for (int i = 0; i < roomLights.Count; i++) if (roomLights[i] != null) roomLights[i].intensity = _roomIntensity[i] * 0.35f;
            if (examSpotlight != null) examSpotlight.enabled = true;
            Banner("しけん かいし！", "Bắt đầu làm bài · " + (_exams.CurrentExam != null ? _exams.CurrentExam.titleVi : "Đề thi"), NLUi.Gold, 3f);
        }

        private void HandleFinished(ExamAttemptResult result)
        {
            ExamCeremonyActive = false;
            ResultsShown++;
            for (int i = 0; i < roomLights.Count; i++) if (roomLights[i] != null) roomLights[i].intensity = _roomIntensity[i];
            if (examSpotlight != null) examSpotlight.enabled = false;
            if (result == null || result.record == null) return;

            bool passed = result.record.passed;
            int score = Mathf.RoundToInt(result.record.totalScore);
            if (score > PlayerPrefs.GetInt(BestScoreKey, 0)) { PlayerPrefs.SetInt(BestScoreKey, score); PlayerPrefs.Save(); }
            RefreshBlackboard();
            Banner(passed ? "ごうかく！" : "おつかれさま！",
                $"{(result.exam != null ? result.exam.titleVi : "Đề thi")} · {score} điểm · {(passed ? "Đạt" : "Chưa đạt — ôn lại phần sai nhé")}",
                passed ? new Color(0.45f, 0.9f, 0.6f) : new Color(1f, 0.7f, 0.35f), 6f);
            if (passed) StartCoroutine(Confetti());
            PlayerStatus.Instance?.AddKnowledge(passed ? 8 : 3);
            StartCoroutine(TeacherComment(passed, score));
        }

        private IEnumerator TeacherComment(bool passed, int score)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) yield break;
            const string teacher = "もりた せんせい · Cô Morita";
            dm.StartConversation(new[]
            {
                new ScenarioNode
                {
                    id = "exam_comment", nodeType = ScenarioNodeType.Dialogue, speakerName = teacher,
                    textJa = passed ? "よく できました！この ちょうしで がんばりましょう。" : "だいじょうぶ。まちがえた ところを いっしょに ふくしゅう しましょう。",
                    textReading = passed ? "よく できました！この ちょうしで がんばりましょう。" : "だいじょうぶ。まちがえた ところを いっしょに ふくしゅう しましょう。",
                    textEn = passed ? $"Làm tốt lắm! ({score} điểm) Cứ giữ phong độ này nhé." : $"Không sao đâu. ({score} điểm) Mình cùng ôn lại phần làm sai nhé.",
                    choices = new List<DialogueChoice> { new DialogueChoice { textJa = "はい、ありがとうございます！", textEn = "Vâng, em cảm ơn cô!", nextNodeId = "" } }
                }
            }, "exam_comment", null);
        }

        private void RefreshBlackboard()
        {
            if (blackboardScore == null) return;
            int best = PlayerPrefs.GetInt(BestScoreKey, 0);
            blackboardScore.text = best > 0 ? $"さいこうてん  {best}\n<size=60%>Điểm cao nhất của bạn</size>" : "もぎしけん  N5 · IELTS\n<size=60%>Bàn thi ở đầu lớp · F</size>";
        }

        // ─────────── UI effects ───────────

        private RectTransform Root()
        {
            if (_canvas == null)
            {
                _font = NLUi.ResolveFont();
                var hud = FindFirstObjectByType<HUDUI>();
                _canvas = NLUi.CreateCanvas("ClassroomFX", 140, hud != null ? hud.transform : transform);
            }
            return (RectTransform)_canvas.transform;
        }

        private void Banner(string ja, string vi, Color accent, float seconds)
        {
            var root = Root();
            var old = root.Find("Banner");
            if (old != null) Destroy(old.gameObject);
            var card = NLUi.Panel(root, "Banner", NLUi.Ink, new RectOffset(36, 36, 16, 18), 4f);
            NLUi.Anchor(card, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(820f, 0f));
            NLUi.FitContent(card);
            NLUi.Label(card, "Ja", ja, 36f, accent, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(card, "Vi", vi, 19f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            StartCoroutine(PopAndFade(card, seconds));
        }

        private static IEnumerator PopAndFade(RectTransform card, float seconds)
        {
            var group = card.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                if (card == null) yield break;
                card.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Mathf.SmoothStep(0f, 1f, t));
                group.alpha = t;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(seconds);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.4f)
            {
                if (card == null) yield break;
                group.alpha = t;
                yield return null;
            }
            if (card != null) Destroy(card.gameObject);
        }

        private IEnumerator Confetti()
        {
            var root = Root();
            Color[] colors = { new Color(1f, 0.8f, 0.3f), new Color(0.45f, 0.9f, 0.6f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.5f, 0.7f) };
            var pieces = new List<(RectTransform rect, Vector2 velocity, float spin)>();
            for (int i = 0; i < 70; i++)
            {
                var piece = new GameObject("Confetti", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                piece.SetParent(root, false);
                piece.anchorMin = piece.anchorMax = new Vector2(Random.value, 1f);
                piece.anchoredPosition = new Vector2(0f, Random.Range(0f, 120f));
                piece.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(14f, 24f));
                piece.GetComponent<Image>().color = colors[i % colors.Length];
                piece.GetComponent<Image>().raycastTarget = false;
                pieces.Add((piece, new Vector2(Random.Range(-60f, 60f), -Random.Range(220f, 420f)), Random.Range(-240f, 240f)));
            }
            for (float t = 0f; t < 3.2f; t += Time.unscaledDeltaTime)
            {
                foreach (var (rect, velocity, spin) in pieces)
                {
                    if (rect == null) continue;
                    rect.anchoredPosition += velocity * Time.unscaledDeltaTime;
                    rect.Rotate(0f, 0f, spin * Time.unscaledDeltaTime);
                }
                yield return null;
            }
            foreach (var (rect, _, _) in pieces) if (rect != null) Destroy(rect.gameObject);
        }
    }
}
