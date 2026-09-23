using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;

namespace NihongoLife.Exam
{
    /// <summary>
    /// Runtime exam-taking engine, shared by JLPT and IELTS (same navigation/timer/scoring machinery;
    /// only the section types and scoring formula differ, both driven entirely by the ExamDefinition data).
    ///
    /// Session shape: sections are attempted in order. Within a section the learner may freely revisit
    /// its own questions; once a section is submitted (by the learner or by its timer running out) it
    /// locks and the next section starts — this matches how a real JLPT/IELTS session behaves and is
    /// intentional, not a limitation to fix later.
    /// </summary>
    public class ExamManager : MonoBehaviour, IGameService
    {
        public static ExamManager Instance { get; private set; }

        public event Action OnStateChanged;
        public event Action<float, float> OnTimerTick; // (secondsRemaining, sectionDuration)
        public event Action<ExamAttemptResult> OnExamFinished;
        public event Action<string> OnGradingStatus;
        public event Action<string> OnSectionLocked; // sectionId, fired when a section times out or is submitted

        private ExamDefinition _exam;
        private readonly List<bool> _sectionLocked = new List<bool>();
        private int _sectionIndex;
        private int _questionIndex;
        private float _sectionTimeRemaining;
        private bool _sectionTimed;
        private readonly Dictionary<string, ExamQuestionRecord> _answers = new Dictionary<string, ExamQuestionRecord>();
        private readonly Dictionary<string, float[]> _speakingSamples = new Dictionary<string, float[]>();
        private readonly Dictionary<string, int> _speakingSampleRates = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _passagePlays = new Dictionary<string, int>();
        private bool _finished;

        public void Initialize() { Instance = this; }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public ExamDefinition CurrentExam => _exam;
        public bool IsAttemptActive => _exam != null && !_finished;
        public int SectionIndex => _sectionIndex;
        public int QuestionIndex => _questionIndex;
        public ExamSection CurrentSection => _exam != null && _sectionIndex < _exam.sections.Count ? _exam.sections[_sectionIndex] : null;
        public ExamQuestion CurrentQuestion
        {
            get
            {
                var section = CurrentSection;
                return section != null && _questionIndex < section.questions.Count ? section.questions[_questionIndex] : null;
            }
        }

        public bool IsSectionLocked(int index) => index >= 0 && index < _sectionLocked.Count && _sectionLocked[index];
        public bool IsCurrentSectionLocked => IsSectionLocked(_sectionIndex);

        // ──────────────────────── Lifecycle ────────────────────────

        public void StartAttempt(ExamDefinition exam)
        {
            _exam = exam;
            _sectionIndex = 0;
            _questionIndex = 0;
            _finished = false;
            _answers.Clear();
            _speakingSamples.Clear();
            _speakingSampleRates.Clear();
            _passagePlays.Clear();
            _sectionLocked.Clear();
            for (int i = 0; i < exam.sections.Count; i++) _sectionLocked.Add(false);

            StartSectionTimer();
            OnStateChanged?.Invoke();
        }

        private void StartSectionTimer()
        {
            var section = CurrentSection;
            _sectionTimed = section != null && section.timeLimitSeconds > 0;
            _sectionTimeRemaining = section != null ? section.timeLimitSeconds : 0f;
        }

        private void Update()
        {
            if (!IsAttemptActive || IsCurrentSectionLocked || !_sectionTimed) return;

            _sectionTimeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Max(0f, _sectionTimeRemaining), CurrentSection.timeLimitSeconds);
            if (_sectionTimeRemaining <= 0f)
            {
                _sectionTimeRemaining = 0f;
                LockCurrentSectionAndAdvance();
            }
        }

        // ──────────────────────── Navigation ────────────────────────

        public bool GoToQuestion(int index)
        {
            var section = CurrentSection;
            if (section == null || index < 0 || index >= section.questions.Count || IsCurrentSectionLocked) return false;
            _questionIndex = index;
            OnStateChanged?.Invoke();
            return true;
        }

