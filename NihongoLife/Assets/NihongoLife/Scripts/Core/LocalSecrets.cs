using System;
using System.IO;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Reads Supabase, Agora, and Gemini credentials from the environment, then from a gitignored
    /// <c>LocalSecrets/secrets.json</c> next to the Unity project (or the path in NIHONGOLIFE_SECRETS_FILE).
    /// Missing values stay empty so online and AI features fall back to offline play.
    /// Values are never written back onto <see cref="GameControlDatabase"/>, so Play Mode cannot save them into the asset.
    /// </summary>
    public static class LocalSecrets
    {
        const string SupabaseUrlKey = "NIHONGOLIFE_SUPABASE_URL";
        const string SupabaseAnonKey = "NIHONGOLIFE_SUPABASE_ANON_KEY";
        const string AgoraAppIdKey = "AGORA_APP_ID";
        const string GeminiApiKey = "NIHONGOLIFE_GEMINI_API_KEY";

        static bool _loaded;
        static SecretsFile _file;
        static bool _warned;

        /// <summary>Environment variable, then the local file, then <paramref name="assetFallback"/>.</summary>
        public static string Resolve(string environmentKey, string assetFallback)
        {
            if (!string.IsNullOrWhiteSpace(environmentKey))
            {
                string fromEnv = Environment.GetEnvironmentVariable(environmentKey);
                if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv.Trim();
                string fromFile = ReadFileValue(environmentKey);
                if (!string.IsNullOrWhiteSpace(fromFile)) return fromFile.Trim();
            }
            return string.IsNullOrWhiteSpace(assetFallback) ? string.Empty : assetFallback.Trim();
        }

        static string ReadFileValue(string environmentKey)
        {
            SecretsFile file = Load();
            if (file == null) return null;
            switch (environmentKey)
            {
                case SupabaseUrlKey: return First(file.NIHONGOLIFE_SUPABASE_URL, file.supabaseProjectUrl);
                case SupabaseAnonKey: return First(file.NIHONGOLIFE_SUPABASE_ANON_KEY, file.supabaseAnonKey);
                case AgoraAppIdKey: return First(file.AGORA_APP_ID, file.agoraAppId);
                case GeminiApiKey: return First(file.NIHONGOLIFE_GEMINI_API_KEY, file.geminiApiKey);
                default: return null;
            }
        }

        static SecretsFile Load()
        {
            if (_loaded) return _file;
            _loaded = true;
            string path = FindPath();
            if (path == null) return null;
            try
            {
                _file = JsonUtility.FromJson<SecretsFile>(File.ReadAllText(path));
                if (_file == null) return null;
                Debug.Log("[LocalSecrets] Using local secrets file. Values stay out of the repository.");
                return _file;
            }
            catch (Exception ex)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning("[LocalSecrets] Could not read the local secrets file (" + ex.GetType().Name + "). Online and AI features use environment variables only.");
                }
                _file = null;
                return null;
            }
        }

        static string FindPath()
        {
            string overridePath = Environment.GetEnvironmentVariable("NIHONGOLIFE_SECRETS_FILE");
            if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
                return overridePath;

            string dataPath = null;
            try { dataPath = Application.dataPath; }
            catch (Exception) { return null; }
            if (string.IsNullOrEmpty(dataPath)) return null;

            string besideProject = Path.GetFullPath(Path.Combine(dataPath, "..", "LocalSecrets", "secrets.json"));
            if (File.Exists(besideProject)) return besideProject;
            string besideRepo = Path.GetFullPath(Path.Combine(dataPath, "..", "..", "LocalSecrets", "secrets.json"));
            if (File.Exists(besideRepo)) return besideRepo;
            return null;
        }

        static string First(string a, string b)
        {
            if (!string.IsNullOrWhiteSpace(a)) return a;
            if (!string.IsNullOrWhiteSpace(b)) return b;
            return null;
        }

        [Serializable]
        class SecretsFile
        {
            public string NIHONGOLIFE_SUPABASE_URL;
            public string NIHONGOLIFE_SUPABASE_ANON_KEY;
            public string AGORA_APP_ID;
            public string NIHONGOLIFE_GEMINI_API_KEY;
            public string supabaseProjectUrl;
            public string supabaseAnonKey;
            public string agoraAppId;
            public string geminiApiKey;
        }
    }
}
