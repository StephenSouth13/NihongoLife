using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The look system on the real Kids-pack models: hair recoloured through the baked mask, beards on the head bone
    /// (boys, from their minimum age), wrinkles and slower walking / speaking with age, and the look panel on the
    /// player in town. Captures: Bao_Cao/character-look.
    /// </summary>
    public class CharacterLookPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-look"));
        private static readonly string[] Kids = { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };

        [UnityTest]
        public IEnumerator Look_AllKids_HairBeardAndWrinkles()
        {
            Assert.AreEqual(8, CharacterLook.Models.models.Length, "looks.json (CharacterLookBuilder) has every kid");
            var stage = new GameObject("LookStage");
            stage.transform.position = new Vector3(0f, -300f, 0f);
            try
            {
                var light = new GameObject("Key", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f;
                light.transform.SetParent(stage.transform, false);
                light.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
                var models = new GameObject[Kids.Length];
                for (int i = 0; i < Kids.Length; i++)
                {
                    var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + Kids[i]);
                    models[i] = Object.Instantiate(prefab, stage.transform);
                    models[i].transform.localPosition = new Vector3((i - 3.5f) * 0.95f, 0f, 0f);
                    models[i].transform.localRotation = Quaternion.identity;
                }
                yield return null;

                // Young, natural: no beard, no lines, the original material.
                foreach (var m in models) CharacterLook.Apply(m, "natural", "full", 16, true);
                yield return null;
                foreach (var m in models) Assert.IsNull(Face(m), m.name + ": no beard at 16");
                Capture(stage, "01_kids_16_natural");

                // 70 with black hair dyed and a full beard: boys get the beard, everyone the wrinkles and the new colour.
                foreach (var m in models) CharacterLook.Apply(m, "black", "full", 70, false);
                yield return null;
                for (int i = 0; i < models.Length; i++)
                {
                    var model = CharacterLook.ModelOf(models[i]);
                    Assert.IsNotNull(model, Kids[i] + " is known");
                    var renderer = models[i].GetComponentInChildren<SkinnedMeshRenderer>();
                    Assert.IsTrue(renderer.sharedMaterial.GetTexture("_Texture2D") is RenderTexture, Kids[i] + ": recoloured hair");
                    var face = Face(models[i]);
                    Assert.IsNotNull(face, Kids[i] + ": wrinkles at 70");
                    int beardPieces = CharacterLook.Options.Beard("full").pieces.Length;
                    int wrinklePieces = CharacterLook.Options.wrinkles.Sum(w => w.pieces.Length);
                    Assert.AreEqual(model.boy ? beardPieces + wrinklePieces : wrinklePieces, face.childCount, Kids[i] + (model.boy ? ": beard + lines" : ": lines only (no beard on girls)"));
                }
                Capture(stage, "02_kids_70_black_full");

                foreach (var m in models) CharacterLook.Apply(m, "natural", "long", 80, true);
                yield return null;
                Capture(stage, "03_kids_80_grey_long");
                foreach (var m in models) CharacterLook.Apply(m, "blonde", "goatee", 30, true);
                yield return null;
                Capture(stage, "04_kids_30_blonde_goatee");

                // Walking and speaking slow down with age.
                Assert.AreEqual(1f, CharacterLook.WalkFactor(30), 0.001f);
                Assert.Less(CharacterLook.WalkFactor(80), 0.85f);
                Assert.Less(CharacterLook.TalkFactor(80), CharacterLook.WalkFactor(80));
            }
            finally
            {
                Object.Destroy(stage);
            }
        }

        [UnityTest]
        public IEnumerator Look_PanelOnThePlayer_BeardOpensWithAge()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            CharacterLook.ResetForTests();
            try
            {
                var player = Object.FindAnyObjectByType<PlayerController>();
                Assert.IsNotNull(player);
                var look = player.GetComponent<CharacterLook>();
                Assert.IsNotNull(look, "The player wears the look");
                Assert.IsTrue(look.IsBoy, "Haruto is the default character");

                CharacterLook.AgeOverride = 16;
                CharacterLookUI.Open();
                yield return null;
                Assert.IsTrue(CharacterLookUI.IsOpen);
                Assert.IsFalse(Button("Beard_full").interactable, "No beard at 16");
                Assert.IsNotNull(CharacterLook.SetBeard("full", true), "Refused at 16");
                Button("Hair_navy").onClick.Invoke();
                yield return null;
                Assert.AreEqual("navy", CharacterLook.State.hair);
                Capture(null, "05_panel_16");

                CharacterLook.AgeOverride = 30;
                CharacterLookUI.Open();
                yield return null;
                Button("Beard_full").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.AreEqual("full", CharacterLook.State.beard);
                Assert.IsNotNull(Face(player.transform.Find("Visual").gameObject), "The beard is on the player's head");
                Capture(null, "06_panel_30_beard");

                // At 80 the player walks slower, and an elder NPC speaks slower.
                CharacterLook.AgeOverride = 80;
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.Less(CharacterLook.PlayerWalkFactor, 0.85f);
                CharacterLookUI.Close();
            }
            finally
            {
                CharacterLook.AgeOverride = null;
                CharacterLook.ResetForTests();
                CharacterLookUI.Close();
            }
        }

        private static Transform Face(GameObject model)
        {
            var animator = model.GetComponentInChildren<Animator>();
            var head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head == null) return null;
            foreach (Transform child in head) if (child.name == CharacterLook.BeardName && child.gameObject.activeSelf) return child;
            return null;
        }

        private static Button Button(string name) =>
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name);

        /// <summary>Front close-up of the stage's heads, or the main camera with the UI.</summary>
        private static void Capture(GameObject stage, string name)
        {
            const int width = 1600, height = 900;
            Camera camera;
            GameObject temp = null;
            Canvas[] canvases = System.Array.Empty<Canvas>();
            if (stage != null)
            {
                temp = new GameObject("LookCamera", typeof(Camera));
                camera = temp.GetComponent<Camera>();
                camera.transform.position = stage.transform.position + new Vector3(0f, 0.95f, 8.4f);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.05f, -1f));
                camera.fieldOfView = 30f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.55f, 0.66f, 0.74f);
            }
            else
            {
                camera = Camera.main;
                if (camera == null) return;
                canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.ForceUpdateCanvases(); // back to screen size now, or HudFitRect fits the next frame to the camera-mode size
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
            if (temp != null) Object.Destroy(temp);
        }
    }
}
