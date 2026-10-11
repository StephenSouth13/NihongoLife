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
            _open = open;
            _window.SetActive(open);
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
            var toggle = NLUi.Button(canvas.transform, "MailboxToggle", "✉  Bài chấm", _font, () => SetOpen(!_open),
                new Color(0.12f, 0.17f, 0.23f, 0.96f), 17f, minHeight: 48f);
            NLUi.Anchor((RectTransform)toggle.transform, new Vector2(1f, 1f), new Vector2(-120f, -84f), new Vector2(180f, 48f));
            _badge = toggle.GetComponentInChildren<TMP_Text>();

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
            _detail = NLUi.Label(rect, "Detail", "Chọn một thư để xem phản hồi.", 16f, NLUi.Text, _font);
            NLUi.Anchor(_detail.rectTransform, new Vector2(0f, 1f), new Vector2(290f, -112f), new Vector2(280f, 500f)); _detail.rectTransform.pivot = new Vector2(0f, 1f);
            _detail.overflowMode = TextOverflowModes.Ellipsis;
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

        private void Show(IeltsProductiveRecord record)
        {
            if (record == null) return;
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
            _badge.text = unread > 0 ? $"✉  Bài chấm  <color=#F2B83F>{unread}</color>" : "✉  Bài chấm";
        }
    }
}
