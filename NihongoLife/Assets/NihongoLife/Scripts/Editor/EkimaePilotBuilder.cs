#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    public static class EkimaePilotBuilder
    {
        private const string GroundModel = "Assets/ZRNAssets/005030_06196_1_1/Models/Fukuoka_Ground.fbx";
        private const string PropModel = "Assets/ZRNAssets/005030_06196_1_1/Models/Fukuoka_Prop.fbx";

        public static void AuditSource()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(GroundModel);
            if (source == null) throw new System.InvalidOperationException($"Missing source model: {GroundModel}");

            AuditModel(source, GroundModel, "block_");

            GameObject propSource = AssetDatabase.LoadAssetAtPath<GameObject>(PropModel);
            if (propSource == null) throw new System.InvalidOperationException($"Missing source model: {PropModel}");
            AuditModel(propSource, PropModel, "prop_");
        }

        private static void AuditModel(GameObject source, string sourcePath, string labelPrefix)
        {
            GameObject instance = Object.Instantiate(source);
            try
            {
                Debug.Log($"[EkimaeAudit] Source={sourcePath} root={source.name} scale={source.transform.localScale}");
                for (char suffix = 'A'; suffix <= 'I'; suffix++)
                {
                    Transform block = FindDeep(instance.transform, "block_" + suffix);
                    if (block == null) block = FindDeep(instance.transform, "prop_" + suffix);
                    if (block == null) block = FindDeep(instance.transform, "Prop_" + suffix);
                    if (block == null)
                    {
                        IEnumerable<Transform> matches = instance.GetComponentsInChildren<Transform>(true)
                            .Where(item => item.name.EndsWith("_" + suffix) &&
                                           (item.name.StartsWith("_propA") || item.name.StartsWith("_propB")));
                        Renderer[] looseRenderers = matches.SelectMany(item => item.GetComponentsInChildren<Renderer>(true)).ToArray();
                        if (looseRenderers.Length > 0)
                        {
                            Bounds looseBounds = CalculateBounds(looseRenderers);
                            Debug.Log($"[EkimaeAudit] {labelPrefix}{suffix} looseParts={looseRenderers.Length} bounds={looseBounds.size} center={looseBounds.center}");
                            continue;
                        }
                        Debug.LogWarning($"[EkimaeAudit] {labelPrefix}{suffix} missing");
                        continue;
                    }

                    Renderer[] renderers = block.GetComponentsInChildren<Renderer>(true);
                    Bounds bounds = CalculateBounds(renderers);
                    string materials = string.Join(", ", renderers
                        .SelectMany(renderer => renderer.sharedMaterials)
                        .Where(material => material != null)
                        .Select(material => material.name)
                        .Distinct()
                        .OrderBy(name => name));
                    string children = string.Join(", ", Enumerable.Range(0, block.childCount)
                        .Select(index => block.GetChild(index).name));

                    Debug.Log($"[EkimaeAudit] block_{suffix} bounds={bounds.size} center={bounds.center} " +
                              $"renderers={renderers.Length} children=[{children}] materials=[{materials}]");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform FindDeep(Transform root, string targetName)
        {
            if (root.name == targetName) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static Bounds CalculateBounds(IReadOnlyList<Renderer> renderers)
        {
            if (renderers.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
#endif
