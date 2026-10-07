using UnityEngine;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.UI;

namespace NihongoLife.Shop
{
    /// <summary>A shelf section in Hibari Mart. F opens the product browser for that section.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KonbiniShelf : MonoBehaviour, IInteractable
    {
        [SerializeField] private KonbiniSection section = KonbiniSection.Onigiri;

        public KonbiniSection Section => section;
        public void Configure(KonbiniSection value) => section = value;
        public string GetPromptJa() => KonbiniCatalog.SectionJa(section) + "を みる";
        public string GetpromptEn() => "Xem kệ " + KonbiniCatalog.SectionVi(section);
        public Transform GetTransform() => transform;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        public void Interact(GameObject player) => KonbiniShopUI.GetOrCreate().OpenSection(section);
    }
}
