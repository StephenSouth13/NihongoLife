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
            public Bar(Image fill, TextMeshProUGUI value) { Fill = fill; Value = value; }
            public Image Fill { get; }
            public TextMeshProUGUI Value { get; }
        }

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
            _bars["health"] = BarRow(card, "体", "Thể lực", new Color(0.9f, 0.36f, 0.36f));
            _bars["energy"] = BarRow(card, "元", "Năng lượng", new Color(0.39f, 0.71f, 0.96f));
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
            NLUi.Size(value, 36f);
            return new Bar(fill, value);
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

        private static void SetFill(Image fill, float value01)
        {
            if (fill == null) return;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }

        private void BuildCharacterWindow(RectTransform root, Action close)
        {
            _characterWindow = NLUi.Panel(root, "CharacterWindow", NLUi.Ink, new RectOffset(32, 32, 26, 26), 12f);
            NLUi.Anchor(_characterWindow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 0f));
            NLUi.FitContent(_characterWindow);
            var header = NLUi.Group(_characterWindow, "Header", false, 14f, TextAnchor.MiddleLeft, false);
            NLUi.Size(NLUi.Pill(header, "Avatar", "学", _font, NLUi.Gold, NLUi.Ink, 34f), 72f, 72f);
            var names = NLUi.Group(header, "Names", true, 2f, TextAnchor.MiddleLeft);
            NLUi.Size(names, flexibleWidth: 1f);
            NLUi.Label(names, "Kicker", "じぶん · Nhân vật", 15f, NLUi.Gold, _font, FontStyles.Bold);
            _characterName = NLUi.Label(names, "Name", "", 28f, NLUi.Text, _font, FontStyles.Bold);
            _characterLevel = NLUi.Label(names, "Level", "", 16f, NLUi.Muted, _font);
            NLUi.Size(NLUi.Button(header, "Close", "Tab · Đóng", _font, () => close?.Invoke(), NLUi.Card, 16f, NLUi.Muted, 44f), 130f, 44f);
            _characterExp = BarFill(_characterWindow, "Exp", new Color(0.55f, 0.45f, 0.95f), 8f);
            NLUi.Divider(_characterWindow);
            _characterBars["health"] = BarRow(_characterWindow, "体", "Thể lực", new Color(0.9f, 0.36f, 0.36f));
            _characterBars["energy"] = BarRow(_characterWindow, "元", "Năng lượng", new Color(0.39f, 0.71f, 0.96f));
            _characterBars["hunger"] = BarRow(_characterWindow, "食", "No", new Color(0.95f, 0.72f, 0.29f));
            _characterBars["thirst"] = BarRow(_characterWindow, "水", "Khát", new Color(0.3f, 0.82f, 0.78f));
            _characterBars["rest"] = BarRow(_characterWindow, "眠", "Tỉnh táo", new Color(0.7f, 0.6f, 0.95f));
            NLUi.Divider(_characterWindow);
            _characterInfo = NLUi.Label(_characterWindow, "Info", "", 17f, NLUi.Text, _font);
            NLUi.Label(_characterWindow, "Tip", "Mẹo: ăn ở konbini để hồi No/Khát, ngủ ở phòng trọ để hồi Năng lượng và Tỉnh táo.", 14f, NLUi.Muted, _font);
            _characterWindow.gameObject.SetActive(false);
        }

        private float _nextRefresh;

        private void Update()
        {
            Bind();
            if (Time.unscaledTime >= _nextRefresh) { _nextRefresh = Time.unscaledTime + 1f; Refresh(); }
            bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
            float alpha = dialogueOpen ? 0f : 1f;
            _dockGroup.alpha = Mathf.MoveTowards(_dockGroup.alpha, alpha, Time.unscaledDeltaTime * 6f);
            _dockGroup.blocksRaycasts = !dialogueOpen;
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

        private static void Apply(Dictionary<string, Bar> bars, PlayerStatus status)
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
