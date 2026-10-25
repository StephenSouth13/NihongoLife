using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;

namespace NihongoLife.Exam
{
    /// <summary>
    /// Exam AI settings read from the Control Database. Writing/Speaking are only recorded by default
    /// (AiGradingEnabled = false): no automatic AI call, no quota, no network. A learner may still ask for an
    /// optional AI grade of one saved submission (first few free, then in-game yen), see AiGradeCredits.
    /// </summary>
    public static class ExamAiSettings
    {
        public const int DefaultFreeGradings = 3;
        public const int DefaultPriceYen = 50;

        /// <summary>Tests only: forces the automatic-grading flag regardless of the database.</summary>
        public static bool? AutoGradingOverride;

        private static GameControlDatabase Db => GameServices.TryGet(out GameControlService control) ? control.Database : null;

        /// <summary>Automatic AI grading when an exam/paper is submitted. Off by default.</summary>
        public static bool AiGradingEnabled => AutoGradingOverride ?? (Db != null && Db.aiGradingEnabled);
        /// <summary>The learner may ask for an AI grade of one pending submission (free uses, then yen).</summary>
        public static bool OnDemandGradingAllowed => Db == null || Db.allowOnDemandAiGrading;
        public static int FreeGradings => Db != null ? Mathf.Max(0, Db.aiFreeGradings) : DefaultFreeGradings;
        public static int PriceYen => Db != null ? Mathf.Max(0, Db.aiGradingPriceYen) : DefaultPriceYen;
    }

    public static class SubmissionStatus
    {
        public const string Pending = "pending";
        public const string Graded = "graded";
    }

    [Serializable]
    public class ExamSubmissionItem
    {
        public string questionId;
        /// <summary>writing | speaking</summary>
        public string kind;
        public string question;
        /// <summary>Path relative to the submission folder (essay .txt or recording .wav).</summary>
        public string answerPath;
        public int wordCount;
        public float durationSeconds;
        public int sampleRate;
        public string status = SubmissionStatus.Pending;
        public float band;
        public string feedback;
    }

    /// <summary>manifest.json inside persistentDataPath/exam_submissions/&lt;exam&gt;/&lt;timestamp&gt;/.</summary>
    [Serializable]
    public class ExamSubmissionManifest
    {
        public string examId;
        public string examType;
        public string createdUtc;
        public string status = SubmissionStatus.Pending;
        public List<ExamSubmissionItem> items = new List<ExamSubmissionItem>();
        [NonSerialized] public string folder;
    }

    [Serializable]
    public class SubmissionGradeResult
    {
        public bool ok;
        public float band;
        public string feedback;
        public string error;
    }

    /// <summary>Plug point for a future AI (or human) grader of pending submissions. Nothing calls one
    /// automatically while ExamAiSettings.AiGradingEnabled is false.</summary>
    public interface ISubmissionGrader
    {
        void Grade(ExamSubmissionManifest submission, Action<SubmissionGradeResult> done);
    }

    public static class ExamSubmissionStore
    {
        public const string ManifestName = "manifest.json";
        /// <summary>Tests only: replaces persistentDataPath/exam_submissions.</summary>
        public static string RootOverride;
        public static string Root => RootOverride ?? Path.Combine(Application.persistentDataPath, "exam_submissions");

        public static string Safe(string name) => string.IsNullOrWhiteSpace(name) ? "exam" : Regex.Replace(name.Trim(), @"[^A-Za-z0-9_\-\.]", "_");

        public static ExamSubmissionManifest Begin(string examId, string examType, DateTime utc)
        {
            string baseFolder = Path.Combine(Root, Safe(examId), utc.ToString("yyyyMMdd_HHmmss_fff"));
            string folder = baseFolder;
            for (int i = 1; Directory.Exists(folder); i++) folder = baseFolder + "_" + i;
            return new ExamSubmissionManifest { examId = examId, examType = examType, createdUtc = utc.ToString("o"), folder = folder };
        }

