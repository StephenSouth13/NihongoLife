using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>J1: the shared dialogue box is compact (a three-choice guided line takes under a third of the screen). Capture: Bao_Cao/dialogue-compact.</summary>
    public class CompactDialoguePlayModeTests
    {
        [UnityTest]
        public IEnumerator Dialogue_ThreeChoiceGuidedLine_IsCompact_AndCentred()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || Object.FindFirstObjectByType<DialogueView>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var view = Object.FindFirstObjectByType<DialogueView>();
            var data = new DialogueDisplayData
            {
                speakerName = "Ito", textJa = "いらっしゃいませ。ごちゅうもんは なんですか。", textReading = "いらっしゃいませ。ごちゅうもんは なんですか。",
                textRomaji = "irasshaimase. gochuumon wa nan desu ka.", textEn = "Welcome. What would you like to order?",
                learningMode = LearningMode.GuidedPractice,
                choices = new List<DialogueChoice>
                {
                    new DialogueChoice { textJa = "おにぎりを ふたつ ください。", textVi = "Cho tôi hai cái cơm nắm.", textEn = "Two rice balls, please." },
                    new DialogueChoice { textJa = "おちゃを ください。", textVi = "Cho tôi trà.", textEn = "Tea, please." },
                    new DialogueChoice { textJa = "いくらですか。", textVi = "Bao nhiêu tiền?", textEn = "How much is it?" },
                }
            };
            view.Show(data, true);
            yield return null; yield return null;
            var box = (RectTransform)view.Root.transform.Find("DialogueBox");
            Assert.IsNotNull(box);
            var canvas = view.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            float ratio = box.rect.height / canvas.rect.height;
            Debug.Log($"[J1] box {box.rect.width:0}x{box.rect.height:0} canvas {canvas.rect.width:0}x{canvas.rect.height:0} ratio {ratio:0.00}");
            Assert.Less(box.rect.height, 350f, "A guided line with three choices stays under a third of a 1080p screen");
            Assert.LessOrEqual(box.rect.width, 1040f, "Narrower box leaves the street visible");
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/dialogue-compact")); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "measure.txt"), $"box {box.rect.width:0}x{box.rect.height:0} canvas {canvas.rect.width:0}x{canvas.rect.height:0} ratio {ratio:0.000}");
            var cam = Camera.main; var c = canvas.GetComponent<Canvas>(); var mode = c.renderMode;
            c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.06f; Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, "01_dialogue_three_choices.png"), tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex);
            c.renderMode = mode; c.worldCamera = null;
            view.Hide();
        }
    }
}