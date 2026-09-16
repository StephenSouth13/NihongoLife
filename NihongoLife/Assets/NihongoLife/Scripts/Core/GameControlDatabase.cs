using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Core
{
    [System.Serializable]
    public class VoiceLineEntry
    {
        public string nodeId;
        public GameLanguage language = GameLanguage.Japanese;
        public AudioClip clip;
        public string remoteUrl;
        public AudioType remoteAudioType = AudioType.MPEG;
        public string englishIpa;
    }

    [CreateAssetMenu(fileName = "NihongoLifeControlDatabase", menuName = "NihongoLife/Control Database")]
    public class GameControlDatabase : ScriptableObject
    {
        [Header("Game Flow")]
        public string activeScenarioId = "scenario.street.first_talk";
        public GameLanguage defaultLanguage = GameLanguage.Vietnamese;

        [Header("Menu")]
        public string menuTitle = "NIHONGO LIFE";
        public string menuSubtitleVi = "Game học ngoại ngữ qua một khu phố Nhật có tương tác";
        public string menuSubtitleEn = "Learn languages inside a living Japanese town";
        public string menuSubtitleJa = "日本の街で外国語を学ぶゲーム";

        [Header("Scenario Library")]
        public List<ScenarioDefinition> scenarios = new List<ScenarioDefinition>();
        public List<string> campaignScenarioIds = new List<string>
        {
            "scenario.street.first_talk",
            "scenario.konbini.buy_onigiri",
            "scenario.house1.greeting",
            "scenario.house2.lostcat",
            "scenario.house3.garbage"
        };
        public bool continueCampaignInGameplayScene = true;

        [Header("Voice / Pronunciation")]
        public List<VoiceLineEntry> voiceLines = new List<VoiceLineEntry>();
        public bool useProceduralVoiceWhenMissingClip = false;
        public bool enableTownAmbientAudio = true;
        public AudioClip townBgmClip;
        public AudioClip[] streetVoiceClips;

        [Header("Gemini Conversation")]
        public bool enableGeminiConversation = true;
        public bool allowGeminiDirectClientCalls = true;
        public string geminiModel = "gemini-2.5-flash";
        public string geminiApiKeyEnvironmentKey = "NIHONGOLIFE_GEMINI_API_KEY";

        [Header("Online Database")]
        public bool enableOnlineSync = false;

        [Header("Authentication")]
        public bool enableAnonymousLogin = true;
        public bool enableEmailLogin = true;

        [Header("Online World Simulation")]
        public bool enableLocalOnlineSimulation = true;
        public int maxVisiblePlayers = 24;
        public int chatHistoryLimit = 80;

        public string supabaseProjectUrl = "";
        public string supabaseAnonKey = "";
        public string supabaseHost = "aws-0-ap-northeast-2.pooler.supabase.com";
        public int postgresPort = 5432;
        public string databaseName = "postgres";
        public string userName = "";
        public string passwordEnvironmentKey = "NIHONGOLIFE_POSTGRES_PASSWORD";
        public bool requireSsl = true;
    }
}
