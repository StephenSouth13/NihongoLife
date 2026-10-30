using UnityEngine;
using NihongoLife.Home;
using NihongoLife.Learning;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    /// <summary>Wardrobe at the home desk (IMGUI scaffold, no shop scene): buy with yen from PlayerInventory, equip, and the
    /// player's WardrobeLook shows it.</summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private GameObject _player;
        public bool IsOpen { get; private set; }
        public void Open(GameObject player) { _player = player; IsOpen = true; }
        public void Close() => IsOpen = false;

        /// <summary>Buy (spends yen) then equip. Returns false when unaffordable or unknown.</summary>
        public static bool BuyOrEquip(string id, GameObject player)
        {
            var w = StudyProgressStore.Current.wardrobe;
            if (!w.Owns(id))
            {
                var it = WardrobeCatalog.Find(id);
                var inv = PlayerInventory.Instance;
                int yen = inv != null ? inv.Yen : 0;
                if (it == null || w.Buy(id, yen) == 0) return false;
                if (inv != null && it.price > 0) inv.SpendYen(it.price, "wardrobe:" + id);
            }
            bool ok = w.Equip(id);
            StudyProgressStore.Save();
            if (player != null) WardrobeLook.On(player).Apply(w.equipped);
            return ok;
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            var w = StudyProgressStore.Current.wardrobe;
            var items = WardrobeCatalog.Catalog;
            float bw = 520f, h = 80f + items.Length * 40f;
            var r = new Rect((Screen.width - bw) * 0.5f, (Screen.height - h) * 0.5f, bw, h);
            int yen = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            GUI.Box(r, EduUiText.Get("wr.title") + "  -  \u00A5" + yen);
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                bool on = w.equipped.Contains(it.id);
                string label = it.id.Replace('_', ' ') + "  (" + it.slot + ")  " + (w.Owns(it.id) ? (on ? "- " + EduUiText.Get("wr.worn") : "- " + EduUiText.Get("wr.equip")) : "- " + EduUiText.Get("wr.buy") + " \u00A5" + it.price);
                GUI.enabled = !on;
                if (GUI.Button(new Rect(r.x + 20, r.y + 30 + i * 40, bw - 40, 34), label)) BuyOrEquip(it.id, _player);
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(r.xMax - 90, r.yMax - 36, 80, 28), EduUiText.Get("ui.close"))) Close();
        }
    }
}
