using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// オーダーラッシュ: customers order in Japanese ("ラーメンを ふたつと おちゃを ひとつ ください"); the player taps the
    /// pictures to fill the tray and serves before the customer's patience runs out. Orders grow from one dish to three
    /// with counts. The meaning can be peeked (in the interface language, costing points); the Japanese interface has
    /// no peek. Every dish is a vocab target.
    /// </summary>
    public sealed class OrderRushGame : ArcadeGame
    {
        public sealed class Dish
        {
            public Dish(string id, string ja, string vi, string en, params string[] pictures) { Id = id; Ja = ja; Vi = vi; En = en; Pictures = pictures; }
            public string Id { get; }
            public string Ja { get; }
            public string Vi { get; }
            public string En { get; }
            public string[] Pictures { get; }
            public string Name => Loc.Pick(Vi, En, Ja);
            public string TargetId => "vocab.food." + Id;
        }

        public static readonly Dish[] Menu =
        {
            new Dish("onigiri", "おにぎり", "cơm nắm", "rice ball", "MiniGames/Food/onigiri", "MiniGames/Words/onigiri"),
            new Dish("sushi", "すし", "sushi", "sushi", "MiniGames/Food/sushi", "MiniGames/Words/sushi"),
            new Dish("pan", "パン", "bánh mì", "bread", "MiniGames/Food/pan", "MiniGames/Words/pan"),
            new Dish("keeki", "ケーキ", "bánh kem", "cake", "MiniGames/Food/keeki", "MiniGames/Words/keeki"),
            new Dish("ocha", "おちゃ", "trà", "tea", "MiniGames/Food/ocha", "MiniGames/Words/ocha"),
            new Dish("koohii", "コーヒー", "cà phê", "coffee", "MiniGames/Food/koohii", "MiniGames/Words/koohii"),
            new Dish("tamago", "たまご", "trứng", "egg", "MiniGames/Food/tamago", "MiniGames/Words/tamago"),
            new Dish("sakana", "さかな", "cá", "fish", "MiniGames/Food/sakana", "MiniGames/Words/sakana"),
            new Dish("ringo", "りんご", "táo", "apple", "MiniGames/Food/ringo", "MiniGames/Words/ringo"),
            new Dish("aisu", "アイス", "kem", "ice cream", "MiniGames/Food/aisu", "MiniGames/Words/aisu"),
            new Dish("karaage", "からあげ", "gà rán", "fried chicken", "MiniGames/Food/karaage", "Items/karaage"),
            new Dish("nikuman", "にくまん", "bánh bao", "meat bun", "MiniGames/Food/nikuman", "Items/nikuman"),
            new Dish("ramen", "ラーメン", "mì ramen", "ramen", "MiniGames/Food/ramen", "icon:ramen_dining"),
            new Dish("ichigo", "いちご", "dâu tây", "strawberry", "MiniGames/Food/ichigo", "MiniGames/Words/ichigo"),
        };

        private static readonly string[] Counters = { "", "ひとつ", "ふたつ", "みっつ" };
        private static readonly string[] Customers = { "たなかさん", "すずきさん", "さとうさん", "もりたせんせい", "キムさん", "あおいさん", "やまださん" };

        public override string GameId => "order_rush";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(1f, 0.72f, 0.3f);
        protected override (string vi, string en, string ja) HowTo => (
            "Nghe khách gọi món bằng tiếng Nhật, bấm vào hình món ăn để cho vào khay (đúng số lượng) rồi bấm Phục vụ trước khi khách hết kiên nhẫn.",
            "Customers order in Japanese. Tap the dishes to put them on the tray (in the right numbers) and serve before their patience runs out.",
            "お客さんの注文を聞いて、料理をトレーに入れて（数も正しく）、待ちきれなくなる前に出そう！");

        private readonly Dictionary<string, int> _order = new();
        private readonly Dictionary<string, int> _tray = new();
        private float _patience, _patienceMax;
        private int _served;
        private RectTransform _customerCard, _trayRow, _patienceFill;
        private TextMeshProUGUI _customerName, _line, _meaning;
        private Button _peek;
        private bool _peeked;

        public IReadOnlyDictionary<string, int> Order => _order;
        public IReadOnlyDictionary<string, int> Tray => _tray;

        protected override void BuildStage(RectTransform stage)
        {
            // Left: the customer and the order. Right: the menu. Bottom: the tray and Serve.
            var customer = UiKit.Card(stage, "Customer", UiTheme.Surface, 20, 0.3f, false);
            UiKit.Place(customer.rectTransform, 0f, 0f, 430f, 380f);
            _customerCard = customer.rectTransform;
            var avatar = UiKit.Image(customer.transform, "Avatar", UiKit.Alpha(Accent, 0.2f), UiKit.Circle);
            UiKit.Place(avatar.rectTransform, 24f, 24f, 84f, 84f);
            var face = UiKit.Icon(avatar.transform, "emoji_people", Accent, 54f);
            UiKit.Pin(face.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 54f, 54f);
            _customerName = UiKit.Label(customer.transform, "Name", "", 24f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(_customerName.rectTransform, 124f, 30f, 290f, 34f);
            var patience = UiKit.ProgressBar(customer.transform, "Patience", UiTheme.Good, UiTheme.Sunken, 5);
            UiKit.Place((RectTransform)patience.parent, 124f, 76f, 280f, 12f);
            _patienceFill = patience;
            var bubble = UiKit.Card(customer.transform, "Speech", UiTheme.Paper, 18, 0f, false);
            UiKit.Place(bubble.rectTransform, 24f, 130f, 382f, 150f);
            _line = UiKit.Label(bubble.transform, "Line", "", 26f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UiKit.Fill(_line.rectTransform, 16f, 10f, 16f, 10f);
            _meaning = UiKit.Label(customer.transform, "Meaning", "", 16f, UiTheme.TextSoft, FontStyles.Italic, TextAlignmentOptions.Center, true);
            UiKit.Place(_meaning.rectTransform, 24f, 290f, 382f, 40f);
            _peek = UiKit.Button(customer.transform, "Peek", "Xem nghĩa (−30)", "Peek meaning (−30)", "いみ", Peek, UiKit.Kind.Ghost, "visibility", 15f);
            UiKit.Place((RectTransform)_peek.transform, 110f, 330f, 210f, 38f);
            _peek.gameObject.SetActive(!Loc.IsJapanese);

            var menu = UiKit.Rect(stage, "Menu");
            UiKit.Place(menu, 460f, 0f, 700f, 380f);
            var grid = menu.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(164f, 118f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            foreach (var dish in Menu.Take(12)) MenuTile(menu, dish);

            var tray = UiKit.Card(stage, "Tray", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(tray.rectTransform, 0f, 400f, 900f, 200f);
            UiKit.Place(UiKit.Label(tray.transform, "TrayTitle", "Khay của bạn (bấm món để bỏ ra)", "Your tray (tap a dish to take it off)", "トレー（タップで もどす）", 15f, UiTheme.Muted, FontStyles.Bold), 20f, 12f, 600f, 22f);
            _trayRow = UiKit.Rect(tray.transform, "Items");
            UiKit.Place(_trayRow, 20f, 44f, 860f, 140f);
            var row = _trayRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f; row.childControlWidth = row.childControlHeight = false; row.childAlignment = TextAnchor.MiddleLeft;

            var serve = UiKit.Button(stage, "Serve", "Phục vụ", "Serve", "おまたせしました！", Serve, UiKit.Kind.Primary, "restaurant", 22f, 18);
            UiKit.Place((RectTransform)serve.transform, 920f, 430f, 240f, 70f);
            var clear = UiKit.Button(stage, "ClearTray", "Làm lại khay", "Clear tray", "やりなおす", () => { _tray.Clear(); RefreshTray(); }, UiKit.Kind.Ghost, "refresh", 15f);
            UiKit.Place((RectTransform)clear.transform, 920f, 516f, 240f, 44f);
        }

        private void MenuTile(RectTransform menu, Dish dish)
        {
            var tile = UiKit.Card(menu, "Dish_" + dish.Id, UiTheme.Paper, 16, 0f);
            var pic = UiKit.Image(tile.transform, "Picture", Color.white, Picture(dish.Pictures));
            pic.preserveAspect = true;
            UiKit.Pin(pic.rectTransform, new Vector2(0.5f, 1f), 0f, -8f, 80f, 74f);
            var name = UiKit.Label(tile.transform, "Name", dish.Ja, 17f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(name.rectTransform, new Vector2(0.5f, 0f), 0f, 6f, 150f, 26f);
            var button = tile.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Add(dish.Id));
        }

        protected override void OnRoundStart() => NextCustomer();

        protected override void OnTick(float dt)
        {
            _patience -= dt;
            UiKit.SetProgress(_patienceFill, _patience / _patienceMax);
            _patienceFill.GetComponent<Image>().color = _patience / _patienceMax > 0.35f ? UiTheme.Good : UiTheme.Bad;
            if (_patience <= 0f)
            {
                Miss(_order.Keys.Select(k => "vocab.food." + k).FirstOrDefault(), null, OrderNote(), _customerCard);
                if (State == Phase.Playing) NextCustomer();
            }
        }

        private void NextCustomer()
        {
            _order.Clear();
            _tray.Clear();
            _peeked = false;
            int dishes = _served < 3 ? 1 : _served < 7 ? 2 : 3;
            bool counts = _served >= 4;
            foreach (var dish in Menu.Take(12).OrderBy(_ => Random.value).Take(dishes))
                _order[dish.Id] = counts ? Random.Range(1, 4) : 1;
            _customerName.text = Customers[Random.Range(0, Customers.Length)];
            _line.text = OrderLine();
            _meaning.text = "";
            _peek.interactable = true;
            _patienceMax = Mathf.Lerp(16f, 9f, Mathf.Clamp01(_served / 12f)) + dishes * 2f;
            _patience = _patienceMax;
            RefreshTray();
            StartCoroutine(Bounce(_customerCard, 0.05f));
        }

        private string OrderLine()
        {
            var parts = _order.Select(kv => Find(kv.Key).Ja + "を " + (kv.Value > 1 || _served >= 4 ? Counters[kv.Value] : "")).Select(s => s.TrimEnd()).ToList();
            return string.Join("と ", parts) + " ください。";
        }

        private string OrderMeaning() => string.Join(", ", _order.Select(kv => $"{kv.Value} × {Find(kv.Key).Name}"));
        private string OrderNote() => $"{OrderLine()}  →  {(Loc.IsJapanese ? "" : OrderMeaning())}";

        private static Dish Find(string id) => Menu.First(d => d.Id == id);

        private void Peek()
        {
            if (_peeked || State != Phase.Playing) return;
            _peeked = true;
            _meaning.text = OrderMeaning();
            _peek.interactable = false;
        }

        /// <summary>Puts a dish on the tray (tile tap, or tests).</summary>
        public void Add(string dishId)
        {
            if (State != Phase.Playing) return;
            _tray.TryGetValue(dishId, out int n);
            if (n >= 3) return;
            _tray[dishId] = n + 1;
            Cue(NihongoLife.Audio.GameAudioCue.UiTick);
            RefreshTray();
        }

        private void RefreshTray()
        {
            if (_trayRow == null) return;
            UiKit.Clear(_trayRow);
            foreach (var kv in _tray)
            {
                var dish = Find(kv.Key);
                var item = UiKit.Card(_trayRow, "TrayItem_" + dish.Id, UiTheme.Raised, 14, 0f);
                item.rectTransform.sizeDelta = new Vector2(120f, 130f);
                var pic = UiKit.Image(item.transform, "Picture", Color.white, Picture(dish.Pictures));
                pic.preserveAspect = true;
                UiKit.Pin(pic.rectTransform, new Vector2(0.5f, 1f), 0f, -8f, 76f, 70f);
                UiKit.Pin(UiKit.Label(item.transform, "Count", "× " + kv.Value, 22f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform, new Vector2(0.5f, 0f), 0f, 8f, 110f, 34f);
                string id = kv.Key;
                var button = item.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => { if (_tray.TryGetValue(id, out int c)) { if (c <= 1) _tray.Remove(id); else _tray[id] = c - 1; RefreshTray(); } });
            }
        }

        /// <summary>Serves the tray (button, or tests).</summary>
        public void Serve()
        {
            if (State != Phase.Playing) return;
            bool right = _order.Count == _tray.Count && _order.All(kv => _tray.TryGetValue(kv.Key, out int n) && n == kv.Value);
            if (right)
            {
                foreach (var id in _order.Keys) Hit("vocab.food." + id, _customerCard, 0);
                int bonus = 60 * _order.Values.Sum() + Mathf.RoundToInt(_patience * 8f) - (_peeked ? 30 : 0);
                HitBonus(bonus);
                _served++;
                NextCustomer();
            }
            else
            {
                var wrong = _order.Keys.FirstOrDefault(k => !_tray.TryGetValue(k, out int n) || n != _order[k]) ?? _order.Keys.First();
                Miss("vocab.food." + wrong, _tray.Keys.FirstOrDefault(), OrderNote(), _customerCard);
                StartCoroutine(Shake(_customerCard));
            }
        }

        private void HitBonus(int points)
        {
            if (points > 0) Pop(_customerCard, $"+{points}  ありがとう！", UiTheme.Gold);
            AddScore(points);
        }

        /// <summary>Tests: fills the tray with exactly the order.</summary>
        public void FillTrayCorrectly()
        {
            _tray.Clear();
            foreach (var kv in _order) _tray[kv.Key] = kv.Value;
            RefreshTray();
        }
    }
}
