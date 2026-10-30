using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>IMGUI panel: enter the link code from quangdungnihongo.com to link this device to a free Edu account,
    /// or unlink / reset the linked account locally. Opened from the study desk menu ("Tai khoan" button).</summary>
    public sealed class EduAccountLinkScreen : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        private string _code = "";
        private string _status = "";
        private bool _busy;
        private bool _confirmReset;

        public void Open() { IsOpen = true; _status = ""; _confirmReset = false; }
        public void Close() => IsOpen = false;

        private void Redeem()
        {
            if (_busy || string.IsNullOrWhiteSpace(_code)) return;
            _busy = true; _status = "Đang liên kết...";
            StartCoroutine(EduAccountLink.Redeem(_code, (ok, msg) =>
            {
                _busy = false;
                _status = ok ? "Đã liên kết: " + msg : "Không liên kết được. Kiểm tra mã và mạng.";
                if (!ok) Debug.LogWarning("[EduAccountLink] " + msg);
            }));
        }

        private void ResetLinked()
        {
            EduAccountLink.Unlink();
            string path = PlayerSessionService.GuestSavePath;
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            _status = "Đã hủy liên kết và xóa tiến độ trên máy này.";
            _confirmReset = false;
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            float w = 420f, h = 250f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, "Tài khoản Edu (quangdungnihongo.com)");
            float y = r.y + 34;
            GUI.Label(new Rect(r.x + 20, y, w - 40, 22), EduAccountLink.IsLinked ? "Đang liên kết: " + EduAccountLink.GameUser : "Chưa liên kết. Nhập mã liên kết từ trang web:");
            y += 28;
            GUI.enabled = !_busy;
            _code = GUI.TextField(new Rect(r.x + 20, y, w - 150, 30), _code ?? "", 16);
            if (GUI.Button(new Rect(r.xMax - 120, y, 100, 30), "Liên kết")) Redeem();
            y += 42;
            if (!_confirmReset)
            {
                GUI.enabled = !_busy && (EduAccountLink.IsLinked || PlayerSessionService.GuestSavePath != null);
                if (GUI.Button(new Rect(r.x + 20, y, w - 40, 30), "Hủy liên kết / Reset tài khoản trên máy này")) _confirmReset = true;
            }
            else
            {
                GUI.Label(new Rect(r.x + 20, y, w - 40, 22), "Xóa liên kết và tiến độ trên máy này?");
                if (GUI.Button(new Rect(r.x + 20, y + 24, (w - 50) / 2, 30), "Đồng ý")) ResetLinked();
                if (GUI.Button(new Rect(r.x + 30 + (w - 50) / 2, y + 24, (w - 50) / 2, 30), "Thôi")) _confirmReset = false;
            }
            GUI.enabled = true;
            GUI.Label(new Rect(r.x + 20, r.yMax - 66, w - 40, 36), _status);
            if (GUI.Button(new Rect(r.xMax - 90, r.yMax - 34, 80, 28), EduUiText.Get("ui.close"))) Close();
        }
    }
}
