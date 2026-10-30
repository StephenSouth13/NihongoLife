using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>
    /// Tourists and locals walking Dương Đông's pavements (PhuQuocDowntownBuilder). They walk lane segments end to end,
    /// and when two meet they stop, face each other and greet in their own language (vi / en / ja / ru / ko / zh) —
    /// the island's mix of visitors. No colliders (they never push the player), no per-frame searches or allocations:
    /// meetings are checked on a 0.5 s timer. Bodies are the town's kid-toon models with fixed adult looks; any held
    /// prop is released on spawn (the "board in front of the belly" seen on spawned NPCs).
    /// </summary>
    public sealed class PhuQuocStreetLife : MonoBehaviour
    {
        public static bool Disabled;
        [SerializeField] private Vector3[] lanes = new Vector3[0]; // pairs: start, end (local)
        [SerializeField] private int count = 10;

        private static readonly string[] Models = { "kid_haruto", "kid_hina", "kid_mio", "kid_riku", "kid_sakura", "kid_sota", "kid_yui", "kid_yuto" };
        private static readonly string[] Hairs = { "natural", "black", "brown", "blonde", "silver" };
        private static readonly string[] Greetings =
        {
            "Xin chào!", "Hello!", "こんにちは!", "Привет!", "안녕하세요!", "你好!", "Ăn gì chưa?", "Đi chợ đêm không?",
            "Nice beach!", "Спасибо!", "Ngon quá!", "美味しい!",
        };

        private sealed class Walker
        {
            public GameObject go; public CharacterAnimationController anim; public TextMeshPro bubble;
            public int lane; public float t, dir, speed, pause, talkCooldown;
        }

        private readonly List<Walker> _walkers = new List<Walker>();
        private float _meetTimer;

        public IReadOnlyList<GameObject> Bodies { get { var l = new List<GameObject>(); foreach (var w in _walkers) l.Add(w.go); return l; } }

        public void Configure(Vector3[] laneEnds, int people) { lanes = laneEnds; count = people; }

        private void Start()
        {
            if (Disabled || lanes == null || lanes.Length < 2) return;
            var rnd = new System.Random(65);
            int laneCount = lanes.Length / 2;
            for (int i = 0; i < count; i++)
            {
                var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + Models[i % Models.Length]);
                if (prefab == null) continue;
                var go = Instantiate(prefab, transform);
                go.name = "StreetWalker_" + i;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
                var anim = go.GetComponent<CharacterAnimationController>();
                if (anim == null) anim = go.AddComponent<CharacterAnimationController>();
                anim.SetAnimator(go.GetComponentInChildren<Animator>(true));
                anim.ReleaseProp();
                CharacterLook.On(go, false).Fixed(Hairs[rnd.Next(Hairs.Length)], "none", 18 + rnd.Next(50));
                var bubble = new GameObject("Bubble").AddComponent<TextMeshPro>();
                bubble.transform.SetParent(go.transform, false);
                bubble.transform.localPosition = Vector3.up * 2.1f;
                bubble.fontSize = 3f; bubble.alignment = TextAlignmentOptions.Center;
                bubble.rectTransform.sizeDelta = new Vector2(3f, 1f);
                bubble.gameObject.AddComponent<NihongoLife.UI.BillboardUI>();
                bubble.gameObject.SetActive(false);
                var w = new Walker
                {
                    go = go, anim = anim, bubble = bubble, lane = i % laneCount, t = (float)rnd.NextDouble(),
                    dir = rnd.Next(2) == 0 ? 1f : -1f, speed = 0.9f + (float)rnd.NextDouble() * 0.5f,
                };
                _walkers.Add(w);
                Place(w);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _walkers.Count; i++)
            {
                var w = _walkers[i];
                if (w.go == null) continue;
                w.talkCooldown -= dt;
                if (w.pause > 0f)
                {
                    w.pause -= dt;
                    if (w.pause <= 0f) { w.bubble.gameObject.SetActive(false); w.anim.SetTalking(false); }
                    w.anim.SetSpeed(0f);
                    continue;
                }
                Vector3 a = lanes[w.lane * 2], b = lanes[w.lane * 2 + 1];
                float len = Mathf.Max(1f, Vector3.Distance(a, b));
                w.t += w.dir * w.speed * dt / len;
                if (w.t > 1f || w.t < 0f) { w.t = Mathf.Clamp01(w.t); w.dir = -w.dir; w.pause = 1.5f; }
                Place(w);
                w.anim.SetSpeed(w.pause > 0f ? 0f : w.speed);
            }
            _meetTimer -= dt;
            if (_meetTimer <= 0f) { _meetTimer = 0.5f; Meet(); }
        }

        private void Place(Walker w)
        {
            Vector3 a = lanes[w.lane * 2], b = lanes[w.lane * 2 + 1];
            w.go.transform.localPosition = Vector3.Lerp(a, b, w.t);
            Vector3 fwd = (b - a) * w.dir; fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.001f) w.go.transform.localRotation = Quaternion.LookRotation(fwd);
        }

        private void Meet()
        {
            for (int i = 0; i < _walkers.Count; i++)
                for (int j = i + 1; j < _walkers.Count; j++)
                {
                    var p = _walkers[i]; var q = _walkers[j];
                    if (p.lane != q.lane || p.pause > 0f || q.pause > 0f || p.talkCooldown > 0f || q.talkCooldown > 0f) continue;
                    var d = q.go.transform.position - p.go.transform.position;
                    if (d.sqrMagnitude > 1.8f * 1.8f) continue;
                    Greet(p, q, d); Greet(q, p, -d);
                }
        }

        private void Greet(Walker w, Walker other, Vector3 toward)
        {
            w.pause = 3f; w.talkCooldown = 20f;
            toward.y = 0f;
            if (toward.sqrMagnitude > 0.001f) w.go.transform.rotation = Quaternion.LookRotation(toward);
            w.bubble.text = Greetings[Random.Range(0, Greetings.Length)];
            w.bubble.gameObject.SetActive(true);
            w.anim.SetTalking(true);
            w.anim.SetSpeed(0f);
        }
    }
}
