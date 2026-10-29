using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>The line's timetable and the direction signs of every map are data (Resources/Train/timetable.json, Resources/Navigation/route_signs.json); these tests keep that data sane.</summary>
    public class RouteSignAndTimetableTests
    {
        private static string ScenesDir => Path.Combine(Application.dataPath, "NihongoLife/Scenes");

        [SetUp]
        public void Reload()
        {
            TrainTimetable.Reload();
            RouteSignCatalog.Reload();
        }

        [Test]
        public void Timetable_StopsFollowTheGameMap()
        {
            var data = TrainTimetable.Data;
            var expected = new[]
            {
                ("hibari", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance),
                ("gakuen", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance),
                ("minato", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance),
                ("midori", WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation),
                ("airport", WorldLocationCatalog.PhuQuocIslandScene, WorldLocationCatalog.PhuQuocArrival),
            };
            Assert.AreEqual(expected.Length, data.stops.Length, "the line has the five stops of the game");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Item1, data.stops[i].id);
                Assert.AreEqual(expected[i].Item2, data.stops[i].scene, data.stops[i].id + " scene");
                Assert.AreEqual(expected[i].Item3, data.stops[i].spawn, data.stops[i].id + " spawn");
                Assert.IsTrue(data.stops[i].place.HasAllLanguages, data.stops[i].id + " place has vi/en/ja/ru");
                Assert.IsFalse(string.IsNullOrWhiteSpace(data.stops[i].ja) || string.IsNullOrWhiteSpace(data.stops[i].vi), data.stops[i].id + " names");
                if (i > 0) Assert.Greater(data.stops[i].price, data.stops[i - 1].price, "fares grow along the line");
            }
            Assert.AreEqual(0, data.stops[0].price, "the home stop is free");
            Assert.IsTrue(data.lineName.HasAllLanguages, "the line name");
        }

        [Test]
        public void Timetable_TimingsLeaveRoomForTheTrainToLeaveAndReturn()
        {
            var data = TrainTimetable.Data;
            // StationTravelController rolls the train out for 12 s and back for 12 s around the doors-open window.
            Assert.Greater(data.serviceIntervalSeconds, data.boardingWindowSeconds + 24f + 1f, "a train arrives only after the previous one has gone and come back");
            Assert.Greater(data.boardingWindowSeconds, 5f, "enough time to board");
            Assert.Greater(data.legSeconds, 4f);
            Assert.Greater(data.dwellSeconds, 0f);
            Assert.Greater(data.trainTravelDistance, 10f);
            foreach (var stop in data.stops.Skip(1)) Assert.GreaterOrEqual(TrainTimetable.LegSeconds(stop), 4f, stop.id + " leg");
        }

        [Test]
        public void Timetable_FeedsTheController()
        {
            Assert.AreEqual(TrainTimetable.Data.stops.Length, StationTravelController.Line.Length);
            Assert.AreEqual("airport", StationTravelController.Line.Last().Id);
            foreach (var stop in StationTravelController.Line) Assert.IsFalse(string.IsNullOrEmpty(stop.Place), stop.Id + " place text");
        }

        [Test]
        public void RouteSigns_EveryMapHasASceneAndEveryBoardFourLanguages()
        {
            var file = RouteSignCatalog.File;
            CollectionAssert.AreEquivalent(
                new[] { WorldLocationCatalog.CityScene, WorldLocationCatalog.StationScene, WorldLocationCatalog.MidoriIslandScene },
                file.maps.Select(m => m.scene).ToArray(), "maps that carry route signs");
            foreach (var map in file.maps)
            {
                Assert.IsTrue(File.Exists(Path.Combine(ScenesDir, map.scene + ".unity")), map.scene + " exists");
                Assert.IsFalse(string.IsNullOrEmpty(map.root), map.scene + " root name");
                Assert.AreEqual(map.posts.Length, map.posts.Select(p => p.id).Distinct().Count(), map.scene + " post ids are unique");
                Assert.AreEqual(map.posts.Length, map.posts.Select(p => p.name).Distinct().Count(), map.scene + " post names are unique");
                foreach (var post in map.posts)
                {
                    Assert.AreEqual(3, post.pos.Length, post.id + " position");
                    Assert.GreaterOrEqual(post.boards.Length, 2, post.id + " has boards");
                    Assert.LessOrEqual(post.boards.Length, 8, post.id + " fits the pole");
                    Assert.AreEqual(post.boards.Length, post.boards.Select(b => b.id).Distinct().Count(), post.id + " board ids are unique");
                    foreach (var board in post.boards)
                    {
                        string where = $"{map.scene}/{post.id}/{board.id}";
                        Assert.IsTrue(board.label != null && board.label.HasAllLanguages, where + " label vi/en/ja/ru");
                        Assert.IsTrue(!string.IsNullOrEmpty(board.anchor) || (board.pos != null && board.pos.Length == 3), where + " has a target");
                        Assert.IsTrue(board.color == "green" || board.color == "blue", where + " colour");
                        Assert.IsTrue(board.label.ru.Any(c => c >= '\u0400' && c <= '\u04FF'), where + " Russian label is Cyrillic");
                    }
                }
            }
        }

        [Test]
        public void LocalizedLine_FollowsTheInterfaceLanguage()
        {
            var line = new LocalizedLine("Cổng", "Gates", "改札", "Турникеты");
            var before = Loc.Language;
            try
            {
                Loc.Set(GameLanguage.Vietnamese); Assert.AreEqual("Cổng", line.Text);
                Loc.Set(GameLanguage.English); Assert.AreEqual("Gates", line.Text); Assert.AreEqual("改札  \u00b7  Gates", line.Bilingual);
                Loc.Set(GameLanguage.Japanese); Assert.AreEqual("改札", line.Text); Assert.AreEqual("改札", line.Bilingual);
                Loc.Set(GameLanguage.Russian); Assert.AreEqual("Турникеты", line.Text);
            }
            finally { Loc.Set(before); }
        }
    }
}
