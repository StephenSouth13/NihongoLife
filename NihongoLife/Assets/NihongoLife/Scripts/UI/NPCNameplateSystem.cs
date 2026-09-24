using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NihongoLife.NPC;

namespace NihongoLife.UI
{
    public sealed class NPCNameplateSystem : MonoBehaviour
    {
        public static NPCNameplateSystem Instance { get; private set; }
        public bool Visible { get; private set; } = true;
        private readonly Dictionary<NPCController, TextMeshPro> _labels = new Dictionary<NPCController, TextMeshPro>();

        private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

        private void Update()
        {
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
            {
                if (!_labels.ContainsKey(npc)) CreateLabel(npc);
            }
            foreach (var pair in _labels)
            {
                if (pair.Key == null || pair.Value == null) continue;
                pair.Value.gameObject.SetActive(Visible);
                var camera = Camera.main;
                if (camera != null) pair.Value.transform.rotation = Quaternion.LookRotation(pair.Value.transform.position - camera.transform.position);
            }
        }

        public void Toggle() => SetVisible(!Visible);
        public void SetVisible(bool visible) { Visible = visible; }

        private void CreateLabel(NPCController npc)
        {
            var objectLabel = new GameObject("NPCNameplate");
            objectLabel.transform.SetParent(npc.transform, false);
            objectLabel.transform.localPosition = Vector3.up * 2.25f;
            var label = objectLabel.AddComponent<TextMeshPro>();
            string name = string.IsNullOrWhiteSpace(npc.DisplayName) ? npc.name : npc.DisplayName;
            label.text = string.IsNullOrWhiteSpace(npc.Role) ? name : $"{name}\n<size=70%>{npc.Role}</size>";
            label.fontSize = 0.22f; label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold; label.outlineWidth = 0.24f;
            label.outlineColor = new Color(0.01f, 0.025f, 0.04f, 0.95f);
            label.color = RoleColor(npc.Role);
            _labels[npc] = label;
        }

        private static Color RoleColor(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return new Color(0.82f, 0.93f, 1f);
            string normalized = role.ToLowerInvariant();
            if (normalized.Contains("teacher") || normalized.Contains("giáo") || normalized.Contains("sensei"))
                return new Color(1f, 0.78f, 0.28f);
            if (normalized.Contains("station") || normalized.Contains("ga") || normalized.Contains("cashier"))
                return new Color(0.35f, 0.9f, 0.86f);
            return new Color(0.92f, 0.82f, 1f);
        }
    }
}
