using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>What the small objective chip (top-left) shows. Progression.QuestService sets it for tracked tasks.</summary>
    public static class HudObjective
    {
        /// <summary>Returns (kicker, objective line, progress) or null to fall back to the story objective.</summary>
        public static Func<(string kicker, string text, string progress)?> Provider;
    }

    /// <summary>
    /// Always-on HUD shared by every scene (the city hosts it, zones load on top). Screen policy:
    /// · bottom-left — compact status widget: portrait, level, yen, knowledge and five radial need gauges;
    ///   a warning chip appears above it only when a need is critical;
    /// · top-left — one tracked objective (the full list lives in the Task Journal, N);
    /// · top-centre — zone bar (station steps, island toolbar) and the notification feed (HudFeed);
    /// · bottom-centre — the single interaction prompt;
    /// · centre / right — windows and contextual cards.
    /// Shortcut buttons are not shown on desktop (keys are listed in Settings → Điều khiển); touch layouts keep
    /// an action bar because they have no keyboard. Tab opens the character window. The dock fades out while a
    /// dialogue or a full-screen activity is open.
    /// </summary>
    public sealed class StatusDock : MonoBehaviour
    {
        private readonly struct Bar
        {
            public Bar(Image fill, TextMeshProUGUI value, Color color) { Fill = fill; Value = value; Color = color; }
            public Image Fill { get; }
            public TextMeshProUGUI Value { get; }
            public Color Color { get; }
        }

        private readonly struct Ring
        {
            public Ring(Image fill, Image icon, TextMeshProUGUI value, Color color) { Fill = fill; Icon = icon; Value = value; Color = color; }
            public Image Fill { get; }
            public Image Icon { get; }
            public TextMeshProUGUI Value { get; }
            public Color Color { get; }
        }

        private static Sprite NeedIcon(string key) => key switch
        {
            "health" => HudIcons.Heart,
            "energy" => HudIcons.Runner,
            "hunger" => HudIcons.ForkKnife,
            "thirst" => HudIcons.Drop,
            _ => HudIcons.Moon,
        };

        private static string DockLabel(string name, string fallback) => name switch
        {
            "BagButton" => NihongoLife.Core.Loc.T("Balo", "Bag", "バッグ"),
            "CharacterButton" => NihongoLife.Core.Loc.T("Nhân vật", "Character", "キャラ"),
            "MapButton" => NihongoLife.Core.Loc.T("Bản đồ", "Map", "地図"),
            "QuestButton" => NihongoLife.Core.Loc.T("Nhiệm vụ", "Tasks", "タスク"),
            "ExamButton" => NihongoLife.Core.Loc.T("Luyện thi", "Exams", "試験"),
            "NotebookButton" => NihongoLife.Core.Loc.T("Sổ tay", "Notebook", "ノート"),
            "OverviewButton" => NihongoLife.Core.Loc.T("Góc nhìn", "View", "視点"),
            "SettingsButton" => NihongoLife.Core.Loc.T("Cài đặt", "Settings", "設定"),
            _ => fallback,
        };

        private static string NeedName(string key) => key switch
        {
            "health" => NihongoLife.Core.Loc.T("Sức khỏe", "Health", "体力"),
            "energy" => NihongoLife.Core.Loc.T("Thể lực", "Stamina", "元気"),
            "hunger" => NihongoLife.Core.Loc.T("No", "Fullness", "満腹"),
            "thirst" => NihongoLife.Core.Loc.T("Khát", "Thirst", "のど"),
            _ => NihongoLife.Core.Loc.T("Tỉnh táo", "Alertness", "眠気"),
        };

        private static readonly (string key, string glyph, string vi, Color color)[] Needs =
        {
            ("health", "体", "Sức khỏe", new Color(0.92f, 0.38f, 0.38f)),
            ("energy", "元", "Thể lực", new Color(0.39f, 0.71f, 0.96f)),
            ("hunger", "食", "No", new Color(0.96f, 0.72f, 0.29f)),
            ("thirst", "水", "Khát", new Color(0.3f, 0.82f, 0.78f)),
            ("rest", "眠", "Tỉnh táo", new Color(0.7f, 0.6f, 0.95f)),
        };

        private readonly Dictionary<Image, float> _fillTargets = new();
        private TMP_FontAsset _font;
        private CanvasGroup _dockGroup;
        private RectTransform _weatherChip;
        private TextMeshProUGUI _weatherText;
        /// <summary>The need currently shown in the warning chip (tests).</summary>
        public string CriticalNeed { get; private set; }
        public string WarningText => _warningText != null && _warning.gameObject.activeSelf ? _warningText.text : null;
        public string WeatherEffectText => _weatherChip != null && _weatherChip.gameObject.activeSelf ? _weatherText.text : null;
        private TextMeshProUGUI _name, _level, _wallet, _levelBadge;
        private Image _expFill;
        private readonly Dictionary<string, Ring> _rings = new();
        private RectTransform _warning;
        private TextMeshProUGUI _warningText;
        private RectTransform _objective;
        private TextMeshProUGUI _objectiveKicker, _objectiveText, _objectiveProgress;

        private readonly Dictionary<string, Bar> _characterBars = new();
        private RectTransform _characterWindow;
        private TextMeshProUGUI _characterName, _characterLevel, _characterInfo;
        private TextMeshProUGUI _statYen, _statKnowledge, _statShifts, _statWords;
        private Image _characterExp;
        private ProfilePreview _preview;
        private HudPortrait _portrait;
        private PlayerStatus _boundStatus;
        private PlayerInventory _boundInventory;

        public GameObject CharacterRoot => _characterWindow != null ? _characterWindow.gameObject : null;
        public RectTransform StatusWidget { get; private set; }
        public RectTransform ObjectiveChip => _objective;

        public static StatusDock Create(Transform parent, TMP_FontAsset font, IReadOnlyList<(string name, string key, string label, Action action)> buttons, Action closeCharacter)
        {
            var canvas = NLUi.CreateCanvas("StatusDockCanvas", 90, parent);
            var dock = canvas.gameObject.AddComponent<StatusDock>();
            dock._font = font != null ? font : NLUi.ResolveFont();
            dock.Build((RectTransform)canvas.transform, buttons, closeCharacter);
            return dock;
        }

        private void Build(RectTransform root, IReadOnlyList<(string name, string key, string label, Action action)> buttons, Action closeCharacter)
        {
            var dock = new GameObject("Dock", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            dock.SetParent(root, false);
            NLUi.Stretch(dock);
            _dockGroup = dock.GetComponent<CanvasGroup>();
            BuildVignette(root);

            BuildStatusWidget(dock);
            BuildObjectiveChip(dock);

            // Touch layouts have no keyboard: keep the action bar there only.
            if (HudCanvasFitter.IsTouchLayout(false))
            {
                var bar = NLUi.Group(dock, "ActionBar", false, 8f, TextAnchor.MiddleRight, false);
                NLUi.Anchor(bar, new Vector2(1f, 0f), new Vector2(-24f, 190f), new Vector2(buttons.Count * 112f, 64f));
                bar.pivot = new Vector2(1f, 0f);
                ((HorizontalLayoutGroup)bar.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                foreach (var (name, _, label, action) in buttons)
                {
                    var button = NLUi.Button(bar, name, label, _font, () => action?.Invoke(), NLUi.Ink, 16f, NLUi.Text, 62f);
                    string buttonName = name, fallback = label;
                    LocText.Set(button.GetComponentInChildren<TextMeshProUGUI>(), () => DockLabel(buttonName, fallback));
                    NLUi.Size(button, 104f, 62f);
                }
            }

            BuildCharacterWindow(root, closeCharacter);
        }

        // ─────────── Compact status widget ───────────

        private void BuildStatusWidget(RectTransform dock)
        {
            // Dark glass card: portrait | name, place, wallet / five need gauges.
            var widget = NLUi.Panel(dock, "StatusWidget", new Color(0.04f, 0.05f, 0.07f, 0.9f), new RectOffset(14, 20, 14, 14), 18f, vertical: false);
            NLUi.Anchor(widget, Vector2.zero, new Vector2(24f, 24f), new Vector2(0f, 0f));
            widget.pivot = Vector2.zero;
            var widgetLayout = (HorizontalLayoutGroup)widget.GetComponent<HorizontalOrVerticalLayoutGroup>();
            widgetLayout.childForceExpandWidth = false;
            widgetLayout.childAlignment = TextAnchor.MiddleLeft;
            NLUi.FitContent(widget, width: true, height: true);
            var edge = widget.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(1f, 1f, 1f, 0.07f);
            edge.effectDistance = new Vector2(1f, -1f);
            var shadow = widget.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);
            StatusWidget = widget;

            // Portrait of the real player model in a framed circle, with the level badge on its rim.
            const float portraitSize = 100f;
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            portrait.SetParent(widget, false);
            var portraitLayout = portrait.GetComponent<LayoutElement>();
            portraitLayout.preferredWidth = portraitLayout.minWidth = portraitSize; portraitLayout.preferredHeight = portraitLayout.minHeight = portraitSize;
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            mask.SetParent(portrait, false);
            NLUi.Stretch(mask, 4f);
            var maskImage = mask.GetComponent<Image>();
            maskImage.sprite = HudGraphics.Disc;
            maskImage.color = new Color(0.33f, 0.52f, 0.62f, 1f); // shown until the first photo is taken
            maskImage.raycastTarget = false;
            _portrait = HudPortrait.Create(mask);
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            frame.transform.SetParent(portrait, false);
            NLUi.Stretch(frame.rectTransform);
            frame.sprite = HudGraphics.Hairline; frame.color = new Color(1f, 1f, 1f, 0.9f); frame.raycastTarget = false;

            var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
            badge.SetParent(portrait, false);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 0f); badge.pivot = new Vector2(0.72f, 0.22f); badge.sizeDelta = new Vector2(40f, 40f);
            var badgeImage = badge.GetComponent<Image>(); badgeImage.sprite = HudGraphics.Disc; badgeImage.color = new Color(0.47f, 0.36f, 0.9f);
            var badgeEdge = badge.GetComponent<Outline>(); badgeEdge.effectColor = new Color(1f, 1f, 1f, 0.85f); badgeEdge.effectDistance = new Vector2(1.5f, -1.5f);
            var lv = NLUi.Label(badge, "Lv", "Lv.", 10f, new Color(1f, 1f, 1f, 0.85f), _font, FontStyles.Bold, TextAlignmentOptions.Top);
            NLUi.Stretch(lv.rectTransform); lv.rectTransform.offsetMax = new Vector2(0f, -5f);
            _levelBadge = NLUi.Label(badge, "Level", "1", 17f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Bottom);
            NLUi.Stretch(_levelBadge.rectTransform); _levelBadge.rectTransform.offsetMin = new Vector2(0f, 3f);
            _levelBadge.textWrappingMode = TextWrappingModes.NoWrap;

            var column = NLUi.Group(widget, "Column", true, 8f, TextAnchor.UpperLeft, false);
            var top = NLUi.Group(column, "Top", false, 18f, TextAnchor.MiddleLeft, false);
            var identity = NLUi.Group(top, "Identity", true, 2f, TextAnchor.UpperLeft, false);
            _name = NLUi.Label(identity, "Name", "", 22f, NLUi.Text, _font, FontStyles.Bold);
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            _name.characterSpacing = 1f;
            var placeRow = NLUi.Group(identity, "PlaceRow", false, 5f, TextAnchor.MiddleLeft, false);
            Icon(placeRow, "Pin", HudIcons.Pin, 14f, NLUi.Muted);
            _level = NLUi.Label(placeRow, "Place", "", 14f, NLUi.Muted, _font);
            _level.textWrappingMode = TextWrappingModes.NoWrap;
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(top, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var wallet = NLUi.Panel(top, "WalletPill", new Color(0.12f, 0.1f, 0.05f, 0.85f), new RectOffset(12, 16, 6, 6), 8f, vertical: false);
            var walletLayout = (HorizontalLayoutGroup)wallet.GetComponent<HorizontalOrVerticalLayoutGroup>();
            walletLayout.childForceExpandWidth = false; walletLayout.childAlignment = TextAnchor.MiddleCenter;
            wallet.GetComponent<Image>().sprite = UiKit.Rounded(16);
            UiKit.Border(wallet, new Color(0.95f, 0.75f, 0.32f, 0.75f), 16);
            Icon(wallet, "WalletIcon", UiIcons.Coin, 26f, Color.white); // the gold yen coin
            _wallet = NLUi.Label(wallet, "Wallet", "", 19f, NLUi.Gold, _font, FontStyles.Bold);
            _wallet.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.FitContent(wallet, width: true, height: true);
            // +¥/−¥ with the reason rises from the chip; a click lists the last transactions.
            wallet.GetComponent<Image>().raycastTarget = true;
            wallet.gameObject.AddComponent<WalletFeedback>().Configure(_font);

            _expFill = BarFill(column, "Exp", new Color(0.55f, 0.45f, 0.95f), 3f);
            NLUi.Size(_expFill.transform.parent, flexibleWidth: 1f);
            NLUi.Size(top, flexibleWidth: 1f);
            var rings = NLUi.Group(column, "Needs", false, 12f, TextAnchor.MiddleLeft, false);
            foreach (var (key, _, _, color) in Needs) _rings[key] = MakeRing(rings, key, color);

            // Critical warning chip, above the widget, only while a need is below 20 %.
            _warning = NLUi.Panel(dock, "NeedWarning", new Color(0.55f, 0.13f, 0.12f, 0.95f), new RectOffset(16, 16, 8, 8), 0f, vertical: false);
            NLUi.Anchor(_warning, Vector2.zero, new Vector2(24f, 190f), new Vector2(0f, 0f));
            _warning.pivot = Vector2.zero;
            NLUi.FitContent(_warning, width: true, height: true);
            _warningText = NLUi.Label(_warning, "Text", "", 16f, Color.white, _font, FontStyles.Bold);
            _warningText.textWrappingMode = TextWrappingModes.NoWrap;
            _warning.gameObject.SetActive(false);
        }

        private static Image Icon(Transform parent, string name, Sprite sprite, float size, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite; image.color = color; image.raycastTarget = false; image.preserveAspect = true;
            var element = image.GetComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = size; element.preferredHeight = element.minHeight = size;
            return image;
        }

        /// <summary>One need gauge: a slim progress ring with the need's icon and its percentage inside.</summary>
        private Ring MakeRing(RectTransform parent, string key, Color color)
        {
            const float size = 60f;
            var holder = new GameObject("Need_" + key, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            var element = holder.GetComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = size; element.preferredHeight = element.minHeight = size;
            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            plate.transform.SetParent(holder, false); NLUi.Stretch(plate.rectTransform, 3f);
            plate.sprite = HudGraphics.Disc; plate.color = new Color(1f, 1f, 1f, 0.04f); plate.raycastTarget = false;
            var back = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(holder, false); NLUi.Stretch(back.rectTransform);
            back.sprite = HudGraphics.Gauge; back.color = new Color(1f, 1f, 1f, 0.1f); back.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(holder, false); NLUi.Stretch(fill.rectTransform);
            fill.sprite = HudGraphics.Gauge; fill.color = color; fill.raycastTarget = false;
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true;
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(holder, false);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            icon.rectTransform.sizeDelta = new Vector2(22f, 22f);
            icon.sprite = NeedIcon(key); icon.color = color; icon.raycastTarget = false; icon.preserveAspect = true;
            var value = NLUi.Label(holder, "Value", "", 11f, color, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.rectTransform.anchorMin = value.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            value.rectTransform.sizeDelta = new Vector2(size, 18f);
            return new Ring(fill, icon, value, color);
        }

        // ─────────── Tracked objective chip ───────────

        private void BuildObjectiveChip(RectTransform dock)
        {
            _objective = NLUi.Panel(dock, "TrackedObjective", new Color(0.04f, 0.055f, 0.07f, 0.86f), new RectOffset(16, 18, 10, 12), 2f);
            NLUi.Anchor(_objective, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 0f));
            _objective.pivot = new Vector2(0f, 1f);
            NLUi.FitContent(_objective);
            _objectiveKicker = NLUi.Label(_objective, "Kicker", "", 13f, NLUi.Gold, _font, FontStyles.Bold);
            _objectiveText = NLUi.Label(_objective, "Text", "", 17f, NLUi.Text, _font);
            _objectiveText.textWrappingMode = TextWrappingModes.Normal;
            _objectiveText.overflowMode = TextOverflowModes.Ellipsis;
            _objectiveText.maxVisibleLines = 2;
            _objectiveProgress = NLUi.Label(_objective, "Progress", "", 13f, NLUi.Muted, _font);
        }

        private void RefreshObjective()
        {
            (string kicker, string text, string progress)? info = null;
            try { info = HudObjective.Provider?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            if (info == null)
            {
                var scenario = ScenarioManager.Instance;
                var objective = scenario != null ? scenario.Objectives.FirstOrDefault(o => o.state == ObjectiveState.Active) : null;
                if (objective != null)
                {
                    string title = objective.Title;
                    int done = scenario.Objectives.Count(o => o.state == ObjectiveState.Completed);
                    info = ("◆ " + NihongoLife.Core.Loc.T("CỐT TRUYỆN", "STORY", "ストーリー"), title, NihongoLife.Core.Loc.F("{0}/{1} mục · N mở sổ nhiệm vụ", "{0}/{1} steps · N opens the task journal", "{0}/{1} · Nでタスク帳", done, scenario.Objectives.Count));
                }
            }
            _objective.gameObject.SetActive(info != null);
            if (info == null) return;
            _objectiveKicker.text = info.Value.kicker;
            _objectiveText.text = info.Value.text;
            _objectiveProgress.text = info.Value.progress;
        }

        // ─────────── Bars (character window) ───────────

        private Bar BarRow(RectTransform parent, string icon, string label, Color color)
        {
            var row = NLUi.Group(parent, "Row_" + label, false, 8f, TextAnchor.MiddleLeft, false);
            var glyph = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            glyph.transform.SetParent(row, false);
            glyph.sprite = NeedIcon(icon); glyph.color = color; glyph.preserveAspect = true; glyph.raycastTarget = false;
            var glyphSize = glyph.GetComponent<LayoutElement>(); glyphSize.preferredWidth = glyphSize.preferredHeight = 20f;
            string need = icon;
            NLUi.Size(LocText.Set(NLUi.Label(row, "Label", "", 16f, NLUi.Muted, _font), () => NeedName(need)), 96f);
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            track.SetParent(row, false);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var element = track.GetComponent<LayoutElement>();
            element.preferredWidth = 150f; element.preferredHeight = 10f; element.flexibleWidth = 1f;
            var fill = Fill(track, color);
            var value = NLUi.Label(row, "Value", "100", 15f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Right);
            NLUi.Size(value, 70f);
            return new Bar(fill, value, color);
        }

        private Image BarFill(RectTransform parent, string name, Color color, float height)
        {
            var track = new GameObject(name + "Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            track.SetParent(parent, false);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            track.GetComponent<LayoutElement>().preferredHeight = height;
            return Fill(track, color);
        }

        private static Image Fill(RectTransform track, Color color)
        {
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(track, false);
            fill.color = color;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0.5f);
            return fill;
        }

        private void SetFill(Image fill, float value01)
        {
            if (fill == null) return;
            value01 = Mathf.Clamp01(value01);
            if (!_fillTargets.ContainsKey(fill)) ApplyFill(fill, value01);
            _fillTargets[fill] = value01;
        }

        private static float CurrentFill(Image fill) => fill.type == Image.Type.Filled ? fill.fillAmount : fill.rectTransform.anchorMax.x;

        private static void ApplyFill(Image fill, float value)
        {
            if (fill.type == Image.Type.Filled) fill.fillAmount = value;
            else fill.rectTransform.anchorMax = new Vector2(value, 1f);
        }

        private void AnimateFills()
        {
            float step = Time.unscaledDeltaTime * 1.6f;
            foreach (var kv in _fillTargets)
            {
                if (kv.Key == null) continue;
                float current = CurrentFill(kv.Key);
                float next = Mathf.MoveTowards(current, kv.Value, step);
                if (!Mathf.Approximately(next, current)) ApplyFill(kv.Key, next);
            }
        }

        /// <summary>Normal colour → amber under 30 % → red flashing under 15 %, flashing faster as it nears 0.</summary>
        private static Color Urgency(Color normal, float value01)
        {
            if (value01 >= 0.5f) return normal;
            if (value01 >= PlayerStatus.DangerThreshold) return Color.Lerp(new Color(0.96f, 0.68f, 0.22f), normal, Mathf.Clamp01((value01 - 0.3f) / 0.2f));
            return Color.Lerp(new Color(1f, 0.32f, 0.3f), new Color(0.45f, 0.1f, 0.1f), Flash(value01));
        }

        private static float Flash(float value01) =>
            Mathf.PingPong(Time.unscaledTime * Mathf.Lerp(7f, 2.5f, value01 / PlayerStatus.DangerThreshold), 1f);

        /// <summary>Screen-edge glow: red pulse when health is in danger, dark edges while nodding off.</summary>
        private void BuildVignette(RectTransform root)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "HudVignette" };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.55f + new Vector2(dx, dy).magnitude * 0.45f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1.08f, d))));
                }
            texture.Apply();
            var go = new GameObject("DangerVignette", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(root, false);
            go.transform.SetAsFirstSibling();
            NLUi.Stretch((RectTransform)go.transform);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = new Color(1f, 0f, 0f, 0f);
            _vignetteRaw = image;
        }

        private RawImage _vignetteRaw;

        private void UpdateVignette(PlayerStatus status, bool hide)
        {
            if (_vignetteRaw == null) return;
            float health = status.CurrentHealth / Mathf.Max(1f, status.MaxHealth);
            Color target = new Color(1f, 0f, 0f, 0f);
            if (!hide && health < PlayerStatus.DangerThreshold)
                target = new Color(0.85f, 0.05f, 0.05f, Mathf.Lerp(0.18f, 0.55f, 1f - health / PlayerStatus.DangerThreshold) * (0.55f + 0.45f * Flash(health)));
            else if (!hide && status.Sleepiness >= 100f)
                target = new Color(0.02f, 0.02f, 0.05f, Mathf.Lerp(0.35f, 0.85f, status.NoddingOffSeconds / PlayerStatus.FallAsleepAfterSeconds) * (0.75f + 0.25f * Mathf.PingPong(Time.unscaledTime * 0.7f, 1f)));
            _vignetteRaw.color = Color.Lerp(_vignetteRaw.color, target, Time.unscaledDeltaTime * 8f);
        }

        public float DangerVignetteAlpha => _vignetteRaw != null ? _vignetteRaw.color.a : 0f;

        private void UpdateWeatherChip(PlayerStatus status, bool hide)
        {
            if (_weatherChip == null)
            {
                _weatherChip = NLUi.Panel(_warning.parent, "WeatherEffect", new Color(0.12f, 0.2f, 0.32f, 0.92f), new RectOffset(14, 14, 6, 6), 0f, vertical: false);
                NLUi.Anchor(_weatherChip, Vector2.zero, Vector2.zero, Vector2.zero);
                _weatherChip.pivot = Vector2.zero;
                NLUi.FitContent(_weatherChip, width: true, height: true);
                _weatherText = NLUi.Label(_weatherChip, "Text", "", 15f, Color.white, _font, FontStyles.Bold);
                _weatherText.textWrappingMode = TextWrappingModes.NoWrap;
            }
            string text = null;
            if (status.HasCold) text = NihongoLife.Core.Loc.T("Bị cảm lạnh vì ướt mưa — mất máu từ từ, mau buồn ngủ. Vào nhà cho khô.", "You caught a cold in the rain — health drains slowly and you tire fast. Go inside and dry off.", "雨でかぜをひきました。体力が少しずつ減り、眠くなりやすいです。家に入って乾かしましょう。");
            else if (NihongoLife.Core.WeatherCycle.PlayerInRain) text = NihongoLife.Core.Loc.F("Đang ướt mưa ({0:0}%) — hồi thể lực chậm một nửa", "Getting wet in the rain ({0:0}%) — stamina recovers at half speed", "雨にぬれています（{0:0}%）— 元気の回復が半分になります", status.Wetness * 100f);
            else if (status.InHotSun) text = NihongoLife.Core.Loc.T("Nắng gắt buổi trưa — khát nhanh hơn, nhớ uống nước", "Strong midday sun — you get thirsty faster, drink water", "昼の強い日差し — のどが渇きやすいので水を飲みましょう");
            bool show = text != null && !hide;
            if (_weatherChip.gameObject.activeSelf != show) _weatherChip.gameObject.SetActive(show);
            if (!show) return;
            _weatherText.text = text;
            float y = 24f + (StatusWidget != null ? StatusWidget.rect.height : 160f) + 10f + (_warning.gameObject.activeSelf ? _warning.rect.height + 8f : 0f);
            _weatherChip.anchoredPosition = new Vector2(24f, y);
        }

        // ─────────── Character window (Tab) ───────────

        private void BuildCharacterWindow(RectTransform root, Action close)
        {
            _characterWindow = NLUi.Panel(root, "CharacterWindow", NLUi.Ink, new RectOffset(24, 24, 22, 22), 20f, vertical: false);
            NLUi.Anchor(_characterWindow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 0f));
            NLUi.FitContent(_characterWindow);
            ((HorizontalLayoutGroup)_characterWindow.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;

            var stage = NLUi.Panel(_characterWindow, "ModelCard", new Color(0.86f, 0.91f, 0.97f, 1f), new RectOffset(0, 0, 0, 0), 0f);
            NLUi.Size(stage, 280f, 440f);
            _preview = ProfilePreview.Create(stage, new Vector2(280f, 404f));
            var hint = NLUi.Label(stage, "DragHint", "", 14f, new Color(0.32f, 0.4f, 0.5f), _font, FontStyles.Normal, TextAlignmentOptions.Center);
            LocText.Set(hint, "Kéo chuột để xoay", "Drag to rotate", "ドラッグで回転");
            NLUi.Size(hint, preferredHeight: 36f);

            var info = NLUi.Group(_characterWindow, "Info", true, 10f, TextAnchor.UpperLeft);
            NLUi.Size(info, 548f, flexibleWidth: 1f);
            var header = NLUi.Group(info, "Header", false, 14f, TextAnchor.MiddleLeft, false);
            var names = NLUi.Group(header, "Names", true, 2f, TextAnchor.MiddleLeft);
            NLUi.Size(names, flexibleWidth: 1f);
            LocText.Set(NLUi.Label(names, "Kicker", "", 15f, NLUi.Gold, _font, FontStyles.Bold), "Hồ sơ nhân vật", "Character profile", "じぶんのプロフィール");
            _characterName = NLUi.Label(names, "Name", "", 28f, NLUi.Text, _font, FontStyles.Bold);
            _characterLevel = NLUi.Label(names, "Level", "", 16f, NLUi.Muted, _font);
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);
            NLUi.CloseButton(_characterWindow, _font, () => close?.Invoke(), 46f, 18f);
            _characterExp = BarFill(info, "Exp", new Color(0.55f, 0.45f, 0.95f), 8f);

            // Progress tiles: money, knowledge, work, vocabulary — four separate ideas, never merged into XP.
            var tiles = NLUi.Group(info, "Tiles", false, 8f, TextAnchor.MiddleLeft, true);
            _statYen = Tile(tiles, "Yen", NLUi.Gold, () => NihongoLife.Core.Loc.T("Tiền", "Money", "お金"));
            _statKnowledge = Tile(tiles, "Knowledge", new Color(0.45f, 0.8f, 0.95f), () => NihongoLife.Core.Loc.T("Kiến thức", "Knowledge", "知識"));
            _statShifts = Tile(tiles, "Shifts", new Color(0.55f, 0.85f, 0.55f), () => NihongoLife.Core.Loc.T("Ca làm thêm", "Job shifts", "シフト"));
            _statWords = Tile(tiles, "Words", new Color(0.85f, 0.65f, 0.95f), () => NihongoLife.Core.Loc.T("Từ đã học", "Words learned", "覚えた言葉"));

            LocText.Set(NLUi.Label(info, "NeedsTitle", "", 17f, NLUi.Soft, _font, FontStyles.Bold), "Nhu cầu", "Needs", "ステータス");
            foreach (var (key, glyph, vi, color) in Needs) _characterBars[key] = BarRow(info, key, vi, color);
            _characterInfo = NLUi.Label(info, "Details", "", 16f, NLUi.Text, _font);
            _characterInfo.textWrappingMode = TextWrappingModes.Normal;
            _characterWindow.gameObject.SetActive(false);
        }

        private TextMeshProUGUI Tile(RectTransform parent, string id, Color color, Func<string> label)
        {
            var tile = NLUi.Panel(parent, "Tile_" + id, new Color(1f, 1f, 1f, 0.05f), new RectOffset(12, 12, 8, 8), 0f);
            var value = NLUi.Label(tile, "Value", "0", 22f, color, _font, FontStyles.Bold);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            LocText.Set(NLUi.Label(tile, "Label", "", 13f, NLUi.Muted, _font), label);
            return value;
        }

        // ─────────── Refresh ───────────

        private float _nextRefresh;

        private void Update()
        {
            Bind();
            if (Time.unscaledTime >= _nextRefresh) { _nextRefresh = Time.unscaledTime + 0.5f; Refresh(); }
            AnimateFills();
            bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
            bool hide = dialogueOpen || UiModalStack.ImmersiveOpen;
            _dockGroup.alpha = Mathf.MoveTowards(_dockGroup.alpha, hide ? 0f : 1f, Time.unscaledDeltaTime * 6f);
            _dockGroup.blocksRaycasts = !hide;
            var status = PlayerStatus.Instance;
            if (status == null) return;
            string critical = null;
            float criticalValue = 1f;
            foreach (var (key, _, vi, color) in Needs)
            {
                float value = Value01(status, key);
                if (_rings.TryGetValue(key, out Ring ring))
                {
                    ring.Fill.color = Urgency(color, value); ring.Icon.color = ring.Fill.color; ring.Value.color = ring.Fill.color;
                    // In danger the whole ring throbs, not only its colour.
                    float pulse = value < PlayerStatus.DangerThreshold ? 1f + 0.09f * Flash(value) : 1f;
                    ring.Fill.transform.parent.localScale = Vector3.one * pulse;
                }
                if (_characterBars.TryGetValue(key, out Bar bar)) bar.Fill.color = Urgency(color, value);
                // The most urgent need wins the chip (health first when equal).
                if (value < 0.3f && value < criticalValue) { critical = key; criticalValue = value; }
            }
            if (status.IsExhausted && critical == null) critical = "exhausted";
            CriticalNeed = critical;
            _warning.gameObject.SetActive(critical != null && !hide);
            if (StatusWidget != null) _warning.anchoredPosition = new Vector2(24f, 24f + StatusWidget.rect.height + 10f);
            if (critical != null)
            {
                bool zero = criticalValue <= 0.001f;
                _warningText.text = (critical, zero) switch
                {
                    ("health", _) => criticalValue < PlayerStatus.DangerThreshold
                        ? NihongoLife.Core.Loc.F("SẮP NGẤT ({0:0}%) — ăn, uống hoặc ngủ ngay! Ngất sẽ mất 3 giờ và tiền viện phí", "ABOUT TO FAINT ({0:0}%) — eat, drink or sleep now! Fainting costs 3 hours and a clinic fee", "倒れそう（{0:0}%）— すぐ食べる・飲む・寝る！倒れると3時間と診察料を失います", criticalValue * 100f)
                        : NihongoLife.Core.Loc.T("Sức khỏe yếu — nghỉ ngơi và ăn uống", "Low health — rest, eat and drink", "体力が低い — 休んで、食べて、飲みましょう"),
                    ("hunger", true) => NihongoLife.Core.Loc.T("ĐANG ĐÓI LẢ — mất máu dần và đi chậm 20%. Mua đồ ăn ở konbini!", "STARVING — losing health and 20% slower. Buy food at the konbini!", "お腹がペコペコ — 体力が減り、20%遅くなります。コンビニで食べ物を！"),
                    ("hunger", false) => NihongoLife.Core.Loc.F("Đói ({0:0}%) — khi về 0 sẽ mất máu và đi chậm", "Hungry ({0:0}%) — at 0 you lose health and slow down", "空腹（{0:0}%）— 0になると体力が減り、遅くなります", criticalValue * 100f),
                    ("thirst", true) => NihongoLife.Core.Loc.T("ĐANG KHÁT KHÔ — mất máu dần, không chạy được. Uống nước ngay!", "PARCHED — losing health and can't run. Drink water now!", "のどがカラカラ — 体力が減り、走れません。すぐ水を！"),
                    ("thirst", false) => NihongoLife.Core.Loc.F("Khát ({0:0}%) — khi về 0 sẽ mất máu, không chạy được", "Thirsty ({0:0}%) — at 0 you lose health and can't run", "のどが渇いた（{0:0}%）— 0になると体力が減り、走れません", criticalValue * 100f),
                    ("rest", true) => NihongoLife.Core.Loc.F("GỤC NGÃ SAU {0:0}s — về phòng ngủ ngay!", "COLLAPSING IN {0:0}s — go to bed now!", "あと{0:0}秒で倒れます — すぐ部屋で寝て！", Mathf.Max(0f, PlayerStatus.FallAsleepAfterSeconds - status.NoddingOffSeconds)),
                    ("rest", false) => NihongoLife.Core.Loc.F("Buồn ngủ (tỉnh táo {0:0}%) — khi về 0 sẽ đi chậm rồi ngủ gục ngoài đường", "Sleepy ({0:0}% alert) — at 0 you slow down, then fall asleep in the street", "眠い（{0:0}%）— 0になると遅くなり、道で眠ってしまいます", criticalValue * 100f),
                    _ => NihongoLife.Core.Loc.T("Hết sức — đi chậm lại hoặc nghỉ một chút", "Exhausted — slow down or take a short rest", "へとへと — ゆっくり歩くか、少し休みましょう"),
                };
                // The chip flashes with the danger, faster as the value nears 0.
                var chipImage = _warning.GetComponent<Image>();
                if (chipImage != null) chipImage.color = criticalValue < PlayerStatus.DangerThreshold
                    ? Color.Lerp(new Color(0.75f, 0.12f, 0.1f, 0.97f), new Color(0.35f, 0.05f, 0.05f, 0.97f), Flash(criticalValue))
                    : new Color(0.55f, 0.32f, 0.08f, 0.95f);
            }
            UpdateVignette(status, hide);
            UpdateWeatherChip(status, hide);
        }

        private static float Value01(PlayerStatus status, string key) => key switch
        {
            "health" => status.CurrentHealth / Mathf.Max(1f, status.MaxHealth),
            "energy" => status.CurrentEnergy / Mathf.Max(1f, status.MaxEnergy),
            "hunger" => status.Hunger / 100f,
            "thirst" => status.Thirst / 100f,
            _ => status.Restfulness / 100f,
        };

        private static float Raw(PlayerStatus status, string key) => key switch
        {
            "health" => status.CurrentHealth,
            "energy" => status.CurrentEnergy,
            "hunger" => status.Hunger,
            "thirst" => status.Thirst,
            _ => status.Restfulness,
        };

        private void Bind()
        {
            if (_boundStatus != PlayerStatus.Instance)
            {
                if (_boundStatus != null) _boundStatus.OnStatusChanged -= Refresh;
                _boundStatus = PlayerStatus.Instance;
                if (_boundStatus != null) _boundStatus.OnStatusChanged += Refresh;
                Refresh();
            }
            if (_boundInventory != PlayerInventory.Instance)
            {
                if (_boundInventory != null) _boundInventory.OnInventoryChanged -= Refresh;
                _boundInventory = PlayerInventory.Instance;
                if (_boundInventory != null) _boundInventory.OnInventoryChanged += Refresh;
                Refresh();
            }
        }

        public void Refresh()
        {
            var status = PlayerStatus.Instance;
            var inventory = PlayerInventory.Instance;
            RefreshObjective();
            if (status == null) return;
            int yen = inventory != null ? inventory.Yen : 0;
            string place = WorldLocationCatalog.Get(SceneManager.GetActiveScene().name).VietnameseName;
            _name.text = status.PlayerName;
            _level.text = place;
            _levelBadge.text = status.Level.ToString();
            _wallet.text = yen.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            SetFill(_expFill, status.MaxExp > 0 ? status.CurrentExp / (float)status.MaxExp : 0f);
            foreach (var (key, _, _, _) in Needs)
            {
                if (!_rings.TryGetValue(key, out Ring ring)) continue;
                float need = Value01(status, key);
                SetFill(ring.Fill, need);
                ring.Value.text = $"{Mathf.RoundToInt(Mathf.Clamp01(need) * 100f)}%";
            }

            if (_characterWindow == null || !_characterWindow.gameObject.activeInHierarchy) return;
            _characterName.text = status.PlayerName;
            _characterLevel.text = NihongoLife.Core.Loc.F("Cấp {0}  ·  {1}/{2} XP tới cấp {3}", "Level {0}  ·  {1}/{2} XP to level {3}", "レベル{0}  ·  レベル{3}まで {1}/{2} XP", status.Level, status.CurrentExp, status.MaxExp, status.Level + 1);
            SetFill(_characterExp, status.MaxExp > 0 ? status.CurrentExp / (float)status.MaxExp : 0f);
            foreach (var (key, _, _, _) in Needs)
            {
                if (!_characterBars.TryGetValue(key, out Bar bar)) continue;
                SetFill(bar.Fill, Value01(status, key));
                bar.Value.text = $"{Mathf.RoundToInt(Raw(status, key))}/100";
            }
            var progress = GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;
            int shifts = progress?.careers?.Sum(c => c.totalShifts) ?? 0;
            int words = (progress?.island?.words?.Count ?? 0) + (progress?.masteryLevels?.Count ?? 0);
            _statYen.text = NihongoLife.Core.Loc.Yen(yen);
            _statKnowledge.text = status.Knowledge.ToString();
            _statShifts.text = shifts.ToString();
            _statWords.text = words.ToString();
            (string kicker, string text, string progress)? tracked = null;
            try { tracked = HudObjective.Provider?.Invoke(); } catch { }
            _characterInfo.text =
                $"<color=#A8B4C4>{NihongoLife.Core.Loc.T("Đang ở", "Location", "現在地")}</color>  {WorldLocationCatalog.Get(SceneManager.GetActiveScene().name).DisplayName}      " +
                $"<color=#A8B4C4>{NihongoLife.Core.Loc.T("Balo", "Bag", "かばん")}</color>  {(inventory != null ? inventory.Items.Count : 0)}/{(inventory != null ? inventory.MaxSlots : 0)}\n" +
                $"<color=#A8B4C4>{NihongoLife.Core.Loc.T("Đang theo dõi", "Tracking", "追跡中")}</color>  {(tracked != null ? tracked.Value.text : "—")}";
        }

        private void OnDestroy()
        {
            if (_boundStatus != null) _boundStatus.OnStatusChanged -= Refresh;
            if (_boundInventory != null) _boundInventory.OnInventoryChanged -= Refresh;
        }
    }
}
