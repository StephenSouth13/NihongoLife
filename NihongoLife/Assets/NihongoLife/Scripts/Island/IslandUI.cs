using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>
    /// Midori Island interface: toolbar, welcome banner, farm card, animal card, shop, notebook (progress), word toasts
    /// and the return-trip travel screen. One visual language — warm cream surfaces, deep natural green, restrained gold —
    /// built on NLUi. Every window registers with UiModalStack (Esc closes the top one) and locks walking while open.
    /// </summary>
    public sealed class IslandUI : MonoBehaviour
    {
        // ─────────── Palette ───────────
        public static readonly Color Cream = new Color(0.985f, 0.968f, 0.918f, 0.985f);
        public static readonly Color Cream2 = new Color(0.945f, 0.918f, 0.85f, 1f);
        public static readonly Color Green = new Color(0.16f, 0.36f, 0.24f, 1f);
        public static readonly Color GreenSoft = new Color(0.86f, 0.92f, 0.83f, 1f);
        public static readonly Color Gold = new Color(0.83f, 0.63f, 0.2f, 1f);
        public static readonly Color Ink = new Color(0.16f, 0.2f, 0.15f, 1f);
        public static readonly Color Muted = new Color(0.42f, 0.46f, 0.4f, 1f);
        public static readonly Color Error = new Color(0.76f, 0.27f, 0.22f, 1f);
        public static readonly Color Water = new Color(0.22f, 0.52f, 0.78f, 1f);

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
            ui._font = NLUi.ResolveFont();
            ui._canvas = NLUi.CreateCanvas("IslandCanvas", 520, go.transform);
            ui._toasts = Column((RectTransform)ui._canvas.transform, "Toasts", 8f);
            // Left column under the quest panel: never under the right-hand farm / animal cards.
            NLUi.Anchor(ui._toasts, new Vector2(0f, 1f), new Vector2(28f, -215f), new Vector2(400f, 0f));
            ui._toasts.pivot = new Vector2(0f, 1f);
            NLUi.FitContent(ui._toasts);
            return ui;
        }

        private void OnDestroy()
        {
            IslandState.Changed -= RefreshToolbar;
            IslandLanguage.Changed -= OnLanguage;
            if (_instance == this) _instance = null;
            LockPlayer(false, force: true);
        }

        public static void DestroyAll()
        {
            if (_instance != null) Destroy(_instance.gameObject);
            _instance = null;
        }

        // ─────────── Shared pieces ───────────

        private static RectTransform Column(RectTransform parent, string name, float spacing) => NLUi.Group(parent, name, true, spacing);

        private RectTransform Card(string name, Vector2 anchor, Vector2 position, float width)
        {
            if (name != "Welcome") { Discard(_welcome); _welcome = null; } // the welcome banner never sits over a window
            var card = NLUi.Panel(_canvas.transform, name, Cream, new RectOffset(0, 0, 0, 18), 0f);
            NLUi.Anchor(card, anchor, position, new Vector2(width, 0f));
            NLUi.FitContent(card);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -6f);
            return card;
        }

        private RectTransform Header(RectTransform card, string title, string kicker, Action close)
        {
            var header = NLUi.Panel(card, "Header", Green, new RectOffset(24, 70, 14, 14), 2f);
            if (!string.IsNullOrEmpty(kicker)) NLUi.Label(header, "Kicker", kicker, 14f, new Color(0.86f, 0.93f, 0.82f), _font, FontStyles.Bold);
            NLUi.Label(header, "Title", title, 24f, Color.white, _font, FontStyles.Bold);
            NLUi.CloseButton(header, _font, close, 40f, 12f);
            return header;
        }

        private RectTransform Body(RectTransform card, float spacing = 12f)
        {
            var body = NLUi.Group(card, "Body", true, spacing);
            var layout = body.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 16, 0);
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

        /// <summary>Target-language word with its reading; the Vietnamese meaning appears on demand.</summary>
        private void WordBlock(RectTransform parent, IslandWord word, float size, string extraHelper = null)
        {
            var row = NLUi.Group(parent, "Word", true, 0f);
            Text(row, IslandLanguage.Primary(word), size, Ink, FontStyles.Bold);
            string reading = IslandLanguage.Reading(word);
            if (!string.IsNullOrEmpty(reading)) Text(row, reading, size * 0.55f, Muted, FontStyles.Italic);
            var line = NLUi.Group(row, "Meaning", false, 8f, TextAnchor.MiddleLeft, false);
            TextMeshProUGUI meaning = null;
            var reveal = Btn(line, "<b>Nghĩa?</b>", null, GreenSoft, Green, 30f, 13.5f);
            NLUi.Size(reveal, 92f, 30f);
            meaning = Text(line, "", 15f, Muted);
            reveal.onClick.AddListener(() =>
            {
                meaning.text = IslandLanguage.Helper(word) + (string.IsNullOrEmpty(extraHelper) ? "" : "  ·  " + extraHelper);
                reveal.gameObject.SetActive(false);
            });
        }

        private void PhraseCard(RectTransform parent, IslandWord phrase, string label)
        {
            if (phrase == null) return;
            var card = NLUi.Panel(parent, "Phrase", GreenSoft, new RectOffset(16, 16, 10, 12), 2f);
            Text(card, label, 13f, Green, FontStyles.Bold);
            WordBlock(card, phrase, 20f);
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

        public static void Toast(string message, bool error = false)
        {
            var ui = Instance;
            var card = NLUi.Panel(ui._toasts, "Toast", error ? new Color(0.98f, 0.9f, 0.88f, 0.98f) : Cream, new RectOffset(18, 18, 12, 12), 2f);
            var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            bar.transform.SetParent(card, false);
            bar.GetComponent<LayoutElement>().ignoreLayout = true;
            var r = (RectTransform)bar.transform; r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(0f, 1f); r.sizeDelta = new Vector2(6f, 0f); r.pivot = new Vector2(0f, 0.5f);
            bar.GetComponent<Image>().color = error ? Error : Green;
            ui.Text(card, message, 16f, error ? Error : Ink);
            ui.StartCoroutine(ui.FadeOut(card, 4.5f));
        }

        public static void WordToast(IslandWord word, string kicker)
        {
            var ui = Instance;
            var card = NLUi.Panel(ui._toasts, "WordToast", Cream, new RectOffset(18, 18, 12, 12), 2f);
            ui.Text(card, kicker.ToUpperInvariant(), 12.5f, Gold, FontStyles.Bold);
            ui.Text(card, IslandLanguage.Primary(word), 26f, Green, FontStyles.Bold);
            string reading = IslandLanguage.Reading(word) ?? "";
            ui.Text(card, (reading.Length > 0 ? reading + "  ·  " : "") + IslandLanguage.Helper(word), 15f, Muted);
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
            var card = ui.Card("Welcome", new Vector2(0.5f, 1f), new Vector2(0f, -110f), 720f);
            card.pivot = new Vector2(0.5f, 1f);
            var head = NLUi.Panel(card, "Head", ui.WelcomeGreen(), new RectOffset(28, 28, 18, 16), 2f);
            var places = IslandCatalog.Load().places;
            NLUi.Label(head, "Kicker", "ようこそ · WELCOME", 14f, new Color(0.95f, 0.85f, 0.55f), ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(head, "Title", places.island.ja + "  ·  " + places.island.en, 34f, Color.white, ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(head, "Sub", "Đảo Xanh", 18f, new Color(0.86f, 0.93f, 0.82f), ui._font, FontStyles.Normal, TextAlignmentOptions.Center);
            var body = ui.Body(card, 6f);
            ui.Text(body, "Ruộng ở phía trước, chuồng thú bên phải, cửa hàng Midori cạnh ga. Đến gần và nhấn <b>F</b> để tương tác; <b>P</b> mở cửa hàng.", 16.5f, Ink, FontStyles.Normal, TextAlignmentOptions.Center);
            ui._welcome = card;
            ui.StartCoroutine(ui.FadeOut(card, 7f));
        }

        private Color WelcomeGreen() => new Color(0.13f, 0.32f, 0.22f, 1f);

        // ─────────── Toolbar ───────────

        public static void ShowToolbar()
        {
            var ui = Instance;
            ui.BuildToolbar();
            IslandState.Changed -= ui.RefreshToolbar;
            IslandState.Changed += ui.RefreshToolbar;
            IslandLanguage.Changed -= ui.OnLanguage;
            IslandLanguage.Changed += ui.OnLanguage;
        }

        private void OnLanguage(TargetLanguage _) => BuildToolbar();
        private void RefreshToolbar() => BuildToolbar();

        private void BuildToolbar()
        {
            if (_canvas == null) return;
            Discard(_toolbar);
            _toolbar = NLUi.Panel(_canvas.transform, "IslandToolbar", Cream, new RectOffset(16, 16, 8, 8), 10f, vertical: false);
            NLUi.Anchor(_toolbar, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(0f, 60f));
            _toolbar.pivot = new Vector2(0.5f, 1f);
            ((HorizontalLayoutGroup)_toolbar.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.FitContent(_toolbar, width: true, height: false);
            foreach (var tool in IslandCatalog.Load().tools)
            {
                bool owned = IslandEconomy.Owned(tool.id) > 0;
                var slot = NLUi.Panel(_toolbar, "Tool_" + tool.id, owned ? GreenSoft : new Color(0.9f, 0.88f, 0.83f, 1f), new RectOffset(6, 10, 4, 4), 6f, vertical: false);
                ((HorizontalLayoutGroup)slot.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                var icon = Icon(slot, tool.id, 36f);
                if (!owned) icon.color = new Color(1f, 1f, 1f, 0.35f);
                var label = Text(slot, IslandLanguage.Primary(tool.word), 15f, owned ? Ink : Muted);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                NLUi.Size(label, 78f);
            }
            var lang = Btn(_toolbar, $"Học: <b>{IslandLanguage.LanguageLabel}</b>  ⇄", ToggleLanguage, Green, Color.white, 44f, 16f);
            NLUi.Size(lang, 160f, 44f);
            var book = Btn(_toolbar, $"Sổ tay  ({DistinctWords().Count} từ)", OpenProgress, Cream2, Green, 44f, 16f);
            NLUi.Size(book, 150f, 44f);
            var shop = Btn(_toolbar, "Cửa hàng · P", OpenShop, Gold, new Color(0.18f, 0.12f, 0.02f), 44f, 16f);
            NLUi.Size(shop, 132f, 44f);
        }

        public static void ToggleLanguage()
        {
            IslandLanguage.Target = IslandLanguage.Target == TargetLanguage.Japanese ? TargetLanguage.English : TargetLanguage.Japanese;
            Toast(IslandLanguage.Target == TargetLanguage.English ? "Đang học tiếng Anh — tên đồ vật và câu hành động chuyển sang English." : "Đang học tiếng Nhật — 日本語で 学びます。");
        }

        // ─────────── Farm card ───────────

        private FarmPlot _plot;
        private Image _growFill;
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

        private void BuildFarm(string feedback, bool error)
        {
            Discard(_farmCard);
            var plot = _plot;
            var catalog = IslandCatalog.Load();
            var phase = plot.CurrentPhase;
            _lastPhase = phase; // the card always reflects this phase; Update rebuilds only on a real change
            var crop = plot.Crop;
            _farmCard = Card("FarmCard", new Vector2(1f, 0.5f), new Vector2(-28f, 20f), 460f);
            Header(_farmCard, $"{(IslandLanguage.Target == TargetLanguage.English ? "Field" : "はたけ")} {plot.Number}", "Ô RUỘNG · " + catalog.places.farm.ja, CloseFarm);
            var body = Body(_farmCard);

            var top = NLUi.Group(body, "Crop", false, 14f, TextAnchor.MiddleLeft, false);
            Icon(top, crop != null ? crop.ProduceItemId : "seed_carrot", 84f).color = crop != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            var info = NLUi.Group(top, "Info", true, 2f);
            NLUi.Size(info, flexibleWidth: 1f);
            if (crop != null) WordBlock(info, crop.word, 26f);
            else Text(info, phase == FarmPlot.Phase.Untilled ? "Đất chưa xới" : "Đất đã xới, chờ gieo hạt", 20f, Ink, FontStyles.Bold);

            var status = NLUi.Group(body, "Status", true, 4f);
            Color dot = phase switch { FarmPlot.Phase.NeedsWater => Water, FarmPlot.Phase.Ready => Gold, FarmPlot.Phase.Growing => Green, _ => Muted };
            Text(status, $"<color=#{ColorUtility.ToHtmlStringRGB(dot)}>●</color>  {Capital(FarmPlot.PhaseLabel(phase))}" + (crop != null ? $"   ·   giai đoạn {Mathf.Min(plot.Record.stage + 1, 4)}/4" : ""), 16.5f, Ink);
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            track.SetParent(status, false);
            track.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
            track.GetComponent<LayoutElement>().preferredHeight = 10f;
            _growFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _growFill.transform.SetParent(track, false);
            _growFill.color = Green;
            var fr = _growFill.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0f, 1f); fr.offsetMin = fr.offsetMax = Vector2.zero;
            _growText = Text(status, "", 14f, Muted);

            // Contextual action + the phrase it teaches.
            var actions = NLUi.Group(body, "Actions", true, 8f);
            switch (phase)
            {
                case FarmPlot.Phase.Untilled:
                {
                    var tool = catalog.Tool("tool_hoe");
                    PhraseCard(body, tool.verb, "CÂU HÀNH ĐỘNG");
                    var (toolId, toolName, seconds) = FarmActionTimes.Till();
                    string how = toolId == null ? $"Xới bằng tay  ·  {seconds:0.#}s (chưa có cuốc)" : $"Xới đất  ·  {toolName}  ·  {seconds:0.#}s";
                    ActionButton(actions, how, toolId ?? tool.id, () => Work("Đang xới đất…", toolName, seconds, () => Do(plot.Till(), tool.verb, "Đã xới đất.")));
                    if (toolId == null) Text(actions, "Có cuốc (くわ) thì xới nhanh gấp 4 lần — mua ở cửa hàng Midori (P).", 14f, Muted);
                    break;
                }
                case FarmPlot.Phase.Tilled:
                {
                    PhraseCard(body, catalog.verbs.plant, "CÂU HÀNH ĐỘNG");
                    BuildSeedPicker(actions, plot);
                    break;
                }
                case FarmPlot.Phase.NeedsWater:
                {
                    var tool = catalog.Tool("tool_watering_can");
                    PhraseCard(body, tool.verb, "CÂU HÀNH ĐỘNG");
                    if (IslandEconomy.Owned(tool.id) > 0)
                        ActionButton(actions, $"Tưới nước  ·  {IslandLanguage.Primary(tool.word)}  ·  {FarmActionTimes.Water:0.#}s", tool.id,
                            () => Work("Đang tưới nước…", IslandLanguage.Primary(tool.word), FarmActionTimes.Water, () => Do(plot.Water(), tool.verb, "Đã tưới — cây bắt đầu lớn.")));
                    else
                    {
                        Text(actions, "Không có bình tưới (じょうろ) thì không mang nước được — mua ở cửa hàng Midori (P).", 15f, Error);
                        Btn(actions, "Mở cửa hàng Midori", () => { CloseFarm(); OpenShop(); }, Gold, new Color(0.18f, 0.12f, 0.02f), 44f, 16f);
                    }
                    break;
                }
                case FarmPlot.Phase.Growing:
                    Text(actions, "Cây đang lớn. Bạn có thể đi làm việc khác — cây vẫn lớn kể cả khi bạn rời đảo.", 15f, Muted);
                    break;
                case FarmPlot.Phase.Ready:
                {
                    PhraseCard(body, catalog.verbs.harvest, "CÂU HÀNH ĐỘNG");
                    ActionButton(actions, $"Thu hoạch  ·  {IslandLanguage.Primary(crop.word)} ×{crop.yield}  ·  {FarmActionTimes.Harvest:0.#}s", crop.ProduceItemId, () =>
                        Work("Đang thu hoạch…", "tay", FarmActionTimes.Harvest, () =>
                        {
                            string err = plot.Harvest(out int amount);
                            Do(err, catalog.verbs.harvest, $"Thu hoạch {amount} {crop.word.vi} — đã vào balo.");
                            if (err == null) WordToast(crop.word, "Thu hoạch");
                        }));
                    break;
                }
            }
            if (crop != null && phase != FarmPlot.Phase.Ready)
            {
                var shovel = catalog.Tool("tool_shovel");
                var (_, clearTool, clearSeconds) = FarmActionTimes.Clear();
                Btn(actions, $"Dọn ô  ·  {clearTool}  ·  {clearSeconds:0.#}s", () => Work("Đang dọn ô đất…", clearTool, clearSeconds, () => Do(plot.Clear(), shovel.verb, "Đã dọn ô đất.")), Cream2, Muted, 38f, 14f);
            }
            _farmFeedback = Text(body, feedback ?? "", 15.5f, error ? Error : Green, FontStyles.Bold);
            UpdateGrowth();
        }

        /// <summary>Farm work takes time (progress card); repeated clicks while working are ignored.</summary>
        private static void Work(string label, string tool, float seconds, Action done)
        {
            if (TimedAction.Busy) return;
            TimedAction.Run(label, tool, seconds, done);
        }

        private void ActionButton(RectTransform parent, string label, string iconItem, Action click)
        {
            var b = Btn(parent, label, click, Green, Color.white, 54f, 18f);
            b.GetComponent<HorizontalOrVerticalLayoutGroup>().padding = new RectOffset(58, 18, 10, 10);
            var icon = Icon((RectTransform)b.transform, iconItem, 34f);
            icon.GetComponent<LayoutElement>().ignoreLayout = true;
            var r = icon.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(12f, 0f); r.sizeDelta = new Vector2(34f, 34f);
        }

        private void BuildSeedPicker(RectTransform parent, FarmPlot plot)
        {
            var catalog = IslandCatalog.Load();
            var owned = catalog.crops.Where(c => IslandEconomy.Owned(c.SeedItemId) > 0).ToList();
            Text(parent, "Chọn hạt giống", 15f, Green, FontStyles.Bold);
            if (owned.Count == 0)
            {
                Text(parent, "Bạn chưa có hạt giống nào.", 15.5f, Error);
                Btn(parent, "Mua hạt ở cửa hàng Midori", () => { CloseFarm(); OpenShop(); }, Gold, new Color(0.18f, 0.12f, 0.02f), 46f, 16f);
                return;
            }
            foreach (var crop in owned)
            {
                var row = NLUi.Panel(parent, "Seed_" + crop.id, Cream2, new RectOffset(10, 10, 6, 6), 10f, vertical: false);
                ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                Icon(row, crop.SeedItemId, 48f);
                var col = NLUi.Group(row, "Text", true, 0f);
                NLUi.Size(col, flexibleWidth: 1f);
                Text(col, $"<b>{IslandLanguage.Primary(crop.word)}</b>  <size=80%><color=#6B7566>×{IslandEconomy.Owned(crop.SeedItemId)}</color></size>", 18f, Ink);
                Text(col, $"Lớn trong {Mathf.RoundToInt(crop.secondsPerStage * 3)} giây · tưới 3 lần · thu {crop.yield}", 13.5f, Muted);
                var plant = Btn(row, "Gieo", () => Work("Đang gieo hạt…", "tay", FarmActionTimes.Plant, () => Do(plot.Plant(crop.id), catalog.verbs.plant, $"Đã gieo hạt {crop.word.vi}. Giờ hãy tưới nước.")), Green, Color.white, 40f, 16f);
                NLUi.Size(plant, 84f, 40f);
                plant.name = "Plant_" + crop.id;
            }
        }

        private void Do(string error, IslandWord phrase, string success)
        {
            if (error != null) { BuildFarm(error, true); return; }
            IslandState.Discover("verb:" + phrase.en);
            IslandAchievements.Check();
            BuildFarm(success + "  " + IslandLanguage.Primary(phrase), false);
            ActionFeedback(_plot != null ? _plot.transform.position : Vector3.zero);
        }

        private void ActionFeedback(Vector3 at)
        {
            var player = FindFirstObjectByType<PlayerController>();
            var animation = player != null ? player.GetComponentInChildren<NihongoLife.Core.CharacterAnimationController>() : null;
            if (animation != null) animation.TriggerPoint();
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
            _growFill.rectTransform.anchorMax = new Vector2(_plot.Crop == null ? 0f : total, 1f);
            _growText.text = phase switch
            {
                FarmPlot.Phase.Growing => $"Giai đoạn tiếp theo sau {Mathf.CeilToInt(_plot.SecondsLeft)} giây",
                FarmPlot.Phase.NeedsWater => "Cây khát nước — tưới để cây lớn tiếp",
                FarmPlot.Phase.Ready => "Đã chín!",
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
            _animalCard = Card("AnimalCard", new Vector2(1f, 0.5f), new Vector2(-28f, 20f), 440f);
            Header(_animalCard, IslandLanguage.Primary(def.word), "ĐỘNG VẬT · " + IslandCatalog.Load().places.barn.ja, CloseAnimal);
            var body = Body(_animalCard);
            WordBlock(body, def.word, 30f);
            Text(body, $"「{IslandLanguage.Sound(def.sound)}」", 22f, Green, FontStyles.Italic);
            var actions = NLUi.Group(body, "Actions", true, 8f);
            if (def.foods != null && def.foods.Length > 0)
            {
                PhraseCard(body, verbs.feed, "CÂU HÀNH ĐỘNG");
                string foods = string.Join(" / ", def.foods.Select(f => IslandLanguage.Primary(IslandCatalog.Load().Crop(f)?.word)));
                var feed = Btn(actions, $"Cho ăn  ·  {foods}", () => Work("Đang cho ăn…", "tay", FarmActionTimes.Feed, () =>
                {
                    if (_animalCard == null) return;
                    string err = _animal.Feed(out string food);
                    if (err != null) { BuildAnimal(err, true); return; }
                    IslandState.Discover("verb:" + verbs.feed.en);
                    BuildAnimal($"{def.word.vi} ăn ngon lành!  {IslandLanguage.Primary(verbs.feed)}", false);
                }), Green, Color.white, 50f, 17f);
                feed.name = "Feed";
            }
            var pet = Btn(actions, "Vuốt ve", () =>
            {
                _animal.Pet();
                IslandState.Discover("verb:" + verbs.pet.en);
                BuildAnimal($"{def.word.vi} rất vui.  {IslandLanguage.Primary(verbs.pet)}", false);
            }, Cream2, Green, 46f, 16f);
            pet.name = "Pet";
            Text(body, feedback ?? "", 15.5f, error ? Error : Green, FontStyles.Bold);
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
                        list.Add(new ShopEntry { ItemId = crop.SeedItemId, Word = crop.word, Price = crop.seedPrice, Info = $"Hạt giống · lớn trong {Mathf.RoundToInt(crop.secondsPerStage * 3)} giây · thu {crop.yield} quả, bán ¥{crop.sellPrice}/quả" });
                    foreach (var tool in c.tools)
                        list.Add(new ShopEntry { ItemId = tool.id, Word = tool.word, Price = tool.price, Info = $"Dụng cụ · {tool.verb.vi}" });
                    break;
                case "technology":
                    foreach (var p in c.products.Where(p => p.category == "technology"))
                        list.Add(new ShopEntry { ItemId = p.itemId, Word = p.word, Price = p.price, Info = p.note });
                    break;
                case "sell":
                    foreach (var crop in c.crops.Where(cr => IslandEconomy.Owned(cr.ProduceItemId) > 0))
                        list.Add(new ShopEntry { ItemId = crop.ProduceItemId, Word = crop.word, Price = crop.sellPrice, Info = $"Nông sản của bạn · đang có {IslandEconomy.Owned(crop.ProduceItemId)}", Sell = true });
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
            var result = entry.Sell ? IslandEconomy.Sell(entry.ItemId, entry.Price, ui._shopQty) : IslandEconomy.Buy(entry.ItemId, entry.Price, ui._shopQty);
            string message = result switch
            {
                IslandEconomy.Result.Ok when entry.Sell => $"Đã bán {ui._shopQty} {entry.Word.vi} · +¥{entry.Price * ui._shopQty}.  {IslandLanguage.Primary(verbs.sell)}",
                IslandEconomy.Result.Ok => $"Đã mua {ui._shopQty} {entry.Word.vi} · −¥{entry.Price * ui._shopQty}.  {IslandLanguage.Primary(verbs.buy)}",
                IslandEconomy.Result.NoMoney => $"Không đủ tiền: cần ¥{entry.Price * ui._shopQty}, bạn có ¥{IslandEconomy.Yen}.",
                IslandEconomy.Result.BagFull => "Balo đầy — bỏ bớt đồ hoặc bán nông sản.",
                IslandEconomy.Result.NotOwned => "Bạn không có đủ món này để bán.",
                _ => "Không thực hiện được.",
            };
            if (result == IslandEconomy.Result.Ok)
            {
                IslandState.Discover((entry.Sell ? "verb:" + verbs.sell.en : "verb:" + verbs.buy.en));
                IslandState.Discover(WordKey(entry.ItemId));
                if (entry.Sell && IslandEconomy.Owned(entry.ItemId) == 0) ui._shopSelected = null;
            }
            if (ui._shop != null) ui.BuildShop(message, result != IslandEconomy.Result.Ok);
            return result;
        }

        private void BuildShop(string feedback, bool error)
        {
            Discard(_shop);
            var places = IslandCatalog.Load().places;
            _shop = Card("IslandShop", new Vector2(0.5f, 0.5f), Vector2.zero, 1120f);
            var header = Header(_shop, places.shop.ja + "  ·  " + places.shop.en, "CỬA HÀNG MIDORI", CloseShop);
            var wallet = NLUi.Pill(header, "Wallet", $"¥{IslandEconomy.Yen:N0}", _font, Gold, new Color(0.18f, 0.12f, 0.02f), 18f);
            var walletLayout = wallet.GetComponent<LayoutElement>();
            if (walletLayout == null) walletLayout = wallet.gameObject.AddComponent<LayoutElement>();
            walletLayout.ignoreLayout = true;
            NLUi.Anchor(wallet, new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(130f, 36f));

            var main = NLUi.Group(_shop, "Main", false, 18f, TextAnchor.UpperLeft, false);
            main.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(24, 24, 18, 0);
            // Tabs
            var tabs = NLUi.Group(main, "Tabs", true, 8f);
            NLUi.Size(tabs, 200f);
            foreach (var (id, label, sub) in new[] { ("agriculture", "Nông nghiệp", "のうぎょう · Agriculture"), ("fashion", "Thời trang", "ファッション · Fashion"), ("technology", "Công nghệ", "テクノロジー · Technology"), ("sell", "Bán nông sản", "うる · Sell") })
            {
                bool on = _shopTab == id;
                string captured = id;
                var tab = Btn(tabs, $"<b>{label}</b>\n<size=70%>{sub}</size>", () => SelectShopTab(captured), on ? Green : Cream2, on ? Color.white : Ink, 58f, 16f);
                tab.name = "Tab_" + id;
            }

            // Grid
            var gridHost = NLUi.Group(main, "GridHost", true, 10f);
            NLUi.Size(gridHost, 520f);
            var entries = Entries(_shopTab);
            if (_shopTab == "fashion")
            {
                var empty = NLUi.Panel(gridHost, "Empty", Cream2, new RectOffset(20, 20, 18, 18), 6f);
                Text(empty, "Chưa có hàng thời trang", 20f, Ink, FontStyles.Bold);
                Text(empty, "Dự án hiện chưa có mô hình quần áo / phụ kiện nào nên mục này để trống thay vì bán đồ không có thật. Trang phục sẽ được thêm khi có mô hình phù hợp.", 15f, Muted);
            }
            else if (entries.Count == 0)
            {
                var empty = NLUi.Panel(gridHost, "Empty", Cream2, new RectOffset(20, 20, 18, 18), 6f);
                Text(empty, _shopTab == "sell" ? "Chưa có nông sản để bán — thu hoạch ở ruộng trước nhé." : "Chưa có mặt hàng.", 17f, Muted);
            }
            else
            {
                var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                gridGo.transform.SetParent(gridHost, false);
                var grid = gridGo.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(122f, 150f); grid.spacing = new Vector2(10f, 10f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
                gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                foreach (var e in entries)
                {
                    bool selected = e.ItemId == _shopSelected;
                    string id = e.ItemId;
                    var cell = new GameObject("Item_" + e.ItemId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
                    cell.transform.SetParent(gridGo.transform, false);
                    cell.GetComponent<Image>().color = selected ? GreenSoft : Color.white;
                    var outline = cell.AddComponent<Outline>(); outline.effectColor = selected ? Green : new Color(0f, 0f, 0f, 0.08f); outline.effectDistance = new Vector2(2f, -2f);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectShopItem(id));
                    var v = cell.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(8, 8, 8, 8); v.spacing = 2f; v.childAlignment = TextAnchor.UpperCenter; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
                    Icon((RectTransform)cell.transform, e.ItemId, 74f);
                    var name = Text((RectTransform)cell.transform, IslandLanguage.Primary(e.Word), 15f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                    name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
                    Text((RectTransform)cell.transform, $"¥{e.Price}", 14.5f, e.Sell ? Green : Gold, FontStyles.Bold, TextAlignmentOptions.Center);
                }
            }

            // Detail
            var detail = NLUi.Panel(main, "Detail", Cream2, new RectOffset(20, 20, 18, 18), 10f);
            NLUi.Size(detail, 290f);
            var entry = entries.FirstOrDefault(e => e.ItemId == _shopSelected);
            if (entry == null)
            {
                Text(detail, _shopTab == "sell" ? "Chọn nông sản để bán." : "Chọn một món để xem chi tiết.", 16f, Muted);
            }
            else
            {
                var iconRow = NLUi.Group(detail, "IconRow", false, 0f, TextAnchor.MiddleCenter, false);
                Icon(iconRow, entry.ItemId, 120f);
                WordBlock(detail, entry.Word, 24f);
                Text(detail, entry.Info, 14.5f, Muted);
                Text(detail, $"Đang có: <b>{IslandEconomy.Owned(entry.ItemId)}</b>", 15f, Ink);
                var qtyRow = NLUi.Group(detail, "Qty", false, 8f, TextAnchor.MiddleCenter, false);
                var minus = Btn(qtyRow, "−", () => SetShopQuantity(_shopQty - 1), Color.white, Ink, 40f, 20f); NLUi.Size(minus, 46f, 40f);
                var q = Text(qtyRow, _shopQty.ToString(), 20f, Ink, FontStyles.Bold, TextAlignmentOptions.Center); NLUi.Size(q, 50f);
                var plus = Btn(qtyRow, "+", () => SetShopQuantity(_shopQty + 1), Color.white, Ink, 40f, 20f); NLUi.Size(plus, 46f, 40f);
                bool canAfford = entry.Sell ? IslandEconomy.Owned(entry.ItemId) >= _shopQty : IslandEconomy.Yen >= entry.Price * _shopQty;
                var confirm = Btn(detail, entry.Sell ? $"Bán  ·  +¥{entry.Price * _shopQty}" : $"Mua  ·  ¥{entry.Price * _shopQty}", () => ConfirmShop(), canAfford ? Green : new Color(0.62f, 0.64f, 0.6f), Color.white, 52f, 18f);
                confirm.name = "Confirm";
                if (!canAfford) Text(detail, entry.Sell ? "Không đủ số lượng." : $"Thiếu ¥{entry.Price * _shopQty - IslandEconomy.Yen}.", 14f, Error);
            }
            _shopFeedback = Text(_shop, feedback ?? "", 16f, error ? Error : Green, FontStyles.Bold, TextAlignmentOptions.Center);
            _shopFeedback.margin = new Vector4(24f, 6f, 24f, 0f);
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
            _book = Card("IslandNotebook", new Vector2(0.5f, 0.5f), Vector2.zero, 960f);
            Header(_book, "Sổ tay Đảo Xanh", "ノート · PROGRESS", CloseProgress);
            var body = Body(_book, 14f);
            var stats = NLUi.Group(body, "Stats", false, 12f, TextAnchor.MiddleLeft, true);
            foreach (var (n, label) in new[] { (record.harvested.ToString(), "lần thu hoạch"), (record.sold.ToString(), "nông sản đã bán"), ($"¥{record.earned:N0}", "tiền kiếm được"), (DistinctWords().Count.ToString(), "từ đã học"), (record.animalsMet.Count + "/5", "con vật quen") })
            {
                var tile = NLUi.Panel(stats, "Stat", Cream2, new RectOffset(14, 14, 10, 10), 0f);
                Text(tile, n, 28f, Green, FontStyles.Bold);
                Text(tile, label, 13.5f, Muted);
            }
            Text(body, $"Từ vựng  <size=75%><color=#6B7566>(đang học {IslandLanguage.LanguageLabel} — bấm vào từ để xem nghĩa)</color></size>", 18f, Ink, FontStyles.Bold);
            var gridGo = new GameObject("Words", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridGo.transform.SetParent(body, false);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(176f, 62f); grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
            gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var words = DistinctWords().Take(25).ToList();
            if (words.Count == 0) Text(body, "Chưa có từ nào — trồng cây, cho thú ăn và mua sắm để học từ mới.", 15f, Muted);
            foreach (var w in words)
            {
                var cell = new GameObject("Word", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
                cell.transform.SetParent(gridGo.transform, false);
                cell.GetComponent<Image>().color = Color.white;
                var v = cell.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(10, 10, 6, 6); v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
                var primary = Text((RectTransform)cell.transform, IslandLanguage.Primary(w), 16f, Ink, FontStyles.Bold);
                primary.textWrappingMode = TextWrappingModes.NoWrap; primary.overflowMode = TextOverflowModes.Ellipsis;
                string reading = IslandLanguage.Reading(w) ?? "";
                var second = Text((RectTransform)cell.transform, reading.Length > 0 ? reading : "·", 12.5f, Muted, FontStyles.Italic);
                second.textWrappingMode = TextWrappingModes.NoWrap; second.overflowMode = TextOverflowModes.Ellipsis;
                var word = w;
                cell.GetComponent<Button>().onClick.AddListener(() => second.text = word.vi);
            }
            Text(body, "Thành tích", 18f, Ink, FontStyles.Bold);
            var ach = NLUi.Group(body, "Achievements", false, 10f, TextAnchor.MiddleLeft, true);
            foreach (var def in IslandAchievements.All)
            {
                bool done = record.achievements.Contains(def.Id);
                var tile = NLUi.Panel(ach, "A_" + def.Id, done ? GreenSoft : new Color(0.92f, 0.9f, 0.86f, 1f), new RectOffset(12, 12, 8, 8), 0f);
                Text(tile, (done ? "<color=#2E6B45>✓</color> " : "<color=#9AA095>○</color> ") + def.Ja, 14.5f, done ? Ink : Muted, FontStyles.Bold);
                Text(tile, def.Vi, 12.5f, Muted);
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
            return crop != null ? "crop:" + crop.id : "item:" + itemId;
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
                case "animal": return c.Animal(id)?.word;
                case "item":
                    if (c.Tool(id) != null) return c.Tool(id).word;
                    var crop = c.crops.FirstOrDefault(x => x.SeedItemId == id || x.ProduceItemId == id);
                    if (crop != null) return crop.word;
                    return c.products.FirstOrDefault(p => p.itemId == id)?.word;
                case "place":
                    var p2 = c.places;
                    return id switch { "station" => p2.station, "farm" => p2.farm, "shop" => p2.shop, "barn" => p2.barn, "view" => p2.view, _ => p2.island };
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
            root.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.12f, 0.97f);
            ui._travel = root;
            var title = NLUi.Label(root, "Title", "みどりせん · Midori Line", 30f, new Color(0.95f, 0.85f, 0.55f), ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 50f));
            var sub = NLUi.Label(root, "Sub", $"{from}  →  {to}", 24f, Color.white, ui._font, FontStyles.Normal, TextAlignmentOptions.Center);
            NLUi.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(900f, 40f));
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            track.SetParent(root, false);
            NLUi.Anchor(track, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(700f, 6f));
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
            var train = NLUi.Label(root, "Train", "■■■▶", 26f, new Color(0.86f, 0.93f, 0.82f), ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            var note = NLUi.Label(root, "Note", "でんしゃは はしって います… · Tàu đang chạy", 18f, new Color(0.8f, 0.86f, 0.8f), ui._font, FontStyles.Italic, TextAlignmentOptions.Center);
            NLUi.Anchor(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 40f));
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                NLUi.Anchor(train.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Lerp(-350f, 350f, t / seconds), 26f), new Vector2(120f, 40f));
                yield return null;
            }
        }

        public static void EndTravelScreen()
        {
            if (_instance != null && _instance._travel != null) Destroy(_instance._travel.gameObject);
        }
    }
}
