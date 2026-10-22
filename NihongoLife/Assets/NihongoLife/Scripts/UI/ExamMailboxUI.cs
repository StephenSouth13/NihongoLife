using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Exam mailbox (key I, or the bell): every marked Writing / Speaking paper — by the AI examiner or by a teacher in
    /// a live Speaking exam — with its band ring, the four criteria and the feedback; "See the full paper" opens it in
    /// the test screen. Unread mails show a badge on the bell.
    /// </summary>
    public sealed class ExamMailboxUI : MonoBehaviour
    {
        private const string SeenKey = "NL.ExamMailbox.Seen";
        private static ExamMailboxUI _instance;
        private GameObject _window;
        private RectTransform _list, _detail;
        private TMP_Text _badge;
        private bool _open;
        private NihongoLife.Player.PlayerController _player;
        private NihongoLife.Cameras.ThirdPersonCameraController _camera;
        private bool _oldPlayerLock, _oldCameraLock, _oldCursorVisible;
        private CursorLockMode _oldCursorLock;
        private Button _openFull;
        private IeltsProductiveRecord _shown;

        private void Awake()
        {
            _instance = this;
            Build();
            UiModalStack.Register(this, () => _open, () => SetOpen(false), "Exam mailbox");
            Loc.Watch(this, () => { if (_open) { Refresh(); if (_shown != null) Show(_shown); } });
        }

        private GameObject _bell;

        private void Update()
        {
            // The bell belongs to the game, not to the main menu / boot screens.
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool inGame = scene != "01_MainMenu" && scene != "00_Bootstrap";
            if (_bell != null && _bell.activeSelf != inGame) _bell.SetActive(inGame);
            if (!inGame) { if (_open) SetOpen(false); return; }
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame && !UiModalStack.BlocksHotkeys)
                SetOpen(!_open);
        }

        public static void NotifyGraded(IeltsProductiveRecord record)
        {
            if (record == null) return;
            _instance?.Refresh();
            bool live = record.mode == "live";
            HudNotificationTray.Instance?.Post("exam-mail-" + record.id,
                live ? Loc.T("Giám khảo đã chấm bài Speaking", "Your examiner marked your Speaking", "試験官がスピーキングを採点しました") : Loc.T("Có thư chấm bài mới", "New marked paper", "新しい採点結果"),
                $"{SkillName(record.skill)} · {(record.overall > 0f ? "Band " + record.overall.ToString("0.0") : Loc.T("Xem phản hồi", "See feedback", "フィードバックを見る"))}",
                () => _instance?.OpenRecord(record.id), HudNoticeTone.Warning, true);
        }

        /// <summary>Opens the mailbox on one paper (from a notification or a live result).</summary>
        public static void Open(string recordId) => _instance?.OpenRecord(recordId);

        private static string SkillName(string skill) => skill == "writing" ? Loc.T("Viết (Writing)", "Writing", "ライティング") : Loc.T("Nói (Speaking)", "Speaking", "スピーキング");

        public void SetOpen(bool open)
        {
            if (_open == open) return;
            if (open)
            {
                _player = SceneRefs.Find<NihongoLife.Player.PlayerController>();
                _camera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
                _oldPlayerLock = _player != null && _player.InputLocked;
                _oldCameraLock = _camera != null && _camera.IsLocked;
                _oldCursorLock = Cursor.lockState;
                _oldCursorVisible = Cursor.visible;
                if (_player != null) _player.InputLocked = true;
                if (_camera != null) _camera.IsLocked = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if (_player != null) _player.InputLocked = _oldPlayerLock;
                if (_camera != null) _camera.IsLocked = _oldCameraLock;
                Cursor.lockState = _oldCursorLock;
                Cursor.visible = _oldCursorVisible;
            }
            _open = open;
            _window.SetActive(open);
            if (open) UiModalStack.Register(this, () => _open, () => SetOpen(false), "Exam mailbox");
            if (open) { Refresh(); UIStyleKit.PlayShowAnimation(_window); }
        }

        private void OpenRecord(string id)
        {
            SetOpen(true);
            Show(IeltsProductiveStore.All(null).FirstOrDefault(r => r.id == id));
        }

        // ─────────── Building ───────────

        private void Build()
        {
            var canvas = NLUi.CreateCanvas("ExamMailboxCanvas", 175, transform);

            // Bell (draggable HUD icon) with the unread badge.
            var bell = UiKit.Card(canvas.transform, "NotificationBell", new Color(0.08f, 0.12f, 0.18f, 0.96f), 18, 0.35f, true);
            _bell = bell.gameObject;
            var bellRect = bell.rectTransform;
            UiKit.Pin(bellRect, new Vector2(1f, 1f), -74f, -84f, 62f, 62f);
            var toggle = bell.gameObject.AddComponent<Button>();
            toggle.targetGraphic = bell;
            UIStyleKit.StyleButton(toggle, bell.color, new Color(0.16f, 0.22f, 0.3f, 1f), UiTheme.GoldDeep);
            bell.sprite = UiKit.Rounded(18);
            toggle.onClick.AddListener(() => SetOpen(!_open));
            var icon = UiKit.Icon(bellRect, "inbox", UiTheme.Gold, 32f);
            UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 32f, 32f);
            var badgeRoot = UiKit.Image(bellRect, "UnreadBadge", UiTheme.Bad, UiKit.Rounded(12));
            UiKit.Pin(badgeRoot.rectTransform, new Vector2(1f, 1f), 6f, 6f, 26f, 24f);
            _badge = UiKit.Label(badgeRoot.transform, "Count", "", 13f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(_badge.rectTransform);
            badgeRoot.transform.SetAsLastSibling();
            bell.gameObject.AddComponent<DraggableHudIcon>().Initialize("NL.ExamMailbox.IconPosition");

            // Window.
            var window = UiKit.Card(canvas.transform, "MailboxWindow", UiTheme.Ink, 24, 0.6f, true);
            _window = window.gameObject;
            var rect = window.rectTransform;
            UiKit.Pin(rect, new Vector2(1f, 0.5f), -28f, 0f, 820f, 700f);
            var glow = UiKit.Image(rect, "Glow", UiKit.Alpha(UiTheme.Gold, 0.08f), UiKit.VerticalFade);
            UiKit.Strip(glow.rectTransform, true, 0f, 100f, 24f, 24f);
            var headIcon = UiKit.Image(rect, "HeadDisc", UiKit.Alpha(UiTheme.Gold, 0.16f), UiKit.Circle);
            UiKit.Place(headIcon.rectTransform, 30f, 28f, 48f, 48f);
            var hi = UiKit.Icon(headIcon.transform, "inbox", UiTheme.Gold, 26f);
            UiKit.Pin(hi.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 26f, 26f);
            var title = UiKit.Label(rect, "Title", "Hộp thư kết quả", "Results mailbox", "結果ボックス", 26f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 92f, 26f, 600f, 34f);
            var hint = UiKit.Label(rect, "Hint", "Bài Writing/Speaking đã chấm · phím I để đóng/mở", "Marked Writing/Speaking papers · key I to open/close", "採点済みのライティング/スピーキング · Iキーで開閉", 14f, UiTheme.Muted);
            UiKit.Place(hint.rectTransform, 92f, 60f, 640f, 22f);
            UiKit.CloseButton(rect, () => SetOpen(false), 24f);

            _list = UiKit.Scroll(rect, "Messages", out var _, 8f);
            UiKit.Place((RectTransform)_list.parent.parent, 26f, 106f, 300f, 566f);

            var detailCard = UiKit.Card(rect, "DetailCard", UiTheme.Surface, 18, 0f, false);
            UiKit.Place(detailCard.rectTransform, 342f, 106f, 452f, 500f);
            _detail = UiKit.Scroll(detailCard.rectTransform, "DetailScroll", out var _, 10f, new RectOffset(20, 20, 18, 18));
            UiKit.Fill((RectTransform)_detail.parent.parent);

            _openFull = UiKit.Button(rect, "OpenFullResult", "Xem bài đầy đủ", "See the full paper", "答案をすべて見る", OpenFull, UiKit.Kind.Primary, "article", 16f);
            UiKit.Place((RectTransform)_openFull.transform, 342f, 620f, 452f, 50f);
            _openFull.gameObject.SetActive(false);
            _window.SetActive(false);
            RefreshBadge();
        }

        private void Refresh()
        {
            UiKit.Clear(_list);
            var records = IeltsProductiveStore.All(null).OrderByDescending(r => r.finishedUtc).Take(12).ToList();
            var seen = Seen();
            if (records.Count == 0)
            {
                var empty = UiKit.Label(_list, "Empty", "Chưa có bài đã chấm.", "No marked paper yet.", "採点済みの答案はまだありません。", 15f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
                UiKit.Size(empty, -1f, 60f);
            }
            foreach (var record in records)
            {
                var r = record;
                bool selected = _shown != null && _shown.id == r.id;
                var b = UiKit.Button(_list, "Mail_" + r.id, null, null, null, () => Show(r), selected ? UiKit.Kind.Accent : UiKit.Kind.Ghost, null, 15f, 14);
                UiKit.Size(b, -1f, 74f);
                UiKit.LabelOf(b).gameObject.SetActive(false);
                bool live = r.mode == "live";
                var disc = UiKit.Image(b.transform, "Disc", UiKit.Alpha(live ? UiTheme.Live : UiTheme.Ielts, 0.2f), UiKit.Circle);
                UiKit.Place(disc.rectTransform, 12f, 15f, 44f, 44f);
                var ic = UiKit.Icon(disc.transform, r.skill == "writing" ? "edit_note" : "mic", live ? UiTheme.Live : UiTheme.Ielts, 22f);
                UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 22f, 22f);
                var name = UiKit.Label(b.transform, "Skill", SkillName(r.skill), 15f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(name.rectTransform, 66f, 12f, 160f, 22f);
                string who = live ? Loc.T("Giám khảo", "Examiner", "試験官") : "AI";
                var meta = UiKit.Label(b.transform, "Meta", $"{who} · {Date(r.finishedUtc)}", 12.5f, UiTheme.Muted);
                UiKit.Place(meta.rectTransform, 66f, 38f, 170f, 20f);
                var band = UiKit.Label(b.transform, "Band", r.overall > 0f ? r.overall.ToString("0.0") : "—", 20f, r.overall > 0f ? UiTheme.Gold : UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Pin(band.rectTransform, new Vector2(1f, 0.5f), -14f, 0f, 60f, 30f);
                if (!seen.Contains(r.id))
                {
                    var dot = UiKit.Image(b.transform, "Unread", UiTheme.Bad, UiKit.Circle);
                    UiKit.Place(dot.rectTransform, 48f, 12f, 10f, 10f);
                }
            }
            RefreshBadge();
        }

        private static string Date(string utc) => DateTime.TryParse(utc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime().ToString("dd/MM HH:mm") : "";

        /// <summary>The selected mail's paper in the test screen: criteria, coach, rewrites, recordings.</summary>
        private void OpenFull()
        {
            if (_shown == null) return;
            SetOpen(false);
            if (IeltsTestUI.OpenSaved(_shown) == null)
                HudFeed.Post(Loc.T("Không tìm thấy đề của bài này trên máy.", "This paper's test is not on this machine.", "この答案の問題がこのPCにありません。"), HudFeed.Kind.Warning, 3f);
        }

        private void Show(IeltsProductiveRecord record)
        {
            if (record == null) return;
            _shown = record;
            if (_openFull != null) _openFull.gameObject.SetActive(true);
            UiKit.Clear(_detail);
            bool live = record.mode == "live";

            var head = UiKit.Rect(_detail, "Head");
            UiKit.Size(head, -1f, 132f);
            var ring = UiKit.ProgressRing(head, "BandRing", 116f, UiTheme.Gold, new Color(1f, 1f, 1f, 0.08f));
            UiKit.Place((RectTransform)ring.transform.parent, 0f, 8f, 116f, 116f);
            ring.fillAmount = Mathf.Clamp01(record.overall / 9f);
            var band = UiKit.Label(ring.transform.parent, "Band", record.overall > 0f ? record.overall.ToString("0.0") : "—", 34f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(band.rectTransform, 0f, 0f, 0f, 12f);
            var cap = UiKit.Label(ring.transform.parent, "Cap", "Band", "Band", "バンド", 11.5f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(cap.rectTransform, 0f, 70f, 0f, 0f);
            var t = UiKit.Label(head, "Test", "<noparse>" + (record.testTitle ?? "") + "</noparse>", 16f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(t.rectTransform, 132f, 10f, 278f, 48f);
            string who = live ? Loc.F("Giám khảo: {0}", "Examiner: {0}", "試験官：{0}", (record.speakingExamination?.model ?? "").Replace("examiner:", ""))
                              : Loc.T("Chấm bởi AI Examiner (ước tính)", "Marked by the AI Examiner (estimate)", "AI試験官による採点（目安）");
            var by = UiKit.Chip(head, "By", who, UiKit.Alpha(live ? UiTheme.Live : UiTheme.Ielts, 0.18f), live ? UiTheme.Live : UiTheme.Ielts, live ? "co_present" : "auto_awesome", 12f);
            UiKit.Place(by, 132f, 66f, 260f, 26f);
            var date = UiKit.Label(head, "Date", Date(record.finishedUtc), 12.5f, UiTheme.Muted);
            UiKit.Place(date.rectTransform, 132f, 100f, 260f, 20f);

            if (record.skill == "speaking" && record.speakingExamination != null && record.speakingExamination.Ok)
                foreach (var c in record.speakingExamination.criteria) Criterion(c.code, c.band, c.assessed);
            else if (record.skill == "writing")
                foreach (var task in record.writing)
                {
                    var line = UiKit.Label(_detail, "Task" + task.task, (Func<string>)(() => Loc.F("Task {0} · {1} từ · Band {2:0.0}", "Task {0} · {1} words · Band {2:0.0}", "タスク{0} · {1}語 · バンド{2:0.0}", task.task, task.words, task.examination?.overall ?? 0f)), 15f, UiTheme.Text, FontStyles.Bold);
                    UiKit.Size(line, -1f, 26f);
                }

            string summary = record.skill == "writing"
                ? string.Join("\n\n", record.writing.Select(task => task.examination?.summary ?? task.examination?.error ?? Loc.T("Chưa có phản hồi", "No feedback yet", "まだフィードバックがありません")))
                : record.speakingExamination?.summary ?? record.speakingExamination?.error ?? Loc.T("Chưa có phản hồi", "No feedback yet", "まだフィードバックがありません");
            var fbHead = UiKit.Label(_detail, "FeedbackHead", "Nhận xét", "Feedback", "コメント", 13f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Size(fbHead, -1f, 22f);
            var text = UiUserContent.Mark(UiKit.Label(_detail, "Detail", "<noparse>" + summary + "</noparse>", 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true));
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            AddSeen(record.id);
            RefreshBadge();
            HudNotificationTray.Instance?.Clear("exam-mail-" + record.id);
            // Selected row highlight + unread dot.
            Refresh();
        }

        private void Criterion(string code, float band, bool assessed)
        {
            var row = UiKit.Rect(_detail, "Crit_" + code);
            UiKit.Size(row, -1f, 30f);
            string vi = code == "FC" ? "Trôi chảy & mạch lạc" : code == "LR" ? "Vốn từ" : code == "GRA" ? "Ngữ pháp" : "Phát âm";
            string en = code == "FC" ? "Fluency & coherence" : code == "LR" ? "Lexical resource" : code == "GRA" ? "Grammar" : "Pronunciation";
            string ja = code == "FC" ? "流暢さと一貫性" : code == "LR" ? "語彙" : code == "GRA" ? "文法" : "発音";
            var label = UiKit.Label(row, "Label", vi, en, ja, 13.5f, UiTheme.TextSoft);
            UiKit.Place(label.rectTransform, 0f, 2f, 170f, 26f);
            var fill = UiKit.ProgressBar(row, "Bar", UiTheme.Gold, new Color(1f, 1f, 1f, 0.07f));
            UiKit.Place((RectTransform)fill.parent, 176f, 11f, 190f, 9f);
            UiKit.SetProgress(fill, assessed ? band / 9f : 0f);
            var value = UiKit.Label(row, "Value", assessed ? band.ToString("0.0") : "—", 14f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(value.rectTransform, 372f, 2f, 40f, 26f);
        }

        private static HashSet<string> Seen() => new HashSet<string>(PlayerPrefs.GetString(SeenKey, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        private static void AddSeen(string id) { var seen = Seen(); if (seen.Add(id)) { PlayerPrefs.SetString(SeenKey, string.Join("|", seen)); PlayerPrefs.Save(); } }

        private void RefreshBadge()
        {
            if (_badge == null) return;
            var seen = Seen();
            int unread = IeltsProductiveStore.All(null).Count(r => !seen.Contains(r.id));
            _badge.transform.parent.gameObject.SetActive(unread > 0);
            _badge.text = unread > 99 ? "99+" : unread.ToString();
        }
    }

    public sealed class DraggableHudIcon : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
    {
        private RectTransform _rect;
        private Canvas _canvas;
        private string _key;

        public void Initialize(string key)
        {
            _key = key; _rect = (RectTransform)transform; _canvas = GetComponentInParent<Canvas>();
            if (PlayerPrefs.HasKey(_key + ".x"))
                _rect.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(_key + ".x"), PlayerPrefs.GetFloat(_key + ".y"));
        }

        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            _rect.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scale);
            float half = 31f;
            _rect.position = new Vector3(Mathf.Clamp(_rect.position.x, half, Screen.width - half), Mathf.Clamp(_rect.position.y, half, Screen.height - half), _rect.position.z);
        }
        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            PlayerPrefs.SetFloat(_key + ".x", _rect.anchoredPosition.x);
            PlayerPrefs.SetFloat(_key + ".y", _rect.anchoredPosition.y);
            PlayerPrefs.Save();
        }
    }
}
