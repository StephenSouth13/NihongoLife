using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Lets one piece of UI build code serve both authoring and runtime. Inside a <see cref="Scope"/>:
    /// editor bake (not playing, scope listed in <see cref="BakeScopes"/>): <see cref="Make"/> creates objects, tags them with
    /// <see cref="UiBakeId"/> and the bake saves them into the scene. Runtime with the scope found in the scene: <see cref="Make"/>
    /// creates nothing and returns the authored object with the same creation order, so the build code only finds and hooks
    /// (texts, sprites, listeners) what the scene holds. Otherwise (for example a gameplay scene without a baked copy) it
    /// creates as before.
    /// </summary>
    public static class UiBind
    {
        private enum Mode { Create, Bake, Bind }

        private sealed class Frame
        {
            public Mode mode;
            public string scope;
            public int next;
            public List<GameObject> baked;
            public List<string> made;
            public int cursor, used;
        }

        private static readonly Stack<Frame> Frames = new Stack<Frame>();

        /// <summary>Scopes an editor bake should author; empty at runtime.</summary>
        public static readonly HashSet<string> BakeScopes = new HashSet<string>();

        /// <summary>Objects a bound scope still had to create (code and scene out of step); read by tests.</summary>
        public static readonly List<string> Misses = new List<string>();

        /// <summary>True while a scope hands back authored objects (code that rebuilds by destroying must skip that).</summary>
        public static bool Binding => Frames.Count > 0 && Frames.Peek().mode == Mode.Bind;

        /// <summary>Order trace (scope#order name) of the last builds, for comparing a bake with a runtime bind.</summary>
        public static readonly List<string> Trace = new List<string>();

        public static IDisposable Scope(Transform root, string scope)
        {
            var frame = new Frame { scope = scope, mode = Mode.Create };
            if (!Application.isPlaying && BakeScopes.Contains(scope)) frame.mode = Mode.Bake;
            else if (root != null)
            {
                var list = new List<UiBakeId>();
                foreach (var id in root.GetComponentsInChildren<UiBakeId>(true))
                    if (id.scope == scope) list.Add(id);
                if (list.Count > 0)
                {
                    list.Sort((x, y) => x.order.CompareTo(y.order));
                    frame.mode = Mode.Bind;
                    frame.baked = new List<GameObject>(list.Count);
                    frame.made = new List<string>(list.Count);
                    foreach (var id in list) { frame.baked.Add(id.gameObject); frame.made.Add(string.IsNullOrEmpty(id.made) ? id.gameObject.name : id.made); }
                }
            }
            Frames.Push(frame);
            return new Closer(frame);
        }

        private sealed class Closer : IDisposable
        {
            private Frame _frame;
            public Closer(Frame frame) { _frame = frame; }
            public void Dispose()
            {
                if (_frame == null) return;
                if (_frame.mode == Mode.Bind) Trace.Add(_frame.scope + ": bound " + _frame.used + " of " + _frame.baked.Count + " authored objects");
                _frame = null;
                if (Frames.Count > 0) Frames.Pop();
            }
        }

        /// <summary>Use instead of <c>new GameObject(name, types)</c> in UI build code.</summary>
        public static GameObject Make(string name, params Type[] components)
        {
            if (Frames.Count == 0) return new GameObject(name, components);
            var frame = Frames.Peek();
            int order = frame.next++;
            Trace.Add(frame.scope + "#" + order + " " + name);
            if (frame.mode == Mode.Bind)
            {
                // Next authored object made under the same name. Objects that idempotent helpers found by name instead of
                // making (the bake made them, the bind finds them) are simply stepped over.
                for (int i = frame.cursor; i < frame.baked.Count; i++)
                {
                    var found = frame.baked[i];
                    if (found == null || frame.made[i] != name) continue;
                    frame.cursor = i + 1;
                    frame.used++;
                    foreach (var type in components)
                        if (found.GetComponent(type) == null) found.AddComponent(type);
                    return found;
                }
                Misses.Add(frame.scope + "#" + order + " " + name);
                return new GameObject(name, components);
            }
            var go = new GameObject(name, components);
            if (frame.mode == Mode.Bake)
            {
                var id = go.AddComponent<UiBakeId>();
                id.scope = frame.scope;
                id.order = order;
                id.made = name;
            }
            return go;
        }

        /// <summary>GetComponent, or AddComponent when missing (authored objects already carry their components).</summary>
        public static T Need<T>(this GameObject go) where T : Component
        {
            // Exact type: an Outline is also a Shadow, and a widget asking for both must still get two components.
            foreach (var c in go.GetComponents<T>())
                if (c.GetType() == typeof(T)) return c;
            return go.AddComponent<T>();
        }

        public static T Need<T>(this Component c) where T : Component => c.gameObject.Need<T>();
    }
}