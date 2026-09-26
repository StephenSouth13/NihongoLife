using System.IO;
using UnityEditor;
using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Editor.Scenario
{
    public class ScenarioImporterTool : EditorWindow
    {
        private TextAsset jsonFile;
        private string outputFolder = "Assets/NihongoLife/Resources/Scenarios";

        [MenuItem("NihongoLife/Scenario/Import Scenario from JSON")]
        public static void ShowWindow()
        {
            GetWindow<ScenarioImporterTool>("Scenario Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Import Scenario JSON to ScriptableObject", EditorStyles.boldLabel);
            
            jsonFile = (TextAsset)EditorGUILayout.ObjectField("Scenario JSON File", jsonFile, typeof(TextAsset), false);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate Scenario Asset", GUILayout.Height(40)))
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
                "Sử dụng AI (ChatGPT/Claude) để viết kịch bản hội thoại dựa trên sườn JSON mẫu. " +
                "Công cụ này sẽ tự động parse JSON và tạo ra ScenarioDefinition Asset chứa đầy đủ " +
                "Nodes, Choices, và Điểm thưởng (Score Modifiers).", 
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
            ScenarioDefinition scenarioAsset = ScriptableObject.CreateInstance<ScenarioDefinition>();
            
            try
            {
                JsonUtility.FromJsonOverwrite(jsonText, scenarioAsset);
                
                // If ID is missing, auto-generate it
                if (string.IsNullOrEmpty(scenarioAsset.id))
                {
                    scenarioAsset.id = fileName;
                }

                string assetPath = $"{outputFolder}/{scenarioAsset.id}.asset";
                
                // Check if it already exists
                ScenarioDefinition existingAsset = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(assetPath);
                if (existingAsset != null)
                {
                    EditorUtility.CopySerialized(scenarioAsset, existingAsset);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[ScenarioImporter] Updated existing scenario asset: {assetPath}");
                }
                else
                {
                    AssetDatabase.CreateAsset(scenarioAsset, assetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[ScenarioImporter] Created new scenario asset: {assetPath}");
                }

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(assetPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScenarioImporter] Failed to parse JSON. Ensure it matches ScenarioDefinition structure. Error: {e.Message}");
                EditorUtility.DisplayDialog("Parse Error", "Failed to parse JSON. Check console for details.", "OK");
            }
        }
    }
}
