using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NihongoLife.Home;
using NihongoLife.Learning;

namespace NihongoLife.UI
{
    /// <summary>The study desk's multi-menu (IMGUI): Word quiz, Flashcards, Album, Exam prep, Wardrobe.
    /// Opened by StudyDeskInteractable.Interact; one instance lives on the desk object.</summary>
    public sealed class StudyDeskMenuScreen : MonoBehaviour
    {
        private StudyDeskInteractable _desk;
        private GameObject _player;
        private List<StudyDeskMenu.Entry> _entries;
        public bool IsOpen { get; private set; }

        public static StudyDeskMenuScreen Open(StudyDeskInteractable desk, GameObject player)
        {
            var s = Comp<StudyDeskMenuScreen>(desk.gameObject);
            s._desk = desk; s._player = player; s.Refresh(); s.IsOpen = true;
            return s;
        }

        public void Close() => IsOpen = false;

        public void Refresh()
        {
            var d = StudyProgressStore.Current;
            _entries = StudyDeskMenu.Build(d.wordsSeen, DueCount(), d.album.Count, d.wardrobe.owned.Count > 1);
        }

        public static int DueCount()
        {
            StudyProgressStore.EnsureCards(StudyDeskInteractable.Words.Select(w => w.japanese), StudyProgressStore.Today);
            return FlashcardScheduler.Due(StudyProgressStore.Current.cards, StudyProgressStore.Today, 999).Count;
        }

        /// <summary>Runs an option (the buttons call this; tests call it directly).</summary>
        public void Choose(StudyDeskMenu.Option option)
        {
            var e = _entries.Find(x => x.option == option);
            if (!e.unlocked) { Toast(e.lockReason); return; }
            Close();
            switch (option)
            {
                case StudyDeskMenu.Option.WordQuiz: _desk.StartQuiz(_player); break;
                case StudyDeskMenu.Option.Flashcards: OpenFlashcards(); break;
                case StudyDeskMenu.Option.Album: (Comp<AlbumScreen>(gameObject)).Open(); break;
                case StudyDeskMenu.Option.Wardrobe: (Comp<WardrobeScreen>(gameObject)).Open(_player); break;
                case StudyDeskMenu.Option.ExamPrep: Toast(EduUiText.Get("desk.examToast")); break;
            }
        }

        public FlashcardSession OpenFlashcards()
        {
            DueCount();
            var faces = StudyDeskInteractable.Words.ToDictionary(w => w.japanese, w => new FlashcardSession.Face(w.japanese, w.reading + "\n" + w.Meaning));
            var session = new FlashcardSession(StudyProgressStore.Current.cards, faces, StudyProgressStore.Today);
            var screen = Comp<FlashcardScreen>(gameObject);
            screen.Open(session);
            screen.Closed -= StudyProgressStore.Save; screen.Closed += StudyProgressStore.Save;
            return session;
        }

        private static void Toast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (HomeBedroomRuntime.Instance != null) HomeBedroomRuntime.Instance.ShowToast("\u307E\u3060", msg); else Debug.Log("[StudyDesk] " + msg);
        }

        private void OnGUI()
        {
            if (!IsOpen || _entries == null) return;
            float w = 360f, h = 70f + _entries.Count * 48f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, EduUiText.Get("desk.title"));
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                string label = Label(e.option) + (e.option == StudyDeskMenu.Option.Flashcards ? "  " + StudyDeskMenu.DueBadge(DueCount()) : "") + (e.unlocked ? "" : "  " + EduUiText.Get("desk.locked"));
                GUI.enabled = e.unlocked;
                if (GUI.Button(new Rect(r.x + 20, r.y + 30 + i * 48, w - 40, 40), label)) Choose(e.option);
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(r.xMax - 90, r.yMax - 34, 80, 28), EduUiText.Get("ui.close"))) Close();
        }

        private static string Label(StudyDeskMenu.Option o) => o switch
        {
            StudyDeskMenu.Option.WordQuiz => EduUiText.Get("desk.quiz"),
            StudyDeskMenu.Option.Flashcards => EduUiText.Get("desk.flashcards"),
            StudyDeskMenu.Option.Album => EduUiText.Get("desk.album"),
            StudyDeskMenu.Option.ExamPrep => EduUiText.Get("desk.exam"),
            _ => EduUiText.Get("desk.wardrobe"),
        };

        private static T Comp<T>(GameObject g) where T : Component { return g.TryGetComponent<T>(out var c) ? c : g.AddComponent<T>(); }
    }
}
