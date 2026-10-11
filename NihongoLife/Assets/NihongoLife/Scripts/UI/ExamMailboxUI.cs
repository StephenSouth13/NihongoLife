using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Exam.Ielts;

namespace NihongoLife.UI
{
    /// <summary>Compact persistent inbox for completed Writing/Speaking assessments.</summary>
    public sealed class ExamMailboxUI : MonoBehaviour
    {
        private const string SeenKey = "NL.ExamMailbox.Seen";
        private static ExamMailboxUI _instance;
        private GameObject _window;
        private RectTransform _list;
        private TMP_Text _detail;
        private TMP_Text _badge;
        private TMP_FontAsset _font;
        private bool _open;
        private NihongoLife.Player.PlayerController _player;
        private NihongoLife.Cameras.ThirdPersonCameraController _camera;
        private bool _oldPlayerLock, _oldCameraLock, _oldCursorVisible;
        private CursorLockMode _oldCursorLock;

        private void Awake()
        {
            _instance = this;
            Build();
            UiModalStack.Register(this, () => _open, () => SetOpen(false), "Exam mailbox");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame && !UiModalStack.BlocksHotkeys)
                SetOpen(!_open);
        }

        public static void NotifyGraded(IeltsProductiveRecord record)
        {
            if (record == null) return;
            _instance?.Refresh();
            string skill = record.skill == "writing" ? "Writing" : "Speaking";
            HudNotificationTray.Instance?.Post("exam-mail-" + record.id, "Có thư chấm bài mới",
                $"{skill} · {(record.overall > 0f ? "Band " + record.overall.ToString("0.0") : "Cần xem phản hồi")}",
                () => _instance?.OpenRecord(record.id), HudNoticeTone.Warning, true);
        }

