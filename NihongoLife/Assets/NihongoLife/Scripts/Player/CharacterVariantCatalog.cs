using System;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Player
{
    // ----------- Data: Resources/Characters/variants.json (written by hand, built into prefabs by CharacterVariantBuilder) -----------

    /// <summary>One prop of a uniform (a hat, a tie, an apron ...), a primitive shape on a named anchor of the body.
    /// Positions and sizes are in "head widths" of the model, so the same piece fits every Kids model.</summary>
    [Serializable]
    public sealed class VariantPiece
    {
        public string name, shape = "cube", anchor = "head", hex = "#FFFFFF";
        public float[] pos = new float[3], rot = new float[3], size = { 1f, 1f, 1f };
    }

    /// <summary>A job outfit: the top and bottom colours of the atlas plus the props on the body.</summary>
    [Serializable]
    public sealed class VariantUniform
    {
        public string id, nameVi, nameEn, nameJa, topHex, bottomHex;
        public VariantPiece[] pieces = Array.Empty<VariantPiece>();
        public string Name => Loc.Pick(nameVi, nameEn, nameJa);
    }

    /// <summary>One distinct person of the town: a Kids-pack model with its own skin tone, hair, age, build and clothes.</summary>
    [Serializable]
    public sealed class CharacterVariantSpec
    {
        /// <summary>The Kids prefab it derives from (kid_haruto ...).</summary>
        public string id, kid, uniform, hair = "natural", beard = "none", skinHex, topHex, bottomHex, person, build, ageGroup;
        public int age = 30;
        /// <summary>Body proportions relative to the Kids prefab: height and girth (1 = as the prefab).</summary>
        public float height = 1f, girth = 1f;

        public string PrefabPath => CharacterVariants.PrefabFolder + "NL_var_" + id;

        public string TopOf(CharacterVariantFile file) => !string.IsNullOrEmpty(topHex) ? topHex : file.Uniform(uniform)?.topHex;
        public string BottomOf(CharacterVariantFile file) => !string.IsNullOrEmpty(bottomHex) ? bottomHex : file.Uniform(uniform)?.bottomHex;
        public LookTint TintOf(CharacterVariantFile file) => new LookTint(skinHex, TopOf(file), BottomOf(file));
    }

    [Serializable]
    public sealed class CharacterVariantFile
    {
        public string schema;
        public VariantUniform[] uniforms = Array.Empty<VariantUniform>();
        public CharacterVariantSpec[] variants = Array.Empty<CharacterVariantSpec>();

        public VariantUniform Uniform(string id) => string.IsNullOrEmpty(id) ? null : uniforms.FirstOrDefault(u => u.id == id);
        public CharacterVariantSpec Find(string id) => variants.FirstOrDefault(v => v.id == id);
    }

    /// <summary>Loads the variants and spawns a ready-made person (prefab NL_var_&lt;id&gt; built by CharacterVariantBuilder).</summary>
    public static class CharacterVariants
    {
        public const string Path = "Characters/variants";
        public const string PrefabFolder = "Characters/Variants/";
        private static CharacterVariantFile _file;

        public static CharacterVariantFile File
        {
            get
            {
                if (_file != null) return _file;
                var asset = Resources.Load<TextAsset>(Path);
                _file = asset != null ? JsonUtility.FromJson<CharacterVariantFile>(asset.text) ?? new CharacterVariantFile() : new CharacterVariantFile();
                return _file;
            }
        }

        public static CharacterVariantSpec Find(string id) => File.Find(id);
        public static void Reset() => _file = null;

        public static GameObject Prefab(string id) => Resources.Load<GameObject>(PrefabFolder + "NL_var_" + id);

        /// <summary>Spawns variant <paramref name="id"/> standing at <paramref name="position"/>; null when it is unknown.</summary>
        public static GameObject Spawn(string id, Vector3 position, float yaw = 0f, Transform parent = null)
        {
            var prefab = Prefab(id);
            if (prefab == null) return null;
            var instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), parent);
            instance.name = "Person_" + id;
            return instance;
        }
    }
}
