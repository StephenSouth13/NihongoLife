using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Home
{
    /// <summary>Shows the equipped wardrobe on a Kids-pack rig. The pack has no clothing meshes, so for v1: hats are real
    /// meshes on the head bone (non la = conical palm hat), and garments tint a small sash/collar child so the change is
    /// visible. Rebuilt only when Apply is called (no per-frame work).</summary>
    public sealed class WardrobeLook : MonoBehaviour
    {
        private readonly List<GameObject> _parts = new List<GameObject>();
        public IReadOnlyList<GameObject> Parts => _parts;

        public static WardrobeLook On(GameObject who) => Comp<WardrobeLook>(who);

        public void Apply(IEnumerable<string> equipped)
        {
            foreach (var p in _parts) if (p != null) { if (Application.isPlaying) Destroy(p); else DestroyImmediate(p); }
            _parts.Clear();
            var anim = GetComponentInChildren<Animator>();
            Transform head = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform chest = anim != null && anim.isHuman ? (anim.GetBoneTransform(HumanBodyBones.UpperChest) != null ? anim.GetBoneTransform(HumanBodyBones.UpperChest) : anim.GetBoneTransform(HumanBodyBones.Chest)) : null;
            foreach (var id in equipped)
            {
                switch (id)
                {
                    case "non_la": Add(head, "Wear_NonLa", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.03f, 0.55f), new Color(0.86f, 0.74f, 0.48f)); break;
                    case "ao_dai": Add(chest, "Wear_AoDai", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.09f), new Vector3(0.26f, 0.3f, 0.02f), new Color(0.85f, 0.2f, 0.25f)); break;
                    case "yukata": Add(chest, "Wear_Yukata", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0.1f), new Vector3(0.28f, 0.05f, 0.02f), new Color(0.2f, 0.3f, 0.6f)); break;
                    case "linen_shirt": Add(chest, "Wear_Linen", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.09f), new Vector3(0.2f, 0.06f, 0.02f), new Color(0.95f, 0.93f, 0.85f)); break;
                    case "server_uniform": Add(chest, "Wear_Uniform", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.09f), new Vector3(0.12f, 0.04f, 0.02f), new Color(0.15f, 0.15f, 0.18f)); break;
                }
            }
        }

        private void Add(Transform bone, string name, PrimitiveType shape, Vector3 local, Vector3 scale, Color color)
        {
            if (bone == null) return;
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.transform.SetParent(bone, false);
            go.transform.localPosition = local; go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            var mat = new Material(r.sharedMaterial) { color = color };
            r.sharedMaterial = mat;
            _parts.Add(go);
        }

        private static T Comp<T>(GameObject g) where T : Component { return g.TryGetComponent<T>(out var c) ? c : g.AddComponent<T>(); }
    }
}