        public bool NextQuestion() => GoToQuestion(_questionIndex + 1);
        public bool PreviousQuestion() => GoToQuestion(_questionIndex - 1);

        /// <summary>Learner-initiated: submit the current section early and move to the next one.</summary>
        public void SubmitCurrentSection()
        {
            LockCurrentSectionAndAdvance();
        }

        private void LockCurrentSectionAndAdvance()
        {
            if (IsCurrentSectionLocked) return;

            var section = CurrentSection;
            if (section != null) _sectionLocked[_sectionIndex] = true;
            OnSectionLocked?.Invoke(section != null ? section.id : string.Empty);

            if (_sectionIndex + 1 < _exam.sections.Count)
            {
                _sectionIndex++;
                _questionIndex = 0;
                StartSectionTimer();
                OnStateChanged?.Invoke();
            }
            else
            {
                FinishExam();
            }
        }

        // ──────────────────────── Answers ────────────────────────

        public ExamQuestionRecord GetAnswer(string questionId)
        {
            return _answers.TryGetValue(questionId, out var record) ? record : null;
        }

        public void SubmitChoice(int choiceIndex)
        {
            var question = CurrentQuestion;
            if (question == null || IsCurrentSectionLocked) return;

            bool correct = choiceIndex == question.correctChoiceIndex;
            _answers[question.id] = new ExamQuestionRecord { questionId = question.id, givenAnswer = choiceIndex.ToString(), correct = correct, rawAnswerText = choiceIndex >= 0 && choiceIndex < question.choices.Count ? Pick(question.choices[choiceIndex]) : string.Empty };
            OnStateChanged?.Invoke();
        }

        public void SubmitFillBlank(string text)
        {
            var question = CurrentQuestion;
            if (question == null || IsCurrentSectionLocked) return;

            string normalized = Normalize(text);
            bool correct = question.acceptedAnswers != null && question.acceptedAnswers.Exists(a => Normalize(a) == normalized);
            _answers[question.id] = new ExamQuestionRecord { questionId = question.id, givenAnswer = text, correct = correct, rawAnswerText = text };
            OnStateChanged?.Invoke();
        }

        public void SubmitEssayText(string text)
        {
            var question = CurrentQuestion;
            if (question == null || IsCurrentSectionLocked) return;
            // Essay correctness is not binary; graded later. "correct" stays false until AI grading fills feedback.
            _answers[question.id] = new ExamQuestionRecord { questionId = question.id, givenAnswer = text, correct = false, rawAnswerText = text };
            OnStateChanged?.Invoke();
        }

        public void SubmitSpeakingRecording(float[] samples, int frequency)
        {
            var question = CurrentQuestion;
            if (question == null || IsCurrentSectionLocked) return;
            _speakingSamples[question.id] = samples;
            _speakingSampleRates[question.id] = frequency;
            _answers[question.id] = new ExamQuestionRecord { questionId = question.id, givenAnswer = "[recording]", correct = false, rawAnswerText = string.Empty };
            OnStateChanged?.Invoke();
        }

        /// <summary>JLPT listening enforces a limited number of plays per passage (usually 1-2). Returns
        /// true and consumes one play if the learner is still allowed to hear it.</summary>
        public bool TryConsumePassagePlay(ExamPassage passage)
        {
            if (passage == null) return true;
            if (passage.maxPlays <= 0) return true;

            _passagePlays.TryGetValue(passage.id, out int used);
            if (used >= passage.maxPlays) return false;
            _passagePlays[passage.id] = used + 1;
            return true;
        }

        public int RemainingPlays(ExamPassage passage)
        {
            if (passage == null || passage.maxPlays <= 0) return int.MaxValue;
            _passagePlays.TryGetValue(passage.id, out int used);
            return Mathf.Max(0, passage.maxPlays - used);
        }

        // ──────────────────────── Finish + scoring ────────────────────────

