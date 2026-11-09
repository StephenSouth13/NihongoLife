using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>A video's picture from YouTube, kept for the session. A picture that cannot load leaves the coloured card behind it.</summary>
    public static class VideoThumbnails
    {
        private static readonly Dictionary<string, Texture2D> Loaded = new Dictionary<string, Texture2D>();

        /// <summary>A lesson's picture: the game's own lessons carry theirs under StreamingAssets, the channel's come from YouTube.</summary>
        public static IEnumerator Load(NihongoLife.StudyModes.Video.VideoLesson lesson, RawImage target)
        {
            if (lesson == null || target == null) yield break;
            if (string.IsNullOrEmpty(lesson.thumb)) { yield return Load(lesson.id, target); yield break; }
            if (!Loaded.TryGetValue(lesson.thumb, out var texture))
            {
                string url = Application.streamingAssetsPath + "/" + lesson.thumb;
                if (!url.Contains("://")) url = "file://" + url; // desktop: a path; the browser: already an http URL
                using var request = UnityWebRequestTexture.GetTexture(url);
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) yield break;
                texture = DownloadHandlerTexture.GetContent(request);
                Loaded[lesson.thumb] = texture;
            }
            if (target == null) yield break;
            target.texture = texture;
            target.color = Color.white;
        }

        public static IEnumerator Load(string videoId, RawImage target)
        {
            if (string.IsNullOrEmpty(videoId) || target == null) yield break;
            if (!Loaded.TryGetValue(videoId, out var texture))
            {
                using var request = UnityWebRequestTexture.GetTexture("https://i.ytimg.com/vi/" + videoId + "/mqdefault.jpg");
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) yield break;
                texture = DownloadHandlerTexture.GetContent(request);
                Loaded[videoId] = texture;
            }
            if (target == null) yield break;
            target.texture = texture;
            target.color = Color.white;
        }
    }
}
