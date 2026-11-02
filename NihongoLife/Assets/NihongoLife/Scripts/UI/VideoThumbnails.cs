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
