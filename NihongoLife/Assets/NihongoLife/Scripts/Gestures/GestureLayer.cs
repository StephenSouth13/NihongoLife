using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Gestures
{
    public enum GestureLimb { RightHand = 0, LeftHand = 1 }

    /// <summary>
    /// Procedural body-language layer on a humanoid character (every Kids model): after the Animator has posed the rig, it
    /// bends the spine and head, moves the hips and solves arm and leg IK toward the targets of the playing gesture
    /// (<see cref="GestureLibrary"/>), spawns the gesture's props and effects, and blends everything in and out with
    /// eased weights and spring smoothing, so a wave, a bow, a chop or a dance step starts and ends without a pop and
    /// one gesture crossfades into the next. Targets are relative to the body (shoulder, arm length, leg length), so the
    /// same gesture fits every character size. Idle cost is nil: with no gesture and no residual weight it returns at once.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed partial class GestureLayer : MonoBehaviour
    {
        // Channel indices.
        private const int RH = 0, LH = 1, RF = 2, LF = 3, HP = 4, HR = 5, SP = 6, HD = 7, P0 = 8, Count = 12;
        private static readonly string[] Names = { "rh", "lh", "rf", "lf", "hp", "hr", "sp", "hd", "p0", "p1", "p2", "p3" };

        private sealed class Chan { public Vector3 value, vel, from; public float inf, infVel; }

        private readonly Chan[] _ch = new Chan[Count];
        private GestureDef _cur;
        private Vector3 _lastPos;
        private float _xf = 99f, _xfDur = 0.3f, _t, _w, _carry, _speed = 1f, _bpm, _stopT, _stopFade;
        private bool _stopping, _manual;
        private readonly List<string> _queue = new List<string>();
        private readonly Vector3[] _pulse = new Vector3[2];

        private Animator _animator;
        private bool _resolved;
        private Transform _hips, _spine, _chest, _upperChest, _neck, _head;
        private Transform[] _arm = new Transform[6], _leg = new Transform[6]; // upper, lower, end for right [0..2] then left [3..5]
        private float _armLen, _legLen;

        /// <summary>Raised when a gesture ends (id of the gesture); not raised when another gesture replaces it.</summary>
        public event Action<string> Finished;

        public string Current => _cur != null ? _cur.id : null;
        public bool IsPlaying => _cur != null;
        /// <summary>Overall weight of the playing gesture, 0..1.</summary>
        public float Weight => _w;
        public float Time01 => _cur != null && _cur.dur > 0f ? Mathf.Clamp01(_t / _cur.dur) : 0f;
        /// <summary>Largest influence of any channel: above 0 while the layer still changes the pose.</summary>
        public float Influence { get { float m = 0f; foreach (var c in _ch) if (c != null && c.inf > m) m = c.inf; return m; } }
        public float ArmLength => _armLen;
        public float LegLength => _legLen;
        public Animator Animator { get { Resolve(); return _animator; } }
        public bool Manual { get => _manual; set => _manual = value; }

        /// <summary>The layer of <paramref name="go"/> (added on first use).</summary>
        public static GestureLayer On(GameObject go)
        {
            var layer = go.GetComponent<GestureLayer>();
            return layer != null ? layer : go.AddComponent<GestureLayer>();
        }

        private void Awake() { for (int i = 0; i < Count; i++) _ch[i] = new Chan(); }

        private void OnDisable() { ClearProps(true); ClearFx(); }
        private void OnDestroy() { ClearProps(true); ClearFx(); }

        // ------------------------------------------------------------------ API

        /// <summary>Plays gesture <paramref name="id"/>, crossfading from the one that plays; false when it is unknown or the rig is not humanoid.</summary>
        public bool Play(string id, float speed = 1f)
        {
            var def = GestureLibrary.Find(id);
            if (def == null || !Resolve()) return false;
            _queue.Clear();
            Begin(def, speed);
            return true;
        }

        /// <summary>Plays the gestures one after the other, each crossfading into the next (a looping gesture ends the sequence).</summary>
        public bool PlaySequence(params string[] ids)
        {
            if (ids == null || ids.Length == 0) return false;
            if (!Play(ids[0])) return false;
            for (int i = 1; i < ids.Length; i++) if (GestureLibrary.Find(ids[i]) != null) _queue.Add(ids[i]);
            return true;
        }

        /// <summary>Fades the gesture out over <paramref name="fade"/> seconds (negative = its own fade-out; 0 = at once).</summary>
        public void Stop(float fade = -1f)
        {
            _queue.Clear();
            if (_cur == null) return;
            _stopping = true; _stopT = 0f;
            _stopFade = fade < 0f ? _cur.fout : fade;
            if (_stopFade <= 0f) End();
        }

        /// <summary>The body was just placed (seated at an instrument, teleported): measure "walking away" from here, so
        /// the next gesture is not ended by the jump. Not done in Play: callers that replay a gesture often rely on the
        /// walk check seeing the real motion between frames.</summary>
        public void ResetMotion() => _lastPos = transform.position;

        /// <summary>Tempo of the playing dance or instrument gesture in beats per minute (0 = the gesture's own).</summary>
        public void SetBpm(float bpm) => _bpm = Mathf.Max(0f, bpm);

        /// <summary>A short press of a hand (a key, a drum, a pad): the hand dips and returns. Call it on each note a simulator plays.</summary>
        public void Pulse(GestureLimb limb, float strength = 1f)
        {
            _pulse[(int)limb] += new Vector3(0f, -0.14f, 0.02f) * Mathf.Clamp(strength, 0f, 2f);
        }

        /// <summary>Advances the layer by <paramref name="dt"/> and poses the rig (called every LateUpdate; tests call it by hand with <see cref="Manual"/>).</summary>
        public void Step(float dt)
        {
            if (!Resolve()) return;
            Advance(dt);
            if (_cur == null && !HasResidual()) { UpdateProps(dt, false); UpdateFx(dt); return; }
            Pose(dt);
            UpdateProps(dt, true);
            UpdateFx(dt);
        }

        private void LateUpdate() { if (!_manual) Step(Time.deltaTime); }

        // ------------------------------------------------------------------ playback

        private void Begin(GestureDef def, float speed)
        {
            _carry = _cur != null ? _w : (HasResidual() ? Mathf.Clamp01(Influence) : 0f);
            // The new gesture's targets blend in from where each channel is now, so one gesture glides into the next.
            foreach (var c in _ch) if (c != null) c.from = c.value;
            _xf = _carry > 0.05f ? 0f : 99f; _xfDur = Mathf.Max(0.3f, def.fin);
            _cur = def; _t = 0f; _speed = Mathf.Max(0.05f, speed); _stopping = false; _stopT = 0f;
            _w = _carry;
            BindProps(def);
            StartFx(def);
        }

        private void End()
        {
            var id = _cur != null ? _cur.id : null;
            _cur = null; _w = 0f; _stopping = false; _queue.Clear();
            EndFx();
            BindProps(null);
            if (id != null) Finished?.Invoke(id);
        }

        private bool HasResidual()
        {
            foreach (var c in _ch) if (c != null && c.inf > 0.002f) return true;
            return false;
        }

        private static float Smooth(float a) { a = Mathf.Clamp01(a); return a * a * (3f - 2f * a); }

        private void Advance(float dt)
        {
            if (_cur == null) { _w = 0f; _lastPos = transform.position; return; }
            // Walking away ends a gesture (the body language belongs to standing people).
            if (dt > 0f && !_stopping)
            {
                float speed = (transform.position - _lastPos).magnitude / dt;
                if (speed > 0.7f && !_cur.id.StartsWith("carry")) Stop(0.2f);
            }
            _lastPos = transform.position;
            float rate = _speed * (_bpm > 0f && _cur.bpm > 0f ? _bpm / _cur.bpm : 1f);
            _t += dt * rate;
            _xf += dt;
            float tin = _cur.fin > 0f ? Smooth(_t / _cur.fin) : 1f;
            float win = Mathf.Max(tin, _carry * (1f - tin));
            float wout = 1f;
            if (!_cur.loop && _cur.fout > 0f) wout = Smooth((_cur.dur - _t) / _cur.fout);
            if (_stopping)
            {
                _stopT += dt;
                wout = Mathf.Min(wout, _stopFade > 0f ? 1f - Smooth(_stopT / _stopFade) : 0f);
            }
            _w = win * wout;

            if (!_cur.loop && _queue.Count > 0 && _t >= _cur.dur - Mathf.Max(0.05f, _cur.fout))
            {
                var next = GestureLibrary.Find(_queue[0]);
                _queue.RemoveAt(0);
                if (next != null) { var rest = new List<string>(_queue); Begin(next, _speed); _queue.AddRange(rest); return; }
            }
            bool done = _stopping ? _stopT >= _stopFade : (!_cur.loop && _t >= _cur.dur);
            if (done) End();
        }

        // ------------------------------------------------------------------ rig

        private bool Resolve()
        {
            if (_resolved) return _hips != null;
            _resolved = true;
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null || !_animator.isHuman) { _hips = null; return false; }
            Transform B(HumanBodyBones b) => _animator.GetBoneTransform(b);
            _hips = B(HumanBodyBones.Hips); _spine = B(HumanBodyBones.Spine); _chest = B(HumanBodyBones.Chest);
            _upperChest = B(HumanBodyBones.UpperChest); _neck = B(HumanBodyBones.Neck); _head = B(HumanBodyBones.Head);
            _arm[0] = B(HumanBodyBones.RightUpperArm); _arm[1] = B(HumanBodyBones.RightLowerArm); _arm[2] = B(HumanBodyBones.RightHand);
            _arm[3] = B(HumanBodyBones.LeftUpperArm); _arm[4] = B(HumanBodyBones.LeftLowerArm); _arm[5] = B(HumanBodyBones.LeftHand);
            _leg[0] = B(HumanBodyBones.RightUpperLeg); _leg[1] = B(HumanBodyBones.RightLowerLeg); _leg[2] = B(HumanBodyBones.RightFoot);
            _leg[3] = B(HumanBodyBones.LeftUpperLeg); _leg[4] = B(HumanBodyBones.LeftLowerLeg); _leg[5] = B(HumanBodyBones.LeftFoot);
            foreach (var t in _arm) if (t == null) { _hips = null; return false; }
            foreach (var t in _leg) if (t == null) { _hips = null; return false; }
            if (_hips == null || _spine == null || _head == null) { _hips = null; return false; }
            _armLen = (_arm[1].position - _arm[0].position).magnitude + (_arm[2].position - _arm[1].position).magnitude;
            _legLen = (_leg[1].position - _leg[0].position).magnitude + (_leg[2].position - _leg[1].position).magnitude;
            return true;
        }    }
}