        public static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        public static ExamSubmissionItem AddWriting(ExamSubmissionManifest m, string questionId, string question, string text)
        {
            Directory.CreateDirectory(m.folder);
            string file = Safe(questionId) + ".txt";
            File.WriteAllText(Path.Combine(m.folder, file), text ?? string.Empty);
            var item = new ExamSubmissionItem { questionId = questionId, kind = "writing", question = question, answerPath = file, wordCount = CountWords(text) };
            m.items.Add(item);
            return item;
        }

        public static ExamSubmissionItem AddSpeaking(ExamSubmissionManifest m, string questionId, string question, float[] samples, int frequency)
        {
            Directory.CreateDirectory(m.folder);
            var item = new ExamSubmissionItem { questionId = questionId, kind = "speaking", question = question, sampleRate = frequency };
            if (samples != null && samples.Length > 0 && frequency > 0)
            {
                string file = Safe(questionId) + ".wav";
                File.WriteAllBytes(Path.Combine(m.folder, file), SpeechAudio.EncodeWav(samples, frequency));
                item.answerPath = file;
                item.durationSeconds = samples.Length / (float)frequency;
            }
            m.items.Add(item);
            return item;
        }

        /// <summary>Speaking take already saved as a WAV file (IELTS papers): copied next to the manifest.</summary>
        public static ExamSubmissionItem AddSpeakingFile(ExamSubmissionManifest m, string questionId, string question, string wavPath, float seconds)
        {
            Directory.CreateDirectory(m.folder);
            var item = new ExamSubmissionItem { questionId = questionId, kind = "speaking", question = question, durationSeconds = seconds };
            if (!string.IsNullOrEmpty(wavPath) && File.Exists(wavPath))
            {
                string file = Safe(questionId) + ".wav";
                File.Copy(wavPath, Path.Combine(m.folder, file), true);
                item.answerPath = file;
            }
            m.items.Add(item);
            return item;
        }

        public static string Save(ExamSubmissionManifest m)
        {
            Directory.CreateDirectory(m.folder);
            string path = Path.Combine(m.folder, ManifestName);
            File.WriteAllText(path, JsonUtility.ToJson(m, true));
            return path;
        }

        public static ExamSubmissionManifest Load(string folder)
        {
            string path = Path.Combine(folder, ManifestName);
            if (!File.Exists(path)) return null;
            var m = JsonUtility.FromJson<ExamSubmissionManifest>(File.ReadAllText(path));
            if (m != null) m.folder = folder;
            return m;
        }
    }

    /// <summary>Decides whether a submission goes to a grader right away. With the flag off nothing is called.</summary>
    public static class SubmissionGrading
    {
        public static bool TryAutoGrade(ISubmissionGrader grader, ExamSubmissionManifest submission, Action<SubmissionGradeResult> done)
        {
            if (!ExamAiSettings.AiGradingEnabled || grader == null || submission == null) return false;
            grader.Grade(submission, done);
            return true;
        }
    }

