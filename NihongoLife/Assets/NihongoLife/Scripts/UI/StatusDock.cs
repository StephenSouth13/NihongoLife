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
            public Ring(Image fill, TextMeshProUGUI glyph, Color color) { Fill = fill; Glyph = glyph; Color = color; }
            public Image Fill { get; }
            public TextMeshProUGUI Glyph { get; }
            public Color Color { get; }
        }

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

            BuildStatusWidget(dock);
            BuildObjectiveChip(dock);

            // Touch layouts have no keyboard: keep the action bar there only.
            if (HudCanvasFitter.IsTouchLayout(false))
            {
                var bar = NLUi.Group(dock, "ActionBar", false, 8f, TextAnchor.MiddleRight, false);
                NLUi.Anchor(bar, new Vector2(1f, 0f), new Vector2(-24f, 190f), new Vector2(760f, 64f));
                bar.pivot = new Vector2(1f, 0f);
                ((HorizontalLayoutGroup)bar.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                foreach (var (name, _, label, action) in buttons)
                {
                    var button = NLUi.Button(bar, name, label, _font, () => action?.Invoke(), NLUi.Ink, 16f, NLUi.Text, 62f);
                    NLUi.Size(button, 112f, 62f);
                }
            }

            BuildCharacterWindow(root, closeCharacter);
        }

        // ─────────── Compact status widget ───────────

        private void BuildStatusWidget(RectTransform dock)
        {
            var widget = NLUi.Panel(dock, "StatusWidget", new Color(0.04f, 0.055f, 0.07f, 0.9f), new RectOffset(14, 18, 12, 12), 14f, vertical: false);
            NLUi.Anchor(widget, Vector2.zero, new Vector2(24f, 24f), new Vector2(0f, 0f));
            widget.pivot = Vector2.zero;
            ((HorizontalLayoutGroup)widget.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.FitContent(widget, width: true, height: true);
            StatusWidget = widget;

            // Portrait: gold disc with the learner glyph and a level badge.
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            portrait.SetParent(widget, false);
            var portraitLayout = portrait.GetComponent<LayoutElement>();
            portraitLayout.preferredWidth = portraitLayout.minWidth = 70f; portraitLayout.preferredHeight = portraitLayout.minHeight = 70f;
            var disc = portrait.GetComponent<Image>(); disc.sprite = HudGraphics.Disc; disc.color = NLUi.Gold;
            var glyph = NLUi.Label(portrait, "Glyph", "学", 32f, NLUi.Ink, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(glyph.rectTransform);
            var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            badge.SetParent(portrait, false);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 0f); badge.pivot = new Vector2(0.75f, 0.25f); badge.sizeDelta = new Vector2(30f, 30f);
            var badgeImage = badge.GetComponent<Image>(); badgeImage.sprite = HudGraphics.Disc; badgeImage.color = new Color(0.55f, 0.45f, 0.95f);
            _levelBadge = NLUi.Label(badge, "Level", "1", 15f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(_levelBadge.rectTransform);

            var column = NLUi.Group(widget, "Column", true, 6f, TextAnchor.UpperLeft, false);
            var top = NLUi.Group(column, "Top", false, 10f, TextAnchor.MiddleLeft, false);
            _name = NLUi.Label(top, "Name", "", 18f, NLUi.Text, _font, FontStyles.Bold);
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            _level = NLUi.Label(top, "Place", "", 14f, NLUi.Muted, _font);
            _level.textWrappingMode = TextWrappingModes.NoWrap;
            _expFill = BarFill(column, "Exp", new Color(0.55f, 0.45f, 0.95f), 4f);
            var rings = NLUi.Group(column, "Needs", false, 8f, TextAnchor.MiddleLeft, false);
            foreach (var (key, needGlyph, _, color) in Needs) _rings[key] = MakeRing(rings, key, needGlyph, color);
            _wallet = NLUi.Label(rings, "Wallet", "", 16f, NLUi.Gold, _font, FontStyles.Bold);
            _wallet.textWrappingMode = TextWrappingModes.NoWrap;
            _wallet.margin = new Vector4(6f, 0f, 0f, 0f);

            // Critical warning chip, above the widget, only while a need is below 20 %.
            _warning = NLUi.Panel(dock, "NeedWarning", new Color(0.55f, 0.13f, 0.12f, 0.95f), new RectOffset(16, 16, 8, 8), 0f, vertical: false);
            NLUi.Anchor(_warning, Vector2.zero, new Vector2(24f, 150f), new Vector2(0f, 0f));
            _warning.pivot = Vector2.zero;
            NLUi.FitContent(_warning, width: true, height: true);
            _warningText = NLUi.Label(_warning, "Text", "", 16f, Color.white, _font, FontStyles.Bold);
            _warningText.textWrappingMode = TextWrappingModes.NoWrap;
            _warning.gameObject.SetActive(false);
        }

        private Ring MakeRing(RectTransform parent, string key, string glyph, Color color)
        {
            var holder = new GameObject("Need_" + key, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            var element = holder.GetComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 40f; element.preferredHeight = element.minHeight = 40f;
            var back = new GameObject("Track", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(holder, false); NLUi.Stretch(back.rectTransform);
            back.sprite = HudGraphics.Ring; back.color = new Color(1f, 1f, 1f, 0.12f); back.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(holder, false); NLUi.Stretch(fill.rectTransform);
            fill.sprite = HudGraphics.Ring; fill.color = color; fill.raycastTarget = false;
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = true;
            var label = NLUi.Label(holder, "Glyph", glyph, 15f, color, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(label.rectTransform);
            return new Ring(fill, label, color);
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
                    string title = string.IsNullOrWhiteSpace(objective.titleVi) ? objective.titleEn : objective.titleVi;
                    int done = scenario.Objectives.Count(o => o.state == ObjectiveState.Completed);
                    info = ("◆ CỐT TRUYỆN", title, $"{done}/{scenario.Objectives.Count} mục · N mở sổ nhiệm vụ");
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
            NLUi.Size(NLUi.Label(row, "Icon", icon, 16f, color, _font, FontStyles.Bold, TextAlignmentOptions.Center), 22f);
            NLUi.Size(NLUi.Label(row, "Label", label, 16f, NLUi.Muted, _font), 96f);
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

        private static Color Urgency(Color normal, float value01)
        {
            if (value01 >= 0.5f) return normal;
            if (value01 >= 0.2f) return Color.Lerp(new Color(0.96f, 0.68f, 0.22f), normal, (value01 - 0.2f) / 0.3f);
            return Color.Lerp(new Color(0.95f, 0.3f, 0.3f), new Color(0.6f, 0.15f, 0.15f), Mathf.PingPong(Time.unscaledTime * 2.5f, 1f));
        }

        // ─────────── Character window (Tab) ───────────

        private void BuildCharacterWindow(RectTransform root, Action close)
        {
            _characterWindow = NLUi.Panel(root, "CharacterWindow", NLUi.Ink, new RectOffset(24, 24, 22, 22), 20f, vertical: false);
            NLUi.Anchor(_characterWindow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 0f));
            NLUi.FitContent(_characterWindow);
            ((HorizontalLayoutGroup)_characterWindow.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;

            var stage = NLUi.Panel(_characterWindow, "ModelCard", new Color(0.07f, 0.09f, 0.12f, 1f), new RectOffset(0, 0, 0, 0), 0f);
            NLUi.Size(stage, 280f, 440f);
            _preview = ProfilePreview.Create(stage, new Vector2(280f, 404f));
            var hint = NLUi.Label(stage, "DragHint", "Kéo chuột để xoay", 14f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            NLUi.Size(hint, preferredHeight: 36f);

            var info = NLUi.Group(_characterWindow, "Info", true, 10f, TextAnchor.UpperLeft);
            NLUi.Size(info, 548f, flexibleWidth: 1f);
            var header = NLUi.Group(info, "Header", false, 14f, TextAnchor.MiddleLeft, false);
            var names = NLUi.Group(header, "Names", true, 2f, TextAnchor.MiddleLeft);
            NLUi.Size(names, flexibleWidth: 1f);
            NLUi.Label(names, "Kicker", "じぶん · Hồ sơ nhân vật", 15f, NLUi.Gold, _font, FontStyles.Bold);
            _characterName = NLUi.Label(names, "Name", "", 28f, NLUi.Text, _font, FontStyles.Bold);
            _characterLevel = NLUi.Label(names, "Level", "", 16f, NLUi.Muted, _font);
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);
            NLUi.CloseButton(_characterWindow, _font, () => close?.Invoke(), 46f, 18f);
            _characterExp = BarFill(info, "Exp", new Color(0.55f, 0.45f, 0.95f), 8f);

            // Progress tiles: money, knowledge, work, vocabulary — four separate ideas, never merged into XP.
            var tiles = NLUi.Group(info, "Tiles", false, 8f, TextAnchor.MiddleLeft, true);
            _statYen = Tile(tiles, "Tiền", NLUi.Gold);
            _statKnowledge = Tile(tiles, "Kiến thức", new Color(0.45f, 0.8f, 0.95f));
            _statShifts = Tile(tiles, "Ca làm thêm", new Color(0.55f, 0.85f, 0.55f));
            _statWords = Tile(tiles, "Từ đã học", new Color(0.85f, 0.65f, 0.95f));

            NLUi.Label(info, "NeedsTitle", "Nhu cầu", 17f, NLUi.Soft, _font, FontStyles.Bold);
            foreach (var (key, glyph, vi, color) in Needs) _characterBars[key] = BarRow(info, glyph, vi, color);
            _characterInfo = NLUi.Label(info, "Details", "", 16f, NLUi.Text, _font);
            _characterInfo.textWrappingMode = TextWrappingModes.Normal;
            _characterWindow.gameObject.SetActive(false);
        }

        private TextMeshProUGUI Tile(RectTransform parent, string label, Color color)
        {
            var tile = NLUi.Panel(parent, "Tile_" + label, new Color(1f, 1f, 1f, 0.05f), new RectOffset(12, 12, 8, 8), 0f);
            var value = NLUi.Label(tile, "Value", "0", 22f, color, _font, FontStyles.Bold);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.Label(tile, "Label", label, 13f, NLUi.Muted, _font);
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
            foreach (var (key, _, vi, color) in Needs)
            {
                float value = Value01(status, key);
                if (_rings.TryGetValue(key, out Ring ring)) { ring.Fill.color = Urgency(color, value); ring.Glyph.color = ring.Fill.color; }
                if (_characterBars.TryGetValue(key, out Bar bar)) bar.Fill.color = Urgency(color, value);
                if (value < 0.2f && critical == null) critical = key;
            }
            if (status.IsExhausted) critical ??= "exhausted";
            _warning.gameObject.SetActive(critical != null && !hide);
            if (critical != null) _warningText.text = critical switch
            {
                "health" => "体  Sức khỏe yếu — nghỉ ngơi và ăn uống",
                "energy" or "exhausted" => "元  Hết sức — đi chậm lại hoặc nghỉ",
                "hunger" => "食  Đói — mua đồ ăn ở konbini",
                "thirst" => "水  Khát — uống nước",
                _ => "眠  Buồn ngủ — về phòng ngủ một giấc",
            };
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
            _wallet.text = $"¥{yen:N0}";
            SetFill(_expFill, status.MaxExp > 0 ? status.CurrentExp / (float)status.MaxExp : 0f);
            foreach (var (key, _, _, _) in Needs)
                if (_rings.TryGetValue(key, out Ring ring)) SetFill(ring.Fill, Value01(status, key));

            if (_characterWindow == null || !_characterWindow.gameObject.activeInHierarchy) return;
            _characterName.text = status.PlayerName;
            _characterLevel.text = $"Cấp {status.Level}  ·  {status.CurrentExp}/{status.MaxExp} XP tới cấp {status.Level + 1}";
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
            _statYen.text = $"¥{yen:N0}";
            _statKnowledge.text = status.Knowledge.ToString();
            _statShifts.text = shifts.ToString();
            _statWords.text = words.ToString();
            (string kicker, string text, string progress)? tracked = null;
            try { tracked = HudObjective.Provider?.Invoke(); } catch { }
            _characterInfo.text =
                $"<color=#A8B4C4>Đang ở</color>  {WorldLocationCatalog.Get(SceneManager.GetActiveScene().name).DisplayName}      " +
                $"<color=#A8B4C4>Balo</color>  {(inventory != null ? inventory.Items.Count : 0)}/{(inventory != null ? inventory.MaxSlots : 0)} ô\n" +
                $"<color=#A8B4C4>Đang theo dõi</color>  {(tracked != null ? tracked.Value.text : "—")}";
        }

        private void OnDestroy()
        {
            if (_boundStatus != null) _boundStatus.OnStatusChanged -= Refresh;
            if (_boundInventory != null) _boundInventory.OnInventoryChanged -= Refresh;
        }
    }
}
