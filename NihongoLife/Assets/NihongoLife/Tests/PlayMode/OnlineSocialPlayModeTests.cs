using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The online layer against the real Supabase project (Realtime must be reachable): the realtime client, world
    /// shards with pose sync, area/community/group chat, end-to-end encrypted private messages, friends online, and
    /// in the city: remote avatars, F-targeting in a crowd and the chat window. Captures go to Bao_Cao/online-regression.
    /// Every test uses unique topics/ids, so it never mixes with real players' rooms.
    /// </summary>
    public class OnlineSocialPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/online-regression"));
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<string> _prefsToDelete = new List<string>();

        [SetUp]
        public void SetUp() => SupabaseRealtimeClient.Verbose = true;

        [TearDown]
        public void TearDown()
        {
            SupabaseRealtimeClient.Verbose = false;
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
            foreach (var key in _prefsToDelete) PlayerPrefs.DeleteKey(key);
            _prefsToDelete.Clear();
            SocialChatWindow.Close();
            SocialChatWindow.HidePlayerCard();
        }

        // ─────────── Realtime client ───────────

        [UnityTest]
        public IEnumerator Realtime_TwoClients_PresenceBroadcastOrderAndTrickyText()
        {
            var a = NewClient("A");
            var b = NewClient("B");
            string topic = "test:rt:" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var ca = a.Join(topic, "alice");
            var cb = b.Join(topic, "bob");
            var got = new List<string>();
            Dictionary<string, object> aliceMeta = null;
            string left = null;
            cb.OnBroadcast += (evt, p) => { if (evt == "m") got.Add(p.Str("t")); };
            cb.OnPresenceJoin += (key, meta) => { if (key == "alice") aliceMeta = meta; };
            cb.OnPresenceLeave += key => left = key;
            yield return WaitFor(() => ca.Joined && cb.Joined, 15f, "both joined");

            ca.Track(new Dictionary<string, object> { ["n"] = "Alice", ["c"] = "kid_yui" });
            yield return WaitFor(() => aliceMeta != null, 10f, "presence join seen by B");
            Assert.AreEqual("Alice", aliceMeta.Str("n"));
            Assert.AreEqual("kid_yui", aliceMeta.Str("c"));

            // Text that broke the old brace-counting parser, and 30 sends in one frame (one SendAsync at a time).
            string tricky = "xin chào {braces} [x] \"quoted\" \\ back 日本語 🙂 }";
            ca.Send("m", new Dictionary<string, object> { ["t"] = tricky });
            for (int i = 0; i < 30; i++) ca.Send("m", new Dictionary<string, object> { ["t"] = "n" + i });
            yield return WaitFor(() => got.Count >= 31, 15f, "31 broadcasts");
            CollectionAssert.Contains(got, tricky, "Exact text round-trip");
            // Supabase does not promise order inside a burst (poses carry a sequence number for that); none may be lost.
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 30).Select(i => "n" + i).ToList(), got.Where(x => x != tricky).ToList(), "None lost");

            ca.Leave();
            yield return WaitFor(() => left == "alice", 10f, "presence leave");
            Debug.Log($"[OnlineTest] realtime ok — A sent {a.SentFrames} frames, B received {b.ReceivedFrames}");
        }

        // ─────────── World shard ───────────

        [UnityTest]
        public IEnumerator World_TwoPlayers_SameShard_PoseAndAreaChat()
        {
            string scene = "TestPlaza_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var wa = NewWorld("wa", "Aki", "kid_hina");
            var wb = NewWorld("wb", "Ben", "kid_riku");
            yield return null;
            wa.UpdateLocalPlayerPose(scene, new Vector3(1f, 0f, 2f), Quaternion.identity);
            wb.UpdateLocalPlayerPose(scene, new Vector3(5f, 0f, 2f), Quaternion.identity);
            yield return WaitFor(() => wa.ShardTopic != null && wb.ShardTopic != null, 10f, "both in a shard");
            Assert.AreEqual(wa.ShardTopic, wb.ShardTopic, "Same place → same shard");
            StringAssert.EndsWith(":0", wa.ShardTopic);

            // A walks; B sees A's model id and position (to the centimetre).
            for (float t = 0f; t < 2.5f; t += Time.unscaledDeltaTime)
            {
                wa.UpdateLocalPlayerPose(scene, new Vector3(1f + t, 0f, 2f), Quaternion.Euler(0f, 90f, 0f));
                yield return null;
            }
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                wa.UpdateLocalPlayerPose(scene, new Vector3(4f, 0f, 2f), Quaternion.Euler(0f, 90f, 0f));
                yield return null;
            }
            OnlinePlayerSnapshot seen = null;
            yield return WaitFor(() => (seen = wb.VisiblePlayers.FirstOrDefault(p => p.playerId == wa.LocalPlayerId && p.hasPose && Mathf.Abs(p.position.x - 4f) < 0.02f)) != null, 10f, "B sees A at x=4");
            // The model id comes with presence, which can land a moment after the first position.
            yield return WaitFor(() => seen.characterId != null, 5f, "A's presence (character) at B");
            Assert.AreEqual("kid_hina", seen.characterId);
            Assert.AreEqual("Aki", seen.displayName);
            Assert.AreEqual(90f, seen.rotation.eulerAngles.y, 1f);

            wa.SendChatMessage("area", "こんにちは <b>bold?</b>");
            yield return WaitFor(() => wb.ChatHistory.Any(m => m.senderPlayerId == wa.LocalPlayerId), 10f, "area chat");
            var msg = wb.ChatHistory.Last(m => m.senderPlayerId == wa.LocalPlayerId);
            Assert.AreEqual("こんにちは ‹b›bold?‹/b›", msg.text, "Rich-text tags are neutralised");

            // B leaves the place → A no longer has B.
            wb.UpdateLocalPlayerPose("Bedroom_" + scene, Vector3.zero, Quaternion.identity);
            yield return WaitFor(() => wa.VisiblePlayers.All(p => p.playerId != wb.LocalPlayerId), 10f, "B left the shard");
            Assert.IsNull(wb.ShardTopic, "Bedrooms are private");
        }

        // ─────────── Social ───────────

        [UnityTest]
        public IEnumerator Social_CommunityGroupPrivateE2E_AndFriendsOnline()
        {
            string scene = "TestCafe_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            string community = "test:community:" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var wa = NewWorld("sa", "Aoi", "kid_mio");
            var wb = NewWorld("sb", "Bao", "kid_sota");
            var ha = NewHub(wa, "_testA", community);
            var hb = NewHub(wb, "_testB", community);
            wa.UpdateLocalPlayerPose(scene, Vector3.zero, Quaternion.identity);
            wb.UpdateLocalPlayerPose(scene, Vector3.one, Quaternion.identity);
            float t0 = Time.realtimeSinceStartup;
            yield return WaitFor(() => wa.IsOnline(wb.LocalPlayerId) && wb.IsOnline(wa.LocalPlayerId), 20f, "both in the lobby");
            yield return WaitFor(() => ha.CryptoReady && hb.CryptoReady, 60f, "RSA keys generated");
            Debug.Log($"[OnlineTest] lobby + 2 RSA-2048 key pairs ready after {Time.realtimeSinceStartup - t0:0.0} s");

            // Community
            ha.Send(SocialHub.CommunityId, "Chào cả nhà!");
            yield return WaitFor(() => hb.Get(SocialHub.CommunityId).Messages.Any(m => m.text == "Chào cả nhà!"), 10f, "community");
            Assert.AreEqual(1, hb.Get(SocialHub.CommunityId).Unread, "Unread while not looking");

            // Group: create, invite, accept, talk both ways
            var g = ha.CreateGroup("Nhóm ôn N4");
            yield return WaitFor(() => g.MemberCount == 1, 10f, "group joined");
            ha.InviteToGroup(g.Id, wb.LocalPlayerId);
            yield return WaitFor(() => hb.Invites.Any(i => i.GroupId == g.Id), 10f, "invite received");
            Assert.AreEqual("Nhóm ôn N4", hb.Invites[0].GroupName);
            hb.AcceptInvite(hb.Invites[0]);
            yield return WaitFor(() => g.MemberCount == 2, 10f, "B in group");
            hb.Send(SocialHub.GroupConversationId(g.Id), "Mình vào rồi");
            yield return WaitFor(() => ha.Get(SocialHub.GroupConversationId(g.Id)).Messages.Any(m => m.text == "Mình vào rồi"), 10f, "group message");

            // Private: a third, raw listener on B's inbox sees only ciphertext.
            var spy = NewClient("spy");
            var spyChannel = spy.Join("inbox:" + wb.LocalPlayerId, string.Empty);
            var raw = new List<string>();
            spyChannel.OnBroadcast += (evt, p) => raw.Add(evt + " " + MiniJson.Serialize(p));
            yield return WaitFor(() => spyChannel.Joined, 10f, "spy joined");
            string secret = "Mật khẩu wifi là sakura-2026 {x}";
            ha.OpenDirect(wb.LocalPlayerId, "Bao");
            Assert.IsTrue(ha.Send(SocialHub.DirectId(wb.LocalPlayerId), secret));
            yield return WaitFor(() => hb.Get(SocialHub.DirectId(wa.LocalPlayerId))?.Messages.Any(m => m.text == secret) == true, 15f, "DM decrypted by B");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(raw.Any(r => r.StartsWith("dm ")), "The DM went through B's inbox");
            Assert.IsFalse(raw.Any(r => r.Contains("sakura")), "…as ciphertext only");
            Assert.AreEqual(ha.SafetyCode, hb.PeerSafetyCode(wa.LocalPlayerId), "Both sides show the same safety code");
            hb.Send(SocialHub.DirectId(wa.LocalPlayerId), "Cảm ơn!");
            yield return WaitFor(() => ha.Get(SocialHub.DirectId(wb.LocalPlayerId)).Messages.Any(m => m.text == "Cảm ơn!"), 15f, "reply");

            // Block: B blocks A, A's next message is not shown to B.
            hb.SetBlocked(wa.LocalPlayerId, true);
            ha.Send(SocialHub.CommunityId, "tin bị chặn");
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.IsFalse(hb.Get(SocialHub.CommunityId).Messages.Any(m => m.text == "tin bị chặn"), "Blocked people are silent");
            hb.SetBlocked(wa.LocalPlayerId, false);

            // Friends: online with place, then offline when they quit.
            ha.DebugSetFriends(new[] { wb.LocalPlayerId });
            Assert.IsTrue(ha.Friends[0].Online);
            Assert.AreEqual("Bao", ha.Friends[0].Name);
            wb.Disconnect();
            yield return WaitFor(() => !wa.IsOnline(wb.LocalPlayerId), 15f, "B offline in lobby");
            ha.DebugSetFriends(new[] { wb.LocalPlayerId });
            Assert.IsFalse(ha.Friends[0].Online, "Friend shows offline");
            Assert.IsFalse(ha.Send(SocialHub.DirectId(wb.LocalPlayerId), "còn đó không?"), "DM to an offline friend is refused, not stored");
        }

        // ─────────── In the city ───────────

        [UnityTest]
        public IEnumerator City_RemotePlayers_TargetingAndChatWindow()
        {
            yield return LoadCity();
            Assert.IsTrue(GameServices.TryGet(out IOnlineWorldService world) && world is SupabaseOnlineWorldService, "Online is enabled in the control database");
            var online = (SupabaseOnlineWorldService)world;
            yield return WaitFor(() => online.ShardTopic != null, 15f, "local player in a city shard");
            var player = Object.FindFirstObjectByType<PlayerController>();
            string scene = SceneManager.GetActiveScene().name;

            // Two other players appear right in front of us (they join the same shard through the network).
            Vector3 fwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            var bots = new[]
            {
                NewWorld("cityA", "Haru", "kid_haruto"),
                NewWorld("cityB", "Yui", "kid_yui")
            };
            var spots = new[] { player.transform.position + fwd * 1.6f - right * 0.7f, player.transform.position + fwd * 2.1f + right * 0.8f };
            var botTransforms = new Quaternion[] { Quaternion.LookRotation(-fwd), Quaternion.LookRotation(-fwd) };
            var manager = Object.FindFirstObjectByType<RemotePlayerManager>();
            Assert.IsNotNull(manager, "RemotePlayerManager runs on AppRoot");
            for (float t = 0f; t < 20f && manager.AvatarCount < 2; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < bots.Length; i++) bots[i].UpdateLocalPlayerPose(scene, spots[i], botTransforms[i]);
                yield return null;
            }
            Assert.GreaterOrEqual(manager.AvatarCount, 2, "Both remote players spawned");
            yield return WaitFor(() => manager.Avatars.Values.Count(a => a.DisplayName == "Haru" || a.DisplayName == "Yui") == 2, 10f, "names from presence");
            yield return new WaitForSecondsRealtime(1.2f);
            foreach (var avatar in manager.Avatars.Values.Where(a => a.DisplayName == "Haru" || a.DisplayName == "Yui"))
            {
                var animator = avatar.GetComponentInChildren<Animator>(true);
                string tree = string.Join(", ", avatar.GetComponentsInChildren<Transform>(true).Select(t => t.name + (t.gameObject.activeInHierarchy ? "" : "(off)")).Take(12));
                Assert.IsNotNull(animator, $"Real character model, not a capsule — {avatar.CharacterId}: {tree}");
                Assert.IsTrue(animator.gameObject.activeInHierarchy, "Model shown: " + tree);
                Assert.IsNotNull(animator.runtimeAnimatorController, "Animated");
                Assert.AreEqual(avatar.DisplayName == "Haru" ? "kid_haruto" : "kid_yui", avatar.CharacterId, "Wears the character the other player picked");
                Assert.Less(Vector3.Distance(avatar.transform.position, spots[avatar.DisplayName == "Haru" ? 0 : 1]), 0.1f, "Interpolated onto the sent position");
            }

            // F-targeting in the crowd: one clear target, Q cycles, the marker names it.
            var detector = player.GetComponent<InteractionDetector>();
            var highlight = player.GetComponent<InteractionHighlight>();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.GreaterOrEqual(detector.CandidateCount, 2, "Two people in reach");
            var first = detector.CurrentInteractable;
            Assert.IsNotNull(first);
            Assert.IsTrue(highlight.Visible);
            StringAssert.Contains("[F]", highlight.LabelText);
            StringAssert.Contains("/", highlight.LabelText, "Shows i/n when several are in reach");
            // Stable: the target does not flicker over a second of standing still.
            for (int i = 0; i < 10; i++) { yield return new WaitForSecondsRealtime(0.1f); Assert.AreSame(first, detector.CurrentInteractable, "No flicker"); }
            Capture("crowd_target_first");
            detector.CycleTarget();
            yield return null;
            Assert.AreNotSame(first, detector.CurrentInteractable, "Q picks the next one");
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("crowd_target_cycled");

            // F on a remote player opens their card.
            var target = detector.CurrentInteractable as RemotePlayerAvatar;
            if (target == null) target = manager.Avatars.Values.First();
            target.Interact(player.gameObject);
            yield return null;
            Assert.IsTrue(SocialChatWindow.CardOpen, "Player card");
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("player_card");
            SocialChatWindow.HidePlayerCard();

            // Area chat from a remote player shows in the window.
            bots[0].SendChatMessage("area", "Xin chào, mình là Haru! Cùng luyện N5 không?");
            SocialChatWindow.Open(SocialChatWindow.Tab.Area);
            yield return WaitFor(() => GameServices.TryGet(out SocialHub hub) && hub.Get(SocialHub.AreaId).Messages.Any(m => m.text.Contains("Haru")), 10f, "area chat arrived");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Greater(SocialChatWindow.Instance.RenderedMessageCount, 0);
            Capture("chat_area");
            SocialChatWindow.Instance.SelectTab(SocialChatWindow.Tab.Groups);
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("chat_groups");
            SocialChatWindow.Instance.SelectTab(SocialChatWindow.Tab.Friends);
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("chat_friends");
            SocialChatWindow.Close();
            yield return null;
            Assert.IsFalse(player.InputLocked, "Closing the chat gives control back");
        }

        // ─────────── helpers ───────────

        private SupabaseRealtimeClient NewClient(string name)
        {
            var db = Resources.Load<GameControlDatabase>("Control/NihongoLifeControlDatabase");
            Assume.That(db != null && !string.IsNullOrEmpty(db.supabaseProjectUrl), "Supabase not configured");
            string url = db.supabaseProjectUrl.TrimEnd('/').Replace("https://", "wss://") + "/realtime/v1/websocket?apikey=" + db.supabaseAnonKey + "&vsn=1.0.0";
            var go = new GameObject("TestRealtime_" + name);
            _spawned.Add(go);
            var client = go.AddComponent<SupabaseRealtimeClient>();
            client.InitializeRaw(url, db.supabaseAnonKey);
            return client;
        }

        private SupabaseOnlineWorldService NewWorld(string name, string displayName, string characterId)
        {
            var client = NewClient(name);
            var world = client.gameObject.AddComponent<SupabaseOnlineWorldService>();
            world.InitializeWith(client);
            world.UseAuthIdentity = false;
            world.LocalCharacterId = characterId;
            world.ConnectLocalPlayer("test_" + name + "_" + Guid.NewGuid().ToString("N").Substring(0, 8), displayName);
            return world;
        }

        private SocialHub NewHub(SupabaseOnlineWorldService world, string suffix, string community)
        {
            var hub = world.gameObject.AddComponent<SocialHub>();
            hub.CommunityTopicOverride = community;
            hub.InitializeWith(world, suffix);
            _prefsToDelete.Add("NL.Chat.Rsa.v1" + suffix);
            _prefsToDelete.Add("NL.Chat.Pins.v1" + suffix);
            return hub;
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds, string what)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for: " + what);
                yield return null;
            }
        }

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private static void Capture(string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }
    }
}
