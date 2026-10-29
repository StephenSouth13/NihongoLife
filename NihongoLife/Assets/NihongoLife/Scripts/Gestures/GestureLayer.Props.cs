using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Gestures
{
    /// <summary>Props (a watering can, a broom, a keyboard) and small effects (water, music notes, steam) of the playing gesture.</summary>
    public sealed partial class GestureLayer
    {
        private sealed class PropInst
        {
            public GProp def; public GameObject go; public int index = -1; public float ts, te = float.MaxValue, scale;
            public Quaternion last = Quaternion.identity;
        }

        private sealed class Fx { public Transform t; public Vector3 vel; public float life, age, size; public int kind; public bool alive; }

        private readonly List<PropInst> _props = new List<PropInst>();
        private readonly List<Fx> _fx = new List<Fx>();
        private string _fxKinds = "";
        private float _fxClock, _fxEmit;
        private bool _sparked;
        private static Material _baseMaterial;
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly System.Random Rng = new System.Random(7);

        /// <summary>The live prop instances of the playing gesture (index in the gesture's prop list; tests read their positions).</summary>
        public IEnumerable<(string id, Transform transform, float scale)> ActiveProps
        {
            get { foreach (var p in _props) if (p.go != null) yield return (p.def.id, p.go.transform, p.scale); }
        }

        public int ActiveFx { get { int n = 0; foreach (var f in _fx) if (f.alive) n++; return n; } }
        private float PropScale => Mathf.Max(0.2f, _armLen / 0.55f);

        // ------------------------------------------------------------------ props

        private bool _hideProps;

        /// <summary>True while a tool of the game is held instead (the gesture's own props are not shown).</summary>
        public void HideProps(bool hide)
        {
            _hideProps = hide;
            if (hide) foreach (var p in _props) p.index = -1;
        }

        private void BindProps(GestureDef g)
        {
            foreach (var p in _props) p.index = -1;
            if (g != null && !_hideProps)
            {
                for (int i = 0; i < g.props.Length; i++)
                {
                    var use = g.props[i];
                    var def = GestureLibrary.Prop(use.id);
                    if (def == null) continue;
                    var inst = _props.Find(p => p.index < 0 && p.def.id == use.id && p.go != null);
                    if (inst == null)
                    {
                        inst = new PropInst { def = def, go = BuildProp(def, gameObject.layer) };
                        inst.go.SetActive(false);
                        _props.Add(inst);
                    }
                    inst.index = i; inst.ts = use.ts; inst.te = use.te < 0f ? float.MaxValue : use.te;
                }
            }
        }

        private void UpdateProps(float dt, bool posed)
        {
            if (_props.Count == 0) return;
            float sc = PropScale;
            var frame = transform;
            for (int i = _props.Count - 1; i >= 0; i--)
            {
                var p = _props[i];
                if (p.go == null) { _props.RemoveAt(i); continue; }
                bool wanted = p.index >= 0 && _cur != null && _t >= p.ts && _t < p.te && _w > 0.02f;
                p.scale = Mathf.MoveTowards(p.scale, wanted ? 1f : 0f, dt / 0.16f);
                if (p.index < 0 && p.scale <= 0.001f) { Destroy(p.go); _props.RemoveAt(i); continue; }
                bool visible = p.scale > 0.001f;
                if (p.go.activeSelf != visible) p.go.SetActive(visible);
                if (!visible || !_resolved || _hips == null) continue;

                Vector3 pch = Vector3.zero;
                if (p.index >= 0 && p.index < 4) { var c = _ch[P0 + p.index]; pch = c.value * c.inf; }
                var baseRot = Quaternion.Euler(V3(p.def.rot));
                var extra = Euler(pch, frame.right, frame.up, frame.forward);
                Vector3 offset = frame.rotation * (V3(p.def.pos) * sc);
                Vector3 pos; Quaternion rot;
                switch (p.def.hand)
                {
                    case "L": pos = _arm[5].position + offset; rot = extra * frame.rotation * baseRot; break;
                    case "W": pos = frame.position + offset; rot = extra * frame.rotation * baseRot; break;
                    case "A":
                        {
                            Vector3 r = _arm[2].position, l = _arm[5].position, aim = r - l;
                            if (aim.sqrMagnitude > 1e-4f)
                            {
                                Vector3 f = Vector3.ProjectOnPlane(frame.forward, aim);
                                if (f.sqrMagnitude < 1e-4f) f = Vector3.ProjectOnPlane(frame.right, aim);
                                p.last = Quaternion.LookRotation(f, aim);
                            }
                            rot = extra * p.last * baseRot; pos = (r + l) * 0.5f + offset; break;
                        }
                    default: pos = _arm[2].position + offset; rot = extra * frame.rotation * baseRot; break;
                }
                p.go.transform.SetPositionAndRotation(pos, rot);
                p.go.transform.localScale = Vector3.one * (sc * Smooth(p.scale));
            }
        }

        private void ClearProps(bool destroy)
        {
            foreach (var p in _props) if (p.go != null) { if (Application.isPlaying) Destroy(p.go); else DestroyImmediate(p.go); }
            _props.Clear();
        }

        private static Vector3 V3(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;

        private static GameObject BuildProp(GProp def, int layer)
        {
            var root = new GameObject("GestureProp_" + def.id) { layer = layer };
            foreach (var piece in def.pieces)
            {
                PrimitiveType type = piece.shape switch { "sphere" => PrimitiveType.Sphere, "cyl" => PrimitiveType.Cylinder, "capsule" => PrimitiveType.Capsule, _ => PrimitiveType.Cube };
                var go = GameObject.CreatePrimitive(type);
                go.layer = layer;
                var col = go.GetComponent<Collider>();
                if (col != null) { col.enabled = false; if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
                var tr = go.transform;
                tr.SetParent(root.transform, false);
                tr.localPosition = V3(piece.pos);
                tr.localRotation = Quaternion.Euler(V3(piece.rot));
                var size = V3(piece.size);
                tr.localScale = type == PrimitiveType.Cylinder || type == PrimitiveType.Capsule ? new Vector3(size.x, size.y * 0.5f, size.z) : size;
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = MaterialFor(piece.hex);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return root;
        }

        private static Material MaterialFor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFF";
            if (Materials.TryGetValue(hex, out var cached) && cached != null) return cached;
            if (_baseMaterial == null)
            {
                _baseMaterial = Resources.Load<Material>("Animation/NL_GestureProp");
                if (_baseMaterial == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                    _baseMaterial = new Material(shader) { name = "NL_GestureProp (runtime)" };
                }
            }
            var m = new Material(_baseMaterial) { name = "NL_GestureProp_" + hex };
            if (!ColorUtility.TryParseHtmlString(hex, out var color)) color = Color.white;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            Materials[hex] = m;
            return m;
        }

        // ------------------------------------------------------------------ effects

        private void StartFx(GestureDef def)
        {
            _fxKinds = def != null && !string.IsNullOrEmpty(def.fx) ? def.fx : "";
            _fxClock = 0f; _fxEmit = 0f; _sparked = false;
        }

        private void EndFx() => _fxKinds = "";

        private void ClearFx()
        {
            foreach (var f in _fx) if (f.t != null) { if (Application.isPlaying) Destroy(f.t.gameObject); else DestroyImmediate(f.t.gameObject); }
            _fx.Clear();
        }

        private Vector3 FxOrigin(bool tip)
        {
            if (tip) foreach (var p in _props)
                if (p.go != null && p.go.activeSelf && p.index >= 0 && p.def.tip != null && p.def.tip.Length >= 3)
                    return p.go.transform.TransformPoint(V3(p.def.tip));
            return _head != null ? _head.position + transform.up * 0.2f : transform.position + Vector3.up * 1.6f;
        }

        private Quaternion FxDirection()
        {
            foreach (var p in _props) if (p.go != null && p.index >= 0 && p.def.tip != null) return p.go.transform.rotation;
            return transform.rotation;
        }

        private void UpdateFx(float dt)
        {
            if (dt > 0f && _cur != null && _fxKinds.Length > 0 && _w > 0.3f)
            {
                _fxClock += dt;
                float rate = _bpm > 0f && _cur.bpm > 0f ? _bpm / _cur.bpm : 1f;
                if (_fxKinds.Contains("water") && _ch[P0].value.x > 18f)
                    Emit(0, FxOrigin(true), FxDirection() * new Vector3(0f, -0.2f, 1.1f) + RandomVec(0.12f), 0.9f, 0.022f, dt, 38f);
                if (_fxKinds.Contains("notes"))
                    Emit(1, FxOrigin(false) + transform.right * Rand(-0.2f, 0.2f), transform.up * 0.45f + transform.right * Rand(-0.25f, 0.25f), 1.7f, 0.05f, dt, 2.4f * rate);
                if (_fxKinds.Contains("steam"))
                    Emit(2, FxOrigin(true), transform.up * 0.35f + RandomVec(0.05f), 1.0f, 0.04f, dt, 7f);
                if (_fxKinds.Contains("spark") && !_sparked && _t >= _cur.dur * 0.42f)
                {
                    _sparked = true;
                    for (int i = 0; i < 12; i++) Spawn(3, FxOrigin(true), RandomVec(1.1f) + transform.up * 0.5f, 0.55f, 0.03f);
                }
            }
            for (int i = _fx.Count - 1; i >= 0; i--)
            {
                var f = _fx[i];
                if (!f.alive) continue;
                f.age += dt;
                if (f.age >= f.life || f.t == null) { Kill(f); continue; }
                if (f.kind == 0) f.vel += Vector3.down * 9.8f * dt;
                else if (f.kind == 1) f.vel += transform.right * Mathf.Sin(f.age * 5f + f.size * 90f) * 0.9f * dt;
                else if (f.kind == 3) f.vel += Vector3.down * 3f * dt;
                f.t.position += f.vel * dt;
                float k = f.age / f.life;
                f.t.localScale = Vector3.one * f.size * (f.kind == 2 ? 1f + k : 1f - k * k);
                if (f.kind == 0 && f.t.position.y < transform.position.y) Kill(f);
            }
        }

        private float _emitAcc0, _emitAcc1, _emitAcc2;

        private void Emit(int kind, Vector3 pos, Vector3 vel, float life, float size, float dt, float perSecond)
        {
            ref float acc = ref (kind == 0 ? ref _emitAcc0 : ref (kind == 1 ? ref _emitAcc1 : ref _emitAcc2));
            acc += dt * perSecond;
            while (acc >= 1f) { acc -= 1f; Spawn(kind, pos, vel, life, size); }
        }

        private void Spawn(int kind, Vector3 pos, Vector3 vel, float life, float size)
        {
            Fx f = null;
            foreach (var x in _fx) if (!x.alive) { f = x; break; }
            if (f == null)
            {
                if (_fx.Count >= 56) return;
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "GestureFx"; go.layer = gameObject.layer;
                var col = go.GetComponent<Collider>(); col.enabled = false; Destroy(col);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                f = new Fx { t = go.transform };
                _fx.Add(f);
            }
            string hex = kind switch { 0 => "#7CC8FF", 1 => (Rng.Next(4)) switch { 0 => "#FFD86B", 1 => "#FF9FB8", 2 => "#9BE7B0", _ => "#9DB8FF" }, 2 => "#F2F2F2", _ => "#FFE066" };
            f.t.GetComponent<Renderer>().sharedMaterial = MaterialFor(hex);
            f.kind = kind; f.vel = vel; f.life = life; f.age = 0f; f.size = size; f.alive = true;
            f.t.position = pos; f.t.localScale = Vector3.one * size;
            f.t.gameObject.SetActive(true);
        }

        private static void Kill(Fx f) { f.alive = false; if (f.t != null) f.t.gameObject.SetActive(false); }
        private static float Rand(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
        private static Vector3 RandomVec(float m) => new Vector3(Rand(-m, m), Rand(-m, m), Rand(-m, m));
    }
}