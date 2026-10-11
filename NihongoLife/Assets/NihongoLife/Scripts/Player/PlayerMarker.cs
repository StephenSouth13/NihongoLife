using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// "This one is you": a soft ring on the ground under the player, in the selected character's colour. NPCs never
    /// get one (they have name plates instead), so the player stands out even when wearing the same kind of model
    /// as the people around. Added by PlayableCharacterCatalog.ApplyVisual to the player only.
    /// </summary>
    public sealed class PlayerMarker : MonoBehaviour
    {
        private Transform _ring;
        private Material _material;
        private Color _color = Color.white;
        private float _radius = 0.55f;

        public Transform Ring => _ring;
        public Color Color => _color;

        public void Configure(Color color, float radius)
        {
            _color = color;
            _radius = radius;
            if (_ring == null) Build();
            _ring.localScale = new Vector3(_radius * 2f, _radius * 2f, 1f);
            _material.color = new Color(color.r, color.g, color.b, 0.75f);
        }

        private void Build()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "PlayerMarkerRing";
            Destroy(go.GetComponent<Collider>());
            _ring = go.transform;
            _ring.SetParent(transform, false);
            _ring.localPosition = new Vector3(0f, 0.035f, 0f);
            _ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _material = new Material(Shader.Find("Sprites/Default")) { name = "PlayerMarker", mainTexture = NihongoLife.UI.HudGraphics.Ring.texture };
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (_ring == null) return;
            float pulse = 1f + Mathf.Sin(Time.time * 2.4f) * 0.04f;
            _ring.localScale = new Vector3(_radius * 2f * pulse, _radius * 2f * pulse, 1f);
            // Keep it flat on the ground even when the player is mid-jump.
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                _ring.position = hit.point + Vector3.up * 0.035f;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
