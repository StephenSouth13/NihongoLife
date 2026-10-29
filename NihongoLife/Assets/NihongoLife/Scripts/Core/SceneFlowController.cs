using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NihongoLife.Player;
using NihongoLife.Cameras;

namespace NihongoLife.Core
{
    public class SceneFlowController : MonoBehaviour, IGameService
    {
        private CanvasGroup _overlay;
        private TextMeshProUGUI _placeName;
        private TextMeshProUGUI _status;
        private Image _progressFill;
        private NihongoLife.UI.LoadingCard _card;
        private bool _isLoading;
        private string _hostSceneName;
        private string _activeZoneSceneName;
        private readonly Dictionary<GameObject, bool> _hiddenRootStates = new();

        public bool IsLoading => _isLoading;
        /// <summary>The zone the player is in (empty in the town).</summary>
        public string ActiveZone => _activeZoneSceneName ?? string.Empty;

        public void Initialize()
        {
            EnsureOverlay();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Debug.Log("[SceneFlowController] Initialized.");
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        // A single-mode load (title, save load, direct LoadScene) replaces every zone, so stale zone state must not block the next EnterZone.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || _isLoading) return;
            _hostSceneName = null;
            _activeZoneSceneName = null;
            _hiddenRootStates.Clear();
        }

        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            LoadScene(sceneName, FriendlyName(sceneName));
        }

        public void LoadScene(string sceneName, string displayName)
        {
            if (_isLoading || !CanLoad(sceneName)) return;
            StartCoroutine(LoadSingleRoutine(sceneName, displayName));
        }

        public void EnterZone(string sceneName, string spawnId, string displayName)
        {
            if (_isLoading || !CanLoad(sceneName) || !string.IsNullOrEmpty(_activeZoneSceneName)) return;
            if (!PassesUnlock(sceneName)) return;
            StartCoroutine(EnterZoneRoutine(sceneName, spawnId, displayName));
        }

        /// <summary>Moves the player from the active zone straight into another zone (train arrivals);
        /// the host city stays hidden and the zone's own exit still returns to the city.</summary>
        public void TransferZone(string sceneName, string spawnId, string displayName, System.Action onArrived = null)
        {
            if (_isLoading || !CanLoad(sceneName)) return;
            if (!PassesUnlock(sceneName)) return;
            if (string.IsNullOrEmpty(_activeZoneSceneName))
            {
                StartCoroutine(EnterZoneRoutine(sceneName, spawnId, displayName, onArrived));
                return;
            }
            StartCoroutine(TransferZoneRoutine(sceneName, spawnId, displayName, onArrived));
        }

        public void ExitZone(string sceneName, string citySpawnId, string displayName = null)
        {
            if (_isLoading) return;
            StartCoroutine(ExitZoneRoutine(sceneName, citySpawnId, displayName));
        }

        public string GetCurrentSceneName() => SceneManager.GetActiveScene().name;

