using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam;
using NihongoLife.Exam.Ielts;
using NihongoLife.Notebook;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.Shop;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The pocket notebook (limited pages, paper from Hibari Mart, saved with the progress) and exam highlighting
    /// (pen / colours / eraser driven by real pointer events on the text, persisted with the IELTS attempt, copied
    /// into the notebook). Notebook and JLPT captures: Bao_Cao/notebook-regression. Captures that show Cambridge
    /// text stay local: LocalContent/IELTS/_captures.
    /// </summary>
    public class NotebookHighlightPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/notebook-regression"));

        [UnityTest]
        public IEnumerator Notebook_PagesPaperAndSave()
        {
            yield return LoadCity();
            string original = JsonUtility.ToJson(NotebookService.Record);
            try
            {
                var record = NotebookService.Record;
                record.pages = NotebookService.StartPages;
                record.entries.Clear();
                record.currentPage = 0;
                NotebookService.Save();

                NotebookUI.Toggle();
                yield return null; yield return null;
                Assert.IsTrue(NotebookUI.IsOpen);
                var ui = NotebookUI.Instance;
                Assert.AreEqual("Notebook", UiModalStack.TopName, "Esc closes the notebook first");

                ui.TitleField.text = "Từ mới N5 · ở konbini";
                ui.BodyField.text = "おにぎり – cơm nắm\nいらっしゃいませ – xin mời vào\nふくろ は いりますか – có cần túi không?\nIELTS: 'in either order' = đáp án nào trước cũng được";
                yield return null;
                Assert.AreEqual("Từ mới N5 · ở konbini", NotebookService.Page(0).title);
                StringAssert.Contains("ふくろ", NotebookService.Page(0).body);
                Assert.AreEqual(1, NotebookService.UsedPages);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Capture("01_notebook_page");

                // A page holds at most PageChars characters.
                Click(ui.Window, "Next");
                yield return null;
                Assert.AreEqual(1, NotebookService.CurrentPage);
                ui.BodyField.text = new string('あ', NotebookService.PageChars + 150);
                yield return null;
                Assert.LessOrEqual(NotebookService.Page(1).body.Length, NotebookService.PageChars);

                // Write on every sheet: the notebook says it is out of paper.
                for (int i = 2; i < NotebookService.Pages; i++) NotebookService.Write(i, $"Trang {i + 1}", "Ghi chú " + i);
                NotebookService.SetCurrentPage(NotebookService.Pages - 1);
                yield return null; yield return null;
                Assert.AreEqual(NotebookService.Pages, NotebookService.UsedPages);
                var hint = ui.Window.GetComponentsInChildren<TextMeshProUGUI>(false).FirstOrDefault(t => t.text.Contains("Hết giấy"));
                Assert.NotNull(hint, "Full notebook points to Hibari Mart");
                yield return Capture("02_notebook_full");

                // Erasing needs two clicks and frees the page.
                Click(ui.Window, "Erase");
                yield return null;
                Assert.AreEqual(NotebookService.Pages, NotebookService.UsedPages, "First click only arms the eraser");
                Click(ui.Window, "Erase");
                yield return null;
                Assert.AreEqual(NotebookService.Pages - 1, NotebookService.UsedPages);

                // Closing saves into the progress.
                NotebookUI.Toggle();
                yield return null;
                Assert.IsFalse(NotebookUI.IsOpen);
                Assert.IsTrue(GameServices.TryGet(out IProgressRepository repository));
                Assert.AreEqual("Từ mới N5 · ở konbini", repository.GetProgress().notebook.entries[0].title, "Saved with the progress");

                // More paper from the stationery shelf of Hibari Mart.
                var inventory = PlayerInventory.Instance;
                inventory.AddYen(1000);
                int yen = inventory.Yen, pages = NotebookService.Pages;
                var shop = KonbiniShopUI.GetOrCreate();
                shop.OpenSection(KonbiniSection.Stationery);
                yield return null;
                var paper = KonbiniCatalog.Find("note_paper_12");
                shop.Select(paper);
                yield return new WaitForSecondsRealtime(0.2f);
                yield return Capture("03_stationery_shelf");
                shop.AddSelectedToBasket();
                Assert.IsTrue(shop.Pay());
                Assert.AreEqual(pages + 12, NotebookService.Pages, "Paper becomes notebook pages");
                Assert.AreEqual(yen - paper.price, inventory.Yen);
                Assert.IsFalse(inventory.HasItem("note_paper_12"), "Paper never goes into the bag");
                yield return new WaitForSecondsRealtime(0.3f);
                UiModalStack.CloseTop(); // the clerk's thank-you
                yield return null;
                NotebookUI.Toggle();
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Capture("04_notebook_more_paper");
                NotebookUI.Toggle();
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(original, NotebookService.Record);
                NotebookService.Save();
            }
        }

        [UnityTest]
        public IEnumerator Highlight_IeltsPassage_PenEraserPersistAndCopy()
        {
            const string id = "cambridge14_test1_reading";
            var package = IeltsLibrary.Discover().FirstOrDefault(p => p.Id == id);
            if (package == null) { Assert.Ignore("Local package not present on this machine."); yield break; }
            yield return LoadCity();
            string original = JsonUtility.ToJson(NotebookService.Record);
            try
            {
                IeltsAttemptStore.ClearAttempt(id);
                var ui = IeltsTestUI.Open(package, IeltsTestUI.PracticeMode, false);
                yield return null; yield return null;
                var paragraph = Paragraph(ui);
                Assert.NotNull(paragraph, "Passage paragraphs carry a highlighter");
                Assert.IsFalse(paragraph.raycastTarget, "Pen off: the text does not catch the pointer (scrolling works as before)");

                // Pen: drag from the 3rd to the 6th word of the first paragraph.
                HighlightTool.SetColor(0);
                Assert.IsTrue(paragraph.raycastTarget);
                yield return Drag(paragraph, WordChar(paragraph, 2), WordChar(paragraph, 5));
                var book = paragraph.GetComponent<TextHighlighter>().Book;
                Assert.AreEqual(1, book.Count(), "One stroke = one highlight");
                StringAssert.Contains("<mark=", paragraph.text);
                string snippet = book.Snippets().Single();
                Assert.AreEqual(4, snippet.Split(' ').Length, $"Whole words 3–6 are highlighted: '{snippet}'");

                // A second colour by clicking a single word further on.
                HighlightTool.SetColor(2);
                yield return Drag(paragraph, WordChar(paragraph, 12), WordChar(paragraph, 12));
                Assert.AreEqual(2, book.Count());
                yield return new WaitForSecondsRealtime(0.2f);
                CaptureLocal("h01_ielts_highlight");

                // Survives switching parts and leaving / resuming the attempt.
                ui.ShowPart(1); yield return null;
                ui.ShowPart(0); yield return null; yield return null;
                StringAssert.Contains("<mark=", Paragraph(ui).text, "Re-rendered part keeps its highlights");
                ui.Close();
                yield return null;
                ui = IeltsTestUI.Open(package, IeltsTestUI.PracticeMode, true);
                yield return null; yield return null;
                paragraph = Paragraph(ui);
                StringAssert.Contains("<mark=", paragraph.text, "Highlights are saved with the attempt");
                book = paragraph.GetComponent<TextHighlighter>().Book;
                Assert.AreEqual(2, book.Count());

                // → Sổ copies the highlighted passages into the notebook, which opens above the exam.
                NotebookService.Record.currentPage = 0;
                NotebookService.Record.entries[0].title = NotebookService.Record.entries[0].body = string.Empty;
                var copy = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "CopyToNotebook" && Under(b.transform, "IeltsRoot"));
                copy.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                StringAssert.Contains(snippet, NotebookService.Page(0).body);
                Assert.IsTrue(NotebookUI.IsOpen);
                CaptureLocal("h02_notebook_over_exam");
                NotebookUI.Toggle();

                // Eraser: one click on a highlighted word removes that highlight.
                HighlightTool.SetMode(HighlightTool.ToolMode.Eraser);
                yield return Drag(paragraph, WordChar(paragraph, 3), WordChar(paragraph, 3));
                Assert.AreEqual(1, book.Count());
                HighlightTool.SetMode(HighlightTool.ToolMode.Off);
                Assert.IsFalse(paragraph.raycastTarget);
                ui.Close();
                IeltsAttemptStore.ClearAttempt(id);
            }
            finally
            {
                HighlightTool.SetMode(HighlightTool.ToolMode.Off);
                JsonUtility.FromJsonOverwrite(original, NotebookService.Record);
                NotebookService.Save();
            }
        }

        [UnityTest]
        public IEnumerator Highlight_JlptQuestion_JapaneseCharacters()
        {
            yield return LoadCity();
            var exam = Resources.Load<ExamDefinition>("Exams/jlpt_n5_mock_2");
            var manager = ExamManager.Instance;
            manager.StartAttempt(exam);
            var ui = ExamPlayUI.GetOrCreate(NLUi.CreateCanvas("ExamHostCanvas", 400).transform);
            ui.Show();
            yield return new WaitForSecondsRealtime(1.5f);
            var prompt = ui.GetComponentsInChildren<TextHighlighter>(true).Select(h => h.GetComponent<TextMeshProUGUI>()).First(t => t.transform.parent.name == "QuestionPanel");
            HighlightTool.SetColor(1);
            prompt.ForceMeshUpdate();
            int first = Enumerable.Range(0, prompt.textInfo.characterCount).First(i => prompt.textInfo.characterInfo[i].character >= 0x3040);
            yield return Drag(prompt, first, first + 2);
            var book = prompt.GetComponent<TextHighlighter>().Book;
            Assert.AreEqual(3, book.Snippets().Last().Length, "Japanese text: exactly the characters dragged over (no word snapping)");
            yield return Capture("05_jlpt_highlight");
            HighlightTool.SetMode(HighlightTool.ToolMode.Off);
            book.Clear();
            ui.Hide();
        }

        // ─────────── Helpers ───────────

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || PlayerInventory.Instance == null || t < 2f); t += Time.unscaledDeltaTime) yield return null;
        }

        private static TextMeshProUGUI Paragraph(IeltsTestUI ui) =>
            Object.FindObjectsByType<TextHighlighter>(FindObjectsSortMode.None).Select(h => h.GetComponent<TextMeshProUGUI>())
              .Where(t => t.isActiveAndEnabled && Under(t.transform, "PassageScroll") && TextHighlighter.StripTags(t.text).Length > 200)
              .OrderBy(t => t.transform.GetSiblingIndex()).FirstOrDefault();

        private static bool Under(Transform t, string name)
        {
            for (; t != null; t = t.parent) if (t.name == name) return true;
            return false;
        }

        private static int WordChar(TMP_Text text, int word)
        {
            text.ForceMeshUpdate();
            var info = text.textInfo;
            return info.wordInfo[Mathf.Clamp(word, 0, info.wordCount - 1)].firstCharacterIndex + 1;
        }

        private static Vector2 Screen(TMP_Text text, int charIndex)
        {
            var c = text.textInfo.characterInfo[charIndex];
            var local = (c.bottomLeft + c.topRight) * 0.5f;
            return RectTransformUtility.WorldToScreenPoint(null, text.transform.TransformPoint(local));
        }

        /// <summary>Real pointer events on the text: down, (drag), up.</summary>
        private static IEnumerator Drag(TMP_Text text, int fromChar, int toChar)
        {
            text.ForceMeshUpdate();
            var from = Screen(text, fromChar);
            var to = Screen(text, toChar);
            var target = text.gameObject;
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = from, pressPosition = from };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            if (fromChar != toChar)
            {
                ExecuteEvents.Execute(target, data, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(target, data, ExecuteEvents.beginDragHandler);
                data.position = Vector2.Lerp(from, to, 0.5f);
                ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler);
                yield return null;
                data.position = to;
                ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler);
            }
            data.position = to;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            yield return null;
        }

        private static void Click(RectTransform root, string name)
        {
            var button = root.GetComponentsInChildren<Button>(false).FirstOrDefault(b => b.name == name);
            Assert.NotNull(button, $"Button {name}");
            button.onClick.Invoke();
        }

        private static IEnumerator Capture(string name)
        {
            yield return null;
            Shoot(Folder, name);
        }

        private static void CaptureLocal(string name) => Shoot(Path.Combine(IeltsLibrary.Root, "_captures"), name);

        private static void Shoot(string folder, string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            Directory.CreateDirectory(folder);
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(width, height, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".jpg"), tex.EncodeToJPG(88));
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }
    }
}
