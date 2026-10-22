using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>Partial of CityStreetBuilder: crossings, mirrors, vending machines, and manholes. The public entry point stays CityStreetBuilder.cs.</summary>
    public static partial class CityStreetBuilder
    {
        // ─────────────────────────── furniture ───────────────────────────

        private static void Crossing(Ctx c)
        {
            // Signals are built in Traffic(); here the pedestrian corner bollards.
            var group = Group(c.root, "CrossingBollards");
            foreach (float x in new[] { LaneW - 0.3f, LaneE + 0.3f })
                foreach (float z in new[] { AveN + 0.45f, AveS - 0.45f })
                {
                    var b = Prim(PrimitiveType.Cylinder, group, "Bollard", new Vector3(x, 0.45f, c.dz + z), new Vector3(0.14f, 0.45f, 0.14f), _signalBody);
                    Object.DestroyImmediate(b.GetComponent<Collider>());
                }
        }

        private static void Mirrors(Ctx c)
        {
            var group = Group(c.root, "CurveMirrors");
            Mirror(group, new Vector3(LaneE + 0.5f, 0f, c.dz + WalkS - 0.8f), 225f);
            Mirror(group, new Vector3(LaneW - 0.4f, 0f, c.dz + AlleyN1 + 0.6f), 135f);
        }

        /// <summary>カーブミラー: orange pole, round mirror with an orange rim, tilted to the street.</summary>
        private static void Mirror(Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "CurveMirror");
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var pole = Prim(PrimitiveType.Cylinder, root, "Pole", new Vector3(0f, 1.45f, 0f), new Vector3(0.09f, 1.45f, 0.09f), _mirrorPole);
            pole.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var head = Group(root, "Head");
            head.localPosition = new Vector3(0f, 2.75f, 0.12f);
            head.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var rim = Prim(PrimitiveType.Cylinder, head, "Rim", Vector3.zero, new Vector3(0.82f, 0.03f, 0.82f), _mirrorPole);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var glass = Prim(PrimitiveType.Cylinder, head, "Glass", new Vector3(0f, 0f, 0.035f), new Vector3(0.72f, 0.01f, 0.72f), _mirror);
            glass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (var col in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            root.gameObject.AddComponent<CapsuleCollider>().center = new Vector3(0f, 1.4f, 0f);
            var cap = root.GetComponent<CapsuleCollider>(); cap.radius = 0.12f; cap.height = 2.8f;
        }

        private static void Vending(Ctx c)
        {
            var group = Group(c.root, "VendingMachines");
            VendingMachine(group, new Vector3(-13.8f, 0f, c.dz + WalkN + 0.45f), 180f);
            VendingMachine(group, new Vector3(-12.8f, 0f, c.dz + WalkN + 0.45f), 180f);
            VendingMachine(group, new Vector3(25.2f, 0f, c.dz + WalkS - 0.45f), 0f);
        }

        /// <summary>自動販売機: a body, a lit front of drink rows (generated texture), a coin strip.</summary>
        private static void VendingMachine(Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "VendingMachine");
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var body = Prim(PrimitiveType.Cube, root, "Body", new Vector3(0f, 0.92f, 0f), new Vector3(0.92f, 1.84f, 0.72f), _vendBody);
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "Front"; Object.DestroyImmediate(front.GetComponent<Collider>());
            front.transform.SetParent(root, false);
            front.transform.localPosition = new Vector3(0f, 1.05f, 0.362f);
            front.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            front.transform.localScale = new Vector3(0.8f, 1.5f, 1f);
            front.GetComponent<Renderer>().sharedMaterial = _vendFront;
            var slot = Prim(PrimitiveType.Cube, root, "Slot", new Vector3(0f, 0.18f, 0.33f), new Vector3(0.6f, 0.2f, 0.1f), _iron);
            Object.DestroyImmediate(slot.GetComponent<Collider>());
        }

        private static void Manholes(Ctx c)
        {
            var group = Group(c.root, "Manholes");
            foreach (var (x, z) in new[] { (-40f, -8.4f), (22f, -11.6f), (50f, -8.4f), ((LaneW + LaneE) / 2f, -30f) })
            {
                var m = Prim(PrimitiveType.Cylinder, group, "Manhole", new Vector3(x, 0.013f, c.dz + z), new Vector3(0.7f, 0.004f, 0.7f), _iron);
                Object.DestroyImmediate(m.GetComponent<Collider>());
                m.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