    /// <summary>All texts of the pending-review flow, vi / en / ja.</summary>
    public static class ExamPendingText
    {
        public static readonly string[] Pending = { "Chờ chấm", "Pending review", "採点待ち" };
        public static readonly string[] ObjectiveOnly = { "Điểm tổng chỉ tính Đọc/Nghe", "Overall = Reading/Listening only", "総合はリーディング・リスニングのみ" };
        public static readonly string[] OverallPending = { "Tổng điểm: chờ chấm Viết/Nói", "Overall: pending Writing/Speaking review", "総合：ライティング・スピーキング採点待ち" };
        public static readonly string[] Saved = { "Bài Viết/Nói đã được lưu và đang chờ chấm. Không dùng AI, không tốn lượt, không cần mạng.",
            "Writing/Speaking answers are saved and pending review. No AI, no quota, no network used.",
            "ライティング・スピーキングの解答は保存され、採点待ちです。AI・回数・通信は使用していません。" };
        public static readonly string[] GradeButton = { "Chấm bằng AI", "Grade with AI", "AIで採点" };
        public static readonly string[] ConfirmTitle = { "Chấm bài bằng AI?", "Grade with AI?", "AIで採点しますか？" };
        public static readonly string[] ConfirmFree = { "Lượt miễn phí còn lại: {0}/{1}. Lần này miễn phí.\nSố dư: {2}",
            "Free gradings left: {0}/{1}. This one is free.\nBalance: {2}",
            "無料採点の残り：{0}/{1}。今回は無料です。\n残高：{2}" };
        public static readonly string[] ConfirmPaid = { "Đã hết lượt miễn phí. Lần chấm này tốn {0}.\nSố dư: {1}\nChỉ trừ khi chấm thành công; lỗi sẽ được hoàn lại.",
            "No free gradings left. This grading costs {0}.\nBalance: {1}\nCharged only on success; refunded on failure.",
            "無料採点は使い切りました。今回の採点は{0}です。\n残高：{1}\n成功した場合のみ支払い、失敗時は返金されます。" };
        public static readonly string[] NotEnough = { "Không đủ tiền: cần {0}, số dư {1}.", "Not enough money: needs {0}, balance {1}.", "所持金が足りません：必要 {0}、残高 {1}。" };
        public static readonly string[] Refunded = { "Chấm không thành công — đã hoàn lại lượt/tiền.", "Grading failed — your free use / money was refunded.", "採点に失敗しました。無料回数・料金は返却されました。" };
        public static readonly string[] Yes = { "Chấm", "Grade", "採点する" };

        public static IEnumerable<string[]> All()
        {
            yield return Pending; yield return ObjectiveOnly; yield return OverallPending; yield return Saved;
            yield return GradeButton; yield return ConfirmTitle; yield return ConfirmFree; yield return ConfirmPaid;
            yield return NotEnough; yield return Refunded; yield return Yes;
        }

        public static string T(string[] s) => Loc.T(s[0], s[1], s[2]);
        public static string F(string[] s, params object[] args) => Loc.F(s[0], s[1], s[2], args);
    }

    /// <summary>Pure scoring rules when some sections are pending review.</summary>
    public static class ExamPendingRules
    {
        /// <summary>IELTS estimate from objective (non-pending) sections only.</summary>
        public static float ObjectiveBand(IList<ExamSectionResult> sections)
        {
            float sum = 0f; int n = 0;
            foreach (var s in sections)
            {
                if (s == null || s.pendingReview) continue;
                sum += s.aiGraded ? s.scoredPoints / 10f : (s.maxPoints > 0 ? (float)s.scoredPoints / s.maxPoints * 9f : 0f);
                n++;
            }
            return n > 0 ? Mathf.Round(sum / n * 2f) / 2f : 0f;
        }

        public static bool AnyPending(IList<ExamSectionResult> sections)
        {
            foreach (var s in sections) if (s != null && s.pendingReview) return true;
            return false;
        }

        public static bool AnyObjective(IList<ExamSectionResult> sections)
        {
            foreach (var s in sections) if (s != null && !s.pendingReview) return true;
            return false;
        }

        /// <summary>Score cell of a section on the results screen: localized "pending" or "Band x.x".</summary>
        public static string SectionScoreLabel(ExamSectionResult s)
        {
            if (s.pendingReview) return ExamPendingText.T(ExamPendingText.Pending);
            float band = s.aiGraded ? s.scoredPoints / 10f : (s.maxPoints > 0 ? (float)s.scoredPoints / s.maxPoints * 9f : 0f);
            return $"Band {band:0.0}";
        }
    }

    /// <summary>Spendable in-game money for optional AI grading (the existing yen wallet). No real money.</summary>
    public interface IAiGradeWallet
    {
        int Balance { get; }
        bool TrySpend(int amount);
        void Refund(int amount);
    }

