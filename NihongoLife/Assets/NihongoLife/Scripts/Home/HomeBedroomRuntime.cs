using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Interaction;
using NihongoLife.Player;

namespace NihongoLife.Home
{
    /// <summary>
    /// The room geometry (floor/walls/bed/desk/rug/BedRestPoint) is baked directly into
    /// 45_HomeBedroom.unity by GameplayZoneSceneBuilder.BuildHomeBedroom() — it used to be built here
    /// every Start(), which meant the saved scene had no visible room until Play was pressed. This
    /// component now only owns the live status HUD (energy/rest/knowledge/yen), which genuinely needs
    /// to run at runtime, the same way the rest of the HUD is runtime-built.
    /// </summary>
    public sealed class HomeBedroomRuntime : MonoBehaviour
    {
        public static HomeBedroomRuntime Instance { get; private set; }
        public event System.Action OnNewDay;
        private TextMeshProUGUI _statusText;
        private CanvasGroup _sleepFade;
        private TextMeshProUGUI _sleepMessage;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void ResetDailyActivities() => OnNewDay?.Invoke();

        public void SetRoomLights(bool lit, bool includeInactive)
        {
            foreach (var light in GetComponentsInChildren<Light>(includeInactive)) light.enabled = lit;
        }

        public System.Collections.IEnumerator FadeToNight(string title, string subtitle)
        {
            EnsureSleepFade();
            _sleepMessage.text = title + "\n" + subtitle;
            yield return FadeSleep(1f);
        }

        public System.Collections.IEnumerator FadeToMorning(string title, string subtitle, string summary)
        {
            EnsureSleepFade();
            _sleepMessage.text = title + "\n" + subtitle + "\n<size=70%>" + summary + "</size>";
            yield return new WaitForSecondsRealtime(1.5f);
            yield return FadeSleep(0f);
        }

        private void EnsureSleepFade()
        {
            if (_sleepFade != null) return;
            var root = new GameObject("SleepFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var panel = new GameObject("Night", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.015f, .025f, .045f, 1f);
            _sleepMessage = MakeText(panel.transform, "", 28, Color.white);
            _sleepMessage.rectTransform.anchorMin = _sleepMessage.rectTransform.anchorMax = new Vector2(.5f, .5f);
            _sleepMessage.rectTransform.pivot = new Vector2(.5f, .5f);
            _sleepMessage.rectTransform.sizeDelta = new Vector2(1100f, 240f);
            _sleepMessage.alignment = TextAlignmentOptions.Center;
            _sleepFade = root.GetComponent<CanvasGroup>();
            _sleepFade.alpha = 0f;
            _sleepFade.blocksRaycasts = false;
        }

        private System.Collections.IEnumerator FadeSleep(float target)
        {
            float start = _sleepFade.alpha;
            _sleepFade.blocksRaycasts = true;
            for (float t = 0f; t < .6f; t += Time.unscaledDeltaTime)
            {
                _sleepFade.alpha = Mathf.Lerp(start, target, t / .6f);
                yield return null;
            }
            _sleepFade.alpha = target;
            _sleepFade.blocksRaycasts = target > 0f;
        }

        private void Start()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            gameObject.name = "HomeBedroom_YourRoom";
            var canvasObject = new GameObject("BedroomHUD");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasObject.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("StatusPanel");
            panel.transform.SetParent(canvasObject.transform, false);
            panel.AddComponent<Image>().color = new Color(0.02f, 0.05f, 0.08f, 0.88f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(28f, -300f); rect.sizeDelta = new Vector2(390f, 132f);
            var title = MakeText(panel.transform, "PHÒNG RIÊNG  •  YOUR ROOM", 22, new Color(1f, 0.82f, 0.3f));
            title.rectTransform.anchoredPosition = new Vector2(20f, -28f);
            _statusText = MakeText(panel.transform, "Đang tải trạng thái...", 16, Color.white);
            _statusText.rectTransform.anchoredPosition = new Vector2(20f, -74f);
            var help = MakeText(canvasObject.transform, "[F] Nghỉ ngơi trên giường   •   [Esc] Đóng bảng", 18, new Color(0.8f, 0.9f, 0.95f));
            help.alignment = TextAlignmentOptions.Center;
            help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(.5f, 0f);
            help.rectTransform.pivot = new Vector2(.5f, 0f);
            help.rectTransform.sizeDelta = new Vector2(900f, 52f);
            help.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        }

        private TextMeshProUGUI MakeText(Transform parent, string value, int size, Color color)
        {
            var textObject = new GameObject("Text"); textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, .5f);
            text.rectTransform.sizeDelta = new Vector2(350f, 52f); return text;
        }

        private void Update()
        {
            if (_statusText == null) return;
            var status = PlayerStatus.Instance; var inventory = FindFirstObjectByType<PlayerInventory>();
            _statusText.text = status == null ? "Phòng cá nhân • Room ID riêng" :
                $"Năng lượng {status.CurrentEnergy:0}   Nghỉ ngơi {status.Restfulness:0}\nKiến thức {status.Knowledge}   Tiền {(inventory == null ? 0 : inventory.Yen):N0}¥";
        }
    }

}
