#if (UNITY_WEBGL && !UNITY_EDITOR) || NL_WEBGL_CHECK
// Web build stand-in for Agora's VideoSurface (see AgoraWebShim.cs). Own file: a MonoBehaviour needs a matching file name.
using UnityEngine;

namespace Agora.Rtc
{
    /// <summary>Stands in for Agora's video view component: draws nothing on the web.</summary>
    internal sealed class VideoSurface : MonoBehaviour
    {
        public void SetForUser(uint uid, string channelId, VIDEO_SOURCE_TYPE sourceType) { }
        public void SetEnable(bool enable) { }
    }
}
#endif
