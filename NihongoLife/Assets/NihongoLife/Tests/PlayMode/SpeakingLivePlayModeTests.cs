using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Learning;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The live Speaking exam over the real Supabase Realtime: a student opens an exam room, the teacher hears about it
    /// at once, accepts it (the student is told immediately), both enter the room and see each other arrive, the
    /// examiner's questions and timers reach the student, and the marks arrive with the overall band computed with
    /// IELTS rounding. Measures how fast each hint arrives.
    /// </summary>
    public class SpeakingLivePlayModeTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void CleanUp()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
            SpeakingLiveHub.LobbyTopicOverride = null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LiveExam_TeacherAndStudent_RealTime()
        {
            SpeakingLiveHub.LobbyTopicOverride = "nl:speaking:test:" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var teacher = NewHub("teacher", "t-" + System.Guid.NewGuid().ToString("N").Substring(0, 6), "Morita", true);
            var student = NewHub("student", "s-" + System.Guid.NewGuid().ToString("N").Substring(0, 6), "Long", false);
            yield return WaitFor(() => teacher.LobbyJoined && student.LobbyJoined, 20f, "both in the speaking lobby");

            string openedRoom = null, openedTest = null, claimedBy = null, peerAtStudent = null, peerAtTeacher = null;
            SpeakingLiveState seen = null;
            SpeakingLiveResult marks = null;
            teacher.OnRoomOpened += (room, name, test, level) => { openedRoom = room; openedTest = test; };
            student.OnRoomClaimed += (room, name) => claimedBy = name;
            student.OnPeerEntered += (name, role) => peerAtStudent = name + "/" + role;
            teacher.OnPeerEntered += (name, role) => peerAtTeacher = name + "/" + role;
            student.OnStateChanged += s => seen = s;
            student.OnResult += r => marks = r;

            string room = "room-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            float t0 = Time.realtimeSinceStartup;
            student.AnnounceRoomOpen(room, "nl_speaking_1", "IELTS Speaking · NL-1", 3);
            yield return WaitFor(() => openedRoom == room, 10f, "teacher hears the room open");
            float openMs = (Time.realtimeSinceStartup - t0) * 1000f;
            Assert.AreEqual("IELTS Speaking · NL-1", openedTest);

            t0 = Time.realtimeSinceStartup;
            teacher.AnnounceClaimed(room, student.LocalId);
            yield return WaitFor(() => claimedBy == "Morita", 10f, "student hears the teacher accepted");
            float claimMs = (Time.realtimeSinceStartup - t0) * 1000f;

            student.EnterRoom(room);
            teacher.EnterRoom(room);
            yield return WaitFor(() => student.RoomJoined && teacher.RoomJoined, 15f, "both in the exam room");
            yield return WaitFor(() => peerAtStudent == "Morita/teacher" && peerAtTeacher == "Long/student", 10f, "each sees the other arrive");

            t0 = Time.realtimeSinceStartup;
            double ends = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0 + 60;
            teacher.SendState(new SpeakingLiveState { testId = "nl_speaking_1", part = 2, index = 5, phase = "prep", question = "Describe a time when you learned something useful outside school.",
                cuePoints = new[] { "what you learned", "who helped you" }, endsUtc = ends, asked = 5, total = 9 });
            yield return WaitFor(() => seen != null && seen.index == 5 && seen.phase == "prep", 10f, "the question and timer reach the student");
            float stateMs = (Time.realtimeSinceStartup - t0) * 1000f;
            Assert.AreEqual(2, seen.cuePoints.Length);
            Assert.AreEqual("Morita", seen.examinerName);
            Assert.AreEqual(ends, seen.endsUtc, 0.01);

            t0 = Time.realtimeSinceStartup;
            teacher.SendResult(new SpeakingLiveResult { testId = "nl_speaking_1", testTitle = "IELTS Speaking · NL-1", fc = 6, lr = 7, gra = 6, p = 7, comment = "Good range; extend Part 3 answers." });
            yield return WaitFor(() => marks != null, 10f, "the marks reach the student");
            float resultMs = (Time.realtimeSinceStartup - t0) * 1000f;
            Assert.AreEqual(6.5f, marks.overall, 0.001f, "(6+7+6+7)/4 = 6.5");
            Assert.AreEqual("Morita", marks.examinerName);
            var record = marks.ToRecord();
            Assert.AreEqual("live", record.mode);
            Assert.AreEqual(4, record.speakingExamination.criteria.Count);
            Assert.IsTrue(record.speakingExamination.Ok);

            // Rounding like IELTS: 6.25 → 6.5, 6.125 → 6.0.
            Assert.AreEqual(6.5f, SpeakingLiveResult.Overall(6, 6, 6, 7), 0.001f);
            Assert.AreEqual(6.0f, SpeakingLiveResult.Overall(6, 6, 6, 6.5f), 0.001f);

            student.LeaveRoom();
            yield return WaitFor(() => peerAtTeacher == null || true, 1f, "");
            Debug.Log($"[SpeakingLive] open {openMs:0} ms · claimed {claimMs:0} ms · question {stateMs:0} ms · result {resultMs:0} ms");
        }

        private SpeakingLiveHub NewHub(string name, string id, string displayName, bool teacherRole)
        {
            var db = Resources.Load<GameControlDatabase>("Control/NihongoLifeControlDatabase");
            Assume.That(db != null && !string.IsNullOrEmpty(db.supabaseProjectUrl), "Supabase not configured");
            string url = db.supabaseProjectUrl.TrimEnd('/').Replace("https://", "wss://") + "/realtime/v1/websocket?apikey=" + db.supabaseAnonKey + "&vsn=1.0.0";
            var go = new GameObject("TestSpeaking_" + name);
            _spawned.Add(go);
            var realtime = go.AddComponent<SupabaseRealtimeClient>();
            realtime.InitializeRaw(url, db.supabaseAnonKey);
            var hub = go.AddComponent<SpeakingLiveHub>();
            hub.Connect(realtime, id, displayName, teacherRole);
            return hub;
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (condition()) yield break;
                yield return null;
            }
            Assert.Fail("Timed out waiting for: " + what);
        }
    }
}
