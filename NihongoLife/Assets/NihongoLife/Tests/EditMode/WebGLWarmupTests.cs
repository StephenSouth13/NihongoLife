using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// P0.b: the shader variants the WebGL loader warms up. <c>RecordedShaderVariants</c> is recorded in play by
    /// WebGLShaderRecordingPlayModeTests (NIHONGO_RECORD_SHADERS=1); this validates the file and that the loader still reads it.
    /// </summary>
    public class WebGLWarmupTests
    {
        private static readonly string[] Kids = { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };

        private static (HashSet<Object> shaders, int variants, int keyworded) Read(ShaderVariantCollection collection)
        {
            var shaders = new HashSet<Object>();
            int variants = 0, keyworded = 0;
            var list = new SerializedObject(collection).FindProperty("m_Shaders");
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                shaders.Add(entry.FindPropertyRelative("first").objectReferenceValue);
                var items = entry.FindPropertyRelative("second.variants");
                for (int v = 0; v < items.arraySize; v++)
                {
                    variants++;
                    if (!string.IsNullOrWhiteSpace(items.GetArrayElementAtIndex(v).FindPropertyRelative("keywords").stringValue)) keyworded++;
                }
            }
            return (shaders, variants, keyworded);
        }

        [Test]
        public void RecordedVariants_ExistAndCarryRealKeywords()
        {
            var collection = Resources.Load<ShaderVariantCollection>("WebGL/RecordedShaderVariants");
            Assert.IsNotNull(collection, "Resources/WebGL/RecordedShaderVariants (re-record: NIHONGO_RECORD_SHADERS=1, WebGLShaderRecordingPlayModeTests)");
            var (shaders, variants, keyworded) = Read(collection);
            Assert.GreaterOrEqual(variants, 15, "the recording saw the town, the characters and the zones");
            Assert.GreaterOrEqual(keyworded, 2, "it holds variants WITH keywords (fog, SSAO) - the keyword-less base list misses exactly these");
            Assert.IsFalse(shaders.Contains(null), "no shader reference is missing");
        }

        [Test]
        public void RecordedVariants_ListEveryCharacterShader()
        {
            var collection = Resources.Load<ShaderVariantCollection>("WebGL/RecordedShaderVariants");
            Assert.IsNotNull(collection);
            var (shaders, _, _) = Read(collection);
            foreach (var id in Kids)
            {
                var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + id);
                foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        Assert.IsTrue(shaders.Contains(material.shader), id + ": the recording lists " + material.shader.name);
            }
        }

        [Test]
        public void Loader_WarmsBothCollections_Progressively()
        {
            string source = File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "NihongoLife/Scripts/Core/SceneContentLoader.cs")));
            StringAssert.Contains("WebGL/RecordedShaderVariants", source);
            StringAssert.Contains("WebGL/WebGLShaderVariants", source);
            StringAssert.Contains("WarmUpProgressively", source, "a whole-collection WarmUp() in one frame froze the page");
            Assert.IsNotNull(Resources.Load<ShaderVariantCollection>("WebGL/WebGLShaderVariants"));
        }
    }
}