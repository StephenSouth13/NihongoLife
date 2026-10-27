using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;

namespace NihongoLife.Clinic
{
    /// <summary>
    /// The waiting room's own life in Hibari Clinic: townspeople (clinic.json "patients") come in, check in at
    /// reception (受付), sit on a bench until the call board shows their number (「N番の方、診察室へどうぞ」), see
    /// Dr Mori in the consultation room, take their medicine at the window (お薬) and leave. Elders look and move
    /// their age (CharacterLook: grey hair, beard, lines, a slower walk). One person in the consultation room at a time;
    /// they never block the player (no colliders). The route is the "PatientRoute" points ClinicBuilder places.
    /// </summary>
    public sealed class ClinicPatientFlow : MonoBehaviour
    {
        /// <summary>Tests: off = the clinic stays empty of townspeople.</summary>
        public static bool Enabled = true;
        private const float WalkSpeed = 1.15f;

        private readonly Dictionary<string, Transform> _points = new Dictionary<string, Transform>();
        private readonly List<Transform> _seats = new List<Transform>();
        private readonly HashSet<Transform> _takenSeats = new HashSet<Transform>();
        private readonly Queue<Patient> _waiting = new Queue<Patient>();
        private readonly List<Patient> _inside = new List<Patient>();
        private Transform _nurse, _doctor, _pharmacist;
        private TMP_Text _board;
        private TMP_FontAsset _font;
        private ClinicCatalog _catalog;
        private bool _receptionBusy, _consultBusy, _medicineBusy;
        private int _ticket, _next;
        private float _nextArrival;

        /// <summary>Townspeople in the clinic now (tests, captures).</summary>
        public int Count => _inside.Count;
        public int Called { get; private set; }
        public int Treated { get; private set; }

        private sealed class Patient
        {
            public ClinicPatient data;
            public GameObject go;
            public CharacterAnimationController anim;
            public CharacterLook look;
            public int number;
            public float seatedAt;
            public TextMeshPro bubble;
        }

        private void Awake()
        {
            _catalog = ClinicCatalog.Load();
            var route = transform.Find("PatientRoute");
            if (route != null)
                foreach (Transform point in route)
                {
                    _points[point.name] = point;
                    if (point.name.StartsWith("Seat_")) _seats.Add(point);
                }
            _nurse = FindDeep("Nurse_Sato");
            _doctor = FindDeep("Doctor_Mori");
            _pharmacist = FindDeep("Pharmacist");
            var board = FindDeep("CallBoard_Text");
            _board = board != null ? board.GetComponent<TMP_Text>() : null;
            _font = _board != null ? _board.font : null;
            _ticket = 2;
        }

        private Transform FindDeep(string name) => GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private void OnEnable()
        {
            if (!Enabled || _catalog.patients.Length == 0 || !_points.ContainsKey("Door")) return;
            // The room is never empty when you walk in: two people already wait.
            for (int i = 0; i < 2; i++) StartCoroutine(Visit(NextPatient(), true));
            _nextArrival = Time.time + 4f;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            foreach (var p in _inside) if (p.go != null) Destroy(p.go);
            _inside.Clear(); _waiting.Clear(); _takenSeats.Clear();
            _receptionBusy = _consultBusy = _medicineBusy = false;
        }

        private void Update()
        {
            if (!Enabled || Time.time < _nextArrival) return;
            _nextArrival = Time.time + Mathf.Max(4f, _catalog.patientEverySeconds) * Random.Range(0.75f, 1.25f);
            if (_inside.Count < Mathf.Max(1, _catalog.maxPatients) && _seats.Count > _takenSeats.Count) StartCoroutine(Visit(NextPatient(), false));
        }

        private ClinicPatient NextPatient()
        {
            var pool = _catalog.patients.Where(p => _inside.All(i => i.data.id != p.id)).ToArray();
            if (pool.Length == 0) pool = _catalog.patients;
            return pool[(_next++ + Random.Range(0, pool.Length)) % pool.Length];
        }

        // ─────────── One visit ───────────

