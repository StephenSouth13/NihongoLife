using NihongoLife.Core;
using TMPro;
using UnityEngine;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// Shows in the world who F will talk to: a gold ring on the ground under the target and a small plate above it
    /// ("▼ [F] Name", plus "Q 2/4" when several are in reach). Added by InteractionDetector to the player.
    /// </summary>
    public sealed class InteractionHighlight : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);

        private InteractionDetector _detector;
        private Transform _root, _ring;
        private TextMeshPro _label;
        private Material _ringMaterial;
        private float _labelHeight = 2.1f;
        private IInteractable _shown;

        public bool Visible => _root != null && _root.gameObject.activeSelf;
        public string LabelText => _label != null ? _label.text : string.Empty;
        public Vector3 RingPosition => _ring != null ? _ring.position : Vector3.zero;

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
        }

        private void Build()
        {
            _root = new GameObject("InteractionHighlight").transform;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "TargetRing";
            Destroy(quad.GetComponent<Collider>());
            _ring = quad.transform;
            _ring.SetParent(_root, false);
            _ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _ringMaterial = new Material(Shader.Find("Sprites/Default")) { name = "TargetRing", mainTexture = NihongoLife.UI.HudGraphics.Ring.texture, color = Gold };
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _ringMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var labelGo = new GameObject("TargetLabel");
            labelGo.transform.SetParent(_root, false);
            labelGo.transform.localScale = Vector3.one * 0.06f;
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.fontSize = 30f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Gold;
            _label.outlineWidth = 0.25f;
            _label.outlineColor = new Color32(30, 18, 0, 220);
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.rectTransform.sizeDelta = new Vector2(60f, 8f);
        }

        private void LateUpdate()
        {
            var target = _detector != null ? _detector.CurrentInteractable : null;
            if (target is Object unityObject && unityObject == null) target = null;
            bool show = target != null && !NihongoLife.UI.UiModalStack.BlocksHotkeys;
            if (!show)
            {
                if (_root != null && _root.gameObject.activeSelf) _root.gameObject.SetActive(false);
                _shown = null;
                return;
            }
            if (_root == null) Build();
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);

            Transform t = target.GetTransform();
            if (target != _shown)
            {
                _shown = target;
                _labelHeight = TopOf(t) + 0.45f;
            }

            Vector3 basePos = t.position;
            if (Physics.Raycast(basePos + Vector3.up * 0.6f, Vector3.down, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                basePos.y = hit.point.y;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
            _ring.position = basePos + Vector3.up * 0.04f;
            _ring.localScale = new Vector3(1.15f * pulse, 1.15f * pulse, 1f);

            string key = GameInputService.GetOrCreate().GetBindingLabel(GameInputId.Interact);
            string name = NameOf(target);
            int count = _detector.CandidateCount;
            string cycle = count > 1
                ? $"\n<size=70%><color=#E8F1FF>{GameInputService.GetOrCreate().GetBindingLabel(GameInputId.CycleTarget)} đổi · {_detector.CurrentIndex + 1}/{count}</color></size>"
                : string.Empty;
            _label.text = $"▼ <b>[{key}]</b> {name}{cycle}";
            float bob = Mathf.Sin(Time.time * 3f) * 0.05f;
            _label.transform.position = new Vector3(t.position.x, t.position.y + _labelHeight + bob, t.position.z);
            var cam = Camera.main;
            if (cam != null) _label.transform.rotation = Quaternion.LookRotation(_label.transform.position - cam.transform.position);
        }

        private static string NameOf(IInteractable target)
        {
            switch (target)
            {
                case NihongoLife.Player.RemotePlayerAvatar remote: return remote.DisplayName;
                case NihongoLife.NPC.NPCController npc: return npc.DisplayName;
                default: return string.Empty;
            }
        }

        private static float TopOf(Transform t)
        {
            bool any = false;
            var b = new Bounds(t.position, Vector3.zero);
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r.GetComponent<TMP_Text>() != null) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? Mathf.Clamp(b.max.y - t.position.y, 0.4f, 2.8f) : 1.8f;
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_ringMaterial != null) Destroy(_ringMaterial);
        }
    }
}
