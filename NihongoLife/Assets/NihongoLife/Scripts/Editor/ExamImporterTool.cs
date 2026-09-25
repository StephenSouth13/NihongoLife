using System.IO;
using UnityEditor;
using UnityEngine;
using NihongoLife.Exam;

namespace NihongoLife.Editor.Exam
{
    public class ExamImporterTool : EditorWindow
    {
        private TextAsset jsonFile;
        private string outputFolder = "Assets/NihongoLife/Resources/Exams";

        [MenuItem("NihongoLife/Exam/Import Exam from JSON")]
        public static void ShowWindow()
        {
            GetWindow<ExamImporterTool>("Exam Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Import Exam JSON to ScriptableObject", EditorStyles.boldLabel);
            
            jsonFile = (TextAsset)EditorGUILayout.ObjectField("Exam JSON File", jsonFile, typeof(TextAsset), false);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate Exam Asset", GUILayout.Height(40)))
            {
                if (jsonFile == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a JSON file first.", "OK");
                    return;
                }

                ImportJson(jsonFile.text, jsonFile.name);
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "Để tính năng AI phân tích kiến thức xịn xò sau này (VD: Phân tích Mondai nào yếu), " +
                "hãy đảm bảo file JSON của bạn có chứa các `tags` trong từng câu hỏi (VD: 'Mondai_5_Usage', 'Kanji', 'Reading'). " +
                "Tool này tự động map chính xác dữ liệu JSON vào kiến trúc ExamDefinition của NihongoLife.", 
                MessageType.Info);
        }

        private void ImportJson(string jsonText, string fileName)
        {
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                string[] folders = outputFolder.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath += "/" + folders[i];
                }
            }

            // Create a new ScriptableObject instance
            ExamDefinition examAsset = ScriptableObject.CreateInstance<ExamDefinition>();
            
            // Use JsonUtility to overwrite the instance with JSON data.
            // IMPORTANT: The JSON keys must match the field names in ExamDefinition exactly!
            try
            {
                JsonUtility.FromJsonOverwrite(jsonText, examAsset);
                
                // If ID is missing, auto-generate it
                if (string.IsNullOrEmpty(examAsset.id))
                {
                    examAsset.id = fileName;
                }

                string assetPath = $"{outputFolder}/{examAsset.id}.asset";
                
                // Check if it already exists
                ExamDefinition existingAsset = AssetDatabase.LoadAssetAtPath<ExamDefinition>(assetPath);
                if (existingAsset != null)
                {
                    EditorUtility.CopySerialized(examAsset, existingAsset);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[ExamImporter] Updated existing exam asset: {assetPath}");
                }
                else
                {
                    AssetDatabase.CreateAsset(examAsset, assetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[ExamImporter] Created new exam asset: {assetPath}");
                }

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<ExamDefinition>(assetPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ExamImporter] Failed to parse JSON. Ensure it matches ExamDefinition structure. Error: {e.Message}");
                EditorUtility.DisplayDialog("Parse Error", "Failed to parse JSON. Check console for details.", "OK");
            }
        }
    }
}
