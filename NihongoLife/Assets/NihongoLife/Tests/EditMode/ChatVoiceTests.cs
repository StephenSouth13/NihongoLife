using System;
using NihongoLife.Core;
using NihongoLife.Learning;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    public class ChatVoiceTests
    {
        private GameLanguage _saved;
        [SetUp] public void SetUp() { _saved = Loc.Language; Loc.Set(GameLanguage.English); }
        [TearDown] public void TearDown() { Loc.Set(_saved); VoicePrefs.Reset(); }

        [Test]
        public void Typing_ThrottlesExpiresAndLabels()
        {
            var t = new ChatTyping();
            Assert.IsTrue(t.ShouldSend("community", 0f));
            Assert.IsFalse(t.ShouldSend("community", 1f), "throttled");
            Assert.IsTrue(t.ShouldSend("grp:a", 1f), "another conversation signals at once");
            Assert.IsTrue(t.ShouldSend("community", 1f + ChatTyping.SendEvery));
            t.ResetSend();
            Assert.IsTrue(t.ShouldSend("community", 4f), "after sending a message the next key signals again");

            Assert.AreEqual("", t.Label("community", 0f));
            t.Note("community", "u1", "Lan", 10f);
            Assert.AreEqual("Lan is typing…", t.Label("community", 11f));
            t.Note("community", "u2", "Minh", 10f);
            Assert.AreEqual("Lan and Minh are typing…", t.Label("community", 11f));
            t.Note("community", "u3", "An", 10f);
            Assert.AreEqual("3 people are typing…", t.Label("community", 11f));
            Assert.AreEqual("", t.Label("grp:x", 11f), "per conversation");
            t.Clear("community", "u3");
            Assert.AreEqual(2, t.Active("community", 11f).Count, "a message ends typing");
            Assert.AreEqual(0, t.Active("community", 10f + ChatTyping.Expire + 0.01f).Count, "expires when signals stop");
        }

        [Test]
        public void ChatTime_StampsAndDaySeparators()
        {
            var now = new DateTime(2026, 10, 31, 15, 0, 0, DateTimeKind.Local);
            long Ticks(DateTime local) => local.ToUniversalTime().Ticks;
            Assert.AreEqual("14:05", ChatTime.Stamp(Ticks(new DateTime(2026, 10, 31, 14, 5, 0, DateTimeKind.Local)), now));
            Assert.AreEqual("Yesterday 23:59", ChatTime.Stamp(Ticks(new DateTime(2026, 10, 30, 23, 59, 0, DateTimeKind.Local)), now));
            Assert.AreEqual("28/10 08:00", ChatTime.Stamp(Ticks(new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Local)), now));
            Assert.AreEqual("Today", ChatTime.DayLabel(Ticks(now), now));
            Assert.IsTrue(ChatTime.NewDay(0, Ticks(now)), "first message gets a separator");
            Assert.IsFalse(ChatTime.NewDay(Ticks(now.AddHours(-2)), Ticks(now)));
            Assert.IsTrue(ChatTime.NewDay(Ticks(now.AddDays(-1)), Ticks(now)));
        }

        [Test]
        public void VoicePrefs_PersistAndClamp()
        {
            VoicePrefs.Reset();
            Assert.IsFalse(VoicePrefs.OpenMic, "push-to-talk by default");
            Assert.IsFalse(VoicePrefs.SelfMuted);
            Assert.AreEqual(100, VoicePrefs.SpeakerVolume);
            VoicePrefs.OpenMic = true; VoicePrefs.SelfMuted = true; VoicePrefs.SpeakerVolume = 999; VoicePrefs.MicVolume = -5;
            Assert.IsTrue(VoicePrefs.OpenMic); Assert.IsTrue(VoicePrefs.SelfMuted);
            Assert.AreEqual(VoicePrefs.MaxVolume, VoicePrefs.SpeakerVolume);
            Assert.AreEqual(0, VoicePrefs.MicVolume);
            Assert.AreEqual(120, VoicePrefs.Step(100, 1)); Assert.AreEqual(80, VoicePrefs.Step(100, -1));
            Assert.AreEqual(200, VoicePrefs.Step(200, 1)); Assert.AreEqual("120%", VoicePrefs.Percent(120));
            VoicePrefs.SetUserMuted(42u, true);
            Assert.IsTrue(VoicePrefs.IsUserMuted(42u));
            VoicePrefs.SetUserMuted(42u, false);
            Assert.IsFalse(VoicePrefs.IsUserMuted(42u));
        }

        [Test]
        public void VoiceReconnect_ClassifiesAndBacksOff()
        {
            Assert.AreEqual(VoiceReconnect.Step.Reconnecting, VoiceReconnect.Classify(VoiceReconnect.Reconnecting, false, true));
            Assert.AreEqual(VoiceReconnect.Step.Restored, VoiceReconnect.Classify(VoiceReconnect.Connected, false, true));
            Assert.AreEqual(VoiceReconnect.Step.Rejoin, VoiceReconnect.Classify(VoiceReconnect.Failed, false, true));
            Assert.AreEqual(VoiceReconnect.Step.Rejoin, VoiceReconnect.Classify(VoiceReconnect.Disconnected, false, true));
            Assert.AreEqual(VoiceReconnect.Step.None, VoiceReconnect.Classify(VoiceReconnect.Disconnected, false, false));
            Assert.AreEqual(VoiceReconnect.Step.None, VoiceReconnect.Classify(VoiceReconnect.Failed, true, true), "leaving on purpose never rejoins");
            Assert.AreEqual(1f, VoiceReconnect.Delay(0)); Assert.AreEqual(2f, VoiceReconnect.Delay(1));
            Assert.AreEqual(16f, VoiceReconnect.Delay(4)); Assert.AreEqual(30f, VoiceReconnect.Delay(10));
            Assert.IsTrue(VoiceReconnect.IsSpeaking(40)); Assert.IsFalse(VoiceReconnect.IsSpeaking(5));
        }

        [Test]
        public void VoiceService_UsesAgoraUidForSpeakingAndMute()
        {
            // Same FNV-1a uid as the Edge Functions: mute and speaking are keyed by it.
            Assert.AreEqual(VoiceChatService.AgoraUid("abc"), VoiceChatService.AgoraUid("abc"));
            Assert.AreNotEqual(VoiceChatService.AgoraUid("abc"), VoiceChatService.AgoraUid("abd"));
            Assert.AreNotEqual(0u, VoiceChatService.AgoraUid(""));
        }
    }
}
