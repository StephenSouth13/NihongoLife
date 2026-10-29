using System.Linq;
using NihongoLife.Gestures;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>The gesture library (Resources/Animation/gestures.json): complete, valid and in four languages.</summary>
    public class GestureLibraryTests
    {
        [SetUp] public void SetUp() => GestureLibrary.Reset();

        [Test]
        public void Library_Loads_WithAtLeast70Gestures_AndProps()
        {
            Assert.GreaterOrEqual(GestureLibrary.All.Count, 70);
            Assert.GreaterOrEqual(GestureLibrary.File.props.Length, 45);
        }

        [Test]
        public void EveryGesture_HasFourLanguageNames_UniqueId_AndValidChannels()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var g in GestureLibrary.All)
            {
                Assert.IsTrue(seen.Add(g.id), "duplicate " + g.id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(g.vi), g.id + " vi");
                Assert.IsFalse(string.IsNullOrWhiteSpace(g.en), g.id + " en");
                Assert.IsFalse(string.IsNullOrWhiteSpace(g.ja), g.id + " ja");
                Assert.IsFalse(string.IsNullOrWhiteSpace(g.ru), g.id + " ru");
                Assert.Greater(g.dur, 0.3f, g.id);
                Assert.IsNotEmpty(g.ch, g.id);
                foreach (var c in g.ch)
                {
                    Assert.Contains(c.n, new[] { "rh", "lh", "rf", "lf", "hp", "hr", "sp", "hd", "p0", "p1", "p2", "p3" }, g.id);
                    if (c.n == "rh" || c.n == "lh") Assert.LessOrEqual(c.MaxMagnitude(), 1.2f, g.id + "/" + c.n);
                    for (int i = 1; i < c.k.Length; i++) Assert.Greater(c.k[i].t, c.k[i - 1].t, g.id + " key order");
                }
                foreach (var u in g.props) Assert.NotNull(GestureLibrary.Prop(u.id), g.id + " prop " + u.id);
            }
        }

        [Test]
        public void EveryProp_HasPieces_AndKnownShapes()
        {
            foreach (var p in GestureLibrary.File.props)
            {
                Assert.IsNotEmpty(p.pieces, p.id);
                Assert.Contains(p.hand, new[] { "R", "L", "A", "W", "H" }, p.id);
                foreach (var pc in p.pieces) Assert.Contains(pc.shape, new[] { "cube", "sphere", "cyl", "capsule" }, p.id);
            }
        }

        [Test]
        public void OwnersList_IsCovered()
        {
            string[] need =
            {
                // greetings and interactions
                "bow_formal", "wai", "namaste", "gassho", "handshake", "high_five", "wave", "hug", "sit_chair", "pickup", "carry_box",
                "eat_rice", "drink_cup", "sweep", "mop_floor", "water_plants",
                // cooking
                "cook_chop", "cook_stir", "cook_flip", "cook_plate",
                // instruments
                "piano_play", "guitar_strum", "drums_play", "dj_mix",
                // dance
                "dance_hiphop_bounce", "dance_hiphop_wave", "dance_hiphop_stomp", "dance_disco_point", "dance_robot", "dance_folk_sway",
                // jobs
                "farm_hoe", "farm_plant", "farm_harvest", "cashier_scan", "cashier_pay", "sushi_press", "sushi_roll", "paint_canvas", "serve_tray", "doctor_check"
            };
            foreach (var id in need) Assert.NotNull(GestureLibrary.Find(id), id);
        }

        [Test]
        public void Categories_AreAllPresent()
        {
            foreach (var cat in new[] { "greet", "interact", "cook", "music", "dance", "job" })
                Assert.GreaterOrEqual(GestureLibrary.InCategory(cat).Count(), 4, cat);
        }

        [Test]
        public void ChannelEval_IsSmoothAndWrapsForLoops()
        {
            var c = new GChannel { n = "rh", k = new[] { new GKey { t = 0, x = 0 }, new GKey { t = 1, x = 1 }, new GKey { t = 2, x = 0 } } };
            Assert.AreEqual(0f, c.Eval(0f, 2f, true).x, 1e-4f);
            Assert.AreEqual(1f, c.Eval(1f, 2f, true).x, 1e-4f);
            Assert.AreEqual(0.5f, c.Eval(0.5f, 2f, true).x, 1e-3f);
            Assert.AreEqual(c.Eval(0.3f, 2f, true).x, c.Eval(2.3f, 2f, true).x, 1e-4f);
        }
    }
}