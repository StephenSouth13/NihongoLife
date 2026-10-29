using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Island;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>F2: tiered skills and the skills screen (Y). Captures: Bao_Cao/skills.</summary>
    public class SkillTiersPlayModeTests
    {
        private int _fished, _harvested, _fed, _sold;

        [TearDown]
        public void TearDown()
        {
            var r = IslandState.Record; if (r != null) { r.fished = _fished; r.harvested = _harvested; r.fed = _fed; r.sold = _sold; }
            Loc.Set(GameLanguage.Vietnamese);
        }

        [Test]
        public void Tiers_FollowWhatThePlayerDid()
        {
            var farm = SkillTiers.Skill("farming");
            Assert.AreEqual(1, farm.TierFor(0)); Assert.AreEqual(1, farm.TierFor(4)); Assert.AreEqual(2, farm.TierFor(5));
            Assert.AreEqual(5, farm.TierFor(200)); Assert.AreEqual(0, farm.ToNext(500)); Assert.AreEqual(1, farm.ToNext(4));
            var fish = SkillTiers.Skill("fishing");
            for (int n = 0; n < 40; n++) Assert.AreEqual(FishingSkill.LevelFor(n), fish.TierFor(n), "Fishing tiers = FishingSkill levels at " + n);
            Assert.AreEqual(4, fish.Tiers);
            Assert.AreEqual("●●○○○", SkillTiers.Dots(2, 5));
            Assert.AreEqual(6, SkillTiers.All.Length);
        }

        [UnityTest]
        public IEnumerator SkillsScreen_ShowsTiersAndPerks_InEveryLanguage()
        {
            LogAssert.ignoreFailingMessages = true;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/skills")); Directory.CreateDirectory(folder);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<SkillsPopup>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            var ui = Object.FindFirstObjectByType<SkillsPopup>();
            Assert.NotNull(ui, "The skills screen is created with the HUD (key Y)");
            var rec = IslandState.Record; _fished = rec.fished; _harvested = rec.harvested; _fed = rec.fed; _sold = rec.sold;
            rec.fished = 13; rec.harvested = 30; rec.fed = 0; rec.sold = 5;

            Loc.Set(GameLanguage.English); ui.Show(); yield return null; yield return null;
            Assert.IsTrue(ui.IsOpen);
            var text = ui.RowsText;
            Assert.IsTrue(text.Contains("Fishing") && text.Contains("Farming") && text.Contains("Study"), text);
            Assert.IsTrue(text.Contains("●●●○") && text.Contains("Skilled angler"), "Fishing tier 3 at 13 fish: " + text);
            Assert.IsTrue(text.Contains("Fish bite 20% sooner") && !text.Contains("Fish bite 35%"), "Tier-3 perk only: " + text);
            Assert.IsTrue(text.Contains("17 more for tier 4"), text);   // 30 - 13
            Assert.IsTrue(text.Contains("50 more for tier 4"), text);   // farming: 80 - 30
            Capture(ui, Path.Combine(folder, "01_skills_en.png"));

            foreach (var lang in new[] { GameLanguage.Vietnamese, GameLanguage.Russian })
            {
                Loc.Set(lang); ui.Hide(); ui.Show(); yield return null;
                string t2 = ui.RowsText;
                Assert.IsFalse(t2.Contains("Fishing") || t2.Contains("Farming") || t2.Contains("Skilled") || t2.Contains("more for tier"), lang + ": " + t2);
                if (lang == GameLanguage.Russian) Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(t2, "[ăâêôơưđ]"), "No Vietnamese in Russian: " + t2);
            }
            ui.Hide();
        }

        private static void Capture(MenuPopupBase popup, string path)
        {
            var cam = Camera.main; var c = popup.GetComponentInParent<Canvas>().rootCanvas; var mode = c.renderMode;
            c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.06f; Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex);
            c.renderMode = mode; c.worldCamera = null;
        }
    }
}