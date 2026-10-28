using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Exam
{
    /// <summary>
    /// The one place AI grading timeouts come from: GameControlDatabase.aiRequestTimeoutSeconds (default 60 s) for
    /// text requests; requests carrying audio get twice that (default 120 s). Callers that wait for a grade wait
    /// longer than the HTTP side can take (two attempts plus the retry pause), so they never give up first.
    /// </summary>
    public static class AiGradingTimeouts
    {
        public const int DefaultTextSeconds = 60;

        public static int TextSeconds
        {
            get
            {
                int seconds = DefaultTextSeconds;
                if (GameServices.TryGet(out GameControlService control) && control.Database != null)
                    seconds = control.Database.aiRequestTimeoutSeconds;
                return Mathf.Clamp(seconds, 10, 180);
            }
        }

        public static int AudioSeconds => TextSeconds * 2;

        public static int HttpSeconds(bool hasAudio) => hasAudio ? AudioSeconds : TextSeconds;

        /// <summary>How long a caller waits for one grading call: two HTTP attempts, the retry pause, and a margin.</summary>
        public static float ClientWaitSeconds(bool hasAudio) => HttpSeconds(hasAudio) * 2f + 10f;
    }
}
