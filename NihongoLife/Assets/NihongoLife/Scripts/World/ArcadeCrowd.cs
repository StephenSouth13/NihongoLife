using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Gestures;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// The regulars of a room (the Game Center): townspeople built from the Kids rigs, each standing at a machine or with
    /// friends and doing something — playing (a looping gesture), watching, cheering now and then. They have no colliders
    /// and stand only where the player has no game to start, so they never block a machine. Spawned on enable; reactions
    /// run on per-person timers (no per-frame scans). Places are written by the builder (GameCenterBuilder.AddArcadeCrowd).
    /// </summary>
    public sealed class ArcadeCrowd : MonoBehaviour
    {
        [Serializable]
        public sealed class Regular
        {
            public string id;
            public string model = "kid_haruto";
            public Vector3 position;
            public float yaw;
            /// <summary>Looping activity ("arcade_play", "drink_cup" …); empty = standing.</summary>
            public string activity;
            public string hair = "natural";
            public int age = 16;
        }

        private static readonly string[] Reactions = { "cheer", "clap", "think", "thumbs_up", "look_around" };

        [SerializeField] private Regular[] regulars = Array.Empty<Regular>();

        private sealed class Live { public Regular data; public GameObject go; public GestureLayer layer; public float next, back; }
        private readonly List<Live> _people = new List<Live>();
        private System.Random _random;

        public IReadOnlyList<Regular> Regulars => regulars;
        public int Count => _people.Count;
        public IEnumerable<GameObject> People { get { foreach (var p in _people) if (p.go != null) yield return p.go; } }

        public void Configure(Regular[] list) => regulars = list ?? Array.Empty<Regular>();

        private void OnEnable()
        {
            _random ??= new System.Random(GetInstanceID());
            if (_people.Count == 0) Spawn();
        }

        private void Spawn()
        {
            foreach (var r in regulars)
            {
                var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + r.model);
                if (prefab == null) continue;
                var go = Instantiate(prefab, transform);
                go.name = "Regular_" + r.id;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c); // never in the player's way
                go.transform.SetPositionAndRotation(transform.TransformPoint(r.position), transform.rotation * Quaternion.Euler(0f, r.yaw, 0f));
                var animation = go.AddComponent<CharacterAnimationController>();
                animation.SetAnimator(go.GetComponentInChildren<Animator>(true));
                CharacterLook.On(go, false).Fixed(r.hair, "none", r.age);
                var live = new Live { data = r, go = go, layer = GestureLayer.On(go), next = Time.time + 8f + (float)_random.NextDouble() * 20f };
                live.layer.ResetMotion(); // just placed: not "walking away" from the origin, or the gesture stops at once
                if (!string.IsNullOrEmpty(r.activity)) live.layer.Play(r.activity);
                _people.Add(live);
            }
        }

        private void Update()
        {
            float now = Time.time;
            foreach (var p in _people)
            {
                if (p.go == null || p.layer == null) continue;
                if (p.back > 0f && now >= p.back)
                {
                    p.back = 0f;
                    if (!string.IsNullOrEmpty(p.data.activity)) p.layer.Play(p.data.activity); else p.layer.Stop(0.3f);
                }
                if (now < p.next) continue;
                // Now and then a reaction (a win, a near miss, a friend's turn), then back to what they were doing.
                p.next = now + 14f + (float)_random.NextDouble() * 22f; // now and then, not everyone at once
                p.layer.Play(Reactions[_random.Next(Reactions.Length)]);
                p.back = now + 2.6f;
            }
        }
    }
}
