using UnityEngine;
using NihongoLife.Learning;

namespace NihongoLife.UI
{
    /// <summary>Album pages from StudyProgressStore (IMGUI scaffold): place, Japanese caption, local caption, day; paged 4 at a time.</summary>
    public sealed class AlbumScreen : MonoBehaviour
    {
        private int _page;
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> _thumbs = new System.Collections.Generic.Dictionary<string, Texture2D>();

        private Texture2D Thumb(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_thumbs.TryGetValue(path, out var t)) return t;
            t = null;
            if (System.IO.File.Exists(path)) { t = new Texture2D(2, 2); if (!t.LoadImage(System.IO.File.ReadAllBytes(path))) { Destroy(t); t = null; } }
            _thumbs[path] = t;
            return t;
        }

        private void OnDestroy() { foreach (var t in _thumbs.Values) if (t != null) Destroy(t); }
        public bool IsOpen { get; private set; }
        public void Open() { IsOpen = true; _page = 0; }
        public void Close() => IsOpen = false;

        private void OnGUI()
        {
            if (!IsOpen) return;
            var pages = StudyProgressStore.Current.album.pages;
            float w = 640f, h = 420f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, EduUiText.Get("desk.album") + "  (" + pages.Count + ")");
            if (pages.Count == 0) GUI.Label(new Rect(r.x + 20, r.y + 60, w - 40, 40), EduUiText.Get("album.empty"));
            for (int i = 0; i < 4; i++)
            {
                int k = _page * 4 + i;
                if (k >= pages.Count) break;
                var p = pages[k];
                GUI.Box(new Rect(r.x + 20, r.y + 40 + i * 82, w - 40, 74), "");
                var tex = Thumb(p.image);
                float tx = 0f;
                if (tex != null) { GUI.DrawTexture(new Rect(r.x + 24, r.y + 44 + i * 82, 118, 66), tex, ScaleMode.ScaleAndCrop); tx = 124f; }
                GUI.Label(new Rect(r.x + 30 + tx, r.y + 46 + i * 82, w - 60 - tx, 66), p.place + "  -  " + EduUiText.Get("album.day") + " " + p.day + "\n" + p.captionJa + "\n" + p.captionLocal);
            }
            if (_page > 0 && GUI.Button(new Rect(r.x + 20, r.yMax - 36, 80, 28), "<")) _page--;
            if ((_page + 1) * 4 < pages.Count && GUI.Button(new Rect(r.x + 110, r.yMax - 36, 80, 28), ">")) _page++;
            if (GUI.Button(new Rect(r.xMax - 90, r.yMax - 36, 80, 28), EduUiText.Get("ui.close"))) Close();
        }
    }
}
