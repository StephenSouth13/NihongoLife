using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>
    /// Guests walking into Gánh Ngon (113 Trần Hưng Đạo): they come in from the pavement, sit at a table spot,
    /// order a real menu dish in their own language (pre-written lines, no runtime AI), wait, eat and leave, then
    /// another guest arrives. While the player works a shift, using a Table station (GanhNgonShift.StationUsed)
    /// serves the guest who has waited longest; otherwise the (fictional) staff serve them after a while.
    /// </summary>
    public sealed class GanhNgonCustomers : MonoBehaviour
    {
        public static bool Disabled;
        public enum State { Arriving, Ordering, Waiting, Eating, Leaving, Away }

        /// <summary>Pre-written orders (dishes from ganh_ngon.json; mixed guest languages like the real restaurant).</summary>
        public static readonly string[] Orders =
        {
            "Cho mình một lẩu gà sầu riêng nhé!",
            "Hải sản nướng BBQ cho 2 người ạ.",
            "Seafood BBQ for two, please!",
            "Could we try the fried durian?",
            "サウリエンの揚げ物をください。",
            "ドリアン鶏鍋をお願いします！",
            "Мне говядину на гриле, пожалуйста.",
            "Cơm niêu trưa nay còn không em?",
            "One cocktail from the pub bar, please.",
            "Bò nướng một phần, cảm ơn!",
            "焼き海鮮セットを一つ。",
            "Можно меню на английском?",
        };
        public static readonly string[] Thanks = { "Ngon quá!", "Delicious!", "おいしい！", "Очень вкусно!", "Cảm ơn nhé!", "ごちそうさま！" };

        public const float OrderTime = 5f, StaffServeAfter = 40f, EatTime = 25f, AwayTime = 12f, WalkSpeed = 1.2f;

        /// <summary>The flow, without Unity objects (tested in EditMode).</summary>
        public static State Next(State s, float timeInState, bool served, bool arrived)
        {
            switch (s)
            {
                case State.Arriving: return arrived ? State.Ordering : s;
                case State.Ordering: return timeInState >= OrderTime ? State.Waiting : s;
                case State.Waiting: return served || timeInState >= StaffServeAfter ? State.Eating : s;
                case State.Eating: return timeInState >= EatTime ? State.Leaving : s;
                case State.Leaving: return arrived ? State.Away : s;
                default: return timeInState >= AwayTime ? State.Arriving : s;
            }
        }

        [SerializeField] private Vector3[] seats = new Vector3[0];    // local; guests face seatFacing[i]
        [SerializeField] private Vector3[] seatFacing = new Vector3[0];
        [SerializeField] private Vector3 door = new Vector3(0, 0, -4.5f);
        [SerializeField] private Vector3[] streetEnds = { new Vector3(-14, 0, -6), new Vector3(14, 0, -6) };

        private static readonly string[] Models = { "kid_sota", "kid_mio", "kid_yuto", "kid_hina", "kid_riku", "kid_yui" };

        private sealed class Guest
        {
            public GameObject go; public CharacterAnimationController anim; public TextMeshPro bubble;
            public State state; public float timer; public int seat; public bool served; public int leg; public Vector3 from;
        }

        private readonly List<Guest> _guests = new List<Guest>();
        public int SeatCount => seats.Length;
        public int Served { get; private set; }

        public void Configure(Vector3[] seatPositions, Vector3[] facing, Vector3 entrance, Vector3[] ends)
        {
            seats = seatPositions; seatFacing = facing; door = entrance; streetEnds = ends;
        }

        private void OnEnable() => GanhNgonShift.StationUsed += OnStation;
        private void OnDisable() => GanhNgonShift.StationUsed -= OnStation;

        private void OnStation(GanhNgonStation station)
        {
            if (station != GanhNgonStation.Table) return;
            Guest best = null;
            foreach (var g in _guests)
                if (g.state == State.Waiting && !g.served && (best == null || g.timer > best.timer)) best = g;
            if (best != null) best.served = true;
        }

        private void Start()
        {
            if (Disabled || seats.Length == 0) return;
            for (int i = 0; i < seats.Length; i++)
            {
                var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + Models[i % Models.Length]);
                if (prefab == null) continue;
                var go = Instantiate(prefab, transform);
                go.name = "Guest_" + i;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
                var anim = go.GetComponent<CharacterAnimationController>();
                if (anim == null) anim = go.AddComponent<CharacterAnimationController>();
                anim.SetAnimator(go.GetComponentInChildren<Animator>(true));
                anim.ReleaseProp();
                CharacterLook.On(go, false).Fixed(i % 2 == 0 ? "black" : "brown", "none", 22 + i * 7);
                var bubble = new GameObject("Bubble").AddComponent<TextMeshPro>();
                bubble.transform.SetParent(go.transform, false);
                bubble.transform.localPosition = Vector3.up * 2.2f;
                bubble.fontSize = 2.6f; bubble.alignment = TextAlignmentOptions.Center;
                bubble.rectTransform.sizeDelta = new Vector2(3.4f, 1f);
                bubble.gameObject.AddComponent<BillboardUI>();
                bubble.gameObject.SetActive(false);
                // Stagger: the first guests are already seated, the rest arrive over the first minute.
                var g = new Guest { go = go, anim = anim, bubble = bubble, seat = i, state = i < 2 ? State.Waiting : State.Away, timer = i < 2 ? 10f * i : -6f * i };
                g.from = streetEnds[i % streetEnds.Length];
                go.transform.localPosition = g.state == State.Waiting ? seats[i] : g.from;
                if (g.state == State.Waiting) Sit(g); else go.SetActive(false);
                _guests.Add(g);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var g in _guests)
            {
                g.timer += dt;
                bool arrived = false;
                if (g.state == State.Arriving) arrived = Walk(g, g.leg == 0 ? door : seats[g.seat], dt, ref g.leg, 2);
                else if (g.state == State.Leaving) arrived = Walk(g, g.leg == 0 ? door : g.from, dt, ref g.leg, 2);
                var next = Next(g.state, g.timer, g.served, arrived);
                if (next == g.state) continue;
                Enter(g, next);
            }
        }

        private void Enter(Guest g, State s)
        {
            g.state = s; g.timer = 0f; g.leg = 0;
            switch (s)
            {
                case State.Arriving:
                    g.served = false; g.go.SetActive(true); g.go.transform.localPosition = g.from; g.anim.SetSitting(false);
                    break;
                case State.Ordering:
                    Sit(g); Say(g, Orders[Random.Range(0, Orders.Length)]);
                    break;
                case State.Waiting:
                    Say(g, "…");
                    break;
                case State.Eating:
                    Served++; Say(g, Thanks[Random.Range(0, Thanks.Length)]);
                    break;
                case State.Leaving:
                    g.bubble.gameObject.SetActive(false); g.anim.SetSitting(false); g.anim.SetTalking(false);
                    g.from = streetEnds[Random.Range(0, streetEnds.Length)];
                    break;
                case State.Away:
                    g.go.SetActive(false);
                    break;
            }
        }

        private void Sit(Guest g)
        {
            g.go.transform.localPosition = seats[g.seat];
            var f = seatFacing.Length > g.seat ? seatFacing[g.seat] : Vector3.forward;
            if (f.sqrMagnitude > 0.001f) g.go.transform.localRotation = Quaternion.LookRotation(f);
            g.anim.SetSpeed(0f);
            g.anim.SetSitting(true);
        }

        private void Say(Guest g, string line)
        {
            g.bubble.text = line;
            g.bubble.gameObject.SetActive(true);
            g.anim.SetTalking(line.Length > 1);
        }

        private bool Walk(Guest g, Vector3 target, float dt, ref int leg, int legs)
        {
            var t = g.go.transform;
            var d = target - t.localPosition; d.y = 0f;
            float step = WalkSpeed * dt;
            if (d.magnitude <= step)
            {
                t.localPosition = target;
                leg++;
                return leg >= legs;
            }
            t.localPosition += d.normalized * step;
            t.localRotation = Quaternion.LookRotation(d);
            g.anim.SetSpeed(WalkSpeed);
            return false;
        }
    }
}
