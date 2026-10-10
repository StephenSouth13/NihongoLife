using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Progression
{
    /// <summary>
    /// Short-lived state of the shift in progress (what the player is carrying, which shelves are done, what each
    /// table ordered). Keyed by the quest's acceptance time, so a new shift always starts clean. Objective counts
    /// themselves are saved by QuestService; this only holds the in-between steps (a carried box is not saved).
    /// </summary>
    public static class JobShift
    {
        private static string _shiftKey;
        private static readonly HashSet<string> Done = new();
        private static readonly Dictionary<string, string> TableOrders = new();
        private static GameObject _carryProp;

        public static string CarryKind { get; private set; }
        public static string CarryId { get; private set; }
        public static string CarryLabel { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _shiftKey = null; Done.Clear(); TableOrders.Clear(); CarryKind = CarryId = CarryLabel = null; _carryProp = null; }

        /// <summary>Clears the in-between state when a different shift (or no shift) is active.</summary>
        public static void Sync(string questId)
        {
            var state = QuestService.State(questId);
            string key = state != null && state.status == "active" ? questId + ":" + state.acceptedTicks : null;
            if (key == _shiftKey) return;
            _shiftKey = key;
            Done.Clear();
            TableOrders.Clear();
            DropCarry();
        }

        public static bool IsDone(string stationId) => Done.Contains(stationId);
        public static void MarkDone(string stationId) => Done.Add(stationId);

        public static string OrderAt(string table) => TableOrders.TryGetValue(table, out string dish) ? dish : null;
        public static void SetOrder(string table, string dish) { if (dish == null) TableOrders.Remove(table); else TableOrders[table] = dish; }
        public static IEnumerable<KeyValuePair<string, string>> Orders => TableOrders;

        public static void Carry(string kind, string id, string label)
        {
            DropCarry();
            CarryKind = kind; CarryId = id; CarryLabel = label;
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            // Visible in the world: a box (stock) or a tray (dish) held in front of the chest.
            _carryProp = GameObject.CreatePrimitive(kind == "dish" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            Object.Destroy(_carryProp.GetComponent<Collider>());
            _carryProp.name = "CarriedItem";
            _carryProp.transform.SetParent(player.transform, false);
            _carryProp.transform.localPosition = new Vector3(0f, 1.05f, 0.42f);
            _carryProp.transform.localScale = kind == "dish" ? new Vector3(0.42f, 0.03f, 0.42f) : new Vector3(0.46f, 0.32f, 0.36f);
            var renderer = _carryProp.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) renderer.material = new Material(shader) { color = kind == "dish" ? new Color(0.95f, 0.93f, 0.88f) : new Color(0.72f, 0.55f, 0.34f) };
        }

        public static void DropCarry()
        {
            CarryKind = CarryId = CarryLabel = null;
            if (_carryProp != null) Object.Destroy(_carryProp);
            _carryProp = null;
        }
    }
}