    /// <summary>Uses PlayerInventory when the game is running (keeps HUD and ledger in sync), else the save's yen.</summary>
    public sealed class PlayerYenWallet : IAiGradeWallet
    {
        private readonly PlayerProgressDto _progress;
        public PlayerYenWallet(PlayerProgressDto progress) { _progress = progress; }
        private static string Reason => Loc.Triple("Chấm bài AI", "AI grading", "AI採点");
        public int Balance => NihongoLife.Player.PlayerInventory.Instance != null ? NihongoLife.Player.PlayerInventory.Instance.Yen : _progress?.yen ?? 0;
        public bool TrySpend(int amount)
        {
            if (NihongoLife.Player.PlayerInventory.Instance != null) return NihongoLife.Player.PlayerInventory.Instance.SpendYen(amount, Reason);
            if (_progress == null || _progress.yen < amount) return false;
            _progress.yen -= amount;
            return true;
        }
        public void Refund(int amount)
        {
            if (amount <= 0) return;
            if (NihongoLife.Player.PlayerInventory.Instance != null) NihongoLife.Player.PlayerInventory.Instance.AddYen(amount, Reason);
            else if (_progress != null) _progress.yen += amount;
        }
    }

    public sealed class AiGradeQuote
    {
        public bool free;
        public int freeLeft;
        public int freeTotal;
        public int cost;
        public int balance;
        public bool CanProceed => free || balance >= cost;
    }

    /// <summary>A reserved grading: the free use or yen is taken up front and given back by Refund() on failure,
    /// so the net effect is "consumed only on a successful grade".</summary>
    public sealed class AiGradeTicket
    {
        internal PlayerProgressDto progress;
        internal IAiGradeWallet wallet;
        public bool usedFree;
        public int charged;
        public bool settled;
    }

    /// <summary>Free AI gradings (per player save, PlayerProgressDto.aiFreeGradesUsed) then a yen price.</summary>
    public static class AiGradeCredits
    {
        public static int FreeLeft(PlayerProgressDto progress, int freeTotal) => Mathf.Max(0, freeTotal - (progress?.aiFreeGradesUsed ?? 0));

        public static AiGradeQuote Quote(PlayerProgressDto progress, IAiGradeWallet wallet, int freeTotal, int price)
        {
            int left = FreeLeft(progress, freeTotal);
            return new AiGradeQuote { free = left > 0, freeLeft = left, freeTotal = freeTotal, cost = left > 0 ? 0 : Mathf.Max(0, price), balance = wallet?.Balance ?? 0 };
        }

        /// <summary>Null when the player has neither a free use nor enough yen.</summary>
        public static AiGradeTicket Reserve(PlayerProgressDto progress, IAiGradeWallet wallet, int freeTotal, int price)
        {
            if (progress == null) return null;
            var ticket = new AiGradeTicket { progress = progress, wallet = wallet };
            if (FreeLeft(progress, freeTotal) > 0)
            {
                progress.aiFreeGradesUsed++;
                ticket.usedFree = true;
                return ticket;
            }
            price = Mathf.Max(0, price);
            if (wallet == null || !wallet.TrySpend(price)) return null;
            ticket.charged = price;
            return ticket;
        }

        /// <summary>Grade succeeded (2xx + valid JSON): keep the charge.</summary>
        public static void Commit(AiGradeTicket ticket) { if (ticket != null) ticket.settled = true; }

        /// <summary>Grade failed: give the free use or the yen back. Safe to call twice.</summary>
        public static void Refund(AiGradeTicket ticket)
        {
            if (ticket == null || ticket.settled) return;
            ticket.settled = true;
            if (ticket.usedFree) ticket.progress.aiFreeGradesUsed = Mathf.Max(0, ticket.progress.aiFreeGradesUsed - 1);
            else ticket.wallet?.Refund(ticket.charged);
        }
    }
}
