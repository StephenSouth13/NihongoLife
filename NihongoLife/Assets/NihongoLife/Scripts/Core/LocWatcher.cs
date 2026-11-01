using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>Holds the redraw callbacks of one GameObject (see <see cref="Loc.Watch"/>).</summary>
    [DisallowMultipleComponent]
    public sealed class LocWatcher : MonoBehaviour
    {
        private readonly List<Action> _redraws = new List<Action>();
        private bool _subscribed;

        // Subscribed on Add, not Awake: a screen built while hidden (inactive) must still redraw.
        public void Add(Action redraw)
        {
            _redraws.Add(redraw);
            if (!_subscribed) { Loc.Changed += Redraw; _subscribed = true; }
        }

        private void OnDestroy() { Loc.Changed -= Redraw; _subscribed = false; }

        private void Redraw()
        {
            if (this == null) { Loc.Changed -= Redraw; return; } // destroyed without OnDestroy (never activated)
            for (int i = 0; i < _redraws.Count; i++)
            {
                try { _redraws[i](); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }
    }
}