        public void FinishExam()
        {
            if (_finished) return;
            _finished = true;
            for (int i = 0; i < _sectionLocked.Count; i++) _sectionLocked[i] = true;
            StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            var result = new ExamAttemptResult { exam = _exam };
            result.record.examId = _exam.id;
            result.record.completedAtUtcTicks = DateTime.UtcNow.Ticks;

            int aiSectionCount = 0;
            foreach (var section in _exam.sections)
            {
                if (RequiresAiGrading(section)) aiSectionCount++;
            }

            int aiSectionsDone = 0;
            var gradingService = GameServices.TryGet(out ExamGradingService grading) ? grading : null;

            foreach (var section in _exam.sections)
            {
                var sectionResult = new ExamSectionResult { sectionId = section.id };

                if (RequiresAiGrading(section))
                {
                    aiSectionsDone++;
                    OnGradingStatus?.Invoke($"Đang chấm {Localize(section)} ({aiSectionsDone}/{aiSectionCount})...");
                    yield return GradeAiSection(section, gradingService, sectionResult);
                }
                else
                {
                    ScoreObjectiveSection(section, sectionResult);
                }

                result.record.sections.Add(sectionResult);
            }

            foreach (var pair in _answers) result.record.answers.Add(pair.Value);

            FinalizeScoring(result);
            yield return SaveResult(result);

            OnGradingStatus?.Invoke(string.Empty);
            OnExamFinished?.Invoke(result);
        }

        private static bool RequiresAiGrading(ExamSection section)
        {
            if (section?.questions == null) return false;
            return section.questions.Exists(q => q != null && (q.type == ExamQuestionType.Essay || q.type == ExamQuestionType.SpeakingPrompt));
        }

        private void ScoreObjectiveSection(ExamSection section, ExamSectionResult sectionResult)
        {
            int scored = 0;
            int max = 0;
            foreach (var question in section.questions)
            {
                if (question == null) continue;
                max += question.points;
                if (_answers.TryGetValue(question.id, out var record) && record.correct) scored += question.points;
            }

            sectionResult.maxPoints = section.scoreScaleMax;
            sectionResult.scoredPoints = max > 0 ? Mathf.RoundToInt((float)scored / max * section.scoreScaleMax) : 0;
        }

