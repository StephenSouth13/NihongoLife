#if UNITY_EDITOR
using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.NPC;
using NihongoLife.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// One ticket clerk at Hibari Station. The counter had two overlapping figures: the rigged Kimura NPC and
    /// a static, un-rigged mannequin under StationTicketClerk. Kimura was deleted, leaving the mannequin.
    /// This replaces the mannequin with the rigged NL_Cashier (Idle animation) and makes StationTicketClerk
    /// itself Kimura (npc_station_staff) with the ticket-selling service — one figure, animated, interactive.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.StationClerkBuilder.Build
    /// </summary>
    public static class StationClerkBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/20_StationDistrict.unity";
        private const string PrefabPath = "Assets/NihongoLife/Prefabs/Characters/NL_Cashier.prefab";
        private const string ControllerPath = "Assets/NihongoLife/Animations/NL_Humanoid.controller";

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var clerk = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "StationTicketClerk");
                if (clerk == null) throw new InvalidOperationException("StationTicketClerk not found");
                var counter = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "TicketCounter");

                // Remove every previous figure / helper under the clerk anchor (the static mannequin, old triggers).
                foreach (Transform child in clerk.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                foreach (var component in clerk.GetComponents<Component>().Where(c => !(c is Transform)).ToArray()) Object.DestroyImmediate(component);
                // Any other npc_station_staff left in the scene would double the clerk again.
                foreach (var stray in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NPCController>(true)).Where(n => n.NpcId == "npc_station_staff").ToArray())
                    Object.DestroyImmediate(stray.gameObject);

                if (counter != null)
                {
                    Vector3 toCounter = counter.position - clerk.position; toCounter.y = 0f;
                    if (toCounter.sqrMagnitude > 0.01f) clerk.rotation = Quaternion.LookRotation(toCounter.normalized);
                }
                clerk.position = new Vector3(clerk.position.x, 0.06f, clerk.position.z);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Missing " + PrefabPath);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, clerk);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var animator = visual.GetComponentInChildren<Animator>(true);
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                if (animator != null && controller != null) animator.runtimeAnimatorController = controller;

                clerk.gameObject.layer = InteractableLayer;
                var box = clerk.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0f, 0.95f, 0.6f);
                box.size = new Vector3(1.2f, 1.9f, 1.6f);
                var animation = clerk.gameObject.AddComponent<CharacterAnimationController>();
                animation.SetAnimator(animator);
                var so = new SerializedObject(clerk.gameObject.AddComponent<NPCController>());
                so.FindProperty("npcId").stringValue = "npc_station_staff";
                so.FindProperty("displayName").stringValue = "Kimura";
                so.FindProperty("role").stringValue = "Station staff";
                so.FindProperty("promptJa").stringValue = "きっぷを かう";
                so.FindProperty("promptEn").stringValue = "Mua vé với anh Kimura";
                so.FindProperty("fallbackJa").stringValue = "いらっしゃいませ。";
                so.FindProperty("fallbackReading").stringValue = "いらっしゃいませ。";
                so.FindProperty("fallbackEn").stringValue = "Xin chào quý khách.";
                so.FindProperty("fallbackRomaji").stringValue = "Irasshaimase.";
                so.ApplyModifiedPropertiesWithoutUndo();
                clerk.gameObject.AddComponent<StationStaffService>();
                var plate = Text(clerk, "Nameplate", "えきいん · Kimura", new Vector3(0f, 2.15f, 0f), 180f, 0.09f, Color.white, 1.4f);
                plate.gameObject.AddComponent<StationBillboardLabel>();

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                Debug.Log("[StationClerkBuilder] One animated clerk (Kimura) at the ticket counter.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
#endif
