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
    /// <summary>Partial of CityStreetBuilder: signals and generated cars. The public entry point stays CityStreetBuilder.cs.</summary>
    public static partial class CityStreetBuilder
    {
        // ─────────────────────────── traffic ───────────────────────────

        private static void Traffic(Ctx c, Dictionary<string, GameObject> cars)
        {
            var group = Group(c.root, "Traffic");
            var traffic = group.gameObject.AddComponent<StreetTraffic>();
            traffic.Configure(AveC + 1.6f, AveC - 1.6f, XMin + 0.5f, XMax - 0.5f, Rect.MinMaxRect(LaneW, AveS, LaneE, AveN), StopEastX, StopWestX);

            // Signals: horizontal car signals (青・黄・赤) on both sides, walking-figure signals at both ends.
            CarSignal(traffic, group, new Vector3(-1.6f, 0f, AveN + 0.7f), 270f);
            CarSignal(traffic, group, new Vector3(8.6f, 0f, AveS - 0.7f), 90f);
            var chirp = WalkSignal(traffic, group, new Vector3(LaneE + 0.25f, 0f, AveN + 0.9f), 180f);
            WalkSignal(traffic, group, new Vector3(LaneW - 0.25f, 0f, AveS - 0.9f), 0f);
            traffic.SetChirp(chirp);

            var keys = cars.Keys.Where(k => k != "car_police").ToArray();
            var rnd = new System.Random(5 + c.k);
            for (int i = 0; i < 6; i++)
            {
                int dir = i < 3 ? 1 : -1;
                float x = Mathf.Lerp(XMin + 6f, XMax - 6f, (i % 3 + (float)rnd.NextDouble() * 0.6f) / 3f);
                var prefab = cars[keys[Mod(i + c.k, keys.Length)]];
                var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                car.name = (dir > 0 ? "Car_East_" : "Car_West_") + i;
                car.transform.localPosition = new Vector3(x, 0f, dir > 0 ? AveC + 1.6f : AveC - 1.6f);
                car.transform.localRotation = Quaternion.Euler(0f, dir > 0 ? 90f : 270f, 0f);
                var wheels = car.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("WheelPivot")).ToArray();
                traffic.AddCar(car.transform, wheels, dir, 6.2f + (float)rnd.NextDouble() * 1.6f);
            }
        }

        private static void CarSignal(StreetTraffic traffic, Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "CarSignal");
            root.localPosition = pos; root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(PrimitiveType.Cylinder, root, "Pole", new Vector3(0f, 2.3f, 0f), new Vector3(0.14f, 2.3f, 0.14f), _signalBody);
            var arm = Prim(PrimitiveType.Cube, root, "Arm", new Vector3(0f, 4.4f, 1.2f), new Vector3(0.08f, 0.08f, 2.4f), _signalBody);
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            var box = Prim(PrimitiveType.Cube, root, "Housing", new Vector3(0f, 4.4f, 2.2f), new Vector3(0.28f, 0.42f, 1.25f), _signalBody);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            var lamps = new[] { (_green, StreetTraffic.Phase.CarGreen, 2.6f), (_yellow, StreetTraffic.Phase.CarYellow, 2.2f), (_red, StreetTraffic.Phase.AllRed1, 1.8f) };
            foreach (var (mat, phase, z) in lamps)
            {
                var lamp = Prim(PrimitiveType.Cylinder, root, "Lamp_" + phase, new Vector3(-0.15f, 4.4f, z), new Vector3(0.32f, 0.02f, 0.32f), _lampOff);
                lamp.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                traffic.AddLamp(lamp.GetComponent<Renderer>(), phase, mat, _lampOff);
            }
            var hood = root.GetComponentsInChildren<Collider>();
            foreach (var h in hood) if (h.name != "Pole") Object.DestroyImmediate(h);
        }

        private static AudioSource WalkSignal(StreetTraffic traffic, Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "WalkSignal");
            root.localPosition = pos; root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(PrimitiveType.Cylinder, root, "Pole", new Vector3(0f, 1.4f, 0f), new Vector3(0.1f, 1.4f, 0.1f), _signalBody);
            var box = Prim(PrimitiveType.Cube, root, "Housing", new Vector3(0f, 2.55f, 0.12f), new Vector3(0.36f, 0.72f, 0.2f), _signalBody);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            foreach (var (mat, phase, y) in new[] { (_walkRed, StreetTraffic.Phase.AllRed2, 2.74f), (_walkGreen, StreetTraffic.Phase.WalkGreen, 2.36f) })
            {
                var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                face.name = "Walk_" + phase; Object.DestroyImmediate(face.GetComponent<Collider>());
                face.transform.SetParent(root, false);
                face.transform.localPosition = new Vector3(0f, y, 0.225f);
                face.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                face.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                face.GetComponent<Renderer>().sharedMaterial = _lampOff;
                traffic.AddLamp(face.GetComponent<Renderer>(), phase, mat, LampOffFor(mat));
            }
            var audio = root.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1f; audio.minDistance = 3f; audio.maxDistance = 30f; audio.volume = 0.5f;
            return audio;
        }

        private static Material LampOffFor(Material lit) => lit == _walkGreen ? Mat("WalkGreenOff", WalkTexture(new Color(0.12f, 0.2f, 0.16f), true), new Color(0.6f, 0.6f, 0.6f), 0f)
            : Mat("WalkRedOff", WalkTexture(new Color(0.22f, 0.1f, 0.1f), false), new Color(0.6f, 0.6f, 0.6f), 0f);

        // ─────────────────────────── cars ───────────────────────────

        /// <summary>KayKit cars as prefabs: body scaled to ~4.3 m, each wheel under a pivot at its centre so it can spin.</summary>
        private static Dictionary<string, GameObject> CarPrefabs()
        {
            var result = new Dictionary<string, GameObject>();
            foreach (string name in new[] { "car_sedan", "car_taxi", "car_hatchback", "car_stationwagon", "car_police" })
            {
                var bodySrc = AssetDatabase.LoadAssetAtPath<GameObject>(KayKit + name + ".obj");
                if (bodySrc == null) continue;
                var root = new GameObject(name);
                var model = Group(root.transform, "Model");
                var body = (GameObject)PrefabUtility.InstantiatePrefab(bodySrc, model);
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (string w in new[] { "front_left", "front_right", "rear_left", "rear_right" })
                {
                    var wheelSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{KayKit}{name}_wheel_{w}.obj");
                    if (wheelSrc == null) continue;
                    var wheel = (GameObject)PrefabUtility.InstantiatePrefab(wheelSrc);
                    PrefabUtility.UnpackPrefabInstance(wheel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var wb = RendererBounds(wheel.transform);
                    var pivot = Group(model, "WheelPivot_" + w);
                    pivot.position = wb.center;
                    wheel.transform.SetParent(pivot, true);
                }
                IslandBuilder.ConvertMaterials(root);
                foreach (var col in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                var b = RendererBounds(root.transform);
                float length = Mathf.Max(b.size.x, b.size.z);
                model.localScale = Vector3.one * (4.3f / Mathf.Max(0.01f, length));
                // Face +z and sit on the ground at the origin.
                b = RendererBounds(root.transform);
                if (b.size.x > b.size.z) { model.localRotation = Quaternion.Euler(0f, 90f, 0f); b = RendererBounds(root.transform); }
                model.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
                b = RendererBounds(root.transform);
                var box = root.AddComponent<BoxCollider>();
                box.center = b.center; box.size = b.size;
                var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
                result[name] = PrefabUtility.SaveAsPrefabAsset(root, $"{Gen}/Cars/{name}.prefab");
                Object.DestroyImmediate(root);
            }
            return result;
        }
    }
}
