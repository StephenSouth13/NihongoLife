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
                count.text = $"{Loc.T("Số từ", "Words", "語数")}: <b>{words}</b>" + (group.minWords > 0 ? $"  /  {Loc.T("tối thiểu", "minimum", "最低")} {group.minWords}" : "") +
                             (LastSavedAt != default ? $"   ·   <color=#5C6B7A>{Loc.T("đã lưu tự động lúc", "autosaved at", "自動保存")} {LastSavedAt:HH:mm:ss}</color>" : "");
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
            if (part == 1 && !IsSubmitted) LiveExamBanner(sheet);
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
                PaperText(sheet, Loc.T("Không tìm thấy micro trên máy này: hãy cắm micro rồi mở lại bài. Bạn vẫn xem được đề để luyện nói.",
                    "No microphone found: plug one in and reopen the test. You can still read the questions to practise.",
                    "マイクが見つかりません。接続してから開き直してください。問題を見て練習することはできます。"), 15.5f, FontStyles.Bold).color = Bad;
            else if (!_micAllowed)
                PaperText(sheet, Loc.T("Game chưa được cấp quyền micro. Hãy cho phép trong cài đặt quyền riêng tư của hệ điều hành rồi mở lại bài.",
                    "The game has no microphone permission. Allow it in the system privacy settings, then reopen the test.",
                    "マイクの許可がありません。OSのプライバシー設定で許可してから開き直してください。"), 15.5f, FontStyles.Bold).color = Bad;

            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                int number = item.number;
                _anchors[number] = sheet;
                var box = Box(sheet, "Q" + number);
                if (part != 2) PaperText(box, $"<b>{number}.</b>  {item.text}", 17.5f, FontStyles.Normal);
                var row = Row(box, "Controls", 12f);
                var speakingRow = new SpeakingRow { Part = part, MaxSeconds = MaxSecondsForPart(part) };
                speakingRow.Record = UiKit.Button(row, "Record_" + number, null, null, null, () => ToggleRecording(number), UiKit.Kind.Danger, "mic", 15.5f, 22);
                UiKit.Size(speakingRow.Record, 200f, 44f);
                speakingRow.RecordLabel = UiKit.LabelOf(speakingRow.Record);
                speakingRow.Play = UiKit.Button(row, "Play_" + number, "Nghe lại", "Play back", "再生", () => PlayTake(number), UiKit.Kind.Accent, "play_arrow", 15.5f, 22);
                UiKit.Size(speakingRow.Play, 160f, 44f);
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
                    var prep = UiKit.Button(row, "Prepare_" + number, "Chuẩn bị 1 phút", "Prepare 1 min", "準備1分", () => StartPreparation(number), UiKit.Kind.Primary, "hourglass_top", 14.5f, 22);
                    UiKit.Size(prep, 190f, 44f);
                }
                _speakingRows[number] = speakingRow;
                _refreshers[number] = () => RefreshSpeakingRow(number);
                RefreshSpeakingRow(number);
            }
        }

        /// <summary>"Be examined by a teacher, live" — the AI cannot fully judge pronunciation from a recording.</summary>
        private void LiveExamBanner(RectTransform sheet)
        {
            var banner = Box(sheet, "LiveExamBanner");
            banner.GetComponent<Image>().color = new Color(1f, 0.95f, 0.96f, 1f);
            var row = Row(banner, "Row", 16f);
            var text = PaperText(row, Loc.T("<b>Muốn được chấm đủ cả phát âm?</b> Thi trực tiếp với giám khảo qua video — câu hỏi, đồng hồ và band hiện realtime, kết quả vào hộp thư.",
                "<b>Want pronunciation marked too?</b> Take it live with an examiner on video — questions, timers and bands appear in real time; the result goes to your mailbox.",
                "<b>発音まで採点してほしい？</b>試験官とビデオでライブ受験できます。質問・タイマー・バンドがリアルタイムで表示され、結果はボックスに届きます。"), 15f, FontStyles.Normal);
            UiKit.Size(text, 760f, -1f, 1f);
            var button = UiKit.Button(row, "LiveExam", "Thi với giám khảo", "Live with examiner", "試験官とライブ", OpenLiveExam, UiKit.Kind.Danger, "co_present", 15f);
            UiKit.Size(button, 230f, 46f);
        }

        private void OpenLiveExam()
        {
            var meeting = NihongoLife.Learning.EduMeetingManager.Instance ?? SceneRefs.Find<NihongoLife.Learning.EduMeetingManager>();
            if (meeting == null)
            {
                HudFeed.Post(Loc.T("Phòng thi trực tuyến chỉ có ở trường Hibari.", "The live exam room is at Hibari school.", "ライブ試験室はひばり学院にあります。"), HudFeed.Kind.Warning, 3.5f);
                return;
            }
            Close();
            meeting.OpenForSpeakingExam(_test.id, _test.DisplayTitle);
        }

        private void RefreshSpeakingRow(int number)
        {
            if (!_speakingRows.TryGetValue(number, out var row) || row.Status == null) return;
            bool recording = _recordingNumber == number && _mic.IsRecording;
            bool preparing = _prepNumber == number && _prepUntil > 0f;
            TryParseTake(GetAnswer(number), out string file, out float seconds);
            bool hasTake = file != null;
            row.RecordLabel.text = recording ? Loc.T("Dừng", "Stop", "停止") : hasTake ? Loc.T("Ghi lại", "Re-record", "録り直す") : Loc.T("Ghi âm", "Record", "録音");
            // Exam mode: one take per question, as in the real test.
            row.Record.interactable = !IsSubmitted && MicRecorder.HasDevice && _micAllowed && !preparing &&
                                      (recording || _recordingNumber < 0) && (!hasTake || !IsExam || recording);
            row.Play.interactable = hasTake && !recording;
            if (preparing) row.Status.text = Loc.F("Chuẩn bị: <b>{0}</b>s — ghi âm tự bắt đầu khi hết giờ", "Preparing: <b>{0}</b>s — recording starts when time is up", "準備中：<b>{0}</b>秒 — 時間になると録音が始まります", Mathf.CeilToInt(_prepUntil - Time.unscaledTime));
            else if (recording) row.Status.text = $"<color=#D0453A>● {Loc.T("Đang ghi", "Recording", "録音中")}</color>  {_mic.Elapsed:0}s / {row.MaxSeconds}s";
            else if (!string.IsNullOrEmpty(_mic.LastError) && _recordingNumber == -2 - number) row.Status.text = $"<color=#D0453A>{_mic.LastError}</color>";
            else row.Status.text = hasTake
                ? Loc.F("Đã ghi <b>{0:0}s</b>", "Recorded <b>{0:0}s</b>", "録音済み <b>{0:0}秒</b>", seconds) + (IsExam ? "" : Loc.T(" — có thể ghi lại", " — you can re-record", " — 録り直せます"))
                : Loc.F("Chưa ghi âm (tối đa {0}s)", "Not recorded (max {0}s)", "未録音（最大{0}秒）", row.MaxSeconds);
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
                string notReady = Loc.T("Dịch vụ chấm điểm chưa sẵn sàng (hãy mở bài từ trong game).", "The marking service is not ready (open the paper from inside the game).", "採点サービスが準備できていません（ゲーム内から開いてください）。");
                foreach (var task in record.writing) task.examination = new IeltsAiExamination { error = notReady };
                if (record.skill == "speaking") record.speakingExamination = new IeltsAiExamination { error = notReady };
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
            bool live = record.mode == "live";
            PaperText(summary, record.skill == "writing" ? Loc.T("KẾT QUẢ VIẾT · AI EXAMINER", "WRITING RESULT · AI EXAMINER", "ライティング結果 · AI試験官")
                             : live ? Loc.T("KẾT QUẢ NÓI · GIÁM KHẢO", "SPEAKING RESULT · EXAMINER", "スピーキング結果 · 試験官")
                             : Loc.T("KẾT QUẢ NÓI · AI EXAMINER", "SPEAKING RESULT · AI EXAMINER", "スピーキング結果 · AI試験官"), 15f, FontStyles.Bold).color = Accent;
            string overall = IsGrading ? $"<i>{Loc.T("Đang chấm…", "Marking…", "採点中…")}</i>" : record.overall > 0f ? $"<size=170%><b>{record.overall:0.0}</b></size>" : $"<i>{Loc.T("Chưa có band", "No band yet", "バンドはまだありません")}</i>";
            PaperText(summary, $"{(live ? Loc.T("Band", "Band", "バンド") : Loc.T("Band ước tính", "Estimated band", "推定バンド"))}: {overall}", 22f, FontStyles.Normal).name = "OverallBand";
            if (record.skill == "writing" && record.writing.Count > 1 && record.overall > 0f)
                PaperText(summary, Loc.T("Band Viết = (Task 1 + 2 × Task 2) / 3, làm tròn theo quy tắc IELTS. Band mỗi task = trung bình 4 tiêu chí, làm tròn xuống nửa band.",
                    "Writing band = (Task 1 + 2 × Task 2) / 3, IELTS rounding. Each task = mean of its 4 criteria, rounded down to the half band.",
                    "ライティングのバンド =（Task 1 + 2 × Task 2）÷ 3、IELTSの丸め。各タスク = 4項目の平均を0.5刻みで切り捨て。"), 14.5f, FontStyles.Italic).color = PaperMuted;
            if (record.skill == "speaking" && record.speakingExamination != null && record.speakingExamination.criteria.Any(c => c.code == "P" && !c.assessed))
                PaperText(summary, Loc.T("<b>Band chưa gồm Phát âm:</b> AI không nghe đủ rõ để đánh giá Phát âm từ bản ghi, nên band chỉ tính trên 3 tiêu chí còn lại.",
                    "<b>Band without Pronunciation:</b> the AI could not hear the recording well enough to judge Pronunciation, so the band uses the other 3 criteria.",
                    "<b>発音を含まないバンド：</b>録音から発音を評価できなかったため、残り3項目で算出しています。"), 15f, FontStyles.Normal).color = Bad;
            if (!live)
            {
                PaperText(summary, Loc.T("Band do AI ước tính theo mô tả tiêu chí IELTS công khai — không phải kết quả chính thức. Chỉ bài làm gốc được chấm; bản viết lại chỉ để luyện, không tính vào band.",
                    "The band is an AI estimate from the public IELTS criteria — not an official result. Only the original is marked; rewrites are for practice and never banded.",
                    "バンドは公開されたIELTS基準によるAIの推定で、公式結果ではありません。採点は最初の答案のみで、書き直しは練習用です。"), 14.5f, FontStyles.Italic).color = PaperMuted;
                PaperText(summary, Loc.F("Lượt chấm AI còn lại hôm nay: <b>{0}</b> (kết quả đã chấm được lưu lại, xem lại không tốn lượt).", "AI markings left today: <b>{0}</b> (marked results are saved; viewing them again is free).", "本日のAI採点の残り回数：<b>{0}</b>（採点結果は保存され、見直しは無料です）。", ExamGradingService.RequestsLeftToday), 14.5f, FontStyles.Normal).color = PaperMuted;
            }

            if (record.skill == "writing") foreach (var task in record.writing) WritingTaskResult(content, task);
            else SpeakingResult(content, record);

            ProgressHistory(content, record.skill);

            var buttons = Row(content, "Buttons", 12f);
            bool failed = !IsGrading && (record.skill == "writing" ? record.writing.Any(t => t.examination == null || !t.examination.Ok) : record.speakingExamination == null || !record.speakingExamination.Ok);
            if (failed && !live) UiKit.Size(UiKit.Button(buttons, "Regrade", "Chấm lại", "Mark again", "再採点", RegradeProductive, UiKit.Kind.Primary, "refresh", 16f), 190f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Again", "Làm bài mới (luyện tập)", "New paper (practice)", "新しく練習する", () => Begin(_package, PracticeMode, false), UiKit.Kind.Accent, "replay", 16f), 280f, 50f);
            if (record.skill == "speaking") UiKit.Size(UiKit.Button(buttons, "Live", "Thi với giám khảo", "Live with examiner", "試験官とライブ", () => OpenLiveExam(), UiKit.Kind.Danger, "co_present", 16f), 240f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Close", "Đóng", "Close", "閉じる", Close, UiKit.Kind.Secondary, "close", 16f), 160f, 50f);
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
                var band = Cell(row, c.assessed ? $"<size=130%><b>{c.band:0.0}</b></size>" : $"<i>{Loc.T("chưa chấm", "not marked", "未採点")}</i>", 110f, FontStyles.Normal);
                band.name = "Band_" + c.code;
                Cell(row, c.explanation ?? "", 640f, FontStyles.Normal);
            }
        }

        private void WritingTaskResult(RectTransform content, IeltsWritingTaskRecord task)
        {
            var box = Box(content, "Task" + task.task);
            box.name = "TaskResult_" + task.task;
            var exam = task.examination;
            string band = exam != null && exam.Ok ? $"{Loc.T("Band task", "Task band", "タスクのバンド")}: <b>{exam.overall:0.0}</b>" : "";
            PaperText(box, $"<b>WRITING TASK {task.task}</b>   ·   {task.words} {Loc.T("từ", "words", "語")}{(task.words < task.minWords ? $" <color=#D0453A>({Loc.T("dưới", "under", "未満")} {task.minWords})</color>" : "")}   {band}", 18f, FontStyles.Normal);
            if (exam == null) PaperText(box, IsGrading ? $"<i>{Loc.T("Đang chờ AI Examiner…", "Waiting for the AI Examiner…", "AI試験官を待っています…")}</i>" : $"<i>{Loc.T("Chưa chấm.", "Not marked yet.", "未採点です。")}</i>", 16f, FontStyles.Normal);
            else if (!exam.Ok) PaperText(box, "⚠ " + exam.error, 16f, FontStyles.Bold).color = Bad;
            else
            {
                CriteriaTable(box, exam);
                if (!string.IsNullOrEmpty(exam.summary)) PaperText(box, $"<b>{Loc.T("Nhận xét chung", "Overall comment", "総評")}:</b> " + exam.summary, 16f, FontStyles.Normal);
                if (exam.fromCache) PaperText(box, Loc.T("(kết quả đã lưu — không tốn lượt chấm)", "(saved result — no marking used)", "（保存済みの結果 — 採点回数は使いません）"), 13.5f, FontStyles.Italic).color = PaperMuted;
            }

            // The original text, with the Coach's sentences marked.
            PaperText(box, $"<b>{Loc.T("Bài gốc của bạn", "Your original essay", "あなたの元の答案")}</b>", 16.5f, FontStyles.Normal);
            var marked = new HashSet<int>((task.coach?.issues ?? new List<IeltsCoachIssue>()).Select(i => i.sentence));
            var sentences = ExamGradingService.SplitSentences(task.original);
            string essay = sentences.Count == 0 ? $"<i>{Loc.T("(bỏ trống)", "(blank)", "（空欄）")}</i>" : string.Join(" ", sentences.Select((s, i) => marked.Contains(i + 1) ? $"<mark=#F4C43066><b><size=80%>[{i + 1}]</size></b> {Escape(s)}</mark>" : Escape(s)));
            PaperText(box, essay, 16.5f, FontStyles.Normal).name = "OriginalEssay_" + task.task;

            // AI Coach.
            if (task.coach == null)
            {
                var coachRow = Row(box, "CoachRow", 12f);
                UiKit.Size(UiKit.Button(coachRow, "Coach_" + task.task, "AI Coach: góp ý từng câu", "AI Coach: sentence by sentence", "AIコーチ：一文ずつ添削", () => RequestCoach(task.number), UiKit.Kind.Accent, "auto_awesome", 15.5f), 320f, 46f);
                return;
            }
            CoachReport(box, task.coach, Loc.T("AI COACH · bài gốc", "AI COACH · original", "AIコーチ · 元の答案"));
            if (!task.coach.Ok || task.coach.overallAdvice == "…") return;

            // Rewrite.
            PaperText(box, Loc.T("<b>Viết lại</b> — sửa theo góp ý rồi gửi để Coach xem lại. Bản viết lại được lưu nhưng <b>không chấm band</b>.",
                "<b>Rewrite</b> — fix it following the comments, then send it to the Coach. Rewrites are saved but <b>never banded</b>.",
                "<b>書き直し</b> — コメントに沿って直し、コーチに送ります。書き直しは保存されますが<b>バンドは付きません</b>。"), 16f, FontStyles.Normal);
            var input = MakeInput(box, 1060f, 280f, multiline: true);
            input.name = "Rewrite_" + task.task;
            string draft = _rewriteDrafts.TryGetValue(task.number, out string d) ? d : task.rewrites.Count > 0 ? task.rewrites[^1].text : task.original;
            input.text = draft;
            input.onValueChanged.AddListener(v => _rewriteDrafts[task.number] = v);
            var row = Row(box, "RewriteRow", 12f);
            UiKit.Size(UiKit.Button(row, "SendRewrite_" + task.task, "Gửi bản viết lại cho Coach", "Send the rewrite to the Coach", "書き直しをコーチに送る", () => SubmitRewrite(task.number, input.text), UiKit.Kind.Success, "send", 15.5f), 340f, 46f);
            for (int i = 0; i < task.rewrites.Count; i++)
                CoachReport(box, task.rewrites[i].coach, Loc.F("AI COACH · bản viết lại {0} ({1} từ)", "AI COACH · rewrite {0} ({1} words)", "AIコーチ · 書き直し{0}（{1}語）", i + 1, SpeechAudio.CountWords(task.rewrites[i].text)));
        }

        private void CoachReport(RectTransform parent, IeltsCoachReport report, string title)
        {
            var box = Box(parent, "Coach");
            box.GetComponent<Image>().color = new Color(0.99f, 0.98f, 0.93f, 1f);
            PaperText(box, $"<b>{title}</b>", 16f, FontStyles.Normal).color = Accent;
            if (report == null) return;
            if (!report.Ok) { PaperText(box, "⚠ " + report.error, 15.5f, FontStyles.Bold).color = Bad; return; }
            if (report.overallAdvice == "…") { PaperText(box, $"<i>{Loc.T("Đang nhờ AI Coach đọc…", "The AI Coach is reading…", "AIコーチが読んでいます…")}</i>", 15.5f, FontStyles.Normal); return; }
            if (report.issues.Count == 0) PaperText(box, Loc.T("Không tìm thấy lỗi đáng kể.", "No significant problems found.", "大きな問題は見つかりませんでした。"), 15.5f, FontStyles.Normal);
            foreach (var issue in report.issues)
            {
                var item = Column(box, "Issue", 2f);
                PaperText(item, $"<b>{Loc.F("Câu {0}", "Sentence {0}", "{0}文目", issue.sentence)}</b> · <color=#5C6B7A>{issue.category}</color> · {Escape(issue.problem)}", 15.5f, FontStyles.Normal);
                PaperText(item, $"<color=#B53A2E><s>{Escape(issue.original)}</s></color>   →   <color=#2C8A55><b>{Escape(issue.suggestion)}</b></color>", 15.5f, FontStyles.Normal);
                PaperText(item, Escape(issue.explanation), 14.5f, FontStyles.Normal).color = PaperMuted;
            }
            if (!string.IsNullOrEmpty(report.overallAdvice)) PaperText(box, $"<b>{Loc.T("Lời khuyên", "Advice", "アドバイス")}:</b> " + Escape(report.overallAdvice), 15.5f, FontStyles.Normal);
        }

        private void SpeakingResult(RectTransform content, IeltsProductiveRecord record)
        {
            var exam = record.speakingExamination;
            var box = Box(content, "SpeakingCriteria");
            if (exam == null) PaperText(box, IsGrading ? $"<i>{Loc.T("Đang gửi bản ghi cho AI Examiner…", "Sending the recordings to the AI Examiner…", "録音をAI試験官に送っています…")}</i>" : $"<i>{Loc.T("Chưa chấm.", "Not marked yet.", "未採点です。")}</i>", 16f, FontStyles.Normal);
            else if (!exam.Ok) PaperText(box, "⚠ " + exam.error, 16f, FontStyles.Bold).color = Bad;
            else
            {
                CriteriaTable(box, exam);
                if (!string.IsNullOrEmpty(exam.summary)) PaperText(box, $"<b>{Loc.T("Nhận xét chung", "Overall comment", "総評")}:</b> " + exam.summary, 16f, FontStyles.Normal);
                if (record.mode != "live")
                    PaperText(box, Loc.T("Độ trôi chảy dựa thêm vào số đo thời gian nói đo trực tiếp từ âm thanh trên máy bạn; Phát âm chỉ chấm từ âm thanh (không suy ra từ bản chép lời).",
                        "Fluency also uses speech timing measured from the audio on your device; Pronunciation is judged from the audio only (never from the transcript).",
                        "流暢さは端末で音声から測った発話時間も使います。発音は音声のみで評価します（書き起こしからは推測しません）。"), 14f, FontStyles.Italic).color = PaperMuted;
            }
            foreach (var answer in record.speaking)
            {
                var item = Box(content, "Answer" + answer.number);
                PaperText(item, $"<b>Part {answer.part} · {Loc.F("Câu {0}", "Question {0}", "問{0}", answer.number)}.</b> {Escape(answer.question)}", 16.5f, FontStyles.Normal);
                if (string.IsNullOrEmpty(answer.audioFile)) { PaperText(item, $"<i>{Loc.T("Không có bản ghi.", "No recording.", "録音がありません。")}</i>", 15f, FontStyles.Normal).color = Bad; continue; }
                var row = Row(item, "Row", 12f);
                int number = answer.number;
                UiKit.Size(UiKit.Button(row, "PlayResult_" + number, "Nghe", "Play", "再生", () => PlayTake(number), UiKit.Kind.Accent, "play_arrow", 14.5f), 130f, 40f);
                var m = answer.metrics ?? new IeltsSpeakingMetrics();
                Cell(row, Loc.F("{0:0}s ghi · {1:0}s có tiếng nói · {2} lần ngừng >0,5s (dài nhất {3:0.0}s)", "{0:0}s recorded · {1:0}s of speech · {2} pauses >0.5s (longest {3:0.0}s)", "録音{0:0}秒 · 発話{1:0}秒 · 0.5秒超の間 {2}回（最長{3:0.0}秒）", m.seconds, m.speechSeconds, m.pauses, m.longestPause)
                    + (m.wordsPerMinute > 0f ? Loc.F(" · {0:0} từ/phút", " · {0:0} words/min", " · 毎分{0:0}語", m.wordsPerMinute) : ""), 900f, FontStyles.Normal).color = PaperMuted;
                PaperText(item, string.IsNullOrEmpty(answer.transcript) ? $"<i>{Loc.T("Bản chép lời: (chưa có)", "Transcript: (none yet)", "書き起こし：（まだありません）")}</i>" : $"<b>{Loc.T("Bản chép lời", "Transcript", "書き起こし")}:</b> " + Escape(answer.transcript), 15.5f, FontStyles.Normal).name = "Transcript_" + number;
            }
        }

        /// <summary>Progress over earlier papers of the same skill (original submissions only).</summary>
        private void ProgressHistory(RectTransform content, string skill)
        {
            var records = IeltsProductiveStore.All(skill).Where(r => r.overall > 0f).ToList();
            var box = Box(content, "Progress");
            string skillName = skill == "writing" ? Loc.T("VIẾT", "WRITING", "ライティング") : Loc.T("NÓI", "SPEAKING", "スピーキング");
            PaperText(box, $"<b>{Loc.T("TIẾN BỘ", "PROGRESS", "上達の記録")} · {skillName}</b>  <size=80%>{Loc.T("(chỉ tính bài gốc)", "(original papers only)", "（元の答案のみ）")}</size>", 16f, FontStyles.Normal).color = Accent;
            if (records.Count == 0) { PaperText(box, Loc.T("Chưa có bài nào được chấm.", "No marked paper yet.", "採点済みの答案はまだありません。"), 15f, FontStyles.Normal); return; }
            string line = string.Join("   →   ", records.TakeLast(8).Select(r => $"{(DateTime.TryParse(r.finishedUtc, out var t) ? t.ToLocalTime().ToString("dd/MM") : "?")}: <b>{r.overall:0.0}</b>"));
            PaperText(box, line, 16f, FontStyles.Normal);
            if (records.Count >= 2)
            {
                float change = records[^1].overall - records[^2].overall;
                PaperText(box, change > 0f ? Loc.F("Tăng <b>+{0:0.0}</b> so với bài trước.", "Up <b>+{0:0.0}</b> on the previous paper.", "前回より<b>+{0:0.0}</b>上がりました。", change)
                             : change < 0f ? Loc.F("Giảm <b>{0:0.0}</b> so với bài trước.", "Down <b>{0:0.0}</b> on the previous paper.", "前回より<b>{0:0.0}</b>下がりました。", change)
                             : Loc.T("Giữ nguyên so với bài trước.", "Same as the previous paper.", "前回と同じです。"), 15f, FontStyles.Normal).color = change >= 0f ? Good : Bad;
            }
        }

        /// <summary>Learner/AI text shown verbatim (a "&lt;" in an essay must not become a TMP tag).</summary>
        private static string Escape(string text) => string.IsNullOrEmpty(text) ? "" : "<noparse>" + text.Replace("</noparse>", "") + "</noparse>";
    }
}
