using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The baked direction signs of the town, Hibari Station and Midori Island: they exist in their scene, point at the places they name
    /// (target = the scene object, arrow and distance from the post), follow the interface language, and the station controller runs on the
    /// timetable JSON. Captures: Bao_Cao/route-signs (one per post, Vietnamese and Russian).
    /// </summary>
    public class RouteSignPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/route-signs"));

        /// <summary>Play inside a zone boots the town first, then enters the zone: wait until it is really there.</summary>
        private static IEnumerator Arrive(string scene)
        {
            for (float t = 0f; t < 40f && (SceneManager.GetActiveScene().name != scene || Object.FindFirstObjectByType<NihongoLife.Player.PlayerController>() == null); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
        }

        [UnityTest]
        public IEnumerator EveryMap_HasItsSigns_PointingAtTheirTargets_InEveryLanguage()
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(Folder);
            var before = Loc.Language;
            try
            {
                foreach (var map in RouteSignCatalog.File.maps)
                {
                    Loc.Set(GameLanguage.Vietnamese);
                    yield return SceneManager.LoadSceneAsync(map.scene);
                    yield return Arrive(map.scene);
                    var posts = Object.FindObjectsByType<RouteSignPost>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.gameObject.scene.name == map.scene).ToArray();
                    Assert.AreEqual(map.posts.Length, posts.Length, map.scene + ": baked posts");
                    foreach (var data in map.posts)
                    {
                        var post = posts.Single(p => p.PostId == data.id);
                        string where = map.scene + "/" + data.id;
                        Assert.AreEqual(data.name, post.name, where + " name");
                        Assert.Less((post.transform.position - new Vector3(data.pos[0], data.pos[1], data.pos[2])).magnitude, 0.05f, where + " position");
                        Assert.AreEqual(data.boards.Length, post.Boards.Length, where + " boards");
                        for (int i = 0; i < data.boards.Length; i++)
                        {
                            var board = data.boards[i];
                            var view = post.Boards[i];
                            Assert.AreEqual(board.id, view.id, where + " board order");
                            if (!string.IsNullOrEmpty(board.anchor))
                            {
                                var anchor = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                    .First(t => t.name == board.anchor.Substring(board.anchor.LastIndexOf('/') + 1));
                                var flat = new Vector3(anchor.position.x, 0f, anchor.position.z);
                                Assert.Less((view.target - flat).magnitude, 0.5f, where + "/" + board.id + " target is the scene object " + board.anchor);
                            }
                            foreach (bool front in new[] { true, false })
                            {
                                string line = post.Line(i, front);
                                StringAssert.Contains(board.label.ja, line, where + "/" + board.id + " Japanese label");
                                StringAssert.Contains("m</size>", line, where + "/" + board.id + " distance");
                                Assert.IsTrue("\u2191\u2197\u2192\u2198\u2193\u2199\u2190\u2196".Contains(line[0]), where + "/" + board.id + " starts with an arrow");
                                Assert.AreEqual(line, (front ? view.front : view.back).text, where + "/" + board.id + " the baked text is the live text");
                            }
                            // Front and back read from opposite sides, so their arrows point opposite ways.
                            int a = "\u2191\u2197\u2192\u2198\u2193\u2199\u2190\u2196".IndexOf(post.Line(i, true)[0]);
                            int b = "\u2191\u2197\u2192\u2198\u2193\u2199\u2190\u2196".IndexOf(post.Line(i, false)[0]);
                            Assert.AreEqual((a + 4) % 8, b, where + "/" + board.id + " back arrow is the front arrow turned round");
                        }

                        // Language: Vietnamese shows the Vietnamese label, English the English one, Russian Cyrillic, Japanese only Japanese.
                        Loc.Set(GameLanguage.Vietnamese);
                        for (int i = 0; i < data.boards.Length; i++) StringAssert.Contains(data.boards[i].label.vi, post.Line(i, true), where + " vi");
                        yield return Capture(post, map.scene + "_" + data.id + "_vi");
                        Loc.Set(GameLanguage.English);
                        for (int i = 0; i < data.boards.Length; i++) StringAssert.Contains(data.boards[i].label.en, post.Line(i, true), where + " en");
                        Loc.Set(GameLanguage.Japanese);
                        for (int i = 0; i < data.boards.Length; i++) Assert.IsFalse(post.Line(i, true).Contains(data.boards[i].label.vi), where + " ja shows no Vietnamese");
                        Loc.Set(GameLanguage.Russian);
                        for (int i = 0; i < data.boards.Length; i++)
                        {
                            StringAssert.Contains(data.boards[i].label.ru, post.Line(i, true), where + " ru");
                            Assert.AreEqual(post.Line(i, true), post.Boards[i].front.text, where + " the board redrew itself on the language change");
                        }
                        yield return Capture(post, map.scene + "_" + data.id + "_ru");
                        Loc.Set(GameLanguage.Vietnamese);
                    }
                }
            }
            finally { Loc.Set(before); }
        }

        [UnityTest]
        public IEnumerator Town_SpawnPost_PointsTheRightWay()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1.5f);
            var post = Object.FindObjectsByType<RouteSignPost>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(p => p.PostId == "spawn");
            int station = post.Boards.ToList().FindIndex(b => b.id == "station");
            int home = post.Boards.ToList().FindIndex(b => b.id == "home");
            // The sign stands at (4.6, -16.4): the station entrance (24, 0) is ahead and to the right, the apartment (-30, 1) ahead and to the left.
            Assert.AreEqual('\u2197', post.Line(station, true)[0], "station is ahead-right for a viewer walking north");
            Assert.AreEqual('\u2196', post.Line(home, true)[0], "home is ahead-left for a viewer walking north");
            Assert.AreEqual('\u2199', post.Line(station, false)[0], "and behind-left for a viewer walking south");
        }

        [UnityTest]
        public IEnumerator Station_RunsOnTheTimetableJson()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.StationScene);
            yield return Arrive(WorldLocationCatalog.StationScene);
            var controller = Object.FindFirstObjectByType<StationTravelController>();
            Assert.NotNull(controller);
            var data = TrainTimetable.Data;
            float Field(string name) => (float)typeof(StationTravelController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
            Assert.AreEqual(data.serviceIntervalSeconds, Field("serviceInterval"), "service interval from the timetable");
            Assert.AreEqual(data.boardingWindowSeconds, Field("boardingWindow"), "door window from the timetable");
            Assert.AreEqual(data.trainTravelDistance, Field("trainTravelDistance"), "roll-out distance from the timetable");
            Assert.AreEqual(data.stops.Select(s => s.price).ToArray(), StationTravelController.Line.Select(s => s.Price).ToArray(), "fares from the timetable");
        }

        private static IEnumerator Capture(RouteSignPost post, string name)
        {
            yield return null;
            var go = new GameObject("RouteSignCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.9f);
            cam.nearClipPlane = 0.1f;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            go.transform.position = post.transform.TransformPoint(new Vector3(0f, 1.75f, -5.2f));
            go.transform.rotation = post.transform.rotation;
            var previous = RenderTexture.active;
            cam.Render();
            RenderTexture.active = rt;
            var shot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            shot.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), shot.EncodeToPNG());
            RenderTexture.active = previous;
            Object.Destroy(shot);
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(go);
        }
    }
}
