using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>What the student sees during a live Speaking exam (sent by the examiner, mirrored by the student).</summary>
    [Serializable]
    public sealed class SpeakingLiveState
    {
        public string roomId;
        public string testId;
        public string testTitle;
        public int part;              // 1, 2, 3 (0 = not started)
        public int index;             // question number inside the test
        public string question;
        public string[] cuePoints = Array.Empty<string>();
        /// <summary>waiting | question | prep | talk | grading | done</summary>
        public string phase = "waiting";
        /// <summary>UTC unix seconds when the current timed phase (prep / talk) ends; 0 = untimed.</summary>
        public double endsUtc;
        public int asked;             // questions asked so far
        public int total;             // questions in the test
        public string examinerName;
    }

    /// <summary>The examiner's marks: the four IELTS Speaking criteria, overall (IELTS rounding) and a comment.</summary>
    [Serializable]
    public sealed class SpeakingLiveResult
    {
        public string roomId;
        public string testId;
        public string testTitle;
        public string examinerName;
        public float fc, lr, gra, p;
        public float overall;
        public string comment;
        public string sentUtc;

        public static float Overall(float fc, float lr, float gra, float p) => IeltsBands.RoundOverall((fc + lr + gra + p) / 4f);

        /// <summary>The result as a Speaking paper in the exam mailbox (mode "live", graded by a person).</summary>
        public IeltsProductiveRecord ToRecord()
        {
            var record = new IeltsProductiveRecord
            {
                id = IeltsProductiveStore.NewId(string.IsNullOrEmpty(testId) ? "live_speaking" : testId) + "_live",
                testId = testId, testTitle = testTitle, skill = "speaking", mode = "live",
                finishedUtc = string.IsNullOrEmpty(sentUtc) ? DateTime.UtcNow.ToString("o") : sentUtc,
                overall = overall,
                speakingExamination = new IeltsAiExamination
                {
                    model = "examiner:" + examinerName,
                    summary = comment,
                    overall = overall,
                    createdUtc = sentUtc,
                    criteria = new List<IeltsCriterionScore>
                    {
                        new IeltsCriterionScore { code = "FC", name = "Fluency and Coherence", band = fc },
                        new IeltsCriterionScore { code = "LR", name = "Lexical Resource", band = lr },
                        new IeltsCriterionScore { code = "GRA", name = "Grammatical Range and Accuracy", band = gra },
                        new IeltsCriterionScore { code = "P", name = "Pronunciation", band = p },
                    },
                },
            };
            return record;
        }
    }

    /// <summary>A Speaking test the examiner can run live: the built-in NihongoLife sets and any local IELTS speaking package.</summary>
    public sealed class SpeakingLiveTest
    {
        public string Id, Title;
        public readonly List<SpeakingLiveQuestion> Questions = new List<SpeakingLiveQuestion>();

        public static List<SpeakingLiveTest> All()
        {
            var list = new List<SpeakingLiveTest>();
            foreach (var package in IeltsLibrary.Discover().Where(p => p.Test.skill == "speaking"))
            {
                var test = new SpeakingLiveTest { Id = package.Test.id, Title = package.Test.DisplayTitle };
                foreach (var part in package.Test.parts)
                    foreach (var group in part.groups ?? Array.Empty<IeltsGroup>())
                        foreach (var item in group.items ?? Array.Empty<IeltsItem>())
                            test.Questions.Add(new SpeakingLiveQuestion
                            {
                                Part = part.number, Number = item.number, Text = item.text,
                                CuePoints = part.number == 2 ? (group.lines ?? Array.Empty<IeltsLine>()).Select(l => l.text).ToArray() : Array.Empty<string>(),
                            });
                if (test.Questions.Count > 0) list.Add(test);
            }
            return list;
        }
    }

    public sealed class SpeakingLiveQuestion
    {
        public int Part, Number;
        public string Text;
        public string[] CuePoints;
    }

    /// <summary>
    /// Real-time layer of the Speaking lounge (Supabase Realtime broadcast). The classroom server (edge function) stays
    /// the authority for rooms, roles and Agora tokens; this hub only makes everyone hear about changes at once instead
    /// of waiting for the next poll, and carries the live exam between examiner and student:
    /// · lobby "nl:speaking" — a room opened / closed, a teacher accepted a room, a student asked to join, a request
    ///   was approved;
    /// · room "nl:speaking:&lt;room&gt;" — who is in the room, the current question / phase / timer, the result.
    /// A receiver only acts on hints addressed to it, then asks the server (so a forged hint changes nothing).
    /// </summary>
    public sealed class SpeakingLiveHub : MonoBehaviour
    {
        public const string DefaultLobby = "nl:speaking";
        public static string LobbyTopicOverride;
        public static SpeakingLiveHub Instance { get; private set; }

        private SupabaseRealtimeClient _realtime;
        private bool _ownsRealtime;
        private SupabaseRealtimeClient.Channel _lobby, _room;

        public string LocalId { get; private set; }
        public string LocalName { get; private set; }
        public bool IsTeacher { get; set; }
        public string RoomId { get; private set; }
        public bool LobbyJoined => _lobby != null && _lobby.Joined;
        public bool RoomJoined => _room != null && _room.Joined;
        public SpeakingLiveState State { get; private set; } = new SpeakingLiveState();
        public SpeakingLiveResult LastResult { get; private set; }
        public string PeerName { get; private set; }

        // Lobby hints
        public event Action<string, string, string, int> OnRoomOpened;   // roomId, studentName, testTitle, level
        public event Action<string> OnRoomClosed;                         // roomId
        public event Action<string, string> OnRoomClaimed;                // roomId, teacherName  (for my room)
        public event Action<string, string, int> OnJoinRequested;         // roomId, studentName, level (to me, teacher)
        public event Action<string, string> OnRequestApproved;            // roomId, teacherName  (for me, student)
        // Room
        public event Action<string, string> OnPeerEntered;                // name, role
        public event Action<string> OnPeerLeft;                           // name
        public event Action<SpeakingLiveState> OnStateChanged;
        public event Action<SpeakingLiveResult> OnResult;

        // The first hub is the game's; tests create more (one per simulated person).
        private void Awake() { if (Instance == null) Instance = this; }

        private void OnDestroy()
        {
            LeaveRoom();
            _lobby?.Leave();
            if (_ownsRealtime && _realtime != null) Destroy(_realtime.gameObject);
            if (Instance == this) Instance = null;
        }

        public static SpeakingLiveHub GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SpeakingLiveHub");
            DontDestroyOnLoad(go);
            return go.AddComponent<SpeakingLiveHub>();
        }

        // ─────────── Connecting ───────────

        /// <summary>Joins the lobby with the game's identity, sharing the online world's socket when it is open.</summary>
        public void Connect()
        {
            if (_lobby != null) return;
            ResolveIdentity();
            var world = FindFirstObjectByType<SupabaseOnlineWorldService>();
            _realtime = world != null ? world.Realtime : null;
            if (_realtime == null)
            {
                var client = FindFirstObjectByType<SupabaseClient>();
                if (client == null || !client.IsConfigured) return;
                var go = new GameObject("SpeakingRealtime");
                DontDestroyOnLoad(go);
                _realtime = go.AddComponent<SupabaseRealtimeClient>();
                _realtime.Initialize(client);
                _ownsRealtime = true;
            }
            JoinLobby();
        }

        /// <summary>Tests and tools: use this realtime connection and identity.</summary>
        public void Connect(SupabaseRealtimeClient realtime, string id, string displayName, bool teacher)
        {
            _realtime = realtime;
            LocalId = id;
            LocalName = displayName;
            IsTeacher = teacher;
            JoinLobby();
        }

        private void ResolveIdentity()
        {
            if (GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated && !string.IsNullOrEmpty(auth.UserId))
            {
                LocalId = auth.UserId;
                LocalName = !string.IsNullOrWhiteSpace(auth.DisplayName) ? auth.DisplayName : NihongoLife.Player.PlayableCharacterCatalog.GetPlayerName();
            }
            else
            {
                const string key = "NL.SpeakingLive.Id";
                if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetString(key, Guid.NewGuid().ToString("N"));
                LocalId = PlayerPrefs.GetString(key);
                LocalName = NihongoLife.Player.PlayableCharacterCatalog.GetPlayerName();
            }
        }

        private void JoinLobby()
        {
            if (_realtime == null || _lobby != null) return;
            _lobby = _realtime.Join(LobbyTopicOverride ?? DefaultLobby, string.Empty);
            _lobby.OnBroadcast += HandleLobby;
        }

        // ─────────── Lobby hints (sent after the server confirmed the action) ───────────

        public void AnnounceRoomOpen(string roomId, string testId, string testTitle, int level) =>
            _lobby?.Send("open", new Dictionary<string, object> { ["r"] = roomId, ["s"] = LocalId, ["n"] = LocalName, ["lv"] = level, ["t"] = testId ?? "", ["tt"] = testTitle ?? "" });

        public void AnnounceRoomClosed(string roomId) => _lobby?.Send("closed", new Dictionary<string, object> { ["r"] = roomId });

        public void AnnounceClaimed(string roomId, string studentId) =>
            _lobby?.Send("claimed", new Dictionary<string, object> { ["r"] = roomId, ["s"] = studentId ?? "", ["tn"] = LocalName, ["ti"] = LocalId });

        public void AnnounceJoinRequest(string roomId, string teacherId, int level) =>
            _lobby?.Send("request", new Dictionary<string, object> { ["r"] = roomId, ["s"] = LocalId, ["n"] = LocalName, ["lv"] = level, ["ti"] = teacherId ?? "" });

        public void AnnounceApproved(string roomId, string studentId) =>
            _lobby?.Send("approved", new Dictionary<string, object> { ["r"] = roomId, ["s"] = studentId ?? "", ["tn"] = LocalName });

        private void HandleLobby(string evt, Dictionary<string, object> p)
        {
            string room = p.Str("r", "");
            switch (evt)
            {
                case "open":
                    if (IsTeacher && p.Str("s") != LocalId) OnRoomOpened?.Invoke(room, Clean(p.Str("n", "")), Clean(p.Str("tt", "")), (int)p.Num("lv", 1));
                    break;
                case "closed":
                    OnRoomClosed?.Invoke(room);
                    break;
                case "claimed":
                    if (p.Str("s") == LocalId) OnRoomClaimed?.Invoke(room, Clean(p.Str("tn", "")));
                    else if (IsTeacher) OnRoomClosed?.Invoke(room); // taken by another teacher: drop it from my list
                    break;
                case "request":
                    if (IsTeacher && (p.Str("ti") == LocalId || string.IsNullOrEmpty(p.Str("ti")))) OnJoinRequested?.Invoke(room, Clean(p.Str("n", "")), (int)p.Num("lv", 1));
                    break;
                case "approved":
                    if (p.Str("s") == LocalId) OnRequestApproved?.Invoke(room, Clean(p.Str("tn", "")));
                    break;
            }
        }

        // ─────────── Room ───────────

        public void EnterRoom(string roomId)
        {
            if (_realtime == null || string.IsNullOrEmpty(roomId)) return;
            if (RoomId == roomId && _room != null) return;
            LeaveRoom();
            RoomId = roomId;
            State = new SpeakingLiveState { roomId = roomId };
            LastResult = null;
            PeerName = null;
            _room = _realtime.Join("nl:speaking:" + roomId, string.Empty);
            _room.OnBroadcast += HandleRoom;
            _room.OnJoined += () => _room?.Send("hello", Hello());
        }

        /// <summary>Captures and tests: applies a state / result locally, as if it had arrived from the room.</summary>
        public void ApplyLocal(SpeakingLiveState state, SpeakingLiveResult result = null)
        {
            if (state != null) { State = state; OnStateChanged?.Invoke(state); }
            if (result != null) { LastResult = result; OnResult?.Invoke(result); }
        }

        /// <summary>Forgets the exam state and result (a new exam starts clean).</summary>
        public void ResetExam()
        {
            State = new SpeakingLiveState { roomId = RoomId };
            LastResult = null;
            OnStateChanged?.Invoke(State);
        }

        public void LeaveRoom()
        {
            if (_room == null) return;
            _room.Send("bye", new Dictionary<string, object> { ["n"] = LocalName });
            _room.Leave();
            _room = null;
            RoomId = null;
        }

        private Dictionary<string, object> Hello() => new Dictionary<string, object> { ["i"] = LocalId, ["n"] = LocalName, ["role"] = IsTeacher ? "teacher" : "student" };

        /// <summary>Examiner: shows a question / starts a phase for the student (and mirrors it locally).</summary>
        public void SendState(SpeakingLiveState state)
        {
            state.roomId = RoomId;
            state.examinerName = LocalName;
            State = state;
            OnStateChanged?.Invoke(state);
            _room?.Send("state", new Dictionary<string, object> { ["j"] = JsonUtility.ToJson(state) });
        }

        /// <summary>Examiner: sends the marks; the student's game files them in the exam mailbox.</summary>
        public void SendResult(SpeakingLiveResult result)
        {
            result.roomId = RoomId;
            result.examinerName = LocalName;
            result.overall = SpeakingLiveResult.Overall(result.fc, result.lr, result.gra, result.p);
            result.sentUtc = DateTime.UtcNow.ToString("o");
            LastResult = result;
            _room?.Send("result", new Dictionary<string, object> { ["j"] = JsonUtility.ToJson(result) });
            var done = new SpeakingLiveState { roomId = RoomId, testId = result.testId, testTitle = result.testTitle, phase = "done", asked = State.asked, total = State.total };
            SendState(done);
        }

        private void HandleRoom(string evt, Dictionary<string, object> p)
        {
            switch (evt)
            {
                case "hello":
                    if (p.Str("i") == LocalId) return;
                    bool firstTime = PeerName == null;
                    PeerName = Clean(p.Str("n", ""));
                    OnPeerEntered?.Invoke(PeerName, p.Str("role", "student"));
                    // Answer once so a late joiner also learns who is here, and catches up on the exam.
                    if (firstTime)
                    {
                        _room?.Send("hello", Hello());
                        if (IsTeacher && State != null && State.part > 0) _room?.Send("state", new Dictionary<string, object> { ["j"] = JsonUtility.ToJson(State) });
                    }
                    break;
                case "bye":
                    OnPeerLeft?.Invoke(Clean(p.Str("n", PeerName ?? "")));
                    PeerName = null;
                    break;
                case "state":
                    if (IsTeacher) return; // the examiner is the source of the state
                    var state = TryParse<SpeakingLiveState>(p.Str("j"));
                    if (state == null) return;
                    State = state;
                    OnStateChanged?.Invoke(state);
                    break;
                case "result":
                    if (IsTeacher) return;
                    var result = TryParse<SpeakingLiveResult>(p.Str("j"));
                    if (result == null) return;
                    result.overall = SpeakingLiveResult.Overall(Clamp(result.fc), Clamp(result.lr), Clamp(result.gra), Clamp(result.p));
                    LastResult = result;
                    OnResult?.Invoke(result);
                    break;
            }
        }

        private static float Clamp(float band) => Mathf.Clamp(Mathf.Round(band * 2f) / 2f, 0f, 9f);

        private static T TryParse<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch { return null; }
        }

        /// <summary>Names from other clients are shown as plain text, short.</summary>
        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("<", "‹").Replace(">", "›");
            return value.Length > 40 ? value.Substring(0, 40) : value;
        }
    }
}
