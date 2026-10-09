using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Always-on HUD shared by every scene (the city hosts it, zones load on top):
    /// a vitals card bottom-left (health, energy, hunger, thirst, rest, knowledge, yen), an action bar
    /// bottom-right with a clickable button for every menu (bag B, character Tab, map M, quests J,
    /// exams K, settings Esc) and the character window opened by Tab. Hidden while a dialogue is open
    /// so it never covers the dialogue box.
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

        // Smoothly animated fills: Refresh sets targets, Update glides the bars towards them.
        private readonly Dictionary<Image, float> _fillTargets = new();
        private static readonly string[] WarnKeys = { "health", "energy", "hunger", "thirst", "rest" };

        private TMP_FontAsset _font;
        private CanvasGroup _dockGroup;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _level;
        private Image _expFill;
        private TextMeshProUGUI _footer;
        private readonly Dictionary<string, Bar> _bars = new();
        private readonly Dictionary<string, Bar> _characterBars = new();
        private RectTransform _characterWindow;
        private TextMeshProUGUI _characterName;
        private TextMeshProUGUI _characterLevel;
        private Image _characterExp;
        private TextMeshProUGUI _characterInfo;
        private ProfilePreview _preview;
        private PlayerStatus _boundStatus;
        private PlayerInventory _boundInventory;

        public GameObject CharacterRoot => _characterWindow != null ? _characterWindow.gameObject : null;

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

            // Vitals card.
            var card = NLUi.Panel(dock, "VitalsCard", NLUi.Ink, new RectOffset(18, 18, 14, 14), 7f);
            NLUi.Anchor(card, Vector2.zero, new Vector2(24f, 24f), new Vector2(360f, 0f));
            card.pivot = Vector2.zero;
            NLUi.FitContent(card);
            var header = NLUi.Group(card, "Header", false, 10f, TextAnchor.MiddleLeft, false);
            var avatar = NLUi.Pill(header, "Avatar", "学", _font, NLUi.Gold, NLUi.Ink, 22f);
            NLUi.Size(avatar, 44f, 44f);
            var names = NLUi.Group(header, "Names", true, 0f, TextAnchor.MiddleLeft);
            NLUi.Size(names, flexibleWidth: 1f);
            _name = NLUi.Label(names, "Name", "Học viên", 19f, NLUi.Text, _font, FontStyles.Bold);
            _level = NLUi.Label(names, "Level", "Lv 1", 15f, NLUi.Muted, _font);
            _expFill = BarFill(card, "Exp", new Color(0.55f, 0.45f, 0.95f), 5f);
            _bars["health"] = BarRow(card, "体", "Sức khỏe", new Color(0.9f, 0.36f, 0.36f));
            _bars["energy"] = BarRow(card, "元", "Thể lực", new Color(0.39f, 0.71f, 0.96f));
            _bars["hunger"] = BarRow(card, "食", "No", new Color(0.95f, 0.72f, 0.29f));
            _bars["thirst"] = BarRow(card, "水", "Khát", new Color(0.3f, 0.82f, 0.78f));
            _footer = NLUi.Label(card, "Footer", "", 15f, NLUi.Soft, _font);

            // Action bar.
            var bar = NLUi.Group(dock, "ActionBar", false, 8f, TextAnchor.MiddleRight, false);
            NLUi.Anchor(bar, new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(760f, 64f));
            bar.pivot = new Vector2(1f, 0f);
            ((HorizontalLayoutGroup)bar.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            foreach (var (name, key, label, action) in buttons)
            {
                var button = NLUi.Button(bar, name, $"<size=72%><color=#F2B233>{key}</color></size>\n{label}", _font, () => action?.Invoke(), NLUi.Ink, 16f, NLUi.Text, 62f);
                NLUi.Size(button, 112f, 62f);
            }

            BuildCharacterWindow(root, closeCharacter);
        }

        private Bar BarRow(RectTransform parent, string icon, string label, Color color)
        {
            var row = NLUi.Group(parent, "Row_" + label, false, 8f, TextAnchor.MiddleLeft, false);
            NLUi.Size(NLUi.Label(row, "Icon", icon, 15f, color, _font, FontStyles.Bold, TextAlignmentOptions.Center), 22f);
            NLUi.Size(NLUi.Label(row, "Label", label, 15f, NLUi.Muted, _font), 96f);
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            track.SetParent(row, false);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var element = track.GetComponent<LayoutElement>();
            element.preferredWidth = 150f; element.preferredHeight = 10f; element.flexibleWidth = 1f;
            var fill = Fill(track, color);
            var value = NLUi.Label(row, "Value", "100", 14f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Right);
            NLUi.Size(value, 62f);
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
            if (!_fillTargets.ContainsKey(fill)) fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            _fillTargets[fill] = Mathf.Clamp01(value01);
        }

        private void AnimateFills()
        {
            float step = Time.unscaledDeltaTime * 1.6f;
            foreach (var kv in _fillTargets)
            {
                if (kv.Key == null) continue;
                var rect = kv.Key.rectTransform;
                float x = Mathf.MoveTowards(rect.anchorMax.x, kv.Value, step);
                if (!Mathf.Approximately(x, rect.anchorMax.x)) rect.anchorMax = new Vector2(x, 1f);
            }
        }

        /// <summary>Normal colour above 50 %, amber below, pulsing red under 20 % (Sims-style urgency).</summary>
        private static void Tint(Bar bar, float value01, string warning)
        {
            if (bar.Fill == null) return;
            if (value01 >= 0.5f) bar.Fill.color = bar.Color;
            else if (value01 >= 0.2f) bar.Fill.color = Color.Lerp(new Color(0.96f, 0.68f, 0.22f), bar.Color, (value01 - 0.2f) / 0.3f);
            else
            {
                bar.Fill.color = Color.Lerp(new Color(0.95f, 0.3f, 0.3f), new Color(0.6f, 0.15f, 0.15f), Mathf.PingPong(Time.unscaledTime * 2.5f, 1f));
                if (warning != null) bar.Value.text = warning;
            }
        }

        private void BuildCharacterWindow(RectTransform root, Action close)
        {
            // Character profile (Tab): the real player model on the left (drag to rotate), identity, needs and
            // progress on the right. Same bars and colours as the HUD card so both read the same.
            _characterWindow = NLUi.Panel(root, "CharacterWindow", NLUi.Ink, new RectOffset(28, 28, 24, 24), 18f, vertical: false);
            NLUi.Anchor(_characterWindow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 0f));
            NLUi.FitContent(_characterWindow);
            ((HorizontalLayoutGroup)_characterWindow.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;

            var stage = NLUi.Panel(_characterWindow, "ModelCard", new Color(0.07f, 0.09f, 0.12f, 1f), new RectOffset(0, 0, 0, 0), 0f);
            NLUi.Size(stage, 380f, 560f);
            _preview = ProfilePreview.Create(stage, new Vector2(380f, 520f));
            var hint = NLUi.Label(stage, "DragHint", "Kéo chuột để xoay nhân vật", 14f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            NLUi.Size(hint, preferredHeight: 40f);

            var info = NLUi.Group(_characterWindow, "Info", true, 12f, TextAnchor.UpperLeft);
            NLUi.Size(info, 580f, flexibleWidth: 1f);
            var header = NLUi.Group(info, "Header", false, 14f, TextAnchor.MiddleLeft, false);
            var names = NLUi.Group(header, "Names", true, 2f, TextAnchor.MiddleLeft);
            NLUi.Size(names, flexibleWidth: 1f);
            NLUi.Label(names, "Kicker", "じぶん · Hồ sơ nhân vật", 15f, NLUi.Gold, _font, FontStyles.Bold);
            _characterName = NLUi.Label(names, "Name", "", 30f, NLUi.Text, _font, FontStyles.Bold);
            _characterLevel = NLUi.Label(names, "Level", "", 16f, NLUi.Muted, _font);
            NLUi.Label(header, "TabHint", "Tab", 15f, NLUi.Muted, _font).textWrappingMode = TextWrappingModes.NoWrap;
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);
            NLUi.CloseButton(_characterWindow, _font, () => close?.Invoke(), 46f, 18f);
            _characterExp = BarFill(info, "Exp", new Color(0.55f, 0.45f, 0.95f), 8f);
            NLUi.Divider(info);
            NLUi.Label(info, "NeedsTitle", "Nhu cầu", 17f, NLUi.Soft, _font, FontStyles.Bold);
            _characterBars["health"] = BarRow(info, "体", "Sức khỏe", new Color(0.9f, 0.36f, 0.36f));
            _characterBars["energy"] = BarRow(info, "元", "Thể lực", new Color(0.39f, 0.71f, 0.96f));
            _characterBars["hunger"] = BarRow(info, "食", "No", new Color(0.95f, 0.72f, 0.29f));
            _characterBars["thirst"] = BarRow(info, "水", "Khát", new Color(0.3f, 0.82f, 0.78f));
            _characterBars["rest"] = BarRow(info, "眠", "Tỉnh táo", new Color(0.7f, 0.6f, 0.95f));
            NLUi.Divider(info);
            _characterInfo = NLUi.Label(info, "Details", "", 17f, NLUi.Text, _font);
            NLUi.Label(info, "Tip", "Ăn/uống để hồi No/Khát; nghỉ để hồi Thể lực, ngủ để tỉnh táo.", 14f, NLUi.Muted, _font);
            _characterWindow.gameObject.SetActive(false);
        }

        private float _nextRefresh;

        private void Update()
        {
            Bind();
            if (Time.unscaledTime >= _nextRefresh) { _nextRefresh = Time.unscaledTime + 1f; Refresh(); }
            AnimateFills();
            bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
            bool hide = dialogueOpen || UiModalStack.ImmersiveOpen;
            _dockGroup.alpha = Mathf.MoveTowards(_dockGroup.alpha, hide ? 0f : 1f, Time.unscaledDeltaTime * 6f);
            _dockGroup.blocksRaycasts = !hide;
            var status = PlayerStatus.Instance;
            if (status == null) return;
            foreach (var bars in new[] { _bars, _characterBars })
            {
                if (bars.TryGetValue("health", out Bar health)) Tint(health, status.CurrentHealth / Mathf.Max(1f, status.MaxHealth), "Yếu!");
                if (bars.TryGetValue("hunger", out Bar hunger)) Tint(hunger, status.Hunger / 100f, "Đói!");
                if (bars.TryGetValue("thirst", out Bar thirst)) Tint(thirst, status.Thirst / 100f, "Khát!");
                if (bars.TryGetValue("rest", out Bar rest)) Tint(rest, status.Restfulness / 100f, "Buồn ngủ");
                if (bars.TryGetValue("energy", out Bar energy))
                {
                    if (status.IsExhausted)
                    {
                        // Out of breath: the energy bar pulses red until sprinting is allowed again.
                        energy.Fill.color = Color.Lerp(new Color(0.95f, 0.3f, 0.3f), new Color(0.6f, 0.15f, 0.15f), Mathf.PingPong(Time.unscaledTime * 2.5f, 1f));
                        energy.Value.text = "Hết sức";
                    }
                    else Tint(energy, status.CurrentEnergy / Mathf.Max(1f, status.MaxEnergy), null);
                }
            }
        }

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
            if (status == null) return;
            _name.text = status.PlayerName;
            _level.text = $"Lv {status.Level}  ·  {WorldLocationCatalog.Get(SceneManager.GetActiveScene().name).VietnameseName}";
            SetFill(_expFill, status.MaxExp > 0 ? status.CurrentExp / (float)status.MaxExp : 0f);
            Apply(_bars, status);
            _footer.text = $"知 Kiến thức {status.Knowledge}      ¥{(inventory != null ? inventory.Yen : 0):N0}";

            if (_characterWindow == null) return;
            _characterName.text = status.PlayerName;
            _characterLevel.text = $"Cấp {status.Level}  ·  {status.CurrentExp}/{status.MaxExp} XP";
            SetFill(_characterExp, status.MaxExp > 0 ? status.CurrentExp / (float)status.MaxExp : 0f);
            Apply(_characterBars, status);
            var scenario = ScenarioManager.Instance?.CurrentScenario;
            string place = WorldLocationCatalog.Get(SceneManager.GetActiveScene().name).DisplayName;
            _characterInfo.text =
                $"<color=#A8B4C4>Đang ở</color>   {place}\n" +
                $"<color=#A8B4C4>Nhiệm vụ</color>   {(scenario != null ? scenario.titleEn : "—")}\n" +
                $"<color=#A8B4C4>Kiến thức</color>   {status.Knowledge}      <color=#A8B4C4>Tiền</color>   ¥{(inventory != null ? inventory.Yen : 0):N0}      " +
                $"<color=#A8B4C4>Đồ trong balo</color>   {(inventory != null ? inventory.Items.Count : 0)}";
        }

        private void Apply(Dictionary<string, Bar> bars, PlayerStatus status)
        {
            void Set(string key, float value, float max)
            {
                if (!bars.TryGetValue(key, out Bar bar)) return;
                SetFill(bar.Fill, max > 0f ? value / max : 0f);
                bar.Value.text = Mathf.RoundToInt(value).ToString();
            }
            Set("health", status.CurrentHealth, status.MaxHealth);
            Set("energy", status.CurrentEnergy, status.MaxEnergy);
            Set("hunger", status.Hunger, 100f);
            Set("thirst", status.Thirst, 100f);
            Set("rest", status.Restfulness, 100f);
        }

        private void OnDestroy()
        {
            if (_boundStatus != null) _boundStatus.OnStatusChanged -= Refresh;
            if (_boundInventory != null) _boundInventory.OnInventoryChanged -= Refresh;
        }
    }
}