        private IEnumerator Visit(ClinicPatient data, bool alreadyWaiting)
        {
            var p = Spawn(data);
            if (p == null) yield break;
            _inside.Add(p);
            var symptom = _catalog.Symptom(data.symptom);
            p.number = ++_ticket;

            if (!alreadyWaiting)
            {
                // 受付: wait for the counter, give the card, say what is wrong.
                Place(p, _points["Door"]);
                yield return WalkTo(p, "ReceptionWait");
                while (_receptionBusy) yield return null;
                _receptionBusy = true;
                yield return WalkTo(p, "Reception");
                Face(p, _nurse);
                Say(_nurse, _catalog.greetJa, null, 3f);
                yield return new WaitForSeconds(1.6f);
                Say(p.go.transform, symptom?.sayJa, symptom?.Meaning, 3.2f);
                yield return new WaitForSeconds(3f);
                _receptionBusy = false;
            }

            // The waiting room: a free seat on a bench.
            var seat = _seats.Where(s => !_takenSeats.Contains(s)).OrderBy(_ => Random.value).FirstOrDefault();
            if (seat == null) { yield return Leave(p); yield break; }
            _takenSeats.Add(seat);
            if (alreadyWaiting) Place(p, seat);
            else yield return WalkTo(p, Lane(seat), Side(seat), Front(seat));
            Sit(p, seat);
            p.seatedAt = Time.time - (alreadyWaiting ? 6f : 0f);
            _waiting.Enqueue(p);
            while (_waiting.Peek() != p || _consultBusy || Time.time - p.seatedAt < 7f) yield return null;
            _waiting.Dequeue();
            _consultBusy = true;

            // The call: the board shows the number, the nurse calls it.
            Called++;
            if (_board != null) _board.text = $"しんさつ ばんごう\n<size=180%><color=#7CF0C8>{p.number}</color></size>";
            Say(_nurse, string.Format(_catalog.callJa ?? "{0}", p.number), string.Format(Loc.Pick(_catalog.callVi, _catalog.callVi, _catalog.callJa) ?? "{0}", p.number), 3.5f);
            yield return new WaitForSeconds(1.2f);
            Stand(p);
            _takenSeats.Remove(seat);
            yield return WalkTo(p, Front(seat), Side(seat), Lane(seat));
            yield return WalkTo(p, "Hall_E", "Hall_N", "Corridor", "ConsultIn", "ConsultApproach", "Stool");
            Sit(p, _points["Stool"]);
            Say(_doctor, _catalog.doctorJa, null, 3f);
            yield return new WaitForSeconds(2.4f);
            Say(p.go.transform, symptom?.sayJa, symptom?.Meaning, 3f);
            yield return new WaitForSeconds(3.2f);
            if (symptom != null) Say(_doctor, symptom.diagnosisJa, symptom.Diagnosis, 3.5f);
            yield return new WaitForSeconds(3.6f);
            Stand(p);
            yield return WalkTo(p, "ConsultApproach", "ConsultIn", "Corridor", "Hall_N", "Hall_E");
            _consultBusy = false;

            // お薬: the medicine window, then home.
            yield return WalkTo(p, "MedicineApproach", "MedicineSide");
            while (_medicineBusy) yield return null;
            _medicineBusy = true;
            yield return WalkTo(p, "Medicine");
            Face(p, _pharmacist);
            Say(_pharmacist, (symptom != null ? symptom.medicineJa + "。" : "") + _catalog.byeJa, Loc.Pick(_catalog.byeVi, _catalog.byeVi, _catalog.byeJa), 3.5f);
            yield return new WaitForSeconds(3.2f);
            _medicineBusy = false;
            Treated++;
            yield return Leave(p);
        }

        private IEnumerator Leave(Patient p)
        {
            yield return WalkTo(p, "Exit");
            _inside.Remove(p);
            if (p.go != null) Destroy(p.go);
        }

        // ─────────── Bodies, walking, sitting, speech ───────────

