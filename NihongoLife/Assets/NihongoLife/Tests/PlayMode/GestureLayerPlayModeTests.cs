using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The gesture layer on real Kids-family people, stepped at 60 fps: every gesture plays without a pop, ends cleanly and
    /// leaves nothing behind; targets, rotations and the sit pose have the right sign and size; gestures crossfade; props
    /// sit in the hands; simulators can pulse a hand; tempo scales time; the same gesture fits every body.
    /// </summary>
    public class GestureLayerPlayModeTests : GestureTestBase
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/gestures"));

        [Test]
        public void EveryGesture_PlaysSmoothly_EndsCleanly_AndHoldsItsProps()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 30);
            var report = new StringBuilder("id;seconds;max hand step (m);max head step;max hips step;max foot step;props;max fx;ends clean\n");
            var failures = new List<string>();
            foreach (var def in GestureLibrary.All)
            {
                if (!layer.Play(def.id)) { failures.Add(def.id + " does not play"); continue; }
                int steps = Mathf.RoundToInt(def.dur * (def.loop ? 2.2f : 1f) / Dt);
                var prev = Snapshot(layer);
                float hand = 0f, head = 0f, hips = 0f, foot = 0f; int maxFx = 0; bool propsOk = true; int propsSeen = 0;
                for (int i = 0; i < steps; i++)
                {
                    Tick(layer);
                    var s = Snapshot(layer);
                    for (int k = 0; k < s.Length; k++) if (!Finite(s[k])) { failures.Add(def.id + " NaN at " + i); i = steps; break; }
                    if (i >= steps) break;
                    hand = Mathf.Max(hand, (s[0] - prev[0]).magnitude, (s[1] - prev[1]).magnitude);
                    head = Mathf.Max(head, (s[2] - prev[2]).magnitude);
                    hips = Mathf.Max(hips, (s[3] - prev[3]).magnitude);
                    foot = Mathf.Max(foot, (s[4] - prev[4]).magnitude, (s[5] - prev[5]).magnitude);
                    prev = s;
                    maxFx = Mathf.Max(maxFx, layer.ActiveFx);
                    if (i == steps / 2)
                    {
                        foreach (var p in layer.ActiveProps)
                        {
                            propsSeen++;
                            var prop = GestureLibrary.Prop(p.id);
                            if (!Finite(p.transform.position)) { propsOk = false; failures.Add(def.id + " prop " + p.id + " NaN"); continue; }
                            float d;
                            if (prop.hand == "R") d = Vector3.Distance(p.transform.position, s[0]);
                            else if (prop.hand == "L") d = Vector3.Distance(p.transform.position, s[1]);
                            else if (prop.hand == "A") d = Vector3.Distance(p.transform.position, (s[0] + s[1]) * 0.5f);
                            else d = Vector3.Distance(p.transform.position, layer.transform.position);
                            float limit = prop.hand == "W" ? 2.0f : 0.8f * Mathf.Max(1f, layer.ArmLength / 0.55f);
                            if (d > limit) { propsOk = false; failures.Add(def.id + " prop " + p.id + " (" + prop.hand + ") is " + d.ToString("0.00") + " m from its holder"); }
                        }
                    }
                }
                int expectProps = def.props.Count(u => u.ts <= def.dur * 0.5f && (u.te < 0f || u.te >= def.dur * 0.5f));
                if (def.loop || true) { /* props with a time window shorter than the half of the run are not asserted */ }
                if (expectProps > 0 && propsSeen == 0 && !def.loop) failures.Add(def.id + " shows no prop at mid-gesture");
                // end
                if (def.loop) layer.Stop();
                Tick(layer, 120);
                bool clean = !layer.IsPlaying && layer.Influence < 0.01f && !layer.ActiveProps.Any();
                if (!clean) failures.Add(def.id + " does not end clean (playing " + layer.IsPlaying + ", influence " + layer.Influence.ToString("0.000") + ", props " + layer.ActiveProps.Count() + ")");
                Tick(layer, 150); // sparks and drops fade
                if (layer.ActiveFx != 0) failures.Add(def.id + " leaves " + layer.ActiveFx + " particles");
                report.AppendLine(string.Join(";", def.id, (steps * Dt).ToString("0.0"), hand.ToString("0.000"), head.ToString("0.000"), hips.ToString("0.000"), foot.ToString("0.000"), propsSeen, maxFx, clean));
                if (hand > 0.11f) failures.Add(def.id + " hand jumps " + hand.ToString("0.000") + " m in one frame");
                if (head > 0.07f) failures.Add(def.id + " head jumps " + head.ToString("0.000") + " m in one frame");
                if (hips > 0.07f) failures.Add(def.id + " hips jump " + hips.ToString("0.000") + " m in one frame");
                if (foot > 0.13f) failures.Add(def.id + " foot jumps " + foot.ToString("0.000") + " m in one frame");
                if (!propsOk) { }
            }
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "_motion_report.csv"), report.ToString(), new UTF8Encoding(false));
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void HandTargets_AreReached_InArmLengthsFromTheShoulder()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            MakeStatic("t_hands", ("rh", 0.2f, 0.3f, 0.5f), ("lh", 0.1f, 0.1f, 0.4f));
            Tick(layer, 20);
            layer.Play("t_hands");
            Tick(layer, 90);
            var root = layer.transform;
            float arm = layer.ArmLength;
            var rightShoulder = Bone(layer, HumanBodyBones.RightUpperArm);
            var leftShoulder = Bone(layer, HumanBodyBones.LeftUpperArm);
            var wantR = rightShoulder + root.rotation * new Vector3(0.2f, 0.3f, 0.5f) * arm;
            var wantL = leftShoulder + root.rotation * new Vector3(-0.1f, 0.1f, 0.4f) * arm;
            Assert.Less(Vector3.Distance(Bone(layer, HumanBodyBones.RightHand), wantR), 0.06f * arm / 0.55f, "right hand reaches its target");
            Assert.Less(Vector3.Distance(Bone(layer, HumanBodyBones.LeftHand), wantL), 0.06f * arm / 0.55f, "left hand reaches its target (x is mirrored)");
        }

        [Test]
        public void Rotations_HaveTheDocumentedSigns()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 40);
            var root = layer.transform;
            var baseSnap = Snapshot(layer);
            var baseRot = layer.Animator.GetBoneTransform(HumanBodyBones.Head).rotation;
            var baseRightShoulder = Bone(layer, HumanBodyBones.RightUpperArm);

            MakeStatic("t_pitch", ("sp", 25f, 0f, 0f));
            layer.Play("t_pitch"); Tick(layer, 90);
            float forward = Vector3.Dot(Bone(layer, HumanBodyBones.Head) - baseSnap[2], root.forward);
            Assert.Greater(forward, 0.05f, "sp pitch+ leans the head forward");
            layer.Stop(0f); Tick(layer, 60);

            MakeStatic("t_lean", ("sp", 0f, 0f, 20f));
            layer.Play("t_lean"); Tick(layer, 90);
            Assert.Greater(Vector3.Dot(Bone(layer, HumanBodyBones.Head) - baseSnap[2], root.right), 0.03f, "sp lean+ leans to the right");
            layer.Stop(0f); Tick(layer, 60);

            MakeStatic("t_turn", ("hr", 0f, 40f, 0f));
            layer.Play("t_turn"); Tick(layer, 90);
            float back = Vector3.Dot(Bone(layer, HumanBodyBones.RightUpperArm) - baseRightShoulder, root.forward);
            Assert.Less(back, -0.03f, "hr yaw+ turns the hips to the right (the right shoulder goes back)");
            layer.Stop(0f); Tick(layer, 60);

            MakeStatic("t_head", ("hd", 0f, 40f, 0f));
            layer.Play("t_head"); Tick(layer, 90);
            float turned = Quaternion.Angle(baseRot, layer.Animator.GetBoneTransform(HumanBodyBones.Head).rotation);
            Assert.AreEqual(40f, turned, 8f, "hd turns the head by about the asked angle");
        }

        [Test]
        public void HipsOffset_LowersTheBody_AndFeetStayPlanted()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 40);
            var b = Snapshot(layer);
            MakeStatic("t_squat", ("hp", 0f, -0.3f, 0f));
            layer.Play("t_squat"); Tick(layer, 100);
            var s = Snapshot(layer);
            Assert.AreEqual(0.3f * layer.LegLength, b[3].y - s[3].y, 0.06f * layer.LegLength + 0.03f, "hips drop by the asked part of the leg");
            Assert.Less(Mathf.Abs(s[4].y - b[4].y), 0.03f, "right foot stays on the floor");
            Assert.Less(Mathf.Abs(s[5].y - b[5].y), 0.03f, "left foot stays on the floor");
            Assert.Less(new Vector2(s[4].x - b[4].x, s[4].z - b[4].z).magnitude, 0.04f, "right foot does not slide");
        }

        [Test]
        public void SitChair_LowersHips_PlantsFeet_AndShowsAChair()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 40);
            var b = Snapshot(layer);
            Assert.IsTrue(layer.Play("sit_chair"));
            Tick(layer, 150);
            var s = Snapshot(layer);
            Assert.Greater(b[3].y - s[3].y, 0.25f * layer.LegLength, "hips go down");
            Assert.Less(Mathf.Abs(s[4].y - b[4].y), 0.05f, "feet stay on the floor");
            Assert.IsTrue(layer.ActiveProps.Any(p => p.id == "chair"), "a chair stands behind");
            layer.Stop(); Tick(layer, 120);
            var e = Snapshot(layer);
            Assert.Less(Mathf.Abs(e[3].y - b[3].y), 0.04f, "standing again");
        }

        [Test]
        public void CrossfadeAndSequence_AreContinuous()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 30);
            layer.Play("wave"); Tick(layer, 70);
            float before = layer.Weight;
            var prev = Snapshot(layer);
            layer.Play("bow_formal");
            Tick(layer);
            Assert.GreaterOrEqual(layer.Weight, Mathf.Min(0.9f, before) - 0.05f, "a new gesture keeps the weight of the old one");
            float maxStep = 0f; int worst = -1;
            for (int i = 0; i < 150; i++)
            {
                Tick(layer);
                var s = Snapshot(layer);
                for (int k = 0; k < 3; k++) { float m = (s[k] - prev[k]).magnitude; if (m > maxStep) { maxStep = m; worst = i * 10 + k; } }
                prev = s;
            }
            Assert.Less(maxStep, 0.1f, "no pop at the switch (frame*10+bone " + worst + ")");

            layer.Stop(0f); Tick(layer, 60);
            var order = new List<string>();
            string finished = null;
            layer.Finished += id => finished = id;
            Assert.IsTrue(layer.PlaySequence("wave", "bow_light", "point_there"));
            for (int i = 0; i < 60 * 14 && finished == null && order.Count < 3; i++)
            {
                Tick(layer);
                if (layer.Current != null && (order.Count == 0 || order[order.Count - 1] != layer.Current)) order.Add(layer.Current);
            }
            CollectionAssert.AreEqual(new[] { "wave", "bow_light", "point_there" }, order);
            Tick(layer, 90);
            if (layer.IsPlaying) layer.Stop();
            Tick(layer, 90);
            Assert.AreEqual("point_there", finished);
        }

        [Test]
        public void Pulse_DipsTheHand_AndItReturns()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            MakeStatic("t_keys", ("rh", 0.15f, -0.1f, 0.5f), ("lh", 0.15f, -0.1f, 0.5f));
            Tick(layer, 20);
            layer.Play("t_keys"); Tick(layer, 80);
            float rest = Bone(layer, HumanBodyBones.RightHand).y;
            layer.Pulse(GestureLimb.RightHand, 1f);
            float low = rest;
            for (int i = 0; i < 20; i++) { Tick(layer); low = Mathf.Min(low, Bone(layer, HumanBodyBones.RightHand).y); }
            Assert.Greater(rest - low, 0.02f, "the hand presses");
            Tick(layer, 60);
            Assert.Less(Mathf.Abs(Bone(layer, HumanBodyBones.RightHand).y - rest), 0.01f, "and comes back");
        }

        [Test]
        public void WaterPlants_ShowsCan_AndDrops_ThenCleansUp()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 30);
            Assert.IsTrue(layer.Play("water_plants"));
            int maxFx = 0; bool can = false;
            var def = GestureLibrary.Find("water_plants");
            for (int i = 0; i < Mathf.RoundToInt(def.dur / Dt); i++)
            {
                Tick(layer);
                maxFx = Mathf.Max(maxFx, layer.ActiveFx);
                foreach (var p in layer.ActiveProps) if (p.id == "can" && p.scale > 0.9f) can = true;
            }
            Assert.IsTrue(can, "the watering can is held at full size");
            Assert.Greater(maxFx, 3, "water falls from the spout");
            Tick(layer, 200);
            Assert.AreEqual(0, layer.ActiveFx);
            Assert.IsFalse(layer.ActiveProps.Any());
        }

        [Test]
        public void Bpm_ScalesGestureTime()
        {
            Person(FirstVariant, Vector3.zero, out var layer);
            Tick(layer, 20);
            var def = GestureLibrary.Find("dance_hiphop_bounce");
            layer.Play("dance_hiphop_bounce"); Tick(layer, 60);
            float normal = layer.Time01;
            layer.Stop(0f); Tick(layer, 10);
            layer.Play("dance_hiphop_bounce"); layer.SetBpm(def.bpm * 2f); Tick(layer, 60);
            Assert.AreEqual(normal * 2f, layer.Time01, 0.02f, "double tempo, double speed");
        }

        [Test]
        public void SameGestures_FitEveryBody()
        {
            var variants = CharacterVariants.File.variants;
            var picks = new List<string>();
            for (int i = 0; i < 7; i++) picks.Add(variants[Mathf.Min(variants.Length - 1, i * Mathf.Max(1, variants.Length / 7))].id);
            picks = picks.Distinct().ToList();
            Assert.GreaterOrEqual(picks.Count, 5);
            var failures = new List<string>();
            foreach (var id in picks)
            {
                Person(id, new Vector3(picks.IndexOf(id) * 3f, 0f, 0f), out var layer);
                Tick(layer, 20);
                foreach (var g in new[] { "wave", "bow_formal", "handshake", "dance_hiphop_bounce", "cook_chop", "piano_play", "farm_hoe", "sit_chair", "carry_box" })
                {
                    if (!layer.Play(g)) { failures.Add(id + " cannot play " + g); continue; }
                    var def = GestureLibrary.Find(g);
                    var prev = Snapshot(layer); float step = 0f;
                    for (int i = 0; i < Mathf.RoundToInt(Mathf.Min(def.dur, 3.5f) / Dt); i++)
                    {
                        Tick(layer);
                        var s = Snapshot(layer);
                        for (int k = 0; k < s.Length; k++)
                        {
                            if (!Finite(s[k])) failures.Add(id + "/" + g + " NaN");
                            step = Mathf.Max(step, (s[k] - prev[k]).magnitude);
                        }
                        prev = s;
                    }
                    if (step > 0.13f) failures.Add(id + "/" + g + " jumps " + step.ToString("0.000"));
                    layer.Stop(0f); Tick(layer, 30);
                }
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}