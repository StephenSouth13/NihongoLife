#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Contact sheets of Kids animation clips (side view on top, three-quarter view below, five moments) to choose and
    /// check clips for NL_Humanoid. Renders in Play Mode, where humanoid clips evaluate on the Animator. Clip list from
    /// the environment variable NL_SHEET_CLIPS (comma-separated, "boy_" prefix and ".anim" left out; a name ending in
    /// ".anim" or containing "/" is a full asset path). Output: Bao_Cao/anim-candidates/&lt;clip&gt;.png
    /// </summary>
    public class AnimationCandidateSheetTests
    {
        private const string Root = "Assets/ThirdParty/New Chac/ForSurvival/KidsCharacters/";

        [UnityTest, Timeout(600000)]
        public IEnumerator Render_ContactSheets()
        {
            string list = System.Environment.GetEnvironmentVariable("NL_SHEET_CLIPS");
            if (string.IsNullOrEmpty(list)) Assert.Ignore("Set NL_SHEET_CLIPS to render contact sheets.");
            LogAssert.ignoreFailingMessages = true; // Kids clips carry gameplay animation events with no receiver here
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/anim-candidates"));
            Directory.CreateDirectory(outDir);

            var scene = SceneManager.CreateScene("ContactSheet");
            SceneManager.SetActiveScene(scene);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = Vector3.one * 2f;
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.82f, 0.86f, 0.9f);
            cam.fieldOfView = 30f;

            var kid = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Fbx/Humanoid/Boy0_Humanoid.fbx"));
            var animator = kid.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            const int cell = 240;
            var rt = new RenderTexture(cell, cell * 2, 24);
            foreach (string raw in list.Split(','))
            {
                string name = raw.Trim();
                string path = name.Contains("/") ? name : Root + "AnimationClips/boy_Humanoid/boy_" + name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) { foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is AnimationClip c && !c.name.StartsWith("__preview")) clip = c; }
                if (clip == null) { Debug.LogWarning("[Sheet] missing " + name); continue; }
                var graph = PlayableGraph.Create("sheet");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "out", animator);
                var play = AnimationClipPlayable.Create(graph, clip);
                play.SetApplyFootIK(true);
                output.SetSourcePlayable(play);
                graph.Play();
                const int frames = 5;
                var sheet = new Texture2D(cell * frames, cell * 2, TextureFormat.RGB24, false);
                for (int f = 0; f < frames; f++)
                {
                    play.SetTime(clip.length * f / (frames - 1) * 0.999f);
                    graph.Evaluate(0f);
                    kid.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    yield return null;
                    for (int v = 0; v < 2; v++)
                    {
                        cam.transform.position = v == 0 ? new Vector3(3.0f, 0.7f, 0f) : new Vector3(1.9f, 1.1f, 2.2f);
                        cam.transform.LookAt(new Vector3(0f, 0.55f, 0f));
                        cam.targetTexture = rt;
                        cam.rect = new Rect(0f, v == 0 ? 0.5f : 0f, 1f, 0.5f);
                        cam.Render();
                    }
                    RenderTexture.active = rt;
                    sheet.ReadPixels(new Rect(0, 0, cell, cell * 2), cell * f, 0);
                    RenderTexture.active = null;
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(outDir, Path.GetFileNameWithoutExtension(name.Replace('/', '_')) + ".png"), sheet.EncodeToPNG());
                Object.Destroy(sheet);
                graph.Destroy();
                Debug.Log($"[Sheet] {name} {clip.length:0.00}s loop={clip.isLooping}");
            }
            cam.targetTexture = null;
            rt.Release();
        }
    }
}
#endif
