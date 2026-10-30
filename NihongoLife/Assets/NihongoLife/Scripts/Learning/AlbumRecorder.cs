using NihongoLife.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Learning
{
    /// <summary>Fills the study album: a "first visit" page the first time each place is loaded, and word-count milestones from the desk.</summary>
    public static class AlbumRecorder
    {
        public static readonly int[] WordMilestones = { 10, 50, 100, 300, 1000 };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            RecordPlace(SceneManager.GetActiveScene().name);
        }

        private static void OnLoaded(Scene s, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) RecordPlace(s.name); }

        /// <summary>Adds the place's first-visit page; false for menus, bootstrap and test scenes.</summary>
        public static bool RecordPlace(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return false;
            string key = "place." + scene;
            string local = EduUiText.Get(key);
            if (local == key) return false; // not a place
            bool added = StudyProgressStore.AddPhoto("visit_" + scene, local, EduUiText.Ja(key) + " - " + EduUiText.Ja("photo.visit"), EduUiText.Get("photo.visit"));
            if (added) AlbumSnapshotter.Request("visit_" + scene);
            return added;
        }

        /// <summary>Called after words-seen grows from <paramref name="before"/> to <paramref name="after"/>; adds a page per milestone crossed.</summary>
        public static int RecordWords(int before, int after)
        {
            int n = 0;
            foreach (var m in WordMilestones)
                if (before < m && after >= m &&
                    StudyProgressStore.AddPhoto("words_" + m, EduUiText.Get("desk.title"), string.Format(EduUiText.Ja("photo.words"), m), string.Format(EduUiText.Get("photo.words"), m)))
                    n++;
            return n;
        }
    }
}
