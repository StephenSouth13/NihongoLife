using System.Collections.Generic;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>A scene with a floor, a sun and Kids-family people whose Animator and GestureLayer are stepped by hand (1/60 s).</summary>
    public abstract class GestureTestBase
    {
        protected const float Dt = 1f / 60f;
        protected Scene Scene, Previous;
        protected readonly List<GameObject> Spawned = new List<GameObject>();

        [SetUp]
        public void BaseSetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            CharacterVariants.Reset(); GestureLibrary.Reset();
            Previous = SceneManager.GetActiveScene();
            Scene = SceneManager.CreateScene("Gesture_" + System.Guid.NewGuid().ToString("N").Substring(0, 6));
            SceneManager.SetActiveScene(Scene);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.15f; light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(38f, 160f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.68f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Floor"; ground.transform.position = new Vector3(0f, -0.05f, 0f); ground.transform.localScale = new Vector3(40f, 0.1f, 40f);
            Object.Destroy(ground.GetComponent<Collider>());
            ground.GetComponent<Renderer>().sharedMaterial.color = new Color(0.86f, 0.87f, 0.88f);
        }

        [TearDown]
        public void BaseTearDown()
        {
            foreach (var go in Spawned) if (go != null) Object.Destroy(go);
            Spawned.Clear();
            if (Previous.IsValid() && Previous.isLoaded) SceneManager.SetActiveScene(Previous);
            if (Scene.IsValid()) SceneManager.UnloadSceneAsync(Scene);
        }

        protected GameObject Person(string variantId, Vector3 position, out GestureLayer layer)
        {
            var go = CharacterVariants.Spawn(variantId, position, 0f);
            Assert.NotNull(go, variantId + " spawns");
            SceneManager.MoveGameObjectToScene(go, Scene);
            Spawned.Add(go);
            var animator = go.GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            animator.enabled = false; // updated by hand
            layer = GestureLayer.On(go);
            layer.Manual = true;
            animator.Update(Dt);
            return go;
        }

        protected static string FirstVariant => CharacterVariants.File.variants[0].id;

        protected static void Tick(GestureLayer layer, int steps = 1)
        {
            for (int i = 0; i < steps; i++) { layer.Animator.Update(Dt); layer.Step(Dt); }
        }

        protected static Vector3 Bone(GestureLayer layer, HumanBodyBones bone) => layer.Animator.GetBoneTransform(bone).position;

        /// <summary>Right hand, left hand, head, hips, right foot, left foot.</summary>
        protected static readonly HumanBodyBones[] Tracked =
        {
            HumanBodyBones.RightHand, HumanBodyBones.LeftHand, HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.RightFoot, HumanBodyBones.LeftFoot
        };

        protected static Vector3[] Snapshot(GestureLayer layer)
        {
            var s = new Vector3[Tracked.Length];
            for (int i = 0; i < s.Length; i++) s[i] = Bone(layer, Tracked[i]);
            return s;
        }

        protected static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        protected static GestureDef MakeStatic(string id, params (string ch, float x, float y, float z)[] values)
        {
            var def = new GestureDef { id = id, cat = "test", vi = id, en = id, ja = id, ru = id, dur = 3f, loop = true, hold = true, fin = 0.2f, fout = 0.2f };
            var list = new List<GChannel>();
            foreach (var v in values)
                list.Add(new GChannel { n = v.ch, k = new[] { new GKey { t = 0f, x = v.x, y = v.y, z = v.z }, new GKey { t = 3f, x = v.x, y = v.y, z = v.z } } });
            def.ch = list.ToArray();
            GestureLibrary.Register(def);
            return def;
        }
    }
}