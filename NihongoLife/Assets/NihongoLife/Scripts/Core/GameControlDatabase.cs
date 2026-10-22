using System;
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

    [Serializable]
    public class LocationUnlock
    {
        public string sceneName;
        /// <summary>Trí tuệ (knowledge points) the player needs.</summary>
        public int requiredKnowledge;
        /// <summary>Story quests that must be finished first.</summary>
        public string[] requiredScenarioIds = new string[0];
        /// <summary>Opens as soon as this quest has started (e.g. the room you move into during the arrival).</summary>
        public string startedByScenarioId;
        /// <summary>The story quest that takes the player there the first time (shown on the unlock card).</summary>
        public string gatewayScenarioId;
        /// <summary>Guide photo key (Resources/Guide/&lt;key&gt;.jpg) and the stage name / why it opens, in three languages.</summary>
        public string guideKey;
        public string titleVi, titleEn, titleJa;
        public string storyVi, storyEn, storyJa;
    }

    [CreateAssetMenu(fileName = "NihongoLifeControlDatabase", menuName = "NihongoLife/Control Database")]
    public class GameControlDatabase : ScriptableObject
    {
        [Header("Game Flow")]
        public string activeScenarioId = "scenario.street.first_talk";
        [Tooltip("Last quest of the guided opening; free play (jobs, farm, daily tasks, story in any order) opens after it.")]
        public string prologueEndScenarioId = "scenario.konbini.buy_onigiri";
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
            "scenario.intro.arrival",
            "scenario.street.first_talk",
            "scenario.house1.greeting",
            "scenario.school.self_intro",
            "scenario.konbini.buy_onigiri",
            "scenario.house2.lostcat",
            "scenario.house3.garbage",
            "scenario.restaurant.sushi_dining",
            "scenario.restaurant.order_ramen",
            "scenario.neighborhood.cat_followup",
            "scenario.neighborhood.recycling_morning",
            "scenario.konbini.evening_shift",
            "scenario.station.buy_ticket",
            "scenario.arcade.kim_invite",
            "scenario.island.midori_trip",
            "scenario.town.summer_festival"
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
        public string geminiModel = "gemini-flash-latest";
        public string geminiApiKeyEnvironmentKey = "NIHONGOLIFE_GEMINI_API_KEY";

        [Header("AI cost limits (IELTS Examiner / Coach)")]
        [Tooltip("Paid AI requests per day on this device; cached answers are free and do not count.")]
        public int aiDailyRequestLimit = 40;
        [Tooltip("Longest essay sent to the AI, in words (an IELTS Task 2 is ~250-350).")]
        public int aiMaxEssayWords = 700;
        [Tooltip("Longest total Speaking audio sent in one request, in seconds (a full test is ~11-14 min of which ~6-7 is the candidate).")]
        public int aiMaxSpeakingSeconds = 480;
        public int aiRequestTimeoutSeconds = 60;

        [Header("Agora (video classroom, microphone check)")]
        [Tooltip("Environment variable holding the Agora App ID. LocalSecrets/secrets.json is checked when the variable is empty.")]
        public string agoraAppIdEnvironmentKey = "AGORA_APP_ID";
        [Tooltip("Optional App ID fallback. Prefer AGORA_APP_ID or LocalSecrets/secrets.json, and leave this empty in git.")]
        public string agoraAppId = "";
        [Tooltip("Optional override for the authenticated Agora room/token API. Empty uses <Supabase URL>/functions/v1/agora-classroom.")]
        public string agoraClassroomApiUrl = "";

        [Header("Location Unlocks")]
        [Tooltip("Optional override of the map roadmap. Empty uses LocationUnlocks' defaults (Sushi 100, Station 160, Game Center 240, Island 320, each also behind a story quest). Hibari Mart, the school and the city are always open.")]
        public LocationUnlock[] locationUnlocks = new LocationUnlock[0];

        [Header("Online Database")]
        public bool enableOnlineSync = false;

        [Header("Authentication")]
        public bool enableAnonymousLogin = true;
        public bool enableEmailLogin = true;

        [Header("Online World Simulation")]
        public bool enableLocalOnlineSimulation = true;
        public int maxVisiblePlayers = 24;
        public int chatHistoryLimit = 80;

        [Tooltip("Optional fallback. Prefer NIHONGOLIFE_SUPABASE_URL or LocalSecrets/secrets.json. Leave empty in git.")]
        public string supabaseProjectUrl = "";
        [Tooltip("Optional anon-key fallback. Prefer NIHONGOLIFE_SUPABASE_ANON_KEY or LocalSecrets/secrets.json. Leave empty in git.")]
        public string supabaseAnonKey = "";
        [Tooltip("Legacy direct-Postgres host. SupabaseClient does not use it. Leave empty in git. A pooler, if you still connect that way, looks like aws-0-<region>.pooler.supabase.com and the user is postgres.<SUPABASE_PROJECT_REF>.")]
        public string supabaseHost = "";
        public int postgresPort = 5432;
        public string databaseName = "postgres";
        public string userName = "";
        public string passwordEnvironmentKey = "NIHONGOLIFE_POSTGRES_PASSWORD";
        public bool requireSsl = true;

        public string ResolveSupabaseProjectUrl() => LocalSecrets.Resolve("NIHONGOLIFE_SUPABASE_URL", supabaseProjectUrl);

        public string ResolveSupabaseAnonKey() => LocalSecrets.Resolve("NIHONGOLIFE_SUPABASE_ANON_KEY", supabaseAnonKey);
    }
}
