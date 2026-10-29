using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// G3 (part 2): the popups opened from hotkeys show only the chosen language.
    /// Hard for the screens made in this project's own UI (company, skills, quest log, guide, about, study hub);
    /// the rest (courses, exams) hold learning content and are only reported. Report: Bao_Cao/lang-leak/popups.txt.
    /// </summary>
    public class LanguageLeakPopupsPlayModeTests
    {
        private static readonly string[] Hard = { "BusinessDashboardUI", "SkillsPopup", "QuestLogPopup", "GuidePopup", "AboutPopup", "StudyHubPopup" };
        private static readonly string[] Skip = { "ExamPlayUI", "InGameVideoPlayerUI", "AdminControlCenter" };

        [UnityTest]
        public IEnumerator City_Popups_ShowOnlyTheChosenLanguage()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<NihongoLife.Player.PlayerController>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int g = 0; g < 40 && dm != null && dm.IsOpen; g++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }

            var report = new StringBuilder(); int hard = 0, checkedCount = 0; var original = Loc.Language;
            var popups = Object.FindObjectsByType<MenuPopupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => !Skip.Contains(p.GetType().Name)).ToList();
            foreach (var lang in new[] { GameLanguage.English, GameLanguage.Russian, GameLanguage.Vietnamese })
            {
                Loc.Set(lang); yield return null; yield return null;
                foreach (var popup in popups)
                {
                    if (popup == null) continue;
                    bool ok = true;
                    try { popup.Show(); } catch (System.Exception e) { report.AppendLine("SHOWFAIL " + popup.GetType().Name + ": " + e.Message); ok = false; }
                    if (!ok) continue;
                    yield return null; yield return null;
                    checkedCount++;
                    var seen = new HashSet<string>();
                    foreach (var tmp in popup.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (!tmp.gameObject.activeInHierarchy) continue;
                        foreach (var leak in LanguageLeakPlayModeTests.Leaks(lang, tmp.text, popup.GetType().Name + "/" + tmp.name))
                        {
                            if (!seen.Add(leak)) continue;
                            bool isHard = Hard.Contains(popup.GetType().Name) && !leak.StartsWith("[en-in-ru]");
                            report.AppendLine((isHard ? "HARD " : "note ") + lang + " " + leak);
                            if (isHard) hard++;
                        }
                    }
                    try { popup.Hide(); } catch { }
                    yield return null;
                }
            }
            Loc.Set(original);
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/lang-leak")); Directory.CreateDirectory(folder);
            report.Insert(0, "popups scanned (x3 languages): " + checkedCount + " of " + popups.Count + " popup types: " + string.Join(", ", popups.Select(p => p.GetType().Name)) + "\n");
            File.WriteAllText(Path.Combine(folder, "popups.txt"), report.ToString(), new UTF8Encoding(false));
            Assert.Greater(checkedCount, 6, "Some popups must be scanned");
            Assert.AreEqual(0, hard, "Leaks in popups (Bao_Cao/lang-leak/popups.txt):\n" + string.Join("\n", report.ToString().Split('\n').Where(l => l.StartsWith("HARD")).Take(25)));
        }
    }
}