using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// G3: once a language is chosen, no other interface language and no yen sign leaks into the visible screens.
    /// Scans every TMP label of the city HUD and the bag in en / ru / vi. Japanese being learned (kana/kanji) is content and allowed.
    /// Report: Bao_Cao/lang-leak/report.txt.
    /// </summary>
    public class LanguageLeakPlayModeTests
    {
        private static readonly Regex Vietnamese = new Regex("[ăâêôơưđĐĂÂÊÔƠƯ\u1EA0-\u1EF9àáãèéìíòóõùúýÀÁÃÈÉÌÍÒÓÕÙÚÝ]");
        private static readonly Regex Cyrillic = new Regex("[\u0400-\u04FF]");
        private static readonly Regex YenAmount = new Regex("\u00a5\\s?\\d");
        private static readonly Regex EnglishSentence = new Regex("\\b[A-Za-z]{3,}\\b[ ,]+\\b[A-Za-z]{3,}\\b[ ,]+\\b[A-Za-z]{3,}\\b");

        private static string Path_ => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/lang-leak"));

        private static string Strip(string t) => Regex.Replace(t ?? "", "<[^>]+>", "");

        internal static IEnumerable<string> Leaks(GameLanguage lang, string text, string where)
        {
            string s = Strip(text);
            if (string.IsNullOrWhiteSpace(s)) yield break;
            if (lang != GameLanguage.Japanese && YenAmount.IsMatch(s)) yield return $"[yen] {where}: {s}";
            if (lang != GameLanguage.Vietnamese && Vietnamese.IsMatch(s) && !Cyrillic.IsMatch(s)) yield return $"[vi-in-{lang}] {where}: {s}";
            if (lang != GameLanguage.Russian && Cyrillic.IsMatch(s)) yield return $"[ru-in-{lang}] {where}: {s}";
            if (lang == GameLanguage.Russian && EnglishSentence.IsMatch(s) && !Cyrillic.IsMatch(s)) yield return $"[en-in-ru] {where}: {s}";
        }

        internal static string PathOf(Transform t) { var sb = new StringBuilder(t.name); for (var p = t.parent; p != null && sb.Length < 90; p = p.parent) sb.Insert(0, p.name + "/"); return sb.ToString(); }

        [UnityTest]
        public IEnumerator City_HudAndBag_ShowOnlyTheChosenLanguage()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || PlayerInventory.Instance == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var inv = PlayerInventory.Instance;
            if (inv.Items.Count == 0) inv.AddItem("a_cola", "コーラ", "Cola", 150, 6, false, ItemUseType.Drink, 0, 20, 0);
            var window = Object.FindObjectsByType<InventoryWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (window == null) window = InventoryWindow.Create(new GameObject("BagHost").transform, null);
            window.Root.SetActive(true);
            foreach (var tr in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tr.name == "SettingsPanel" || tr.name == "OnlineChatPanel" || tr.name == "ResumeDialogue") tr.gameObject.SetActive(true);
            yield return null;

            var report = new StringBuilder(); int hard = 0;
            var original = Loc.Language;
            foreach (var lang in new[] { GameLanguage.English, GameLanguage.Russian, GameLanguage.Vietnamese })
            {
                Loc.Set(lang);
                yield return null; yield return null; yield return null;
                window.Refresh(); window.SlotEnter(0);
                yield return null; yield return null;
                var seen = new HashSet<string>();
                foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    bool active = tmp.gameObject.activeInHierarchy;
                    foreach (var leak in Leaks(lang, tmp.text, PathOf(tmp.transform)))
                    {
                        if (!seen.Add(leak)) continue;
                        report.AppendLine((active ? "ACTIVE " : "hidden ") + lang + " " + leak);
                        if (active && !leak.StartsWith("[en-in-ru]")) hard++;
                    }
                }
                window.SlotExit(0);
            }
            Loc.Set(original);
            Directory.CreateDirectory(Path_);
            File.WriteAllText(Path.Combine(Path_, "report.txt"), report.ToString(), new UTF8Encoding(false));
            window.Root.SetActive(false);
            Assert.AreEqual(0, hard, "Visible labels in the wrong language / with a yen sign (see Bao_Cao/lang-leak/report.txt):\n" + string.Join("\n", report.ToString().Split('\n').Where(l => l.StartsWith("ACTIVE")).Take(25)));
        }
    }
}