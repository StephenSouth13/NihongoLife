using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// The people on Hibari Station's platform, as on a Japanese platform: some wait in line at the boarding marks
    /// painted by the doors (facing the track), two sit on the benches, one paces. When the train's doors open the
    /// queue steps in and is gone; afterwards new passengers come down the stairs and line up again. Townspeople look
    /// their age (CharacterLook) and never block the player (no colliders). Replaces the old static waiting figures
    /// (Passenger_Waiting_A/B sat in the air in front of the benches) and the cloned walkers. Added by
    /// StationTravelController when the station wakes; positions come from the scene's Platform, benches and door.
    /// </summary>
    public sealed class PlatformCrowd : MonoBehaviour
    {
        private static readonly string[] Models = { "kid_yuto", "kid_hina", "kid_riku", "kid_mio", "kid_sota", "kid_sakura", "kid_haruto", "kid_yui" };
        private static readonly (string hair, string beard, int age)[] Looks =
        {
            ("silver", "long", 76), ("natural", "none", 16), ("dark_brown", "mustache", 42), ("natural", "none", 24),
            ("ash", "goatee", 58), ("black", "none", 19), ("natural", "stubble", 33), ("white", "none", 81),
        };
        private const float WalkSpeed = 1.1f;

        private StationTravelController _station;
        private readonly List<Transform> _queueMarks = new List<Transform>();
        private readonly List<GameObject> _queued = new List<GameObject>();
        private Vector3 _stairs, _platformCentre, _towardTrack;
        private int _next;
        private bool _boardedThisTrain, _alightedThisTrain;
        private Vector3 _doorPos, _along;
        private float _nextArrival;

        public int Queued => _queued.Count;
        public int Seated { get; private set; }
        /// <summary>Passengers who stepped off the arriving train and walked to the stairs (all trains so far).</summary>
        public int Alighted { get; private set; }
        /// <summary>Passengers walking from the doors to the stairs right now.</summary>
        public int Alighting { get; private set; }

        private void Start()
        {
            _station = GetComponent<StationTravelController>();
            var platform = FindDeep("Platform");
            var door = FindDeep("BoardTrainDoor");
            if (platform == null || door == null) { enabled = false; return; }
            foreach (var old in new[] { "Passenger_Waiting_A", "Passenger_Waiting_B" })
            {
                var figure = FindDeep(old);
                if (figure != null) figure.gameObject.SetActive(false);
            }
            _platformCentre = platform.position;
            _towardTrack = door.position - _platformCentre;
            _towardTrack.y = 0f;
            _towardTrack = _towardTrack.sqrMagnitude > 0.01f ? _towardTrack.normalized : Vector3.back;
            Vector3 along = Vector3.Cross(Vector3.up, _towardTrack).normalized;
            // The platform's floor, found under the pacing spot — not the door's height: the door's pivot is its middle
            // (≈ 1.15 m), and people placed there stood in the air behind the gates.
            float y = Floor(_platformCentre - _towardTrack * 1.2f, _platformCentre.y);
            _doorPos = door.position;
            _along = along;
            var stairs = FindDeep("ExitStairwell");
            // The stairwell's pivot can sit far from its geometry (the station is built at an offset), so use where it really is.
            Vector3 stairsAt = stairs != null ? stairs.position : Vector3.zero;
            if (stairs != null)
            {
                var parts = stairs.GetComponentsInChildren<Renderer>();
                if (parts.Length > 0) { var box = parts[0].bounds; for (int i = 1; i < parts.Length; i++) box.Encapsulate(parts[i].bounds); stairsAt = box.center; }
            }
            _stairs = stairs != null ? new Vector3(stairsAt.x, y, Mathf.Lerp(_platformCentre.z, stairsAt.z, 0.35f)) : new Vector3(_platformCentre.x, y, _platformCentre.z) - _towardTrack * 3f;

            // Boarding marks a step back from the yellow line, in front of the three doors.
            var marks = new GameObject("BoardingMarks").transform;
            marks.SetParent(transform, false);
            foreach (float x in new[] { -10f, 0f, 10f })
            {
                var mark = new GameObject("Mark_" + x).transform;
                mark.SetParent(marks, false);
                Vector3 at = new Vector3(door.position.x, y, door.position.z) + along * x - _towardTrack * 1.0f;
                // Slide a mark toward the middle until it stands on the platform's own floor (its ends have none).
                for (int step = 0; step < 12 && x != 0f && Mathf.Abs(Floor(at, -99f) - y) > 0.1f; step++) at -= along * Mathf.Sign(x);
                mark.position = at;
                mark.rotation = Quaternion.LookRotation(_towardTrack);
                _queueMarks.Add(mark);
            }

            // Two people on the benches.
            foreach (var bench in new[] { "PlatformBench_A", "PlatformBench_B" })
            {
                var seat = FindDeep(bench);
                if (seat == null) continue;
                var person = Spawn("Bench_" + bench, seat.position, Quaternion.LookRotation(_towardTrack));
                if (person == null) continue;
                person.AddComponent<SeatedPassenger>().Configure(true);
                Seated++;
            }
            // Two already waiting, one pacing.
            for (int i = 0; i < 2; i++) Queue(Spawn("Waiting_" + i, _queueMarks[i * 2].position - _towardTrack * (0.7f * i), _queueMarks[i * 2].rotation));
            Vector3 paceAt = _platformCentre - _towardTrack * 1.2f;
            var pacer = Spawn("Pacing", new Vector3(paceAt.x, y, paceAt.z), Quaternion.LookRotation(along));
            if (pacer != null) StartCoroutine(Pace(pacer.transform, along));
            _nextArrival = Time.time + 6f;
        }

        private void Update()
        {
            if (_station == null) return;
            if (_station.IsTrainBoarding)
            {
                if (!_alightedThisTrain) { _alightedThisTrain = true; StartCoroutine(Alight()); }
                if (!_boardedThisTrain && _queued.Count > 0)
                {
                    _boardedThisTrain = true;
                    for (int i = 0; i < _queued.Count; i++) StartCoroutine(Board(_queued[i], i * 0.8f));
                    _queued.Clear();
                }
                return;
            }
            _boardedThisTrain = false;
            _alightedThisTrain = false;
            if (Time.time < _nextArrival || _queued.Count >= 4) return;
            _nextArrival = Time.time + Random.Range(7f, 12f);
            var mark = _queueMarks[_next % _queueMarks.Count];
            int behind = 0;
            foreach (var q in _queued) if (q != null && Vector3.Distance(q.transform.position, mark.position) < 2.2f) behind++;
            var person = Spawn("Arriving_" + _next, _stairs, Quaternion.LookRotation(-_towardTrack));
            if (person != null) StartCoroutine(ComeAndQueue(person, mark.position - _towardTrack * (0.75f * behind), mark.rotation));
        }

        private IEnumerator ComeAndQueue(GameObject person, Vector3 spot, Quaternion facing)
        {
            yield return Walk(person.transform, spot);
            if (person == null) yield break;
            person.transform.rotation = facing;
            Queue(person);
        }

        private void Queue(GameObject person)
        {
            if (person == null) return;
            // Arrived late while the doors are open: step straight in.
            if (_station != null && _station.IsTrainBoarding && _boardedThisTrain) { StartCoroutine(Board(person, 0f)); return; }
            _queued.Add(person);
        }

        /// <summary>The train arrives: a few passengers step out of the doors, cross the platform and go up the stairs.</summary>
        private IEnumerator Alight()
        {
            float[] doors = { -10f, 0f, 10f };
            for (int i = 0; i < doors.Length; i++)
            {
                yield return new WaitForSeconds(0.6f);
                Vector3 start = new Vector3(_doorPos.x, _stairs.y, _doorPos.z) + _along * doors[i] + _towardTrack * 0.6f;
                var person = Spawn("Alighting_" + i, start, Quaternion.LookRotation(-_towardTrack));
                if (person != null) StartCoroutine(GetOffAndLeave(person));
            }
        }

        private IEnumerator GetOffAndLeave(GameObject person)
        {
            Alighting++;
            yield return Walk(person.transform, person.transform.position - _towardTrack * 2.2f);
            yield return Walk(person.transform, _stairs);
            Alighting--;
            Alighted++;
            if (person != null) Destroy(person);
        }

        private IEnumerator Board(GameObject person, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (person == null) yield break;
            Vector3 door = person.transform.position + _towardTrack * 2.4f;
            yield return Walk(person.transform, door);
            if (person != null) Destroy(person);
        }

        private IEnumerator Pace(Transform body, Vector3 along)
        {
            Vector3 home = body.position;
            float side = 1f;
            while (body != null)
            {
                yield return Walk(body, home + along * 6f * side);
                side = -side;
                yield return new WaitForSeconds(Random.Range(0.8f, 2.4f));
            }
        }

        private IEnumerator Walk(Transform body, Vector3 target)
        {
            var anim = body != null ? body.GetComponent<CharacterAnimationController>() : null;
            var look = body != null ? body.GetComponent<CharacterLook>() : null;
            while (body != null)
            {
                Vector3 to = target - body.position;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance < 0.05f) break;
                float speed = WalkSpeed * (look != null ? look.Walk : 1f);
                body.position = Vector3.MoveTowards(body.position, new Vector3(target.x, body.position.y, target.z), speed * Time.deltaTime);
                body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(to / distance), 8f * Time.deltaTime);
                if (anim != null) anim.SetSpeed(speed);
                yield return null;
            }
            if (anim != null) anim.SetSpeed(0f);
        }

        private GameObject Spawn(string name, Vector3 position, Quaternion rotation)
        {
            int i = _next++;
            var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + Models[i % Models.Length]);
            if (prefab == null) return null;
            var root = new GameObject("PlatformPerson_" + name);
            root.transform.SetParent(transform, true);
            root.transform.SetPositionAndRotation(position, rotation);
            var visual = Instantiate(prefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c); // never in the player's way
            var anim = root.AddComponent<CharacterAnimationController>();
            anim.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            var look = Looks[i % Looks.Length];
            CharacterLook.On(root, false).Fixed(look.hair, look.beard, look.age);
            return root;
        }

        /// <summary>Height of the floor under a point (solid colliders only; people have none); the fallback when nothing is hit.</summary>
        private static float Floor(Vector3 at, float fallback)
        {
            return Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : fallback;
        }

        private Transform FindDeep(string name)
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            return null;
        }
    }
}
