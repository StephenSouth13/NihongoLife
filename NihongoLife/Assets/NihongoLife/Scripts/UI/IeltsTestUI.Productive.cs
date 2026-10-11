using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam;
using NihongoLife.Exam.Ielts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// IELTS Writing and Speaking papers in the same test screen as Listening/Reading:
    /// · Writing — Task 1 (with its table) and Task 2, live word count, autosave stamp; after submitting, the AI
    ///   Examiner scores the four criteria of each task (overall computed with IELTS rounding, Task 2 weighted ×2),
    ///   the AI Coach marks problems sentence by sentence and the learner can rewrite; rewrites are coached but
    ///   never scored.
    /// · Speaking — Part 1/2/3 questions, one recording per question (Part 2 with a one-minute preparation
    ///   timer), per-part length caps, playback; on submit the audio goes to the Examiner for transcripts and
    ///   FC/LR/GRA/P (P judged from the audio only), with speech timing measured on the device.
    /// Everything is saved (IeltsProductiveStore): original, examination, coaching, rewrites and the band history.
    /// </summary>
    public sealed partial class IeltsTestUI
    {
        private const string RecPrefix = "rec:";

        private bool IsProductive => _test != null && (_test.skill == "writing" || _test.skill == "speaking");
        private bool IsSpeakingPaper => _test != null && _test.skill == "speaking";

        private readonly MicRecorder _mic = new MicRecorder();
        private int _recordingNumber = -1;
        private int _prepNumber = -1;
        private float _prepUntil = -1f;
        private readonly Dictionary<int, SpeakingRow> _speakingRows = new();
        private readonly Dictionary<int, string> _rewriteDrafts = new();
        private readonly HashSet<int> _coachOpen = new();
        private bool _micAllowed = true;

        public DateTime LastSavedAt { get; private set; }
        public IeltsProductiveRecord ProductiveRecord { get; private set; }
        public bool IsGrading { get; private set; }
        public int RecordingNumber => _recordingNumber;

        private sealed class SpeakingRow
        {
            public TextMeshProUGUI Status;
            public TextMeshProUGUI RecordLabel;
            public Button Record;
            public Button Play;
            public RectTransform Meter;
            public int Part;
            public int MaxSeconds;
        }

        private void ResetProductive()
        {
            StopMicIfRecording();
            _prepNumber = -1;
            _prepUntil = -1f;
            _speakingRows.Clear();
            _rewriteDrafts.Clear();
            _coachOpen.Clear();
            ProductiveRecord = null;
            IsGrading = false;
            if (IsSpeakingPaper) StartCoroutine(MicRecorder.RequestPermission(ok => _micAllowed = ok));
        }

        private void StopMicIfRecording()
        {
            if (_mic.IsRecording) FinishRecording();
            _recordingNumber = -1;
        }

        // ─────────── Writing paper ───────────

        /// <summary>The part title is already printed above the groups: repeat a heading only when it says more.</summary>
        private bool RepeatsPartTitle(IeltsGroup group)
        {
            var part = _test.parts.FirstOrDefault(p => p.groups != null && p.groups.Contains(group));
            return part != null && string.Equals((group.heading ?? "").Trim(), (part.title ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void RenderWriting(RectTransform sheet, IeltsGroup group)
        {
            if (!RepeatsPartTitle(group)) PaperText(sheet, group.heading ?? "Writing task", 20f, FontStyles.Bold);
            if (group.minutes > 0) PaperText(sheet, $"You should spend about {group.minutes} minutes on this task.", 16.5f, FontStyles.Italic);
            var prompt = Box(sheet, "Prompt");
            PaperText(prompt, group.stem ?? "", 17.5f, FontStyles.Normal);
            if (group.lines != null && group.lines.Any(l => l.style == "table" || l.style == "table_head")) DataTable(prompt, group.lines);
            if (group.minWords > 0) PaperText(sheet, $"Write at least {group.minWords} words.", 16.5f, FontStyles.Italic);
            int number = group.from;
            _anchors[number] = sheet;
            var input = MakeInput(sheet, 1060f, number == 1 && group.minWords < 200 ? 320f : 460f, multiline: true);
            input.name = "Essay_" + number;
            input.text = GetAnswer(number);
            input.interactable = !IsSubmitted;
            var count = PaperText(sheet, "", 15f, FontStyles.Normal);
            count.name = "WordCount_" + number;
            void Count(string v)
            {
                int words = SpeechAudio.CountWords(v);
                count.text = $"Số từ: <b>{words}</b>" + (group.minWords > 0 ? $"  /  tối thiểu {group.minWords}" : "") +
                             (LastSavedAt != default ? $"   ·   <color=#5C6B7A>đã lưu tự động lúc {LastSavedAt:HH:mm:ss}</color>" : "");
                count.color = group.minWords > 0 && words < group.minWords ? Bad : Good;
            }
            input.onValueChanged.AddListener(v => { SetAnswer(number, v); Count(v); });
            _refreshers[number] = () => Count(input.text);
            Count(input.text);
        }

        /// <summary>Task 1 data as a real table: "label" is the row header, "text" the cells separated by " | ".</summary>
        private void DataTable(RectTransform parent, IeltsLine[] lines)
        {
            var table = Column(parent, "DataTable", 0f);
            foreach (var line in lines.Where(l => l.style == "table" || l.style == "table_head"))
            {
                bool head = line.style == "table_head";
                var row = Row(table, "Row", 0f);
                row.gameObject.AddComponent<Image>().color = head ? new Color(0.86f, 0.9f, 0.95f, 1f) : new Color(1f, 1f, 1f, 1f);
                var outline = row.gameObject.AddComponent<Outline>();
                outline.effectColor = PaperLine;
                Cell(row, line.label ?? "", 220f, head ? FontStyles.Bold : FontStyles.Bold);
                foreach (string cell in (line.text ?? "").Split('|')) Cell(row, cell.Trim(), 250f, head ? FontStyles.Bold : FontStyles.Normal).alignment = TextAlignmentOptions.Center;
            }
        }

        // ─────────── Speaking paper ───────────

        private static int MaxSecondsForPart(int part) => part == 2 ? 120 : part == 3 ? 75 : 40;

        private void RenderSpeaking(RectTransform sheet, IeltsGroup group)
        {
            int part = Array.FindIndex(_test.parts, p => p.groups != null && p.groups.Contains(group)) + 1;
            part = part > 0 ? _test.parts[part - 1].number : 1;
            if (!RepeatsPartTitle(group)) PaperText(sheet, group.heading ?? "Speaking", 20f, FontStyles.Bold);
            if (!string.IsNullOrEmpty(group.instruction)) PaperText(sheet, group.instruction, 16f, FontStyles.Italic).color = PaperMuted;
            if (part == 2 && !string.IsNullOrEmpty(group.stem))
            {
                var card = Box(sheet, "CueCard");
                PaperText(card, $"<b>{group.stem}</b>", 18f, FontStyles.Normal);
                if (group.lines != null && group.lines.Length > 0)
                {
                    PaperText(card, "You should say:", 16.5f, FontStyles.Normal);
                    foreach (var line in group.lines) PaperText(card, "   •  " + line.text, 16.5f, FontStyles.Normal);
                }
            }
            if (!MicRecorder.HasDevice)
                PaperText(sheet, "Không tìm thấy micro trên máy này: hãy cắm micro rồi mở lại bài. Bạn vẫn xem được đề để luyện nói.", 15.5f, FontStyles.Bold).color = Bad;
            else if (!_micAllowed)
                PaperText(sheet, "Game chưa được cấp quyền micro. Hãy cho phép trong cài đặt quyền riêng tư của hệ điều hành rồi mở lại bài.", 15.5f, FontStyles.Bold).color = Bad;

            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                int number = item.number;
                _anchors[number] = sheet;
                var box = Box(sheet, "Q" + number);
                if (part != 2) PaperText(box, $"<b>{number}.</b>  {item.text}", 17.5f, FontStyles.Normal);
                var row = Row(box, "Controls", 12f);
                var speakingRow = new SpeakingRow { Part = part, MaxSeconds = MaxSecondsForPart(part) };
                speakingRow.Record = NLUi.Button(row, "Record_" + number, "● Ghi âm", _font, () => ToggleRecording(number), Bad, 16f, Color.white, 44f);
                NLUi.Size(speakingRow.Record, 200f, 44f);
                speakingRow.RecordLabel = speakingRow.Record.GetComponentInChildren<TextMeshProUGUI>();
                speakingRow.Play = NLUi.Button(row, "Play_" + number, "▶ Nghe lại", _font, () => PlayTake(number), Accent, 16f, Color.white, 44f);
                NLUi.Size(speakingRow.Play, 160f, 44f);
                var meterBack = new GameObject("Meter", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
                meterBack.SetParent(row, false);
                meterBack.GetComponent<Image>().color = new Color(0.86f, 0.87f, 0.85f, 1f);
                var meterElement = meterBack.GetComponent<LayoutElement>();
                meterElement.preferredWidth = meterElement.minWidth = 140f; meterElement.preferredHeight = meterElement.minHeight = 12f;
                speakingRow.Meter = new GameObject("Level", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                speakingRow.Meter.SetParent(meterBack, false);
                speakingRow.Meter.anchorMin = Vector2.zero; speakingRow.Meter.anchorMax = new Vector2(0f, 1f);
                speakingRow.Meter.pivot = new Vector2(0f, 0.5f); speakingRow.Meter.sizeDelta = Vector2.zero;
                speakingRow.Meter.GetComponent<Image>().color = Good;
                speakingRow.Status = PaperText(row, "", 15.5f, FontStyles.Normal);
                NLUi.Size(speakingRow.Status, 420f);
                if (part == 2)
                {
                    var prep = NLUi.Button(row, "Prepare_" + number, "Chuẩn bị 1 phút", _font, () => StartPreparation(number), NLUi.Gold, 15f, new Color(0.1f, 0.08f, 0.04f), 44f);
                    NLUi.Size(prep, 180f, 44f);
                }
                _speakingRows[number] = speakingRow;
                _refreshers[number] = () => RefreshSpeakingRow(number);
                RefreshSpeakingRow(number);
            }
        }

        private void RefreshSpeakingRow(int number)
        {
            if (!_speakingRows.TryGetValue(number, out var row) || row.Status == null) return;
            bool recording = _recordingNumber == number && _mic.IsRecording;
            bool preparing = _prepNumber == number && _prepUntil > 0f;
            TryParseTake(GetAnswer(number), out string file, out float seconds);
            bool hasTake = file != null;
            row.RecordLabel.text = recording ? "■ Dừng" : hasTake ? "● Ghi lại" : "● Ghi âm";
            // Exam mode: one take per question, as in the real test.
            row.Record.interactable = !IsSubmitted && MicRecorder.HasDevice && _micAllowed && !preparing &&
                                      (recording || _recordingNumber < 0) && (!hasTake || !IsExam || recording);
            row.Play.interactable = hasTake && !recording;
            if (preparing) row.Status.text = $"Chuẩn bị: <b>{Mathf.CeilToInt(_prepUntil - Time.unscaledTime)}</b>s — ghi âm tự bắt đầu khi hết giờ";
            else if (recording) row.Status.text = $"<color=#D0453A>● Đang ghi</color>  {_mic.Elapsed:0}s / {row.MaxSeconds}s";
            else if (!string.IsNullOrEmpty(_mic.LastError) && _recordingNumber == -2 - number) row.Status.text = $"<color=#D0453A>{_mic.LastError}</color>";
            else row.Status.text = hasTake ? $"Đã ghi <b>{seconds:0}s</b>" + (IsExam ? "" : " — có thể ghi lại") : $"Chưa ghi âm (tối đa {row.MaxSeconds}s)";
        }

        private void StartPreparation(int number)
        {
            if (_mic.IsRecording || IsSubmitted) return;
            _prepNumber = number;
            _prepUntil = Time.unscaledTime + 60f;
            RefreshSpeakingRow(number);
        }

        private void ToggleRecording(int number)
        {
            if (_mic.IsRecording) { if (_recordingNumber == number) FinishRecording(); return; }
            if (_prepNumber == number) { _prepNumber = -1; _prepUntil = -1f; }
            int max = _speakingRows.TryGetValue(number, out var row) ? row.MaxSeconds : 60;
            if (!_mic.Start(max))
            {
                _recordingNumber = -2 - number; // remembers which row shows the error
                RefreshSpeakingRow(number);
                return;
            }
            _recordingNumber = number;
            RefreshSpeakingRow(number);
        }

        private void FinishRecording()
        {
            int number = _recordingNumber;
            var samples = _mic.Stop(out int frequency);
            _recordingNumber = -1;
            if (number < 0) return;
            if (samples.Length < frequency / 2) { RefreshSpeakingRow(number); return; } // under half a second: ignore
            StoreTake(number, samples, frequency);
        }

        /// <summary>Saves a take as the answer to <paramref name="number"/> (also used by tests with prepared audio).</summary>
        public void StoreTake(int number, float[] samples, int frequency)
        {
            if (IsSubmitted) return;
            string folder = Path.Combine(IeltsAttemptStore.Folder, "speaking_drafts");
            Directory.CreateDirectory(folder);
            string file = $"{_test.id}_{number}.wav";
            File.WriteAllBytes(Path.Combine(folder, file), SpeechAudio.EncodeWav(SpeechAudio.Resample(samples, frequency, MicRecorder.SampleRate), MicRecorder.SampleRate));
            SetAnswer(number, $"{RecPrefix}{file}|{samples.Length / (float)frequency:0.0}");
            Save();
        }

        private static bool TryParseTake(string value, out string file, out float seconds)
        {
            file = null; seconds = 0f;
            if (string.IsNullOrEmpty(value) || !value.StartsWith(RecPrefix)) return false;
            string[] bits = value.Substring(RecPrefix.Length).Split('|');
            file = bits[0];
            if (bits.Length > 1) float.TryParse(bits[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            return true;
        }

        private string DraftPath(string file) => Path.Combine(IeltsAttemptStore.Folder, "speaking_drafts", file);

        private void PlayTake(int number)
        {
            string path = null;
            if (ProductiveRecord != null)
            {
                var answer = ProductiveRecord.speaking.FirstOrDefault(a => a.number == number);
                if (answer != null) path = Path.Combine(IeltsProductiveStore.AudioFolder(ProductiveRecord.id), answer.audioFile);
            }
            else if (TryParseTake(GetAnswer(number), out string file, out _)) path = DraftPath(file);
            if (path == null || !File.Exists(path)) return;
            var samples = SpeechAudio.DecodeWav(File.ReadAllBytes(path), out int frequency);
            if (samples.Length == 0) return;
            var clip = AudioClip.Create("Take_" + number, samples.Length, 1, frequency, false);
            clip.SetData(samples, 0);
            _audio.Stop();
            _audio.clip = clip;
            _audio.Play();
        }

        private void TickProductive()
        {
            if (!IsSpeakingPaper || IsSubmitted) return;
            if (_prepNumber >= 0 && _prepUntil > 0f && Time.unscaledTime >= _prepUntil)
            {
                int number = _prepNumber;
                _prepNumber = -1; _prepUntil = -1f;
                ToggleRecording(number);
            }
            if (_mic.IsRecording && _mic.ReachedLimit) FinishRecording();
            foreach (var kv in _speakingRows)
            {
                if (kv.Value.Meter != null)
                    kv.Value.Meter.anchorMax = new Vector2(_recordingNumber == kv.Key ? _mic.Level : 0f, 1f);
                if (_recordingNumber == kv.Key || _prepNumber == kv.Key) RefreshSpeakingRow(kv.Key);
            }
        }

        // ─────────── Submitting and grading ───────────

        private void SubmitProductive()
        {
            StopMicIfRecording();
            Save();
            _audio.Stop();
            _confirm.gameObject.SetActive(false);
            var record = new IeltsProductiveRecord
            {
                id = IeltsProductiveStore.NewId(_test.id), testId = _test.id, testTitle = _test.title, skill = _test.skill,
                mode = _attempt.mode, finishedUtc = DateTime.UtcNow.ToString("o"), elapsedSeconds = _attempt.elapsedSeconds,
            };
            foreach (var part in _test.parts)
                foreach (var group in part.groups ?? Array.Empty<IeltsGroup>())
                {
                    if (group.type == "writing_task")
                    {
                        string table = group.lines == null ? "" : "\n" + string.Join("\n", group.lines.Where(l => l.style != null && l.style.StartsWith("table")).Select(l => $"{l.label} | {l.text}"));
                        string text = GetAnswer(group.from);
                        record.writing.Add(new IeltsWritingTaskRecord
                        {
                            number = group.from, task = part.number, prompt = (group.stem ?? "") + table, original = text,
                            words = SpeechAudio.CountWords(text), minWords = group.minWords,
                        });
                    }
                    else if (group.type == "speaking_part")
                    {
                        foreach (var item in group.items ?? Array.Empty<IeltsItem>())
                        {
                            var answer = new IeltsSpeakingAnswer { number = item.number, part = part.number, question = item.text };
                            if (TryParseTake(GetAnswer(item.number), out string file, out _) && File.Exists(DraftPath(file)))
                            {
                                Directory.CreateDirectory(IeltsProductiveStore.AudioFolder(record.id));
                                File.Copy(DraftPath(file), Path.Combine(IeltsProductiveStore.AudioFolder(record.id), file), true);
                                answer.audioFile = file;
                                var samples = SpeechAudio.DecodeWav(File.ReadAllBytes(DraftPath(file)), out int frequency);
                                answer.metrics = SpeechAudio.Analyze(samples, frequency);
                            }
                            record.speaking.Add(answer);
                        }
                    }
                }
            _attempt.submitted = true;
            IeltsAttemptStore.ClearAttempt(_test.id);
            IeltsProductiveStore.Save(record);
            ProductiveRecord = record;
            _submitButton.interactable = false;
            StartCoroutine(GradeProductive());
        }

        /// <summary>Opens a submitted paper from anywhere (the exam mailbox): its test screen with the full results —
        /// criteria, coaching, rewrites, recordings — and "Chấm lại" if it was never graded.</summary>
        public static IeltsTestUI OpenSaved(IeltsProductiveRecord record)
        {
            if (record == null) return null;
            var package = IeltsLibrary.Discover().FirstOrDefault(p => p.Id == record.testId);
            if (package == null) return null;
            var ui = Open(package, record.mode == ExamMode ? ExamMode : PracticeMode, resume: false);
            ui._attempt.submitted = true;
            ui._submitButton.interactable = false;
            ui.ShowSavedRecord(record);
            return ui;
        }

        /// <summary>Opens a saved paper (e.g. to grade it again once online).</summary>
        public void ShowSavedRecord(IeltsProductiveRecord record)
        {
            ProductiveRecord = record;
            ShowProductiveResults();
        }

        public void RegradeProductive()
        {
            if (ProductiveRecord == null || IsGrading) return;
            StartCoroutine(GradeProductive());
        }

        private IEnumerator GradeProductive()
        {
            var record = ProductiveRecord;
            IsGrading = true;
            ShowProductiveResults();
            if (!GameServices.TryGet(out ExamGradingService grading))
            {
                foreach (var task in record.writing) task.examination = new IeltsAiExamination { error = "Dịch vụ chấm điểm chưa sẵn sàng (hãy mở bài từ trong game)." };
                if (record.skill == "speaking") record.speakingExamination = new IeltsAiExamination { error = "Dịch vụ chấm điểm chưa sẵn sàng (hãy mở bài từ trong game)." };
            }
            else if (record.skill == "writing")
            {
                foreach (var task in record.writing)
                {
                    if (task.examination != null && task.examination.Ok) continue;
                    bool done = false;
                    grading.ExamineWriting(task, result => { task.examination = result; done = true; });
                    while (!done) yield return null;
                    IeltsProductiveStore.Save(record);
                    ShowProductiveResults();
                }
                var t1 = record.writing.FirstOrDefault(t => t.task == 1);
                var t2 = record.writing.FirstOrDefault(t => t.task == 2);
                bool ok1 = t1?.examination != null && t1.examination.Ok, ok2 = t2?.examination != null && t2.examination.Ok;
                record.overall = ok1 || ok2 ? IeltsBands.WritingOverall(ok1 ? t1.examination.overall : 0f, ok2 ? t2.examination.overall : 0f, ok1, ok2) : 0f;
            }
            else
            {
                var takes = new List<ExamGradingService.SpeakingTake>();
                foreach (var answer in record.speaking.Where(a => !string.IsNullOrEmpty(a.audioFile)))
                {
                    string path = Path.Combine(IeltsProductiveStore.AudioFolder(record.id), answer.audioFile);
                    if (!File.Exists(path)) continue;
                    var samples = SpeechAudio.DecodeWav(File.ReadAllBytes(path), out int frequency);
                    takes.Add(new ExamGradingService.SpeakingTake { Answer = answer, Samples = samples, Frequency = frequency });
                }
                bool done = false;
                grading.ExamineSpeaking(takes, result => { record.speakingExamination = result; done = true; });
                while (!done) yield return null;
                record.overall = record.speakingExamination.Ok ? record.speakingExamination.overall : 0f;
            }
            // The band history only ever receives the original submission's band.
            if (record.overall > 0f && !record.countedInHistory)
            {
                record.countedInHistory = true;
                IeltsAttemptStore.AddHistory(new IeltsHistoryEntry { testId = record.testId, finishedUtc = record.finishedUtc, mode = record.mode, band = record.overall });
            }
            IeltsProductiveStore.Save(record);
            IsGrading = false;
            ShowProductiveResults();
            ExamMailboxUI.NotifyGraded(record);
        }

        /// <summary>AI Coach on the original text of a task.</summary>
        public void RequestCoach(int taskNumber)
        {
            var task = ProductiveRecord?.writing.FirstOrDefault(t => t.number == taskNumber);
            if (task == null || !GameServices.TryGet(out ExamGradingService grading)) return;
            _coachOpen.Add(taskNumber);
            task.coach = new IeltsCoachReport { error = null, overallAdvice = "…" };
            ShowProductiveResults();
            grading.CoachWriting(task, task.original, null, report =>
            {
                task.coach = report;
                IeltsProductiveStore.Save(ProductiveRecord);
                ShowProductiveResults();
            });
        }

        /// <summary>Coaching on a rewrite: saved with the task, never scored.</summary>
        public void SubmitRewrite(int taskNumber, string text)
        {
            var task = ProductiveRecord?.writing.FirstOrDefault(t => t.number == taskNumber);
            if (task == null || string.IsNullOrWhiteSpace(text) || !GameServices.TryGet(out ExamGradingService grading)) return;
            var rewrite = new IeltsRewrite { text = text, createdUtc = DateTime.UtcNow.ToString("o"), coach = new IeltsCoachReport { overallAdvice = "…" } };
            task.rewrites.Add(rewrite);
            _rewriteDrafts[taskNumber] = text;
            IeltsProductiveStore.Save(ProductiveRecord);
            ShowProductiveResults();
            string previous = task.rewrites.Count > 1 ? task.rewrites[task.rewrites.Count - 2].text : task.original;
            grading.CoachWriting(task, text, previous, report =>
            {
                rewrite.coach = report;
                IeltsProductiveStore.Save(ProductiveRecord);
                ShowProductiveResults();
            });
        }

        // ─────────── Results ───────────

        private void ShowProductiveResults()
        {
            var record = ProductiveRecord;
            if (record == null) return;
            _bodyArea.gameObject.SetActive(false);
            float scroll = 1f;
            var oldScroll = _results.GetComponentInChildren<ScrollRect>();
            if (oldScroll != null) scroll = oldScroll.verticalNormalizedPosition;
            foreach (Transform child in _results) Destroy(child.gameObject);
            _results.gameObject.SetActive(true);
            var view = MakeScroll(_results, "ResultScroll", out var content);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(0f, 12f); rect.offsetMax = new Vector2(0f, -12f);

            var summary = Box(content, "Summary");
            PaperText(summary, record.skill == "writing" ? "KẾT QUẢ WRITING · AI EXAMINER" : "KẾT QUẢ SPEAKING · AI EXAMINER", 15f, FontStyles.Bold).color = Accent;
            string overall = IsGrading ? "<i>Đang chấm…</i>" : record.overall > 0f ? $"<size=170%><b>{record.overall:0.0}</b></size>" : "<i>Chưa có band</i>";
            PaperText(summary, $"Band ước tính: {overall}", 22f, FontStyles.Normal).name = "OverallBand";
            if (record.skill == "writing" && record.writing.Count > 1 && record.overall > 0f)
                PaperText(summary, "Band Writing = (Task 1 + 2 × Task 2) / 3, làm tròn theo quy tắc IELTS. Band mỗi task = trung bình 4 tiêu chí, làm tròn xuống nửa band.", 14.5f, FontStyles.Italic).color = PaperMuted;
            if (record.skill == "speaking" && record.speakingExamination != null && record.speakingExamination.criteria.Any(c => c.code == "P" && !c.assessed))
                PaperText(summary, "<b>Band chưa gồm Phát âm:</b> AI không nghe đủ rõ để đánh giá Pronunciation từ bản ghi, nên band chỉ tính trên 3 tiêu chí còn lại.", 15f, FontStyles.Normal).color = Bad;
            PaperText(summary, "Band do AI ước tính theo mô tả tiêu chí IELTS công khai — không phải kết quả chính thức. Chỉ bài làm gốc được chấm; bản viết lại chỉ để luyện, không tính vào band.", 14.5f, FontStyles.Italic).color = PaperMuted;
            PaperText(summary, $"Lượt chấm AI còn lại hôm nay: <b>{ExamGradingService.RequestsLeftToday}</b> (kết quả đã chấm được lưu lại, xem lại không tốn lượt).", 14.5f, FontStyles.Normal).color = PaperMuted;

            if (record.skill == "writing") foreach (var task in record.writing) WritingTaskResult(content, task);
            else SpeakingResult(content, record);

            ProgressHistory(content, record.skill);

            var buttons = Row(content, "Buttons", 12f);
            bool failed = !IsGrading && (record.skill == "writing" ? record.writing.Any(t => t.examination == null || !t.examination.Ok) : record.speakingExamination == null || !record.speakingExamination.Ok);
            if (failed) NLUi.Size(NLUi.Button(buttons, "Regrade", "Chấm lại", _font, RegradeProductive, NLUi.Gold, 17f, new Color(0.1f, 0.08f, 0.04f), 50f), 180f, 50f);
            NLUi.Size(NLUi.Button(buttons, "Again", "Làm bài mới (luyện tập)", _font, () => Begin(_package, PracticeMode, false), Accent, 17f, Color.white, 50f), 270f, 50f);
            NLUi.Size(NLUi.Button(buttons, "Close", "Đóng", _font, Close, new Color(0.3f, 0.34f, 0.4f), 17f, Color.white, 50f), 140f, 50f);
            Canvas.ForceUpdateCanvases();
            view.verticalNormalizedPosition = scroll;
        }

        private void CriteriaTable(RectTransform parent, IeltsAiExamination exam)
        {
            foreach (var c in exam.criteria)
            {
                var row = Row(parent, "Criterion_" + c.code, 12f);
                row.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f, 1f);
                Cell(row, $"<b>{c.code}</b>", 60f, FontStyles.Normal);
                Cell(row, c.name ?? "", 250f, FontStyles.Normal);
                var band = Cell(row, c.assessed ? $"<size=130%><b>{c.band:0.0}</b></size>" : "<i>chưa chấm</i>", 110f, FontStyles.Normal);
                band.name = "Band_" + c.code;
                Cell(row, c.explanation ?? "", 640f, FontStyles.Normal);
            }
        }

        private void WritingTaskResult(RectTransform content, IeltsWritingTaskRecord task)
        {
            var box = Box(content, "Task" + task.task);
            box.name = "TaskResult_" + task.task;
            var exam = task.examination;
            string band = exam != null && exam.Ok ? $"Band task: <b>{exam.overall:0.0}</b>" : "";
            PaperText(box, $"<b>WRITING TASK {task.task}</b>   ·   {task.words} từ{(task.words < task.minWords ? $" <color=#D0453A>(dưới {task.minWords})</color>" : "")}   {band}", 18f, FontStyles.Normal);
            if (exam == null) PaperText(box, IsGrading ? "<i>Đang chờ AI Examiner…</i>" : "<i>Chưa chấm.</i>", 16f, FontStyles.Normal);
            else if (!exam.Ok) PaperText(box, "⚠ " + exam.error, 16f, FontStyles.Bold).color = Bad;
            else
            {
                CriteriaTable(box, exam);
                if (!string.IsNullOrEmpty(exam.summary)) PaperText(box, "<b>Nhận xét chung:</b> " + exam.summary, 16f, FontStyles.Normal);
                if (exam.fromCache) PaperText(box, "(kết quả đã lưu — không tốn lượt chấm)", 13.5f, FontStyles.Italic).color = PaperMuted;
            }

            // The original text, with the Coach's sentences marked.
            PaperText(box, "<b>Bài gốc của bạn</b>", 16.5f, FontStyles.Normal);
            var marked = new HashSet<int>((task.coach?.issues ?? new List<IeltsCoachIssue>()).Select(i => i.sentence));
            var sentences = ExamGradingService.SplitSentences(task.original);
            string essay = sentences.Count == 0 ? "<i>(bỏ trống)</i>" : string.Join(" ", sentences.Select((s, i) => marked.Contains(i + 1) ? $"<mark=#F4C43066><b><size=80%>[{i + 1}]</size></b> {Escape(s)}</mark>" : Escape(s)));
            PaperText(box, essay, 16.5f, FontStyles.Normal).name = "OriginalEssay_" + task.task;

            // AI Coach.
            if (task.coach == null)
            {
                var coachRow = Row(box, "CoachRow", 12f);
                NLUi.Size(NLUi.Button(coachRow, "Coach_" + task.task, "AI Coach: góp ý từng câu", _font, () => RequestCoach(task.number), Accent, 16f, Color.white, 46f), 300f, 46f);
                return;
            }
            CoachReport(box, task.coach, "AI COACH · bài gốc");
            if (!task.coach.Ok || task.coach.overallAdvice == "…") return;

            // Rewrite.
            PaperText(box, "<b>Viết lại</b> — sửa theo góp ý rồi gửi để Coach xem lại. Bản viết lại được lưu nhưng <b>không chấm band</b>.", 16f, FontStyles.Normal);
            var input = MakeInput(box, 1060f, 280f, multiline: true);
            input.name = "Rewrite_" + task.task;
            string draft = _rewriteDrafts.TryGetValue(task.number, out string d) ? d : task.rewrites.Count > 0 ? task.rewrites[^1].text : task.original;
            input.text = draft;
            input.onValueChanged.AddListener(v => _rewriteDrafts[task.number] = v);
            var row = Row(box, "RewriteRow", 12f);
            NLUi.Size(NLUi.Button(row, "SendRewrite_" + task.task, "Gửi bản viết lại cho Coach", _font, () => SubmitRewrite(task.number, input.text), Good, 16f, Color.white, 46f), 320f, 46f);
            for (int i = 0; i < task.rewrites.Count; i++)
                CoachReport(box, task.rewrites[i].coach, $"AI COACH · bản viết lại {i + 1} ({SpeechAudio.CountWords(task.rewrites[i].text)} từ)");
        }

        private void CoachReport(RectTransform parent, IeltsCoachReport report, string title)
        {
            var box = Box(parent, "Coach");
            box.GetComponent<Image>().color = new Color(0.99f, 0.98f, 0.93f, 1f);
            PaperText(box, $"<b>{title}</b>", 16f, FontStyles.Normal).color = Accent;
            if (report == null) return;
            if (!report.Ok) { PaperText(box, "⚠ " + report.error, 15.5f, FontStyles.Bold).color = Bad; return; }
            if (report.overallAdvice == "…") { PaperText(box, "<i>Đang nhờ AI Coach đọc…</i>", 15.5f, FontStyles.Normal); return; }
            if (report.issues.Count == 0) PaperText(box, "Không tìm thấy lỗi đáng kể.", 15.5f, FontStyles.Normal);
            foreach (var issue in report.issues)
            {
                var item = Column(box, "Issue", 2f);
                PaperText(item, $"<b>Câu {issue.sentence}</b> · <color=#5C6B7A>{issue.category}</color> · {Escape(issue.problem)}", 15.5f, FontStyles.Normal);
                PaperText(item, $"<color=#B53A2E><s>{Escape(issue.original)}</s></color>   →   <color=#2C8A55><b>{Escape(issue.suggestion)}</b></color>", 15.5f, FontStyles.Normal);
                PaperText(item, Escape(issue.explanation), 14.5f, FontStyles.Normal).color = PaperMuted;
            }
            if (!string.IsNullOrEmpty(report.overallAdvice)) PaperText(box, "<b>Lời khuyên:</b> " + Escape(report.overallAdvice), 15.5f, FontStyles.Normal);
        }

        private void SpeakingResult(RectTransform content, IeltsProductiveRecord record)
        {
            var exam = record.speakingExamination;
            var box = Box(content, "SpeakingCriteria");
            if (exam == null) PaperText(box, IsGrading ? "<i>Đang gửi bản ghi cho AI Examiner…</i>" : "<i>Chưa chấm.</i>", 16f, FontStyles.Normal);
            else if (!exam.Ok) PaperText(box, "⚠ " + exam.error, 16f, FontStyles.Bold).color = Bad;
            else
            {
                CriteriaTable(box, exam);
                if (!string.IsNullOrEmpty(exam.summary)) PaperText(box, "<b>Nhận xét chung:</b> " + exam.summary, 16f, FontStyles.Normal);
                PaperText(box, "Fluency dựa thêm vào số đo thời gian nói đo trực tiếp từ âm thanh trên máy bạn; Pronunciation chỉ chấm từ âm thanh (không suy ra từ transcript).", 14f, FontStyles.Italic).color = PaperMuted;
            }
            foreach (var answer in record.speaking)
            {
                var item = Box(content, "Answer" + answer.number);
                PaperText(item, $"<b>Part {answer.part} · Câu {answer.number}.</b> {Escape(answer.question)}", 16.5f, FontStyles.Normal);
                if (string.IsNullOrEmpty(answer.audioFile)) { PaperText(item, "<i>Không có bản ghi.</i>", 15f, FontStyles.Normal).color = Bad; continue; }
                var row = Row(item, "Row", 12f);
                int number = answer.number;
                NLUi.Size(NLUi.Button(row, "PlayResult_" + number, "▶ Nghe", _font, () => PlayTake(number), Accent, 15f, Color.white, 40f), 120f, 40f);
                var m = answer.metrics ?? new IeltsSpeakingMetrics();
                Cell(row, $"{m.seconds:0}s ghi · {m.speechSeconds:0}s có tiếng nói · {m.pauses} lần ngừng >0,5s (dài nhất {m.longestPause:0.0}s)" + (m.wordsPerMinute > 0f ? $" · {m.wordsPerMinute:0} từ/phút" : ""), 900f, FontStyles.Normal).color = PaperMuted;
                PaperText(item, string.IsNullOrEmpty(answer.transcript) ? "<i>Transcript: (chưa có)</i>" : "<b>Transcript:</b> " + Escape(answer.transcript), 15.5f, FontStyles.Normal).name = "Transcript_" + number;
            }
        }

        /// <summary>Progress over earlier papers of the same skill (original submissions only).</summary>
        private void ProgressHistory(RectTransform content, string skill)
        {
            var records = IeltsProductiveStore.All(skill).Where(r => r.overall > 0f).ToList();
            var box = Box(content, "Progress");
            PaperText(box, $"<b>TIẾN BỘ {skill.ToUpperInvariant()}</b>  <size=80%>(chỉ tính bài gốc)</size>", 16f, FontStyles.Normal).color = Accent;
            if (records.Count == 0) { PaperText(box, "Chưa có bài nào được chấm.", 15f, FontStyles.Normal); return; }
            string line = string.Join("   →   ", records.TakeLast(8).Select(r => $"{(DateTime.TryParse(r.finishedUtc, out var t) ? t.ToLocalTime().ToString("dd/MM") : "?")}: <b>{r.overall:0.0}</b>"));
            PaperText(box, line, 16f, FontStyles.Normal);
            if (records.Count >= 2)
            {
                float change = records[^1].overall - records[^2].overall;
                PaperText(box, change > 0f ? $"Tăng <b>+{change:0.0}</b> so với bài trước." : change < 0f ? $"Giảm <b>{change:0.0}</b> so với bài trước." : "Giữ nguyên so với bài trước.", 15f, FontStyles.Normal).color = change >= 0f ? Good : Bad;
            }
        }

        /// <summary>Learner/AI text shown verbatim (a "&lt;" in an essay must not become a TMP tag).</summary>
        private static string Escape(string text) => string.IsNullOrEmpty(text) ? "" : "<noparse>" + text.Replace("</noparse>", "") + "</noparse>";
    }
}
