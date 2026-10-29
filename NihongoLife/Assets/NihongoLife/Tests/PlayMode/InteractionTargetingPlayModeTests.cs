using System.Collections;
using System.Collections.Generic;
using NihongoLife.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// P9: F talks to the thing the player means. A crowd of stand-ins around a player: the one in front and near wins,
    /// the target is found fast, a wall hides what is behind it, Q cycles nearest-first and wraps, walking up to a new
    /// thing switches the target within a fraction of a second. (The detector itself is the production class.)
    /// </summary>
    public class InteractionTargetingPlayModeTests
    {
        private sealed class Stand : MonoBehaviour, IInteractable
        {
            public string label;
            public string GetPromptJa() => label;
            public string GetpromptEn() => label;
            public void Interact(GameObject player) { }
            public Transform GetTransform() => transform;
        }

        private readonly List<GameObject> _made = new List<GameObject>();
        /// <summary>An empty yard far from anything a previous test left loaded (the town's doors and NPCs).</summary>
        private static readonly Vector3 O = new Vector3(-3000f, 0f, -3000f);

        [TearDown] public void TearDown() { foreach (var g in _made) if (g != null) Object.Destroy(g); _made.Clear(); }

        private InteractionDetector Player()
        {
            var go = new GameObject("TargetingPlayer");
            go.transform.position = O;
            go.transform.rotation = Quaternion.identity; // faces +Z
            _made.Add(go);
            return go.AddComponent<InteractionDetector>();
        }

        private Stand Person(string label, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = label; go.transform.position = O + pos + Vector3.up; _made.Add(go);
            var s = go.AddComponent<Stand>(); s.label = label;
            return s;
        }

        private static IEnumerator Settle(float seconds) { for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null; }

        [UnityTest]
        public IEnumerator InFrontAndNear_Wins_AndIsFoundWithinTwoTenthsOfASecond()
        {
            var d = Player();
            Person("behind", new Vector3(0f, 0f, -1.2f));
            var front = Person("front", new Vector3(0f, 0f, 1.6f));
            Person("far", new Vector3(0.4f, 0f, 3.2f));
            float t0 = Time.unscaledTime;
            while (d.CurrentInteractable == null && Time.unscaledTime - t0 < 1f) yield return null;
            float found = Time.unscaledTime - t0;
            Assert.AreSame(front, d.CurrentInteractable, "the person in front, near, is the target (not the one behind)");
            Assert.LessOrEqual(found, 0.2f, "target found fast: " + found + " s");
            Assert.AreEqual(3, d.CandidateCount);
        }

        [UnityTest]
        public IEnumerator AWall_HidesWhatIsBehindIt()
        {
            var d = Player();
            var near = Person("behindWall", new Vector3(0.3f, 0f, 1.3f));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _made.Add(wall);
            wall.transform.position = O + new Vector3(0.3f, 1f, 0.85f); wall.transform.localScale = new Vector3(2f, 2f, 0.1f);
            var visible = Person("visible", new Vector3(-1.9f, 0f, 1.7f));
            yield return Settle(0.3f);
            Assert.AreSame(visible, d.CurrentInteractable, "the visible person beats the nearer one behind a wall");
            _ = near;
        }

        [UnityTest]
        public IEnumerator Q_CyclesNearestFirst_AndWraps()
        {
            var d = Player();
            var a = Person("a", new Vector3(0f, 0f, 1.2f));
            var b = Person("b", new Vector3(1.6f, 0f, 1.6f));
            var c = Person("c", new Vector3(-2.2f, 0f, 2.0f));
            yield return Settle(0.3f);
            Assert.AreSame(a, d.CurrentInteractable);
            var seen = new List<Object> { (Object)d.CurrentInteractable };
            for (int i = 0; i < 2; i++) { d.CycleTarget(); seen.Add((Object)d.CurrentInteractable); }
            CollectionAssert.AreEquivalent(new Object[] { a, b, c }, seen, "Q visits everyone in reach once");
            d.CycleTarget();
            Assert.AreSame(seen[0], d.CurrentInteractable, "wraps round to the first");
        }

        [UnityTest]
        public IEnumerator WalkingUpToSomethingNew_SwitchesTheTargetQuickly_WithoutFlicker()
        {
            var d = Player();
            var first = Person("first", new Vector3(0f, 0f, 1.5f));
            var second = Person("second", new Vector3(0f, 0f, 6f));
            yield return Settle(0.3f);
            Assert.AreSame(first, d.CurrentInteractable);
            // stand still next to both for a second: no flicker between two equally unlikely options
            int changes = 0; d.OnInteractableChanged += _ => changes++;
            yield return Settle(1f);
            Assert.AreEqual(0, changes, "a steady target does not flicker");
            // walk to the second person
            d.transform.position = O + new Vector3(0f, 0f, 4.8f);
            float t0 = Time.unscaledTime;
            while (!ReferenceEquals(d.CurrentInteractable, second) && Time.unscaledTime - t0 < 1f) yield return null;
            Assert.AreSame(second, d.CurrentInteractable);
            Assert.LessOrEqual(Time.unscaledTime - t0, 0.25f, "switched within a quarter of a second");
        }
    }
}
