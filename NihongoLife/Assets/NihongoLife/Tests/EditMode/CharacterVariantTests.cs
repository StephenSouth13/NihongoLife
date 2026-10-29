using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The town's people (Resources/Characters/variants.json, built into NL_var_* prefabs by CharacterVariantBuilder):
    /// every one derives from the Kids family, the cast is diverse (skin tones, age, gender, build, hair), every job
    /// has its uniform and props, the proportions stay safe for the foot grounder, and every label speaks all
    /// three interface languages.
    /// </summary>
    public class CharacterVariantTests
    {
        private static readonly string[] Kids = { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };
        private static readonly string[] Jobs = { "doctor", "nurse", "chef", "waiter", "bank", "train", "cafe", "tourist" };
        private static readonly string[] Anchors = { "headTop", "head", "neck", "chestFront", "chestBack", "waistFront" };
        private const float KidScale = 1.35f;

        private static CharacterVariantFile File => CharacterVariants.File;
        private static bool IsHex(string hex) => !string.IsNullOrEmpty(hex) && hex.Length == 7 && hex[0] == '#' && ColorUtility.TryParseHtmlString(hex, out _);
        private static bool IsBoy(string kid) => LookPaletteCatalog.ForKid(kid)?.model.StartsWith("Boy") == true;

        [SetUp]
        public void SetUp() { CharacterVariants.Reset(); LookPaletteCatalog.Reset(); }

        [Test]
        public void Variants_AreIdentified_AndEachHasABuiltPrefab()
        {
            Assert.GreaterOrEqual(File.variants.Length, 24, "a real cast, not a handful");
            Assert.AreEqual(File.variants.Length, File.variants.Select(v => v.id).Distinct().Count(), "ids are unique");
            foreach (var v in File.variants)
            {
                Assert.That(v.id, Does.Match("^[a-z0-9_]+$"), "id " + v.id);
                var prefab = CharacterVariants.Prefab(v.id);
                Assert.NotNull(prefab, v.id + ": prefab NL_var_" + v.id + " is missing - run CharacterVariantBuilder.Build");
                var component = prefab.GetComponent<CharacterVariant>();
                Assert.NotNull(component, v.id + ": no CharacterVariant");
                Assert.AreEqual(v.id, component.VariantId, v.id + ": the prefab carries its own id");
                var animator = prefab.GetComponentInChildren<Animator>(true);
                Assert.NotNull(animator, v.id + ": no Animator");
                Assert.IsTrue(animator.avatar != null && animator.avatar.isHuman, v.id + ": humanoid avatar (the clips retarget)");
                Assert.NotNull(animator.runtimeAnimatorController, v.id + ": animated by the Kids controller");
            }
        }

        [Test]
        public void EveryPerson_DerivesFromTheKidsFamily()
        {
            foreach (var v in File.variants)
            {
                Assert.Contains(v.kid, Kids, v.id + ": only the 8 Kids models");
                var kid = Resources.Load<GameObject>("Characters/Kids/NL_" + v.kid);
                Assert.NotNull(kid, v.id + ": Kids prefab " + v.kid);
                var variantMesh = CharacterVariants.Prefab(v.id).GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh).ToArray();
                var kidMesh = kid.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh).ToArray();
                CollectionAssert.AreEqual(kidMesh, variantMesh, v.id + ": the same body mesh as " + v.kid);
            }
            Assert.AreEqual(8, File.variants.Select(v => v.kid).Distinct().Count(), "every Kids model is used");
        }

        [Test]
        public void EveryJob_HasAUniform_WithPropsThatSitOnThePrefabs()
        {
            foreach (var job in Jobs)
            {
                var u = File.Uniform(job);
                Assert.NotNull(u, "uniform " + job);
                Assert.GreaterOrEqual(u.pieces.Length, 3, job + ": a uniform is more than a colour (hat, tie, apron ...)");
                Assert.IsTrue(IsHex(u.topHex) && IsHex(u.bottomHex), job + ": top / bottom colours");
                var people = File.variants.Where(v => v.uniform == job).ToArray();
                Assert.GreaterOrEqual(people.Length, 2, job + ": at least two different people wear it");
                foreach (var v in people)
                {
                    var worn = CharacterVariants.Prefab(v.id).GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Uniform_")).ToArray();
                    Assert.AreEqual(u.pieces.Length, worn.Length, v.id + ": every prop of the " + job + " uniform is on the prefab");
                    foreach (var piece in worn)
                    {
                        Assert.NotNull(piece.GetComponent<MeshRenderer>().sharedMaterial, v.id + "/" + piece.name + ": material");
                        Assert.IsNull(piece.GetComponent<Collider>(), v.id + "/" + piece.name + ": props never block the player");
                        Assert.IsTrue(piece.IsChildOf(CharacterVariants.Prefab(v.id).GetComponentInChildren<Animator>(true).transform), piece.name);
                        Assert.AreNotEqual(CharacterVariants.Prefab(v.id).transform, piece.parent, v.id + "/" + piece.name + ": on a bone (follows the animation), not on the root");
                    }
                }
            }
            Assert.IsNotNull(File.Uniform("resident"), "everyday clothes for the town's people");
        }

        [Test]
        public void UniformPieces_AreWellFormed()
        {
            foreach (var u in File.uniforms)
            {
                var names = u.pieces.Select(p => p.name).ToArray();
                Assert.AreEqual(names.Length, names.Distinct().Count(), u.id + ": piece names are unique");
                foreach (var p in u.pieces)
                {
                    Assert.Contains(p.shape, new[] { "cube", "sphere", "cylinder" }, u.id + "/" + p.name);
                    Assert.Contains(p.anchor, Anchors, u.id + "/" + p.name);
                    Assert.IsTrue(IsHex(p.hex), u.id + "/" + p.name + ": colour");
                    Assert.AreEqual(3, p.pos.Length); Assert.AreEqual(3, p.rot.Length); Assert.AreEqual(3, p.size.Length);
                    Assert.IsTrue(p.size.All(s => s > 0.01f && s < 2.5f), u.id + "/" + p.name + ": size in head widths");
                }
            }
        }

        [Test]
        public void TheCast_IsDiverse()
        {
            var people = File.variants;
            Assert.GreaterOrEqual(people.Select(v => v.skinHex).Distinct().Count(), 6, "six or more skin tones");
            var tones = people.Select(v => v.skinHex).Distinct().Select(h => { ColorUtility.TryParseHtmlString(h, out var c); return c.grayscale; }).OrderBy(g => g).ToArray();
            Assert.Less(tones.First(), 0.3f, "a deep skin tone");
            Assert.Greater(tones.Last(), 0.75f, "a light skin tone");
            for (int i = 1; i < tones.Length; i++) Assert.Greater(tones[i] - tones[i - 1], 0.04f, "neighbouring skin tones are told apart");
            var boys = people.Count(v => IsBoy(v.kid));
            Assert.GreaterOrEqual(boys, 10, "men"); Assert.GreaterOrEqual(people.Length - boys, 10, "women");
            foreach (var group in new[] { "child", "teen", "adult", "senior" }) Assert.GreaterOrEqual(people.Count(v => v.ageGroup == group), 2, group);
            foreach (var build in new[] { "slim", "average", "sturdy" }) Assert.GreaterOrEqual(people.Count(v => v.build == build), 3, build);
            Assert.GreaterOrEqual(people.Select(v => v.hair).Distinct().Count(), 8, "hair colours");
            Assert.GreaterOrEqual(people.Select(v => v.person).Distinct().Count(), people.Length, "everyone has a different name");
            Assert.GreaterOrEqual(people.Select(v => v.height).Distinct().Count(), 10, "heights");
        }

        [Test]
        public void Tags_MatchTheNumbers()
        {
            foreach (var v in File.variants)
            {
                string group = v.age < 13 ? "child" : v.age < 20 ? "teen" : v.age < 60 ? "adult" : "senior";
                Assert.AreEqual(group, v.ageGroup, v.id + ": age group");
                string build = v.girth < 0.97f ? "slim" : v.girth > 1.07f ? "sturdy" : "average";
                Assert.AreEqual(build, v.build, v.id + ": build");
                Assert.IsTrue(IsHex(v.skinHex), v.id + ": skin colour");
                Assert.IsTrue(IsHex(v.TopOf(File)) && IsHex(v.BottomOf(File)), v.id + ": clothes colours (own or the uniform's)");
                Assert.IsTrue(v.hair == "natural" || CharacterLook.Options.Hair(v.hair) != null, v.id + ": hair " + v.hair);
                if (v.beard != "none")
                {
                    Assert.IsTrue(IsBoy(v.kid), v.id + ": beards on men's models only");
                    Assert.IsNull(CharacterLook.BeardBlocker(v.beard, v.age, true), v.id + ": old enough for " + v.beard);
                }
            }
        }

        [Test]
        public void Proportions_StaySafeForTheFootGrounderAndTheCounters()
        {
            foreach (var v in File.variants)
            {
                Assert.That(v.height, Is.InRange(0.8f, 1.2f), v.id + ": height");
                Assert.That(v.girth, Is.InRange(0.85f, 1.25f), v.id + ": girth");
                var scale = CharacterVariants.Prefab(v.id).transform.localScale;
                Assert.AreEqual(KidScale * v.girth, scale.x, 1e-3f, v.id + ": x"); Assert.AreEqual(KidScale * v.girth, scale.z, 1e-3f, v.id + ": z");
                Assert.AreEqual(KidScale * v.height, scale.y, 1e-3f, v.id + ": y");
                if (v.ageGroup == "adult" || v.ageGroup == "senior") Assert.GreaterOrEqual(v.height, 0.9f, v.id + ": grown-ups are not shorter than the kids' prefab by much");
                if (v.ageGroup == "child") Assert.Less(v.height, 0.9f, v.id + ": children are small");
            }
        }

        [Test]
        public void Palettes_CoverEveryKid_AndTheirBlocksAreTellApart()
        {
            Assert.AreEqual(8, LookPaletteCatalog.All.models.Length);
            foreach (var kid in Kids)
            {
                var p = LookPaletteCatalog.ForKid(kid);
                Assert.NotNull(p, kid);
                Assert.NotNull(CharacterLook.Models.models.FirstOrDefault(m => m.model == p.model), kid + ": a look model");
                Assert.IsTrue(IsHex(p.skinFrom) && IsHex(p.topFrom), kid + ": skin / top colours measured");
                ColorUtility.TryParseHtmlString(p.skinFrom, out var skin); ColorUtility.TryParseHtmlString(p.topFrom, out var top);
                Assert.Greater(Vector3.Distance(new Vector3(skin.r, skin.g, skin.b), new Vector3(top.r, top.g, top.b)), 0.1f, kid + ": skin and top differ");
                if (!string.IsNullOrEmpty(p.bottomFrom))
                {
                    Assert.IsTrue(IsHex(p.bottomFrom), kid);
                    ColorUtility.TryParseHtmlString(p.bottomFrom, out var bottom);
                    Assert.Greater(Vector3.Distance(new Vector3(bottom.r, bottom.g, bottom.b), new Vector3(top.r, top.g, top.b)), 0.1f, kid + ": top and bottom differ");
                }
            }
            Assert.NotNull(Resources.Load<Material>("Characters/Variants/NL_PaletteRecolor"), "the recolour material");
        }

        [Test]
        public void EveryLabel_SpeaksAllThreeLanguages_AndNamesAreOriginal()
        {
            var before = Loc.Language;
            try
            {
                foreach (var u in File.uniforms)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(u.nameVi) || string.IsNullOrWhiteSpace(u.nameEn) || string.IsNullOrWhiteSpace(u.nameJa), u.id + ": a name in vi, en and ja");
                    Assert.IsTrue(Loc.IsJapaneseText(u.nameJa), u.id + ": the Japanese name is Japanese");
                    Assert.IsFalse(Loc.IsJapaneseText(u.nameVi) || Loc.IsJapaneseText(u.nameEn), u.id + ": vi / en names carry no Japanese text");
                    foreach (var language in new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese })
                    {
                        Loc.Set(language);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(u.Name), u.id + " in " + language);
                    }
                    Loc.Set(GameLanguage.Vietnamese); string vi = u.Name; Loc.Set(GameLanguage.English); string en = u.Name; Loc.Set(GameLanguage.Japanese); string ja = u.Name;
                    Assert.AreEqual(3, new[] { vi, en, ja }.Distinct().Count(), u.id + ": three different labels, one per language");
                }
            }
            finally { Loc.Set(before); }
            string[] realBrands = { "phuc long", "phúc long", "katinat", "trung nguyen", "trung nguyên", "highlands", "starbucks", "the coffee house", "cong ca phe", "cộng cà phê", "jr ", "mcdonald", "kfc" };
            var text = string.Join(" | ", File.uniforms.SelectMany(u => new[] { u.id, u.nameVi, u.nameEn, u.nameJa }).Concat(File.variants.Select(v => v.person + " " + v.id))).ToLowerInvariant();
            foreach (var brand in realBrands) Assert.IsFalse(text.Contains(brand), "no real trademarks in the cast: " + brand);
        }
    }
}
