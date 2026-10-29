using System;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// A character's skin, top and bottom colours as hex ("#RRGGBB"; empty = the model's own). Applied by
    /// <see cref="CharacterLook"/> through Hidden/NihongoLife/PaletteRecolor when the atlas is baked.
    /// </summary>
    [Serializable]
    public sealed class LookTint
    {
        public string skin, top, bottom;

        public LookTint() { }
        public LookTint(string skin, string top, string bottom) { this.skin = skin; this.top = top; this.bottom = bottom; }

        public bool IsEmpty => string.IsNullOrEmpty(skin) && string.IsNullOrEmpty(top) && string.IsNullOrEmpty(bottom);
        /// <summary>Part of a baked atlas's cache key.</summary>
        public string Key => (skin ?? "") + "/" + (top ?? "") + "/" + (bottom ?? "");
    }

    /// <summary>The flat colour blocks of one Kids-pack model's atlas, as CharacterVariantBuilder measured them
    /// (variant_models.json): the skin, the top (torso and arms) and the bottom (hips and legs). An empty bottom means
    /// the model wears one garment (a dress) and the top colour covers it.</summary>
    [Serializable]
    public sealed class LookPalette
    {
        public string kid, model, skinFrom, topFrom, bottomFrom;
    }

    [Serializable]
    public sealed class LookPalettes
    {
        public string schema;
        public LookPalette[] models = Array.Empty<LookPalette>();
    }

    /// <summary>Loads the per-model palettes (Resources/Characters/Variants/variant_models.json).</summary>
    public static class LookPaletteCatalog
    {
        public const string Path = "Characters/Variants/variant_models";
        private static LookPalettes _all;

        public static LookPalettes All
        {
            get
            {
                if (_all != null) return _all;
                var asset = Resources.Load<TextAsset>(Path);
                _all = asset != null ? JsonUtility.FromJson<LookPalettes>(asset.text) ?? new LookPalettes() : new LookPalettes();
                return _all;
            }
        }

        public static LookPalette For(string model) => All.models.FirstOrDefault(m => m.model == model);
        public static LookPalette ForKid(string kid) => All.models.FirstOrDefault(m => m.kid == kid);
        public static void Reset() => _all = null;
    }
}
