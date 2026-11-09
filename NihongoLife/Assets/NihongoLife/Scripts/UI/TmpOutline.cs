using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// An outline on a label without a material of its own. TMP's outlineWidth / outlineColor setters clone the font material
    /// for every label, and with a fallback font TMP keeps the clones in a static list after the label is gone: every round of
    /// areas left 14 materials behind (ZoneMemoryPlayModeTests), which a phone's browser kept until the tab died.
    /// Labels with the same font material, width and colour share one material. Call it after the label's font is set.
    /// </summary>
    public static class TmpOutline
    {
        private static readonly Dictionary<(int, float, Color32), Material> Shared = new Dictionary<(int, float, Color32), Material>();

        public static void Apply(TMP_Text text, float width, Color32 color)
        {
            Material source = text.font != null && text.font.material != null ? text.font.material : text.fontSharedMaterial;
            if (source == null) return;
            var key = (source.GetInstanceID(), width, color);
            if (!Shared.TryGetValue(key, out Material shared) || shared == null)
            {
                shared = new Material(source) { name = source.name + " Outline" };
                shared.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
                shared.SetColor(ShaderUtilities.ID_OutlineColor, color);
                shared.EnableKeyword(ShaderUtilities.Keyword_Outline); // the full SDF shader needs it; the mobile one always outlines
                Shared[key] = shared;
            }
            if (text.fontSharedMaterial != shared) text.fontSharedMaterial = shared;
        }
    }
}
