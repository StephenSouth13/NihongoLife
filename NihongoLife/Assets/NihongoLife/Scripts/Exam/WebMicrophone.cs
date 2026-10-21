#if (UNITY_WEBGL && !UNITY_EDITOR) || NL_WEBGL_CHECK
// UnityEngine.Microphone does not exist in WebGL builds (CS0103). This stand-in has the members MicRecorder and
// SpeechPracticeController use and reports "no microphone", so their existing messages explain it to the player.
// Being in the NihongoLife namespace, it is found before UnityEngine.Microphone by code inside NihongoLife.*.
// NL_WEBGL_CHECK: only to compile the web stand-ins in the editor as a check.
using UnityEngine;

namespace NihongoLife
{
    internal static class Microphone
    {
        public static string[] devices => System.Array.Empty<string>();
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency) => null;
        public static void End(string deviceName) { }
        public static bool IsRecording(string deviceName) => false;
        public static int GetPosition(string deviceName) => 0;
    }
}
#endif
