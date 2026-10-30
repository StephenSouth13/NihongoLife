using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Core
{
    /// <summary>
    /// Links this device to a free quangdungnihongo.com Edu account (table edu_game_accounts, RPC redeem_edu_link_code),
    /// and honours a server-side reset (game_progress_reset_at). Config comes from Resources/EduAccountConfig.json
    /// ({"supabaseUrl":"...","anonKey":"..."}  the public anon key only, never a DB password). No [MenuItem], no per-frame work.
    /// UI: EduAccountLinkScreen (study desk menu -> "Tai khoan").
    /// </summary>
    public static class EduAccountLink
    {
        const string PrefCode = "NihongoLife.EduLinkCode", PrefUser = "NihongoLife.EduGameUser", PrefResetSeen = "NihongoLife.EduResetSeen";
        [Serializable] class Cfg { public string supabaseUrl; public string anonKey; }
        [Serializable] public class Account { public string game_username; public string link_code; public string status; public string game_progress_reset_at; }
        [Serializable] class RedeemBody { public string p_code; public string p_device_id; }

        public static bool IsLinked => !string.IsNullOrEmpty(PlayerPrefs.GetString(PrefCode, ""));
        public static string GameUser => PlayerPrefs.GetString(PrefUser, "");

        static Cfg Load()
        {
            var ta = Resources.Load<TextAsset>("EduAccountConfig");
            return ta == null ? null : JsonUtility.FromJson<Cfg>(ta.text);
        }

        public static IEnumerator Redeem(string code, Action<bool, string> done)
        {
            var cfg = Load();
            if (cfg == null || string.IsNullOrEmpty(cfg.supabaseUrl)) { done?.Invoke(false, "Missing EduAccountConfig"); yield break; }
            var body = JsonUtility.ToJson(new RedeemBody { p_code = (code ?? "").Trim().ToUpperInvariant(), p_device_id = SystemInfo.deviceUniqueIdentifier });
            using var req = new UnityWebRequest(cfg.supabaseUrl.TrimEnd('/') + "/rest/v1/rpc/redeem_edu_link_code", "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", cfg.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + cfg.anonKey);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { done?.Invoke(false, req.downloadHandler.text); yield break; }
            var acc = JsonUtility.FromJson<Account>(req.downloadHandler.text);
            PlayerPrefs.SetString(PrefCode, acc.link_code); PlayerPrefs.SetString(PrefUser, acc.game_username); PlayerPrefs.Save();
            ApplyServerReset(acc.game_progress_reset_at);
            done?.Invoke(true, acc.game_username);
        }

        /// <summary>If the website requested a reset newer than the last one applied here, wipe local progress once.</summary>
        public static bool ApplyServerReset(string resetAtIso)
        {
            if (string.IsNullOrEmpty(resetAtIso) || resetAtIso == PlayerPrefs.GetString(PrefResetSeen, "")) return false;
            PlayerPrefs.SetString(PrefResetSeen, resetAtIso); PlayerPrefs.Save();
            var path = PlayerSessionService.GuestSavePath; // linked device plays on the local slot
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            Debug.Log("[EduAccountLink] Server reset applied.");
            return true;
        }

        public static void Unlink() { PlayerPrefs.DeleteKey(PrefCode); PlayerPrefs.DeleteKey(PrefUser); PlayerPrefs.Save(); }
    }
}
