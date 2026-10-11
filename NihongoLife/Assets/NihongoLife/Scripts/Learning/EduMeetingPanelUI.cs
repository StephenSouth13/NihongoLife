using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.UI;

namespace NihongoLife.Learning
{
    /// <summary>Polished Canvas/TMP presentation for the F8 Speaking lounge.</summary>
    public sealed class EduMeetingPanelUI : MonoBehaviour
    {
        private EduMeetingManager _manager;
        private GameObject _root;
        private RectTransform _content;
        private TMP_FontAsset _font;
        private float _nextRefresh;
        private string _signature;

        private void Awake()
        {
            _manager = GetComponent<EduMeetingManager>();
            BuildShell();
        }

        private void Update()
        {
            if (_root == null || _manager == null) return;
            if (_root.activeSelf != _manager.IsOpen) _root.SetActive(_manager.IsOpen);
            if (!_manager.IsOpen || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.35f;
            string signature = string.Join("|", _manager.Status, _manager.IsTeacherAccount, _manager.IsBusy,
                _manager.IsInClassroom, _manager.IsMuted, _manager.HasWaitingRoom,
                _manager.SelectedRoom?.id, string.Join(",", _manager.WaitingRooms.Select(r => r.id)));
            if (signature != _signature) { _signature = signature; Rebuild(); }
        }

        private void BuildShell()
        {
            _font = NLUi.ResolveFont();
            var canvas = NLUi.CreateCanvas("SpeakingLoungeCanvas", 180, transform);
            _root = new GameObject("SpeakingLounge", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(canvas.transform, false);
            NLUi.Stretch((RectTransform)_root.transform);
            _root.GetComponent<Image>().color = new Color(0.015f, 0.022f, 0.035f, 0.72f);

            var card = NLUi.Panel(_root.transform, "LoungeCard", new Color(0.035f, 0.055f, 0.075f, 0.99f));
            NLUi.Anchor(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 720f));
            UIStyleKit.StylePanel(card, new Color(0.035f, 0.055f, 0.075f, 0.99f));
            NLUi.CloseButton(card, _font, () => _manager.SetOpen(false), 48f, 18f);

            var eyebrow = NLUi.Label(card, "Eyebrow", "NIHONGO LIFE  ·  LIVE LEARNING", 14f, NLUi.Gold, _font, FontStyles.Bold);
            Place(eyebrow.rectTransform, 42f, 34f, 760f, 24f);
            var title = NLUi.Label(card, "Title", "Speaking Lounge", 34f, NLUi.Text, _font, FontStyles.Bold);
            Place(title.rectTransform, 42f, 62f, 760f, 48f);
            var subtitle = NLUi.Label(card, "Subtitle", "Phòng học trực tiếp 1–1 · camera và micro bảo mật bởi Agora", 16f, NLUi.Muted, _font);
            Place(subtitle.rectTransform, 42f, 111f, 850f, 28f);

            _content = new GameObject("DynamicContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(card, false); Place(_content, 36f, 158f, 1048f, 516f);
            _root.SetActive(false);
        }

        private void Rebuild()
        {
            foreach (Transform child in _content) Destroy(child.gameObject);
            var status = NLUi.Panel(_content, "Status", new Color(0.07f, 0.11f, 0.15f, 1f));
            Place(status, 0f, 0f, 1048f, 62f);
            var dot = NLUi.Pill(status, "State", _manager.IsInClassroom ? "●  ĐANG TRONG LỚP" : _manager.IsBusy ? "●  ĐANG KẾT NỐI" : "●  SẴN SÀNG",
                _font, _manager.IsInClassroom ? new Color(0.10f, 0.34f, 0.24f) : new Color(0.16f, 0.21f, 0.27f),
                _manager.IsInClassroom ? NLUi.Good : NLUi.Soft, 14f);
            Place(dot, 16f, 13f, 190f, 36f);
            var statusText = NLUi.Label(status, "Message", _manager.Status, 15f, NLUi.Muted, _font);
            Place(statusText.rectTransform, 225f, 13f, 790f, 38f);

            if (!_manager.OnlineClassroomAvailable) BuildOffline();
            else if (_manager.IsTeacherAccount) BuildTeacher();
            else BuildStudent();

            if (_manager.IsInClassroom)
            {
                ButtonAt(_content, "Mic", _manager.IsMuted ? "BẬT MICRO" : "TẮT MICRO", 610f, 450f, 190f, 52f,
                    () => _manager.ToggleMicrophone(), new Color(0.12f, 0.19f, 0.25f));
                ButtonAt(_content, "Leave", "RỜI CUỘC GỌI", 816f, 450f, 216f, 52f,
                    () => _manager.LeaveClassroom(), new Color(0.55f, 0.13f, 0.12f));
            }
        }

        private void BuildOffline()
        {
            var box = NLUi.Panel(_content, "LoginRequired", new Color(0.07f, 0.10f, 0.14f, 1f)); Place(box, 0f, 82f, 1048f, 350f);
            var icon = NLUi.Label(box, "Icon", "LIVE", 46f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Center); Place(icon.rectTransform, 440f, 42f, 168f, 100f);
            var title = NLUi.Label(box, "Title", "Đăng nhập để vào Speaking Lounge", 26f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center); Place(title.rectTransform, 170f, 155f, 708f, 42f);
            var text = NLUi.Label(box, "Text", "Học viên tạo phòng chờ; giáo viên xem hồ sơ rồi nhận lớp.\nToken Agora được cấp tự động, không cần nhập mã phòng thủ công.", 17f, NLUi.Muted, _font, alignment: TextAlignmentOptions.Center); Place(text.rectTransform, 170f, 210f, 708f, 80f);
        }

        private void BuildStudent()
        {
            var hero = NLUi.Panel(_content, "StudentHero", new Color(0.07f, 0.12f, 0.17f, 1f)); Place(hero, 0f, 82f, 1048f, 350f);
            var title = NLUi.Label(hero, "Title", _manager.HasWaitingRoom ? "Phòng chờ của bạn đã sẵn sàng" : "Luyện nói trực tiếp cùng giáo viên", 29f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center); Place(title.rectTransform, 120f, 58f, 808f, 48f);
            var text = NLUi.Label(hero, "Text", _manager.HasWaitingRoom ? "Hệ thống đang tìm giáo viên phù hợp. Bạn có thể tiếp tục ở trong game; phòng sẽ tự kết nối khi giáo viên nhận lớp." : "Tạo phòng riêng chỉ với một lần nhấn. Giáo viên sẽ thấy hồ sơ học tập của bạn trước khi nhận lớp.", 17f, NLUi.Muted, _font, alignment: TextAlignmentOptions.Center); Place(text.rectTransform, 160f, 128f, 728f, 72f);
            if (_manager.HasWaitingRoom)
                ButtonAt(hero, "Cancel", "HỦY PHÒNG CHỜ", 374f, 235f, 300f, 58f, () => _manager.CloseWaitingRoom(), new Color(0.34f, 0.13f, 0.13f));
            else
                ButtonAt(hero, "Create", "TẠO PHÒNG CHỜ SPEAKING", 324f, 235f, 400f, 58f, () => _manager.CreateWaitingRoom(), NLUi.Gold, new Color(0.08f, 0.08f, 0.09f));
        }

        private void BuildTeacher()
        {
            var list = NLUi.Panel(_content, "WaitingList", new Color(0.055f, 0.09f, 0.125f, 1f)); Place(list, 0f, 82f, 500f, 350f);
            var head = NLUi.Label(list, "Head", "HỌC VIÊN ĐANG CHỜ", 18f, NLUi.Gold, _font, FontStyles.Bold); Place(head.rectTransform, 20f, 18f, 350f, 30f);
            ButtonAt(list, "Refresh", "↻", 425f, 13f, 52f, 40f, () => _manager.RefreshClassroomLobby(), new Color(0.12f, 0.18f, 0.24f));
            int index = 0;
            foreach (var room in _manager.WaitingRooms.Take(4))
            {
                var captured = room;
                string label = $"{Initials(room.student_name)}     {room.student_name}\n<size=13><color=#9FB2C5>Level {Mathf.Max(1, room.level)}  ·  {Mathf.Max(0, room.xp)} XP</color></size>";
                ButtonAt(list, "Student_" + room.id, label, 20f, 68f + index * 66f, 457f, 56f, () => _manager.SelectWaitingRoom(captured),
                    _manager.SelectedRoom?.id == room.id ? new Color(0.25f, 0.19f, 0.06f) : new Color(0.09f, 0.14f, 0.19f));
                index++;
            }
            if (index == 0)
            {
                var empty = NLUi.Label(list, "Empty", "Chưa có học viên nào đang chờ.\nDanh sách tự làm mới sau vài giây.", 16f, NLUi.Muted, _font, alignment: TextAlignmentOptions.Center); Place(empty.rectTransform, 55f, 130f, 390f, 80f);
            }

            var profile = NLUi.Panel(_content, "Profile", new Color(0.075f, 0.105f, 0.135f, 1f)); Place(profile, 520f, 82f, 528f, 350f);
            var selected = _manager.SelectedRoom;
            if (selected == null)
            {
                var select = NLUi.Label(profile, "Select", "Chọn một học viên\nđể xem hồ sơ trước khi nhận lớp", 20f, NLUi.Muted, _font, alignment: TextAlignmentOptions.Center); Place(select.rectTransform, 60f, 115f, 408f, 90f);
                return;
            }
            var avatar = NLUi.Panel(profile, "Avatar", new Color(0.18f, 0.25f, 0.31f, 1f)); Place(avatar, 24f, 24f, 88f, 88f);
            var initials = NLUi.Label(avatar, "Initials", Initials(selected.student_name), 30f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Center); NLUi.Stretch(initials.rectTransform);
            var name = NLUi.Label(profile, "Name", selected.student_name, 25f, NLUi.Text, _font, FontStyles.Bold); Place(name.rectTransform, 132f, 28f, 360f, 38f);
            var stats = NLUi.Label(profile, "Stats", $"Level {Mathf.Max(1, selected.level)}  ·  {Mathf.Max(0, selected.xp)} XP", 15f, NLUi.Good, _font); Place(stats.rectTransform, 132f, 70f, 340f, 28f);
            var bio = NLUi.Label(profile, "Bio", string.IsNullOrWhiteSpace(selected.bio) ? "Học viên chưa viết phần giới thiệu." : selected.bio, 16f, NLUi.Muted, _font); Place(bio.rectTransform, 24f, 132f, 480f, 90f);
            var id = NLUi.Label(profile, "Id", "ID  " + selected.student_id, 12f, new Color(0.46f, 0.56f, 0.66f), _font); Place(id.rectTransform, 24f, 224f, 480f, 24f);
            ButtonAt(profile, "Accept", "XÁC NHẬN NHẬN LỚP", 24f, 270f, 480f, 58f, () => _manager.ClaimWaitingRoom(selected.id), NLUi.Gold, new Color(0.07f, 0.08f, 0.09f));
        }

        private Button ButtonAt(Transform parent, string name, string label, float x, float y, float width, float height, Action action, Color color, Color? text = null)
        {
            var button = NLUi.Button(parent, name, label, _font, action, color, 16f, text, height);
            Place((RectTransform)button.transform, x, y, width, height);
            return button;
        }

        private static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "HV";
            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(words.Take(2).Select(w => char.ToUpperInvariant(w[0])));
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
