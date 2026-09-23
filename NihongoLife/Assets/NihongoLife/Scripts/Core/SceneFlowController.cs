using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NihongoLife.Player;

namespace NihongoLife.Core
{
    public class SceneFlowController : MonoBehaviour, IGameService
    {
        private CanvasGroup _overlay;
        private TextMeshProUGUI _placeName;
        private TextMeshProUGUI _status;
        private Image _progressFill;
        private bool _isLoading;
        private string _hostSceneName;
        private string _activeZoneSceneName;

        public bool IsLoading => _isLoading;

        public void Initialize()
        {
            EnsureOverlay();
            Debug.Log("[SceneFlowController] Initialized.");
        }

        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSingleRoutine(sceneName, FriendlyName(sceneName)));
        }

        public void LoadScene(string sceneName, string displayName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSingleRoutine(sceneName, displayName));
        }

        public void EnterZone(string sceneName, string spawnId, string displayName)
        {
            if (_isLoading) return;
            StartCoroutine(EnterZoneRoutine(sceneName, spawnId, displayName));
        }

        public void ExitZone(string sceneName, string citySpawnId, string displayName = "街へ戻る / Trở lại thành phố")
        {
            if (_isLoading) return;
            StartCoroutine(ExitZoneRoutine(sceneName, citySpawnId, displayName));
        }

        public string GetCurrentSceneName() => SceneManager.GetActiveScene().name;

        private IEnumerator LoadSingleRoutine(string sceneName, string displayName)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(displayName);
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                yield return FailAndRelease($"Không tìm thấy scene {sceneName}");
                yield break;
            }
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f)
            {
                SetProgress(operation.progress / 0.9f, "Đang chuẩn bị khu vực...");
                yield return null;
            }
            SetProgress(1f, "Sẵn sàng");
            yield return new WaitForSecondsRealtime(0.18f);
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
        }

        private IEnumerator EnterZoneRoutine(string sceneName, string spawnId, string displayName)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(displayName);

            Scene host = SceneManager.GetActiveScene();
            if (!host.IsValid() || !host.isLoaded || string.Equals(host.name, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                yield return FailAndRelease($"Cannot enter {sceneName} from the current scene.");
                yield break;
            }
            _hostSceneName = host.name;

            Scene zone = SceneManager.GetSceneByName(sceneName);
            if (!zone.isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    yield return FailAndRelease($"Không tìm thấy zone {sceneName}");
                    yield break;
                }
                while (!load.isDone)
                {
                    SetProgress(Mathf.Clamp01(load.progress / 0.9f), "Đang mở khu vực...");
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
                yield return FailAndRelease($"Spawn {spawnId} is missing from {sceneName}.");
                yield break;
            }
            SceneManager.SetActiveScene(zone);
            _activeZoneSceneName = zone.name;
            SetProgress(1f, "Đã đến nơi");
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
        }

        private IEnumerator ExitZoneRoutine(string sceneName, string citySpawnId, string displayName)
        {
            _isLoading = true;
            LockPlayer(true);
            yield return FadeIn(displayName);

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
                yield return FailAndRelease($"Return spawn {citySpawnId} is missing from {city.name}.");
                yield break;
            }
            SceneManager.SetActiveScene(city);
            if (zone.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(zone);
                while (unload != null && !unload.isDone)
                {
                    SetProgress(unload.progress, "Đang trở lại thành phố...");
                    yield return null;
                }
            }
            _activeZoneSceneName = null;
            _hostSceneName = null;
            SetProgress(1f, "Đã đến nơi");
            yield return FadeOut();
            LockPlayer(false);
            _isLoading = false;
        }

        private static void SetZoneRootsActive(Scene scene, bool active)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var visibility = root.GetComponent<SceneZoneVisibility>();
                if (visibility != null && visibility.HideWhenZoneChanges) root.SetActive(active);
            }
        }

        private static bool MovePlayerToSpawn(Scene scene, string spawnId)
        {
            var player = FindFirstObjectByType<PlayerController>();
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
                    return true;
                }
            }
            Debug.LogWarning($"[SceneFlowController] Spawn '{spawnId}' was not found in {scene.name}.");
            return false;
        }

        private IEnumerator FadeIn(string displayName)
        {
            EnsureOverlay();
            _placeName.text = displayName;
            SetProgress(0f, "Đang di chuyển...");
            _overlay.blocksRaycasts = true;
            for (float t = 0f; t < 0.38f; t += Time.unscaledDeltaTime)
            {
                _overlay.alpha = Mathf.SmoothStep(0f, 1f, t / 0.38f);
                yield return null;
            }
            _overlay.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _overlay.alpha = Mathf.SmoothStep(1f, 0f, t / 0.5f);
                yield return null;
            }
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;
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
            if (_progressFill != null) _progressFill.fillAmount = Mathf.Clamp01(value);
            if (_status != null) _status.text = message;
        }

        private static void LockPlayer(bool locked)
        {
            var player = FindFirstObjectByType<PlayerController>();
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

            var background = CreateImage(canvasGo.transform, "Background", new Color(0.018f, 0.025f, 0.028f, 1f));
            Stretch(background.rectTransform);
            _overlay = canvasGo.AddComponent<CanvasGroup>();
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;

            var accent = CreateImage(canvasGo.transform, "Accent", new Color(0.96f, 0.66f, 0.18f, 1f));
            accent.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            accent.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            accent.rectTransform.sizeDelta = new Vector2(72f, 4f);
            accent.rectTransform.anchoredPosition = new Vector2(0f, 52f);

            _placeName = CreateText(canvasGo.transform, "PlaceName", new Vector2(0f, 108f), new Vector2(1100f, 80f), 38f);
            _placeName.fontStyle = FontStyles.Bold;
            _status = CreateText(canvasGo.transform, "Status", new Vector2(0f, -56f), new Vector2(700f, 40f), 17f);
            _status.color = new Color(0.72f, 0.78f, 0.8f, 1f);

            var track = CreateImage(canvasGo.transform, "ProgressTrack", new Color(0.12f, 0.15f, 0.16f, 1f));
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(420f, 6f);
            track.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            var fill = CreateImage(track.transform, "Fill", new Color(0.96f, 0.66f, 0.18f, 1f));
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            _progressFill = fill;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = fontSize;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static string FriendlyName(string sceneName) => sceneName switch
        {
            "01_MainMenu" => "NIHONGO LIFE",
            "90_TestSandbox" => "日本の町 / Thành phố Nihongo",
            "20_StationDistrict" => "駅前 / Khu nhà ga",
            "30_SushiRestaurant" => "すし店 / Nhà hàng sushi",
            "40_ShoppingDistrict" => "ショッピング / Khu mua sắm",
            _ => sceneName
        };
    }
}
