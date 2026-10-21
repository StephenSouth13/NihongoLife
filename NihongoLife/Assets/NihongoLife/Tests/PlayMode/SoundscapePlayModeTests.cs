using System.Collections;
using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Scenario;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Audio in the real game: every spoken story line has its Japanese voice file (Resources/Voice/ja/&lt;quest&gt;/&lt;node&gt;),
    /// a story line plays its recorded voice (not the generated beeps), the town plays its day / night music with
    /// ambience, the music ducks under speech, and Midori Island switches to the island track.
    /// </summary>
    public class SoundscapePlayModeTests
    {
        [UnityTest]
        public IEnumerator Voices_Music_Ambience()
        {
            LogAssert.ignoreFailingMessages = true;

            // 1. Every NPC / narration line of every quest has a voice file.
            int lines = 0, missing = 0;
            var missingList = new System.Collections.Generic.List<string>();
            foreach (var scenario in Resources.LoadAll<ScenarioDefinition>("Scenarios"))
                foreach (var node in scenario.nodes.Where(n => !string.IsNullOrWhiteSpace(n.textJa)))
                {
                    lines++;
                    if (Resources.Load<AudioClip>($"Voice/ja/{scenario.id.Replace('.', '_')}/{node.id}") == null) { missing++; missingList.Add(scenario.id + "/" + node.id); }
                }
            Assert.Greater(lines, 300);
            Assert.AreEqual(0, missing, "Lines without voice: " + string.Join(", ", missingList.Take(10)));

            // 2. In town: music + ambience.
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 15f && (Soundscape.Instance == null || !Soundscape.Instance.MusicKey.StartsWith("city_")); t += Time.unscaledDeltaTime) yield return null;
            Assert.NotNull(Soundscape.Instance, "The soundscape runs in the game.");
            StringAssert.StartsWith("city_", Soundscape.Instance.MusicKey, "Town music plays.");
            var sources = Soundscape.Instance.GetComponents<AudioSource>();
            Assert.IsTrue(sources.Any(s => s.isPlaying && s.clip != null && s.clip.name.StartsWith("city_") && s.clip.length > 60f), "A town track is playing.");
            Debug.Log($"[Soundscape] town music {Soundscape.Instance.MusicKey}, ambience {Soundscape.Instance.AmbienceKey}");

            // 3. A story line speaks with its recorded voice.
            var repo = GameServices.Get<IScenarioRepository>();
            var first = repo.GetScenarioById("scenario.intro.arrival");
            ScenarioManager.Instance.StartScenario(first);
            yield return new WaitForSecondsRealtime(0.4f);
            var spoken = first.nodes.First(n => !string.IsNullOrWhiteSpace(n.textJa) && n.nodeType == ScenarioNodeType.Dialogue);
            ScenarioManager.Instance.TransitionToNode(spoken.id);
            yield return new WaitForSecondsRealtime(0.3f);
            var voice = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).FirstOrDefault(s => s.isPlaying && s.clip != null && s.clip.name == spoken.id);
            Assert.NotNull(voice, $"The line '{spoken.id}' plays its recorded voice.");
            Assert.Greater(voice.clip.length, 0.8f);
            Debug.Log($"[Soundscape] voice {voice.clip.name} {voice.clip.length:0.0}s");

            // 4. Midori Island has its own music.
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene);
            for (float t = 0f; t < 20f && Soundscape.Instance.MusicKey != "island"; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual("island", Soundscape.Instance.MusicKey, "The island plays the island track.");
        }
    }
}
