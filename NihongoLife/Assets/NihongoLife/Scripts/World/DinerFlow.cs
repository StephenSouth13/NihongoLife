using System;
using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Customers coming and going in a restaurant (Sushi Hibari; Gánh Ngon can use it too): someone comes in, is
    /// greeted (「いらっしゃいませ」), takes a free seat, the cook serves (「へい、おまち！」), they eat (「いただきます」 …
    /// 「おいしい！」), pay at the register (「ごちそうさまでした」 — 「ありがとうございました！」) and leave. One to three at a
    /// time; townspeople look their age (CharacterLook) and never block the player (no colliders, they walk round).
    /// Configured in code by the restaurant's runtime (points in world space).
    /// </summary>
    public sealed class DinerFlow : MonoBehaviour
    {
        public sealed class Setup
        {
            public Vector3 door, inside, register;
            public Quaternion registerFacing = Quaternion.identity;
            public (Vector3 position, Quaternion facing)[] seats = Array.Empty<(Vector3, Quaternion)>();
            public Transform cook, cashier;
            public string greetJa = "いらっしゃいませ！", serveJa = "へい、おまち！", thanksJa = "ありがとうございました！";
            public float everySeconds = 14f;
            public int maxGuests = 3;
        }

        /// <summary>Tests: off = the restaurant stays empty of customers.</summary>
        public static bool Enabled = true;

        private static readonly string[] Models = { "kid_sota", "kid_mio", "kid_yuto", "kid_hina", "kid_riku", "kid_sakura", "kid_haruto", "kid_yui" };
        private static readonly (string hair, string beard, int age)[] Looks =
        {
            ("natural", "none", 28), ("silver", "mustache", 67), ("black", "none", 21), ("dark_brown", "stubble", 38),
            ("white", "long", 79), ("natural", "none", 17), ("ash", "goatee", 52), ("chestnut", "none", 34),
        };
        private const float WalkSpeed = 1.1f;

        private Setup _setup;
        private readonly HashSet<int> _taken = new HashSet<int>();
        private readonly List<GameObject> _guests = new List<GameObject>();
        private TMP_FontAsset _font;
        private int _next;
        private float _nextArrival;

        public int Guests => _guests.Count;
        public int Served { get; private set; }
        public int Paid { get; private set; }

        public void Configure(Setup setup, TMP_FontAsset font)
        {
            _setup = setup;
            _font = font;
            OnEnable();
        }

        private void OnEnable()
        {
            if (!Enabled || _setup == null) return;
            _nextArrival = Time.time + 2f;
            // Someone is already eating when the player walks in.
            if (_setup.seats.Length > 1) StartCoroutine(Visit(Spawn(_setup.seats[1].position, _setup.seats[1].facing), 1, true));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            foreach (var guest in _guests) if (guest != null) Destroy(guest);
            _guests.Clear();
            _taken.Clear();
        }

        private void Update()
        {
            if (!Enabled || _setup == null || Time.time < _nextArrival) return;
            _nextArrival = Time.time + _setup.everySeconds * UnityEngine.Random.Range(0.75f, 1.3f);
            if (_guests.Count >= _setup.maxGuests) return;
            int seat = FreeSeat();
            if (seat < 0) return;
            var guest = Spawn(_setup.door, Quaternion.LookRotation(_setup.inside - _setup.door));
            if (guest != null) StartCoroutine(Visit(guest, seat, false));
        }

        private int FreeSeat()
        {
            var free = new List<int>();
            for (int i = 0; i < _setup.seats.Length; i++) if (!_taken.Contains(i)) free.Add(i);
            return free.Count == 0 ? -1 : free[UnityEngine.Random.Range(0, free.Count)];
        }

        private IEnumerator Visit(GameObject guest, int seat, bool alreadySeated)
        {
            if (guest == null) yield break;
            _taken.Add(seat);
            var anim = guest.GetComponent<CharacterAnimationController>();
            var (position, facing) = _setup.seats[seat];
            if (!alreadySeated)
            {
                Say(_setup.cashier != null ? _setup.cashier : _setup.cook, _setup.greetJa, Loc.T("Kính chào quý khách!", "Welcome!", ""), 3f);
                yield return Walk(guest.transform, _setup.inside);
                yield return Walk(guest.transform, position - facing * Vector3.forward * 0.7f);
                yield return Walk(guest.transform, position);
                guest.transform.rotation = facing;
                anim.SetSitting(true);
                yield return new WaitForSeconds(3f);
                Say(_setup.cook, _setup.serveJa, Loc.T("Món của quý khách đây ạ!", "Here you are!", ""), 3f);
                Served++;
                yield return new WaitForSeconds(1.6f);
                Say(guest.transform, "いただきます。", Loc.T("Mời cả nhà (trước khi ăn)", "Said before eating", ""), 3f);
            }
            else
            {
                guest.transform.SetPositionAndRotation(position, facing);
                yield return null;
                anim.SetSitting(true);
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(14f, 22f));
            Say(guest.transform, UnityEngine.Random.value < 0.5f ? "おいしい！" : "うまい！", Loc.T("Ngon quá!", "Delicious!", ""), 2.6f);
            yield return new WaitForSeconds(UnityEngine.Random.Range(8f, 14f));
            anim.SetSitting(false);
            yield return null;
            _taken.Remove(seat);
            yield return Walk(guest.transform, position - facing * Vector3.forward * 0.7f);
            yield return Walk(guest.transform, _setup.register);
            guest.transform.rotation = _setup.registerFacing;
            Say(guest.transform, "ごちそうさまでした。", Loc.T("Cảm ơn bữa ăn.", "Thank you for the meal.", ""), 3f);
            yield return new WaitForSeconds(2f);
            Say(_setup.cashier != null ? _setup.cashier : _setup.cook, _setup.thanksJa, Loc.T("Cảm ơn quý khách!", "Thank you very much!", ""), 3f);
            Paid++;
            yield return new WaitForSeconds(1.4f);
            yield return Walk(guest.transform, _setup.door);
            _guests.Remove(guest);
            Destroy(guest);
        }

        private GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            int i = _next++;
            var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + Models[i % Models.Length]);
            if (prefab == null) return null;
            var root = new GameObject("Diner_" + i);
            root.transform.SetParent(transform, true);
            root.transform.SetPositionAndRotation(position, rotation);
            var visual = Instantiate(prefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c);
            root.AddComponent<CharacterAnimationController>().SetAnimator(visual.GetComponentInChildren<Animator>(true));
            var look = Looks[i % Looks.Length];
            CharacterLook.On(root, false).Fixed(look.hair, look.beard, look.age);
            _guests.Add(root);
            return root;
        }

        private static IEnumerator Walk(Transform body, Vector3 target)
        {
            if (body == null) yield break;
            var anim = body.GetComponent<CharacterAnimationController>();
            var look = body.GetComponent<CharacterLook>();
            while (body != null)
            {
                Vector3 to = target - body.position;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance < 0.05f) break;
                float speed = WalkSpeed * (look != null ? look.Walk : 1f);
                body.position = Vector3.MoveTowards(body.position, new Vector3(target.x, body.position.y, target.z), speed * Time.deltaTime);
                body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(to / distance), 9f * Time.deltaTime);
                if (anim != null) anim.SetSpeed(speed);
                yield return null;
            }
            if (anim != null) anim.SetSpeed(0f);
        }

        private readonly Dictionary<Transform, Coroutine> _speech = new Dictionary<Transform, Coroutine>();

        /// <summary>A short line over someone's head: the Japanese, the meaning below, for a few seconds.</summary>
        private void Say(Transform who, string ja, string meaning, float seconds)
        {
            if (who == null || string.IsNullOrEmpty(ja)) return;
            var existing = who.Find("DinerSpeech");
            var bubble = existing != null ? existing.GetComponent<TextMeshPro>() : null;
            if (bubble == null)
            {
                var go = new GameObject("DinerSpeech");
                go.transform.SetParent(who, false);
                go.transform.localPosition = new Vector3(0f, 2.6f / Mathf.Max(0.01f, who.lossyScale.y), 0f);
                bubble = go.AddComponent<TextMeshPro>();
                if (_font != null) bubble.font = _font;
                bubble.fontSize = 1.05f;
                bubble.alignment = TextAlignmentOptions.Center;
                bubble.rectTransform.sizeDelta = new Vector2(3.2f, 1f);
                bubble.outlineWidth = 0.22f;
                bubble.outlineColor = new Color32(40, 24, 14, 255);
                go.AddComponent<NihongoLife.UI.BillboardUI>();
            }
            bubble.text = string.IsNullOrEmpty(meaning) ? ja : $"{ja}\n<size=62%><color=#FFE9C8>{meaning}</color></size>";
            bubble.gameObject.SetActive(true);
            if (_speech.TryGetValue(who, out var running) && running != null) StopCoroutine(running);
            _speech[who] = StartCoroutine(Hide(bubble, seconds));
        }

        private static IEnumerator Hide(TextMeshPro bubble, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (bubble != null) bubble.gameObject.SetActive(false);
        }
    }
}
