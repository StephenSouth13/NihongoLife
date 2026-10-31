using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>The player's voice-chat choices, kept between sessions: push-to-talk or open mic, self-mute and the
    /// microphone / speaker volumes (Agora's 0–400 scale, 100 = unchanged; the game allows 0–200). Muting another
    /// player lasts for this session only.</summary>
    public static class VoicePrefs
    {
        public const int MinVolume = 0, MaxVolume = 200, DefaultVolume = 100, VolumeStep = 20;
        private const string OpenMicKey = "NL.Voice.OpenMic", MutedKey = "NL.Voice.SelfMuted", MicKey = "NL.Voice.Mic", SpeakerKey = "NL.Voice.Speaker";

        private static readonly HashSet<uint> _mutedUsers = new HashSet<uint>();
        public static IReadOnlyCollection<uint> MutedUsers => _mutedUsers;

        public static bool OpenMic { get => PlayerPrefs.GetInt(OpenMicKey, 0) == 1; set => PlayerPrefs.SetInt(OpenMicKey, value ? 1 : 0); }
        public static bool SelfMuted { get => PlayerPrefs.GetInt(MutedKey, 0) == 1; set => PlayerPrefs.SetInt(MutedKey, value ? 1 : 0); }
        public static int MicVolume { get => Clamp(PlayerPrefs.GetInt(MicKey, DefaultVolume)); set => PlayerPrefs.SetInt(MicKey, Clamp(value)); }
        public static int SpeakerVolume { get => Clamp(PlayerPrefs.GetInt(SpeakerKey, DefaultVolume)); set => PlayerPrefs.SetInt(SpeakerKey, Clamp(value)); }

        public static int Clamp(int volume) => Mathf.Clamp(volume, MinVolume, MaxVolume);
        public static int Step(int volume, int direction) => Clamp(volume + (direction > 0 ? VolumeStep : direction < 0 ? -VolumeStep : 0));
        public static string Percent(int volume) => Clamp(volume) + "%";

        public static bool IsUserMuted(uint uid) => _mutedUsers.Contains(uid);
        public static void SetUserMuted(uint uid, bool muted) { if (muted) _mutedUsers.Add(uid); else _mutedUsers.Remove(uid); }

        /// <summary>Back to defaults (used by tests and by the max-settings preset).</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(OpenMicKey); PlayerPrefs.DeleteKey(MutedKey); PlayerPrefs.DeleteKey(MicKey); PlayerPrefs.DeleteKey(SpeakerKey);
            _mutedUsers.Clear();
        }
    }

    /// <summary>What to do when Agora reports a connection change, and how long to wait before rejoining.
    /// Agora reconnects by itself after short network drops (RECONNECTING → CONNECTED); only a FAILED connection,
    /// or a DISCONNECTED one we did not ask for, needs a fresh token and a new join.</summary>
    public static class VoiceReconnect
    {
        public enum Step { None, Reconnecting, Restored, Rejoin }
        public const int MaxAttempts = 6;

        // Agora CONNECTION_STATE_TYPE values.
        public const int Disconnected = 1, Connecting = 2, Connected = 3, Reconnecting = 4, Failed = 5;

        public static Step Classify(int state, bool leftOnPurpose, bool wasJoined)
        {
            if (leftOnPurpose) return Step.None;
            switch (state)
            {
                case Reconnecting: return Step.Reconnecting;
                case Connected: return Step.Restored;
                case Failed: return Step.Rejoin;
                case Disconnected: return wasJoined ? Step.Rejoin : Step.None;
                default: return Step.None;
            }
        }

        /// <summary>1, 2, 4, 8, 16, 30 s…</summary>
        public static float Delay(int attempt) => Mathf.Min(30f, Mathf.Pow(2f, Mathf.Max(0, attempt)));

        /// <summary>Agora volume indication (0–255) loud enough to count as speaking.</summary>
        public const int SpeakingVolume = 12;
        public static bool IsSpeaking(uint volume) => volume > SpeakingVolume;
    }
}
