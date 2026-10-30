using UnityEngine;
using NihongoLife.Learning;

namespace NihongoLife.UI
{
    /// <summary>Scaffold flashcard screen (IMGUI, no prefab needed): card front, Flip, then Again/Hard/Good/Easy.
    /// Opened from the study desk's "Flashcards" option via Open(session). Buttons only (project uses the Input System; keys hook into GameInputService later).</summary>
    public sealed class FlashcardScreen : MonoBehaviour
    {
        private static readonly string[] Grades = { "fc.again", "fc.hard", "fc.good", "fc.easy" };
        private FlashcardSession _session;
        public bool IsOpen => _session != null;
        public FlashcardSession Session => _session;

        public void Open(FlashcardSession session) { _session = session; }
        public event System.Action Closed;
        public void Close() { _session = null; Closed?.Invoke(); }

        private void OnGUI()
        {
            if (_session == null) return;
            float w = Mathf.Min(720f, Screen.width * 0.9f), h = 360f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, EduUiText.Get("desk.flashcards") + "  -  " + _session.Remaining + " " + EduUiText.Get("fc.left") + "  -  " + _session.Reviewed + " " + EduUiText.Get("fc.reviewed"));
            var big = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            if (_session.Done)
            {
                GUI.Label(new Rect(r.x, r.y + 60, w, 120), EduUiText.Get("fc.done"), big);
                if (GUI.Button(new Rect(r.x + w * 0.5f - 80, r.yMax - 60, 160, 40), EduUiText.Get("ui.close"))) Close();
                return;
            }
            GUI.Label(new Rect(r.x, r.y + 40, w, 110), _session.Front, big);
            if (!_session.Flipped)
            {
                if (GUI.Button(new Rect(r.x + w * 0.5f - 80, r.yMax - 60, 160, 40), EduUiText.Get("fc.show"))) _session.Flip();
                return;
            }
            var mid = new GUIStyle(big) { fontSize = 24 };
            GUI.Label(new Rect(r.x, r.y + 160, w, 90), _session.Back, mid);
            float bw = (w - 50) / 4f;
            for (int g = 0; g < 4; g++)
                if (GUI.Button(new Rect(r.x + 10 + g * (bw + 10), r.yMax - 60, bw, 40), EduUiText.Get(Grades[g]))) _session.Grade(g);
        }
    }
}
