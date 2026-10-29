using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Music;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The music room in the real game: a melody is learnt key by key (a wrong key is counted, the right ones advance to
    /// the end), the guitar strums a chord, a theory lesson's quiz completes the lesson, the gallery shows its paintings;
    /// sounds are made in code. Captures: Bao_Cao/music-studio.
    /// </summary>
    public class MusicStudioPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/music-studio"));

        [UnityTest]
        public IEnumerator MusicRoom_PianoGuitarTheoryGallery()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            Assert.IsNotNull(InstrumentSynth.Clip(InstrumentSynth.Voice.Piano, 60), "a piano note");
            Assert.Greater(InstrumentSynth.Clip(InstrumentSynth.Voice.Guitar, 52).length, 1f, "a guitar string rings");

            // Learn Ode to Joy: one wrong key, then every right one.
            MusicStudioUI.PractiseSong("ode-to-joy");
            yield return null;
            Assert.IsTrue(MusicStudioUI.IsOpen);
            Assert.AreEqual(MusicStudioUI.Tab.Piano, MusicStudioUI.CurrentTab);
            var song = MusicCatalog.Song("ode-to-joy");
            MusicStudioUI.Press(song.notes[0].midi + 1);
            Assert.AreEqual(1, MusicStudioUI.Practice.mistakes, "a wrong key is counted");
            Assert.AreEqual(0, MusicStudioUI.Practice.step);
            yield return null;
            Capture("01_piano_practice");
            foreach (var note in song.notes) MusicStudioUI.Press(note.midi);
            Assert.AreEqual(song.notes.Length, MusicStudioUI.Practice.step, "every note played");
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("02_piano_done");

            MusicStudioUI.Open(MusicStudioUI.Tab.Guitar);
            yield return null;
            MusicStudioUI.Strum(MusicCatalog.Progressions[0].chords[0]);
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("03_guitar");

            var lesson = MusicCatalog.Lessons().First();
            MusicStudioUI.OpenLesson(lesson.id);
            yield return null;
            Capture("04_theory_lesson");
            foreach (var q in lesson.quiz)
            {
                Assert.IsFalse(MusicStudioUI.AnswerCurrent((q.answer + 1) % q.options.Length), "a wrong answer does not advance");
                Assert.IsTrue(MusicStudioUI.AnswerCurrent(q.answer));
            }
            Assert.IsTrue(MusicStudioUI.IsLessonDone(lesson.id), "the lesson is complete");

            MusicStudioUI.Open(MusicStudioUI.Tab.Gallery);
            yield return null;
            Assert.Greater(Object.FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None).Count(r => r.name == "Thumb" && r.texture != null), 20, "the gallery shows the paintings");
            Capture("05_gallery");
            MusicStudioUI.Close();
            Assert.IsFalse(MusicStudioUI.IsOpen);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            try { File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG()); }
            catch (IOException e) { Debug.LogWarning("Capture " + name + " skipped (the file is open elsewhere): " + e.Message); }
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases();
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
