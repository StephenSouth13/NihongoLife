using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Picture icons for items (Resources/Items/&lt;itemId&gt;.png, rendered from the Kenney food models by
    /// ItemIconRenderer, or drawn for tickets/prizes). <see cref="Apply"/> turns a glyph tile into a picture
    /// tile: the image fills the tile and the kanji glyph shrinks to a small corner chip. Items without a
    /// picture keep the glyph.
    /// </summary>
    public static class ItemIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new();
        private static readonly Dictionary<int, (float size, Vector2 min, Vector2 max)> GlyphDefaults = new();
        private static readonly Dictionary<string, string> Aliases = new()
        {
            ["onigiri"] = "onigiri_sake",
        };

        public static Sprite Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (Aliases.TryGetValue(itemId, out string alias)) itemId = alias;
            if (Cache.TryGetValue(itemId, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>("Items/" + itemId);
            Sprite sprite = null;
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = itemId;
            }
            Cache[itemId] = sprite;
            return sprite;
        }

        /// <summary>Shows the item's picture on the tile that owns <paramref name="glyph"/>; returns true when one exists.</summary>
        public static bool Apply(TextMeshProUGUI glyph, string itemId, float inset = 6f)
        {
            if (glyph == null) return false;
            var tile = glyph.transform.parent as RectTransform;
            var icon = tile != null ? tile.Find("ItemIcon") as RectTransform : null;
            var sprite = Get(itemId);
            if (icon == null && sprite != null)
            {
                icon = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
                icon.SetParent(tile, false);
                icon.GetComponent<LayoutElement>().ignoreLayout = true;
                icon.anchorMin = Vector2.zero;
                icon.anchorMax = Vector2.one;
                icon.offsetMin = new Vector2(inset, inset);
                icon.offsetMax = new Vector2(-inset, -inset);
                var image = icon.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                icon.SetSiblingIndex(0);
                glyph.transform.SetAsLastSibling();
            }
            bool hasPicture = sprite != null;
            if (icon != null)
            {
                icon.gameObject.SetActive(hasPicture);
                if (hasPicture) icon.GetComponent<Image>().sprite = sprite;
            }
            var chip = glyph.rectTransform;
            if (!glyph.TryGetComponent(out LayoutElement element)) element = glyph.gameObject.AddComponent<LayoutElement>();
            int key = glyph.GetInstanceID();
            if (!GlyphDefaults.ContainsKey(key)) GlyphDefaults[key] = (glyph.fontSize, chip.anchorMin, chip.anchorMax);
            if (!hasPicture)
            {
                var d = GlyphDefaults[key];
                element.ignoreLayout = false;
                glyph.enableAutoSizing = false;
                glyph.fontSize = d.size;
                chip.anchorMin = d.min;
                chip.anchorMax = d.max;
                chip.pivot = new Vector2(0.5f, 0.5f);
                chip.anchoredPosition = Vector2.zero;
                chip.sizeDelta = Vector2.zero;
                glyph.color = new Color(0.15f, 0.12f, 0.1f);
                return false;
            }
            if (hasPicture)
            {
                // Glyph becomes a small kanji chip in the corner (e.g. 梅 / 鮭 on the rice balls).
                element.ignoreLayout = true;
                chip.anchorMin = chip.anchorMax = new Vector2(1f, 0f);
                chip.pivot = new Vector2(1f, 0f);
                chip.anchoredPosition = new Vector2(-4f, 4f);
                chip.sizeDelta = new Vector2(Mathf.Max(28f, tile.rect.height * 0.32f), Mathf.Max(28f, tile.rect.height * 0.32f));
                glyph.enableAutoSizing = true;
                glyph.fontSizeMin = 10f;
                glyph.fontSizeMax = 26f;
                glyph.alignment = TextAlignmentOptions.Center;
                glyph.color = new Color(0.55f, 0.18f, 0.1f);
            }
            return hasPicture;
        }
    }
}
