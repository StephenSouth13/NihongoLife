using TMPro;
using UnityEngine;

namespace NihongoLife.Interaction
{
    /// <summary>Emote icon + Japanese phrase that pops above a character, floats up and fades out.</summary>
    public sealed class EmoteBubble : MonoBehaviour
    {
        private const float Lifetime = 2.6f;
        private SpriteRenderer _icon;
        private TextMeshPro _text;
        private Transform _anchor;
        private float _age;
        private float _height;

        public static EmoteBubble Spawn(Transform anchor, EmoteDefinition emote)
        {
            foreach (var old in anchor.GetComponentsInChildren<EmoteBubble>()) Destroy(old.gameObject);
            var go = new GameObject("EmoteBubble_" + emote.Id);
            go.transform.SetParent(anchor, false);
            var bubble = go.AddComponent<EmoteBubble>();
            bubble._anchor = anchor;
            bubble._height = 2.25f;

            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(go.transform, false);
            bubble._icon = iconObject.AddComponent<SpriteRenderer>();
            bubble._icon.sprite = EmoteCatalog.Icon(emote.Id);
            iconObject.transform.localScale = Vector3.one * 0.7f;

            var textObject = new GameObject("Phrase");
            textObject.transform.SetParent(go.transform, false);
            textObject.transform.localPosition = Vector3.down * 0.52f;
            bubble._text = textObject.AddComponent<TextMeshPro>();
            bubble._text.text = $"<mark=#0B0F16CC padding=\"30,30,10,10\">{emote.Ja}</mark>";
            bubble._text.fontSize = 2.2f;
            bubble._text.alignment = TextAlignmentOptions.Center;
            bubble._text.textWrappingMode = TextWrappingModes.NoWrap;
            bubble._text.rectTransform.sizeDelta = new Vector2(4f, 0.6f);
            bubble._text.color = Color.white;
            bubble._text.outlineWidth = 0.15f;
            bubble._text.outlineColor = new Color32(8, 10, 16, 255);
            go.transform.localPosition = Vector3.up * bubble._height;
            return bubble;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            float pop = _age < 0.18f ? Mathf.SmoothStep(0.3f, 1.12f, _age / 0.18f) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((_age - 0.18f) / 0.15f));
            transform.localScale = Vector3.one * pop;
            transform.position = _anchor.position + Vector3.up * (_height + _age * 0.18f);
            var camera = Camera.main;
            if (camera != null) transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
            float alpha = Mathf.Clamp01((Lifetime - _age) / 0.6f);
            if (_icon != null) _icon.color = new Color(1f, 1f, 1f, alpha);
            if (_text != null) _text.alpha = alpha;
            if (_age >= Lifetime) Destroy(gameObject);
        }
    }
}
