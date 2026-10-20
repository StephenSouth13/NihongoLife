using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// Shared frame for the action arcade games (Word Shooter, Order Rush, Kana Rain, UFO Catcher, Basket Count):
    /// content picker → 3-2-1 → timed round with score, combo, lives and progress → result (stars, tickets, record,
    /// learning targets for mastery). Every word on screen follows the interface language (Loc); the Japanese being
    /// practised is the game content and stays Japanese. A game builds its own play field in <see cref="Stage"/>
    /// and reports <see cref="Hit"/> / <see cref="Miss"/>.
    /// </summary>
    public abstract class ArcadeGame : MonoBehaviour, IMiniGame
    {
        public enum Phase { Choosing, Countdown, Playing, Finished }

        public abstract string GameId { get; }
        /// <summary>One-line how-to (vi, en, ja) shown on the start card.</summary>
        protected abstract (string vi, string en, string ja) HowTo { get; }
        protected virtual int StartLives => 3;
        protected virtual bool UsesContentSets => true;
        protected virtual bool SetFits(KanaPairSet set) => set != null && set.pairs.Count >= 4 && set.AvailableInCurrentLanguage;
        protected virtual Color Accent => UiTheme.Gold;
        protected abstract void BuildStage(RectTransform stage);
        protected virtual void OnRoundStart() { }
        protected virtual void OnTick(float dt) { }

        protected MiniGameDefinition Definition;
        protected RectTransform Root, Stage;
        protected KanaPairSet Set;

        public Phase State { get; private set; }
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Lives { get; private set; }
        public int CorrectCount { get; private set; }
        public int WrongCount { get; private set; }
        public float Elapsed { get; private set; }
        public KanaPairSet CurrentSet => Set;
        public float TimeLeft => Definition != null && Definition.timeLimitSeconds > 0f ? Mathf.Max(0f, Definition.timeLimitSeconds - Elapsed) : 0f;
        public event Action<MiniGameResult> Finished;

        private readonly List<MiniGameMistake> _mistakes = new();
        private readonly List<string> _targets = new();
        private readonly HashSet<string> _missed = new();
        private TextMeshProUGUI _time, _score, _combo, _lives;
        private RectTransform _timeFill;

        // ─────────── Start ───────────

        public void Begin(MiniGameDefinition definition, RectTransform root)
        {
            Definition = definition;
            Root = root;
            var sets = Definition.contentSets.Where(SetFits).ToList();
            if (UsesContentSets && sets.Count > 1) ShowPicker(sets);
            else ShowStartCard(UsesContentSets ? sets.FirstOrDefault() : null);
        }

        public void Abort()
        {
            if (State == Phase.Finished) return;
            State = Phase.Finished;
            StopAllCoroutines();
            Finished?.Invoke(BuildResult(false));
        }

        private RectTransform Window(string name, float width, float height)
        {
            var card = UiKit.Card(Root, name, UiTheme.Ink, 24, 0.5f);
            UiKit.Pin(card.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, width, height);
            var glow = UiKit.Image(card.transform, "Accent", Accent, UiKit.Rounded(3));
            UiKit.Strip(glow.rectTransform, true, 0f, 5f, 24f, 24f);
            return card.rectTransform;
        }

        private RectTransform StartHeader(RectTransform card, float width)
        {
            UiKit.Place(UiKit.Label(card, "Kicker", "ゲームセンター ひばり", 16f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 26f, width, 24f);
            UiKit.Place(UiKit.Label(card, "Title", () => Definition.titleJa + (Loc.IsJapanese ? "" : $"  <size=55%><color={UiTheme.Hex(UiTheme.TextSoft)}>{Definition.Title}</color></size>"),
                44f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 56f, width, 60f);
            UiKit.Place(UiKit.Label(card, "Desc", () => Definition.Description, 18f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center, true), 40f, 122f, width - 80f, 52f);
            var how = UiKit.Card(card, "HowTo", UiTheme.Surface, 14, 0f, false);
            UiKit.Place(how.rectTransform, 40f, 182f, width - 80f, 58f);
            var icon = UiKit.Icon(how.transform, "sports_esports", Accent, 26f);
            UiKit.Pin(icon.rectTransform, new Vector2(0f, 0.5f), 20f, 0f, 26f, 26f);
            UiKit.Fill(UiKit.Label(how.transform, "Text", HowTo.vi, HowTo.en, HowTo.ja, 16f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true).rectTransform, 60f, 6f, 16f, 6f);
            return card;
        }

        private void ShowPicker(List<KanaPairSet> sets)
        {
            State = Phase.Choosing;
            Clear();
            float height = 290f + sets.Count * 66f;
            var card = Window("SetPicker", 780f, height);
            StartHeader(card, 780f);
            UiKit.Place(UiKit.Label(card, "Choose", "Chọn bộ từ để chơi", "Choose a word set", "セットを えらんでください", 15f, UiTheme.Muted, FontStyles.Bold), 40f, 252f, 700f, 22f);
            float y = 282f;
            foreach (var set in sets)
            {
                var chosen = set;
                var button = UiKit.Button(card, "Set_" + set.id, null, null, null, () => StartRound(chosen), UiKit.Kind.Secondary, "play_arrow", 20f, 14);
                UiKit.SetLabel(button, set.titleJa + (Loc.IsJapanese ? "" : $"   <size=78%><color={UiTheme.Hex(UiTheme.TextSoft)}>{set.Title}</color></size>"));
                UiKit.Place((RectTransform)button.transform, 40f, y, 700f, 56f);
                y += 66f;
            }
        }

        private void ShowStartCard(KanaPairSet set)
        {
            State = Phase.Choosing;
            Clear();
            var card = Window("StartCard", 780f, 350f);
            StartHeader(card, 780f);
            var play = UiKit.Button(card, "Play", "Bắt đầu", "Start", "スタート", () => StartRound(set), UiKit.Kind.Primary, "play_arrow", 22f, 16);
            UiKit.Pin((RectTransform)play.transform, new Vector2(0.5f, 0f), 0f, 34f, 300f, 60f);
        }

        // ─────────── Round ───────────

        /// <summary>Starts a round on <paramref name="set"/> (null for games without word sets). Tests call it directly.</summary>
        public void StartRound(KanaPairSet set)
        {
            Set = set;
            Clear();
            Score = Combo = BestCombo = CorrectCount = WrongCount = 0;
            Lives = StartLives;
            Elapsed = 0f;
            _mistakes.Clear(); _targets.Clear(); _missed.Clear();

            var window = Window("Arcade", 1220f, 760f);
            BuildHud(window);
            Stage = UiKit.Rect(window, "Stage");
            UiKit.Fill(Stage, 30f, 110f, 30f, 30f);
            var mask = Stage.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            BuildStage(Stage);
            State = Phase.Countdown;
            StartCoroutine(Countdown());
        }

        private void BuildHud(RectTransform window)
        {
            var title = UiKit.Label(window, "Title", () => Definition.titleJa + (Set != null ? $"  <size=62%><color={UiTheme.Hex(UiTheme.TextSoft)}>{(Loc.IsJapanese ? Set.titleJa : Set.Title)}</color></size>" : Loc.IsJapanese ? "" : $"  <size=62%><color={UiTheme.Hex(UiTheme.TextSoft)}>{Definition.Title}</color></size>"),
                28f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 30f, 22f, 560f, 40f);
            float x = -90f;
            _lives = Chip(window, "Lives", "favorite", UiTheme.Bad, ref x, 120f);
            _combo = Chip(window, "Combo", "bolt", UiTheme.Warn, ref x, 130f);
            _score = Chip(window, "Score", "stars", UiTheme.Gold, ref x, 170f);
            _time = Chip(window, "Time", "timer", UiTheme.Teal, ref x, 130f);
            _timeFill = UiKit.ProgressBar(window, "TimeBar", Accent, UiTheme.Sunken, 4);
            UiKit.Strip((RectTransform)_timeFill.parent, true, 76f, 8f, 30f, 30f);
            RefreshHud();
        }

        private TextMeshProUGUI Chip(RectTransform window, string name, string icon, Color color, ref float x, float width)
        {
            var bg = UiKit.Card(window, name, UiKit.Alpha(color, 0.16f), 18, 0f, false);
            UiKit.Pin(bg.rectTransform, new Vector2(1f, 1f), x, -22f, width, 40f);
            x -= width + 10f;
            var ic = UiKit.Icon(bg.transform, icon, color, 20f);
            UiKit.Pin(ic.rectTransform, new Vector2(0f, 0.5f), 14f, 0f, 20f, 20f);
            var label = UiKit.Label(bg.transform, "Value", "", 18f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiKit.Fill(label.rectTransform, 40f, 0f, 10f, 0f);
            return label;
        }

        private IEnumerator Countdown()
        {
            var label = UiKit.Label(Root, "Countdown", "", 130f, Accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(label.rectTransform);
            label.enableAutoSizing = false;
            foreach (string step in new[] { "3", "2", "1", "スタート！" })
            {
                label.text = step;
                Cue(step.Length > 1 ? GameAudioCue.UiConfirm : GameAudioCue.UiTick);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
                {
                    label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 2f)));
                    label.alpha = 1f - Mathf.Max(0f, t - 0.7f) / 0.3f;
                    yield return null;
                }
            }
            Destroy(label.gameObject);
            BeginPlay();
        }

        /// <summary>Skips the 3-2-1 intro (tests).</summary>
        public void SkipCountdown()
        {
            if (State != Phase.Countdown) return;
            StopAllCoroutines();
            var label = Root != null ? Root.Find("Countdown") : null;
            if (label != null) Destroy(label.gameObject);
            BeginPlay();
        }

        private void BeginPlay()
        {
            State = Phase.Playing;
            Elapsed = 0f;
            OnRoundStart();
        }

        private void Update()
        {
            if (State != Phase.Playing) return;
            float dt = Time.unscaledDeltaTime;
            Elapsed += dt;
            OnTick(dt);
            RefreshHud();
            if (State == Phase.Playing && Definition.timeLimitSeconds > 0f && Elapsed >= Definition.timeLimitSeconds) Finish(true);
        }

        protected void RefreshHud()
        {
            if (_time == null) return;
            float left = TimeLeft;
            _time.text = $"{Mathf.FloorToInt(left / 60f)}:{Mathf.FloorToInt(left % 60f):00}";
            _score.text = Score.ToString("N0");
            _combo.text = "x" + Combo;
            _lives.text = Lives > 0 ? new string('♥', Lives) : "—";
            if (_timeFill != null && Definition.timeLimitSeconds > 0f) UiKit.SetProgress(_timeFill, left / Definition.timeLimitSeconds);
        }

        // ─────────── Scoring ───────────

        /// <summary>A right answer: combo, points (more for a streak), a "+points" pop at <paramref name="at"/>.</summary>
        protected void Hit(string targetId, RectTransform at, int points = 100)
        {
            if (State != Phase.Playing) return;
            CorrectCount++;
            Combo++;
            BestCombo = Mathf.Max(BestCombo, Combo);
            int gained = points + (Combo - 1) * 20;
            Score += gained;
            Track(targetId);
            Cue(GameAudioCue.UiConfirm);
            Pop(at, Combo >= 3 ? $"+{gained}  コンボ x{Combo}!" : $"+{gained}", UiTheme.Good);
            RefreshHud();
        }

        /// <summary>A wrong answer or a target that got away: breaks the combo, may cost a life (game over at 0).</summary>
        protected void Miss(string expectedId, string chosenId, string note, RectTransform at, bool costsLife = true)
        {
            if (State != Phase.Playing) return;
            WrongCount++;
            Combo = 0;
            Score = Mathf.Max(0, Score - 20);
            Track(expectedId);
            if (!string.IsNullOrEmpty(expectedId)) _missed.Add(expectedId);
            if (!string.IsNullOrEmpty(note)) _mistakes.Add(new MiniGameMistake { expectedTargetId = expectedId, chosenTargetId = chosenId, note = note });
            Cue(GameAudioCue.UiError);
            Pop(at, "ちがう！", UiTheme.Bad);
            if (costsLife) Lives = Mathf.Max(0, Lives - 1);
            RefreshHud();
            if (costsLife && Lives <= 0) Finish(true);
        }

        /// <summary>Bonus points (speed, perfect order…).</summary>
        protected void AddScore(int points)
        {
            Score = Mathf.Max(0, Score + points);
            RefreshHud();
        }

        private void Track(string targetId)
        {
            if (!string.IsNullOrEmpty(targetId) && !_targets.Contains(targetId)) _targets.Add(targetId);
        }

        /// <summary>Tests: ends the round as if the time ran out.</summary>
        public void EndRound() => Finish(true);

        /// <summary>Ends the round (time up, game over or cleared). A round counts as completed when anything was answered right.</summary>
        protected void Finish(bool completed)
        {
            if (State == Phase.Finished) return;
            State = Phase.Finished;
            StartCoroutine(FinishSoon(completed && CorrectCount > 0));
        }

        private IEnumerator FinishSoon(bool completed)
        {
            var label = UiKit.Label(Root, "Over", Lives <= 0 ? "ゲームオーバー" : "タイムアップ！", 96f, Accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(label.rectTransform);
            yield return new WaitForSecondsRealtime(0.9f);
            Finished?.Invoke(BuildResult(completed));
        }

        private MiniGameResult BuildResult(bool completed)
        {
            int attempts = CorrectCount + WrongCount;
            var result = new MiniGameResult
            {
                gameId = GameId,
                contentId = Set != null ? Set.id : GameId,
                completed = completed,
                correctCount = CorrectCount,
                incorrectCount = WrongCount,
                accuracy = attempts > 0 ? CorrectCount / (float)attempts : 0f,
                completionSeconds = Elapsed,
                mistakes = new List<MiniGameMistake>(_mistakes),
                score = completed ? Score : CorrectCount * 50,
                bestCombo = BestCombo,
            };
            result.learningTargetIds.AddRange(_targets);
            result.masteredTargetIds.AddRange(_targets.Where(t => !_missed.Contains(t)));
            result.expReward = completed ? CorrectCount * Definition.expPerCorrect : 0;
            result.knowledgeReward = completed ? Definition.knowledgePerRound : 0;
            result.masteryGain = result.masteredTargetIds.Count * 5f - (result.learningTargetIds.Count - result.masteredTargetIds.Count) * 3f;
            result.stars = KanaMatchGame.Stars(result);
            if (completed)
            {
                result.tickets = KanaMatchGame.TicketsFor(result);
                PlayerInventory.Instance?.AddItem(KanaMatchGame.TicketItemId, "チケット", KanaMatchGame.TicketName, 0, result.tickets);
                string key = "NL.Arcade.Best." + GameId + "." + result.contentId;
                if (result.score > PlayerPrefs.GetInt(key, 0))
                {
                    result.newRecord = true;
                    PlayerPrefs.SetInt(key, result.score);
                    PlayerPrefs.Save();
                }
            }
            return result;
        }

        // ─────────── Helpers for games ───────────

        protected static void Cue(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }

        /// <summary>A short text that rises and fades over <paramref name="at"/> (or the stage centre).</summary>
        protected void Pop(RectTransform at, string text, Color color)
        {
            if (Root == null) return;
            var label = UiKit.Label(Root, "Float", text, 28f, color, FontStyles.Bold, TextAlignmentOptions.Center);
            label.enableAutoSizing = false;
            label.rectTransform.sizeDelta = new Vector2(360f, 44f);
            var anchor = at != null ? at : Stage != null ? Stage : Root;
            label.rectTransform.position = anchor.TransformPoint(anchor.rect.center);
            StartCoroutine(Rise(label));
        }

        private static IEnumerator Rise(TextMeshProUGUI label)
        {
            Vector3 start = label.rectTransform.position;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f)
            {
                if (label == null) yield break;
                label.rectTransform.position = start + Vector3.up * (70f * t);
                label.alpha = 1f - t * t;
                yield return null;
            }
            if (label != null) Destroy(label.gameObject);
        }

        protected IEnumerator Shake(RectTransform rect, float seconds = 0.3f)
        {
            if (rect == null) yield break;
            Vector2 p = rect.anchoredPosition;
            for (float t = 0f; t < 1f && rect != null; t += Time.unscaledDeltaTime / seconds)
            {
                rect.anchoredPosition = p + Vector2.right * Mathf.Sin(t * Mathf.PI * 8f) * 10f * (1f - t);
                yield return null;
            }
            if (rect != null) rect.anchoredPosition = p;
        }

        protected IEnumerator Bounce(RectTransform rect, float amount = 0.15f, float seconds = 0.25f)
        {
            for (float t = 0f; t < 1f && rect != null; t += Time.unscaledDeltaTime / seconds)
            {
                rect.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * amount);
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }

        /// <summary>A sprite for a picture word: the Codex-rendered art when present, then the game's own fallbacks.</summary>
        protected static Sprite Picture(params string[] resourcePaths)
        {
            foreach (string path in resourcePaths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var sprite = path.StartsWith("icon:") ? UiIcons.Get(path.Substring(5)) : NihongoLife.Interaction.EmoteCatalog.Load(path);
                if (sprite != null) return sprite;
            }
            return null;
        }

        protected void Clear()
        {
            StopAllCoroutines();
            if (Root == null) return;
            foreach (Transform child in Root) Destroy(child.gameObject);
        }

        protected virtual void OnDestroy() => StopAllCoroutines();
    }
}
