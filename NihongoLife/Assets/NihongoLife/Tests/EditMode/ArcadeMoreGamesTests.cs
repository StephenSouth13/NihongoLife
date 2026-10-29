using System.Collections.Generic;
using System.Linq;
using NihongoLife.MiniGames;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>The second wave of arcade games: their content assets, the controller mapping and the tense exercise bank they read.</summary>
    public class ArcadeMoreGamesTests
    {
        public static readonly string[] NewIds = { "word_whack", "echo_pads", "kana_snake", "kana_breaker", "dance_arrows", "tense_typer", "wheel_quiz", "air_hockey" };
        private const string Dir = "Assets/NihongoLife/MiniGames/Data/";

        private static MiniGameDefinition Load(string id) => AssetDatabase.LoadAssetAtPath<MiniGameDefinition>($"{Dir}minigame_{id}.asset");

        [Test]
        public void EveryNewGame_HasAPlayableDefinition_WithThreeLanguages()
        {
            foreach (string id in NewIds)
            {
                var d = Load(id);
                Assert.NotNull(d, id + " definition asset");
                Assert.AreEqual(id, d.id);
                Assert.IsTrue(d.playable, id + " playable");
                Assert.IsNotEmpty(d.titleJa, id + " ja title");
                Assert.IsNotEmpty(d.titleVi, id + " vi title");
                Assert.IsNotEmpty(d.titleEn, id + " en title");
                Assert.IsNotEmpty(d.descriptionVi, id + " vi description");
                Assert.IsNotEmpty(d.descriptionEn, id + " en description");
                Assert.IsNotEmpty(d.descriptionJa, id + " ja description");
                Assert.Greater(d.timeLimitSeconds, 30f, id + " time limit");
            }
        }

        [Test]
        public void AllArcadeGames_HaveDistinctIdsAndKinds()
        {
            var all = AssetDatabase.FindAssets("t:MiniGameDefinition", new[] { "Assets/NihongoLife/MiniGames/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<MiniGameDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d => d != null && d.playable).ToList();
            Assert.GreaterOrEqual(all.Count, 14, "6 original + 8 new playable games");
            Assert.AreEqual(all.Count, all.Select(d => d.id).Distinct().Count(), "ids are unique");
            Assert.AreEqual(all.Count, all.Select(d => d.kind).Distinct().Count(), "every game has its own kind");
        }

        [Test]
        public void ContentGames_GetEnoughPairs_AndTheOthersNeedNone()
        {
            foreach (string id in new[] { "word_whack", "kana_snake", "kana_breaker", "wheel_quiz" })
            {
                var d = Load(id);
                Assert.IsNotEmpty(d.contentSets, id + " has content");
                Assert.IsTrue(d.contentSets.Any(s => s != null && s.pairs.Count >= 12), id + " has a set with at least 12 pairs");
            }
            foreach (string id in new[] { "kana_snake", "kana_breaker" })
                Assert.IsTrue(Load(id).contentSets.All(s => s.type == KanaPairType.HiraganaRomaji || s.type == KanaPairType.KatakanaRomaji), id + " reads kana only");
            foreach (string id in new[] { "echo_pads", "dance_arrows", "tense_typer", "air_hockey" })
                Assert.IsEmpty(Load(id).contentSets, id + " has no learning sets");
        }

        [Test]
        public void TenseExerciseBank_LoadsTheCourseExercises_AndChecksAnswers()
        {
            var bank = TenseExerciseBank.Load();
            Assert.GreaterOrEqual(bank.Count, 72, "the 12-tense course ships 72 single-blank exercises (120 in all, the rest are choice and reorder)");
            Assert.GreaterOrEqual(bank.Select(e => e.tense).Distinct().Count(), 12, "all 12 tenses");
            foreach (var e in bank)
            {
                Assert.IsTrue(e.promptEn.Contains("_____"), e.id + " has a gap");
                Assert.IsTrue(e.Accepts(e.acceptedAnswers[0]), e.id + " accepts its own answer");
                Assert.IsTrue(e.Accepts("  " + e.acceptedAnswers[0].ToUpperInvariant() + " "), e.id + " ignores case and spaces");
                Assert.IsFalse(e.Accepts(""), e.id + " rejects an empty answer");
            }
            Assert.AreEqual("Present Simple", new TenseExerciseBank.Exercise { tense = "present_simple" }.TenseName);
        }

        [Test]
        public void TenseExerciseBank_Ids_AreUnique()
        {
            var ids = TenseExerciseBank.Load().Select(e => e.id).ToList();
            Assert.AreEqual(ids.Count, new HashSet<string>(ids).Count);
        }
    }
}
