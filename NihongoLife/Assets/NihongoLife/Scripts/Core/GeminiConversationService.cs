using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using NihongoLife.NPC;

namespace NihongoLife.Core
{
    public class GeminiConversationService : MonoBehaviour
    {
        [Serializable]
        private class GeminiRequest
        {
            public GeminiContent[] contents;
            public GeminiGenerationConfig generationConfig;
        }

        [Serializable]
        private class GeminiContent
        {
            public string role;
            public GeminiPart[] parts;
        }

        [Serializable]
        private class GeminiPart
        {
            public string text;
        }

        [Serializable]
        private class GeminiGenerationConfig
        {
            public float temperature = 0.65f;
            public int maxOutputTokens = 160;
        }

        [Serializable]
        private class GeminiResponse
        {
            public GeminiCandidate[] candidates;
        }

        [Serializable]
        private class GeminiCandidate
        {
            public GeminiContent content;
        }

        private const string EndpointFormat = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

        public bool IsConfigured
        {
            get
            {
                if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return false;
                var database = control.Database;
                if (!database.enableGeminiConversation || !database.allowGeminiDirectClientCalls) return false;
                return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(database.geminiApiKeyEnvironmentKey));
            }
        }

        public void RequestNpcReply(NPCController npc, string playerIntent, Action<string> onComplete, Action<string> onError = null)
        {
            StartCoroutine(RequestNpcReplyRoutine(npc, playerIntent, onComplete, onError));
        }

        private IEnumerator RequestNpcReplyRoutine(NPCController npc, string playerIntent, Action<string> onComplete, Action<string> onError)
        {
            if (!GameServices.TryGet(out GameControlService control) || control.Database == null)
            {
                onError?.Invoke("GameControlService is not configured.");
                yield break;
            }

            var database = control.Database;
            string apiKey = Environment.GetEnvironmentVariable(database.geminiApiKeyEnvironmentKey);
            if (!database.enableGeminiConversation || !database.allowGeminiDirectClientCalls || string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke("Gemini is disabled or missing API key.");
                yield break;
            }

            string prompt = BuildPrompt(npc, playerIntent);
            var requestBody = new GeminiRequest
            {
                contents = new[]
                {
                    new GeminiContent
                    {
                        role = "user",
                        parts = new[] { new GeminiPart { text = prompt } }
                    }
                },
                generationConfig = new GeminiGenerationConfig()
            };

            string json = JsonUtility.ToJson(requestBody);
            string url = string.Format(EndpointFormat, UnityWebRequest.EscapeURL(database.geminiModel), UnityWebRequest.EscapeURL(apiKey));

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(request.error);
                    yield break;
                }

                string text = ExtractText(request.downloadHandler.text);
                if (string.IsNullOrWhiteSpace(text))
                {
                    onError?.Invoke("Gemini returned an empty response.");
                    yield break;
                }

                onComplete?.Invoke(text.Trim());
            }
        }

        private static string BuildPrompt(NPCController npc, string playerIntent)
        {
            string language = "Vietnamese";
            if (GameServices.TryGet(out GameSettingsService settings))
            {
                language = settings.Language == GameLanguage.English ? "English" : settings.Language == GameLanguage.Japanese ? "Japanese" : "Vietnamese";
            }

            string npcName = npc != null ? npc.DisplayName : "Town resident";
            string role = npc != null ? npc.Role : "Neighbor";

            return
                "You are an NPC in NihongoLife, a Japanese town language-learning game.\n" +
                $"NPC name: {npcName}. Role: {role}.\n" +
                $"Player intent: {playerIntent}\n" +
                $"UI language: {language}.\n" +
                "Reply as one short natural conversation turn for a beginner learner.\n" +
                "Include Japanese, romaji, and the translation. Keep it friendly and useful.";
        }

        private static string ExtractText(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            try
            {
                var response = JsonUtility.FromJson<GeminiResponse>(json);
                if (response?.candidates == null || response.candidates.Length == 0) return string.Empty;
                var parts = response.candidates[0].content?.parts;
                if (parts == null || parts.Length == 0) return string.Empty;
                return parts[0].text;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeminiConversationService] Failed to parse Gemini response: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
