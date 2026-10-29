using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// Put on a variant prefab (NL_var_*): gives the Kids model its person's hair, beard, age and skin / top / bottom
    /// colours through <see cref="CharacterLook"/> (the atlas is re-baked once per model and colours, shared by every clone).
    /// The proportions and the uniform's props are already in the prefab.
    /// </summary>
    public sealed class CharacterVariant : MonoBehaviour
    {
        [SerializeField] private string variantId;

        public string VariantId => variantId;
        public CharacterVariantSpec Spec => CharacterVariants.Find(variantId);

        private void OnEnable() => Apply();

        /// <summary>Applies the look of <see cref="VariantId"/>.</summary>
        public void Apply()
        {
            var spec = Spec;
            if (spec == null) { Debug.LogWarning("[CharacterVariant] Unknown variant '" + variantId + "' on " + name, this); return; }
            CharacterLook.On(gameObject, false).Fixed(spec.hair, spec.beard, spec.age, spec.TintOf(CharacterVariants.File));
        }

        /// <summary>Becomes another person (tests, the prefab builder).</summary>
        public void Set(string id) { variantId = id; Apply(); }
    }
}
