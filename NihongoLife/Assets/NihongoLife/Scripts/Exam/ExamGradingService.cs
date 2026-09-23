using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using NihongoLife.Core;

namespace NihongoLife.Exam
{
    [Serializable]
    public class ExamAiGrade
    {
        /// <summary>0-9 in 0.5 steps for IELTS. For JLPT essay-style free-response practice this is
        /// re-mapped to a 0-100 confidence score by the caller; see ExamManager.</summary>
        public float band;
        public string feedback;
        /// <summary>Speaking only: what Gemini heard, so the learner can see what was actually said.</summary>
        public string transcript;
    }

    /// <summary>
    /// AI grading for the two question types that cannot be scored by simple string/index matching:
    /// Essay (IELTS Writing) and SpeakingPrompt (IELTS Speaking, JLPT has no speaking section).
    ///
    /// Honesty about what this can and cannot do:
    /// - Essay grading reads the actual text, so lexical resource, grammar patterns and task response can
    ///   be judged reasonably.
    /// - Speaking grading only has the transcript (What Gemini hears in the recording), not the audio's
    ///   prosody. It can judge coherence, vocabulary range and grammatical range from what was said, but it
    ///   cannot judge pronunciation, intonation or fluency (pauses, hesitation) the way a real IELTS
    ///   examiner does. The band returned for Speaking is explicitly a "content-only" estimate — see the
    ///   disclaimer text surfaced by ExamPlayUI and Docs/EXAM_SYSTEM.md.
    /// - When Gemini is not configured (no API key / disabled in GameControlDatabase), both methods fall
    ///   back to a transparent, deterministic rubric (word count / keyword coverage) so the exam still
    ///   produces a result instead of hanging — the fallback feedback says plainly that it is not AI-graded.
    /// </summary>
    public class ExamGradingService : MonoBehaviour, IGameService
    {
        [Serializable] private class InlineData { public string mimeType; public string data; }
        [Serializable] private class GeminiPart { public string text; public InlineData inlineData; }
        [Serializable] private class GeminiContent { public GeminiPart[] parts; }
        [Serializable] private class GeminiRequest { public GeminiContent[] contents; }
        [Serializable] private class GeminiCandidate { public GeminiContent content; }
        [Serializable] private class GeminiResponse { public GeminiCandidate[] candidates; }

        public void Initialize() { }

        public bool IsConfigured
        {
            get
            {
                if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return false;
                var db = control.Database;
                if (!db.enableGeminiConversation || !db.allowGeminiDirectClientCalls) return false;
                return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(db.geminiApiKeyEnvironmentKey));
            }
        }

        // ──────────────────────── Writing (Essay) ────────────────────────

        public void GradeEssay(ExamQuestion question, ExamDefinition exam, string essayText, Action<ExamAiGrade> onComplete)
        {
            StartCoroutine(GradeEssayRoutine(question, exam, essayText ?? string.Empty, onComplete));
        }

        private IEnumerator GradeEssayRoutine(ExamQuestion question, ExamDefinition exam, string essayText, Action<ExamAiGrade> onComplete)
        {
            if (!TryGetConfig(out string apiKey, out string model))
            {
                onComplete?.Invoke(FallbackEssayGrade(question, essayText));
                yield break;
            }

            bool isIelts = exam != null && exam.examType == ExamType.Ielts;
            string task = string.IsNullOrWhiteSpace(question.taskInstructionsEn) ? question.promptEn : question.taskInstructionsEn;
            string prompt = isIelts
                ? "You are an IELTS Writing examiner. Score the essay below against the four official IELTS Writing criteria " +
                  "(Task Response, Coherence and Cohesion, Lexical Resource, Grammatical Range and Accuracy) and return one overall band 0-9 in 0.5 steps.\n" +
                  $"Task:\n{task}\n\nEssay ({CountWords(essayText)} words):\n{essayText}\n\n" +
                  "Return JSON only: {\"band\": <number>, \"feedback\": \"<3-5 sentences of specific, actionable feedback, in Vietnamese>\"}."
                : "You are a Japanese-language writing tutor grading an N5-level learner's short written answer.\n" +
                  $"Task:\n{task}\n\nLearner's answer:\n{essayText}\n\n" +
                  "Score how well it completes the task on a 0-9 scale (9 = excellent for this level). " +
                  "Return JSON only: {\"band\": <number>, \"feedback\": \"<3-5 sentences of specific, actionable feedback, in Vietnamese>\"}.";

            yield return SendTextPrompt(prompt, apiKey, model, grade =>
            {
                onComplete?.Invoke(grade ?? FallbackEssayGrade(question, essayText));
            });
        }