        private IEnumerator GradeAiSection(ExamSection section, ExamGradingService grading, ExamSectionResult sectionResult)
        {
            sectionResult.aiGraded = true;
            var bandVotes = new List<float>();
            var feedbacks = new List<string>();

            foreach (var question in section.questions)
            {
                if (question == null) continue;
                bool done = false;
                ExamAiGrade grade = null;

                if (question.type == ExamQuestionType.Essay)
                {
                    string text = _answers.TryGetValue(question.id, out var record) ? record.givenAnswer : string.Empty;
                    if (grading != null)
                    {
                        grading.GradeEssay(question, _exam, text, g => { grade = g; done = true; });
                    }
                    else
                    {
                        grade = new ExamAiGrade { band = 0f, feedback = "Dịch vụ chấm bài chưa sẵn sàng." };
                        done = true;
                    }
                }
                else if (question.type == ExamQuestionType.SpeakingPrompt)
                {
                    _speakingSamples.TryGetValue(question.id, out var samples);
                    _speakingSampleRates.TryGetValue(question.id, out var rate);
                    if (grading != null)
                    {
                        grading.GradeSpeaking(question, _exam, samples, rate <= 0 ? 44100 : rate, g => { grade = g; done = true; });
                    }
                    else
                    {
                        grade = new ExamAiGrade { band = 0f, feedback = "Dịch vụ chấm bài chưa sẵn sàng." };
                        done = true;
                    }
                }
                else
                {
                    // A non-AI question inside an AI-graded section (rare, but handle it): score objectively.
                    bool correct = _answers.TryGetValue(question.id, out var rec) && rec.correct;
                    bandVotes.Add(correct ? 9f : 0f);
                    continue;
                }

                float timeout = 45f;
                while (!done && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (grade != null)
                {
                    bandVotes.Add(grade.band);
                    if (!string.IsNullOrWhiteSpace(grade.feedback)) feedbacks.Add(grade.feedback);
                    if (_answers.TryGetValue(question.id, out var answered))
                    {
                        answered.correct = grade.band >= 5f;
                        if (!string.IsNullOrWhiteSpace(grade.transcript)) answered.rawAnswerText = grade.transcript;
                    }
                }
            }

            float averageBand = bandVotes.Count > 0 ? Average(bandVotes) : 0f;
            sectionResult.feedback = string.Join(" ", feedbacks);
            if (_exam.examType == ExamType.Ielts)
            {
                sectionResult.maxPoints = 9;
                sectionResult.scoredPoints = Mathf.RoundToInt(RoundToHalf(averageBand) * 10f); // stored *10 so it stays an int (e.g. 65 = band 6.5)
            }
            else
            {
                sectionResult.maxPoints = section.scoreScaleMax;
                sectionResult.scoredPoints = Mathf.RoundToInt(averageBand / 9f * section.scoreScaleMax);
            }
        }

        private void FinalizeScoring(ExamAttemptResult result)
        {
            if (_exam.examType == ExamType.Jlpt)
            {
                int total = 0;
                bool allSectionsPass = true;
                foreach (var section in result.record.sections)
                {
                    total += section.scoredPoints;
                    if (section.scoredPoints < _exam.jlptSectionPassScore) allSectionsPass = false;
                }

                result.record.totalScore = total;
                result.record.totalMaxScore = SumSectionScales();
                result.record.passed = allSectionsPass && total >= _exam.jlptTotalPassScore;
            }
            else
            {
                var bands = new List<float>();
                foreach (var section in result.record.sections)
                {
                    float band = section.aiGraded ? section.scoredPoints / 10f : (section.maxPoints > 0 ? (float)section.scoredPoints / section.maxPoints * 9f : 0f);
                    bands.Add(band);
                }

                result.record.estimatedBand = bands.Count > 0 ? RoundToHalf(Average(bands)) : 0f;
                result.record.totalScore = Mathf.RoundToInt(result.record.estimatedBand * 10f);
                result.record.totalMaxScore = 90;
            }
        }

        private int SumSectionScales()
        {
            int total = 0;
            foreach (var section in _exam.sections) total += section.scoreScaleMax;
            return total;
        }

        private IEnumerator SaveResult(ExamAttemptResult result)
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) yield break;

            var progress = repository.GetProgress();
            if (progress == null) yield break;
            if (progress.examAttempts == null) progress.examAttempts = new List<ExamAttemptRecord>();

            progress.examAttempts.Add(result.record);

            ExamAttemptRecord best = null;
            foreach (var attempt in progress.examAttempts)
            {
                if (attempt.examId != result.record.examId) continue;
                if (best == null || IsBetter(attempt, best, _exam.examType)) best = attempt;
            }

            result.isBestAttempt = ReferenceEquals(best, result.record) || (best != null && best.completedAtUtcTicks == result.record.completedAtUtcTicks);
            repository.SaveProgress(progress);
            yield return null;
        }

        private static bool IsBetter(ExamAttemptRecord a, ExamAttemptRecord b, ExamType type)
        {
            return type == ExamType.Jlpt ? a.totalScore > b.totalScore : a.estimatedBand > b.estimatedBand;
        }

        // ──────────────────────── Helpers ────────────────────────

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        }

        private static float Average(List<float> values)
        {
            float sum = 0f;
            foreach (var v in values) sum += v;
            return values.Count > 0 ? sum / values.Count : 0f;
        }

        private static float RoundToHalf(float value) => Mathf.Round(value * 2f) / 2f;

        private static string Pick(ExamChoice choice)
        {
            if (choice == null) return string.Empty;
            return GameServices.TryGet(out GameSettingsService settings) ? settings.Text(choice.textVi, choice.textEn, choice.textJa) : choice.textVi;
        }

        private static string Localize(ExamSection section)
        {
            if (!GameServices.TryGet(out GameSettingsService settings)) return section.titleVi;
            return settings.Text(section.titleVi, section.titleEn, section.titleJa);
        }
    }
}