        public void SetOpen(bool open)
        {
            if (_open == open) return;
            if (open)
            {
                _player = FindFirstObjectByType<NihongoLife.Player.PlayerController>();
                _camera = FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
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

        private void Build()
        {
            _font = NLUi.ResolveFont();
            var canvas = NLUi.CreateCanvas("ExamMailboxCanvas", 175, transform);
            var toggleObject = new GameObject("NotificationBell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(DraggableHudIcon));
            toggleObject.transform.SetParent(canvas.transform, false);
            var toggleRect = (RectTransform)toggleObject.transform;
            NLUi.Anchor(toggleRect, new Vector2(1f, 1f), new Vector2(-74f, -84f), new Vector2(62f, 62f));
            var toggleImage = toggleObject.GetComponent<Image>();
            toggleImage.sprite = UIStyleKit.RoundedSprite(); toggleImage.type = Image.Type.Sliced;
            var toggle = toggleObject.GetComponent<Button>(); toggle.targetGraphic = toggleImage;
            UIStyleKit.StyleButton(toggle, new Color(0.08f, 0.13f, 0.19f, 0.97f), new Color(0.16f, 0.24f, 0.32f, 1f), new Color(0.95f, 0.72f, 0.25f, 1f));
            toggle.onClick.AddListener(() => SetOpen(!_open));
            var iconObject = new GameObject("Bell", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(toggleRect, false);
            var iconRect = (RectTransform)iconObject.transform; NLUi.Anchor(iconRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            iconObject.GetComponent<Image>().sprite = HudIcons.Bell; iconObject.GetComponent<Image>().color = NLUi.Gold; iconObject.GetComponent<Image>().raycastTarget = false;
            var badgeRoot = NLUi.Panel(toggleRect, "UnreadBadge", new Color(0.88f, 0.22f, 0.18f, 1f));
            NLUi.Anchor(badgeRoot, new Vector2(1f, 1f), new Vector2(3f, 3f), new Vector2(25f, 25f));
            badgeRoot.pivot = new Vector2(1f, 1f);
            _badge = NLUi.Label(badgeRoot, "Count", "", 13f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(_badge.rectTransform);
            badgeRoot.SetAsLastSibling();
            toggleObject.GetComponent<DraggableHudIcon>().Initialize("NL.ExamMailbox.IconPosition");

            _window = new GameObject("MailboxWindow", typeof(RectTransform), typeof(Image));
            _window.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)_window.transform;
            NLUi.Anchor(rect, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(600f, 650f));
            rect.pivot = new Vector2(1f, 0.5f);
            _window.GetComponent<Image>().color = NLUi.Ink;
            UIStyleKit.StylePanel(rect, NLUi.Ink);
            NLUi.CloseButton(rect, _font, () => SetOpen(false));

            var title = NLUi.Label(rect, "Title", "HỘP THƯ KẾT QUẢ", 25f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(500f, 38f)); title.rectTransform.pivot = new Vector2(0f, 1f);
            var hint = NLUi.Label(rect, "Hint", "Bài Writing/Speaking đã chấm · phím I để đóng/mở", 14f, NLUi.Muted, _font);
            NLUi.Anchor(hint.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -68f), new Vector2(520f, 28f)); hint.rectTransform.pivot = new Vector2(0f, 1f);

            _list = NLUi.Group(rect, "Messages", true, 8f);
            NLUi.Anchor(_list, new Vector2(0f, 1f), new Vector2(28f, -112f), new Vector2(240f, 500f)); _list.pivot = new Vector2(0f, 1f);
            // Detail: scrolls (long examiner feedback used to be cut with "…"), with a button to the full results.
            var detailScroll = new GameObject("DetailScroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D), typeof(Image));
            detailScroll.transform.SetParent(rect, false);
            var detailRect = (RectTransform)detailScroll.transform;
            NLUi.Anchor(detailRect, new Vector2(0f, 1f), new Vector2(290f, -112f), new Vector2(282f, 452f)); detailRect.pivot = new Vector2(0f, 1f);
            detailScroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            _detail = NLUi.Label(detailRect, "Detail", "Chọn một thư để xem phản hồi.", 16f, NLUi.Text, _font);
            var detailText = _detail.rectTransform;
            detailText.anchorMin = new Vector2(0f, 1f); detailText.anchorMax = new Vector2(1f, 1f); detailText.pivot = new Vector2(0.5f, 1f);
            detailText.offsetMin = new Vector2(8f, 0f); detailText.offsetMax = new Vector2(-8f, 0f);
            _detail.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = detailScroll.GetComponent<ScrollRect>();
            scroll.content = detailText; scroll.viewport = detailRect; scroll.horizontal = false; scroll.scrollSensitivity = 30f;
            _openFull = NLUi.Button(rect, "OpenFullResult", "Xem bài đầy đủ", _font, () => OpenFull(), NLUi.Gold, 16f, new Color(0.08f, 0.07f, 0.04f), 46f);
            var openRect = (RectTransform)_openFull.transform;
            NLUi.Anchor(openRect, new Vector2(0f, 1f), new Vector2(290f, -576f), new Vector2(282f, 46f)); openRect.pivot = new Vector2(0f, 1f);
            _openFull.gameObject.SetActive(false);
            _window.SetActive(false);
            RefreshBadge();
        }

        private void Refresh()
        {
            foreach (Transform child in _list) Destroy(child.gameObject);
            var records = IeltsProductiveStore.All(null).OrderByDescending(r => r.finishedUtc).Take(7).ToList();
            if (records.Count == 0) NLUi.Label(_list, "Empty", "Chưa có bài đã chấm.", 15f, NLUi.Muted, _font);
            foreach (var record in records)
            {
                string skill = record.skill == "writing" ? "Writing" : "Speaking";
                string band = record.overall > 0f ? $"Band {record.overall:0.0}" : "Xem phản hồi";
                NLUi.Button(_list, "Mail_" + record.id, $"{skill}\n<size=14><color=#A8B8CC>{band}</color></size>", _font,
                    () => Show(record), NLUi.Card, 17f, minHeight: 64f);
            }
            RefreshBadge();
        }

        private Button _openFull;
        private IeltsProductiveRecord _shown;

        /// <summary>The selected mail's paper in the test screen: criteria, coach, rewrites, recordings.</summary>
        private void OpenFull()
        {
            if (_shown == null) return;
            SetOpen(false);
            if (IeltsTestUI.OpenSaved(_shown) == null)
                HudFeed.Post("Không tìm thấy đề của bài này trên máy.", HudFeed.Kind.Warning, 3f);
        }

        private void Show(IeltsProductiveRecord record)
        {
            if (record == null) return;
            _shown = record;
            if (_openFull != null) _openFull.gameObject.SetActive(true);
            string summary = record.skill == "writing"
                ? string.Join("\n\n", record.writing.Select(t => $"Task {t.task} · {t.words} từ · Band {(t.examination?.overall ?? 0f):0.0}\n{t.examination?.summary ?? t.examination?.error ?? "Chưa có phản hồi"}"))
                : record.speakingExamination?.summary ?? record.speakingExamination?.error ?? "Chưa có phản hồi";
            _detail.text = $"<color=#F2B83F><b>{record.testTitle}</b></color>\n\nBand tổng: <b>{record.overall:0.0}</b>\n\n{summary}";
            AddSeen(record.id); RefreshBadge();
            HudNotificationTray.Instance?.Clear("exam-mail-" + record.id);
        }

        private static HashSet<string> Seen() => new HashSet<string>((PlayerPrefs.GetString(SeenKey, "")).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        private static void AddSeen(string id) { var seen = Seen(); if (seen.Add(id)) { PlayerPrefs.SetString(SeenKey, string.Join("|", seen)); PlayerPrefs.Save(); } }
        private void RefreshBadge()
        {
            if (_badge == null) return;
            int unread = IeltsProductiveStore.All(null).Count(r => !Seen().Contains(r.id));
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
