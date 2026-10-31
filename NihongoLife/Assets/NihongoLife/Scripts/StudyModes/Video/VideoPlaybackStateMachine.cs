using System;

namespace NihongoLife.StudyModes.Video
{
    public enum VideoPlaybackState { Idle, Preparing, Playing, Paused, Ended, Error, ExternalOnly }

    public enum VideoErrorKind { None, NoSource, BadUrl, Offline, PrepareFailed, Timeout, Decode }

    /// <summary>
    /// Pure playback state machine behind the in-game video player, independent of UnityEngine.Video so it is
    /// unit-tested. Illegal transitions are ignored (return false) instead of throwing, so stray VideoPlayer
    /// callbacks (late prepareCompleted after Close, errorReceived twice) can never break the UI.
    /// </summary>
    public sealed class VideoPlaybackStateMachine
    {
        public const float DefaultPrepareTimeout = 20f;

        public VideoPlaybackState State { get; private set; } = VideoPlaybackState.Idle;
        public VideoErrorKind Error { get; private set; } = VideoErrorKind.None;
        public string ErrorDetail { get; private set; }
        public float PrepareElapsed { get; private set; }
        public float PrepareTimeout { get; set; } = DefaultPrepareTimeout;
        public int Generation { get; private set; }

        public event Action<VideoPlaybackState> Changed;

        /// <summary>Starts loading an entry. Returns the kind the UI must render.</summary>
        public VideoSourceKind Load(VideoEntry entry, bool networkReachable = true)
        {
            Generation++;
            Error = VideoErrorKind.None; ErrorDetail = null; PrepareElapsed = 0f;
            var kind = VideoCatalog.Classify(entry);
            switch (kind)
            {
                case VideoSourceKind.None: Fail(VideoErrorKind.NoSource, "No playable source"); break;
                case VideoSourceKind.YouTubeOnly: Set(VideoPlaybackState.ExternalOnly); break;
                case VideoSourceKind.DirectUrl when !networkReachable: Fail(VideoErrorKind.Offline, "No internet connection"); break;
                case VideoSourceKind.DirectUrl when !Uri.IsWellFormedUriString(entry.playableUrl.Trim(), UriKind.Absolute):
                    Fail(VideoErrorKind.BadUrl, "Invalid URL"); break;
                default: Set(VideoPlaybackState.Preparing); break;
            }
            return kind;
        }

        public bool OnPrepared(int generation)
        {
            if (generation != Generation || State != VideoPlaybackState.Preparing) return false;
            Set(VideoPlaybackState.Playing);
            return true;
        }

        public bool OnPlayerError(int generation, string message)
        {
            if (generation != Generation) return false;
            if (State != VideoPlaybackState.Preparing && State != VideoPlaybackState.Playing && State != VideoPlaybackState.Paused) return false;
            Fail(State == VideoPlaybackState.Preparing ? VideoErrorKind.PrepareFailed : VideoErrorKind.Decode, message);
            return true;
        }

        /// <summary>Call every frame while preparing; fails with Timeout once the limit passes.</summary>
        public bool Tick(float deltaTime)
        {
            if (State != VideoPlaybackState.Preparing) return false;
            PrepareElapsed += Math.Max(0f, deltaTime);
            if (PrepareElapsed < PrepareTimeout) return false;
            Fail(VideoErrorKind.Timeout, "Timed out while loading");
            return true;
        }

        public bool TogglePause()
        {
            if (State == VideoPlaybackState.Playing) { Set(VideoPlaybackState.Paused); return true; }
            if (State == VideoPlaybackState.Paused) { Set(VideoPlaybackState.Playing); return true; }
            if (State == VideoPlaybackState.Ended) { Set(VideoPlaybackState.Playing); return true; } // replay
            return false;
        }

        public bool OnEnded(int generation)
        {
            if (generation != Generation || (State != VideoPlaybackState.Playing && State != VideoPlaybackState.Paused)) return false;
            Set(VideoPlaybackState.Ended);
            return true;
        }

        /// <summary>Seconds without a new frame while playing before the UI shows "Buffering…".</summary>
        public const float BufferingAfter = 0.75f;
        public const float DefaultStallTimeout = 20f;
        public float StallTimeout { get; set; } = DefaultStallTimeout;
        public bool Buffering { get; private set; }
        public float StallElapsed { get; private set; }
        private long _lastFrame = long.MinValue;

        /// <summary>0..1 share of the prepare timeout already used (drives the loading strip).</summary>
        public float PrepareProgress => State == VideoPlaybackState.Preparing && PrepareTimeout > 0f ? Math.Min(1f, PrepareElapsed / PrepareTimeout) : 0f;

        /// <summary>Call every frame while playing with the player's current frame. Detects buffering (no new frame)
        /// and fails with Timeout after <see cref="StallTimeout"/> seconds of stall. True when it just failed.</summary>
        public bool TickPlayback(long frame, float deltaTime)
        {
            if (State != VideoPlaybackState.Playing) { ResetStall(); return false; }
            if (frame != _lastFrame) { _lastFrame = frame; StallElapsed = 0f; Buffering = false; return false; }
            StallElapsed += Math.Max(0f, deltaTime);
            Buffering = StallElapsed >= BufferingAfter;
            if (StallElapsed < StallTimeout) return false;
            Fail(VideoErrorKind.Timeout, "Stalled while buffering");
            return true;
        }

        /// <summary>After a seek/resume the next frames may take a moment: start counting again.</summary>
        public void ResetStall() { StallElapsed = 0f; Buffering = false; _lastFrame = long.MinValue; }

        public bool CanSeek => State == VideoPlaybackState.Playing || State == VideoPlaybackState.Paused || State == VideoPlaybackState.Ended;

        public void Close()
        {
            Generation++;
            Error = VideoErrorKind.None; ErrorDetail = null; PrepareElapsed = 0f;
            ResetStall();
            Set(VideoPlaybackState.Idle);
        }

        public static float ClampVolume(float v) => float.IsNaN(v) ? 1f : Math.Min(1f, Math.Max(0f, v));

        /// <summary>Seek target in seconds from a 0..1 slider; 0 when the length is unknown.</summary>
        public static double SeekTime(float normalized, double length)
        {
            if (length <= 0 || double.IsNaN(length) || double.IsInfinity(length) || float.IsNaN(normalized)) return 0;
            return Math.Min(1f, Math.Max(0f, normalized)) * length;
        }

        public static string FormatTime(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) seconds = 0;
            int s = (int)seconds;
            return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60}:{s % 60:00}";
        }

        private void Fail(VideoErrorKind kind, string detail)
        {
            Error = kind; ErrorDetail = detail;
            ResetStall();
            Set(VideoPlaybackState.Error);
        }

        private void Set(VideoPlaybackState s)
        {
            if (State == s) return;
            State = s;
            Changed?.Invoke(s);
        }
    }
}
