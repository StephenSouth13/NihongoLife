using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NihongoLife.Core;
using NihongoLife.Exam;
using NihongoLife.Learning;
using NihongoLife.UI;

namespace NihongoLife.Tests.PlayMode
{
    public class SpeakingWritingUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpeakingLoungeAndProductiveFilters_AreVisible()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0; t < 20f && Object.FindFirstObjectByType<EduMeetingManager>() == null; t += Time.unscaledDeltaTime) yield return null;

            var meeting = Object.FindFirstObjectByType<EduMeetingManager>();
            Assert.NotNull(meeting);
            meeting.SetOpen(true);
            yield return new WaitForSecondsRealtime(0.5f);
            var lounge = GameObject.Find("SpeakingLounge");
            Assert.IsTrue(lounge != null && lounge.activeInHierarchy, "F8 Speaking lounge Canvas is visible");
            Capture("speaking-lounge.png");
            meeting.SetOpen(false);

            var center = Object.FindFirstObjectByType<ExamCenterPopup>();
            Assert.NotNull(center);
            center.Show();
            typeof(ExamCenterPopup).GetMethod("SelectTab", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(center, new object[] { ExamType.Ielts });
            yield return new WaitForSecondsRealtime(0.5f);
            var labels = center.GetComponentsInChildren<TMP_Text>(false).Select(t => t.text).ToList();
            Assert.IsTrue(labels.Any(t => t.Contains("WRITING")), "Writing filter is visible");
            Assert.IsTrue(labels.Any(t => t.Contains("SPEAKING")), "Speaking filter is visible");
            Assert.IsTrue(labels.Any(t => t.Contains("IELTS Writing")), "Writing package card is visible");
            Assert.IsTrue(labels.Any(t => t.Contains("IELTS Speaking")), "Speaking package card is visible");
            Capture("ielts-writing-speaking.png");
            center.Hide();
        }

        private static void Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Bao_Cao", "ui-redesign"));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name));
        }
    }
}
