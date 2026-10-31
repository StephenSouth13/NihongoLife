using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>
    /// The island's background people (PhuQuocCrowdBuilder): market vendors and shoppers, tourist families, swimmers,
    /// sunbathers, alley residents, resort guests and the Gánh Ngon staff. Bodies are CC0 Kenney mini-characters posed
    /// through their bones (arms down, sitting, lying, swimming, walking): no Animator, no colliders, no allocations.
    /// Groups (families, shoppers) walk their lane back and forth; limbs only animate near the camera.
    /// </summary>
    public sealed class PhuQuocCrowd : MonoBehaviour
    {
        public static bool Disabled;
        public enum Pose : byte { Stand, Walk, Swim, Sit, Lie, Wave }
        public const float ArmDown = 72f, NearDistance = 90f;

        [SerializeField] private Transform[] people = new Transform[0];
        [SerializeField] private Pose[] poses = new Pose[0];
        [SerializeField] private Transform[] groups = new Transform[0];
        [SerializeField] private Vector3[] groupFrom = new Vector3[0], groupTo = new Vector3[0];
        [SerializeField] private float[] groupSpeed = new float[0];

        public int PeopleCount => people.Length;
        public int GroupCount => groups.Length;
        public int Count(Pose p) { int n = 0; foreach (var q in poses) if (q == p) n++; return n; }
        public Transform Person(int i) => people[i];
        public Pose PoseOf(int i) => poses[i];

        public void Configure(Transform[] persons, Pose[] personPoses, Transform[] groupRoots, Vector3[] from, Vector3[] to, float[] speed)
        {
            people = persons; poses = personPoses; groups = groupRoots; groupFrom = from; groupTo = to; groupSpeed = speed;
        }

        private struct Rig { public Transform legL, legR, armL, armR, model; public float baseY, phase; }
        private Rig[] _rig;
        private bool[] _near;
        private float[] _t, _dir;
        private float _cullTimer;

        public static Transform Bone(Transform person, string name)
        {
            foreach (var t in person.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>Sets the limbs for a pose at animation phase s (−1…1). Used by the builder (static pose) and at runtime.</summary>
        public static void ApplyPose(Transform legL, Transform legR, Transform armL, Transform armR, Pose pose, float s, float wave = 0f)
        {
            Quaternion lL = Quaternion.identity, lR = Quaternion.identity;
            Quaternion aL = Quaternion.Euler(0f, 0f, ArmDown), aR = Quaternion.Euler(0f, 0f, -ArmDown);
            switch (pose)
            {
                case Pose.Walk:
                    lL = Quaternion.Euler(28f * s, 0f, 0f); lR = Quaternion.Euler(-28f * s, 0f, 0f);
                    aL = Quaternion.Euler(-24f * s, 0f, ArmDown); aR = Quaternion.Euler(24f * s, 0f, -ArmDown);
                    break;
                case Pose.Sit:
                    lL = lR = Quaternion.Euler(-90f, 0f, 0f);
                    aL = Quaternion.Euler(-30f, 0f, ArmDown - 8f); aR = Quaternion.Euler(-30f, 0f, -ArmDown + 8f);
                    break;
                case Pose.Lie:
                    aL = Quaternion.Euler(0f, 0f, 20f); aR = Quaternion.Euler(0f, 0f, -20f);
                    break;
                case Pose.Swim:
                    lL = Quaternion.Euler(18f * s, 0f, 0f); lR = Quaternion.Euler(-18f * s, 0f, 0f);
                    aL = Quaternion.Euler(-40f * s, 0f, 25f); aR = Quaternion.Euler(40f * s, 0f, -25f);
                    break;
                case Pose.Wave:
                    aR = Quaternion.Euler(0f, 0f, 55f + 25f * wave);
                    break;
                default:
                    aL = Quaternion.Euler(0f, 0f, ArmDown - 3f * s); aR = Quaternion.Euler(0f, 0f, -ArmDown + 3f * s);
                    break;
            }
            if (legL != null) legL.localRotation = lL;
            if (legR != null) legR.localRotation = lR;
            if (armL != null) armL.localRotation = aL;
            if (armR != null) armR.localRotation = aR;
        }

        private void Start()
        {
            if (Disabled) { enabled = false; return; }
            _rig = new Rig[people.Length];
            _near = new bool[people.Length];
            for (int i = 0; i < people.Length; i++)
            {
                var p = people[i];
                if (p == null) continue;
                var model = p.childCount > 0 ? p.GetChild(0) : p;
                _rig[i] = new Rig
                {
                    legL = Bone(p, "leg-left"), legR = Bone(p, "leg-right"), armL = Bone(p, "arm-left"), armR = Bone(p, "arm-right"),
                    model = model, baseY = model.localPosition.y, phase = i * 1.73f,
                };
            }
            _t = new float[groups.Length]; _dir = new float[groups.Length];
            for (int g = 0; g < groups.Length; g++) { _t[g] = Mathf.Repeat(g * 0.37f, 1f); _dir[g] = g % 2 == 0 ? 1f : -1f; }
            _cullTimer = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime, time = Time.time;
            for (int g = 0; g < groups.Length; g++)
            {
                var root = groups[g];
                if (root == null) continue;
                Vector3 a = groupFrom[g], b = groupTo[g];
                float len = Mathf.Max(1f, Vector3.Distance(a, b));
                _t[g] += _dir[g] * groupSpeed[g] * dt / len;
                if (_t[g] > 1f) { _t[g] = 1f; _dir[g] = -1f; }
                else if (_t[g] < 0f) { _t[g] = 0f; _dir[g] = 1f; }
                root.localPosition = Vector3.Lerp(a, b, _t[g]);
                var fwd = (b - a) * _dir[g]; fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f) root.localRotation = Quaternion.Slerp(root.localRotation, Quaternion.LookRotation(fwd), Mathf.Min(1f, dt * 4f));
            }

            _cullTimer -= dt;
            if (_cullTimer <= 0f)
            {
                _cullTimer = 0.4f;
                var cam = Camera.main;
                Vector3 c = cam != null ? cam.transform.position : transform.position;
                float r2 = NearDistance * NearDistance;
                for (int i = 0; i < people.Length; i++) _near[i] = people[i] != null && (people[i].position - c).sqrMagnitude < r2;
            }

            for (int i = 0; i < people.Length; i++)
            {
                if (!_near[i]) continue;
                var r = _rig[i];
                var pose = poses[i];
                if (pose == Pose.Sit || pose == Pose.Lie) continue; // static, set at build time
                float s = pose == Pose.Walk ? Mathf.Sin(time * 7f + r.phase) : Mathf.Sin(time * 1.6f + r.phase);
                ApplyPose(r.legL, r.legR, r.armL, r.armR, pose, s, Mathf.Sin(time * 6f + r.phase));
                if (r.model == null) continue;
                float lift = pose == Pose.Walk ? Mathf.Abs(s) * 0.05f : pose == Pose.Swim ? s * 0.07f : 0f;
                var lp = r.model.localPosition; lp.y = r.baseY + lift; r.model.localPosition = lp;
            }
        }
    }
}
