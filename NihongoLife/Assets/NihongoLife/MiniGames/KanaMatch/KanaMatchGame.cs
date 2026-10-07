using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// Kana Match: a memory card game over Nihongo learning pairs (hiragana↔romaji, katakana↔romaji,
    /// kanji↔reading, word↔picture, Japanese↔Vietnamese). Randomised board, flip animation, match /
    /// mismatch feedback, timer, attempts and completion detection, re-implemented for Nihongo Life
    /// (no code or UI taken from the reference memory-game projects).
    /// </summary>
    public sealed class KanaMatchGame : MonoBehaviour, IMiniGame
    {
        public enum Phase { ChoosingSet, Playing, Finished }

        public sealed class Card
        {
            public int PairIndex;
            public bool IsSideA;
            public bool FaceUp;
            public bool Matched;
            public RectTransform Rect;
            public Image Background;
            public GameObject Front;
            public GameObject Back;
        }

        private static readonly Color CardBack = new Color(0.16f, 0.2f, 0.32f);
        private static readonly Color CardFront = new Color(0.97f, 0.95f, 0.9f);
        private static readonly Color CardMatched = new Color(0.62f, 0.9f, 0.68f);
        private static readonly Color CardWrong = new Color(0.98f, 0.55f, 0.52f);

        private MiniGameDefinition _definition;
        private RectTransform _root;
        private TMP_FontAsset _font;
        private KanaPairSet _set;
        private List<KanaPair> _pairs = new();
        private readonly List<Card> _cards = new();
        private readonly HashSet<int> _missedPairs = new();
        private readonly List<MiniGameMistake> _mistakes = new();
        private Card _first;
        private bool _busy;
        private int _attempts;
        private int _matchedPairs;
        private float _elapsed;
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _attemptText;
        private TextMeshProUGUI _pairText;
        private TextMeshProUGUI _scoreText;
        private RectTransform _progressFill;
        private RectTransform _boardRoot;
        private int _combo;
        private int _bestCombo;
        private int _liveScore;
        public const string TicketItemId = "game_ticket";

        public int Combo => _combo;
        public int LiveScore => _liveScore;
        public static int Stars(MiniGameResult r) => !r.completed ? 0 : r.accuracy >= 0.9f ? 3 : r.accuracy >= 0.7f ? 2 : 1;
        public static int TicketsFor(MiniGameResult r) => r.completed ? Mathf.Max(1, r.score / 100) + Stars(r) : 0;
        public static string BestKey(string setId) => "NL.KanaMatch.Best." + setId;

        public string GameId => "kana_match";
        public Phase State { get; private set; }
        public IReadOnlyList<Card> Cards => _cards;
        public KanaPairSet CurrentSet => _set;
        public bool IsBusy => _busy;
        public int Attempts => _attempts;
        public event Action<MiniGameResult> Finished;

        public void Begin(MiniGameDefinition definition, RectTransform root)
        {
            _definition = definition;
            _root = root;
            _font = NLUi.ResolveFont();
            ShowSetPicker();
        }

        public void Abort()
        {
            if (State == Phase.Finished) return;
            State = Phase.Finished;
            Finished?.Invoke(BuildResult(false));
        }

        // ─────────── Set picker ───────────

        private void ShowSetPicker()
        {
            State = Phase.ChoosingSet;
            Clear();
            var panel = NLUi.Panel(_root, "SetPicker", NLUi.Ink, new RectOffset(44, 44, 34, 34), 14f);
            NLUi.Anchor(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 0f));
            NLUi.FitContent(panel);
            NLUi.Label(panel, "Kicker", "ゲームセンター ひばり", 18f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(panel, "Title", $"{_definition.titleJa}  <size=60%><color=#E8EEF6>{_definition.titleVi}</color></size>", 46f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(panel, "Desc", _definition.descriptionVi, 19f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            NLUi.Divider(panel);
            foreach (var set in _definition.contentSets.Where(s => s != null && s.pairs.Count >= 2))
            {
                var chosen = set;
                NLUi.Button(panel, "Set_" + set.id, $"{set.titleJa}   <size=75%><color=#A8B4C4>{set.titleVi}</color></size>", _font,
                    () => StartRound(chosen), new Color(0.13f, 0.32f, 0.45f), 24f, null, 60f);
            }
            NLUi.Label(panel, "Hint", "Esc · thoát máy", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        // ─────────── Round ───────────

        public void StartRound(KanaPairSet set)
        {
            if (set == null) return;
            _set = set;
            Clear();
            _cards.Clear();
            _missedPairs.Clear();
            _mistakes.Clear();
            _first = null;
            _busy = false;
            _attempts = 0;
            _matchedPairs = 0;
            _elapsed = 0f;
            int count = Mathf.Clamp(_definition.pairsPerRound, 2, set.pairs.Count);
            _pairs = set.pairs.OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
            _combo = 0;
            _bestCombo = 0;
            _liveScore = 0;
            State = Phase.Playing;
            BuildBoard();
            StartCoroutine(Countdown());
        }

        private IEnumerator Countdown()
        {
            _busy = true;
            var label = NLUi.Label(_root, "Countdown", "", 120f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(label.rectTransform);
            foreach (string step in new[] { "3", "2", "1", "スタート！" })
            {
                label.text = step;
                Cue(step.Length > 1 ? GameAudioCue.UiConfirm : GameAudioCue.UiTick);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.55f)
                {
                    float s = Mathf.Lerp(1.6f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 2f)));
                    label.rectTransform.localScale = Vector3.one * s;
                    label.alpha = 1f - Mathf.Max(0f, t - 0.7f) / 0.3f;
                    yield return null;
                }
            }
            Destroy(label.gameObject);
            _elapsed = 0f;
            _busy = false;
        }

        /// <summary>Skips the 3-2-1 intro (tests).</summary>
        public void SkipCountdown()
        {
            var label = _root != null ? _root.Find("Countdown") : null;
            if (label != null) Destroy(label.gameObject);
            StopAllCoroutines();
            _elapsed = 0f;
            _busy = false;
        }

        private void BuildBoard()
        {
            var frame = NLUi.Panel(_root, "Board", NLUi.Ink, new RectOffset(34, 34, 24, 30), 16f);
            NLUi.Anchor(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 0f));
            NLUi.FitContent(frame);

            var header = NLUi.Group(frame, "Header", false, 12f, TextAnchor.MiddleLeft, false);
            NLUi.Size(NLUi.Label(header, "Title", $"{_definition.titleJa} · <size=70%><color=#A8B4C4>{_set.titleVi}</color></size>", 28f, NLUi.Text, _font, FontStyles.Bold), flexibleWidth: 1f);
            _timerText = NLUi.Pill(header, "Timer", "", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 19f).GetComponentInChildren<TextMeshProUGUI>();
            _attemptText = NLUi.Pill(header, "Attempts", "", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Text, 19f).GetComponentInChildren<TextMeshProUGUI>();
            _pairText = NLUi.Pill(header, "Pairs", "", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Good, 19f).GetComponentInChildren<TextMeshProUGUI>();
            _scoreText = NLUi.Pill(header, "Score", "", _font, new Color(0.95f, 0.7f, 0.2f, 0.18f), NLUi.Gold, 19f).GetComponentInChildren<TextMeshProUGUI>();
            var track = new GameObject("Progress", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            track.SetParent(frame, false);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            track.GetComponent<LayoutElement>().preferredHeight = 8f;
            _progressFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _progressFill.SetParent(track, false);
            _progressFill.GetComponent<Image>().color = NLUi.Good;
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
            _boardRoot = frame;

            int total = _pairs.Count * 2;
            int columns = total <= 12 ? 4 : total <= 16 ? 4 : 5;
            var grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            grid.SetParent(frame, false);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(176f, 150f);
            layout.spacing = new Vector2(16f, 16f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.childAlignment = TextAnchor.MiddleCenter;
            int rows = Mathf.CeilToInt(total / (float)columns);
            grid.GetComponent<LayoutElement>().preferredHeight = rows * 150f + (rows - 1) * 16f;

            var deck = new List<(int pair, bool sideA)>();
            for (int i = 0; i < _pairs.Count; i++) { deck.Add((i, true)); deck.Add((i, false)); }
            deck = deck.OrderBy(_ => UnityEngine.Random.value).ToList();
            foreach (var (pair, sideA) in deck) _cards.Add(CreateCard(grid, pair, sideA, _cards.Count));

            NLUi.Label(frame, "Hint", "Lật 2 thẻ cùng cặp nghĩa · Esc để thoát", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            RefreshHeader();
        }

        private Card CreateCard(RectTransform grid, int pairIndex, bool sideA, int index)
        {
            var pair = _pairs[pairIndex];
            var rect = new GameObject("Card_" + index, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rect.SetParent(grid, false);
            var background = rect.GetComponent<Image>();
            background.color = CardBack;
            var button = rect.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            int captured = index;
            button.onClick.AddListener(() => Pick(captured));

            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.75f, 0.3f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            var back = new GameObject("Back", typeof(RectTransform)).GetComponent<RectTransform>();
            back.SetParent(rect, false);
            NLUi.Stretch(back);
            var backMark = NLUi.Label(back, "Mark", "あ", 64f, new Color(0.95f, 0.75f, 0.3f, 0.85f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(backMark.rectTransform);
            var backTag = NLUi.Label(back, "Tag", "NIHONGO", 13f, new Color(1f, 1f, 1f, 0.35f), _font, FontStyles.Bold, TextAlignmentOptions.Bottom);
            NLUi.Stretch(backTag.rectTransform, 8f);

            var front = new GameObject("Front", typeof(RectTransform)).GetComponent<RectTransform>();
            front.SetParent(rect, false);
            NLUi.Stretch(front);
            bool picture = !sideA && !string.IsNullOrEmpty(pair.imageB);
            if (picture)
            {
                var image = new GameObject("Picture", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(front, false);
                NLUi.Stretch(image.rectTransform, 18f);
                image.sprite = EmoteCatalog.Load(pair.imageB);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                string value = sideA ? pair.a : pair.b;
                float size = value.Length <= 2 ? 64f : value.Length <= 5 ? 40f : 26f;
                var text = NLUi.Label(front, "Text", value, size, new Color(0.12f, 0.14f, 0.2f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
                NLUi.Stretch(text.rectTransform, 8f);
            }
            var side = NLUi.Label(front, "Side", sideA ? "日本語" : TagFor(_set.type), 12f, new Color(0.35f, 0.4f, 0.5f), _font, FontStyles.Bold, TextAlignmentOptions.Top);
            NLUi.Stretch(side.rectTransform, 6f);
            front.gameObject.SetActive(false);

            return new Card { PairIndex = pairIndex, IsSideA = sideA, Rect = rect, Background = background, Front = front.gameObject, Back = back.gameObject };
        }

        private static string TagFor(KanaPairType type) => type switch
        {
            KanaPairType.HiraganaRomaji or KanaPairType.KatakanaRomaji => "ROMAJI",
            KanaPairType.KanjiReading => "よみかた",
            KanaPairType.WordImage => "HÌNH",
            _ => "TIẾNG VIỆT"
        };

        /// <summary>Flips card <paramref name="index"/> (mouse click, or tests).</summary>
        public void Pick(int index)
        {
            if (State != Phase.Playing || _busy || index < 0 || index >= _cards.Count) return;
            var card = _cards[index];
            if (card.Matched || card.FaceUp) return;
            StartCoroutine(Flip(card, true));
            if (_first == null) { _first = card; return; }
            var second = card;
            var first = _first;
            _first = null;
            _attempts++;
            StartCoroutine(Resolve(first, second));
        }

        private IEnumerator Resolve(Card first, Card second)
        {
            _busy = true;
            yield return new WaitForSecondsRealtime(0.3f);
            if (first.PairIndex == second.PairIndex)
            {
                first.Matched = second.Matched = true;
                _matchedPairs++;
                _combo++;
                _bestCombo = Mathf.Max(_bestCombo, _combo);
                int gained = 100 + (_combo - 1) * 25;
                _liveScore += gained;
                Cue(GameAudioCue.UiConfirm);
                FloatText(second.Rect, _combo >= 2 ? $"+{gained}  コンボ x{_combo}!" : $"+{gained}", NLUi.Good);
                StartCoroutine(Tint(first, CardMatched));
                StartCoroutine(Tint(second, CardMatched));
                StartCoroutine(Pop(first.Rect));
                StartCoroutine(Pop(second.Rect));
                RefreshHeader();
                _busy = false;
                if (_matchedPairs == _pairs.Count) StartCoroutine(Complete());
                yield break;
            }

            _missedPairs.Add(first.PairIndex);
            _missedPairs.Add(second.PairIndex);
            var expected = _pairs[first.PairIndex];
            var chosen = _pairs[second.PairIndex];
            _mistakes.Add(new MiniGameMistake
            {
                expectedTargetId = expected.targetId,
                chosenTargetId = chosen.targetId,
                note = $"{expected.a} = {(string.IsNullOrEmpty(expected.imageB) ? expected.b : expected.hintVi)}  (không phải {(first.IsSideA == second.IsSideA ? chosen.a : Describe(chosen, second.IsSideA))})"
            });
            first.Background.color = CardWrong;
            second.Background.color = CardWrong;
            _combo = 0;
            _liveScore = Mathf.Max(0, _liveScore - 15);
            Cue(GameAudioCue.UiError);
            FloatText(second.Rect, "ちがう！ -15", NLUi.Bad);
            yield return Shake(first.Rect, second.Rect);
            yield return new WaitForSecondsRealtime(0.35f);
            StartCoroutine(Flip(first, false));
            yield return Flip(second, false);
            RefreshHeader();
            _busy = false;
        }

        private static string Describe(KanaPair pair, bool sideA) => sideA ? pair.a : string.IsNullOrEmpty(pair.imageB) ? pair.b : pair.hintVi;

        private IEnumerator Complete()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            State = Phase.Finished;
            Finished?.Invoke(BuildResult(true));
        }

        private void Update()
        {
            if (State != Phase.Playing) return;
            if (_root != null && _root.Find("Countdown") != null) return;
            _elapsed += Time.unscaledDeltaTime;
            RefreshHeader();
            if (_definition.timeLimitSeconds > 0f && _elapsed >= _definition.timeLimitSeconds && !_busy)
            {
                State = Phase.Finished;
                Finished?.Invoke(BuildResult(false));
            }
        }

        private void RefreshHeader()
        {
            if (_timerText == null) return;
            float left = Mathf.Max(0f, _definition.timeLimitSeconds - _elapsed);
            _timerText.text = $"時間 {Mathf.FloorToInt(left / 60f)}:{Mathf.FloorToInt(left % 60f):00}";
            _attemptText.text = $"Lượt {_attempts}";
            _pairText.text = $"Cặp {_matchedPairs}/{_pairs.Count}";
            if (_scoreText != null) _scoreText.text = _combo >= 2 ? $"{_liveScore} điểm · x{_combo}" : $"{_liveScore} điểm";
            if (_progressFill != null) _progressFill.anchorMax = new Vector2(_pairs.Count > 0 ? _matchedPairs / (float)_pairs.Count : 0f, 1f);
        }

        private MiniGameResult BuildResult(bool completed)
        {
            int mismatches = _attempts - _matchedPairs;
            var result = new MiniGameResult
            {
                gameId = GameId,
                contentId = _set != null ? _set.id : string.Empty,
                completed = completed,
                correctCount = _matchedPairs,
                incorrectCount = Mathf.Max(0, mismatches),
                accuracy = _attempts > 0 ? _matchedPairs / (float)_attempts : 0f,
                completionSeconds = _elapsed,
                mistakes = new List<MiniGameMistake>(_mistakes),
            };
            for (int i = 0; i < _pairs.Count; i++)
            {
                result.learningTargetIds.Add(_pairs[i].targetId);
                if (!_missedPairs.Contains(i) && _cards.Any(c => c.PairIndex == i && c.Matched)) result.masteredTargetIds.Add(_pairs[i].targetId);
            }
            float timeBonus = _definition.timeLimitSeconds > 0f ? Mathf.Max(0f, _definition.timeLimitSeconds - _elapsed) * 2f : 0f;
            result.score = completed ? Mathf.Max(0, _liveScore + Mathf.RoundToInt(timeBonus)) : _matchedPairs * 50;
            result.expReward = completed ? _matchedPairs * _definition.expPerCorrect : 0;
            result.knowledgeReward = completed ? _definition.knowledgePerRound : 0;
            result.masteryGain = result.masteredTargetIds.Count * 5f - (result.learningTargetIds.Count - result.masteredTargetIds.Count) * 3f;
            result.stars = Stars(result);
            result.bestCombo = _bestCombo;
            if (completed && _set != null)
            {
                result.tickets = TicketsFor(result);
                PlayerInventory.Instance?.AddItem(TicketItemId, "チケット", "Vé thưởng Game Center — đổi quà ở quầy けいひん", 0, result.tickets);
                if (result.score > PlayerPrefs.GetInt(BestKey(_set.id), 0))
                {
                    result.newRecord = true;
                    PlayerPrefs.SetInt(BestKey(_set.id), result.score);
                    PlayerPrefs.Save();
                }
            }
            return result;
        }

        private void FloatText(RectTransform anchor, string value, Color color)
        {
            if (_boardRoot == null) return;
            var label = NLUi.Label(_root, "Float", value, 26f, color, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            label.rectTransform.position = anchor.position;
            label.rectTransform.sizeDelta = new Vector2(320f, 40f);
            StartCoroutine(Rise(label));
        }

        private static IEnumerator Rise(TextMeshProUGUI label)
        {
            Vector3 start = label.rectTransform.position;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f)
            {
                if (label == null) yield break;
                label.rectTransform.position = start + Vector3.up * (60f * t);
                label.alpha = 1f - t;
                yield return null;
            }
            if (label != null) Destroy(label.gameObject);
        }

        private static void Cue(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }

        // ─────────── Animation ───────────

        private IEnumerator Flip(Card card, bool faceUp)
        {
            card.FaceUp = faceUp;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.12f)
            {
                card.Rect.localScale = new Vector3(1f - t, 1f, 1f);
                yield return null;
            }
            card.Front.SetActive(faceUp);
            card.Back.SetActive(!faceUp);
            card.Background.color = faceUp ? (card.Matched ? CardMatched : CardFront) : CardBack;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.12f)
            {
                card.Rect.localScale = new Vector3(t, 1f, 1f);
                yield return null;
            }
            card.Rect.localScale = Vector3.one;
        }

        private static IEnumerator Tint(Card card, Color color)
        {
            yield return new WaitForSecondsRealtime(0.12f);
            card.Background.color = color;
        }

        private static IEnumerator Pop(RectTransform rect)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private static IEnumerator Shake(RectTransform a, RectTransform b)
        {
            Vector3 pa = a.anchoredPosition3D, pb = b.anchoredPosition3D;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float offset = Mathf.Sin(t * Mathf.PI * 8f) * 9f * (1f - t);
                a.anchoredPosition3D = pa + Vector3.right * offset;
                b.anchoredPosition3D = pb + Vector3.right * offset;
                yield return null;
            }
            a.anchoredPosition3D = pa;
            b.anchoredPosition3D = pb;
        }

        private void Clear()
        {
            StopAllCoroutines();
            if (_root == null) return;
            foreach (Transform child in _root) Destroy(child.gameObject);
        }

        private void OnDestroy() => StopAllCoroutines();
    }
}
