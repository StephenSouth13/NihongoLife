using System.Collections;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Health
{
    /// <summary>
    /// How an illness looks on screen: a soft edge glow in the illness's colour that breathes slowly — red with fever,
    /// orange with heatstroke, violet with a headache — stronger while untreated. Its own overlay canvas, never in the
    /// way of clicks; it reads PlayerIllness twice a second, not every frame.
    /// </summary>
    public sealed class IllnessOverlay : MonoBehaviour
    {
        private static IllnessOverlay _instance;
        private RawImage _glow;
        private Color _target = Color.clear;
        private float _nextRead;

        /// <summary>The glow's strength now (tests).</summary>
        public static float Alpha => _instance != null && _instance._glow != null ? _instance._glow.color.a : 0f;

        public static void Ensure()
        {
            if (_instance != null) return;
            var canvas = NLUi.CreateCanvas("IllnessOverlayCanvas", 5);
            DontDestroyOnLoad(canvas.gameObject);
            _instance = canvas.gameObject.AddComponent<IllnessOverlay>();
            var go = new GameObject("Glow", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(canvas.transform, false);
            NLUi.Stretch((RectTransform)go.transform);
            _instance._glow = go.GetComponent<RawImage>();
            _instance._glow.texture = EdgeTexture();
            _instance._glow.raycastTarget = false;
            _instance._glow.color = Color.clear;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextRead)
            {
                _nextRead = Time.unscaledTime + 0.5f;
                var main = PlayerIllness.Instance != null ? PlayerIllness.Main : null;
                if (main == null || main.effect == "sneeze" || main.effect == "ache") _target = Color.clear; // mild: no tint, just the bubbles
                else
                {
                    var record = PlayerIllness.State?.illnesses.Find(r => r.id == main.id);
                    float strength = main.effect == "fever" || main.effect == "heat" ? 0.62f : 0.42f;
                    if (record != null && record.relieved) strength *= 0.5f;
                    _target = new Color(main.Tint.r, main.Tint.g, main.Tint.b, strength);
                }
            }
            float breathe = _target.a > 0f ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 1.6f) : 1f;
            var want = new Color(_target.r, _target.g, _target.b, _target.a * breathe);
            _glow.color = Color.Lerp(_glow.color, want, Time.unscaledDeltaTime * 2f);
        }

        /// <summary>Clear in the middle, coloured toward the edges.</summary>
        private static Texture2D EdgeTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "IllnessEdge" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    byte a = (byte)(Mathf.Clamp01(Mathf.InverseLerp(0.42f, 1.2f, d)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }

    /// <summary>A short line over the character's head (「ハクション！」) for a few seconds — its own child object.</summary>
    public static class SymptomBubble
    {
        public static void Show(Transform actor, string text, Color tint, float seconds = 2.6f)
        {
            if (actor == null) return;
            var existing = actor.Find("SymptomBubble");
            TextMeshPro label;
            if (existing != null) label = existing.GetComponent<TextMeshPro>();
            else
            {
                var go = new GameObject("SymptomBubble");
                go.transform.SetParent(actor, false);
                go.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                label = go.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 1.6f;
                label.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
                label.outlineWidth = 0.25f;
                var font = NLUi.ResolveFont();
                if (font != null) label.font = font;
                go.AddComponent<BillboardUI>();
            }
            if (label == null) return;
            label.text = text;
            label.color = Color.Lerp(tint, Color.white, 0.55f);
            label.outlineColor = new Color32(30, 30, 40, 255);
            label.gameObject.SetActive(true);
            var host = actor.GetComponent<MonoBehaviour>();
            if (host != null && host.isActiveAndEnabled) host.StartCoroutine(Hide(label, seconds));
        }

        private static IEnumerator Hide(TextMeshPro label, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (label != null) label.gameObject.SetActive(false);
        }
    }
}
