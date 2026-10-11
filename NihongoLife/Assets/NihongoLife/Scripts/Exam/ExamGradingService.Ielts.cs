using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Exam
{
    /// <summary>
    /// IELTS Writing and Speaking with Gemini, on top of the same configuration as the older single-band grading:
    /// · Examiner — the four official criteria with a band and an explanation each; the overall band is computed
    ///   locally with the IELTS rounding rules (IeltsBands), never taken from the model. It only ever sees the
    ///   learner's own, unaided text or audio.
    /// · Coach — sentence-level problems with an explanation and a suggested fix; it also reviews rewrites, which
    ///   are practice only and never produce a band.
    /// · Speaking — the audio itself goes to the model (one request for the whole paper): transcripts per answer,
    ///   FC/LR/GRA, and Pronunciation judged only from what is heard (marked "not assessed" when the audio is not
    ///   usable). Speech timing measured locally (SpeechAudio.Analyze) is passed in as evidence for Fluency.
    /// Cost and failure handling: answers are cached by content (a repeat costs nothing), paid requests are capped
    /// per day (GameControlDatabase.aiDailyRequestLimit), input size is capped, offline is detected before calling,
    /// and a busy/rate-limited service is retried once. Every failure comes back as a readable error, never a hang.
    /// </summary>
    public partial class ExamGradingService
    {
        /// <summary>Tests: point requests at another endpoint (e.g. an unreachable one to exercise the offline path).</summary>
        public static string EndpointOverride;
        public static int PaidRequestsThisSession { get; private set; }

        // ─────────── Writing ───────────

        public void ExamineWriting(IeltsWritingTaskRecord task, Action<IeltsAiExamination> done) =>
            StartCoroutine(ExamineWritingRoutine(task, done));

        private IEnumerator ExamineWritingRoutine(IeltsWritingTaskRecord task, Action<IeltsAiExamination> done)
        {
            var result = new IeltsAiExamination { createdUtc = DateTime.UtcNow.ToString("o") };
            int words = SpeechAudio.CountWords(task.original);
            if (words < 20) { result.error = "Bài viết quá ngắn (dưới 20 từ) để chấm theo tiêu chí IELTS."; done?.Invoke(result); yield break; }
            if (words > Limits.maxEssayWords) { result.error = $"Bài viết dài {words} từ, vượt giới hạn {Limits.maxEssayWords} từ cho một lần chấm."; done?.Invoke(result); yield break; }

            string first = task.task == 1 ? "TA" : "TR";
            string firstName = task.task == 1 ? "Task Achievement" : "Task Response";
            string prompt =
                "You are a certified IELTS Writing examiner. Assess ONLY the candidate's own text below against the public IELTS Writing band descriptors.\n" +
                $"Writing Task {task.task} (minimum {task.minWords} words). Task:\n{task.prompt}\n\n" +
                $"Candidate's text ({words} words):\n<<<\n{task.original}\n>>>\n\n" +
                $"Give a band 0-9 in 0.5 steps for each criterion: {first} ({firstName}), CC (Coherence and Cohesion), LR (Lexical Resource), GRA (Grammatical Range and Accuracy). " +
                (words < task.minWords ? $"The text is under {task.minWords} words: apply the under-length penalty to {first}. " : "") +
                "For each criterion explain in Vietnamese (2-4 sentences) why it gets that band, quoting short phrases from the text as evidence. " +
                "Do not invent content that is not in the text. Do not give an overall band.\n" +
                "Reply with JSON only: {\"criteria\":[{\"code\":\"" + first + "\",\"name\":\"" + firstName + "\",\"band\":6.0,\"explanation\":\"...\"},{\"code\":\"CC\",...},{\"code\":\"LR\",...},{\"code\":\"GRA\",...}],\"summary\":\"<2-3 sentences in Vietnamese: main strength and the most important thing to improve>\"}";

            string text = null, error = null; bool cached = false;
            yield return RequestJson("writing-examiner", prompt, null, 1600, (t, e, c) => { text = t; error = e; cached = c; });
            result.fromCache = cached;
            result.model = Model;
            if (error != null) { result.error = error; done?.Invoke(result); yield break; }
            var parsed = Parse<ExaminerReply>(text);
            if (parsed?.criteria == null || parsed.criteria.Count < 4) { result.error = "AI trả về kết quả không đúng định dạng. Hãy thử chấm lại."; done?.Invoke(result); yield break; }
            foreach (var c in parsed.criteria) { c.band = IeltsBands.Clamp(c.band); c.assessed = true; }
            result.criteria = parsed.criteria;
            result.summary = parsed.summary;
            result.overall = IeltsBands.WritingTask(result.criteria);
            done?.Invoke(result);
        }

        /// <summary>Sentence-level coaching on the original (or, with <paramref name="original"/>, on a rewrite of it).</summary>
        public void CoachWriting(IeltsWritingTaskRecord task, string text, string original, Action<IeltsCoachReport> done) =>
            StartCoroutine(CoachWritingRoutine(task, text, original, done));

        private IEnumerator CoachWritingRoutine(IeltsWritingTaskRecord task, string text, string original, Action<IeltsCoachReport> done)
        {
            var report = new IeltsCoachReport { createdUtc = DateTime.UtcNow.ToString("o") };
            int words = SpeechAudio.CountWords(text);
            if (words < 5) { report.error = "Chưa có nội dung để góp ý."; done?.Invoke(report); yield break; }
            if (words > Limits.maxEssayWords) { report.error = $"Bài dài {words} từ, vượt giới hạn {Limits.maxEssayWords} từ."; done?.Invoke(report); yield break; }
            var sentences = SplitSentences(text);
            var numbered = new StringBuilder();
            for (int i = 0; i < sentences.Count; i++) numbered.Append('[').Append(i + 1).Append("] ").AppendLine(sentences[i]);
            string prompt =
                "You are a supportive IELTS writing coach for a Vietnamese learner. Find the concrete problems in the sentences below " +
                "(grammar, vocabulary/word choice, spelling, punctuation, cohesion, and task relevance). Report at most 12 issues, most important first; skip sentences with no real problem.\n" +
                $"Writing Task {task.task}. Task:\n{task.prompt}\n\n" +
                (original != null ? $"This is the learner's REWRITE after earlier feedback. Earlier version for reference:\n<<<\n{original}\n>>>\nAlso say in overallAdvice what improved and what still needs work.\n\n" : "") +
                "Numbered sentences:\n" + numbered + "\n" +
                "For each issue: sentence = the [number]; original = the exact words with the problem; category = grammar|vocabulary|spelling|punctuation|cohesion|task; " +
                "problem = short label; explanation = why it is wrong or weak, in Vietnamese; suggestion = the corrected wording (English). Do not give any band score.\n" +
                "Reply with JSON only: {\"issues\":[{\"sentence\":1,\"original\":\"...\",\"category\":\"grammar\",\"problem\":\"...\",\"explanation\":\"...\",\"suggestion\":\"...\"}],\"overallAdvice\":\"<2-4 sentences in Vietnamese>\"}";

            string reply = null, error = null; bool cached = false;
            yield return RequestJson("writing-coach", prompt, null, 3000, (t, e, c) => { reply = t; error = e; cached = c; });
            report.fromCache = cached;
            if (error != null) { report.error = error; done?.Invoke(report); yield break; }
            var parsed = Parse<CoachReply>(reply);
            if (parsed == null) { report.error = "AI trả về góp ý không đúng định dạng. Hãy thử lại."; done?.Invoke(report); yield break; }
            report.issues = (parsed.issues ?? new List<IeltsCoachIssue>()).Where(i => !string.IsNullOrWhiteSpace(i.original)).ToList();
            report.overallAdvice = parsed.overallAdvice;
            done?.Invoke(report);
        }

        // ─────────── Speaking ───────────

        public sealed class SpeakingTake
        {
            public IeltsSpeakingAnswer Answer;
            public float[] Samples;
            public int Frequency;
        }

        /// <summary>Transcribes every answer and assesses the paper on FC, LR, GRA and (from the audio) P.</summary>
        public void ExamineSpeaking(IList<SpeakingTake> takes, Action<IeltsAiExamination> done) =>
            StartCoroutine(ExamineSpeakingRoutine(takes, done));

        private IEnumerator ExamineSpeakingRoutine(IList<SpeakingTake> takes, Action<IeltsAiExamination> done)
        {
            var result = new IeltsAiExamination { createdUtc = DateTime.UtcNow.ToString("o"), model = Model };
            var usable = takes.Where(t => t.Samples != null && t.Samples.Length > 0).ToList();
            if (usable.Count == 0) { result.error = "Chưa có câu trả lời nào được ghi âm."; done?.Invoke(result); yield break; }
            float total = usable.Sum(t => t.Samples.Length / (float)t.Frequency);
            if (total > Limits.maxSpeakingSeconds) { result.error = $"Tổng thời lượng ghi âm {total:0}s vượt giới hạn {Limits.maxSpeakingSeconds}s cho một lần chấm."; done?.Invoke(result); yield break; }

            var audio = new List<(string label, byte[] wav)>();
            var intro = new StringBuilder();
            foreach (var take in usable)
            {
                var a = take.Answer;
                var m = a.metrics ?? new IeltsSpeakingMetrics();
                intro.AppendLine($"Answer {a.number} (Part {a.part}) — question: \"{a.question}\". Measured on device: {m.seconds:0.0}s recorded, {m.speechSeconds:0.0}s of voice, {m.pauses} pauses over 0.5s (longest {m.longestPause:0.0}s), mean run {m.meanRunSeconds:0.0}s.");
                audio.Add(($"Audio of answer {a.number}:", SpeechAudio.EncodeWav(SpeechAudio.Resample(take.Samples, take.Frequency, MicRecorder.SampleRate), MicRecorder.SampleRate)));
            }
            string prompt =
                "You are a certified IELTS Speaking examiner. The candidate's recorded answers are attached, in order, each preceded by its label.\n" + intro +
                "\n1) Transcribe each answer exactly as spoken (keep false starts; write [inaudible] where you cannot hear).\n" +
                "2) Give a band 0-9 in 0.5 steps for FC (Fluency and Coherence), LR (Lexical Resource), GRA (Grammatical Range and Accuracy), using the transcripts and the measured timing as evidence.\n" +
                "3) Give P (Pronunciation) ONLY from what you actually hear in the audio (intelligibility, individual sounds, word and sentence stress, intonation, chunking). " +
                "Never infer pronunciation from the transcript. If the audio is too short, silent, synthetic-sounding or noisy to judge, set \"assessed\": false for P and say why.\n" +
                "Explain each criterion in Vietnamese (2-4 sentences) with short quotes as evidence. Do not give an overall band.\n" +
                "Reply with JSON only: {\"answers\":[{\"number\":1,\"transcript\":\"...\"}],\"criteria\":[{\"code\":\"FC\",\"name\":\"Fluency and Coherence\",\"band\":6.0,\"explanation\":\"...\",\"assessed\":true},{\"code\":\"LR\",...},{\"code\":\"GRA\",...},{\"code\":\"P\",\"name\":\"Pronunciation\",...}],\"summary\":\"<2-3 sentences in Vietnamese>\"}";

            string reply = null, error = null; bool cached = false;
            yield return RequestJson("speaking-examiner", prompt, audio, 3000, (t, e, c) => { reply = t; error = e; cached = c; });
            result.fromCache = cached;
            if (error != null) { result.error = error; done?.Invoke(result); yield break; }
            var parsed = Parse<SpeakingReply>(reply);
            if (parsed?.criteria == null || parsed.criteria.Count < 3) { result.error = "AI trả về kết quả Speaking không đúng định dạng. Hãy thử chấm lại."; done?.Invoke(result); yield break; }
            foreach (var c in parsed.criteria)
            {
                c.band = IeltsBands.Clamp(c.band);
                if (c.code != "P") c.assessed = true; // only Pronunciation can be "not assessed"
            }
            var p = parsed.criteria.FirstOrDefault(c => c.code == "P");
            if (p == null) parsed.criteria.Add(new IeltsCriterionScore { code = "P", name = "Pronunciation", assessed = false, explanation = "AI không đánh giá được phát âm từ bản ghi này." });
            result.criteria = parsed.criteria;
            result.summary = parsed.summary;
            result.overall = IeltsBands.Speaking(result.criteria);
            foreach (var transcript in parsed.answers ?? new List<TranscriptReply>())
            {
                var take = usable.FirstOrDefault(t => t.Answer.number == transcript.number);
                if (take == null) continue;
                take.Answer.transcript = transcript.transcript;
                if (take.Answer.metrics != null && take.Answer.metrics.speechSeconds > 1f)
                    take.Answer.metrics.wordsPerMinute = SpeechAudio.CountWords(transcript.transcript) / take.Answer.metrics.speechSeconds * 60f;
            }
            done?.Invoke(result);
        }

        // ─────────── Requests, cache and limits ───────────

        private struct LimitSet { public int daily, maxEssayWords, maxSpeakingSeconds, timeout; }

        private static LimitSet Limits
        {
            get
            {
                var l = new LimitSet { daily = 40, maxEssayWords = 700, maxSpeakingSeconds = 480, timeout = 60 };
                if (GameServices.TryGet(out GameControlService control) && control.Database != null)
                {
                    var db = control.Database;
                    l.daily = Mathf.Max(0, db.aiDailyRequestLimit);
                    l.maxEssayWords = Mathf.Max(50, db.aiMaxEssayWords);
                    l.maxSpeakingSeconds = Mathf.Max(30, db.aiMaxSpeakingSeconds);
                    l.timeout = Mathf.Clamp(db.aiRequestTimeoutSeconds, 10, 180);
                }
                return l;
            }
        }

        private static string Model => TryGetConfig(out _, out string model) ? model : "gemini-flash-latest";

        private static string CounterKey => "NL.AiRequests." + DateTime.Now.ToString("yyyyMMdd");
        public static int RequestsToday => PlayerPrefs.GetInt(CounterKey, 0);
        public static int RequestsLeftToday => Mathf.Max(0, Limits.daily - RequestsToday);
        private static string CacheFolder => Path.Combine(Application.persistentDataPath, "ai_cache");

        /// <summary>Sends one JSON-mode request (text prompt plus optional WAV parts). done(text, error, fromCache).</summary>
        private IEnumerator RequestJson(string kind, string prompt, List<(string label, byte[] wav)> audio, int maxTokens, Action<string, string, bool> done)
        {
            string cacheKey = Hash(kind + "|" + Model + "|" + prompt + "|" + string.Join("|", (audio ?? new List<(string, byte[])>()).Select(a => Hash(a.wav))));
            string cachePath = Path.Combine(CacheFolder, cacheKey + ".txt");
            if (File.Exists(cachePath)) { done(File.ReadAllText(cachePath), null, true); yield break; }

            if (!TryGetConfig(out string apiKey, out string model))
            {
                done(null, "Chưa cấu hình AI (Gemini): cần biến môi trường với API key và bật Gemini trong Control Database. Bài làm vẫn được lưu để chấm sau.", false);
                yield break;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable && string.IsNullOrEmpty(EndpointOverride))
            {
                done(null, "Không có kết nối mạng. Bài làm đã được lưu — hãy bấm \"Chấm lại\" khi có mạng.", false);
                yield break;
            }
            var limits = Limits;
            if (RequestsToday >= limits.daily)
            {
                done(null, $"Đã dùng hết {limits.daily} lượt chấm AI hôm nay (giới hạn chi phí). Bài làm đã được lưu — chấm tiếp vào ngày mai.", false);
                yield break;
            }

            string body = BuildBody(prompt, audio, maxTokens);
            string url = !string.IsNullOrEmpty(EndpointOverride)
                ? EndpointOverride
                : $"https://generativelanguage.googleapis.com/v1beta/models/{UnityWebRequest.EscapeURL(model)}:generateContent";
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = limits.timeout;
                yield return request.SendWebRequest();

                // A request that reached the service counts against the daily limit, answered or not.
                if (request.result != UnityWebRequest.Result.ConnectionError)
                {
                    PlayerPrefs.SetInt(CounterKey, RequestsToday + 1);
                    PlayerPrefs.Save();
                    PaidRequestsThisSession++;
                }
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string text = ExtractText(request.downloadHandler.text);
                    if (string.IsNullOrWhiteSpace(text)) { done(null, "AI không trả về nội dung (có thể bị bộ lọc an toàn chặn). Hãy thử lại.", false); yield break; }
                    Directory.CreateDirectory(CacheFolder);
                    File.WriteAllText(cachePath, text);
                    done(text, null, false);
                    yield break;
                }
                long code = request.responseCode;
                bool retry = attempt == 0 && (code == 429 || code == 500 || code == 503 || request.result == UnityWebRequest.Result.ConnectionError && code == 0 && request.error != null && request.error.Contains("timed out"));
                Debug.LogWarning($"[ExamGradingService] {kind}: {code} {request.error}");
                if (retry) { yield return new WaitForSecondsRealtime(2.5f); continue; }
                done(null, code switch
                {
                    0 => "Không kết nối được tới dịch vụ AI (mất mạng hoặc hết thời gian chờ). Bài làm đã được lưu — hãy chấm lại sau.",
                    400 => "Dịch vụ AI từ chối yêu cầu (dữ liệu quá lớn hoặc model không hỗ trợ). Hãy rút ngắn bài/ghi âm rồi thử lại.",
                    401 or 403 => "API key AI không hợp lệ hoặc chưa được cấp quyền.",
                    429 => "Dịch vụ AI đang giới hạn tần suất (hoặc hết hạn mức). Hãy thử lại sau ít phút.",
                    _ => $"Dịch vụ AI lỗi ({code}). Hãy thử lại sau."
                }, false);
                yield break;
            }
        }

        private static string BuildBody(string prompt, List<(string label, byte[] wav)> audio, int maxTokens)
        {
            var sb = new StringBuilder();
            sb.Append("{\"contents\":[{\"role\":\"user\",\"parts\":[");
            sb.Append("{\"text\":").Append(Json(prompt)).Append('}');
            if (audio != null)
                foreach (var (label, wav) in audio)
                {
                    sb.Append(",{\"text\":").Append(Json(label)).Append('}');
                    sb.Append(",{\"inlineData\":{\"mimeType\":\"audio/wav\",\"data\":\"").Append(Convert.ToBase64String(wav)).Append("\"}}");
                }
            sb.Append("]}],\"generationConfig\":{\"responseMimeType\":\"application/json\",\"temperature\":0.2,\"maxOutputTokens\":").Append(maxTokens).Append("}}");
            return sb.ToString();
        }

        private static string Json(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        [Serializable] private class ReplyPart { public string text; }
        [Serializable] private class ReplyContent { public ReplyPart[] parts; }
        [Serializable] private class ReplyCandidate { public ReplyContent content; }
        [Serializable] private class Reply { public ReplyCandidate[] candidates; }
        [Serializable] private class ExaminerReply { public List<IeltsCriterionScore> criteria; public string summary; }
        [Serializable] private class CoachReply { public List<IeltsCoachIssue> issues; public string overallAdvice; }
        [Serializable] private class TranscriptReply { public int number; public string transcript; }
        [Serializable] private class SpeakingReply { public List<TranscriptReply> answers; public List<IeltsCriterionScore> criteria; public string summary; }

        private static string ExtractText(string responseJson)
        {
            try
            {
                var reply = JsonUtility.FromJson<Reply>(responseJson);
                var parts = reply?.candidates != null && reply.candidates.Length > 0 ? reply.candidates[0].content?.parts : null;
                return parts == null ? null : string.Concat(parts.Select(p => p.text));
            }
            catch (Exception e) { Debug.LogWarning("[ExamGradingService] Unreadable reply: " + e.Message); return null; }
        }

        private static T Parse<T>(string text) where T : class
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string json = ExtractJsonObject(text.Trim());
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (Exception e) { Debug.LogWarning($"[ExamGradingService] Bad JSON for {typeof(T).Name}: {e.Message}"); return null; }
        }

        private static string Hash(string text) => Hash(Encoding.UTF8.GetBytes(text ?? ""));

        private static string Hash(byte[] data)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(data ?? Array.Empty<byte>()).Take(16).Select(b => b.ToString("x2")));
        }

        /// <summary>Sentences as the Coach numbers them (and the results screen highlights them).</summary>
        public static List<string> SplitSentences(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"[^.!?\n]+(?:[.!?]+[""”’)]*|$)"))
            {
                string s = m.Value.Trim();
                if (s.Length > 0) list.Add(s);
            }
            return list;
        }
    }
}
