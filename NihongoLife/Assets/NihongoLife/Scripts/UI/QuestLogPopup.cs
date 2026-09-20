using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Save;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>
    /// Quest journal (key J): what you are doing now, which quests are available (unlocked by knowledge
    /// and prerequisites), and what you have completed. Each quest shows its briefing, objectives,
    /// location hint, requirements and rewards; available quests can be started from here and the
    /// current quest can point the way. Data comes from ScenarioDefinition, so a new quest asset
    /// appears here automatically.
    /// </summary>
    public class QuestLogPopup : MenuPopupBase
    {
        private enum Tab { Active, Available, Completed }

        private static readonly Dictionary<string, string> NpcNames = new Dictionary<string, string>
        {
            { "npc_neighbor_1", "Tanaka" }, { "npc_neighbor_2", "Suzuki" }, { "npc_neighbor_3", "Sato" },
            { "npc_cashier", "Ito" }, { "npc_teacher_morita", "Morita" }, { "npc_classmate_kim", "Kim" },
            { "npc_sushi_staff", "Aoki" }, { "npc_sushi_chef", "Ota" }, { "npc_ramen_owner", "Yamada" },
            { "npc_station_staff", "Kimura" }
        };

        private GameObject _dialoguePanel;
        private Tab _tab = Tab.Active;
        private readonly List<ScenarioDefinition> _rows = new List<ScenarioDefinition>();
        private ScenarioDefinition _selected;

        private RectTransform _listRoot;
        private RectTransform _detailRoot;
        private ScrollRect _detailScroll;
        private TextMeshProUGUI _detailText;
        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> _tabLabels = new List<TextMeshProUGUI>();
        private Button _actionButton;
        private TextMeshProUGUI _actionLabel;
        private Button _trackButton;
        private TextMeshProUGUI _trackLabel;
        private TextMeshProUGUI _hintText;

        protected override Vector2 CardSize => new Vector2(1180f, 680f);

        public void SetDialoguePanel(GameObject dialoguePanel) => _dialoguePanel = dialoguePanel;

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Nhật ký nhiệm vụ";
            en = "Quest journal";
            ja = "クエスト記録";
        }

        protected override void Update()
        {
            base.Update();

            if (Keyboard.current == null || !Keyboard.current.jKey.wasPressedThisFrame) return;
            if (IsOpen)
            {
                Hide();
                return;
            }

            bool dialogueOpen = _dialoguePanel != null && _dialoguePanel.activeInHierarchy;
            if (!dialogueOpen) Show();
        }

        // ──────────────────────── Build ────────────────────────

        protected override void Build(RectTransform card)
        {
            string[] labelsVi = { "Đang làm", "Có thể nhận", "Đã xong" };
            string[] labelsEn = { "Active", "Available", "Completed" };
            string[] labelsJa = { "進行中", "受けられる", "完了" };
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                var button = AddButton(card, string.Empty, 36f + i * 138f, 100f, 132f, 44f, false, 16f);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                Localize(label, labelsVi[i], labelsEn[i], labelsJa[i]);
                button.onClick.AddListener(() => SelectTab((Tab)captured));
                _tabButtons.Add(button);
                _tabLabels.Add(label);
            }

            CreateScroll(card, "List", 36f, 156f, 410f, 484f, out _listRoot);
            _detailScroll = CreateScroll(card, "Detail", 464f, 100f, 680f, 470f, out _detailRoot);
            _detailText = AddText(_detailRoot, string.Empty, 18f, 0f, 0f, 640f, 100f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
            _detailText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _detailText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _detailText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _detailText.rectTransform.sizeDelta = new Vector2(0f, 100f);
            _detailText.overflowMode = TextOverflowModes.Overflow;

            _actionButton = AddButton(card, string.Empty, 464f, 588f, 260f, 52f, true, 19f);
            _actionLabel = _actionButton.GetComponentInChildren<TextMeshProUGUI>();
            _actionButton.onClick.AddListener(OnAction);

            _trackButton = AddButton(card, string.Empty, 740f, 588f, 220f, 52f, false, 18f);
            _trackLabel = _trackButton.GetComponentInChildren<TextMeshProUGUI>();
            Localize(_trackLabel, "Chỉ đường", "Show the way", "道案内");
            _trackButton.onClick.AddListener(() => FindFirstObjectByType<QuestDirectionMarker>()?.ShowCurrentObjectiveTarget());

            _hintText = AddText(card, string.Empty, 14f, 976f, 596f, 168f, 40f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, true, Muted);
            _hintText.text = "J / ESC";
        }

        private ScrollRect CreateScroll(Transform parent, string name, float x, float y, float width, float height, out RectTransform content)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            Place((RectTransform)root.transform, x, y, width, height);
            root.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.8f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportRect, false);
            content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            return scroll;
        }

        // ──────────────────────── Lifecycle ────────────────────────

        protected override void OnOpened()
        {
            Scenario.ScenarioManager.Instance?.SetPlayerInputLocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Open on the tab that matters: the running quest if any, else what can be started.
            _tab = ScenarioManager.Instance != null && ScenarioManager.Instance.CurrentScenario != null ? Tab.Active : Tab.Available;
            SelectTab(_tab);
        }

        protected override void OnClosed()
        {
            Scenario.ScenarioManager.Instance?.SetPlayerInputLocked(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void OnLanguageApplied()
        {
            if (_selected != null) ShowDetail(_selected);
        }

        // ──────────────────────── Data ────────────────────────

        private void SelectTab(Tab tab)
        {
            _tab = tab;
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool selected = i == (int)tab;
                UIStyleKit.StyleButton(_tabButtons[i],
                    selected ? UIStyleKit.AccentGold : new Color(0.13f, 0.17f, 0.2f, 1f),
                    selected ? UIStyleKit.AccentGoldHover : UIStyleKit.PanelHover,
                    selected ? UIStyleKit.AccentGoldPressed : UIStyleKit.PanelPressed);
                _tabLabels[i].color = selected ? DarkText : Color.white;
            }

            CollectRows();
            RebuildList();

            _selected = _rows.Count > 0 ? _rows[0] : null;
            if (_selected != null) ShowDetail(_selected);
            else ShowEmpty();
        }

        private void CollectRows()
        {
            _rows.Clear();
            GameServices.TryGet(out IScenarioRepository repository);
            ScenarioDefinition current = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentScenario : null;
            PlayerProgressLite progress = ReadProgress();

            switch (_tab)
            {
                case Tab.Active:
                    if (current != null) _rows.Add(current);
                    break;
                case Tab.Available:
                    if (GameServices.TryGet(out ScenarioCampaignManager campaign))
                    {
                        foreach (var scenario in campaign.GetAvailableBranches())
                        {
                            if (scenario != current) _rows.Add(scenario);
                        }
                    }

                    break;
                default:
                    if (repository != null)
                    {
                        foreach (var scenario in repository.GetAllScenarios())
                        {
                            if (scenario != null && progress.completed.Contains(scenario.id)) _rows.Add(scenario);
                        }

                        _rows.Sort((a, b) => a.chapterIndex.CompareTo(b.chapterIndex));
                    }

                    break;
            }
        }

        private void RebuildList()
        {
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
            {
                var child = _listRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            foreach (var scenario in _rows)
            {
                var captured = scenario;
                var row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.transform.SetParent(_listRoot, false);
                row.GetComponent<LayoutElement>().preferredHeight = 74f;
                var button = row.GetComponent<Button>();
                UIStyleKit.StyleButton(button, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed);

                string title = Pick(scenario.titleEn, scenario.titleEn, scenario.titleJa);
                var text = AddText(row.transform, $"<b>{title}</b>\n<size=78%><color=#A9B7C4>{TypeLabel(scenario)}  ·  {Pick("Chương", "Chapter", "第")} {scenario.chapterIndex}</color></size>", 19f, 0f, 0f, 380f, 74f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false);
                var textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(14f, 4f);
                textRect.offsetMax = new Vector2(-10f, -4f);
                button.onClick.AddListener(() =>
                {
                    _selected = captured;
                    ShowDetail(captured);
                });
            }
        }

        private void ShowEmpty()
        {
            string message = _tab == Tab.Active
                ? Pick("Bạn chưa có nhiệm vụ đang làm. Mở tab 'Có thể nhận' để chọn một nhiệm vụ.", "No active quest. Open 'Available' to pick one.", "進行中のクエストはありません。「受けられる」から選びましょう。")
                : _tab == Tab.Available
                    ? Pick("Chưa có nhiệm vụ mới. Hãy hoàn thành nhiệm vụ hiện tại hoặc tích lũy thêm kiến thức.", "Nothing new yet. Finish your current quest or earn more knowledge.", "新しいクエストはまだありません。今のクエストを終えるか、知識を増やしましょう。")
                    : Pick("Bạn chưa hoàn thành nhiệm vụ nào.", "No completed quests yet.", "完了したクエストはまだありません。");
            _detailText.text = $"<color=#A9B7C4>{message}</color>";
            _actionButton.gameObject.SetActive(false);
            _trackButton.gameObject.SetActive(false);
        }

        private void ShowDetail(ScenarioDefinition scenario)
        {
            PlayerProgressLite progress = ReadProgress();
            ScenarioDefinition current = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentScenario : null;
            bool isCurrent = scenario == current;
            bool completed = progress.completed.Contains(scenario.id);

            var sb = new StringBuilder();
            sb.Append("<size=140%><b>").Append(Pick(scenario.titleEn, scenario.titleEn, scenario.titleJa)).Append("</b></size>\n");
            sb.Append("<color=#A9B7C4>").Append(scenario.titleJa).Append("   ·   ").Append(TypeLabel(scenario)).Append("</color>\n\n");

            string briefing = Pick(scenario.briefingVi, scenario.briefingEn, scenario.briefingJa);
            if (string.IsNullOrWhiteSpace(briefing)) briefing = Pick(scenario.descriptionEn, scenario.descriptionEn, scenario.descriptionJa);
            if (!string.IsNullOrWhiteSpace(briefing)) sb.Append(briefing).Append("\n\n");

            if (!string.IsNullOrWhiteSpace(scenario.giverNpcId))
            {
                sb.Append("<color=#F2B840><b>").Append(Pick("Người giao", "Given by", "依頼人")).Append(":</b></color> ").Append(NpcName(scenario.giverNpcId)).Append('\n');
            }

            string where = Pick(scenario.locationHintVi, scenario.locationHintEn, scenario.locationHintJa);
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append("<color=#F2B840><b>").Append(Pick("Địa điểm", "Where", "場所")).Append(":</b></color> ").Append(where).Append('\n');
            }

            sb.Append('\n').Append("<color=#F2B840><b>").Append(Pick("Mục tiêu", "Objectives", "目標")).Append(":</b></color>\n");
            foreach (var objective in scenario.objectives)
            {
                string mark = "○";
                string color = "#DCE5EC";
                if (completed && !isCurrent)
                {
                    mark = "●";
                    color = "#7FD39B";
                }
                else if (isCurrent && ScenarioManager.Instance != null)
                {
                    var runtime = ScenarioManager.Instance.Objectives.Find(o => o.id == objective.id);
                    if (runtime != null && runtime.state == ObjectiveState.Completed) { mark = "●"; color = "#7FD39B"; }
                    else if (runtime != null && runtime.state == ObjectiveState.Active) { mark = "◐"; color = "#F2B840"; }
                    else if (runtime != null && runtime.state == ObjectiveState.Failed) { mark = "✕"; color = "#F0625A"; }
                }

                string optional = objective.isOptional ? Pick(" (tùy chọn)", " (optional)", "（任意）") : string.Empty;
                sb.Append("<color=").Append(color).Append('>').Append(mark).Append("  ").Append(Pick(objective.titleEn, objective.titleEn, objective.titleJa)).Append(optional).Append("</color>\n");
            }

            sb.Append('\n').Append("<color=#F2B840><b>").Append(Pick("Phần thưởng", "Rewards", "報酬")).Append(":</b></color> ");
            int targets = scenario.learningTargets != null ? scenario.learningTargets.Count : 0;
            sb.Append("~").Append(Mathf.Max(20, scenario.baseKnowledgeReward) + targets * 8).Append(' ').Append(Pick("kiến thức", "knowledge", "知識"));
            if (scenario.rewardYen > 0) sb.Append("   +¥").Append(scenario.rewardYen.ToString("N0"));
            sb.Append('\n');

            string requirement = RequirementText(scenario, progress);
            if (!string.IsNullOrEmpty(requirement))
            {
                sb.Append("<color=#F2B840><b>").Append(Pick("Điều kiện", "Requirements", "条件")).Append(":</b></color> ").Append(requirement).Append('\n');
            }

            if (scenario.learningTargets != null && scenario.learningTargets.Count > 0)
            {
                sb.Append("<color=#F2B840><b>").Append(Pick("Sẽ học", "You will learn", "学ぶこと")).Append(":</b></color> ").Append(string.Join(", ", scenario.learningTargets)).Append('\n');
            }

            if (scenario.supportsCoOp)
            {
                sb.Append("<color=#7FD39B>").Append(Pick("Có thể chơi cùng bạn bè (chế độ Online)", "Can be played with a friend (Online mode)", "友達と一緒に遊べます（オンライン）")).Append("</color>\n");
            }

            _detailText.text = sb.ToString();
            Canvas.ForceUpdateCanvases();
            if (_detailScroll != null) _detailScroll.verticalNormalizedPosition = 1f;

            // Actions
            bool canStart = !isCurrent && (!completed || scenario.repeatable);
            _actionButton.gameObject.SetActive(canStart);
            if (canStart)
            {
                _actionLabel.text = completed ? Pick("Chơi lại", "Play again", "もう一度") : Pick("Bắt đầu nhiệm vụ", "Start quest", "クエスト開始");
            }

            _trackButton.gameObject.SetActive(isCurrent);
        }

        private void OnAction()
        {
            if (_selected == null || ScenarioManager.Instance == null) return;

            ScenarioDefinition scenario = _selected;
            Hide();
            ScenarioManager.Instance.StartScenario(scenario);
        }

        // ──────────────────────── Helpers ────────────────────────

        private struct PlayerProgressLite
        {
            public HashSet<string> completed;
            public int knowledge;
        }

        private static PlayerProgressLite ReadProgress()
        {
            var result = new PlayerProgressLite { completed = new HashSet<string>(), knowledge = 0 };
            if (GameServices.TryGet(out IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                if (progress != null)
                {
                    result.knowledge = progress.knowledge;
                    if (progress.completedScenarios != null) result.completed = new HashSet<string>(progress.completedScenarios);
                }
            }

            return result;
        }

        private string RequirementText(ScenarioDefinition scenario, PlayerProgressLite progress)
        {
            var parts = new List<string>();
            if (scenario.requiredKnowledge > 0)
            {
                bool ok = progress.knowledge >= scenario.requiredKnowledge;
                parts.Add($"{(ok ? "✓" : "✕")} {scenario.requiredKnowledge} {Pick("kiến thức", "knowledge", "知識")}");
            }

            if (scenario.requiredScenarioIds != null)
            {
                GameServices.TryGet(out IScenarioRepository repository);
                foreach (string id in scenario.requiredScenarioIds)
                {
                    var required = repository?.GetScenarioById(id);
                    string name = required != null ? Pick(required.titleEn, required.titleEn, required.titleJa) : id;
                    parts.Add($"{(progress.completed.Contains(id) ? "✓" : "✕")} {name}");
                }
            }

            return string.Join("   ", parts);
        }

        private string TypeLabel(ScenarioDefinition scenario)
        {
            switch ((scenario.questType ?? "main").ToLowerInvariant())
            {
                case "side": return Pick("Phụ", "Side", "サブ");
                case "career": return Pick("Nghề nghiệp", "Career", "仕事");
                case "community": return Pick("Cộng đồng", "Community", "地域");
                case "coop": return Pick("Co-op", "Co-op", "協力");
                default: return Pick("Chính", "Main", "メイン");
            }
        }

        private static string NpcName(string npcId)
        {
            foreach (var npc in FindObjectsByType<NPC.NPCController>(FindObjectsSortMode.None))
            {
                if (npc.NpcId == npcId && !string.IsNullOrWhiteSpace(npc.DisplayName)) return npc.DisplayName;
            }

            return NpcNames.TryGetValue(npcId, out string name) ? name : npcId;
        }
    }
}
