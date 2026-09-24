using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.Core;
using UnityEngine.UI;

namespace NihongoLife.School
{
    /// <summary>
    /// Presentation and interaction pass for the authored Hibari classroom scene.
    /// It only decorates existing objects; it never creates a second environment.
    /// </summary>
    public sealed class HibariClassroomRuntime : MonoBehaviour
    {
        [SerializeField] private bool applyMaterials = true;
        [SerializeField] private bool installSeatInteractions = true;
        [SerializeField] private bool installExamStation = true;

        private readonly List<Material> _runtimeMaterials = new List<Material>();

        private void Start()
        {
            if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "40_ HIBARICLASS", StringComparison.OrdinalIgnoreCase)) return;
            if (applyMaterials) ApplyClassroomMaterials();
            if (installSeatInteractions) InstallSeats();
            if (installExamStation) InstallExamStation();
            EnsureSchoolOverlay();
            InstallExitSign();
        }

        private void ApplyClassroomMaterials()
        {
            Material wall = CreateMaterial("Hibari Wall", new Color(0.86f, 0.89f, 0.92f));
            Material floor = CreateMaterial("Hibari Floor", new Color(0.18f, 0.23f, 0.29f));
            Material wood = CreateMaterial("Hibari Desk Wood", new Color(0.48f, 0.25f, 0.12f));
            Material chair = CreateMaterial("Hibari Chair", new Color(0.16f, 0.32f, 0.43f));
            Material board = CreateMaterial("Hibari Board", new Color(0.07f, 0.20f, 0.18f));
            Material accent = CreateMaterial("Hibari Accent", new Color(0.93f, 0.67f, 0.25f));

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                string n = renderer.gameObject.name.ToLowerInvariant();
                Material material = null;
                if (n.Contains("floor")) material = floor;
                else if (n.Contains("wall") || n.Contains("header")) material = wall;
                else if (n.Contains("board")) material = board;
                else if (n.Contains("chair")) material = chair;
                else if (n.Contains("desk") || n.Contains("table")) material = wood;
                else if (n.Contains("sign")) material = accent;
                if (material != null) renderer.sharedMaterial = material;
            }
        }

        private void InstallSeats()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.ToLowerInvariant().Contains("chair")) continue;
                if (child.GetComponent<ClassroomSeatInteractable>() != null) continue;

                Bounds bounds = CalculateBounds(child);
                BoxCollider collider = child.GetComponent<BoxCollider>() ?? child.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = child.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(Mathf.Max(0.45f, bounds.size.x), Mathf.Max(0.8f, bounds.size.y), Mathf.Max(0.45f, bounds.size.z));

                ClassroomSeatInteractable seat = child.gameObject.AddComponent<ClassroomSeatInteractable>();
                seat.Configure(child.position + Vector3.up * 0.48f, child.forward);
            }
        }

        private void InstallExamStation()
        {
            Transform board = FindTransform("SchoolFrontHeader") ?? FindTransform("TeacherTable");
            if (board == null || board.GetComponent<ClassroomExamStation>() != null) return;
            BoxCollider collider = board.GetComponent<BoxCollider>() ?? board.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.Max(collider.size, new Vector3(1.4f, 1.2f, 0.25f));
            board.gameObject.AddComponent<ClassroomExamStation>();
        }

        private void InstallExitSign()
        {
            Transform exit = FindTransform("ExitToCity");
            if (exit == null || exit.Find("ExitSignUI") != null) return;
            var canvasObject = new GameObject("ExitSignUI", typeof(Canvas));
            canvasObject.transform.SetParent(exit, false);
            canvasObject.transform.localPosition = Vector3.up * 1.9f;
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.01f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.sizeDelta = new Vector2(420f, 100f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.08f, 0.94f);
            CreateLabel(panel.transform, "EXIT TO CITY", 25f, new Color(0.98f, 0.75f, 0.28f, 1f), new Vector2(-190f, -12f), new Vector2(380f, 38f), TMP_Settings.defaultFontAsset).alignment = TextAlignmentOptions.Center;
            CreateLabel(panel.transform, "Ra phố  •  Nhấn [F] để rời trường", 14f, Color.white, new Vector2(-190f, -48f), new Vector2(380f, 26f), TMP_Settings.defaultFontAsset).alignment = TextAlignmentOptions.Center;
        }

        private Transform FindTransform(string exactName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, exactName, StringComparison.OrdinalIgnoreCase)) return child;
            return null;
        }

        private Bounds CalculateBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(target.position, Vector3.one);
            Bounds result = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
            return result;
        }

        private Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            _runtimeMaterials.Add(material);
            return material;
        }

        private void EnsureSchoolOverlay()
        {
            if (GameObject.Find("HibariSchoolOverlay") != null) return;
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var root = new GameObject("HibariSchoolOverlay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var panel = new GameObject("SchoolIdentity", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(390f, 92f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.08f, 0.11f, 0.94f);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            TextMeshProUGUI title = CreateLabel(panel.transform, "HIBARI JAPANESE SCHOOL", 20f, new Color(0.98f, 0.75f, 0.28f, 1f), new Vector2(20f, -14f), new Vector2(350f, 28f), font);
            title.fontStyle = FontStyles.Bold;
            CreateLabel(panel.transform, "ひばり日本語学院  •  Lớp học đang hoạt động", 13f, new Color(0.73f, 0.86f, 0.9f, 1f), new Vector2(20f, -48f), new Vector2(350f, 22f), font);
            var stripe = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(panel.transform, false);
            RectTransform stripeRect = (RectTransform)stripe.transform;
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 0f);
            stripeRect.pivot = new Vector2(0f, 0f);
            stripeRect.anchoredPosition = new Vector2(0f, 0f);
            stripeRect.sizeDelta = new Vector2(390f, 4f);
            stripe.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.65f, 1f);
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string value, float size, Color color, Vector2 position, Vector2 dimensions, TMP_FontAsset font)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        private void OnDestroy()
        {
            foreach (Material material in _runtimeMaterials) if (material != null) Destroy(material);
        }
    }

    public sealed class ClassroomSeatInteractable : MonoBehaviour, IInteractable
    {
        private Vector3 _seatPosition;
        private Vector3 _forward;
        private CharacterAnimationController _animation;

        public void Configure(Vector3 seatPosition, Vector3 forward)
        {
            _seatPosition = seatPosition;
            _forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (_forward.sqrMagnitude < 0.01f) _forward = Vector3.forward;
        }

        public string GetPromptJa() => "[F] 座る";
        public string GetpromptEn() => "[F] Sit down";
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (player == null) return;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(_seatPosition, Quaternion.LookRotation(-_forward, Vector3.up));
            if (controller != null) controller.enabled = true;
            _animation = player.GetComponent<CharacterAnimationController>() ?? player.GetComponentInChildren<CharacterAnimationController>();
            if (_animation != null && !_animation.SetSitting(true))
                Debug.LogWarning("[HibariClassroom] No Sit animation state found. Add a humanoid Sitting clip to the player Animator.", player);
            PlayerController movement = player.GetComponent<PlayerController>();
            if (movement != null) movement.enabled = false;
            Invoke(nameof(ReleasePlayer), 1.25f);
        }

        private void ReleasePlayer()
        {
            PlayerController movement = FindFirstObjectByType<PlayerController>();
            if (movement != null) movement.enabled = true;
            if (_animation != null) _animation.SetSitting(false);
        }
    }

    public sealed class ClassroomExamStation : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => "[F] 試験センター";
        public string GetpromptEn() => "[F] Open test center";
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) return;
            ExamCenterPopup popup = canvas.GetComponent<ExamCenterPopup>() ?? canvas.gameObject.AddComponent<ExamCenterPopup>();
            if (popup.GetComponent<ExamCenterPopup>() != null)
            {
                popup.Initialize(TMPro.TMP_Settings.defaultFontAsset);
                popup.Show();
            }
        }
    }
}
