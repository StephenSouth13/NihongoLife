using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// J2: a HUD panel the player can drag around (position saved in PlayerPrefs, kept inside the screen), with an optional
    /// collapse button (state saved) and a reset (right-click the panel, or <see cref="ResetAll"/>).
    /// </summary>
    public sealed class DraggableHud : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private const string PosPrefix = "hud_pos_", CollapsePrefix = "hud_collapsed_";
        private static readonly List<DraggableHud> All = new List<DraggableHud>();

        private RectTransform _rt;
        private string _key;
        private Vector2 _defaultPos;
        private Canvas _canvas;
        private GameObject[] _collapsible = Array.Empty<GameObject>();
        private TextMeshProUGUI _toggleLabel;

        public string Key => _key;
        public bool Collapsed { get; private set; }
        public bool Moved => ((Vector2)_rt.anchoredPosition - _defaultPos).sqrMagnitude > 0.25f;
        public Vector2 DefaultPosition => _defaultPos;

        public static DraggableHud Attach(RectTransform panel, string key)
        {
            var d = panel.GetComponent<DraggableHud>();
            if (d == null) d = panel.gameObject.AddComponent<DraggableHud>();
            d._rt = panel; d._key = key; d._defaultPos = panel.anchoredPosition;
            if (!All.Contains(d)) All.Add(d);
            var image = panel.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
            d.LoadPosition();
            return d;
        }

        private void OnDestroy() { All.Remove(this); }

        /// <summary>Adds a small collapse button (top-right of the panel) that hides <paramref name="hidden"/> when collapsed.</summary>
        public void AddCollapseButton(TMP_FontAsset font, params GameObject[] hidden)
        {
            _collapsible = hidden ?? Array.Empty<GameObject>();
            var go = new GameObject("CollapseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(_rt, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var img = go.GetComponent<Image>(); img.color = new Color(1f, 1f, 1f, 0.12f);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(-6f, -6f); r.sizeDelta = new Vector2(26f, 22f);
            var lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            lbl.transform.SetParent(go.transform, false);
            lbl.font = font; lbl.fontSize = 16f; lbl.alignment = TextAlignmentOptions.Center; lbl.color = Color.white; lbl.raycastTarget = false;
            var lr = lbl.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
            _toggleLabel = lbl;
            go.GetComponent<Button>().onClick.AddListener(() => SetCollapsed(!Collapsed));
            Collapsed = PlayerPrefs.GetInt(CollapsePrefix + _key, 0) == 1;
            ApplyCollapsed();
        }

        public void SetCollapsed(bool value)
        {
            Collapsed = value;
            PlayerPrefs.SetInt(CollapsePrefix + _key, value ? 1 : 0);
            ApplyCollapsed();
        }

        private void ApplyCollapsed()
        {
            foreach (var g in _collapsible) if (g != null) g.SetActive(!Collapsed);
            if (_toggleLabel != null) _toggleLabel.text = Collapsed ? "+" : "-";
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rt);
        }

        private void LoadPosition()
        {
            string s = PlayerPrefs.GetString(PosPrefix + _key, "");
            var p = s.Split(',');
            if (p.Length == 2 && float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                _rt.anchoredPosition = new Vector2(x, y);
        }

        private void SavePosition() => PlayerPrefs.SetString(PosPrefix + _key, _rt.anchoredPosition.x.ToString("F1", CultureInfo.InvariantCulture) + "," + _rt.anchoredPosition.y.ToString("F1", CultureInfo.InvariantCulture));

        /// <summary>Back to the default spot; the saved position is forgotten.</summary>
        public void ResetPosition()
        {
            _rt.anchoredPosition = _defaultPos;
            PlayerPrefs.DeleteKey(PosPrefix + _key);
        }

        public static void ResetAll() { foreach (var d in All.ToArray()) if (d != null) { d.ResetPosition(); d.SetCollapsed(false); } }

        public void MoveBy(Vector2 delta)
        {
            _rt.anchoredPosition += delta;
            Clamp();
        }

        private void Clamp()
        {
            var parent = _rt.parent as RectTransform;
            if (parent == null) return;
            Rect pr = parent.rect, sr = _rt.rect;
            Vector2 anchor = new Vector2(Mathf.Lerp(pr.xMin, pr.xMax, _rt.anchorMin.x), Mathf.Lerp(pr.yMin, pr.yMax, _rt.anchorMin.y));
            Vector2 pos = _rt.anchoredPosition;
            float left = anchor.x + pos.x - _rt.pivot.x * sr.width, bottom = anchor.y + pos.y - _rt.pivot.y * sr.height;
            float dx = 0f, dy = 0f;
            if (left < pr.xMin) dx = pr.xMin - left; else if (left + sr.width > pr.xMax) dx = pr.xMax - (left + sr.width);
            if (bottom < pr.yMin) dy = pr.yMin - bottom; else if (bottom + sr.height > pr.yMax) dy = pr.yMax - (bottom + sr.height);
            _rt.anchoredPosition = pos + new Vector2(dx, dy);
        }

        public void OnBeginDrag(PointerEventData e) { _canvas = GetComponentInParent<Canvas>(); }
        public void OnDrag(PointerEventData e)
        {
            float scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1f;
            MoveBy(e.delta / scale);
        }
        public void OnEndDrag(PointerEventData e) { Clamp(); SavePosition(); }
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Right) ResetPosition(); }
        /// <summary>Test hook: drop the panel at a position and save it like a finished drag.</summary>
        public void DropAt(Vector2 anchored) { _rt.anchoredPosition = anchored; Clamp(); SavePosition(); }
    }
}