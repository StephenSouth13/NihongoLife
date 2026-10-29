using System.Collections.Generic;
using System.Linq;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Regression guard for "a character loads without its mesh / texture": every Kids-pack prefab the game spawns must
    /// keep a real mesh, a full bone list, supported materials and its atlas, and looks.json must point at files that
    /// exist. Also guards the WebGL shader warm-up (a collection that skips the character shader makes the first
    /// character on screen stall while the browser compiles it).
    /// </summary>
    public class CharacterIntegrityTests
    {
        private static readonly string[] Kids = { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };

        [TestCaseSource(nameof(Kids))]
        public void KidPrefab_HasMeshBonesMaterialsAndAtlas(string id)
        {
            var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + id);
            Assert.IsNotNull(prefab, id + ": prefab under Resources/Characters/Kids");
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.Greater(renderers.Length, 0, id + ": has a skinned mesh");
            var animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator, id + ": Animator");
            Assert.IsTrue(animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman, id + ": a valid Humanoid avatar (Mixamo clips retarget onto it)");
            foreach (var renderer in renderers)
            {
                string where = id + "/" + renderer.name;
                Assert.IsNotNull(renderer.sharedMesh, where + ": mesh");
                Assert.Greater(renderer.sharedMesh.vertexCount, 0, where + ": vertices");
                Assert.Greater(renderer.sharedMesh.bindposes.Length, 0, where + ": bind poses");
                Assert.AreEqual(renderer.sharedMesh.bindposes.Length, renderer.bones.Length, where + ": one bone per bind pose");
                Assert.IsTrue(renderer.bones.All(b => b != null), where + ": no missing bone");
                Assert.IsNotNull(renderer.rootBone, where + ": root bone");
                Assert.Greater(renderer.sharedMaterials.Length, 0, where + ": materials");
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.IsNotNull(material, where + ": material is not missing");
                    Assert.IsNotNull(material.shader, where + ": shader");
                    Assert.IsTrue(material.shader.isSupported, where + ": shader supported (" + material.shader.name + ")");
                    Assert.IsFalse(material.shader.name.Contains("InternalError"), where + ": not the pink error shader");
                    Assert.IsTrue(material.HasProperty("_Texture2D"), where + ": atlas slot");
                    Assert.IsNotNull(material.GetTexture("_Texture2D"), where + ": atlas texture assigned");
                }
            }
        }

        [Test]
        public void LooksJson_CoversEveryKid_AndItsFilesExist()
        {
            var models = CharacterLook.Models.models;
            Assert.AreEqual(8, models.Length, "looks.json has the 8 Kids-pack models");
            var used = new HashSet<string>();
            foreach (var id in Kids)
            {
                var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + id);
                var model = CharacterLook.ModelOf(prefab);
                Assert.IsNotNull(model, id + ": looks.json knows this prefab's atlas");
                Assert.IsTrue(used.Add(model.model), id + ": its own entry (" + model.model + ")");
                Assert.IsNotNull(Resources.Load<Texture2D>("Characters/Looks/" + model.mask), id + ": hair mask " + model.mask);
                Assert.AreEqual(3, model.forward.Length, id + ": face forward");
                Assert.Greater(model.faceWidth, 0f, id + ": face width");
            }
            Assert.IsNotNull(Resources.Load<Material>("Characters/Looks/NL_HairRecolor"), "hair recolour material");
            Assert.IsNotNull(Shader.Find("Hidden/NihongoLife/HairRecolor"), "hair recolour shader is built in");
        }

        [Test]
        public void WebGLShaderWarmup_ListsTheCharacterShaders()
        {
            var collection = Resources.Load<ShaderVariantCollection>("WebGL/WebGLShaderVariants");
            Assert.IsNotNull(collection, "Resources/WebGL/WebGLShaderVariants (rebuilt by every WebGL build)");
            Assert.Greater(collection.shaderCount, 0, "warm-up collection is not empty");
            var listed = new HashSet<Object>();
            var shaders = new SerializedObject(collection).FindProperty("m_Shaders");
            for (int i = 0; i < shaders.arraySize; i++) listed.Add(shaders.GetArrayElementAtIndex(i).FindPropertyRelative("first").objectReferenceValue);
            foreach (var id in Kids)
            {
                var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + id);
                foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        Assert.IsTrue(listed.Contains(material.shader), id + ": the warm-up collection lists " + material.shader.name);
            }
            Assert.IsTrue(listed.Contains(Shader.Find("Universal Render Pipeline/Lit")), "URP Lit (beards, props) is warmed");
        }
    }
}