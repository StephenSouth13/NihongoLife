using System.Collections;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Gestures;
using NihongoLife.Interaction;
using NihongoLife.Island;
using NihongoLife.MiniGames;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// What a player reported looking wrong, entered like a player: the Game Center has people in it (standing on the
    /// floor, playing, never on a machine of the player's), the island's way home is marked on the platform, the
    /// konbini clerk has no green board floating in front of the belly. Captures: Bao_Cao/places-look.
    /// </summary>
    public class PlacesLookPlayModeTests
    {
        private const string Topic = "places-look";

        private static IEnumerator Town()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
        }

        private static IEnumerator Enter(string scene, string spawn)
        {
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            flow.EnterZone(scene, spawn, scene);
            for (float t = 0f; t < 30f && (flow.IsLoading || SceneManager.GetActiveScene().name != scene); t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "entered " + scene);
            yield return new WaitForSecondsRealtime(1.5f);
        }

        [UnityTest]
        public IEnumerator GameCenter_HasRegularsPlaying_OnTheFloor_OffThePlayersMachines()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            yield return Enter(WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance);
            var crowd = Object.FindFirstObjectByType<ArcadeCrowd>();
            Assert.IsNotNull(crowd, "the arcade has regulars");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.GreaterOrEqual(crowd.Count, 8, "people in the Game Center");
            var report = new StringBuilder();
            int playing = 0;
            var launchers = Object.FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None);
            foreach (var person in crowd.People)
            {
                var animator = person.GetComponentInChildren<Animator>();
                float foot = Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                Assert.Less(Mathf.Abs(foot - person.transform.position.y), 0.2f, person.name + " stands on the floor");
                Assert.Less(Mathf.Abs(person.transform.position.y - crowd.transform.position.y), 0.05f, person.name + " at floor height");
                foreach (var launcher in launchers)
                {
                    var box = launcher.GetComponent<Collider>();
                    if (box == null) continue;
                    Vector3 p = person.transform.position; p.y = box.bounds.center.y;
                    Assert.IsFalse(box.bounds.Contains(p), person.name + " stands in front of " + launcher.name + " (the player's machine)");
                }
                var layer = person.GetComponent<GestureLayer>();
                if (layer != null && layer.Current == "arcade_play") playing++;
                report.AppendLine($"{person.name}: {person.transform.position} {(layer != null ? layer.Current : "-")}");
            }
            Assert.GreaterOrEqual(crowd.Regulars.Count(r => r.activity == "arcade_play"), 4, "regulars at the machines");
            // A snapshot can catch someone cheering: look again for a moment where most of them are playing.
            for (float t = 0f; t < 12f && playing < 3; t += 0.5f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                playing = crowd.People.Count(p => p.GetComponent<GestureLayer>() != null && p.GetComponent<GestureLayer>().Current == "arcade_play");
            }
            Assert.GreaterOrEqual(playing, 3, "people playing the machines\n" + report);
            Vector3 o = crowd.transform.position;
            TestShots.Shot(Topic, "01_game_center_people", o + new Vector3(0f, 3.2f, -8.2f), o + new Vector3(0f, 0.8f, 2f), 66f);
            TestShots.Shot(Topic, "02_game_center_claws", o + new Vector3(4.5f, 2.2f, -6.5f), o + new Vector3(9f, 0.9f, -1.5f), 60f);
        }

        [UnityTest]
        public IEnumerator MidoriIsland_TheTrainHome_IsMarkedOnThePlatform()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            yield return Enter(WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation);
            var door = Object.FindFirstObjectByType<IslandTrainDoor>();
            Assert.IsNotNull(door);
            yield return null;
            var beacon = door.GetComponentInChildren<PortalBeacon>(true);
            Assert.IsNotNull(beacon, "a sign over the train door");
            var mark = door.transform.Find("BoardingMark");
            Assert.IsNotNull(mark, "のりば painted on the platform");
            var hit = Physics.RaycastAll(mark.position + Vector3.up * 0.5f, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).FirstOrDefault();
            Assert.IsNotNull(hit.collider, "platform under the mark");
            Assert.Less(Mathf.Abs(mark.position.y - hit.point.y), 0.05f, "lying on the platform");
            // Seen from the middle of the platform, a few metres away.
            var player = Object.FindFirstObjectByType<NihongoLife.Player.PlayerController>();
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = door.transform.position + new Vector3(6f, -1f, 3.5f);
            if (controller != null) controller.enabled = true;
            yield return new WaitForSecondsRealtime(1.2f);
            TestShots.Shot(Topic, "03_island_boarding", door.transform.position + new Vector3(5f, 1.5f, 7f), door.transform.position + new Vector3(0f, 0f, -0.5f), 60f);
        }

        [UnityTest]
        public IEnumerator Classroom_StudentsSitOnTheBenches_TheBoardIsClear()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            yield return Enter(WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance);
            yield return new WaitForSecondsRealtime(1f);
            var desks = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "DeskTopArea").ToList();
            Assert.GreaterOrEqual(desks.Count, 12, "each student desk knows its desk top");
            var students = Object.FindObjectsByType<SeatedPassenger>(FindObjectsSortMode.None).Where(s => s.name.StartsWith("Student_")).ToList();
            Assert.GreaterOrEqual(students.Count, 8);
            var report = new StringBuilder();
            foreach (var student in students)
            {
                var animator = student.GetComponentInChildren<Animator>();
                Vector3 hips = animator.GetBoneTransform(HumanBodyBones.Hips).position, head = animator.GetBoneTransform(HumanBodyBones.Head).position;
                var desk = desks.OrderBy(d => (new Vector3(d.position.x - hips.x, 0f, d.position.z - hips.z)).sqrMagnitude).First();
                Vector3 local = desk.InverseTransformPoint(hips);
                report.AppendLine($"{student.name}: hips {hips} head {head.y:0.00}, desk top {desk.position.y:0.00} local {local}");
                Assert.IsFalse(Mathf.Abs(local.x) < 0.45f && Mathf.Abs(local.z) < 0.5f, student.name + " sits inside the desk\n" + report);
                Assert.Greater(head.y, desk.position.y + 0.15f, student.name + ": the head above the desk top, the body seen\n" + report);
                Assert.Less(Vector3.Dot(desk.position - hips, student.transform.forward), 1.2f, student.name + " sits at its own desk");
                Assert.Greater(Vector3.Dot(desk.position - hips, student.transform.forward), 0f, student.name + " faces the desk");
            }
            Assert.IsNull(GameObject.Find("BoardModel"), "the white board model stood in front of the chalkboard");
            Assert.IsNull(GameObject.Find("TeacherLamp"), "a ceiling lamp on the lectern looked like a table upside down");
            var board = GameObject.Find("BoardSurface").transform;
            TestShots.Shot(Topic, "05_classroom_from_back", board.position + new Vector3(0f, 0.6f, -12.5f), board.position + new Vector3(0f, -1.2f, -5f), 62f);
            TestShots.Shot(Topic, "06_classroom_board", board.position + new Vector3(0.5f, -0.2f, -5.5f), board.position, 55f);
            var first = students.OrderBy(s => s.transform.position.x).First();
            TestShots.Shot(Topic, "07_student_side", first.transform.position + new Vector3(-2.2f, 1.1f, 0.2f), first.transform.position + Vector3.up * 0.6f, 50f);
        }

        [UnityTest]
        public IEnumerator Konbini_Clerk_HasNoFloatingBoard()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            var aprons = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == "UniformApron").ToList();
            Assert.IsEmpty(aprons, "the rigid apron box floated in front of the kid clerk's belly");
            var clerk = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "CashierNPC");
            Assert.IsNotNull(clerk, "the konbini clerk");
            yield return new WaitForSecondsRealtime(2f);
            var animator = clerk.GetComponentInChildren<Animator>();
            Vector3 chest = animator.GetBoneTransform(HumanBodyBones.Chest).position;
            // Nothing hangs in the air in front of the clerk's chest and neck, where a shopper looks (a bag-stand block
            // floated over the gap between the counters there; before it, a rigid apron box).
            Vector3 neck = animator.GetBoneTransform(HumanBodyBones.Neck).position;
            var floating = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !r.transform.IsChildOf(clerk) && !(r is ParticleSystemRenderer) && r.GetComponent<TMPro.TMP_Text>() == null)
                .Where(r =>
                {
                    var b = r.bounds;
                    if (b.size.magnitude > 1.6f || b.max.y < chest.y - 0.05f || b.min.y > neck.y + 0.05f) return false;
                    Vector3 to = b.center - chest; to.y = 0f;
                    float ahead = Vector3.Dot(to, clerk.forward), side = Vector3.Dot(to, clerk.right);
                    return ahead > 0.05f && ahead < 1.0f && Mathf.Abs(side) < 0.6f;
                })
                .Select(r => { var p = r.transform; string path = r.name; while (p.parent != null) { p = p.parent; path = p.name + "/" + path; } return path + " " + r.bounds; }).ToList();
            var layer = clerk.GetComponentInChildren<GestureLayer>();
            TestShots.Portrait(Topic, "04_konbini_clerk", clerk, 1.8f, 0.8f);
            Assert.IsEmpty(floating, "in front of the clerk's chest (gesture " + (layer != null ? layer.Current : "none") + ")");
            var props = layer != null ? layer.ActiveProps.Select(p => p.id + " " + p.transform.position).ToList() : new System.Collections.Generic.List<string>();
            var work = clerk.GetComponentInChildren<CharacterAnimationController>();
            var own = clerk.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.GetComponent<TMPro.TMP_Text>() == null && r.name != "FacePlate" && r.name != "MoodBubble").Select(r => r.name + " " + r.bounds).ToList();
            Assert.IsEmpty(props, $"the clerk shows gesture props (gesture {layer?.Current}, work {work?.CurrentWork}); own meshes: {string.Join("; ", own)}");
            Assert.IsEmpty(own, "loose meshes on the clerk");
        }
    }
}
