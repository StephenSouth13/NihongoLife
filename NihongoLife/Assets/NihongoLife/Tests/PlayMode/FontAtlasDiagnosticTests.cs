using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>World-space signs must keep every glyph after the dynamic font atlas has grown.</summary>
    public class FontAtlasDiagnosticTests
    {
        [UnityTest]
        public IEnumerator GameCenterSigns_KeepAllGlyphs()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.GameCenterScene, LoadSceneMode.Additive);
            yield return new WaitForSecondsRealtime(1.5f);
            var scene = SceneManager.GetSceneByName(WorldLocationCatalog.GameCenterScene);
            var signs = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TextMeshPro>(true)).ToArray();
            Assert.IsNotEmpty(signs);
            var report = new System.Text.StringBuilder();
            int missing = 0;
            foreach (var sign in signs)
            {
                sign.ForceMeshUpdate();
                var info = sign.textInfo;
                for (int i = 0; i < info.characterCount; i++)
                {
                    var c = info.characterInfo[i];
                    if (char.IsWhiteSpace(c.character)) continue;
                    bool ok = c.isVisible && c.textElement != null && c.textElement.textAsset != null;
                    if (ok && c.materialReferenceIndex > 0)
                    {
                        var sub = sign.GetComponentsInChildren<TMP_SubMesh>(true);
                        report.AppendLine($"{sign.name} '{c.character}' sub-mesh {c.materialReferenceIndex} asset={c.textElement.textAsset.name} atlasIdx={(c.textElement as TMP_Character)?.glyph?.atlasIndex} subMeshes={sub.Length} subRendererEnabled={string.Join(",", sub.Select(s => s.renderer != null && s.renderer.enabled))}");
                    }
                    if (!ok) { missing++; report.AppendLine($"{sign.name} '{c.character}' NOT VISIBLE"); }
                }
            }
            var font = signs[0].font;
            report.AppendLine($"font={font.name} atlasTextures={font.atlasTextures.Length} fallback={(TMP_Settings.fallbackFontAssets?.Count ?? 0)}");
            Debug.Log("[FontAtlasDiagnostic]\n" + report);
            Assert.AreEqual(0, missing, report.ToString());
        }
    }
}
