using TMPro;
using UnityEngine;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// Floating sign + bobbing arrow + warm light over every ScenePortal so the player can see where a
    /// door leads ("駅前 / Khu nhà ga · [F] Đi vào", "出口 · Ra phố"). Fades in within range and always
    /// faces the camera. Uses only TextMeshPro and a Light, so it needs no extra material assets.
    /// </summary>
    public sealed class PortalBeacon : MonoBehaviour
    {
        private const float VisibleRange = 22f;
        private const float HideWithin = 3.5f; // close up, the HUD prompt takes over
        private Transform _player;
        private TextMeshPro _sign;
        private TextMeshPro _arrow;
        private Light _light;
        private float _baseArrowY;
        private float _alpha;

        public static void Attach(ScenePortal portal, string title, string action, bool exit)
        {
            if (portal == null || portal.GetComponentInChildren<PortalBeacon>(true) != null) return;
            var root = new GameObject("PortalBeacon");
            root.transform.SetParent(portal.transform, false);
            var collider = portal.GetComponent<Collider>();
            float top = collider != null ? collider.bounds.max.y - portal.transform.position.y : 1.2f;
            root.transform.localPosition = Vector3.zero;
            // Entrances: hang the sign just outside the doorway (the player approaches from -forward) and
            // under typical canopies; exits stay inside the room, below the ceiling.
            Vector3 position = portal.transform.position;
            position.y = portal.transform.position.y - (collider != null ? collider.bounds.extents.y : 1f) + (exit ? 1.95f : 2.05f);
            if (!exit) position -= Vector3.ProjectOnPlane(portal.transform.forward, Vector3.up).normalized * 1.4f;
            root.transform.position = position;
            root.AddComponent<PortalBeacon>().Build(title, action, exit);
        }

        private void Build(string title, string action, bool exit)
        {
            TMP_FontAsset font = null; // TMP default font; NotoSansJP is its project-wide fallback
            string accent = exit ? "#7FD3FF" : "#F2B233";
            _sign = CreateText("Sign", font, 3.2f,
                $"<mark=#0B0F16D9 padding=\"40,40,18,18\"><b>{title}</b>\n<size=70%><color={accent}>[F]</color> {action}</size></mark>");
            _arrow = CreateText("Arrow", font, 4.2f, $"<color={accent}>▼</color>");
            _arrow.transform.localPosition = Vector3.down * 0.7f;
            _baseArrowY = _arrow.transform.localPosition.y;

            var lightObject = new GameObject("BeaconLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.down * 1.2f;
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 4.5f;
            _light.intensity = 0f;
            _light.color = exit ? new Color(0.55f, 0.82f, 1f) : new Color(1f, 0.8f, 0.45f);
            _light.shadows = LightShadows.None;
            SetAlpha(0f);
        }

        private TextMeshPro CreateText(string name, TMP_FontAsset font, float size, string value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(10f, 2.5f);
            text.color = Color.white;
            text.outlineWidth = 0.22f;
            text.outlineColor = new Color32(8, 10, 16, 255);
            return text;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null) return;
            if (_player == null)
            {
                var player = FindFirstObjectByType<NihongoLife.Player.PlayerController>();
                if (player != null) _player = player.transform;
            }
            Vector3 viewer = _player != null ? _player.position : camera.transform.position;
            float distance = Vector3.Distance(new Vector3(viewer.x, 0f, viewer.z), new Vector3(transform.position.x, 0f, transform.position.z));
            float target = distance < VisibleRange && distance > HideWithin ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime * 3f);
            SetAlpha(_alpha);
            if (_alpha <= 0f) return;
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
            var p = _arrow.transform.localPosition;
            p.y = _baseArrowY + Mathf.Sin(Time.time * 3f) * 0.12f;
            _arrow.transform.localPosition = p;
        }

        private void SetAlpha(float alpha)
        {
            if (_sign != null) _sign.alpha = alpha;
            if (_arrow != null) _arrow.alpha = alpha;
            if (_light != null) _light.intensity = 1.6f * alpha;
            bool visible = alpha > 0.01f;
            if (_sign != null && _sign.enabled != visible) _sign.enabled = visible;
            if (_arrow != null && _arrow.enabled != visible) _arrow.enabled = visible;
        }
    }
}
