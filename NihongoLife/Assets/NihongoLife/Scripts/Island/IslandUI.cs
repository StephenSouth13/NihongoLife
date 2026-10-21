using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>
    /// Midori Island interface: toolbar, welcome banner, farm card, animal card, shop (with a price &amp; profit guide),
    /// notebook (progress), toasts and the return-trip screen. Warm cream cards, deep natural green, gold coins.
    /// Interface text is in the chosen language only; the word being learned (Japanese, or English in the island's
    /// English mode) sits in its own word card with its reading. Missing seeds or tools can be bought right from the
    /// farm card. Every window registers with UiModalStack (Esc closes the top one) and locks walking while open.
    /// </summary>
    public sealed class IslandUI : MonoBehaviour
    {
        // ─────────── Palette ───────────
        public static readonly Color Cream = new Color(0.985f, 0.97f, 0.93f, 0.99f);
        public static readonly Color Cream2 = new Color(0.95f, 0.925f, 0.86f, 1f);
        public static readonly Color Green = new Color(0.16f, 0.38f, 0.25f, 1f);
        public static readonly Color GreenDeep = new Color(0.11f, 0.27f, 0.18f, 1f);
        public static readonly Color GreenSoft = new Color(0.87f, 0.93f, 0.84f, 1f);
        public static readonly Color Gold = new Color(0.86f, 0.62f, 0.16f, 1f);
        public static readonly Color Ink = new Color(0.15f, 0.19f, 0.14f, 1f);
        public static readonly Color Muted = new Color(0.43f, 0.47f, 0.41f, 1f);
        public static readonly Color Error = new Color(0.78f, 0.27f, 0.22f, 1f);
        public static readonly Color ErrorSoft = new Color(0.99f, 0.91f, 0.89f, 1f);
        public static readonly Color Water = new Color(0.22f, 0.52f, 0.8f, 1f);

        private static IslandUI _instance;
        private TMP_FontAsset _font;
        private Canvas _canvas;
        private RectTransform _toolbar, _farmCard, _animalCard, _shop, _book, _toasts, _welcome, _travel;
        private bool _playerWasLocked;
        private int _openCount;

        public static IslandUI Instance => _instance != null ? _instance : (_instance = Create());
        public static bool Exists => _instance != null;

        private static IslandUI Create()
        {
            var go = new GameObject("IslandUI");
            var ui = go.AddComponent<IslandUI>();
            ui._font = UiKit.Font;
            ui._canvas = NLUi.CreateCanvas("IslandCanvas", 520, go.transform);
            ui._toasts = Column((RectTransform)ui._canvas.transform, "Toasts", 8f);
            // Left column under the quest panel: never under the right-hand farm / animal cards.
            NLUi.Anchor(ui._toasts, new Vector2(0f, 1f), new Vector2(28f, -215f), new Vector2(420f, 0f));
            ui._toasts.pivot = new Vector2(0f, 1f);
            NLUi.FitContent(ui._toasts);
            Loc.Watch(ui, ui.RebuildOpenWindows);
            return ui;
        }

        private void OnDestroy()
        {
            IslandState.Changed -= RefreshToolbar;
            if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshToolbar;
            IslandLanguage.Changed -= OnLanguage;
            if (_instance == this) _instance = null;
            LockPlayer(false, force: true);
        }

        public static void DestroyAll()
        {
            if (_instance != null) Destroy(_instance.gameObject);
            _instance = null;
        }

        /// <summary>The interface language changed: every open window is rebuilt in it.</summary>
        private void RebuildOpenWindows()
        {
            if (_toolbar != null) BuildToolbar();
            if (_farmCard != null && _plot != null) BuildFarm(null, false);
            if (_animalCard != null && _animal != null) BuildAnimal(null, false);
            if (_shop != null) BuildShop(null, false);
            if (_book != null) BuildBook();
        }

        // ─────────── Shared pieces ───────────

        private static RectTransform Column(RectTransform parent, string name, float spacing) => NLUi.Group(parent, name, true, spacing);

        private RectTransform Card(string name, Vector2 anchor, Vector2 position, float width)
        {
            if (name != "Welcome") { Discard(_welcome); _welcome = null; } // the welcome banner never sits over a window
            var card = NLUi.Panel(_canvas.transform, name, Cream, new RectOffset(0, 0, 0, 20), 0f);
            card.GetComponent<Image>().sprite = UiKit.Rounded(20);
            NLUi.Anchor(card, anchor, position, new Vector2(width, 0f));
            NLUi.FitContent(card);
            UiKit.AddShadow(card, 20, 0.4f, 10f);
            return card;
        }

        private RectTransform Header(RectTransform card, string icon, string title, string kicker, Action close)
        {
            var header = NLUi.Panel(card, "Header", Green, new RectOffset(86, 70, 16, 16), 2f);
            header.GetComponent<Image>().sprite = UiKit.Rounded(20);
            var disc = UiKit.Image(header, "IconDisc", new Color(1f, 1f, 1f, 0.14f), UiKit.Circle);
            disc.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Pin(disc.rectTransform, new Vector2(0f, 0.5f), 22f, 0f, 48f, 48f);
            var ic = UiKit.Icon(disc.transform, icon, Color.white, 26f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 26f, 26f);
            if (!string.IsNullOrEmpty(kicker)) NLUi.Label(header, "Kicker", kicker, 13f, new Color(0.84f, 0.93f, 0.8f), _font, FontStyles.Bold);
            NLUi.Label(header, "Title", title, 24f, Color.white, _font, FontStyles.Bold);
            var x = UiKit.IconButton(header, "CloseX", "close", close, UiKit.Kind.Ghost, 40f, 20f);
            x.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Pin((RectTransform)x.transform, new Vector2(1f, 0.5f), -16f, 0f, 40f, 40f);
            return header;
        }

        private RectTransform Body(RectTransform card, float spacing = 12f)
        {
            var body = NLUi.Group(card, "Body", true, spacing);
            body.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(24, 24, 18, 0);
            return body;
        }

        private TextMeshProUGUI Text(RectTransform parent, string value, float size, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var t = NLUi.Label(parent, "T", value, size, color, _font, style, align);
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        private Button Btn(RectTransform parent, string label, Action click, Color color, Color text, float height = 46f, float fontSize = 17f)
        {
            var b = NLUi.Button(parent, "Btn", label, _font, click, color, fontSize, text, height);
            b.GetComponent<Image>().sprite = UiKit.Rounded(14);
            return b;
        }

        /// <summary>Button with a Material icon before the label (the label stays the first text of the button).</summary>
        private Button IconBtn(RectTransform parent, string name, string icon, string label, Action click, Color color, Color text, float height = 46f, float fontSize = 16f)
        {
            var b = Btn(parent, label, click, color, text, height, fontSize);
            b.name = name;
            var group = b.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group != null) group.padding = new RectOffset(48, 16, 8, 8);
            var ic = UiKit.Icon(b.transform, icon, text, 22f);
            ic.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Pin(ic.rectTransform, new Vector2(0f, 0.5f), 16f, 0f, 22f, 22f);
            return b;
        }

        private Image Icon(RectTransform parent, string itemId, float size)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var e = go.GetComponent<LayoutElement>();
            e.preferredWidth = e.minWidth = size; e.preferredHeight = e.minHeight = size;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            var sprite = ItemIcons.Get(itemId);
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : new Color(0f, 0f, 0f, 0f);
            return img;
        }

        /// <summary>A coin + amount inside a layout (prices, totals, profits).</summary>
        private RectTransform Coin(RectTransform parent, int amount, float size, Color color, bool sign = false)
        {
            var money = UiKit.Money(parent, "Money", amount, size, color, sign);
            return money;
        }

        /// <summary>The word being learned: target word + reading, in a word card (only when it differs from the interface name).</summary>
        private void WordChip(RectTransform parent, IslandWord word, float size = 18f)
        {
            if (word == null || !IslandLanguage.ShowsTarget(word)) return;
            var chip = NLUi.Panel(parent, "WordCard", GreenSoft, new RectOffset(12, 14, 6, 7), 8f, vertical: false);
            chip.GetComponent<Image>().sprite = UiKit.Rounded(12);
            ((HorizontalLayoutGroup)chip.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.FitContent(chip, width: true, height: true);
            var ic = UiKit.Icon(chip, "translate", Green, size * 0.9f);
            UiKit.Size(ic, size * 0.9f, size * 0.9f);
            var t = NLUi.Label(chip, "Target", $"<b>{IslandLanguage.Primary(word)}</b>", size, Green, _font);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            string reading = IslandLanguage.Reading(word);
            if (!string.IsNullOrEmpty(reading))
            {
                var r = NLUi.Label(chip, "Reading", reading, size * 0.68f, Muted, _font, FontStyles.Italic);
                r.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        /// <summary>The action sentence the work teaches: the learned sentence, its reading and its meaning.</summary>
        private void PhraseCard(RectTransform parent, IslandWord phrase)
        {
            if (phrase == null) return;
            var card = NLUi.Panel(parent, "Phrase", GreenSoft, new RectOffset(18, 18, 12, 14), 3f);
            card.GetComponent<Image>().sprite = UiKit.Rounded(14);
            Text(card, Loc.T("CÂU HÀNH ĐỘNG", "ACTION PHRASE", "動作のフレーズ"), 12.5f, Green, FontStyles.Bold);
            Text(card, IslandLanguage.Primary(phrase), 20f, Ink, FontStyles.Bold);
            string reading = IslandLanguage.Reading(phrase);
            if (!string.IsNullOrEmpty(reading)) Text(card, reading, 13f, Muted, FontStyles.Italic);
            string meaning = IslandLanguage.Helper(phrase);
            if (!string.IsNullOrEmpty(meaning)) Text(card, meaning, 14.5f, Muted);
        }

        private void LockPlayer(bool locked, bool force = false)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (locked)
            {
                if (_openCount++ == 0 && player != null) { _playerWasLocked = player.InputLocked; player.InputLocked = true; }
            }
            else
            {
                if (force) _openCount = 1;
                if (_openCount > 0 && --_openCount == 0 && player != null) player.InputLocked = _playerWasLocked;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Hides a window at once and destroys it at the end of the frame (rebuilt windows never overlap their old copy).</summary>
        private void Discard(RectTransform window)
        {
            if (window == null) return;
            window.gameObject.SetActive(false);
            Destroy(window.gameObject);
        }

        private void Close(ref RectTransform window)
        {
            if (window == null) return;
            Discard(window);
            window = null;
            LockPlayer(false);
        }

        // ─────────── Toasts ───────────

        /// <summary>A short message at the left (already in the interface language).</summary>
        public static void Toast(string message, bool error = false)
        {
            var ui = Instance;
            var card = NLUi.Panel(ui._toasts, "Toast", error ? ErrorSoft : Cream, new RectOffset(56, 18, 12, 13), 2f);
            card.GetComponent<Image>().sprite = UiKit.Rounded(14);
            var ic = UiKit.Icon(card, error ? "warning" : "check_circle", error ? Error : Green, 24f);
            ic.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Pin(ic.rectTransform, new Vector2(0f, 0.5f), 18f, 0f, 24f, 24f);
            ui.Text(card, message, 15.5f, error ? Error : Ink);
            ui.StartCoroutine(ui.FadeOut(card, 4.5f));
        }

        /// <summary>A new word: the learned word large, its reading and the meaning in the interface language.</summary>
        public static void WordToast(IslandWord word, string kicker)
        {
            var ui = Instance;
            var card = NLUi.Panel(ui._toasts, "WordToast", Cream, new RectOffset(56, 18, 12, 13), 2f);
            card.GetComponent<Image>().sprite = UiKit.Rounded(14);
            var ic = UiKit.Icon(card, "translate", Gold, 24f);
            ic.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Pin(ic.rectTransform, new Vector2(0f, 1f), 18f, -16f, 24f, 24f);
            ui.Text(card, kicker.ToUpperInvariant(), 12f, Gold, FontStyles.Bold);
            ui.Text(card, IslandLanguage.Primary(word), 26f, Green, FontStyles.Bold);
            string reading = IslandLanguage.Reading(word) ?? "";
            string meaning = IslandLanguage.Helper(word);
            string line = string.Join("  ·  ", new[] { reading, meaning }.Where(x => !string.IsNullOrEmpty(x)));
            if (line.Length > 0) ui.Text(card, line, 14.5f, Muted);
            ui.StartCoroutine(ui.FadeOut(card, 5f));
        }

        private IEnumerator FadeOut(RectTransform card, float seconds)
        {
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime) { if (card == null) yield break; group.alpha = t / 0.25f; yield return null; }
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(seconds);
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { if (card == null) yield break; group.alpha = 1f - t / 0.4f; yield return null; }
            if (card != null) Destroy(card.gameObject);
        }

        // ─────────── Welcome ───────────

        public static void ShowWelcome()
        {
            var ui = Instance;
            ui.Discard(ui._welcome);
            var card = ui.Card("Welcome", new Vector2(0.5f, 1f), new Vector2(0f, -110f), 760f);
            card.pivot = new Vector2(0.5f, 1f);
            var head = NLUi.Panel(card, "Head", GreenDeep, new RectOffset(28, 28, 18, 16), 2f);
            head.GetComponent<Image>().sprite = UiKit.Rounded(20);
            var island = IslandCatalog.Load().places.island;
            NLUi.Label(head, "Kicker", Loc.T("CHÀO MỪNG", "WELCOME", "ようこそ"), 13.5f, new Color(0.96f, 0.85f, 0.55f), ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(head, "Title", IslandLanguage.Title(island), 34f, Color.white, ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            if (IslandLanguage.ShowsTarget(island))
                NLUi.Label(head, "Sub", $"{IslandLanguage.Primary(island)}  ·  {island.reading}", 17f, new Color(0.84f, 0.93f, 0.8f), ui._font, FontStyles.Normal, TextAlignmentOptions.Center);
            var body = ui.Body(card, 6f);
            ui.Text(body, Loc.T("Ruộng ở phía trước, chuồng thú bên phải, cửa hàng Midori cạnh ga, bến câu ở bờ biển. Đến gần và nhấn <b>F</b> để làm việc; <b>P</b> mở cửa hàng.",
                "Fields ahead, the barn on the right, the Midori Store by the station and the pier on the shore. Walk up and press <b>F</b> to work; <b>P</b> opens the store.",
                "前に畑、右に家畜小屋、駅のそばにみどりしょうてん、海辺に釣り場があります。近づいて<b>F</b>で作業、<b>P</b>でお店を開きます。"),
                16f, Ink, FontStyles.Normal, TextAlignmentOptions.Center);
            ui._welcome = card;
            ui.StartCoroutine(ui.FadeOut(card, 7f));
        }

        // ─────────── Toolbar ───────────

        public static void HideToolbar()
        {
            if (_instance == null) return;
            _instance.Discard(_instance._toolbar);
            _instance._toolbar = null;
            IslandState.Changed -= _instance.RefreshToolbar;
        }

        public static void ShowToolbar()
        {
            var ui = Instance;
            ui.BuildToolbar();
            IslandState.Changed -= ui.RefreshToolbar;
            IslandState.Changed += ui.RefreshToolbar;
            var bag = PlayerInventory.Instance;
            if (bag != null) { bag.OnInventoryChanged -= ui.RefreshToolbar; bag.OnInventoryChanged += ui.RefreshToolbar; }
            IslandLanguage.Changed -= ui.OnLanguage;
            IslandLanguage.Changed += ui.OnLanguage;
        }

        private void OnLanguage(TargetLanguage _) => RebuildOpenWindows();
        private void RefreshToolbar() => BuildToolbar();

        private void BuildToolbar()
        {
            if (_canvas == null) return;
            Discard(_toolbar);
            _toolbar = NLUi.Panel(_canvas.transform, "IslandToolbar", Cream, new RectOffset(14, 14, 8, 8), 8f, vertical: false);
            _toolbar.GetComponent<Image>().sprite = UiKit.Rounded(18);
            NLUi.Anchor(_toolbar, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(0f, 66f));
            _toolbar.pivot = new Vector2(0.5f, 1f);
            ((HorizontalLayoutGroup)_toolbar.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.FitContent(_toolbar, width: true, height: false);
            UiKit.AddShadow(_toolbar, 18, 0.25f, 6f);
            foreach (var tool in IslandCatalog.Load().tools)
            {
                bool owned = IslandEconomy.Owned(tool.id) > 0;
                var slot = NLUi.Panel(_toolbar, "Tool_" + tool.id, owned ? GreenSoft : new Color(0.91f, 0.89f, 0.84f, 1f), new RectOffset(6, 10, 4, 4), 6f, vertical: false);
                slot.GetComponent<Image>().sprite = UiKit.Rounded(12);
                ((HorizontalLayoutGroup)slot.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                var icon = Icon(slot, tool.id, 38f);
                if (!owned) icon.color = new Color(1f, 1f, 1f, 0.35f);
                var col = NLUi.Group(slot, "Col", true, 3f);
                var label = Text(col, IslandLanguage.Title(tool.word), 14f, owned ? Ink : Muted, FontStyles.Bold);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                NLUi.Size(label, 92f);
                if (owned && tool.durability > 0)
                {
                    int left = IslandTools.Left(tool.id);
                    var bar = UiKit.ProgressBar(col, "Wear", left <= 5 ? Error : Green, new Color(0f, 0f, 0f, 0.1f), 3);
                    UiKit.Size(bar.parent.GetComponent<Image>(), 92f, 6f);
                    UiKit.SetProgress(bar, left / (float)tool.durability);
                }
                else Text(col, owned ? "∞" : Loc.T("chưa có", "not owned", "未所持"), 11.5f, Muted);
            }
            var lang = IconBtn(_toolbar, "LearnLanguage", "translate", $"{Loc.T("Học", "Learning", "学習")}: <b>{IslandLanguage.LanguageLabel}</b>", ToggleLanguage, Green, Color.white, 46f, 15f);
            NLUi.Size(lang, 196f, 46f);
            var book = IconBtn(_toolbar, "Notebook", "menu_book", Loc.F("Sổ tay · {0} từ", "Notebook · {0} words", "ノート · {0}語", DistinctWords().Count), OpenProgress, Cream2, Green, 46f, 15f);
            NLUi.Size(book, 186f, 46f);
            var shop = IconBtn(_toolbar, "Shop", "storefront", Loc.T("Cửa hàng · P", "Store · P", "お店 · P"), OpenShop, Gold, new Color(0.18f, 0.11f, 0.02f), 46f, 15f);
            NLUi.Size(shop, 158f, 46f);
            var decorate = IconBtn(_toolbar, "Decorate", "yard", Loc.T("Trang trí", "Decorate", "かざる"), () => IslandDecorRuntime.Instance?.BeginEdit(), Green, Color.white, 46f, 15f);
            NLUi.Size(decorate, 150f, 46f);
        }

        public static void ToggleLanguage()
        {
            IslandLanguage.Target = IslandLanguage.Target == TargetLanguage.Japanese ? TargetLanguage.English : TargetLanguage.Japanese;
            Toast(IslandLanguage.Target == TargetLanguage.English
                ? Loc.T("Đang học tiếng Anh — tên đồ vật và câu hành động chuyển sang tiếng Anh.", "Learning English — item names and action phrases switch to English.", "英語を学習中 — 物の名前と動作のフレーズが英語になります。")
                : Loc.T("Đang học tiếng Nhật — tên đồ vật và câu hành động chuyển sang tiếng Nhật.", "Learning Japanese — item names and action phrases switch to Japanese.", "日本語を学習中 — 物の名前と動作のフレーズが日本語になります。"));
        }

        // ─────────── Farm card ───────────

        private FarmPlot _plot;
        private RectTransform _growFill;
        private TextMeshProUGUI _growText, _farmFeedback;

        public static bool FarmOpen => _instance != null && _instance._farmCard != null;
        public static FarmPlot OpenPlot => _instance != null ? _instance._plot : null;

        public static void OpenFarm(FarmPlot plot)
        {
            var ui = Instance;
            bool wasOpen = ui._farmCard != null;
            ui._plot = plot;
            ui.BuildFarm(null, false);
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._farmCard != null, () => ui.Close(ref ui._farmCard), "Farm");
            }
        }

        public static void CloseFarm() { if (_instance != null) _instance.Close(ref _instance._farmCard); }

        private static readonly (string icon, string vi, string en, string ja)[] FarmSteps =
        {
            ("agriculture", "Xới", "Till", "耕す"), ("potted_plant", "Gieo", "Plant", "まく"), ("water_drop", "Tưới", "Water", "水"),
            ("eco", "Lớn", "Grow", "育つ"), ("nutrition", "Thu", "Harvest", "収穫"),
        };

        private static int StepOf(FarmPlot.Phase phase) => phase switch
        {
            FarmPlot.Phase.Untilled => 0,
            FarmPlot.Phase.Tilled => 1,
            FarmPlot.Phase.NeedsWater => 2,
            FarmPlot.Phase.Growing => 3,
            _ => 4,
        };

        private void BuildFarm(string feedback, bool error)
        {
            Discard(_farmCard);
            var plot = _plot;
            var catalog = IslandCatalog.Load();
            var phase = plot.CurrentPhase;
            _lastPhase = phase; // the card always reflects this phase; Update rebuilds only on a real change
            var crop = plot.Crop;
            _farmCard = Card("FarmCard", new Vector2(1f, 0.5f), new Vector2(-28f, 10f), 500f);
            Header(_farmCard, "agriculture", Loc.F("Ô ruộng {0}", "Field {0}", "はたけ {0}", plot.Number), Loc.T("NÔNG TRẠI ĐẢO XANH", "MIDORI FARM", "みどり農園"), CloseFarm);
            var body = Body(_farmCard);

            // Stepper: till → plant → water → grow → harvest.
            var steps = NLUi.Group(body, "Steps", false, 0f, TextAnchor.MiddleCenter, true);
            int current = StepOf(phase);
            for (int i = 0; i < FarmSteps.Length; i++)
            {
                var s = FarmSteps[i];
                bool done = i < current, now = i == current;
                var cell = NLUi.Group(steps, "Step" + i, true, 3f, TextAnchor.UpperCenter, false);
                var disc = UiKit.Image(cell, "Disc", now ? Green : done ? new Color(0.36f, 0.62f, 0.44f, 1f) : new Color(0f, 0f, 0f, 0.07f), UiKit.Circle);
                UiKit.Size(disc, 40f, 40f);
                var ic = UiKit.Icon(disc.transform, done ? "check_circle" : s.icon, now || done ? Color.white : Muted, 22f);
                UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 22f, 22f);
                var label = Text(cell, Loc.T(s.vi, s.en, s.ja), 12.5f, now ? Green : Muted, now ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Center);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // The crop (or the soil) with its name, word card and what it earns.
            var top = NLUi.Panel(body, "Crop", Color.white, new RectOffset(14, 14, 12, 12), 14f, vertical: false);
            top.GetComponent<Image>().sprite = UiKit.Rounded(14);
            ((HorizontalLayoutGroup)top.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            Icon(top, crop != null ? crop.ProduceItemId : "seed_carrot", 78f).color = crop != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            var info = NLUi.Group(top, "Info", true, 4f);
            NLUi.Size(info, flexibleWidth: 1f);
            if (crop != null)
            {
                Text(info, IslandLanguage.Title(crop.word), 23f, Ink, FontStyles.Bold);
                WordChip(info, crop.word, 16f);
                var earn = NLUi.Group(info, "Earn", false, 6f, TextAnchor.MiddleLeft, false);
                Text(earn, Loc.F("Thu {0} quả × ", "Yields {0} × ", "{0}個 × ", crop.yield), 13.5f, Muted);
                Coin(earn, crop.sellPrice, 13.5f, Gold);
                Text(earn, " = ", 13.5f, Muted);
                Coin(earn, crop.yield * crop.sellPrice, 14f, Green);
            }
            else Text(info, phase == FarmPlot.Phase.Untilled
                ? Loc.T("Đất chưa xới", "Untilled soil", "まだ耕していない土")
                : Loc.T("Đất đã xới, chờ gieo hạt", "Tilled — ready for seeds", "耕しました — 種まきを待っています"), 20f, Ink, FontStyles.Bold);

            // Growth.
            var status = NLUi.Group(body, "Status", true, 5f);
            Color dot = phase switch { FarmPlot.Phase.NeedsWater => Water, FarmPlot.Phase.Ready => Gold, FarmPlot.Phase.Growing => Green, _ => Muted };
            Text(status, $"<color=#{ColorUtility.ToHtmlStringRGB(dot)}>●</color>  {Capital(FarmPlot.PhaseLabel(phase))}" +
                (crop != null ? "   ·   " + Loc.F("giai đoạn {0}/4", "stage {0}/4", "段階 {0}/4", Mathf.Min(plot.Record.stage + 1, 4)) : ""), 16f, Ink, FontStyles.Bold);
            _growFill = UiKit.ProgressBar(status, "Growth", Green, new Color(0f, 0f, 0f, 0.08f), 5);
            UiKit.Size(_growFill.parent.GetComponent<Image>(), -1f, 10f);
            _growText = Text(status, "", 13.5f, Muted);

            // The action this phase needs, with the phrase it teaches.
            var actions = NLUi.Group(body, "Actions", true, 8f);
            switch (phase)
            {
                case FarmPlot.Phase.Untilled:
                {
                    var tool = catalog.Tool("tool_hoe");
                    PhraseCard(body, tool.verb);
                    var (toolId, toolName, seconds) = FarmActionTimes.Till();
                    if (toolId != null)
                        ActionButton(actions, Loc.T("Xới đất", "Till the soil", "土を耕す"), toolId, seconds,
                            () => Work(Loc.T("Đang xới đất…", "Tilling…", "耕しています…"), toolName, seconds, () => Do(plot.Till(), tool.verb, Loc.T("Đã xới đất.", "Soil tilled.", "耕しました。")), "Work_Hoe", toolId));
                    else NeedTool(actions, "tool_hoe", Loc.T("Không có cuốc hay xẻng thì không xới được đất.", "Without a hoe or a shovel you cannot till.", "くわかシャベルがないと耕せません。"));
                    break;
                }
                case FarmPlot.Phase.Tilled:
                {
                    PhraseCard(body, catalog.verbs.plant);
                    BuildSeedPicker(actions, plot);
                    break;
                }
                case FarmPlot.Phase.NeedsWater:
                {
                    var tool = catalog.Tool("tool_watering_can");
                    PhraseCard(body, tool.verb);
                    if (IslandTools.Usable(tool.id))
                        ActionButton(actions, Loc.T("Tưới nước", "Water it", "水をあげる"), tool.id, FarmActionTimes.Water,
                            () => Work(Loc.T("Đang tưới nước…", "Watering…", "水やり中…"), IslandLanguage.Name(tool.word), FarmActionTimes.Water,
                                () => Do(plot.Water(), tool.verb, Loc.T("Đã tưới — cây bắt đầu lớn.", "Watered — it starts growing.", "水をあげました — 育ち始めます。")), "Work_Water", "tool_watering_can"));
                    else NeedTool(actions, "tool_watering_can", Loc.T("Không có bình tưới thì không mang nước được.", "Without a watering can you cannot carry water.", "じょうろがないと水を運べません。"));
                    break;
                }
                case FarmPlot.Phase.Growing:
                    Text(actions, Loc.T("Cây đang lớn. Bạn có thể đi làm việc khác — cây vẫn lớn kể cả khi bạn rời đảo.",
                        "It is growing. Go and do something else — it keeps growing even when you leave the island.",
                        "育っています。ほかのことをしても大丈夫 — 島を離れても育ち続けます。"), 15f, Muted);
                    break;
                case FarmPlot.Phase.Ready:
                {
                    PhraseCard(body, catalog.verbs.harvest);
                    ActionButton(actions, Loc.F("Thu hoạch ×{0}", "Harvest ×{0}", "収穫 ×{0}", crop.yield), crop.ProduceItemId, FarmActionTimes.Harvest, () =>
                        Work(Loc.T("Đang thu hoạch…", "Harvesting…", "収穫中…"), Loc.T("tay", "hands", "手"), FarmActionTimes.Harvest, pose: "Work_Harvest", done: () =>
                        {
                            string err = plot.Harvest(out int amount);
                            Do(err, catalog.verbs.harvest, Loc.F("Thu hoạch {0} {1} — đã vào balo. Bán ở cửa hàng Midori.", "Harvested {0} {1} — in your bag. Sell them at the Midori Store.", "{1}を{0}個収穫 — バッグに入りました。みどりしょうてんで売れます。", amount, IslandLanguage.Name(crop.word)));
                            if (err == null) WordToast(crop.word, Loc.T("Thu hoạch", "Harvest", "収穫"));
                        }));
                    break;
                }
            }
            if (crop != null && phase != FarmPlot.Phase.Ready)
            {
                var shovel = catalog.Tool("tool_shovel");
                var (clearId, clearTool, clearSeconds) = FarmActionTimes.Clear();
                if (clearId != null)
                {
                    var clear = IconBtn(actions, "Clear", "delete_sweep", $"{Loc.T("Dọn ô", "Clear plot", "片づける")}  ·  {clearTool}  ·  {clearSeconds:0.#}s  ·  {IslandTools.Label(clearId)}",
                        () => Work(Loc.T("Đang dọn ô đất…", "Clearing…", "片づけ中…"), clearTool, clearSeconds, () => Do(plot.Clear(), shovel.verb, Loc.T("Đã dọn ô đất.", "Plot cleared.", "片づけました。")), "Work_Clear", clearId), Cream2, Muted, 38f, 13.5f);
                }
            }
            _farmFeedback = Text(body, feedback ?? "", 15f, error ? Error : Green, FontStyles.Bold);
            UpdateGrowth();
        }

        /// <summary>No tool: says why, offers to buy it right here (coin price) or to open the store.</summary>
        private void NeedTool(RectTransform actions, string toolId, string why)
        {
            var tool = IslandCatalog.Load().Tool(toolId);
            var box = NLUi.Panel(actions, "NeedTool", ErrorSoft, new RectOffset(16, 16, 12, 14), 8f);
            box.GetComponent<Image>().sprite = UiKit.Rounded(14);
            Text(box, why, 15f, Error, FontStyles.Bold);
            var row = NLUi.Group(box, "Row", false, 10f, TextAnchor.MiddleLeft, false);
            if (tool != null)
            {
                bool afford = IslandEconomy.Yen >= tool.price;
                var buy = IconBtn(row, "QuickBuy_" + toolId, "shopping_cart", Loc.F("Mua {0} · ¥{1:N0}", "Buy a {0} · ¥{1:N0}", "{0}を買う · ¥{1:N0}", IslandLanguage.Name(tool.word), tool.price),
                    () => QuickBuy(toolId, tool.price), afford ? Green : new Color(0.62f, 0.64f, 0.6f), Color.white, 44f, 15f);
                buy.interactable = afford;
                NLUi.Size(buy, 250f, 44f);
            }
            var shop = IconBtn(row, "OpenShop", "storefront", Loc.T("Cửa hàng", "Store", "お店"), () => { CloseFarm(); OpenShopAt("agriculture", toolId); }, Gold, new Color(0.18f, 0.11f, 0.02f), 44f, 15f);
            NLUi.Size(shop, 150f, 44f);
        }

        /// <summary>Buys one item without leaving the farm card (seed or tool), then redraws the card.</summary>
        private void QuickBuy(string itemId, int price)
        {
            var result = IslandEconomy.Buy(itemId, price, 1);
            string message = result switch
            {
                IslandEconomy.Result.Ok => Loc.F("Đã mua 1 {0} (−¥{1:N0}).", "Bought 1 {0} (−¥{1:N0}).", "{0}を1つ買いました（−¥{1:N0}）。", ItemName(itemId), price),
                IslandEconomy.Result.NoMoney => Loc.F("Không đủ tiền: cần ¥{0:N0}, bạn có ¥{1:N0}.", "Not enough money: ¥{0:N0} needed, you have ¥{1:N0}.", "お金が足りません：¥{0:N0}必要、所持¥{1:N0}。", price, IslandEconomy.Yen),
                IslandEconomy.Result.BagFull => Loc.T("Balo đầy — bán bớt nông sản trước.", "Your bag is full — sell some produce first.", "バッグがいっぱいです。先に作物を売りましょう。"),
                _ => Loc.T("Không thực hiện được.", "That did not work.", "できませんでした。"),
            };
            if (result == IslandEconomy.Result.Ok) IslandState.Discover(WordKey(itemId));
            if (_farmCard != null) BuildFarm(message, result != IslandEconomy.Result.Ok);
        }

        private static string ItemName(string itemId)
        {
            var c = IslandCatalog.Load();
            var tool = c.Tool(itemId);
            if (tool != null) return IslandLanguage.Name(tool.word);
            var crop = c.crops.FirstOrDefault(x => x.SeedItemId == itemId || x.ProduceItemId == itemId);
            if (crop != null) return itemId == crop.SeedItemId ? Loc.F("hạt {0}", "{0} seeds", "{0}の種", IslandLanguage.Name(crop.word)) : IslandLanguage.Name(crop.word);
            var fish = c.Fish(itemId);
            if (fish != null) return IslandLanguage.Name(fish.word);
            var decor = c.Decor(itemId);
            if (decor != null) return IslandLanguage.Name(decor.word);
            var product = c.products?.FirstOrDefault(p => p.itemId == itemId);
            return product != null ? IslandLanguage.Name(product.word) : itemId;
        }

        /// <summary>Farm work takes time (progress card); repeated clicks while working are ignored.</summary>
        private static void Work(string label, string tool, float seconds, Action done, string pose = null, string heldToolId = null)
        {
            if (TimedAction.Busy) return;
            TimedAction.Run(label, tool, seconds, done, pose: pose, prop: HeldTool(heldToolId));
        }

        /// <summary>Hand-held model of a tool item (built by IslandBuilder as "Held_&lt;toolId&gt;"), or null.</summary>
        public static GameObject HeldTool(string toolId) =>
            string.IsNullOrEmpty(toolId) || IslandModelLibrary.Instance == null ? null : IslandModelLibrary.Instance.Find("Held_" + toolId);

        /// <summary>The main farm action: item icon, label (its first text), time, and the tool's remaining uses.</summary>
        private void ActionButton(RectTransform parent, string label, string iconItem, float seconds, Action click)
        {
            string wear = IslandTools.Label(iconItem);
            var b = Btn(parent, $"<b>{label}</b>  <size=80%><color=#CFE3D3>·  {seconds:0.#}s{(string.IsNullOrEmpty(wear) ? "" : "  ·  " + wear)}</color></size>", click, Green, Color.white, 58f, 18f);
            b.name = "Action";
            b.GetComponent<HorizontalOrVerticalLayoutGroup>().padding = new RectOffset(64, 18, 10, 10);
            var icon = Icon((RectTransform)b.transform, iconItem, 38f);
            icon.GetComponent<LayoutElement>().ignoreLayout = true;
            var r = icon.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(14f, 0f); r.sizeDelta = new Vector2(38f, 38f);
        }

        private void BuildSeedPicker(RectTransform parent, FarmPlot plot)
        {
            var catalog = IslandCatalog.Load();
            var owned = catalog.crops.Where(c => IslandEconomy.Owned(c.SeedItemId) > 0).ToList();
            Text(parent, Loc.T("Chọn hạt giống", "Choose a seed", "種をえらぶ"), 15f, Green, FontStyles.Bold);
            if (owned.Count == 0) Text(parent, Loc.T("Bạn chưa có hạt giống nào — mua ngay bên dưới.", "You have no seeds — buy some below.", "種がありません — 下で買えます。"), 15f, Error);
            foreach (var crop in owned)
            {
                var row = NLUi.Panel(parent, "Seed_" + crop.id, Color.white, new RectOffset(10, 10, 6, 6), 10f, vertical: false);
                row.GetComponent<Image>().sprite = UiKit.Rounded(12);
                ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                Icon(row, crop.SeedItemId, 46f);
                var col = NLUi.Group(row, "Text", true, 1f);
                NLUi.Size(col, flexibleWidth: 1f);
                Text(col, $"<b>{IslandLanguage.Title(crop.word)}</b>  <size=80%><color=#6B7566>×{IslandEconomy.Owned(crop.SeedItemId)}</color></size>", 17f, Ink);
                Text(col, Loc.F("Lớn trong {0} giây · tưới 3 lần · thu {1}", "Grows in {0}s · water 3 times · yields {1}", "{0}秒で育つ · 水やり3回 · {1}個収穫", Mathf.RoundToInt(crop.secondsPerStage * 3), crop.yield), 12.5f, Muted);
                var plant = Btn(row, Loc.T("Gieo", "Plant", "まく"), () => Work(Loc.T("Đang gieo hạt…", "Planting…", "種まき中…"), Loc.T("tay", "hands", "手"), FarmActionTimes.Plant,
                    () => Do(plot.Plant(crop.id), catalog.verbs.plant, Loc.F("Đã gieo hạt {0}. Giờ hãy tưới nước.", "{0} planted. Now water it.", "{0}の種をまきました。水をあげましょう。", IslandLanguage.Name(crop.word))), "Work_Plant"), Green, Color.white, 40f, 15.5f);
                NLUi.Size(plant, 88f, 40f);
                plant.name = "Plant_" + crop.id;
            }
            // Buy seeds here: one tap per seed, with what it earns.
            Text(parent, Loc.T("Mua hạt giống", "Buy seeds", "種を買う"), 13.5f, Muted, FontStyles.Bold);
            var buyRow = NLUi.Group(parent, "BuySeeds", false, 6f, TextAnchor.MiddleLeft, true);
            foreach (var crop in catalog.crops)
            {
                bool afford = IslandEconomy.Yen >= crop.seedPrice;
                var b = Btn(buyRow, $"<b>{IslandLanguage.Title(crop.word)}</b>\n<size=90%><color=#{(afford ? "A06C0A" : "8E948A")}>¥{crop.seedPrice}</color>  <color=#2E6B45>+¥{crop.yield * crop.sellPrice - crop.seedPrice}</color></size>",
                    () => QuickBuy(crop.SeedItemId, crop.seedPrice), Color.white, Ink, 54f, 14.5f);
                b.name = "BuySeed_" + crop.id;
                b.interactable = afford;
            }
        }

        private void Do(string error, IslandWord phrase, string success)
        {
            if (error != null) { BuildFarm(error, true); return; }
            IslandState.Discover("verb:" + phrase.en);
            IslandAchievements.Check();
            BuildFarm(success, false);
            ActionFeedback(_plot != null ? _plot.transform.position : Vector3.zero);
        }

        private void ActionFeedback(Vector3 at)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (at == Vector3.zero || shader == null) return;
            var puff = new GameObject("FarmPuff").AddComponent<ParticleSystem>();
            puff.transform.position = at + Vector3.up * 0.3f;
            puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = puff.main; main.startLifetime = 0.8f; main.startSpeed = 1.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f); main.gravityModifier = 1.2f; main.duration = 0.4f; main.loop = false;
            main.startColor = new Color(0.6f, 0.45f, 0.3f, 0.9f);
            var emission = puff.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var shape = puff.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 0.5f;
            var renderer = puff.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(shader);
            puff.Play();
            Destroy(puff.gameObject, 2f);
        }

        private void UpdateGrowth()
        {
            if (_plot == null || _growFill == null) return;
            var phase = _plot.CurrentPhase;
            float stage = _plot.Record.stage;
            float total = phase == FarmPlot.Phase.Ready ? 1f : Mathf.Clamp01((stage + _plot.StageProgress) / 3f);
            UiKit.SetProgress(_growFill, _plot.Crop == null ? 0f : total);
            _growText.text = phase switch
            {
                FarmPlot.Phase.Growing => Loc.F("Giai đoạn tiếp theo sau {0} giây", "Next stage in {0}s", "次の段階まで{0}秒", Mathf.CeilToInt(_plot.SecondsLeft)),
                FarmPlot.Phase.NeedsWater => Loc.T("Cây khát nước — tưới để cây lớn tiếp", "Thirsty — water it to keep it growing", "のどが渇いています — 水をあげて育てましょう"),
                FarmPlot.Phase.Ready => Loc.T("Đã chín!", "Ripe!", "実りました！"),
                _ => "",
            };
        }

        private FarmPlot.Phase _lastPhase;
        private void Update()
        {
            if (_farmCard != null && _plot != null)
            {
                var phase = _plot.CurrentPhase;
                if (phase != _lastPhase) { _lastPhase = phase; BuildFarm(null, false); }
                UpdateGrowth();
            }
        }

        private static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ─────────── Animal card ───────────

        public static bool AnimalOpen => _instance != null && _instance._animalCard != null;
        private IslandAnimal _animal;

        public static void OpenAnimal(IslandAnimal animal)
        {
            var ui = Instance;
            bool wasOpen = ui._animalCard != null;
            ui._animal = animal;
            ui.BuildAnimal(null, false);
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._animalCard != null, () => ui.Close(ref ui._animalCard), "Animal");
            }
        }

        public static void CloseAnimal() { if (_instance != null) _instance.Close(ref _instance._animalCard); }

        private void BuildAnimal(string feedback, bool error)
        {
            Discard(_animalCard);
            var def = _animal.Def;
            var verbs = IslandCatalog.Load().verbs;
            _animalCard = Card("AnimalCard", new Vector2(1f, 0.5f), new Vector2(-28f, 20f), 460f);
            Header(_animalCard, "pets", IslandLanguage.Title(def.word), Loc.T("CHUỒNG TRẠI", "BARN", "なや"), CloseAnimal);
            var body = Body(_animalCard);
            WordChip(body, def.word, 20f);
            Text(body, $"「{IslandLanguage.Sound(def.sound)}」", 22f, Green, FontStyles.Italic);
            var actions = NLUi.Group(body, "Actions", true, 8f);
            if (def.foods != null && def.foods.Length > 0)
            {
                PhraseCard(body, verbs.feed);
                string foods = string.Join(" / ", def.foods.Select(f => IslandLanguage.Name(IslandCatalog.Load().Crop(f)?.word)));
                var feed = IconBtn(actions, "Feed", "nutrition", $"{Loc.T("Cho ăn", "Feed", "えさをあげる")}  ·  {foods}", () => Work(Loc.T("Đang cho ăn…", "Feeding…", "えさやり中…"), Loc.T("tay", "hands", "手"), FarmActionTimes.Feed, pose: "Work_Feed", done: () =>
                {
                    if (_animalCard == null) return;
                    string err = _animal.Feed(out string food);
                    if (err != null) { BuildAnimal(err, true); return; }
                    IslandState.Discover("verb:" + verbs.feed.en);
                    BuildAnimal(Loc.F("{0} ăn ngon lành!", "The {0} enjoys it!", "{0}がおいしそうに食べました！", IslandLanguage.Name(def.word)), false);
                }), Green, Color.white, 50f, 16.5f);
            }
            var pet = IconBtn(actions, "Pet", "favorite", Loc.T("Vuốt ve", "Pet", "なでる"), () =>
            {
                _animal.Pet();
                IslandState.Discover("verb:" + verbs.pet.en);
                BuildAnimal(Loc.F("{0} rất vui.", "The {0} is happy.", "{0}はうれしそうです。", IslandLanguage.Name(def.word)), false);
            }, Cream2, Green, 46f, 16f);
            Text(body, feedback ?? "", 15f, error ? Error : Green, FontStyles.Bold);
        }

        // ─────────── Shop ───────────

        public static bool ShopOpen => _instance != null && _instance._shop != null;
        private string _shopTab = "agriculture";
        private string _shopSelected;
        private int _shopQty = 1;
        private TextMeshProUGUI _shopFeedback;

        public static void OpenShop()
        {
            var ui = Instance;
            bool wasOpen = ui._shop != null;
            ui.BuildShop(null, false);
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._shop != null, () => ui.Close(ref ui._shop), "Island shop");
                IslandState.Discover("place:shop");
            }
        }

        /// <summary>Opens the store on a tab with an item selected (e.g. from the farm card's "Store" button).</summary>
        public static void OpenShopAt(string tab, string itemId)
        {
            var ui = Instance;
            ui._shopTab = tab;
            ui._shopSelected = itemId;
            ui._shopQty = 1;
            OpenShop();
        }

        public static void CloseShop() { if (_instance != null) _instance.Close(ref _instance._shop); }

        public sealed class ShopEntry { public string ItemId; public IslandWord Word; public int Price; public string Info; public bool Sell; }

        public static List<ShopEntry> Entries(string tab)
        {
            var c = IslandCatalog.Load();
            var list = new List<ShopEntry>();
            switch (tab)
            {
                case "agriculture":
                    foreach (var crop in c.crops)
                        list.Add(new ShopEntry { ItemId = crop.SeedItemId, Word = crop.word, Price = crop.seedPrice,
                            Info = Loc.F("Hạt giống · lớn trong {0} giây · thu {1} quả, bán ¥{2}/quả · lãi ¥{3}/ô", "Seeds · grow in {0}s · yield {1}, sell ¥{2} each · profit ¥{3} per plot",
                                "種 · {0}秒で育つ · {1}個収穫、1個¥{2}で売れる · 1区画の利益¥{3}", Mathf.RoundToInt(crop.secondsPerStage * 3), crop.yield, crop.sellPrice, crop.yield * crop.sellPrice - crop.seedPrice) });
                    foreach (var tool in c.tools)
                        list.Add(new ShopEntry { ItemId = tool.id, Word = tool.word, Price = tool.price,
                            Info = Loc.F("Dụng cụ · {0} · bền {1} lần dùng (≈¥{2:0.#}/lần)", "Tool · {0} · lasts {1} uses (≈¥{2:0.#} per use)", "道具 · {0} · {1}回使える（1回あたり約¥{2:0.#}）",
                                IslandLanguage.Name(tool.verb), tool.durability, tool.durability > 0 ? tool.price / (float)tool.durability : tool.price)
                                + (IslandEconomy.Owned(tool.id) > 0 ? "  ·  " + Loc.F("cái đang dùng: {0}/{1}", "current one: {0}/{1}", "使用中：{0}/{1}", IslandTools.Left(tool.id), tool.durability) : "") });
                    break;
                case "technology":
                    foreach (var p in c.products.Where(p => p.category == "technology"))
                        list.Add(new ShopEntry { ItemId = p.itemId, Word = p.word, Price = p.price, Info = p.Note });
                    break;
                case "decor":
                    foreach (var d in c.decor ?? Array.Empty<IslandDecorDef>())
                        list.Add(new ShopEntry { ItemId = d.ItemId, Word = d.word, Price = d.price,
                            Info = Loc.F("Đồ trang trí · vào kho trang trí (không chiếm balo) · đang có {0}, đã đặt {1}. Bấm \"Trang trí\" trên thanh công cụ để đặt.",
                                "Decoration · goes to your decor stock (not the bag) · {0} in stock, {1} placed. Press \"Decorate\" on the toolbar to place it.",
                                "かざり · かざりの倉庫へ（かばんは使わない）· 在庫{0}、設置{1}。ツールバーの「かざる」で置けます。",
                                IslandDecor.Stock(d.id), IslandDecor.Placed.Count(p => p.id == d.id)) });
                    break;
                case "sell":
                    foreach (var crop in c.crops.Where(cr => IslandEconomy.Owned(cr.ProduceItemId) > 0))
                        list.Add(new ShopEntry { ItemId = crop.ProduceItemId, Word = crop.word, Price = crop.sellPrice, Sell = true,
                            Info = Loc.F("Nông sản của bạn · đang có {0}", "Your produce · you have {0}", "あなたの作物 · 所持{0}", IslandEconomy.Owned(crop.ProduceItemId)) });
                    foreach (var fish in (c.fish ?? Array.Empty<IslandFish>()).Where(f => IslandEconomy.Owned(f.id) > 0))
                        list.Add(new ShopEntry { ItemId = fish.id, Word = fish.word, Price = fish.sellPrice, Sell = true,
                            Info = Loc.F("Cá bạn câu được · đang có {0}", "Fish you caught · you have {0}", "釣った魚 · 所持{0}", IslandEconomy.Owned(fish.id)) });
                    break;
            }
            return list;
        }

        public static void SelectShopTab(string tab) { var ui = Instance; ui._shopTab = tab; ui._shopSelected = null; ui._shopQty = 1; if (ui._shop != null) ui.BuildShop(null, false); }
        public static void SelectShopItem(string itemId) { var ui = Instance; ui._shopSelected = itemId; ui._shopQty = 1; if (ui._shop != null) ui.BuildShop(null, false); }
        public static void SetShopQuantity(int qty) { var ui = Instance; ui._shopQty = Mathf.Clamp(qty, 1, 20); if (ui._shop != null) ui.BuildShop(null, false); }

        /// <summary>Buys or sells the selected entry (also used by tests); returns the economy result.</summary>
        public static IslandEconomy.Result ConfirmShop()
        {
            var ui = Instance;
            var entry = Entries(ui._shopTab).FirstOrDefault(e => e.ItemId == ui._shopSelected);
            if (entry == null) return IslandEconomy.Result.Unknown;
            var verbs = IslandCatalog.Load().verbs;
            var result = entry.Sell ? IslandEconomy.Sell(entry.ItemId, entry.Price, ui._shopQty)
                : ui._shopTab == "decor" ? IslandDecor.Buy(entry.ItemId, ui._shopQty)
                : IslandEconomy.Buy(entry.ItemId, entry.Price, ui._shopQty);
            string name = IslandLanguage.Name(entry.Word);
            string message = result switch
            {
                IslandEconomy.Result.Ok when entry.Sell => Loc.F("Đã bán {0} {1} · +¥{2:N0}.", "Sold {0} {1} · +¥{2:N0}.", "{1}を{0}個売りました · +¥{2:N0}。", ui._shopQty, name, entry.Price * ui._shopQty),
                IslandEconomy.Result.Ok => Loc.F("Đã mua {0} {1} · −¥{2:N0}.", "Bought {0} {1} · −¥{2:N0}.", "{1}を{0}個買いました · −¥{2:N0}。", ui._shopQty, name, entry.Price * ui._shopQty),
                IslandEconomy.Result.NoMoney => Loc.F("Không đủ tiền: cần ¥{0:N0}, bạn có ¥{1:N0}.", "Not enough money: ¥{0:N0} needed, you have ¥{1:N0}.", "お金が足りません：¥{0:N0}必要、所持¥{1:N0}。", entry.Price * ui._shopQty, IslandEconomy.Yen),
                IslandEconomy.Result.BagFull => Loc.T("Balo đầy — bỏ bớt đồ hoặc bán nông sản.", "Your bag is full — drop something or sell produce.", "バッグがいっぱいです。物を減らすか作物を売りましょう。"),
                IslandEconomy.Result.NotOwned => Loc.T("Bạn không có đủ món này để bán.", "You do not have enough of this to sell.", "売る数が足りません。"),
                _ => Loc.T("Không thực hiện được.", "That did not work.", "できませんでした。"),
            };
            if (result == IslandEconomy.Result.Ok)
            {
                IslandState.Discover(entry.Sell ? "verb:" + verbs.sell.en : "verb:" + verbs.buy.en);
                IslandState.Discover(WordKey(entry.ItemId));
                if (entry.Sell && IslandEconomy.Owned(entry.ItemId) == 0) ui._shopSelected = null;
            }
            if (ui._shop != null) ui.BuildShop(message, result != IslandEconomy.Result.Ok);
            return result;
        }

        private static readonly (string id, string icon, string vi, string en, string ja)[] ShopTabs =
        {
            ("agriculture", "agriculture", "Nông nghiệp", "Farming", "農業"),
            ("fashion", "checkroom", "Thời trang", "Fashion", "ファッション"),
            ("decor", "yard", "Trang trí", "Decor", "かざり"),
            ("technology", "devices", "Công nghệ", "Technology", "テクノロジー"),
            ("sell", "sell", "Bán", "Sell", "売る"),
            ("prices", "price_check", "Bảng giá & lãi", "Prices & profit", "価格と利益"),
        };

        private void BuildShop(string feedback, bool error)
        {
            Discard(_shop);
            _shop = Card("IslandShop", new Vector2(0.5f, 0.5f), Vector2.zero, 1180f);
            var header = Header(_shop, "storefront", Loc.T("Cửa hàng Midori", "Midori Store", "みどりしょうてん"), Loc.T("ĐẢO XANH", "MIDORI ISLAND", "みどりじま"), CloseShop);
            var wallet = NLUi.Panel(header, "Wallet", new Color(0f, 0f, 0f, 0.22f), new RectOffset(12, 16, 6, 6), 6f, vertical: false);
            wallet.GetComponent<Image>().sprite = UiKit.Rounded(16);
            wallet.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            NLUi.Anchor(wallet, new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(150f, 40f));
            wallet.pivot = new Vector2(1f, 0.5f);
            ((HorizontalLayoutGroup)wallet.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            Coin(wallet, IslandEconomy.Yen, 19f, new Color(1f, 0.92f, 0.62f));

            var main = NLUi.Group(_shop, "Main", false, 18f, TextAnchor.UpperLeft, false);
            main.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(24, 24, 18, 0);
            // Tabs
            var tabs = NLUi.Group(main, "Tabs", true, 8f);
            NLUi.Size(tabs, 210f);
            foreach (var (id, icon, vi, en, ja) in ShopTabs)
            {
                bool on = _shopTab == id;
                string captured = id;
                var tab = IconBtn(tabs, "Tab_" + id, icon, Loc.T(vi, en, ja), () => SelectShopTab(captured), on ? Green : Cream2, on ? Color.white : Ink, 52f, 15.5f);
            }

            if (_shopTab == "prices") { BuildPriceGuide(main); FinishShop(feedback, error); return; }

            // Grid
            var gridHost = NLUi.Group(main, "GridHost", true, 10f);
            NLUi.Size(gridHost, 540f);
            var entries = Entries(_shopTab);
            if (_shopTab == "fashion")
            {
                var empty = NLUi.Panel(gridHost, "Empty", Cream2, new RectOffset(20, 20, 18, 18), 6f);
                empty.GetComponent<Image>().sprite = UiKit.Rounded(14);
                Text(empty, Loc.T("Chưa có hàng thời trang", "No fashion items yet", "ファッション商品はまだありません"), 20f, Ink, FontStyles.Bold);
                Text(empty, Loc.T("Dự án chưa có mô hình quần áo / phụ kiện nên mục này để trống thay vì bán đồ không có thật. Trang phục sẽ được thêm khi có mô hình phù hợp.",
                    "There are no clothing models yet, so this shelf stays empty instead of selling things that do not exist. Outfits will come with suitable models.",
                    "服やアクセサリーのモデルがまだないため、存在しない商品は売らずに空けてあります。合うモデルができたら追加します。"), 15f, Muted);
            }
            else if (entries.Count == 0)
            {
                var empty = NLUi.Panel(gridHost, "Empty", Cream2, new RectOffset(20, 20, 18, 18), 6f);
                empty.GetComponent<Image>().sprite = UiKit.Rounded(14);
                Text(empty, _shopTab == "sell" ? Loc.T("Chưa có nông sản để bán — thu hoạch ở ruộng trước nhé.", "Nothing to sell yet — harvest in the fields first.", "まだ売るものがありません — 先に畑で収穫しましょう。")
                                               : Loc.T("Chưa có mặt hàng.", "No items yet.", "商品がありません。"), 17f, Muted);
            }
            else
            {
                var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                gridGo.transform.SetParent(gridHost, false);
                var grid = gridGo.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(126f, 158f); grid.spacing = new Vector2(10f, 10f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
                gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                foreach (var e in entries)
                {
                    bool selected = e.ItemId == _shopSelected;
                    string id = e.ItemId;
                    var cell = new GameObject("Item_" + e.ItemId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
                    cell.transform.SetParent(gridGo.transform, false);
                    var bg = cell.GetComponent<Image>();
                    bg.sprite = UiKit.Rounded(14);
                    bg.type = Image.Type.Sliced;
                    bg.color = selected ? GreenSoft : Color.white;
                    if (selected) UiKit.Border((RectTransform)cell.transform, Green, 14);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectShopItem(id));
                    var v = cell.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(8, 8, 10, 8); v.spacing = 3f; v.childAlignment = TextAnchor.UpperCenter; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
                    Icon((RectTransform)cell.transform, e.ItemId, 72f);
                    var name = Text((RectTransform)cell.transform, IslandLanguage.Capitalize(ItemName(e.ItemId)), 14.5f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                    name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
                    var priceRow = NLUi.Group((RectTransform)cell.transform, "Price", false, 4f, TextAnchor.MiddleCenter, false);
                    Coin(priceRow, e.Price, 15f, e.Sell ? Green : new Color(0.62f, 0.42f, 0.06f), e.Sell);
                }
            }

            // Detail
            var detail = NLUi.Panel(main, "Detail", Cream2, new RectOffset(20, 20, 18, 18), 10f);
            detail.GetComponent<Image>().sprite = UiKit.Rounded(16);
            NLUi.Size(detail, 300f);
            var entry = entries.FirstOrDefault(e => e.ItemId == _shopSelected);
            if (entry == null)
            {
                Text(detail, _shopTab == "sell" ? Loc.T("Chọn nông sản để bán.", "Pick something to sell.", "売るものを選びましょう。")
                                                : Loc.T("Chọn một món để xem chi tiết.", "Pick an item to see the details.", "商品を選ぶと詳しく表示されます。"), 16f, Muted);
            }
            else
            {
                var iconRow = NLUi.Group(detail, "IconRow", false, 0f, TextAnchor.MiddleCenter, false);
                Icon(iconRow, entry.ItemId, 118f);
                Text(detail, IslandLanguage.Capitalize(ItemName(entry.ItemId)), 22f, Ink, FontStyles.Bold);
                WordChip(detail, entry.Word, 17f);
                Text(detail, entry.Info, 14f, Muted);
                Text(detail, Loc.F("Đang có: <b>{0}</b>", "You have: <b>{0}</b>", "所持：<b>{0}</b>", IslandEconomy.Owned(entry.ItemId)), 15f, Ink);
                var qtyRow = NLUi.Group(detail, "Qty", false, 8f, TextAnchor.MiddleCenter, false);
                var minus = Btn(qtyRow, "−", () => SetShopQuantity(_shopQty - 1), Color.white, Ink, 40f, 20f); NLUi.Size(minus, 46f, 40f);
                var q = Text(qtyRow, _shopQty.ToString(), 20f, Ink, FontStyles.Bold, TextAlignmentOptions.Center); NLUi.Size(q, 50f);
                var plus = Btn(qtyRow, "+", () => SetShopQuantity(_shopQty + 1), Color.white, Ink, 40f, 20f); NLUi.Size(plus, 46f, 40f);
                bool canAfford = entry.Sell ? IslandEconomy.Owned(entry.ItemId) >= _shopQty : IslandEconomy.Yen >= entry.Price * _shopQty;
                var totalRow = NLUi.Group(detail, "Total", false, 6f, TextAnchor.MiddleLeft, false);
                Text(totalRow, entry.Sell ? Loc.T("Bạn nhận", "You get", "受け取り") : Loc.T("Tổng", "Total", "合計"), 15f, Muted);
                Coin(totalRow, entry.Price * _shopQty, 20f, entry.Sell ? Green : new Color(0.62f, 0.42f, 0.06f), entry.Sell);
                var confirm = IconBtn(detail, "Confirm", entry.Sell ? "sell" : "shopping_cart", entry.Sell ? Loc.T("Bán", "Sell", "売る") : Loc.T("Mua", "Buy", "買う"),
                    () => ConfirmShop(), canAfford ? Green : new Color(0.62f, 0.64f, 0.6f), Color.white, 52f, 18f);
                if (!canAfford) Text(detail, entry.Sell ? Loc.T("Không đủ số lượng.", "Not enough to sell.", "数が足りません。")
                                                        : Loc.F("Thiếu ¥{0:N0}.", "¥{0:N0} short.", "¥{0:N0}足りません。", entry.Price * _shopQty - IslandEconomy.Yen), 14f, Error);
            }
            FinishShop(feedback, error);
        }

        private void FinishShop(string feedback, bool error)
        {
            _shopFeedback = Text(_shop, feedback ?? "", 15.5f, error ? Error : Green, FontStyles.Bold, TextAlignmentOptions.Center);
            _shopFeedback.margin = new Vector4(24f, 6f, 24f, 0f);
        }

        /// <summary>
        /// How money works on the island, in one table: what each seed costs, how long it grows, what it sells for and
        /// the profit per plot; what each tool costs per use; what fish sell for; the return ticket.
        /// </summary>
        private void BuildPriceGuide(RectTransform main)
        {
            var c = IslandCatalog.Load();
            var host = NLUi.Group(main, "PriceGuide", true, 10f);
            NLUi.Size(host, 860f);
            Text(host, Loc.T("Trồng gì có lãi?", "What pays to grow?", "何を育てるともうかる？"), 19f, Ink, FontStyles.Bold);
            var table = NLUi.Group(host, "Crops", true, 4f);
            Row(table, true, Loc.T("Cây", "Crop", "作物"), Loc.T("Hạt", "Seed", "種"), Loc.T("Thời gian", "Time", "時間"), Loc.T("Thu × giá bán", "Yield × price", "収穫 × 単価"), Loc.T("Lãi / ô", "Profit / plot", "1区画の利益"), null);
            var best = c.crops.OrderByDescending(x => x.yield * x.sellPrice - x.seedPrice).First();
            foreach (var crop in c.crops)
            {
                int profit = crop.yield * crop.sellPrice - crop.seedPrice;
                Row(table, false, IslandLanguage.Title(crop.word), $"¥{crop.seedPrice}", Loc.F("{0} giây", "{0}s", "{0}秒", Mathf.RoundToInt(crop.secondsPerStage * 3)),
                    $"{crop.yield} × ¥{crop.sellPrice}", (profit >= 0 ? "+" : "") + $"¥{profit}", crop.SeedItemId, crop == best);
            }
            Text(host, Loc.T("Dụng cụ (dùng mòn dần)", "Tools (wear out with use)", "道具（使うと減ります）"), 17f, Ink, FontStyles.Bold);
            var tools = NLUi.Group(host, "Tools", true, 4f);
            Row(tools, true, Loc.T("Dụng cụ", "Tool", "道具"), Loc.T("Giá", "Price", "価格"), Loc.T("Số lần", "Uses", "回数"), Loc.T("Mỗi lần", "Per use", "1回あたり"), Loc.T("Dùng để", "For", "用途"), null);
            foreach (var tool in c.tools)
                Row(tools, false, IslandLanguage.Title(tool.word), $"¥{tool.price}", tool.durability > 0 ? tool.durability.ToString() : "∞",
                    tool.durability > 0 ? $"¥{tool.price / (float)tool.durability:0.#}" : "—", IslandLanguage.Name(tool.verb), tool.id);
            var tips = NLUi.Panel(host, "Tips", GreenSoft, new RectOffset(18, 18, 12, 14), 4f);
            tips.GetComponent<Image>().sprite = UiKit.Rounded(14);
            Text(tips, Loc.T("Quy tắc tiền trên đảo", "How money works here", "島のお金のルール"), 14.5f, Green, FontStyles.Bold);
            Text(tips, Loc.F("• Quà chào mừng: cuốc, bình tưới và 3 túi hạt cà rốt — không cần mua gì để bắt đầu.\n• Mua hạt → gieo → tưới 3 lần → thu hoạch → bán ở đây. {0} lãi nhiều nhất mỗi ô.\n• Dụng cụ hỏng sau số lần dùng; giá mỗi lần nằm trong lãi. Cá câu được bán ¥{1}–¥{2}.\n• Vé tàu về Hibari: ¥{3}. Mọi khoản thu chi đều ghi trong lịch sử ví.",
                    "• Welcome gift: a hoe, a watering can and 3 bags of carrot seeds — nothing to buy to start.\n• Buy seeds → plant → water 3 times → harvest → sell here. {0} earns the most per plot.\n• Tools break after their uses; their cost per use is part of the profit. Fish sell for ¥{1}–¥{2}.\n• Train ticket to Hibari: ¥{3}. Every income and expense is in the wallet history.",
                    "• ようこそギフト：くわ、じょうろ、にんじんの種3袋 — 何も買わずに始められます。\n• 種を買う → まく → 水やり3回 → 収穫 → ここで売る。1区画で一番もうかるのは{0}。\n• 道具は回数を使うと壊れます。1回あたりの費用も利益に入ります。魚は¥{1}〜¥{2}で売れます。\n• ひばり行きの切符：¥{3}。収入と支出はすべて財布の履歴に残ります。",
                    IslandLanguage.Name(best.word), (c.fish ?? Array.Empty<IslandFish>()).Select(f => f.sellPrice).DefaultIfEmpty(0).Min(), (c.fish ?? Array.Empty<IslandFish>()).Select(f => f.sellPrice).DefaultIfEmpty(0).Max(), c.ticketPrice), 14f, Ink);
        }

        private void Row(RectTransform table, bool head, string a, string b, string cText, string d, string e, string iconItem, bool highlight = false)
        {
            var row = NLUi.Panel(table, head ? "Head" : "Row", head ? new Color(0f, 0f, 0f, 0.06f) : highlight ? GreenSoft : Color.white, new RectOffset(12, 12, 6, 6), 10f, vertical: false);
            row.GetComponent<Image>().sprite = UiKit.Rounded(10);
            ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            if (!head && iconItem != null) Icon(row, iconItem, 30f);
            else if (!head) { var sp = UiKit.Rect(row, "Spacer"); UiKit.Size(sp, 30f, 30f); }
            else { var sp = UiKit.Rect(row, "Spacer"); UiKit.Size(sp, 30f, 18f); }
            float[] widths = { 170f, 90f, 110f, 170f, 200f };
            string[] cells = { a, b, cText, d, e };
            for (int i = 0; i < cells.Length; i++)
            {
                var t = Text(row, cells[i] ?? "", head ? 13f : 15f, head ? Muted : i == 4 && !head ? Green : Ink, head || i == 0 || i == 4 ? FontStyles.Bold : FontStyles.Normal);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
                NLUi.Size(t, widths[i]);
            }
        }

        // ─────────── Notebook (island progress) ───────────

        public static bool BookOpen => _instance != null && _instance._book != null;

        public static void OpenProgress()
        {
            var ui = Instance;
            bool wasOpen = ui._book != null;
            ui.BuildBook();
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._book != null, () => ui.Close(ref ui._book), "Island notebook");
            }
        }

        public static void CloseProgress() { if (_instance != null) _instance.Close(ref _instance._book); }

        private void BuildBook()
        {
            Discard(_book);
            var record = IslandState.Record;
            _book = Card("IslandNotebook", new Vector2(0.5f, 0.5f), Vector2.zero, 1000f);
            Header(_book, "menu_book", Loc.T("Sổ tay Đảo Xanh", "Midori Island notebook", "みどりじまノート"), Loc.T("TIẾN BỘ", "PROGRESS", "記録"), CloseProgress);
            var body = Body(_book, 14f);
            var stats = NLUi.Group(body, "Stats", false, 12f, TextAnchor.MiddleLeft, true);
            foreach (var (icon, n, vi, en, ja) in new[]
                     {
                         ("nutrition", record.harvested.ToString(), "lần thu hoạch", "harvests", "収穫"),
                         ("sell", record.sold.ToString(), "nông sản đã bán", "sold", "販売"),
                         ("payments", $"¥{record.earned:N0}", "tiền kiếm được", "earned", "かせいだお金"),
                         ("translate", DistinctWords().Count.ToString(), "từ đã học", "words", "覚えた言葉"),
                         ("pets", record.animalsMet.Count + "/5", "con vật quen", "animal friends", "動物の友だち"),
                     })
            {
                var tile = NLUi.Panel(stats, "Stat", Color.white, new RectOffset(14, 14, 10, 12), 2f);
                tile.GetComponent<Image>().sprite = UiKit.Rounded(14);
                var ic = UiKit.Icon(tile, icon, Green, 24f);
                UiKit.Size(ic, 24f, 24f);
                Text(tile, n, 26f, Green, FontStyles.Bold);
                Text(tile, Loc.T(vi, en, ja), 13f, Muted);
            }
            Text(body, $"{Loc.T("Từ vựng", "Vocabulary", "語彙")}  <size=75%><color=#6B7566>({Loc.T("đang học", "learning", "学習中")} {IslandLanguage.LanguageLabel})</color></size>", 18f, Ink, FontStyles.Bold);
            var gridGo = new GameObject("Words", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridGo.transform.SetParent(body, false);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(182f, 70f); grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
            gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var words = DistinctWords().Take(25).ToList();
            if (words.Count == 0) Text(body, Loc.T("Chưa có từ nào — trồng cây, cho thú ăn và mua sắm để học từ mới.", "No words yet — farm, feed the animals and shop to learn new words.", "まだ言葉がありません — 畑仕事・えさやり・買い物で新しい言葉を覚えましょう。"), 15f, Muted);
            foreach (var w in words)
            {
                var cell = new GameObject("Word", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                cell.transform.SetParent(gridGo.transform, false);
                var bg = cell.GetComponent<Image>(); bg.sprite = UiKit.Rounded(12); bg.type = Image.Type.Sliced; bg.color = Color.white;
                var v = cell.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(12, 10, 6, 6); v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
                var primary = Text((RectTransform)cell.transform, IslandLanguage.Primary(w), 16f, Ink, FontStyles.Bold);
                primary.textWrappingMode = TextWrappingModes.NoWrap; primary.overflowMode = TextOverflowModes.Ellipsis;
                string reading = IslandLanguage.Reading(w) ?? "";
                string meaning = IslandLanguage.Helper(w);
                var second = Text((RectTransform)cell.transform, string.Join(" · ", new[] { reading, meaning }.Where(x => !string.IsNullOrEmpty(x))), 12f, Muted, FontStyles.Italic);
                second.textWrappingMode = TextWrappingModes.NoWrap; second.overflowMode = TextOverflowModes.Ellipsis;
            }
            Text(body, Loc.T("Thành tích", "Achievements", "実績"), 18f, Ink, FontStyles.Bold);
            var ach = NLUi.Group(body, "Achievements", false, 10f, TextAnchor.MiddleLeft, true);
            foreach (var def in IslandAchievements.All)
            {
                bool done = record.achievements.Contains(def.Id);
                var tile = NLUi.Panel(ach, "A_" + def.Id, done ? GreenSoft : new Color(0.93f, 0.91f, 0.87f, 1f), new RectOffset(12, 12, 8, 8), 2f);
                tile.GetComponent<Image>().sprite = UiKit.Rounded(12);
                Text(tile, (done ? "<color=#2E6B45>✓</color> " : "<color=#9AA095>○</color> ") + def.Title, 14f, done ? Ink : Muted, FontStyles.Bold);
            }
        }

        /// <summary>Learned words without duplicates (a seed, its crop and its produce are one word).</summary>
        private static List<IslandWord> DistinctWords()
        {
            var seen = new HashSet<IslandWord>();
            var list = new List<IslandWord>();
            foreach (var key in IslandState.Record.words)
            {
                var word = Lookup(key);
                if (word != null && seen.Add(word)) list.Add(word);
            }
            return list;
        }

        /// <summary>Vocabulary key for a shop item: seeds and produce teach their crop's word.</summary>
        private static string WordKey(string itemId)
        {
            var crop = IslandCatalog.Load().crops.FirstOrDefault(c => c.SeedItemId == itemId || c.ProduceItemId == itemId);
            if (crop != null) return "crop:" + crop.id;
            var decor = IslandCatalog.Load().Decor(itemId);
            if (decor != null) return "decor:" + decor.id;
            return IslandCatalog.Load().Fish(itemId) != null ? "fish:" + itemId : "item:" + itemId;
        }

        private static IslandWord Lookup(string key)
        {
            var c = IslandCatalog.Load();
            int colon = key.IndexOf(':');
            if (colon < 0) return null;
            string kind = key.Substring(0, colon), id = key.Substring(colon + 1);
            switch (kind)
            {
                case "crop": return c.Crop(id)?.word;
                case "decor": return c.Decor(id)?.word;
                case "animal": return c.Animal(id)?.word;
                case "fish": return c.Fish(id)?.word;
                case "item":
                    if (c.Tool(id) != null) return c.Tool(id).word;
                    var crop = c.crops.FirstOrDefault(x => x.SeedItemId == id || x.ProduceItemId == id);
                    if (crop != null) return crop.word;
                    return c.products.FirstOrDefault(p => p.itemId == id)?.word;
                case "place":
                    var p2 = c.places;
                    return id switch { "station" => p2.station, "farm" => p2.farm, "shop" => p2.shop, "barn" => p2.barn, "view" => p2.view, "pier" => p2.pier ?? p2.island, _ => p2.island };
                case "verb":
                    var v = c.verbs;
                    foreach (var w in new[] { v.plant, v.harvest, v.feed, v.pet, v.buy, v.sell }) if (w.en == id) return w;
                    foreach (var t in c.tools) if (t.verb.en == id) return t.verb;
                    return null;
            }
            return null;
        }

        // ─────────── Return trip screen ───────────

        public static IEnumerator TravelScreen(string from, string to, float seconds)
        {
            var ui = Instance;
            ui.Discard(ui._travel);
            var root = new GameObject("TravelScreen", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            root.SetParent(ui._canvas.transform, false);
            NLUi.Stretch(root);
            root.GetComponent<Image>().color = new Color(0.07f, 0.15f, 0.11f, 0.97f);
            ui._travel = root;
            var icon = UiKit.Icon(root, "train", new Color(0.95f, 0.85f, 0.55f), 64f);
            UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), 0f, 190f, 64f, 64f);
            var title = NLUi.Label(root, "Title", Loc.T("Tuyến Midori", "Midori Line", "みどり線"), 30f, new Color(0.95f, 0.85f, 0.55f), ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 50f));
            var sub = NLUi.Label(root, "Sub", $"{from}  →  {to}", 24f, Color.white, ui._font, FontStyles.Normal, TextAlignmentOptions.Center);
            NLUi.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(900f, 40f));
            var track = UiKit.Image(root, "Track", new Color(1f, 1f, 1f, 0.2f), UiKit.Rounded(3));
            UiKit.Pin(track.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 700f, 6f);
            var fill = UiKit.Image(track.transform, "Fill", new Color(0.95f, 0.85f, 0.55f), UiKit.Rounded(3));
            var fr = fill.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0f, 1f); fr.offsetMin = fr.offsetMax = Vector2.zero;
            var train = UiKit.Icon(root, "train", new Color(0.86f, 0.93f, 0.82f), 40f);
            var note = NLUi.Label(root, "Note", Loc.T("Tàu đang chạy…", "The train is on its way…", "でんしゃは はしって います…"), 18f, new Color(0.8f, 0.86f, 0.8f), ui._font, FontStyles.Italic, TextAlignmentOptions.Center);
            NLUi.Anchor(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 40f));
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                UiKit.Pin(train.rectTransform, new Vector2(0.5f, 0.5f), Mathf.Lerp(-350f, 350f, k), 28f, 40f, 40f);
                fr.anchorMax = new Vector2(k, 1f);
                yield return null;
            }
        }

        public static void EndTravelScreen()
        {
            if (_instance != null && _instance._travel != null) Destroy(_instance._travel.gameObject);
        }
    }
}
