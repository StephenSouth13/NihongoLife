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
            label.fontSize = 0.22f; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
            _labels[npc] = label;
        }
    }
}
