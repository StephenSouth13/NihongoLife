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

        private static readonly (string id, string vi)[] Filters =
        {
            ("all", "Tất cả"), ("job", "Làm thêm"), ("farm", "Nông trại"), ("learning", "Học tập"), ("daily", "Hằng ngày"), ("story", "Cốt truyện"),
        };

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
                ui._player = FindFirstObjectByType<PlayerController>();
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

            var title = NLUi.Label(card, "Title", "Sổ nhiệm vụ  <size=65%><color=#A8B4C4>タスク · Task journal   [N]</color></size>", 30f, NLUi.Text, _font, FontStyles.Bold);
            Place(title.rectTransform, 28f, 22f, 800f, 44f);
            NLUi.CloseButton(card, _font, Close, 46f, 18f);

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
            if (current != null) entries.Add(new Entry { Story = current, Section = "active" });
            if (GameServices.TryGet(out ScenarioCampaignManager campaign))
                foreach (var scenario in campaign.GetAvailableBranches())
                    if (scenario != null && scenario != current) entries.Add(new Entry { Story = scenario, Section = "available" });
            var progress = GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;
            if (progress?.completedScenarios != null && GameServices.TryGet(out IScenarioRepository scenarios))
                foreach (var scenario in scenarios.GetAllScenarios())
                    if (scenario != null && scenario != current && progress.completedScenarios.Contains(scenario.id)) entries.Add(new Entry { Story = scenario, Section = "completed" });
            return entries;
        }

        private void Rebuild()
        {
            Clear(_filters);
            foreach (var (id, vi) in Filters)
            {
                bool on = _filter == id;
                string captured = id;
                var chip = NLUi.Button(_filters, "Filter_" + id, vi, _font, () => { _filter = captured; Rebuild(); }, on ? NLUi.Gold : new Color(1f, 1f, 1f, 0.07f), 16f, on ? NLUi.Ink : NLUi.Text, 38f);
                NLUi.Size(chip, 132f, 38f);
            }

            var entries = Collect().Where(e => _filter == "all" || e.Category == _filter).ToList();
            if (_selectedKey == null || entries.All(e => e.Key != _selectedKey))
                _selectedKey = (entries.FirstOrDefault(e => e.Section == "active") ?? entries.FirstOrDefault(e => e.Section == "available") ?? entries.FirstOrDefault())?.Key;

            Clear(_list);
            foreach (var (section, label) in new[] { ("active", "ĐANG LÀM"), ("available", "CÓ THỂ NHẬN"), ("completed", "ĐÃ XONG") })
            {
                var group = entries.Where(e => e.Section == section).ToList();
                if (group.Count == 0) continue;
                NLUi.Label(_list, "Section_" + section, $"{label}  <color=#6F7D8C>{group.Count}</color>", 13f, NLUi.Gold, _font, FontStyles.Bold);
                foreach (var entry in group.Take(10)) Row(entry);
            }
            if (entries.Count == 0) NLUi.Label(_list, "Empty", "Chưa có nhiệm vụ trong mục này.", 15f, NLUi.Muted, _font);
            RefreshDetailOnly();
        }

        private void Row(Entry entry)
        {
            bool selected = entry.Key == _selectedKey;
            string title = entry.Quest != null ? entry.Quest.titleVi : StoryTitle(entry.Story);
            string meta = entry.Quest != null ? RowMeta(entry.Quest) : "Cốt truyện";
            var button = NLUi.Button(_list, "Row_" + entry.Key, $"<b>{title}</b>\n<size=78%><color=#A8B4C4>{meta}</color></size>", _font,
                () => { _selectedKey = entry.Key; Rebuild(); }, selected ? new Color(0.95f, 0.7f, 0.2f, 0.22f) : new Color(1f, 1f, 1f, 0.05f), 16f, NLUi.Text, 58f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static string RowMeta(QuestDefinition quest)
        {
            string category = CategoryVi(quest.category);
            string reward = quest.rewards != null && quest.rewards.yen > 0 ? $"¥{quest.rewards.yen:N0}" : $"{quest.rewards?.xp ?? 0} XP";
            return QuestService.Availability(quest) switch
            {
                QuestAvailability.Active => $"{category} · đang làm · {reward}",
                QuestAvailability.Cooldown => $"{category} · ca sau {Mathf.CeilToInt((float)QuestService.CooldownLeft(quest).TotalMinutes)} phút",
                QuestAvailability.Locked => $"{category} · chưa mở",
                QuestAvailability.Completed => $"{category} · đã xong",
                _ => $"{category} · {reward}",
            };
        }

        private static string CategoryVi(string category) => category switch
        {
            "job" => "Làm thêm", "farm" => "Nông trại", "learning" => "Học tập", "daily" => "Hằng ngày", _ => "Cốt truyện",
        };

        private static string StoryTitle(ScenarioDefinition s) => string.IsNullOrWhiteSpace(s.titleVi) ? s.titleEn : s.titleVi;

        // ─────────── Detail ───────────

        private void RefreshDetailOnly()
        {
            if (_detail == null) return;
            Clear(_detail);
            var entry = Collect().FirstOrDefault(e => e.Key == _selectedKey);
            if (entry == null) { NLUi.Label(_detail, "Empty", "Chọn một nhiệm vụ ở bên trái.", 17f, NLUi.Muted, _font); return; }
            if (entry.Quest != null) QuestDetail(entry.Quest); else StoryDetail(entry);
        }

        private void QuestDetail(QuestDefinition quest)
        {
            var availability = QuestService.Availability(quest);
            var state = QuestService.State(quest.id);
            NLUi.Label(_detail, "Kicker", $"{CategoryVi(quest.category).ToUpperInvariant()}  ·  {StatusVi(availability)}", 13f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_detail, "Title", quest.titleVi, 26f, NLUi.Text, _font, FontStyles.Bold).textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(quest.titleJa)) NLUi.Label(_detail, "TitleJa", quest.titleJa, 15f, NLUi.Muted, _font);
            NLUi.Label(_detail, "Description", quest.descriptionVi, 16f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
            string requirement = QuestService.Requirement(quest);
            NLUi.Label(_detail, "Info",
                $"<color=#A8B4C4>Giao việc</color>  {quest.giverNameVi}      <color=#A8B4C4>Ở đâu</color>  {quest.locationVi}\n" +
                $"<color=#A8B4C4>Yêu cầu</color>  Cấp {quest.minLevel}" + (quest.minKnowledge > 0 ? $" · {quest.minKnowledge} kiến thức" : "") +
                (requirement == null ? "  <color=#74D680>✓ đủ điều kiện</color>" : $"  <color=#FF8A7A>✕ {requirement}</color>") +
                (quest.repeatable ? $"      <color=#A8B4C4>Lặp lại</color>  sau {quest.cooldownMinutes} phút" : ""), 15f, NLUi.Text, _font).textWrappingMode = TextWrappingModes.Normal;

            NLUi.Label(_detail, "ObjectivesTitle", "Mục tiêu", 17f, NLUi.Text, _font, FontStyles.Bold);
            for (int i = 0; i < quest.objectives.Length; i++)
            {
                var o = quest.objectives[i];
                int value = state != null && state.status == "active" && state.progress != null && i < state.progress.Count ? state.progress[i] : (availability == QuestAvailability.Completed || availability == QuestAvailability.Cooldown ? o.count : 0);
                bool done = value >= o.count;
                var row = NLUi.Group(_detail, "Objective_" + o.id, false, 10f, TextAnchor.MiddleLeft, false);
                NLUi.Size(NLUi.Label(row, "Check", done ? "✓" : "○", 18f, done ? new Color(0.45f, 0.84f, 0.5f) : NLUi.Muted, _font, FontStyles.Bold, TextAlignmentOptions.Center), 24f);
                var text = NLUi.Label(row, "Text", o.textVi + (o.afterAll ? "  <size=80%><color=#A8B4C4>(cuối cùng)</color></size>" : ""), 16f, done ? NLUi.Muted : NLUi.Text, _font);
                NLUi.Size(text, flexibleWidth: 1f);
                NLUi.Size(NLUi.Label(row, "Count", $"{value}/{o.count}", 16f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Right), 60f);
            }

            var rewards = quest.rewards ?? new QuestRewards();
            var tiles = NLUi.Group(_detail, "Rewards", false, 8f, TextAnchor.MiddleLeft, false);
            NLUi.Label(tiles, "RewardsTitle", "Phần thưởng", 15f, NLUi.Muted, _font);
            if (rewards.yen > 0) NLUi.Pill(tiles, "RewardYen", $"¥{rewards.yen:N0}", _font, NLUi.Gold, NLUi.Ink, 16f);
            if (rewards.xp > 0) NLUi.Pill(tiles, "RewardXp", $"+{rewards.xp} XP", _font, new Color(0.55f, 0.45f, 0.95f), Color.white, 16f);
            if (rewards.knowledge > 0) NLUi.Pill(tiles, "RewardKnowledge", $"+{rewards.knowledge} kiến thức", _font, new Color(0.3f, 0.62f, 0.82f), Color.white, 16f);
            foreach (var grant in quest.grantOnAccept ?? Array.Empty<QuestItemGrant>())
            {
                var holder = NLUi.Group(tiles, "Grant_" + grant.itemId, false, 4f, TextAnchor.MiddleLeft, false);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                icon.transform.SetParent(holder, false);
                icon.sprite = ItemIcons.Get(grant.itemId); icon.preserveAspect = true;
                icon.color = icon.sprite != null ? Color.white : Color.clear;
                var e = icon.GetComponent<LayoutElement>(); e.preferredWidth = e.preferredHeight = 30f;
                NLUi.Label(holder, "Text", $"được phát ×{grant.quantity}", 14f, NLUi.Muted, _font);
            }

            var actions = NLUi.Group(_detail, "Actions", false, 10f, TextAnchor.MiddleLeft, false);
            var feedback = NLUi.Label(_detail, "Feedback", "", 15f, new Color(1f, 0.55f, 0.45f), _font, FontStyles.Bold);
            switch (availability)
            {
                case QuestAvailability.Available:
                {
                    var accept = NLUi.Button(actions, "Accept", quest.IsJob ? "Nhận ca làm" : "Nhận nhiệm vụ", _font, () =>
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
                    var track = NLUi.Button(actions, "Track", tracked ? "✓ Đang theo dõi" : "Theo dõi trên HUD", _font, () => QuestService.Track(tracked ? "" : quest.id), new Color(0.2f, 0.42f, 0.6f), 17f, Color.white, 46f);
                    NLUi.Size(track, 220f, 46f);
                    var abandon = NLUi.Button(actions, "Abandon", quest.IsJob ? "Huỷ ca (không lương)" : "Huỷ nhiệm vụ", _font, () => QuestService.Abandon(quest.id), new Color(0.45f, 0.16f, 0.15f), 16f, Color.white, 46f);
                    NLUi.Size(abandon, 220f, 46f);
                    break;
                }
                case QuestAvailability.Cooldown:
                    NLUi.Label(actions, "Cooldown", $"Ca tiếp theo mở sau {Mathf.CeilToInt((float)QuestService.CooldownLeft(quest).TotalMinutes)} phút.", 16f, NLUi.Muted, _font);
                    break;
                case QuestAvailability.Locked:
                    NLUi.Label(actions, "Locked", requirement ?? "Chưa mở.", 16f, new Color(1f, 0.55f, 0.45f), _font);
                    break;
                case QuestAvailability.Completed:
                    NLUi.Label(actions, "Done", $"Đã hoàn thành {state?.timesCompleted ?? 1} lần.", 16f, new Color(0.45f, 0.84f, 0.5f), _font);
                    break;
            }
        }

        private void StoryDetail(Entry entry)
        {
            var s = entry.Story;
            NLUi.Label(_detail, "Kicker", $"CỐT TRUYỆN  ·  {(entry.Section == "active" ? "đang làm" : entry.Section == "available" ? "có thể bắt đầu" : "đã xong")}", 13f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_detail, "Title", StoryTitle(s), 26f, NLUi.Text, _font, FontStyles.Bold).textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(s.titleJa)) NLUi.Label(_detail, "TitleJa", s.titleJa, 15f, NLUi.Muted, _font);
            string description = s.descriptionEn;
            if (!string.IsNullOrWhiteSpace(description)) NLUi.Label(_detail, "Description", description, 16f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
            if (entry.Section == "active" && ScenarioManager.Instance != null)
            {
                NLUi.Label(_detail, "ObjectivesTitle", "Mục tiêu", 17f, NLUi.Text, _font, FontStyles.Bold);
                foreach (var o in ScenarioManager.Instance.Objectives)
                {
                    bool done = o.state == ObjectiveState.Completed;
                    string text = string.IsNullOrWhiteSpace(o.titleVi) ? o.titleEn : o.titleVi;
                    NLUi.Label(_detail, "Objective", $"{(done ? "<color=#74D680>✓</color>" : "○")}  {text}", 16f, done ? NLUi.Muted : NLUi.Text, _font).textWrappingMode = TextWrappingModes.Normal;
                }
            }
            if (s.rewardYen > 0) NLUi.Pill(_detail, "RewardYen", $"¥{s.rewardYen:N0}", _font, NLUi.Gold, NLUi.Ink, 16f);
            if (entry.Section == "available")
            {
                var start = NLUi.Button(_detail, "StartStory", "Bắt đầu", _font, () =>
                {
                    Close();
                    ScenarioManager.Instance?.StartScenario(s);
                }, NLUi.Gold, 18f, NLUi.Ink, 48f);
                NLUi.Size(start, 200f, 48f);
            }
        }

        private static string StatusVi(QuestAvailability a) => a switch
        {
            QuestAvailability.Active => "đang làm", QuestAvailability.Available => "có thể nhận", QuestAvailability.Cooldown => "chờ ca sau",
            QuestAvailability.Locked => "chưa mở", _ => "đã xong",
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
