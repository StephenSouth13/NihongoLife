using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Renders contact sheets of Kids animation clips (side and three-quarter view, 5 moments each) so clips can be
    /// compared before they are wired into NL_Humanoid. Output: Bao_Cao/anim-candidates/&lt;clip&gt;.png.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.AnimationContactSheet.Render [-clips a,b,c]
    /// Works in an unsaved in-memory scene; nothing in the project changes.
    /// </summary>
    public static class AnimationContactSheet
    {
        private const string ClipFolder = KidsCharactersSetup.Root + "AnimationClips/boy_Humanoid/";
        private const string Model = KidsCharactersSetup.Root + "Fbx/Humanoid/Boy0_Humanoid.fbx";

        public static void Render()
        {
            try
            {
                string list = "action_release_trapped,action_release_sticky,action_cabinet_open,action_switch_l,action_switch_r,action_treat,action_treat_own," +
                              "action_item_drop,action_item_get,action_pocket,action_book,idle_flashlight,move_walk_flashlight,idle_crouch,idle_crouch_listen," +
                              "idle_crouch_lookaround,idle_crouch_treated,idle_hang,idle_tired,idle_listen,down_back,down_forward,faint_back,faint_forward,caught_down," +
                              "idle_crawl,action_slingshot_charge,idle_happy,idle_lookaround,move_run,move_walk,move_sneak,standup_crouch,action_psychic,idle0,idle1,idle2,idle3,idle4,idle5,caught1,caught2";
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-clips") list = args[i + 1];
                string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/anim-candidates"));
                Directory.CreateDirectory(outDir);

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var light = new GameObject("Light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.transform.localScale = Vector3.one * 2f;
                var cam = new GameObject("Cam").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.82f, 0.86f, 0.9f);
                cam.fieldOfView = 30f;

                var kid = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
                var animator = kid.GetComponent<Animator>() ?? kid.AddComponent<Animator>();
                animator.applyRootMotion = false;
                const int cell = 220;
                var rt = new RenderTexture(cell, cell * 2, 24);
                foreach (string name in list.Split(','))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + "boy_" + name.Trim() + ".anim");
                    if (clip == null) { Debug.LogWarning("[ContactSheet] missing " + name); continue; }
                    const int frames = 5;
                    var sheet = new Texture2D(cell * frames, cell * 2, TextureFormat.RGB24, false);
                    for (int f = 0; f < frames; f++)
                    {
                        float t = clip.length * f / (frames - 1);
                        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(kid, clip, t);
                        AnimationMode.EndSampling();
                        kid.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        for (int v = 0; v < 2; v++)
                        {
                            // v0 = side view (from +X), v1 = three-quarter front view.
                            Vector3 eye = v == 0 ? new Vector3(3.0f, 0.7f, 0f) : new Vector3(1.9f, 1.1f, 2.2f);
                            cam.transform.position = eye;
                            cam.transform.LookAt(new Vector3(0f, 0.55f, 0f));
                            cam.targetTexture = rt;
                            cam.rect = new Rect(0f, v == 0 ? 0.5f : 0f, 1f, 0.5f);
                            cam.Render();
                        }
                        RenderTexture.active = rt;
                        sheet.ReadPixels(new Rect(0, 0, cell, cell * 2), cell * f, 0);
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(outDir, name.Trim() + ".png"), sheet.EncodeToPNG());
                    Object.DestroyImmediate(sheet);
                    Debug.Log($"[ContactSheet] {name} {clip.length:0.00}s loop={clip.isLooping}");
                }
                RenderTexture.active = null;
                cam.targetTexture = null;
                rt.Release();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
