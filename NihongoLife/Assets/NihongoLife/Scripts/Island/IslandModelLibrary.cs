using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>Model prefabs for crop stages, produce and tools (filled by IslandBuilder; no runtime asset lookups).</summary>
    public sealed class IslandModelLibrary : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string name; public GameObject prefab; }
        [SerializeField] private List<Entry> entries = new();
        public static IslandModelLibrary Instance { get; private set; }

        public void Set(List<Entry> list) => entries = list;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public GameObject Find(string name)
        {
            foreach (var e in entries) if (e.name == name) return e.prefab;
            return null;
        }
    }

    // ─────────── Animals ───────────
}
