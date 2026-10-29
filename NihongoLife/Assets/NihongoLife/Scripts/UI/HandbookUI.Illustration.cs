using NihongoLife.Content;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    public sealed partial class HandbookUI
    {
        /// <summary>Dictionary page: the companion picture once unlocked, else a dark silhouette with a hint.</summary>
        private void IllustrationRow(RectTransform parent, string conceptId, Color color)
        {
            if (!IllustrationService.Has(conceptId)) return;
            bool open = IllustrationService.IsUnlocked(conceptId);
            var sprite = IllustrationService.GetSprite(conceptId);
            if (sprite == null) return;
            var row = NLUi.Group(parent, "Illustration", false, 14f, TextAnchor.MiddleLeft, false);
            var go = new GameObject(open ? "Picture" : "Silhouette", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(row, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
            img.color = open ? Color.white : new Color(0.08f, 0.08f, 0.1f, 0.85f);
            NLUi.Size((RectTransform)go.transform, 84f, 84f);
            string caption = open
                ? Loc.T("H\u00ECnh minh h\u1ECDa (m\u1EDF kh\u00F3a khi tr\u1EA3 l\u1EDDi \u0111\u00FAng)", "Picture (unlocked by a correct answer)", "\u30a4\u30e9\u30b9\u30c8\uff08\u6b63\u89e3\u3067\u89e3\u653e\uff09")
                : Loc.T("Tr\u1EA3 l\u1EDDi \u0111\u00FAng m\u1ED9t c\u00E2u h\u1ECFi v\u1EC1 t\u1EEB n\u00E0y \u0111\u1EC3 m\u1EDF kh\u00F3a h\u00ECnh minh h\u1ECDa.", "Answer a question about this word correctly to unlock its picture.", "\u3053\u306e\u8a00\u8449\u306e\u554f\u984c\u306b\u6b63\u89e3\u3059\u308b\u3068\u30a4\u30e9\u30b9\u30c8\u304c\u89e3\u653e\u3055\u308c\u307e\u3059\u3002");
            var lbl = NLUi.Label(row, "Caption", caption, 14f, InkSoft, _font);
            NLUi.Size(lbl.rectTransform, 340f);
        }
    }
}