        private static ExamAiGrade FallbackEssayGrade(ExamQuestion question, string essayText)
        {
            int words = CountWords(essayText);
            int minWords = Mathf.Max(1, question.minWords);
            float ratio = Mathf.Clamp01((float)words / minWords);
            float band = Mathf.Clamp(Mathf.Round((3f + ratio * 4f) * 2f) / 2f, 0f, 9f);
            return new ExamAiGrade
            {
                band = band,
                feedback = $"Chấm tự động không dùng AI (chưa cấu hình Gemini): chỉ dựa trên độ dài bài viết ({words}/{minWords} từ). " +
                           "Đây KHÔNG phải điểm đánh giá chất lượng thật — hãy nhờ giáo viên hoặc bật Gemini để có nhận xét thật.",
                transcript = string.Empty
            };
        }

        // ──────────────────────── Speaking ────────────────────────

        public void GradeSpeaking(ExamQuestion question, ExamDefinition exam, float[] samples, int frequency, Action<ExamAiGrade> onComplete)
        {
            StartCoroutine(GradeSpeakingRoutine(question, exam, samples, frequency, onComplete));
        }

        private IEnumerator GradeSpeakingRoutine(ExamQuestion question, ExamDefinition exam, float[] samples, int frequency, Action<ExamAiGrade> onComplete)
        {
            if (samples == null || samples.Length == 0)
            {
                onComplete?.Invoke(new ExamAiGrade { band = 0f, feedback = "Không có bản ghi âm nào để chấm.", transcript = string.Empty });
                yield break;
            }

            if (!TryGetConfig(out string apiKey, out string model))
            {
                onComplete?.Invoke(new ExamAiGrade
                {
                    band = 0f,
                    transcript = string.Empty,
                    feedback = "Chưa cấu hình Gemini nên không thể chuyển giọng nói thành văn bản để chấm. Phần Speaking chỉ ghi âm để bạn tự nghe lại — hãy nhờ giáo viên nhận xét phát âm."
                });
                yield break;
            }

            byte[] wav = EncodeWav(samples, frequency);
            bool isIelts = exam != null && exam.examType == ExamType.Ielts;
            string task = string.IsNullOrWhiteSpace(question.taskInstructionsEn) ? question.promptEn : question.taskInstructionsEn;
            string prompt = isIelts
                ? "First, transcribe the spoken English in the attached audio exactly as spoken (keep hesitations like 'um' out, just the words).\n" +
                  $"Speaking task the candidate was answering:\n{task}\n\n" +
                  "Then, judging ONLY the content of the transcript (coherence, vocabulary range, grammatical range) — you cannot hear pronunciation or fluency from a transcript, so do not claim to assess them — estimate an IELTS Speaking content band 0-9 in 0.5 steps.\n" +
                  "Return JSON only: {\"transcript\": \"<text>\", \"band\": <number>, \"feedback\": \"<3-5 sentences, in Vietnamese, noting this band reflects content only, not pronunciation/fluency>\"}."
                : "Transcribe the spoken Japanese in the attached audio.\n" +
                  $"Task the learner was answering:\n{task}\n\n" +
                  "Judging only the content, score 0-9 (9 = excellent for an N5 learner). " +
                  "Return JSON only: {\"transcript\": \"<text>\", \"band\": <number>, \"feedback\": \"<3-5 sentences, in Vietnamese>\"}.";

            var request = new GeminiRequest
            {
                contents = new[]
                {
                    new GeminiContent
                    {
                        parts = new[]
                        {
                            new GeminiPart { text = prompt },
                            new GeminiPart { inlineData = new InlineData { mimeType = "audio/wav", data = Convert.ToBase64String(wav) } }
                        }
                    }
                }
            };

            yield return SendRequest(request, apiKey, model, grade =>
            {
                onComplete?.Invoke(grade ?? new ExamAiGrade { band = 0f, feedback = "Gemini không phản hồi được. Hãy thử lại.", transcript = string.Empty });
            });
        }

