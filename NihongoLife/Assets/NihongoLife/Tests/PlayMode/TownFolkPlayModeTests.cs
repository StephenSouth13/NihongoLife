using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>F3 / H6: Midori residents and the town's house residents. Captures: Bao_Cao/island-npcs.</summary>
    public class TownFolkPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/island-npcs"));
        private PlayerController _player;

        [TearDown] public void TearDown() { TownFolk.HourOverride = null; AmbientWildlife.HourOverride = null; Loc.Set(GameLanguage.Vietnamese); }

        [Test]
        public void Lines_AreCompleteInEveryLanguage_AndNightIsIndoors()
        {
            TownFolk.RegisterLines();
            var all = TownFolk.Midori.Concat(TownFolk.City).ToArray();
            Assert.AreEqual(all.Length, all.Select(s => s.Id).Distinct().Count(), "Unique ids");
            foreach (var s in all)
            {
                Assert.GreaterOrEqual(s.Lines.Length, 2, s.Id);
                Assert.IsTrue(ScriptedNpcDialogue.ByNpc.ContainsKey(s.Id), s.Id + " registered");
                foreach (var l in s.Lines)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(l.Ja) || string.IsNullOrEmpty(l.Reading) || string.IsNullOrEmpty(l.Romaji) || string.IsNullOrEmpty(l.Vi) || string.IsNullOrEmpty(l.En), s.Id);
                    Assert.IsTrue(RuTable.TryGet(l.En, out var ru) && !string.IsNullOrEmpty(ru), "Russian line for: " + l.En);
                }
                Assert.IsNotNull(Resources.Load<GameObject>("Characters/Kids/NL_" + s.Model), s.Id + " body " + s.Model);
            }
            Assert.IsTrue(TownFolk.IsOutdoors(10f)); Assert.IsFalse(TownFolk.IsOutdoors(23f)); Assert.IsFalse(TownFolk.IsOutdoors(3f));
        }

        private IEnumerator Open(string scene, int min)
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(Folder);
            AmbientWildlife.HourOverride = 10f; TownFolk.HourOverride = 10f;
            yield return SceneManager.LoadSceneAsync(scene);
            for (float t = 0f; t < 30f; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                if (_player != null && TownFolk.Instance != null && TownFolk.Instance.Residents.Count >= min && t > 5f) break;
                yield return null;
            }
            Assert.NotNull(_player);
            yield return new WaitForSecondsRealtime(1f);
        }

        private void Report(string file)
        {
            var sb = new StringBuilder();
            foreach (var r in TownFolk.Instance.Residents)
                sb.AppendLine($"{r.Spec.Id}\t{r.Spec.Name}\thome={r.Home:F1}\tdist={Vector3.Distance(r.Home, _player.transform.position):F0}\tactive={r.gameObject.activeSelf}\twalked={r.Walked:F1}");
            File.WriteAllText(Path.Combine(Folder, file), sb.ToString(), new UTF8Encoding(false));
        }

        private IEnumerator Checks(string tag, int min)
        {
            var f = TownFolk.Instance;
            Report(tag + "_residents.txt");
            Assert.GreaterOrEqual(f.Residents.Count, min, "Residents spawned in " + tag);
            var active = f.Residents.Where(r => r.gameObject.activeSelf).ToList();
            Assert.GreaterOrEqual(active.Count, Mathf.Min(min, 4), "Residents out in daylight");
            foreach (var r in f.Residents) Assert.IsNotNull(r.GetComponent<NPCController>()); // can be talked to
            // They stroll (never frozen): after a while several of them have moved.
            yield return new WaitForSecondsRealtime(16f);
            Report(tag + "_residents_after16s.txt");
            Assert.GreaterOrEqual(f.Residents.Count(r => r.Walked > 0.4f), 2, "Residents wander");
            // Night: indoors. Morning: out again.
            TownFolk.HourOverride = 23f; yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(0, f.Residents.Count(r => r.gameObject.activeSelf), "Everyone is indoors at night");
            TownFolk.HourOverride = 10f; yield return new WaitForSecondsRealtime(0.5f);
            Assert.GreaterOrEqual(f.Residents.Count(r => r.gameObject.activeSelf), 2, "Back out in the morning");
            // Talk to one.
            var talker = f.Residents.First(r => r.gameObject.activeSelf);
            talker.Npc.Interact(_player.gameObject);
            yield return null;
            Assert.IsTrue(DialogueManager.Instance != null && DialogueManager.Instance.IsOpen, "Talking opens a dialogue");
            DialogueManager.Instance.CancelDialogue(); talker.Npc.StopInteracting();
            yield return null;
            // Bring one in front of the camera for the picture.
            var cam = Camera.main; var fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            var at = _player.transform.position + fwd * 3.5f;
            if (TownFolk.Ground(at, out var g, talker.transform)) at = g;
            talker.transform.position = at; talker.transform.rotation = Quaternion.LookRotation(-fwd);
            yield return null; yield return null;
            Capture(tag + "_resident");
            Vector3 vp = cam.WorldToViewportPoint(at + Vector3.up);
            Assert.IsTrue(vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1, "Resident is in view of the camera");
        }

        [UnityTest] public IEnumerator Midori_Residents() { yield return Open(WorldLocationCatalog.MidoriIslandScene, 8); yield return Checks("midori", 8); }
        [UnityTest] public IEnumerator City_HouseResidents() { yield return Open(WorldLocationCatalog.CityScene, 4); yield return Checks("city", 4); }

        private void Capture(string name)
        {
            var camera = Camera.main; if (camera == null) return;
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; Object.Destroy(rt); Object.Destroy(tex);
        }
    }
}