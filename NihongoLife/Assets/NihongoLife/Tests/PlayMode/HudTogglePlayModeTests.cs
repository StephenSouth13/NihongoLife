using System.Collections;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>The parts of the HUD close and come back from the bar, and the choice is remembered.</summary>
    public class HudTogglePlayModeTests
    {
        [TearDown]
        public void TearDown() => HudPrefs.Reset();

        [UnityTest, Timeout(90000)]
        public IEnumerator Bar_ClosesAndBringsBackTheHud()
        {
            HudPrefs.Reset();
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock);
            var bar = Object.FindFirstObjectByType<HudToggleBar>();
            Assert.NotNull(bar, "The bar is built with the HUD");
            Assert.IsTrue(dock.StatusWidget.gameObject.activeSelf);

            bar.transform.Find("ToggleStatus").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsTrue(HudPrefs.StatusHidden);
            Assert.IsFalse(dock.StatusWidget.gameObject.activeSelf, "The card closed");

            bar.transform.Find("ToggleStatus").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsTrue(dock.StatusWidget.gameObject.activeSelf, "The card came back");

            bar.transform.Find("ToggleQuest").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.IsTrue(HudPrefs.QuestHidden);
            bool shown = false;
            foreach (var t in dock.GetComponentsInChildren<RectTransform>(true))
                if (t.name == "TrackedObjective") shown = t.gameObject.activeSelf;
            Assert.IsFalse(shown, "The tracker closed");
        }
    }
}
