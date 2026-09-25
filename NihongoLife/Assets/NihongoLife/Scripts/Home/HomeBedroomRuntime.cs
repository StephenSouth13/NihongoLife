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
        private TextMeshProUGUI _statusText;

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
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("StatusPanel");
            panel.transform.SetParent(canvasObject.transform, false);
            panel.AddComponent<Image>().color = new Color(0.02f, 0.05f, 0.08f, 0.88f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(28f, -28f); rect.sizeDelta = new Vector2(390f, 132f);
            var title = MakeText(panel.transform, "PHÒNG RIÊNG  •  YOUR ROOM", 22, new Color(1f, 0.82f, 0.3f));
            title.rectTransform.anchoredPosition = new Vector2(20f, -28f);
            _statusText = MakeText(panel.transform, "Đang tải trạng thái...", 16, Color.white);
            _statusText.rectTransform.anchoredPosition = new Vector2(20f, -74f);
            var help = MakeText(canvasObject.transform, "[F] Nghỉ ngơi trên giường   •   [Esc] Đóng bảng", 18, new Color(0.8f, 0.9f, 0.95f));
            help.alignment = TextAlignmentOptions.Center; help.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        }

        private TextMeshProUGUI MakeText(Transform parent, string value, int size, Color color)
        {
            var textObject = new GameObject("Text"); textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Left;
            text.rectTransform.sizeDelta = new Vector2(900f, 52f); return text;
        }

        private void Update()
        {
            if (_statusText == null) return;
            var status = PlayerStatus.Instance; var inventory = FindFirstObjectByType<PlayerInventory>();
            _statusText.text = status == null ? "Phòng cá nhân • Room ID riêng" :
                $"Năng lượng {status.CurrentEnergy:0}   Nghỉ ngơi {status.Restfulness:0}\nKiến thức {status.Knowledge}   Tiền {(inventory == null ? 0 : inventory.Yen):N0}¥";
        }
    }

    public sealed class BedroomRestInteractable : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => "[F] 休む";
        public string GetpromptEn() => "[F] Sleep / Rest";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            if (PlayerStatus.Instance != null) PlayerStatus.Instance.Sleep(8f);
            var animator = player != null ? player.GetComponentInChildren<NihongoLife.Core.CharacterAnimationController>() : null;
            if (animator != null && !animator.SetResting(true)) animator.SetSitting(true);
            Invoke(nameof(StandUp), 3f);
        }
        private void StandUp()
        {
            var animator = FindFirstObjectByType<NihongoLife.Core.CharacterAnimationController>();
            if (animator != null) animator.SetResting(false);
        }
    }
}
