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
    public sealed partial class IslandUI : MonoBehaviour
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
            var player = SceneRefs.Find<PlayerController>();
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





    }
}
