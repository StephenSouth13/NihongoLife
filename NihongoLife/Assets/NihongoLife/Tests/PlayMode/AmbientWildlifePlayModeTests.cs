using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>K1: ambient animals (birds, dogs, cats, squirrels). Captures: Bao_Cao/ambient-wildlife.</summary>
    public class AmbientWildlifePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ambient-wildlife"));
        private PlayerController _player;
        private AmbientWildlife _w;

        [TearDown]
        public void TearDown() { AmbientWildlife.SeasonOverride = null; AmbientWildlife.HourOverride = null; }

        [Test]
        public void Budget_FollowsSeasonHourAndRain()
        {
            int Birds(Season s, float h, bool rain) => AmbientWildlife.Budget(WildKind.Bird, s, h, rain);
            Assert.Greater(Birds(Season.Spring, 9f, false), Birds(Season.Winter, 9f, false), "More birds in spring than in winter");
            Assert.Greater(Birds(Season.Summer, 12f, false), Birds(Season.Autumn, 12f, false) - 1);
            Assert.AreEqual(0, Birds(Season.Summer, 23f, false), "No birds at night");
            Assert.Less(Birds(Season.Summer, 12f, true), Birds(Season.Summer, 12f, false), "Fewer birds in the rain");
            Assert.AreEqual(0, AmbientWildlife.Budget(WildKind.Squirrel, Season.Summer, 23f, false), "Squirrels sleep at night");
            Assert.Greater(AmbientWildlife.Budget(WildKind.Squirrel, Season.Autumn, 11f, false), AmbientWildlife.Budget(WildKind.Squirrel, Season.Winter, 11f, false), "Squirrels gather nuts in autumn");
            Assert.AreEqual(0, AmbientWildlife.Budget(WildKind.Dog, Season.Summer, 12f, true), "No dogs out in the rain");
            Assert.Greater(AmbientWildlife.Budget(WildKind.Dog, Season.Summer, 12f, false), AmbientWildlife.Budget(WildKind.Dog, Season.Winter, 12f, false));
            Assert.IsTrue(AmbientWildlife.IsSleepTime(WildKind.Dog, 23f));
            Assert.IsFalse(AmbientWildlife.IsSleepTime(WildKind.Dog, 12f));
            Assert.IsTrue(AmbientWildlife.IsSleepTime(WildKind.Cat, 13f), "Cats nap after lunch");
            Assert.IsNotNull(AmbientWildlife.ChirpClip(0)); Assert.Greater(AmbientWildlife.ChirpClip(1).length, 0.2f);
        }

        [UnityTest]
        public IEnumerator City_Wildlife_BehavioursSeasonsAndLod()
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(Folder);
            AmbientWildlife.SeasonOverride = Season.Summer; AmbientWildlife.HourOverride = 10f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>(); _w = AmbientWildlife.Instance;
                if (_player != null && _w != null && _w.Critters.Count > 0 && _w.ActiveCount(WildKind.Bird) > 0) break;
                yield return null;
            }
            Assert.NotNull(_player); Assert.NotNull(_w);
            yield return new WaitForSecondsRealtime(1.5f);
            _w.ApplyBudget();
            Assert.GreaterOrEqual(_w.ActiveCount(WildKind.Bird), 6, "Birds out on a summer morning");
            Assert.GreaterOrEqual(_w.ActiveCount(WildKind.Dog), 1); Assert.GreaterOrEqual(_w.ActiveCount(WildKind.Cat), 1);
            Assert.Greater(_w.Trees.Count, 0, "Trees found for the squirrels");
            Assert.GreaterOrEqual(_w.ActiveCount(WildKind.Squirrel), 1);
            int renderers = _w.Critters.Where(c => c.gameObject.activeSelf).Sum(c => c.GetComponentsInChildren<Renderer>().Count(r => r.enabled));
            Assert.Less(renderers, 160, "Renderer budget for WebGL");

            // Season change takes effect.
            AmbientWildlife.SeasonOverride = Season.Winter; _w.ApplyBudget();
            Assert.LessOrEqual(_w.ActiveCount(WildKind.Bird), 3, "Few birds in winter");
            AmbientWildlife.HourOverride = 23f; _w.ApplyBudget();
            Assert.AreEqual(0, _w.ActiveCount(WildKind.Bird), "No birds at night");
            Assert.AreEqual(0, _w.ActiveCount(WildKind.Squirrel), "No squirrels at night");
            AmbientWildlife.SeasonOverride = Season.Summer; AmbientWildlife.HourOverride = 10f; _w.ApplyBudget();

            var dog = _w.Critters.First(c => c.Kind == WildKind.Dog && c.gameObject.activeSelf);
            var cat = _w.Critters.First(c => c.Kind == WildKind.Cat && c.gameObject.activeSelf);
            var bird = _w.Critters.First(c => c.Kind == WildKind.Bird && c.gameObject.activeSelf);
            var squirrel = _w.Critters.First(c => c.Kind == WildKind.Squirrel && c.gameObject.activeSelf);
            Vector3 P() => _player.transform.position;

            // Dog: sit, sleep (no movement), walk (moves), pet it (follows).
            PutNear(dog, 5f);
            dog.Force(Critter.State.Sit, 30f); yield return new WaitForSecondsRealtime(1.2f);
            float sitY = dog.transform.GetChild(0).GetChild(0).localPosition.y;
            dog.Force(Critter.State.Sleep, 30f); yield return new WaitForSecondsRealtime(1.2f);
            Vector3 p0 = dog.transform.position; yield return new WaitForSecondsRealtime(1f);
            Assert.Less(Vector3.Distance(p0, dog.transform.position), 0.05f, "A sleeping dog stays put");
            Assert.Less(dog.transform.GetChild(0).GetChild(0).localPosition.y, sitY, "Lying down is lower than sitting");
            dog.Force(Critter.State.Walk, 30f); p0 = dog.transform.position; yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(Vector3.Distance(p0, dog.transform.position), 0.8f, "A walking dog moves");
            PutNear(dog, 6f);
            dog.Interact(_player.gameObject);
            Assert.AreEqual(Critter.State.Follow, dog.Current); Assert.Greater(dog.FriendUntil, Time.time);
            float d0 = Vector3.Distance(dog.transform.position, P());
            yield return new WaitForSecondsRealtime(3.5f);
            Assert.Less(Vector3.Distance(dog.transform.position, P()), d0 - 1.5f, "A petted dog comes to the player");
            Capture("dog_follow");

            // Cat: sits, walks.
            PutNear(cat, 4f);
            cat.Force(Critter.State.Walk, 30f); p0 = cat.transform.position; yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(Vector3.Distance(p0, cat.transform.position), 0.5f, "A walking cat moves");
            cat.Force(Critter.State.Sit, 10f); yield return null;
            Assert.AreEqual(Critter.State.Sit, cat.Current);

            // Bird: perched -> flying (gains height) -> chirps.
            bird.Force(Critter.State.Perched, 30f); yield return new WaitForSecondsRealtime(0.3f);
            bird.Force(Critter.State.Flying, 30f); yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(bird.transform.position.y, P().y + 3.5f, "A flying bird is up in the air");
            int chirps = 0; for (float t = 0f; t < 14f && chirps == 0; t += 0.5f) { yield return new WaitForSecondsRealtime(0.5f); chirps = _w.Critters.Where(c => c.Kind == WildKind.Bird).Sum(c => c.Chirps); }
            Assert.Greater(chirps, 0, "Birds chirp");

            // Squirrel: runs to a tree and climbs it, then comes down.
            Assert.IsTrue(_w.NearestTree(P(), 80f, out var tb, out var top));
            squirrel.transform.position = tb + new Vector3(3f, 0f, 0f);
            if (AmbientWildlife.Ground(squirrel.transform.position + Vector3.up * 3f, out var gp)) squirrel.transform.position = gp;
            squirrel.Force(Critter.State.ToTree, 14f);
            float maxY = squirrel.transform.position.y; float t2 = 0f;
            while (t2 < 12f && squirrel.Current != Critter.State.OnTree) { t2 += Time.unscaledDeltaTime; maxY = Mathf.Max(maxY, squirrel.transform.position.y); yield return null; }
            Assert.AreEqual(Critter.State.OnTree, squirrel.Current, "The squirrel reaches the branch");
            Assert.Greater(squirrel.transform.position.y, tb.y + 1.2f, "It climbed the trunk");
            squirrel.Force(Critter.State.Nibble, 3f); yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(Critter.State.Nibble, squirrel.Current);

            // Staged shot for the report: dog, cat and a squirrel in front of the camera.
            PutNear(dog, 3.2f); PutNear(cat, 4.2f); dog.Force(Critter.State.Sit, 60f); cat.Force(Critter.State.Idle, 60f);
            squirrel.Force(Critter.State.Forage, 60f);
            if (AmbientWildlife.Ground(P() + _player.transform.forward * 5f + Vector3.up * 3f, out var sp)) squirrel.transform.position = sp;
            yield return new WaitForSecondsRealtime(1f);
            Capture("street_animals");

            // LOD / pooling: far away from everything the animals follow the player, none is left behind.
            var body = _player.GetComponent<CharacterController>(); body.enabled = false;
            _player.transform.position += new Vector3(110f, 0f, 0f); body.enabled = true; Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(6f);
            foreach (var c in _w.Critters.Where(c => c.gameObject.activeSelf))
                Assert.Less(Vector3.Distance(c.transform.position, P()), AmbientWildlife.DespawnRadius + 3f, $"{c.name} re-placed near the player");
        }

        private void PutNear(Critter c, float distance)
        {
            var fwd = Camera.main != null ? Camera.main.transform.forward : _player.transform.forward; fwd.y = 0f; fwd.Normalize();
            var p = _player.transform.position + fwd * distance;
            if (AmbientWildlife.Ground(p + Vector3.up * 3f, out var g)) p = g;
            c.transform.position = p;
        }

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