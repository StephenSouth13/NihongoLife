using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Island
{
    /// <summary>What a friend sees of an island: its decorations and fields, plus a few numbers.</summary>
    [Serializable]
    public sealed class IslandLayout
    {
        public List<DecorPlacement> decor = new();
        public List<FarmPlotRecord> plots = new();
        public int harvested, fished, words;
    }

    /// <summary>One row of the <c>island_showcases</c> table (see supabase/migrations/20261020_island_showcases.sql).</summary>
    [Serializable]
    public sealed class IslandShowcase
    {
        public string user_id;
        public string display_name;
        public string layout_json;
        public int likes;
        public string updated_at;

        public IslandLayout Layout => string.IsNullOrEmpty(layout_json) ? new IslandLayout() : JsonUtility.FromJson<IslandLayout>(layout_json) ?? new IslandLayout();

        public static IslandShowcase Of(string userId, string displayName, IslandRecord record) => new IslandShowcase
        {
            user_id = userId,
            display_name = displayName,
            likes = record.likes,
            layout_json = JsonUtility.ToJson(new IslandLayout
            {
                decor = record.decor.Select(d => new DecorPlacement { id = d.id, x = d.x, z = d.z, yaw = d.yaw }).ToList(),
                plots = record.plots.Select(p => new FarmPlotRecord { plotId = p.plotId, tilled = p.tilled, cropId = p.cropId, stage = p.stage, watered = p.watered, stageStartTicks = p.stageStartTicks }).ToList(),
                harvested = record.harvested, fished = record.fished, words = record.words.Count,
            }),
            updated_at = DateTime.UtcNow.ToString("o"),
        };

        /// <summary>A read-only island record built from the showcase (what the visited island scene shows).</summary>
        public IslandRecord ToRecord()
        {
            var layout = Layout;
            return new IslandRecord { welcomed = true, starterKitGiven = true, decor = layout.decor ?? new(), plots = layout.plots ?? new(), harvested = layout.harvested, fished = layout.fished, likes = likes };
        }
    }

    /// <summary>
    /// Visiting a friend's Midori Island. The visit loads the island scene with the friend's showcase as the island
    /// record (read-only: nothing is saved, fields and the store cannot be used); the visitor can walk, look, fish and
    /// leave a heart. Showcases are published by <see cref="IslandShowcaseService"/>. Friends lists call
    /// <see cref="Visit"/> (TASK-O in Docs/TEAM_TASKS.md).
    /// </summary>
    public static class IslandVisit
    {
        public static IslandShowcase Current { get; private set; }
        public static bool Active => Current != null;
        public static IslandRecord Record { get; private set; }
        public static bool Liked { get; private set; }
        public static event Action Changed;

        private static string _returnScene, _returnSpawn;
        private static bool _arriving;

        /// <summary>When visiting: says the island is read-only and returns true (fields, store, animals and pier refuse).</summary>
        public static bool ReadOnlyNotice()
        {
            if (!Active) return false;
            IslandUI.Toast(Loc.F("Đây là đảo của {0} — bạn chỉ ghé xem thôi.", "This is {0}'s island — you are just visiting.", "ここは{0}さんの島です — 見るだけです。", Current.display_name), true);
            return true;
        }

        /// <summary>Fetches the friend's showcase and travels to their island. <paramref name="done"/> gets an error message or null.</summary>
        public static void Visit(string userId, string displayName, Action<string> done = null)
        {
            IslandShowcaseService.Fetch(userId, (showcase, error) =>
            {
                if (showcase == null)
                {
                    string message = error ?? Loc.F("{0} chưa có đảo để thăm.", "{0} has no island to visit yet.", "{0}さんの島はまだありません。", displayName);
                    NihongoLife.UI.HudFeed.Post(message, NihongoLife.UI.HudFeed.Kind.Warning, 4f, "visit");
                    done?.Invoke(message);
                    return;
                }
                if (string.IsNullOrEmpty(showcase.display_name)) showcase.display_name = displayName;
                Begin(showcase);
                done?.Invoke(null);
            });
        }

        /// <summary>Starts a visit with a showcase already in hand (tests, or a cached friend island).</summary>
        public static void Begin(IslandShowcase showcase)
        {
            if (showcase == null) return;
            Current = showcase;
            Record = showcase.ToRecord();
            Liked = false;
            _arriving = true;
            SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded; SceneManager.sceneUnloaded += OnSceneUnloaded;
            var active = SceneManager.GetActiveScene().name;
            if (active == WorldLocationCatalog.MidoriIslandScene && IslandRuntime.Active != null)
            {
                // Already on Midori Island: swap the layout in place (IslandDecorRuntime listens), no loading screen.
                _returnScene = null;
                _arriving = false;
                Changed?.Invoke();
                return;
            }
            _returnScene = active; _returnSpawn = null;
            Changed?.Invoke();
            Travel(WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation,
                Loc.F("Đảo của {0}", "{0}'s island", "{0}さんの島", showcase.display_name));
        }

        /// <summary>Leaves the friend's island: back to the scene the visit started from (Hibari station by default).</summary>
        public static void End()
        {
            if (!Active) return;
            Current = null;
            Record = null;
            bool inPlace = string.IsNullOrEmpty(_returnScene) && SceneManager.GetActiveScene().name == WorldLocationCatalog.MidoriIslandScene;
            Changed?.Invoke();
            if (inPlace) return; // the visit started on the own island: it is simply back
            string scene = string.IsNullOrEmpty(_returnScene) || _returnScene == WorldLocationCatalog.MidoriIslandScene ? WorldLocationCatalog.StationScene : _returnScene;
            string spawn = scene == WorldLocationCatalog.StationScene ? WorldLocationCatalog.StationEntrance : _returnSpawn;
            Travel(scene, spawn, WorldLocationCatalog.Get(scene).DisplayName);
        }

        /// <summary>Leaves a heart on the visited island (once per visit; the server counts one per friend).</summary>
        public static void Like(Action<int> done = null)
        {
            if (!Active || Liked) return;
            Liked = true;
            Current.likes++;
            Changed?.Invoke();
            IslandShowcaseService.Like(Current.user_id, total => { if (Current != null && total >= 0) { Current.likes = total; Changed?.Invoke(); } done?.Invoke(total); });
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == WorldLocationCatalog.MidoriIslandScene) _arriving = false;
        }

        /// <summary>Leaving the island any other way (menu, death, train) also ends the visit, so the own island is back.</summary>
        private static void OnSceneUnloaded(Scene scene)
        {
            if (!Active || _arriving || scene.name != WorldLocationCatalog.MidoriIslandScene) return;
            Current = null;
            Record = null;
            Changed?.Invoke();
        }

        private static void Travel(string scene, string spawn, string title)
        {
            if (!GameServices.TryGet(out SceneFlowController flow)) flow = SceneRefs.Find<SceneFlowController>();
            if (flow == null) { SceneContentLoader.LoadFallback(scene); return; }
            if (string.IsNullOrEmpty(spawn)) flow.LoadScene(scene, title);
            else flow.TransferZone(scene, spawn, title);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Current = null; Record = null; Liked = false; Changed = null; _returnScene = _returnSpawn = null; _arriving = false; }
    }

    /// <summary>
    /// Publishes the player's island to Supabase (<c>island_showcases</c>, upsert by user id) when it changes, at most
    /// every 20 seconds, and reads friends' islands. Works only when signed in; offline play is unaffected.
    /// </summary>
    public sealed class IslandShowcaseService : MonoBehaviour
    {
        private const string Table = "island_showcases";
        private static IslandShowcaseService _instance;
        private bool _dirty;
        private float _next;

        /// <summary>Tests: a showcase to return instead of asking the server.</summary>
        public static Func<string, IslandShowcase> FetchOverride;
        public static string LastPublishedJson { get; private set; }

        private static IslandShowcaseService Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("IslandShowcaseService");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<IslandShowcaseService>();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            IslandState.Changed -= MarkDirty;
            IslandState.Changed += MarkDirty;
        }

        private static void MarkDirty()
        {
            if (IslandVisit.Active) return;
            Instance._dirty = true;
        }

        private void Update()
        {
            if (!_dirty || Time.unscaledTime < _next) return;
            _dirty = false;
            _next = Time.unscaledTime + 20f;
            Publish();
        }

        /// <summary>Uploads the player's island now (also when leaving the island).</summary>
        public static void Publish()
        {
            if (IslandVisit.Active) return;
            if (!TryClient(out var client, out var auth)) return;
            var showcase = IslandShowcase.Of(auth.UserId, auth.DisplayName, IslandState.Record);
            string body = JsonUtility.ToJson(new Row { user_id = showcase.user_id, display_name = showcase.display_name, layout_json = showcase.layout_json, updated_at = showcase.updated_at });
            LastPublishedJson = body;
            client.Upsert(client.RestUrl(Table), body, _ => { }, (code, error) => Debug.LogWarning($"[IslandShowcase] Publish failed ({code}): {error}"));
        }

        public static void Fetch(string userId, Action<IslandShowcase, string> done)
        {
            if (FetchOverride != null) { done(FetchOverride(userId), null); return; }
            if (!TryClient(out var client, out _))
            {
                done(null, Loc.T("Cần đăng nhập online để thăm đảo bạn bè.", "Sign in online to visit friends' islands.", "友だちの島に行くにはオンラインでログインしてください。"));
                return;
            }
            client.Get(client.RestUrl(Table) + $"?user_id=eq.{Uri.EscapeDataString(userId)}&select=*", json =>
            {
                var rows = JsonUtility.FromJson<Rows>("{\"items\":" + json + "}");
                done(rows?.items != null && rows.items.Length > 0 ? rows.items[0] : null, null);
            }, (code, error) => done(null, Loc.F("Không tải được đảo ({0}).", "Could not load the island ({0}).", "島を読み込めませんでした（{0}）。", code)));
        }

        public static void Like(string ownerId, Action<int> done)
        {
            if (!TryClient(out var client, out _)) { done?.Invoke(-1); return; }
            client.Post(client.RestUrl("rpc/like_island"), "{\"target\":\"" + ownerId + "\"}",
                json => done?.Invoke(int.TryParse(json?.Trim(), out int n) ? n : -1),
                (code, error) => { Debug.LogWarning($"[IslandShowcase] Like failed ({code}): {error}"); done?.Invoke(-1); });
        }

        private static bool TryClient(out SupabaseClient client, out IAuthService auth)
        {
            client = SceneRefs.Find<SupabaseClient>();
            if (!GameServices.TryGet(out auth)) auth = SceneRefs.Find<SupabaseAuthService>();
            return client != null && client.IsConfigured && auth != null && OnlineAccess.IsRegistered;
        }

        [Serializable] private sealed class Row { public string user_id, display_name, layout_json, updated_at; }
        [Serializable] private sealed class Rows { public IslandShowcase[] items; }
    }
}
