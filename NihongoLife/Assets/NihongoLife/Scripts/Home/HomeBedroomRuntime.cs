using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Interaction;
using NihongoLife.Player;

namespace NihongoLife.Home
{
    public sealed class HomeBedroomRuntime : MonoBehaviour
    {
        private Material _wall;
        private Material _floor;
        private Material _wood;
        private Material _accent;
        private TextMeshProUGUI _statusText;

        private void Start()
        {
            _wall = Material("Wall", new Color(0.82f, 0.88f, 0.9f));
            _floor = Material("Floor", new Color(0.16f, 0.2f, 0.25f));
            _wood = Material("Wood", new Color(0.42f, 0.22f, 0.12f));
            _accent = Material("Accent", new Color(0.24f, 0.62f, 0.58f));
            BuildRoom();
            BuildUi();
        }

        private void BuildRoom()
        {
            Block("Floor", new Vector3(0f, -0.15f, 0f), new Vector3(12f, 0.3f, 9f), _floor);
            Block("BackWall", new Vector3(0f, 2.5f, 4.35f), new Vector3(12f, 5f, 0.3f), _wall);
            Block("LeftWall", new Vector3(-5.85f, 2.5f, 0f), new Vector3(0.3f, 5f, 9f), _wall);
            Block("RightWall", new Vector3(5.85f, 2.5f, 0f), new Vector3(0.3f, 5f, 9f), _wall);
            Block("BedFrame", new Vector3(-2.8f, 0.45f, 1.4f), new Vector3(4.2f, 0.55f, 2.1f), _wood);
            Block("Mattress", new Vector3(-2.8f, 0.82f, 1.4f), new Vector3(3.9f, 0.25f, 1.9f), _accent);
            CreateRestPoint("BedRestPoint", new Vector3(-2.8f, 1.05f, 0.85f), Quaternion.Euler(0f, 180f, 0f));
            Block("Desk", new Vector3(2.4f, 0.9f, 2.6f), new Vector3(2.3f, 0.18f, 1f), _wood);
            Block("DeskLeg", new Vector3(1.55f, 0.4f, 2.6f), new Vector3(0.16f, 0.8f, 0.16f), _wood);
            Block("DeskLeg", new Vector3(3.25f, 0.4f, 2.6f), new Vector3(0.16f, 0.8f, 0.16f), _wood);
            Block("WindowGlow", new Vector3(2.3f, 2.7f, 4.15f), new Vector3(3.2f, 1.8f, 0.08f), _accent);
            Block("Rug", new Vector3(1f, 0.03f, -1.5f), new Vector3(4.4f, 0.05f, 2.6f), _accent);
            Block("BedHeadboard", new Vector3(-2.8f, 1.5f, 2.25f), new Vector3(4.2f, 1.4f, 0.22f), _wood);
        }

        private void CreateRestPoint(string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2.4f, 1.5f, 1.5f);
            go.AddComponent<BedroomRestInteractable>();
        }

        private GameObject Block(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            return material;
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
