using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NihongoLife.UI
{
    /// <summary>
    /// The one place that decides what Esc does. Every closable overlay (dialogue, shop, bag, map, popups,
    /// mini-game, ticket machine, settings…) registers once with "am I open?" and "close me". Esc closes only the
    /// overlay that opened most recently; with nothing open, Esc does nothing (Settings opens with O instead).
    /// Overlays never poll the Escape key themselves, so one key press can no longer close a dialogue and open
    /// Settings in the same frame. Entries whose owner is destroyed drop out automatically.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class UiModalStack : MonoBehaviour
    {
        private sealed class Entry
        {
            public UnityEngine.Object Owner;
            public Func<bool> IsOpen;
            public Action Close;
            public string Name;
            public bool Immersive;
            public bool WasOpen;
            public long OpenedOrder;
        }

        private static readonly List<Entry> Entries = new();
        private static UiModalStack _instance;
        private static long _order;

        /// <summary>Frame on which Esc was consumed by an overlay (for legacy code that still reads Pause).</summary>
        public static int EscapeHandledFrame { get; private set; } = -1;

        /// <summary>True while any registered overlay is open.</summary>
        public static bool AnyOpen
        {
            get
            {
                foreach (var e in Entries)
                    if (e.Owner != null && SafeIsOpen(e)) return true;
                return false;
            }
        }

        /// <summary>True while a full-screen activity (exam, mini-game) is open: the HUD steps aside.</summary>
        public static bool ImmersiveOpen
        {
            get
            {
                foreach (var e in Entries)
                    if (e.Immersive && e.Owner != null && SafeIsOpen(e)) return true;
                return false;
            }
        }

        /// <summary>Name of the overlay Esc would close now (null when none) — for tests and debugging.</summary>
        public static string TopName => Top()?.Name;

        /// <summary>True while a text field has keyboard focus, so single-letter shortcuts (O, B, M…) must not fire.</summary>
        public static bool IsTyping
        {
            get
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selected == null) return false;
                var tmp = selected.GetComponent<TMP_InputField>();
                if (tmp != null && tmp.isFocused) return true;
                var legacy = selected.GetComponent<UnityEngine.UI.InputField>();
                return legacy != null && legacy.isFocused;
            }
        }

        public static void Register(UnityEngine.Object owner, Func<bool> isOpen, Action close, string name = null, bool immersive = false)
        {
            if (owner == null || isOpen == null || close == null) return;
            Ensure();
            Entries.RemoveAll(e => e.Owner == owner && e.Name == (name ?? owner.name));
            Entries.Add(new Entry { Owner = owner, IsOpen = isOpen, Close = close, Name = name ?? owner.name, Immersive = immersive });
        }

        /// <summary>Closes the most recently opened overlay; returns false when nothing was open.</summary>
        public static bool CloseTop()
        {
            var top = Top();
            if (top == null) return false;
            EscapeHandledFrame = Time.frameCount;
            try { top.Close(); }
            catch (Exception exception) { Debug.LogException(exception); }
            return true;
        }

        private static Entry Top()
        {
            Entry best = null;
            foreach (var e in Entries)
            {
                if (e.Owner == null || !SafeIsOpen(e)) continue;
                if (best == null || e.OpenedOrder > best.OpenedOrder) best = e;
            }
            return best;
        }

        private static bool SafeIsOpen(Entry e)
        {
            try { return e.IsOpen(); }
            catch (MissingReferenceException) { return false; }
            catch (NullReferenceException) { return false; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Entries.Clear();
            _instance = null;
            _order = 0;
            EscapeHandledFrame = -1;
        }

        private static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("UiModalStack");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UiModalStack>();
        }

        private void Update()
        {
            Entries.RemoveAll(e => e.Owner == null);
            foreach (var e in Entries)
            {
                bool open = SafeIsOpen(e);
                if (open && !e.WasOpen) e.OpenedOrder = ++_order;
                e.WasOpen = open;
            }
            var input = GameInputService.Instance;
            bool escape = input != null ? input.WasPressed(GameInputId.Pause)
                : Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (escape && EscapeHandledFrame != Time.frameCount) CloseTop();
        }
    }
}