        // ──────────────────────── Shared HTTP plumbing ────────────────────────

        private static bool TryGetConfig(out string apiKey, out string model)
        {
            apiKey = string.Empty;
            model = "gemini-2.5-flash";
            if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return false;
            var db = control.Database;
            if (!db.enableGeminiConversation || !db.allowGeminiDirectClientCalls) return false;
            apiKey = Environment.GetEnvironmentVariable(db.geminiApiKeyEnvironmentKey);
            model = string.IsNullOrWhiteSpace(db.geminiModel) ? model : db.geminiModel;
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        private IEnumerator SendTextPrompt(string prompt, string apiKey, string model, Action<ExamAiGrade> onComplete)
        {
            var request = new GeminiRequest
            {
                contents = new[] { new GeminiContent { parts = new[] { new GeminiPart { text = prompt } } } }
            };
            yield return SendRequest(request, apiKey, model, onComplete);
        }

        private IEnumerator SendRequest(GeminiRequest requestBody, string apiKey, string model, Action<ExamAiGrade> onComplete)
        {
            string json = JsonUtility.ToJson(requestBody);
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{UnityWebRequest.EscapeURL(model)}:generateContent";

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] payload = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = 45;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ExamGradingService] Gemini call failed: {request.responseCode} {request.error}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                onComplete?.Invoke(ParseGrade(request.downloadHandler.text));
            }
        }

        private static ExamAiGrade ParseGrade(string responseJson)
        {
            try
            {
                var response = JsonUtility.FromJson<GeminiResponse>(responseJson);
                string text = response?.candidates != null && response.candidates.Length > 0
                    ? response.candidates[0].content?.parts?[0].text
                    : null;
                if (string.IsNullOrWhiteSpace(text)) return null;

                string jsonPart = ExtractJsonObject(text.Trim());
                if (string.IsNullOrWhiteSpace(jsonPart)) return null;

                var grade = JsonUtility.FromJson<ExamAiGrade>(jsonPart);
                if (grade == null) return null;
                grade.band = Mathf.Clamp(grade.band, 0f, 9f);
                return grade;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExamGradingService] Failed to parse grading response: {ex.Message}");
                return null;
            }
        }

        private static string ExtractJsonObject(string text)
        {
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                int firstLineEnd = text.IndexOf('\n');
                int fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLineEnd >= 0 && fenceEnd > firstLineEnd) text = text.Substring(firstLineEnd + 1, fenceEnd - firstLineEnd - 1).Trim();
            }

            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            return start < 0 || end <= start ? string.Empty : text.Substring(start, end - start + 1);
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>Standard 16-bit PCM mono WAV, matching what Gemini's audio input expects.</summary>
        private static byte[] EncodeWav(float[] samples, int frequency)
        {
            int sampleCount = samples.Length;
            int byteCount = sampleCount * 2;
            using (var stream = new System.IO.MemoryStream(44 + byteCount))
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + byteCount);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(frequency);
                writer.Write(frequency * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(byteCount);
                for (int i = 0; i < sampleCount; i++)
                {
                    writer.Write((short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue));
                }

                return stream.ToArray();
            }
        }
    }
}
