using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Fashion;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Hibari Mode's clothes window and the player's wardrobe. Docked on the right without dimming: the camera comes
    /// round to the front of the character, and whatever is picked in the list is worn at once as a try-on. Closing
    /// without buying goes back to the outfit in the save.
    /// </summary>
    public sealed class FashionShopUI : MonoBehaviour
    {
        /// <summary>The tab of the player's own clothes.</summary>
        public const string Wardrobe = "";

        private static FashionShopUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _body;
        private string _category = Wardrobe, _message;
        private ThirdPersonCameraController _camera;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeInHierarchy;
        public static string Category => _instance != null ? _instance._category : Wardrobe;

        private static FashionShopUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("FashionShopCanvas", 140);
                if (Application.isPlaying) DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.Need<FashionShopUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Rebuild(); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "FashionShop");
                FashionService.Changed += () => { if (IsOpen) _instance.Rebuild(); };
                return _instance;
            }
        }

        public static void Open(string category = Wardrobe)
        {
            var ui = Instance;
            ui._category = category ?? Wardrobe;
            ui._message = null;
            ui._root.gameObject.SetActive(true);
            ui._root.SetAsLastSibling();
            ui.FacePlayer();
            ui.Rebuild();
        }

        public static void Close()
        {
            if (_instance == null || _instance._root == null || !IsOpen) return;
            _instance._root.gameObject.SetActive(false);
            FashionService.EndTryOn();
            if (_instance._camera != null) _instance._camera.PopOrbit();
            _instance._camera = null;
        }

        /// <summary>The camera comes round to the character's front, a little to the side the window leaves free.</summary>
        private void FacePlayer()
        {
            var player = SceneRefs.Find<PlayerController>();
            _camera = SceneRefs.Find<ThirdPersonCameraController>();
            if (player == null || _camera == null) return;
            _camera.PushOrbit(player.transform.eulerAngles.y + 165f, 6f, 2.9f);
        }

        private void Build(RectTransform canvas)
        {
            _root = NLUi.Panel(canvas, "FashionShop", NLUi.Ink);
            _root.anchorMin = new Vector2(1f, 0.5f); _root.anchorMax = new Vector2(1f, 0.5f); _root.pivot = new Vector2(1f, 0.5f);
            _root.anchoredPosition = new Vector2(-24f, 0f);
            _root.sizeDelta = new Vector2(560f, 800f);
            _root.gameObject.Need<HudFitRect>().Configure(0.48f, 0.94f, new Vector2(-24f, 0f), 0.4f);
            var scrollRoot = UiBind.Make("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollRoot.SetParent(_root, false);
            NLUi.Stretch(scrollRoot);
            scrollRoot.offsetMin = new Vector2(22f, 18f); scrollRoot.offsetMax = new Vector2(-12f, -18f);
            _body = UiKit.Scroll(scrollRoot, "Content", out var scroll, 10f, new RectOffset(2, 14, 2, 12));
            NLUi.Stretch((RectTransform)scroll.transform);
            NLUi.CloseButton(_root, _font, Close, 42f, 12f);
            _root.gameObject.SetActive(false);
        }

        private void Rebuild()
        {
            UiKit.Clear(_body);
            var catalog = FashionCatalog.Load();
            bool wardrobe = string.IsNullOrEmpty(_category);
            NLUi.Label(_body, "Title", wardrobe ? Loc.T("Tủ đồ của bạn", "Your wardrobe", "あなたの服") : "Hibari Mode", 28f, NLUi.Text, _font, FontStyles.Bold);
            int yen = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            NLUi.Label(_body, "Wallet", Loc.F("Trong ví: {0} · bấm một bộ để mặc thử", "In your wallet: {0} · pick a set to try it on", "さいふ：{0} · 服をえらぶと試着できます", Loc.Yen(yen)), 15f, NLUi.Muted, _font);
            if (!string.IsNullOrEmpty(_message)) NLUi.Label(_body, "Message", _message, 15f, NLUi.Bad, _font);

            var tabs = UiBind.Make("Tabs", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            tabs.SetParent(_body, false);
            var layout = tabs.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(120f, 40f); layout.spacing = new Vector2(6f, 6f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 4;
            NLUi.FitContent(tabs);
            Tab(tabs, Wardrobe, Loc.T("Của tôi", "Mine", "自分の服"));
            foreach (var category in catalog.categories) Tab(tabs, category.id, category.Name);

            if (wardrobe)
            {
                Own();
                foreach (var outfit in catalog.outfits.Where(o => FashionService.Owns(o.id))) Row(outfit);
                if (!catalog.outfits.Any(o => FashionService.Owns(o.id)))
                    NLUi.Label(_body, "Empty", Loc.T("Chưa có bộ nào. Đến Hibari Mode ở phố chính để mua đồ.", "Nothing yet. Visit Hibari Mode on the main street.", "まだ服がありません。商店街のひばりモードへどうぞ。"), 15f, NLUi.Muted, _font);
            }
            else foreach (var outfit in catalog.InCategory(_category)) Row(outfit);

            var done = NLUi.Button(_body, "Done", Loc.T("Xong", "Done", "完了"), _font, Close, NLUi.Gold, 17f, NLUi.Ink, 46f);
            NLUi.Size(done, -1f, 46f);
        }

        private void Tab(RectTransform parent, string id, string label)
        {
            bool on = _category == id;
            NLUi.Button(parent, "Tab_" + (id == Wardrobe ? "mine" : id), label, _font, () => { _category = id; _message = null; Rebuild(); }, on ? NLUi.Gold : NLUi.Card, 14f, on ? NLUi.Ink : NLUi.Text, 40f);
        }

        /// <summary>The character's own clothes are always in the wardrobe.</summary>
        private void Own()
        {
            bool worn = string.IsNullOrEmpty(FashionService.WornId);
            var row = NLUi.Panel(_body, "Outfit_own", string.IsNullOrEmpty(FashionService.ShownId) ? new Color(1f, 1f, 1f, 0.14f) : NLUi.Card, new RectOffset(12, 12, 8, 8), 10f, vertical: false);
            NLUi.Size(row, -1f, 62f);
            var name = NLUi.Label(row, "Name", Loc.T("Đồ của nhân vật", "The character's own clothes", "もとの服"), 16f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Size(name, -1f, -1f, 1f);
            NLUi.Button(row, "Try", Loc.T("Mặc thử", "Try on", "試着"), _font, () => { FashionService.TryOn(string.Empty); Rebuild(); }, NLUi.CardHover, 14f, NLUi.Text, 40f);
            if (worn) NLUi.Pill(row, "Worn", Loc.T("Đang mặc", "Wearing", "着ています"), _font, NLUi.Gold, NLUi.Ink, 14f);
            else NLUi.Button(row, "Wear", Loc.T("Mặc", "Wear", "着る"), _font, () => FashionService.Wear(string.Empty), NLUi.Gold, 14f, NLUi.Ink, 40f);
        }

        private void Row(FashionOutfit outfit)
        {
            string id = outfit.id;
            bool shown = FashionService.ShownId == id, worn = FashionService.WornId == id, owned = FashionService.Owns(id);
            var row = NLUi.Panel(_body, "Outfit_" + id, shown ? new Color(1f, 1f, 1f, 0.14f) : NLUi.Card, new RectOffset(12, 12, 8, 8), 10f, vertical: false);
            NLUi.Size(row, -1f, 70f);
            var swatch = NLUi.Group(row, "Swatch", true, 0f, TextAnchor.MiddleCenter, false);
            NLUi.Size(swatch, 34f, 50f);
            foreach (var hex in new[] { outfit.top, outfit.bottom })
            {
                var block = UiBind.Make("Block", typeof(RectTransform), typeof(Image));
                block.transform.SetParent(swatch, false);
                block.GetComponent<Image>().color = CharacterLook.Hex(hex);
                NLUi.Size(block.transform, 34f, 25f);
            }
            var text = NLUi.Group(row, "Text", true, 2f);
            NLUi.Size(text, -1f, -1f, 1f);
            NLUi.Label(text, "Name", outfit.Name, 16f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Label(text, "Price", owned ? Loc.T("Đã có trong tủ", "In your wardrobe", "持っています") : Loc.Yen(outfit.price), 14f, owned ? NLUi.Good : NLUi.Gold, _font);
            NLUi.Button(row, "Try", Loc.T("Mặc thử", "Try on", "試着"), _font, () => Pick(id), NLUi.CardHover, 14f, NLUi.Text, 40f);
            if (worn) NLUi.Pill(row, "Worn", Loc.T("Đang mặc", "Wearing", "着ています"), _font, NLUi.Gold, NLUi.Ink, 14f);
            else if (owned) NLUi.Button(row, "Wear", Loc.T("Mặc", "Wear", "着る"), _font, () => FashionService.Wear(id), NLUi.Gold, 14f, NLUi.Ink, 40f);
            else NLUi.Button(row, "Buy", Loc.T("Mua", "Buy", "買う"), _font, () => Purchase(id), NLUi.Gold, 14f, NLUi.Ink, 40f);
        }

        /// <summary>Puts an outfit on the character for a look.</summary>
        public static void Pick(string id)
        {
            FashionService.TryOn(id);
            Instance._message = null;
            Instance.Rebuild();
        }

        /// <summary>Buys the outfit and wears it.</summary>
        public static void Purchase(string id)
        {
            string problem = FashionService.Buy(id);
            Instance._message = problem;
            if (problem == null)
            {
                FashionService.Wear(id);
                HudFeed.Post(Loc.F("Đã mua: {0}", "Bought: {0}", "買いました：{0}", FashionCatalog.Load().Outfit(id)?.Name), HudFeed.Kind.Reward, 3f);
            }
            Instance.Rebuild();
        }
    }
}
