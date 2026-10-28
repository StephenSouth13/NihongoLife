using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.English
{
    /// <summary>Browser speech for the lightweight built-in listening course. Packaged IELTS audio never uses TTS.</summary>
    public static class EnglishSpeech
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void NihongoLifeSpeakEnglish(string text);
#endif

        public static bool Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
#if UNITY_WEBGL && !UNITY_EDITOR
            NihongoLifeSpeakEnglish(text);
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>Optional offline audio, deliberately outside Assets/Git at LocalContent/English/audio.</summary>
    public static class EnglishAudioLibrary
    {
        public static string Root
        {
            get
            {
                string custom = Environment.GetEnvironmentVariable("NIHONGOLIFE_LOCAL_CONTENT");
                string local = string.IsNullOrWhiteSpace(custom) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalContent")) : custom;
                return Path.Combine(local, "English", "audio");
            }
        }

        public static string PathFor(string lessonId, int questionIndex) => Path.Combine(Root, $"{lessonId}_{questionIndex + 1:00}.wav");

        public static IEnumerator Load(string lessonId, int questionIndex, Action<AudioClip> done)
        {
            string path = PathFor(lessonId, questionIndex);
            if (!File.Exists(path)) { done(null); yield break; }
            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.WAV);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) { done(null); yield break; }
            AudioClip clip = null;
            try { clip = DownloadHandlerAudioClip.GetContent(request); } catch { }
            done(clip);
        }
    }
}