        private Patient Spawn(ClinicPatient data)
        {
            var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + data.model);
            if (prefab == null) return null;
            var go = Instantiate(prefab, transform);
            go.name = "Townsperson_" + data.id;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c); // never in the player's way
            var anim = go.AddComponent<CharacterAnimationController>();
            anim.SetAnimator(go.GetComponentInChildren<Animator>(true));
            var look = CharacterLook.On(go, false).Fixed(data.hair, data.beard, data.age);
            var plate = Label(go.transform, "Nameplate", $"{data.nameJa}\n<size=70%>{data.nameVi}</size>", 2.1f / go.transform.lossyScale.y, 0.9f, new Color(0.95f, 0.95f, 0.9f)); // kids are built at 1.35×
            plate.gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
            return new Patient { data = data, go = go, anim = anim, look = look };
        }

        private static void Place(Patient p, Transform point)
        {
            p.go.transform.SetPositionAndRotation(point.position, point.rotation);
        }

        private IEnumerator WalkTo(Patient p, params string[] names)
        {
            foreach (var name in names)
                if (_points.TryGetValue(name, out var point)) yield return WalkTo(p, point);
        }

        /// <summary>Half a metre in front of a seat (people step there before sitting and after standing).</summary>
        private static Vector3 Front(Transform seat) => seat.position + seat.forward * 0.6f;

        /// <summary>Beside the chair, off its front corner (the way round it).</summary>
        private static Vector3 Side(Transform seat) => Front(seat) + seat.right * 0.7f;

        /// <summary>The aisle west of the chairs, level with a seat's side (so nobody walks through a chair).</summary>
        private Vector3 Lane(Transform seat) => _points.TryGetValue("Aisle", out var aisle) ? new Vector3(aisle.position.x, seat.position.y, Side(seat).z) : Side(seat);

        private IEnumerator WalkTo(Patient p, params Vector3[] targets)
        {
            foreach (var target in targets) yield return WalkTo(p, target, null);
        }

        private IEnumerator WalkTo(Patient p, Transform point) => WalkTo(p, point.position, point.rotation);

        private IEnumerator WalkTo(Patient p, Vector3 target, Quaternion? end)
        {
            var t = p.go.transform;
            while (p.go != null)
            {
                Vector3 to = target - t.position;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance < 0.06f) break;
                float speed = WalkSpeed * p.look.Walk;
                t.position = Vector3.MoveTowards(t.position, new Vector3(target.x, t.position.y, target.z), speed * Time.deltaTime);
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(to / Mathf.Max(0.001f, distance)), 9f * Time.deltaTime);
                p.anim.SetSpeed(speed);
                yield return null;
            }
            if (p.go == null) yield break;
            p.anim.SetSpeed(0f);
            if (end.HasValue) t.rotation = end.Value;
        }

        private static void Face(Patient p, Transform target)
        {
            if (target == null) return;
            Vector3 to = target.position - p.go.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) p.go.transform.rotation = Quaternion.LookRotation(to);
        }

        private static void Sit(Patient p, Transform seat)
        {
            p.go.transform.SetPositionAndRotation(seat.position, seat.rotation);
            p.anim.SetSitting(true);
        }

        private static void Stand(Patient p) => p.anim.SetSitting(false);

        private readonly Dictionary<Transform, Coroutine> _speech = new Dictionary<Transform, Coroutine>();

        /// <summary>A short speech line over someone's head (Japanese, the meaning below), for a few seconds.</summary>
        private void Say(Transform who, string ja, string meaning, float seconds)
        {
            if (who == null || string.IsNullOrEmpty(ja)) return;
            var existing = who.Find("Speech");
            var bubble = existing != null ? existing.GetComponent<TextMeshPro>() : null;
            if (bubble == null)
            {
                bubble = Label(who, "Speech", "", 2.6f / Mathf.Max(0.01f, who.lossyScale.y), 1.05f, Color.white);
                bubble.gameObject.AddComponent<BillboardUI>();
                bubble.outlineWidth = 0.22f;
                bubble.outlineColor = new Color32(20, 40, 46, 255);
            }
            bubble.text = string.IsNullOrEmpty(meaning) ? ja : $"{ja}\n<size=62%><color=#D8F2EA>{meaning}</color></size>";
            bubble.gameObject.SetActive(true);
            if (_speech.TryGetValue(who, out var running) && running != null) StopCoroutine(running);
            _speech[who] = StartCoroutine(Hide(bubble, seconds));
        }

        private static IEnumerator Hide(TextMeshPro bubble, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (bubble != null) bubble.gameObject.SetActive(false);
        }

        private TextMeshPro Label(Transform parent, string name, string text, float height, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            var label = go.AddComponent<TextMeshPro>();
            if (_font != null) label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.rectTransform.sizeDelta = new Vector2(3.2f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }
}
