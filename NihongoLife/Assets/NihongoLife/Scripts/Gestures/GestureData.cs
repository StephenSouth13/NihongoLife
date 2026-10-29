using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Gestures
{
    // ---------- Data: Resources/Animation/gestures.json (written by tools, read here) ----------

    /// <summary>One key of a channel: value (x, y, z) at time t (seconds of the gesture).</summary>
    [Serializable] public sealed class GKey { public float t, x, y, z; }

    /// <summary>A sine added to a channel: amplitude per axis, frequency in Hz, phase in cycles.</summary>
    [Serializable] public sealed class GOsc { public float ax, ay, az, f, ph; }

    /// <summary>
    /// One animated quantity of a gesture. Names: rh / lh = hand target relative to its shoulder in arm lengths
    /// (x outward, y up, z forward); rf / lf = foot offset and hp = hips offset in leg lengths; hr / sp / hd = hips,
    /// spine and head rotation in degrees (x pitch forward, y turn right, z lean right); p0..p3 = rotation of the
    /// gesture's props (degrees, additive to their base rotation).
    /// </summary>
    [Serializable]
    public sealed class GChannel
    {
        public string n;
        public GKey[] k = Array.Empty<GKey>();
        public GOsc[] o = Array.Empty<GOsc>();

        /// <summary>Value at gesture time <paramref name="t"/>. Keys ease (smoothstep) between each other and wrap when looping.</summary>
        public Vector3 Eval(float t, float dur, bool loop)
        {
            var v = Vector3.zero;
            if (k.Length > 0)
            {
                float tt = loop && dur > 0f ? Mathf.Repeat(t, dur) : t;
                int last = k.Length - 1;
                if (tt <= k[0].t) v = V(k[0]);
                else if (tt >= k[last].t)
                {
                    if (loop && dur > k[last].t + 1e-4f)
                        v = Vector3.Lerp(V(k[last]), V(k[0]), Smooth((tt - k[last].t) / (dur - k[last].t)));
                    else v = V(k[last]);
                }
                else
                {
                    int i = 0;
                    while (i < last - 1 && tt >= k[i + 1].t) i++;
                    float span = Mathf.Max(1e-5f, k[i + 1].t - k[i].t);
                    v = Vector3.Lerp(V(k[i]), V(k[i + 1]), Smooth((tt - k[i].t) / span));
                }
            }
            for (int i = 0; i < o.Length; i++)
            {
                float s = Mathf.Sin(6.2831853f * (o[i].f * t + o[i].ph));
                v += new Vector3(o[i].ax, o[i].ay, o[i].az) * s;
            }
            return v;
        }

        /// <summary>Largest distance of any value of the channel from the origin (keys plus every sine at its peak).</summary>
        public float MaxMagnitude()
        {
            float key = 0f;
            foreach (var x in k) key = Mathf.Max(key, V(x).magnitude);
            float osc = 0f;
            foreach (var x in o) osc += new Vector3(x.ax, x.ay, x.az).magnitude;
            return key + osc;
        }

        private static Vector3 V(GKey key) => new Vector3(key.x, key.y, key.z);
        private static float Smooth(float a) { a = Mathf.Clamp01(a); return a * a * (3f - 2f * a); }
    }

    /// <summary>One primitive of a prop (a handle, a blade, a key). Position, rotation (degrees) and size in metres of a person.</summary>
    [Serializable]
    public sealed class GPiece
    {
        public string shape = "cube", hex = "#FFFFFF";
        public float[] pos = new float[3], rot = new float[3], size = { 1f, 1f, 1f };
    }

    /// <summary>
    /// A held or placed object built from primitives. hand R / L: follows that hand (rotation fixed to the body, plus the
    /// gesture's p channel); A: held by both hands, its Y axis running from the left hand to the right hand (a broom, a hoe);
    /// W: stands in the world in front of the character (a chair, a keyboard, a cutting board).
    /// pos and rot give the offset and base rotation.
    /// </summary>
    [Serializable]
    public sealed class GProp
    {
        public string id, hand = "R";
        public float[] pos = new float[3], rot = new float[3], tip;
        public GPiece[] pieces = Array.Empty<GPiece>();
    }

    /// <summary>A prop used by a gesture: from time ts to te (seconds; te &lt; 0 = until the end).</summary>
    [Serializable] public sealed class GPropUse { public string id; public float ts, te = -1f; }

    [Serializable]
    public sealed class GestureDef
    {
        public string id, cat, vi, en, ja, ru, fx;
        public float dur = 1f, bpm, fin = 0.25f, fout = 0.3f;
        public bool loop;
        /// <summary>A looping gesture whose keys are an intro: the last key is held instead of wrapping to the first.</summary>
        public bool hold;
        public GChannel[] ch = Array.Empty<GChannel>();
        public GPropUse[] props = Array.Empty<GPropUse>();

        public string Name => Loc.Code switch { "en" => en, "ja" => ja, "ru" => string.IsNullOrEmpty(ru) ? en : ru, _ => vi };

        public GChannel Channel(string name)
        {
            foreach (var c in ch) if (c.n == name) return c;
            return null;
        }
    }

    [Serializable]
    public sealed class GestureFile
    {
        public string schema;
        public GProp[] props = Array.Empty<GProp>();
        public GestureDef[] gestures = Array.Empty<GestureDef>();
    }

    /// <summary>The gesture library: loads gestures.json once.</summary>
    public static class GestureLibrary
    {
        public const string Path = "Animation/gestures";
        private static GestureFile _file;
        private static Dictionary<string, GestureDef> _byId;
        private static Dictionary<string, GProp> _props;

        public static GestureFile File
        {
            get
            {
                if (_file != null) return _file;
                var asset = Resources.Load<TextAsset>(Path);
                _file = asset != null ? JsonUtility.FromJson<GestureFile>(asset.text) ?? new GestureFile() : new GestureFile();
                _byId = _file.gestures.Where(g => !string.IsNullOrEmpty(g.id)).GroupBy(g => g.id).ToDictionary(g => g.Key, g => g.First());
                _props = _file.props.Where(p => !string.IsNullOrEmpty(p.id)).GroupBy(p => p.id).ToDictionary(g => g.Key, g => g.First());
                return _file;
            }
        }

        public static IReadOnlyList<GestureDef> All => File.gestures;
        public static GestureDef Find(string id) { var _ = File; return id != null && _byId.TryGetValue(id, out var g) ? g : null; }
        public static GProp Prop(string id) { var _ = File; return id != null && _props.TryGetValue(id, out var p) ? p : null; }
        public static IEnumerable<GestureDef> InCategory(string cat) => File.gestures.Where(g => g.cat == cat);
        public static void Reset() { _file = null; _byId = null; _props = null; }

        /// <summary>Adds (or replaces) a gesture at runtime: generated gestures and tests.</summary>
        public static void Register(GestureDef def)
        {
            var file = File;
            file.gestures = file.gestures.Where(g => g.id != def.id).Concat(new[] { def }).ToArray();
            _byId[def.id] = def;
        }
    }
}