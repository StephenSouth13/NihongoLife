using System.Collections.Generic;
using NihongoLife.Gestures;
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

        public static string CarryKind { get; private set; }
        public static string CarryId { get; private set; }
        public static string CarryLabel { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _shiftKey = null; Done.Clear(); TableOrders.Clear(); CarryKind = CarryId = CarryLabel = null; }

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
            // Visible in the hands: a tray with the dish (sushi on a plate, a cup of tea) or a box, through the gesture
            // layer so it sits between the hands of any rig (a fixed 1.05 m disc floated in front of a kid's face, empty).
            var holder = Holder();
            if (holder != null) CarryHold.On(holder).SetJob(kind == "dish" ? "carry_tray" : "carry_box");
        }

        public static void DropCarry()
        {
            CarryKind = CarryId = CarryLabel = null;
            var holder = Holder();
            var hold = holder != null ? holder.GetComponent<CarryHold>() : null;
            if (hold != null) hold.SetJob(null);
        }

        /// <summary>The player's animated body (the object the gesture layer drives).</summary>
        private static GameObject Holder()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return null;
            var animation = player.GetComponentInChildren<NihongoLife.Core.CharacterAnimationController>();
            return animation != null ? animation.gameObject : player;
        }
    }
}
