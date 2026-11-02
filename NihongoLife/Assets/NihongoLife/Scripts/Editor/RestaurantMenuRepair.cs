using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Data;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// The menu assets were saved pointing at the old Prefab object of each model (fileID 100100000), not at its GameObject.
    /// Unity 6 returns something that is not a GameObject, so serving a dish threw and the table stayed in "preparing" for
    /// ever: nothing was served, F did nothing and the unpaid bill locked the exit. Each reference is pointed at the model's
    /// real root; the file's own GUIDs say which model, so nothing has to be chosen by hand.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.RestaurantMenuRepair.Run
    /// </summary>
    public static class RestaurantMenuRepair
    {
        private static readonly Regex Reference = new Regex(@"^\s*(plateModel|servedModel): \{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}", RegexOptions.Multiline);

        public static void Run()
        {
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:RestaurantMenuDefinition"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var menu = AssetDatabase.LoadAssetAtPath<RestaurantMenuDefinition>(path);
                    var found = Reference.Matches(File.ReadAllText(path)).Cast<Match>().ToList();
                    var served = found.Where(m => m.Groups[1].Value == "servedModel").ToList();
                    if (served.Count != menu.dishes.Count) throw new InvalidOperationException($"{path}: {served.Count} models, {menu.dishes.Count} dishes");
                    for (int i = 0; i < served.Count; i++) menu.dishes[i].servedModel = Root(served[i]);
                    var plate = found.FirstOrDefault(m => m.Groups[1].Value == "plateModel");
                    if (plate != null) menu.plateModel = Root(plate);
                    EditorUtility.SetDirty(menu);
                    Debug.Log($"[RestaurantMenuRepair] {path}: {served.Count(m => m.Groups[3].Success)} models repointed.");
                }
                AssetDatabase.SaveAssets();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static GameObject Root(Match reference)
        {
            if (!reference.Groups[3].Success) return null;
            string model = AssetDatabase.GUIDToAssetPath(reference.Groups[3].Value);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (root == null) throw new InvalidOperationException("No model at " + model);
            return root;
        }
    }
}
