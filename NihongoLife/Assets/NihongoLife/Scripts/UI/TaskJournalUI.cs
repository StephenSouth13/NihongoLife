using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Save;
using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Task Journal (N): the one place for story quests, part-time jobs, farm missions, learning tasks and daily
    /// activities. Left: category filter and Active / Available / Completed lists. Right: the selected task —
    /// description, giver and place, requirements, objectives with their real progress, rewards (¥ / XP / knowledge
    /// from the progression data) and Accept / Cancel / Track. Story entries come from the scenario system; every
    /// other entry from Resources/Progression/progression.json.
    /// </summary>
    public sealed class TaskJournalUI : MonoBehaviour
    {
        private sealed class Entry
        {
            public QuestDefinition Quest;
            public ScenarioDefinition Story;
            public string Section;     // active | available | completed
            public string Category => Quest != null ? Quest.category : "story";
            public string Key => Quest != null ? "q:" + Quest.id : "s:" + Story.id;
        }

        private static readonly string[] Filters = { "all", "job", "farm", "learning", "daily", "story" };

        private static string FilterName(string id) => id == "all" ? Loc.T("Tất cả", "All", "すべて") : CategoryName(id);

        private static TaskJournalUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _list, _detail, _filters;
        private string _filter = "all";
        private string _selectedKey;
        private PlayerController _player;
        private bool _wasLocked;
        private float _nextRefresh;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeSelf;
        public static string SelectedQuestId => _instance != null && _instance._selectedKey != null && _instance._selectedKey.StartsWith("q:") ? _instance._selectedKey.Substring(2) : null;

        private static TaskJournalUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("TaskJournalCanvas", 125);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<TaskJournalUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Rebuild(); });
                UiModalStack.Register(_instance, () => IsOpen, () => Close(), "Task journal");
                QuestService.Changed += () => { if (IsOpen) _instance.Rebuild(); };
                return _instance;
            }
        }

        public static void Toggle() { if (IsOpen) Close(); else Open(); }

        public static void Open(string questId = null)
        {
            var ui = Instance;
            if (questId != null) { ui._selectedKey = "q:" + questId; ui._filter = "all"; }
            if (!ui._root.gameObject.activeSelf)
            {
                ui._root.gameObject.SetActive(true);
                ui._player = SceneRefs.Find<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }
            ui._root.SetAsLastSibling();
            ui.Rebuild();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _instance._root.gameObject.SetActive(false);
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
        }

        public static void Select(string questId) { if (IsOpen) { _instance._selectedKey = "q:" + questId; _instance.Rebuild(); } }
        /// <summary>Shows a story quest (story filter), e.g. the next chapter step.</summary>
        public static void SelectStory(string scenarioId) { if (IsOpen) { _instance._selectedKey = "s:" + scenarioId; _instance._filter = "story"; _instance.Rebuild(); } }

        private void Build(RectTransform canvas)
        {
            _root = new GameObject("TaskJournal", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.72f);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            card.SetParent(_root, false);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(1140f, 700f);
            card.GetComponent<Image>().color = NLUi.Ink;
            card.gameObject.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);

            var title = NLUi.Label(card, "Title", "", 30f, NLUi.Text, _font, FontStyles.Bold);
            LocText.Set(title, () => Loc.T("Sổ nhiệm vụ", "Task journal", "タスク帳") + "  <size=65%><color=#A8B4C4>[N]</color></size>");
            Place(title.rectTransform, 28f, 22f, 800f, 44f);
            NLUi.CloseButton(card, _font, Close, 46f, 18f);
            // Careers (from level 10) live in their own window, one click from the journal.
            var career = NLUi.Button(card, "CareerButton", "", _font, () => { Close(); CareerUI.Open(); }, new Color(0.2f, 0.42f, 0.6f), 16f, Color.white, 40f);
            LocText.Set(career.GetComponentInChildren<TextMeshProUGUI>(), () => Loc.T("Nghề nghiệp", "Career", "仕事"));
            Place((RectTransform)career.transform, 880f, 24f, 170f, 40f);

            _filters = NLUi.Group(card, "Filters", false, 8f, TextAnchor.MiddleLeft, false);
            Place(_filters, 28f, 76f, 1080f, 40f);

            var listPanel = new GameObject("ListPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            listPanel.SetParent(card, false);
            Place(listPanel, 28f, 128f, 380f, 548f);
            listPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.035f);
            _list = NLUi.Group(listPanel, "List", true, 6f);
            _list.anchorMin = new Vector2(0f, 1f); _list.anchorMax = new Vector2(1f, 1f); _list.pivot = new Vector2(0.5f, 1f);
            _list.offsetMin = new Vector2(10f, 0f); _list.offsetMax = new Vector2(-10f, -10f);
            NLUi.FitContent(_list);
            // The list can be longer than the panel (unlock card + quests): clip and scroll it.
            listPanel.gameObject.AddComponent<RectMask2D>();
            var scroll = listPanel.gameObject.AddComponent<ScrollRect>();
            scroll.content = _list;
            scroll.viewport = listPanel;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            detailPanel.SetParent(card, false);
            Place(detailPanel, 424f, 128f, 688f, 548f);
            detailPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.035f);
            _detail = NLUi.Group(detailPanel, "Detail", true, 10f);
            _detail.anchorMin = new Vector2(0f, 1f); _detail.anchorMax = new Vector2(1f, 1f); _detail.pivot = new Vector2(0.5f, 1f);
            _detail.offsetMin = new Vector2(22f, 0f); _detail.offsetMax = new Vector2(-22f, -18f);
            NLUi.FitContent(_detail);
            _root.gameObject.SetActive(false);
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private void Update()
        {
            // Live progress while open (cooldown timers, objectives advanced by the world).
            if (IsOpen && Time.unscaledTime > _nextRefresh) { _nextRefresh = Time.unscaledTime + 1f; RefreshDetailOnly(); }
        }

        // ─────────── Entries ───────────

        private List<Entry> Collect()
        {
            var entries = new List<Entry>();
            foreach (var quest in QuestService.Catalog.quests)
            {
                var availability = QuestService.Availability(quest);
                string section = availability switch
                {
                    QuestAvailability.Active => "active",
                    QuestAvailability.Completed => "completed",
                    QuestAvailability.Cooldown => "completed",
                    _ => "available",
                };
                entries.Add(new Entry { Quest = quest, Section = section });
            }
            var current = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentScenario : null;
            if (current != null && StoryDirector.IsDone(current.id)) current = null; // finished: listed under completed
            if (current != null) entries.Add(new Entry { Story = current, Section = "active" });
            if (GameServices.TryGet(out ScenarioCampaignManager campaign))
                foreach (var scenario in campaign.GetAvailableBranches())
                    if (scenario != null && scenario != current) entries.Add(new Entry { Story = scenario, Section = "available" });
            // Next story steps that only wait for knowledge: shown with what is missing, so the goal is visible.
            foreach (var scenario in StoryDirector.Remaining())
                if (scenario != current && entries.All(e => e.Story != scenario) && (scenario.requiredScenarioIds ?? new List<string>()).All(StoryDirector.IsDone))
                    entries.Add(new Entry { Story = scenario, Section = "available" });
            var progress = GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;
            if (progress?.completedScenarios != null && GameServices.TryGet(out IScenarioRepository scenarios))
                foreach (var scenario in scenarios.GetAllScenarios())
                    if (scenario != null && scenario != current && progress.completedScenarios.Contains(scenario.id)) entries.Add(new Entry { Story = scenario, Section = "completed" });
            return entries;
        }

        private void Rebuild()
        {
            Clear(_filters);
            foreach (var id in Filters)
            {
                bool on = _filter == id;
                string captured = id;
                var chip = NLUi.Button(_filters, "Filter_" + id, FilterName(id), _font, () => { _filter = captured; Rebuild(); }, on ? NLUi.Gold : new Color(1f, 1f, 1f, 0.07f), 16f, on ? NLUi.Ink : NLUi.Text, 38f);
                NLUi.Size(chip, 132f, 38f);
            }

            var entries = Collect().Where(e => _filter == "all" || e.Category == _filter).ToList();
            if (_selectedKey == null || entries.All(e => e.Key != _selectedKey))
                _selectedKey = (entries.FirstOrDefault(e => e.Section == "active") ?? entries.FirstOrDefault(e => e.Section == "available") ?? entries.FirstOrDefault())?.Key;

            Clear(_list);
            UnlockProgress();
            foreach (var (section, label) in new[] { ("active", Loc.T("ĐANG LÀM", "IN PROGRESS", "進行中")), ("available", Loc.T("CÓ THỂ NHẬN", "AVAILABLE", "受けられる")), ("completed", Loc.T("ĐÃ XONG", "DONE", "完了")) })
            {
                var group = entries.Where(e => e.Section == section).ToList();
                if (group.Count == 0) continue;
                NLUi.Label(_list, "Section_" + section, $"{label}  <color=#6F7D8C>{group.Count}</color>", 13f, NLUi.Gold, _font, FontStyles.Bold);
                foreach (var entry in group.Take(10)) Row(entry);
            }
            if (entries.Count == 0) NLUi.Label(_list, "Empty", Loc.T("Chưa có nhiệm vụ trong mục này.", "No tasks in this category yet.", "このカテゴリーにはまだタスクがありません。"), 15f, NLUi.Muted, _font);
            RefreshDetailOnly();
        }

        private void Row(Entry entry)
        {
            bool selected = entry.Key == _selectedKey;
            string title = entry.Quest != null ? entry.Quest.Title : StoryTitle(entry.Story);
            string meta = entry.Quest != null ? RowMeta(entry.Quest) : CategoryName("story");
            var button = NLUi.Button(_list, "Row_" + entry.Key, $"<b>{title}</b>\n<size=78%><color=#A8B4C4>{meta}</color></size>", _font,
                () => { _selectedKey = entry.Key; Rebuild(); }, selected ? new Color(0.95f, 0.7f, 0.2f, 0.22f) : new Color(1f, 1f, 1f, 0.05f), 16f, NLUi.Text, 58f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static string RowMeta(QuestDefinition quest)
        {
            string category = CategoryName(quest.category);
            string reward = quest.rewards != null && quest.rewards.yen > 0 ? $"¥{quest.rewards.yen:N0}" : $"{quest.rewards?.xp ?? 0} XP";
            return QuestService.Availability(quest) switch
            {
                QuestAvailability.Active => $"{category} · {StatusName(QuestAvailability.Active)} · {reward}",
                QuestAvailability.Cooldown => category + " · " + Loc.F("ca sau {0} phút", "next shift in {0} min", "次のシフトまで{0}分", Mathf.CeilToInt((float)QuestService.CooldownLeft(quest).TotalMinutes)),
                QuestAvailability.Locked => $"{category} · {StatusName(QuestAvailability.Locked)}",
                QuestAvailability.Completed => $"{category} · {StatusName(QuestAvailability.Completed)}",
                _ => $"{category} · {reward}",
            };
        }

        private static string CategoryName(string category) => category switch
        {
            "job" => Loc.T("Làm thêm", "Part-time job", "アルバイト"), "farm" => Loc.T("Nông trại", "Farm", "農場"),
            "learning" => Loc.T("Học tập", "Learning", "勉強"), "daily" => Loc.T("Hằng ngày", "Daily", "毎日"), _ => Loc.T("Cốt truyện", "Story", "ストーリー"),
        };

        private static string StoryTitle(ScenarioDefinition s) => s.DisplayTitle;

        // ─────────── Detail ───────────

        /// <summary>Where knowledge takes you next: a progress bar to the next place and the list of places.</summary>
        /// <summary>
        /// Lộ trình mở bản đồ: Hibari Mart and the school are always open; every other place is a stage that needs
        /// Trí tuệ and the story quest before it (LocationUnlocks). The next stage is highlighted with its bar.
        /// </summary>
        private void UnlockProgress()
        {
            var card = NLUi.Panel(_list, "Unlocks", new Color(0.12f, 0.16f, 0.2f, 1f), new RectOffset(14, 14, 10, 12), 6f);
            int knowledge = LocationUnlocks.Knowledge;
            var next = LocationUnlocks.Next();
            NLUi.Label(card, "Title", Loc.F("LỘ TRÌNH MỞ BẢN ĐỒ  <color=#A8B4C4>· {0} trí tuệ</color>", "MAP ROADMAP  <color=#A8B4C4>· {0} knowledge</color>", "マップの開放  <color=#A8B4C4>· 知識 {0}</color>", knowledge), 13f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(card, "Always", "<size=85%><color=#7BD88F>✓</color> " + Loc.T("Hibari Mart · Trường Hibari — luôn mở", "Hibari Mart · Hibari School — always open", "ひばりマート・ひばり学校 — いつでも行ける") + "</size>", 15f, NLUi.Muted, _font);
            foreach (var u in LocationUnlocks.All)
            {
                if (u == null) continue;
                bool open = LocationUnlocks.IsUnlocked(u.sceneName);
                bool isNext = next != null && next.sceneName == u.sceneName;
                var row = NLUi.Panel(card, "Stage_" + u.sceneName, isNext ? new Color(0.97f, 0.74f, 0.24f, 0.12f) : new Color(1f, 1f, 1f, 0.03f), new RectOffset(10, 10, 6, 7), 3f);
                row.GetComponent<Image>().sprite = UiKit.Rounded(10);
                string place = WorldLocationCatalog.Get(u.sceneName).DisplayName;
                NLUi.Label(row, "Name", (open ? "<color=#7BD88F>✓</color> " : isNext ? "<color=#F7BD3D>▶</color> " : "<color=#8B95A3>×</color> ") + $"<b>{place}</b>  <size=80%><color=#A8B4C4>{LocationUnlocks.StageTitle(u)}</color></size>", 15f, open ? NLUi.Muted : NLUi.Text, _font);
                if (open) continue;
                if (u.requiredKnowledge > 0)
                {
                    var bar = NLUi.Panel(row, "Track", new Color(1f, 1f, 1f, 0.08f));
                    NLUi.Size(bar, -1f, 7f);
                    var fill = NLUi.Panel(bar, "Fill", knowledge >= u.requiredKnowledge ? NLUi.Good : NLUi.Gold);
                    fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(knowledge / (float)u.requiredKnowledge), 1f); fill.offsetMin = fill.offsetMax = Vector2.zero;
                    NLUi.Label(row, "Knowledge", "<size=80%>" + Loc.F("Trí tuệ {0}/{1}", "Knowledge {0}/{1}", "知識 {0}/{1}", Mathf.Min(knowledge, u.requiredKnowledge), u.requiredKnowledge) + "</size>", 13f, NLUi.Muted, _font);
                }
                foreach (var id in u.requiredScenarioIds ?? System.Array.Empty<string>())
                {
                    bool done = StoryDirector.IsDone(id);
                    NLUi.Label(row, "Req", "<size=80%>" + (done ? "<color=#7BD88F>✓</color> " : "○ ") + LocationUnlocks.QuestTitle(id) + "</size>", 13f, done ? NLUi.Muted : NLUi.Soft, _font);
                }
            }
            NLUi.Label(card, "How", "<size=80%>" + Loc.T("Tăng trí tuệ: học ở Trường Hibari, bài thi JLPT, bàn học ở nhà, nhiệm vụ và cốt truyện.", "Raise knowledge: classes at Hibari School, JLPT exams, the study desk at home, tasks and the story.", "知識の上げ方：ひばり学校の授業、JLPT模試、家の机、タスク、ストーリー。") + "</size>", 14f, NLUi.Muted, _font);
        }

        private void RefreshDetailOnly()
        {
            if (_detail == null) return;
            Clear(_detail);
            var entry = Collect().FirstOrDefault(e => e.Key == _selectedKey);
            if (entry == null) { NLUi.Label(_detail, "Empty", Loc.T("Chọn một nhiệm vụ ở bên trái.", "Pick a task on the left.", "左からタスクを選んでください。"), 17f, NLUi.Muted, _font); return; }
            if (entry.Quest != null) QuestDetail(entry.Quest); else StoryDetail(entry);
        }

        private void QuestDetail(QuestDefinition quest)
        {
            var availability = QuestService.Availability(quest);
            var state = QuestService.State(quest.id);
            NLUi.Label(_detail, "Kicker", $"{CategoryName(quest.category).ToUpperInvariant()}  ·  {StatusName(availability)}", 13f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_detail, "Title", quest.Title, 26f, NLUi.Text, _font, FontStyles.Bold).textWrappingMode = TextWrappingModes.Normal;
            NLUi.Label(_detail, "Description", quest.Description, 16f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
            string requirement = QuestService.Requirement(quest);
            string requirementLine = StoryDirector.Bypass
                ? "<color=#74D680>✓ " + Loc.T("Mở sẵn — không khoá cấp", "Open — no level lock", "開放済み — レベル制限なし") + "</color>"
                : Loc.F("<color=#A8B4C4>Yêu cầu</color>  Cấp {0}", "<color=#A8B4C4>Requires</color>  Level {0}", "<color=#A8B4C4>条件</color>  レベル{0}", quest.minLevel) + (quest.minKnowledge > 0 ? Loc.F(" · {0} trí tuệ", " · {0} knowledge", " · 知識{0}", quest.minKnowledge) : "") +
                  (requirement == null ? "  <color=#74D680>✓ " + Loc.T("đủ điều kiện", "requirements met", "条件クリア") + "</color>" : $"  <color=#FF8A7A>✕ {requirement}</color>");
            NLUi.Label(_detail, "Info",
                Loc.F("<color=#A8B4C4>Giao việc</color>  {0}      <color=#A8B4C4>Ở đâu</color>  {1}\n", "<color=#A8B4C4>Given by</color>  {0}      <color=#A8B4C4>Where</color>  {1}\n", "<color=#A8B4C4>依頼者</color>  {0}      <color=#A8B4C4>場所</color>  {1}\n", quest.GiverName, quest.Location) +
                requirementLine +
                (quest.repeatable ? Loc.F("      <color=#A8B4C4>Lặp lại</color>  sau {0} phút", "      <color=#A8B4C4>Repeats</color>  after {0} min", "      <color=#A8B4C4>くり返し</color>  {0}分後", quest.cooldownMinutes) : ""), 15f, NLUi.Text, _font).textWrappingMode = TextWrappingModes.Normal;

            NLUi.Label(_detail, "ObjectivesTitle", Loc.T("Mục tiêu", "Objectives", "目標"), 17f, NLUi.Text, _font, FontStyles.Bold);
            for (int i = 0; i < quest.objectives.Length; i++)
            {
                var o = quest.objectives[i];
                int value = state != null && state.status == "active" && state.progress != null && i < state.progress.Count ? state.progress[i] : (availability == QuestAvailability.Completed || availability == QuestAvailability.Cooldown ? o.count : 0);
                bool done = value >= o.count;
                var row = NLUi.Group(_detail, "Objective_" + o.id, false, 10f, TextAnchor.MiddleLeft, false);
                NLUi.Size(NLUi.Label(row, "Check", done ? "✓" : "○", 18f, done ? new Color(0.45f, 0.84f, 0.5f) : NLUi.Muted, _font, FontStyles.Bold, TextAlignmentOptions.Center), 24f);
                var text = NLUi.Label(row, "Text", o.Text + (o.afterAll ? "  <size=80%><color=#A8B4C4>" + Loc.T("(cuối cùng)", "(last)", "（最後）") + "</color></size>" : ""), 16f, done ? NLUi.Muted : NLUi.Text, _font);
                NLUi.Size(text, flexibleWidth: 1f);
                NLUi.Size(NLUi.Label(row, "Count", $"{value}/{o.count}", 16f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Right), 60f);
            }

            var rewards = quest.rewards ?? new QuestRewards();
            var tiles = NLUi.Group(_detail, "Rewards", false, 8f, TextAnchor.MiddleLeft, false);
            NLUi.Label(tiles, "RewardsTitle", Loc.T("Phần thưởng", "Rewards", "報酬"), 15f, NLUi.Muted, _font);
            if (rewards.yen > 0) NLUi.Pill(tiles, "RewardYen", Loc.Yen(rewards.yen), _font, NLUi.Gold, NLUi.Ink, 16f);
            if (rewards.xp > 0) NLUi.Pill(tiles, "RewardXp", $"+{rewards.xp} XP", _font, new Color(0.55f, 0.45f, 0.95f), Color.white, 16f);
            if (rewards.knowledge > 0) NLUi.Pill(tiles, "RewardKnowledge", Loc.F("+{0} trí tuệ", "+{0} knowledge", "知識 +{0}", rewards.knowledge), _font, new Color(0.3f, 0.62f, 0.82f), Color.white, 16f);
            foreach (var grant in quest.grantOnAccept ?? Array.Empty<QuestItemGrant>())
            {
                var holder = NLUi.Group(tiles, "Grant_" + grant.itemId, false, 4f, TextAnchor.MiddleLeft, false);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                icon.transform.SetParent(holder, false);
                icon.sprite = ItemIcons.Get(grant.itemId); icon.preserveAspect = true;
                icon.color = icon.sprite != null ? Color.white : Color.clear;
                var e = icon.GetComponent<LayoutElement>(); e.preferredWidth = e.preferredHeight = 30f;
                NLUi.Label(holder, "Text", Loc.F("được phát ×{0}", "given ×{0}", "支給 ×{0}", grant.quantity), 14f, NLUi.Muted, _font);
            }

            var actions = NLUi.Group(_detail, "Actions", false, 10f, TextAnchor.MiddleLeft, false);
            var feedback = NLUi.Label(_detail, "Feedback", "", 15f, new Color(1f, 0.55f, 0.45f), _font, FontStyles.Bold);
            switch (availability)
            {
                case QuestAvailability.Available:
                {
                    var accept = NLUi.Button(actions, "Accept", quest.IsJob ? Loc.T("Nhận ca làm", "Take the shift", "シフトに入る") : Loc.T("Nhận nhiệm vụ", "Accept task", "受ける"), _font, () =>
                    {
                        string error = QuestService.Accept(quest.id);
                        if (error != null) feedback.text = error;
                    }, NLUi.Gold, 18f, NLUi.Ink, 48f);
                    NLUi.Size(accept, 220f, 48f);
                    break;
                }
                case QuestAvailability.Active:
                {
                    bool tracked = QuestService.TrackedId == quest.id;
                    var track = NLUi.Button(actions, "Track", tracked ? Loc.T("✓ Đang theo dõi", "✓ Tracking", "✓ 追跡中") : Loc.T("Theo dõi trên HUD", "Track on HUD", "HUDで追跡"), _font, () => QuestService.Track(tracked ? "" : quest.id), new Color(0.2f, 0.42f, 0.6f), 17f, Color.white, 46f);
                    NLUi.Size(track, 220f, 46f);
                    var abandon = NLUi.Button(actions, "Abandon", quest.IsJob ? Loc.T("Huỷ ca (không lương)", "Quit shift (no pay)", "シフトをやめる（給料なし）") : Loc.T("Huỷ nhiệm vụ", "Abandon task", "タスクをやめる"), _font, () => QuestService.Abandon(quest.id), new Color(0.45f, 0.16f, 0.15f), 16f, Color.white, 46f);
                    NLUi.Size(abandon, 220f, 46f);
                    break;
                }
                case QuestAvailability.Cooldown:
                    NLUi.Label(actions, "Cooldown", Loc.F("Ca tiếp theo mở sau {0} phút.", "The next shift opens in {0} min.", "次のシフトは{0}分後です。", Mathf.CeilToInt((float)QuestService.CooldownLeft(quest).TotalMinutes)), 16f, NLUi.Muted, _font);
                    break;
                case QuestAvailability.Locked:
                    NLUi.Label(actions, "Locked", requirement ?? Loc.T("Chưa mở.", "Locked.", "まだ開いていません。"), 16f, new Color(1f, 0.55f, 0.45f), _font);
                    break;
                case QuestAvailability.Completed:
                    NLUi.Label(actions, "Done", Loc.F("Đã hoàn thành {0} lần.", "Completed {0} times.", "{0}回完了しました。", state?.timesCompleted ?? 1), 16f, new Color(0.45f, 0.84f, 0.5f), _font);
                    break;
            }
        }

        private void StoryDetail(Entry entry)
        {
            var s = entry.Story;
            NLUi.Label(_detail, "Kicker", CategoryName("story").ToUpperInvariant() + "  ·  " + (entry.Section == "active" ? StatusName(QuestAvailability.Active) : entry.Section == "available" ? Loc.T("có thể bắt đầu", "can start", "開始できる") : StatusName(QuestAvailability.Completed)), 13f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_detail, "Title", StoryTitle(s), 26f, NLUi.Text, _font, FontStyles.Bold).textWrappingMode = TextWrappingModes.Normal;
            string description = s.Description;
            if (!string.IsNullOrWhiteSpace(description)) NLUi.Label(_detail, "Description", description, 16f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
            if (entry.Section == "active" && ScenarioManager.Instance != null)
            {
                NLUi.Label(_detail, "ObjectivesTitle", Loc.T("Mục tiêu", "Objectives", "目標"), 17f, NLUi.Text, _font, FontStyles.Bold);
                foreach (var o in ScenarioManager.Instance.Objectives)
                {
                    bool done = o.state == ObjectiveState.Completed;
                    string text = o.Title;
                    NLUi.Label(_detail, "Objective", $"{(done ? "<color=#74D680>✓</color>" : "○")}  {text}", 16f, done ? NLUi.Muted : NLUi.Text, _font).textWrappingMode = TextWrappingModes.Normal;
                }
            }
            if (s.rewardYen > 0) NLUi.Pill(_detail, "RewardYen", Loc.Yen(s.rewardYen), _font, NLUi.Gold, NLUi.Ink, 16f);
            string blocker = entry.Section == "available" ? StoryDirector.Blocker(s) : null;
            if (blocker != null)
            {
                NLUi.Label(_detail, "StoryBlocker", blocker, 16f, NLUi.Gold, _font, FontStyles.Bold).textWrappingMode = TextWrappingModes.Normal;
                NLUi.Label(_detail, "StoryBlockerHint", Loc.T("Học ở trường, bàn học, khu thi cử, làm thêm hay chơi Game Center đều cho điểm trí tuệ.", "School, your desk, the exam hall, jobs and the Game Center all give knowledge.", "学校・机・試験・アルバイト・ゲームセンターで知識ポイントがたまります。"), 15f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
            }
            else if (entry.Section == "available")
            {
                var start = NLUi.Button(_detail, "StartStory", Loc.T("Bắt đầu", "Start", "始める"), _font, () =>
                {
                    Close();
                    ScenarioManager.Instance?.StartScenario(s);
                }, NLUi.Gold, 18f, NLUi.Ink, 48f);
                NLUi.Size(start, 200f, 48f);
            }
        }

        private static string StatusName(QuestAvailability a) => a switch
        {
            QuestAvailability.Active => Loc.T("đang làm", "in progress", "進行中"), QuestAvailability.Available => Loc.T("có thể nhận", "available", "受けられる"),
            QuestAvailability.Cooldown => Loc.T("chờ ca sau", "waiting for next shift", "次のシフト待ち"),
            QuestAvailability.Locked => Loc.T("chưa mở", "locked", "未開放"), _ => Loc.T("đã xong", "done", "完了"),
        };

        private static void Clear(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