        /// <summary>The one place where locked locations are refused (LocationUnlocks); an entered place stays open.</summary>
        private static bool PassesUnlock(string sceneName)
        {
            if (!LocationUnlocks.IsUnlocked(sceneName))
            {
                NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.T("Chưa mở: ", "Locked: ", "未開放：") + LocationUnlocks.LockedMessage(sceneName), NihongoLife.UI.HudFeed.Kind.Warning, 6f, "unlock");
                if (GameServices.TryGet(out NihongoLife.Audio.IAudioService audio)) audio.PlayCue(NihongoLife.Audio.GameAudioCue.UiError, 0.7f);
                return false;
            }
            LocationUnlocks.MarkVisited(sceneName);
            LocationUnlocks.Evaluate();
            return true;
        }

        private IEnumerator LoadSingleRoutine(string sceneName, string displayName)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(sceneName, Title(sceneName, displayName));
            yield return SceneContentLoader.EnsureAvailable(sceneName, progress =>
                SetProgress(Mathf.Clamp01(progress), NihongoLife.Core.Loc.T("Đang tải khu vực...", "Downloading the area...", "エリアをダウンロード中...")));
            if (!string.IsNullOrEmpty(SceneContentLoader.LastError))
            {
                yield return FailAndRelease(SceneContentLoader.LastError);
                yield break;
            }
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                yield return FailAndRelease(NihongoLife.Core.Loc.F("Không tìm thấy scene {0}", "Scene {0} not found", "シーン{0}が見つかりません", sceneName));
                yield break;
            }
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f)
            {
                SetProgress(operation.progress / 0.9f, NihongoLife.Core.Loc.T("Đang chuẩn bị khu vực...", "Preparing the area...", "エリアを準備中..."));
                yield return null;
            }
            SetProgress(1f, NihongoLife.Core.Loc.T("Sẵn sàng", "Ready", "準備完了"));
            yield return new WaitForSecondsRealtime(0.18f);
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;
            _hostSceneName = null;
            _activeZoneSceneName = null;
            _hiddenRootStates.Clear();
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
        }

        private IEnumerator EnterZoneRoutine(string sceneName, string spawnId, string displayName, System.Action onArrived = null)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(sceneName, Title(sceneName, displayName));

            Scene host = SceneManager.GetActiveScene();
            if (!host.IsValid() || !host.isLoaded || string.Equals(host.name, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                yield return FailAndRelease($"Cannot enter {sceneName} from the current scene.");
                yield break;
            }
            _hostSceneName = host.name;

            Scene zone = SceneManager.GetSceneByName(sceneName);
            bool newlyLoaded = !zone.isLoaded;
            if (newlyLoaded)
            {
                yield return SceneContentLoader.EnsureAvailable(sceneName, progress =>
                    SetProgress(Mathf.Clamp01(progress), NihongoLife.Core.Loc.T("Đang tải khu vực...", "Downloading the area...", "エリアをダウンロード中...")));
                if (!string.IsNullOrEmpty(SceneContentLoader.LastError))
                {
                    yield return FailAndRelease(SceneContentLoader.LastError);
                    yield break;
                }
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    yield return FailAndRelease(NihongoLife.Core.Loc.F("Không tìm thấy khu vực {0}", "Area {0} not found", "エリア{0}が見つかりません", sceneName));
                    yield break;
                }
                while (!load.isDone)
                {
                    SetProgress(Mathf.Clamp01(load.progress / 0.9f), NihongoLife.Core.Loc.T("Đang mở khu vực...", "Opening the area...", "エリアを開いています..."));
                    yield return null;
                }
                zone = SceneManager.GetSceneByName(sceneName);
            }

            if (!zone.IsValid() || !zone.isLoaded)
            {
                yield return FailAndRelease($"Zone {sceneName} did not finish loading.");
                yield break;
            }

            SetZoneRootsActive(host, false);
            SetZoneRootsActive(zone, true);
            if (!MovePlayerToSpawn(zone, spawnId))
            {
                SetZoneRootsActive(host, true);
                if (newlyLoaded) yield return SceneManager.UnloadSceneAsync(zone);
                else SetZoneRootsActive(zone, false);
                _hostSceneName = null;
                yield return FailAndRelease($"Spawn {spawnId} is missing from {sceneName}.");
                yield break;
            }
            SceneManager.SetActiveScene(zone);
            foreach (GameObject root in zone.GetRootGameObjects())
                foreach (var preview in root.GetComponentsInChildren<ZonePreviewCamera>(true))
                    preview.gameObject.SetActive(false);
            _activeZoneSceneName = zone.name;
            SetProgress(1f, NihongoLife.Core.Loc.T("Đã đến nơi", "Arrived", "到着しました"));
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
            onArrived?.Invoke();
        }

        private IEnumerator TransferZoneRoutine(string sceneName, string spawnId, string displayName, System.Action onArrived)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(sceneName, Title(sceneName, displayName));

            Scene from = SceneManager.GetSceneByName(_activeZoneSceneName);
            Scene to = SceneManager.GetSceneByName(sceneName);
            bool newlyLoaded = !to.isLoaded;
            if (newlyLoaded)
            {
                yield return SceneContentLoader.EnsureAvailable(sceneName, progress =>
                    SetProgress(Mathf.Clamp01(progress), NihongoLife.Core.Loc.T("Đang tải khu vực...", "Downloading the area...", "エリアをダウンロード中...")));
                if (!string.IsNullOrEmpty(SceneContentLoader.LastError))
                {
                    yield return FailAndRelease(SceneContentLoader.LastError);
                    yield break;
                }
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    yield return FailAndRelease(NihongoLife.Core.Loc.F("Không tìm thấy khu vực {0}", "Area {0} not found", "エリア{0}が見つかりません", sceneName));
                    yield break;
                }
                while (!load.isDone)
                {
                    SetProgress(Mathf.Clamp01(load.progress / 0.9f), NihongoLife.Core.Loc.T("Đang tới nơi...", "On the way...", "移動中..."));
                    yield return null;
                }
                to = SceneManager.GetSceneByName(sceneName);
            }
            if (!to.IsValid() || !to.isLoaded)
            {
                yield return FailAndRelease($"Zone {sceneName} did not finish loading.");
                yield break;
            }

            SetZoneRootsActive(to, true);
            if (!MovePlayerToSpawn(to, spawnId))
            {
                if (newlyLoaded) yield return SceneManager.UnloadSceneAsync(to);
                yield return FailAndRelease($"Spawn {spawnId} is missing from {sceneName}.");
                yield break;
            }
            SceneManager.SetActiveScene(to);
            foreach (GameObject root in to.GetRootGameObjects())
                foreach (var preview in root.GetComponentsInChildren<ZonePreviewCamera>(true))
                    preview.gameObject.SetActive(false);
            if (from.IsValid() && from.isLoaded && from != to)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(from);
                while (unload != null && !unload.isDone) yield return null;
            }
            _activeZoneSceneName = to.name;
            SetProgress(1f, NihongoLife.Core.Loc.T("Đã đến nơi", "Arrived", "到着しました"));
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
            onArrived?.Invoke();
        }

        private IEnumerator ExitZoneRoutine(string sceneName, string citySpawnId, string displayName)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(sceneName, Title(sceneName, displayName));

            string zoneName = string.IsNullOrWhiteSpace(_activeZoneSceneName) ? sceneName : _activeZoneSceneName;
            Scene zone = SceneManager.GetSceneByName(zoneName);
            Scene city = string.IsNullOrWhiteSpace(_hostSceneName)
                ? SceneManager.GetSceneByName(WorldLocationCatalog.CityScene)
                : SceneManager.GetSceneByName(_hostSceneName);
            if (!city.IsValid() || !city.isLoaded || city == zone)
            {
                yield return FailAndRelease("The return city scene is not loaded.");
                yield break;
            }

            SetZoneRootsActive(city, true);
            if (!MovePlayerToSpawn(city, citySpawnId))
            {
                SetZoneRootsActive(city, false);
                yield return FailAndRelease($"Return spawn {citySpawnId} is missing from {city.name}.");
                yield break;
            }
            SceneManager.SetActiveScene(city);
            if (zone.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(zone);
                while (unload != null && !unload.isDone)
                {
                    SetProgress(unload.progress, NihongoLife.Core.Loc.T("Đang trở lại thành phố...", "Returning to town...", "町に戻っています..."));
                    yield return null;
                }
            }
            _activeZoneSceneName = null;
            _hostSceneName = null;
            SetProgress(1f, NihongoLife.Core.Loc.T("Đã đến nơi", "Arrived", "到着しました"));
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
        }

        private static bool CanLoad(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName)) return true;
            Debug.LogError($"[SceneFlowController] Scene '{sceneName}' is not available in build settings.");
            return false;
        }

        private void SetZoneRootsActive(Scene scene, bool active)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var visibility = root.GetComponent<SceneZoneVisibility>();
                if (visibility == null || !visibility.HideWhenZoneChanges) continue;
                if (!active)
                {
                    if (!_hiddenRootStates.ContainsKey(root)) _hiddenRootStates[root] = root.activeSelf;
                    root.SetActive(false);
                }
                else if (_hiddenRootStates.TryGetValue(root, out bool wasActive))
                {
                    root.SetActive(wasActive);
                    _hiddenRootStates.Remove(root);
                }
            }
        }

        private static bool MovePlayerToSpawn(Scene scene, string spawnId)
        {
            var player = SceneRefs.Find<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[SceneFlowController] Player was not found during scene transition.");
                return false;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var spawn in root.GetComponentsInChildren<SceneSpawnPoint>(true))
                {
                    if (!string.Equals(spawn.Id, spawnId, System.StringComparison.OrdinalIgnoreCase)) continue;
                    var controller = player.GetComponent<CharacterController>();
                    if (controller != null) controller.enabled = false;
                    player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
                    if (controller != null) controller.enabled = true;
                    var camera = SceneRefs.Find<ThirdPersonCameraController>();
                    if (camera != null)
                    {
                        camera.ClearConversationTarget();
                        camera.SetTarget(player.transform);
                        bool indoor = scene.name == WorldLocationCatalog.HomeBedroomScene
                            || scene.name == WorldLocationCatalog.SchoolScene || scene.name == WorldLocationCatalog.SushiRestaurantScene
                            || scene.name == WorldLocationCatalog.GameCenterScene || scene.name == WorldLocationCatalog.ClinicScene;
                        camera.SetIndoorMode(indoor);
                        camera.SetOrbit(spawn.transform.eulerAngles.y, indoor ? 22f : 20f, indoor ? 3.2f : 7.2f);
                    }
                    return true;
                }
            }
            Debug.LogWarning($"[SceneFlowController] Spawn '{spawnId}' was not found in {scene.name}.");
            return false;
        }

        private IEnumerator FadeIn(string sceneName, string displayName)
        {
            EnsureOverlay();
            _card.Show(sceneName, displayName);
            SetProgress(0f, NihongoLife.Core.Loc.T("Đang di chuyển...", "Travelling...", "移動中..."));
            _overlay.blocksRaycasts = true;
            if (MotionSettings.ReduceMotion)
            {
                _overlay.alpha = 1f;
                yield break;
            }
            for (float t = 0f; t < 0.38f; t += Time.unscaledDeltaTime)
            {
                _overlay.alpha = Mathf.SmoothStep(0f, 1f, t / 0.38f);
                yield return null;
            }
            _overlay.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (MotionSettings.ReduceMotion)
            {
                _overlay.alpha = 0f;
                _overlay.blocksRaycasts = false;
                _card.Hide();
                yield break;
            }
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _overlay.alpha = Mathf.SmoothStep(1f, 0f, t / 0.5f);
                yield return null;
            }
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;
            _card.Hide();
        }

        private IEnumerator FailAndRelease(string message)
        {
            _status.text = message;
            yield return new WaitForSecondsRealtime(1.2f);
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
        }

        private void SetProgress(float value, string message)
        {
            if (_card != null) _card.SetProgress(value);
            if (_status != null) _status.text = message;
        }

        private static void LockPlayer(bool locked)
        {
            // A dialogue left open across a zone change stays "open" but invisible, which silently blocks every
            // NPC in the new zone (e.g. Kimura). Pause it instead: story progress is kept and R resumes it.
            if (locked)
            {
                var dialogue = NihongoLife.Dialogue.DialogueManager.Instance;
                if (dialogue != null) dialogue.SuspendForTravel();
            }
            var player = SceneRefs.Find<PlayerController>();
            if (player != null) player.InputLocked = locked;
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            var canvasGo = new GameObject("SceneTransitionCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _overlay = canvasGo.AddComponent<CanvasGroup>();
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;
            // The destination's picture, its names, the player riding the progress bar and a phrase to learn.
            _card = canvasGo.AddComponent<NihongoLife.UI.LoadingCard>();
            _card.Build(NihongoLife.UI.NLUi.ResolveFont());
            _placeName = _card.PlaceName;
            _status = _card.Status;
            _progressFill = _card.Fill;
            _card.Hide();
        }

        private static string FriendlyName(string sceneName) => sceneName == "01_MainMenu" ? "NIHONGO LIFE" : Title(sceneName, null);

        /// <summary>
        /// The place name on the loading screen, in the interface language: the location catalog when it knows the
        /// scene, otherwise the caller's text (a <see cref="Loc.Triple"/>, or a scene-authored "日本語 / Tiếng Việt" pair).
        /// </summary>
        private static string Title(string sceneName, string displayName)
        {
            if (WorldLocationCatalog.Has(sceneName)) return WorldLocationCatalog.Get(sceneName).DisplayName;
            if (string.IsNullOrEmpty(displayName)) return sceneName ?? "";
            if (NihongoLife.Core.Loc.IsTriple(displayName)) return NihongoLife.Core.Loc.Resolve(displayName);
            int split = displayName.IndexOf(" / ", System.StringComparison.Ordinal);
            if (split < 0) return displayName;
            return NihongoLife.Core.Loc.IsJapanese ? displayName.Substring(0, split) : displayName.Substring(split + 3);
        }
    }
}
