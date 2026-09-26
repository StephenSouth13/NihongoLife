using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.UI;

namespace NihongoLife.Editor.Exam
{
    public class Exam3DBuilder : EditorWindow
    {
        [MenuItem("NihongoLife/Exam/Create 3D Exam Desk (Diegetic UI)")]
        public static void Create3DExamDesk()
        {
            // 1. Create Root
            GameObject root = new GameObject("ExamDesk_3D");
            root.transform.position = Vector3.zero;

            // 2. Create the physical desk (Placeholder)
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "PhysicalDesk";
            desk.transform.SetParent(root.transform);
            desk.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            desk.transform.localScale = new Vector3(1.5f, 0.05f, 0.8f);
            
            Material deskMat = new Material(Shader.Find("Standard"));
            deskMat.color = new Color(0.2f, 0.2f, 0.2f); // Dark grey desk
            desk.GetComponent<MeshRenderer>().sharedMaterial = deskMat;

            // 3. Create the 3D Screen / Paper
            GameObject screenBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenBase.name = "ExamScreenMonitor";
            screenBase.transform.SetParent(root.transform);
            screenBase.transform.localPosition = new Vector3(0f, 0.95f, 0.2f);
            screenBase.transform.localScale = new Vector3(0.8f, 0.45f, 0.02f);
            screenBase.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);

            // 4. Create the World Space Canvas
            GameObject canvasGo = new GameObject("ExamCanvas3D");
            canvasGo.transform.SetParent(screenBase.transform);
            
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            
            // Adjust the RectTransform to fit perfectly on the monitor
            RectTransform rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1300f, 800f); // Match ExamPlayUI CardSize
            
            // Scale it down so 1300 units fits in 0.8 meters
            // 0.8 / 1300 = 0.000615
            float scale = 0.8f / 1300f;
            rect.localScale = new Vector3(scale, scale, scale);
            
            // Position it exactly on the front face of the monitor
            rect.localPosition = new Vector3(0f, 0f, -0.51f); 
            rect.localRotation = Quaternion.identity;

            // 5. Attach the ExamPlayUI script
            // Since ExamPlayUI inherits from MenuPopupBase, it will auto-build its UI children here!
            ExamPlayUI examUI = canvasGo.AddComponent<ExamPlayUI>();

            // 6. Add an Interaction Trigger (so player can sit down to start the exam)
            GameObject trigger = new GameObject("ExamTrigger");
            trigger.transform.SetParent(root.transform);
            trigger.transform.localPosition = new Vector3(0f, 1f, -0.5f);
            BoxCollider col = trigger.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1f, 2f, 1f);
            
            // Add a mock component to let the player know they can interact
            // (Assumes you have an InteractiveItem or similar script, adding dummy tag for now)
            trigger.tag = "Interactable";

            // 7. Auto Focus
            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            
            Debug.Log("[Exam3DBuilder] Successfully generated 3D Exam Desk with Diegetic World Space Canvas! " + 
                      "You can now place this anywhere in your scenes.");
        }
    }
}
