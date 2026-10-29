using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Entering the islands and the clinic leaves the console clean: no errors, no exceptions, no missing scripts and
    /// no repeated warnings. Every message is listed in the failure so the cause is visible at once.
    /// </summary>
    public class ZoneConsolePlayModeTests
    {
        private readonly List<string> _log = new List<string>();

        private void Capture(string message, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            // Outside the game's control: the licence / cloud config of a batch run, the real network.
            if (message.Contains("Curl error") || message.Contains("[Licensing") || message.Contains("[SupabaseRealtime]") || message.Contains("[Supabase]")) return;
            _log.Add(type + ": " + message.Split('\n')[0]);
        }

        [UnityTest]
        public IEnumerator Islands_And_Clinic_LoadWithoutConsoleErrors()
        {
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += Capture;
            var report = new List<string>();
            try
            {
                foreach (var scene in new[] { WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.PhuQuocIslandScene, WorldLocationCatalog.ClinicScene, WorldLocationCatalog.MusicShopScene })
                {
                    _log.Clear();
                    if (scene == WorldLocationCatalog.ClinicScene || scene == WorldLocationCatalog.MusicShopScene)
                    {
                        // The clinic is a zone: entered from the town like a player does.
                        yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                        yield return new WaitForSecondsRealtime(2f);
                        _log.Clear();
                        var flow = Object.FindFirstObjectByType<SceneFlowController>();
                        flow.EnterZone(scene, scene == WorldLocationCatalog.ClinicScene ? WorldLocationCatalog.ClinicEntrance : WorldLocationCatalog.MusicShopEntrance, scene);
                        for (float t = 0f; t < 20f && (flow.IsLoading || SceneManager.GetActiveScene().name != scene); t += Time.unscaledDeltaTime) yield return null;
                    }
                    else
                    {
                        // Play inside a zone boots the town first, then enters the zone.
                        yield return SceneManager.LoadSceneAsync(scene);
                        for (float t = 0f; t < 40f && (SceneManager.GetActiveScene().name != scene || Object.FindFirstObjectByType<PlayerController>() == null); t += Time.unscaledDeltaTime) yield return null;
                    }
                    var dm = DialogueManager.Instance;
                    for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                    yield return new WaitForSecondsRealtime(4f);

                    // Objects with a missing script, by name.
                    foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (go.GetComponents<Component>().Any(c => c == null)) _log.Add("Missing script on: " + Path(go.transform));

                    var distinct = _log.GroupBy(l => l).Select(g => (g.Count() > 1 ? g.Count() + "× " : "") + g.Key).ToList();
                    Debug.Log($"[ZoneConsole] {scene}: {distinct.Count} distinct messages\n" + string.Join("\n", distinct));
                    if (distinct.Count > 0) report.Add(scene + ":\n  " + string.Join("\n  ", distinct.Take(25)));
                }
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }
            Assert.IsEmpty(report, "Console messages when entering:\n" + string.Join("\n", report));
        }

        private static string Path(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
