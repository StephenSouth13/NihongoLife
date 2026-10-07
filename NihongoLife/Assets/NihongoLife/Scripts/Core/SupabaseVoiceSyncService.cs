using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Core
{
    public class SupabaseVoiceSyncService : MonoBehaviour
    {
        [SerializeField] private bool testConnectionOnStart = true;
        private static bool _testedThisSession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => _testedThisSession = false;

        private void Start()
        {
            // The city reloads often (zones, menu); probing the REST API once per session is enough.
            if (testConnectionOnStart && !_testedThisSession)
            {
                _testedThisSession = true;
                StartCoroutine(TestConnection());
            }
        }

        private IEnumerator TestConnection()
        {
            if (!GameServices.TryGet(out GameControlService control)
                || control.Database == null
                || !control.Database.enableOnlineSync)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(control.Database.supabaseProjectUrl)
                || string.IsNullOrWhiteSpace(control.Database.supabaseAnonKey))
            {
                Debug.LogWarning("[SupabaseVoiceSync] Online sync is enabled but Supabase project URL or anon key is empty.");
                yield break;
            }

            string url = control.Database.supabaseProjectUrl.TrimEnd('/') + "/rest/v1/voice_lines?select=id&limit=1";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", control.Database.supabaseAnonKey);
                request.SetRequestHeader("Authorization", "Bearer " + control.Database.supabaseAnonKey);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[SupabaseVoiceSync] Supabase REST connection OK. Voice remoteUrl lines can be managed from database/storage.");
                }
                else if (request.responseCode == 404)
                {
                    // Reachable project, but the optional voice_lines table is not provisioned: voices stay local.
                    Debug.Log("[SupabaseVoiceSync] voice_lines table not provisioned yet — voice lines use the bundled clips.");
                }
                else
                {
                    Debug.LogWarning($"[SupabaseVoiceSync] Supabase REST test failed: {request.responseCode} {request.error}");
                }
            }
        }
    }
}
