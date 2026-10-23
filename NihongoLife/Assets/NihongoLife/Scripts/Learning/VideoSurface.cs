#if UNITY_WEBGL
// Web build stand-in for Agora's VideoSurface. The browser draws the camera into an HTML element
// positioned over this RawImage; WebGL cannot cheaply copy a MediaStream into a Unity texture.
using Agora.Rtc;
using UnityEngine;
using UnityEngine.UI;

namespace Agora.Rtc
{
    internal sealed class VideoSurface : MonoBehaviour
    {
        static readonly Vector3[] Corners = new Vector3[4];
        string _divId;
        bool _enabled;

        public void SetForUser(uint uid, string channelId, VIDEO_SOURCE_TYPE sourceType)
        {
            _divId = sourceType == VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA_PRIMARY || uid == 0
                ? "nl-agora-local"
                : "nl-agora-remote";
        }

        public void SetEnable(bool enable)
        {
            _enabled = enable;
            if (string.IsNullOrEmpty(_divId)) return;
            if (!enable) AgoraWebNative.NL_AgoraSetVideoVisible(_divId, 0);
        }

        void LateUpdate()
        {
            if (string.IsNullOrEmpty(_divId)) return;
            var image = GetComponent<RawImage>();
            if (image == null || !_enabled || !image.gameObject.activeInHierarchy)
            {
                AgoraWebNative.NL_AgoraSetVideoVisible(_divId, 0);
                return;
            }
            var rect = image.rectTransform;
            rect.GetWorldCorners(Corners);
            Camera cam = null;
            var canvas = image.canvas;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera;
            Vector3 bl = cam != null ? cam.WorldToScreenPoint(Corners[0]) : Corners[0];
            Vector3 tr = cam != null ? cam.WorldToScreenPoint(Corners[2]) : Corners[2];
            if (bl.z < 0f && cam != null)
            {
                AgoraWebNative.NL_AgoraSetVideoVisible(_divId, 0);
                return;
            }
            AgoraWebNative.NL_AgoraSetVideoVisible(_divId, 1);
            AgoraWebNative.NL_AgoraSetVideoRect(_divId, bl.x, bl.y, tr.x - bl.x, tr.y - bl.y, Screen.width, Screen.height);
        }

        void OnDisable()
        {
            if (!string.IsNullOrEmpty(_divId)) AgoraWebNative.NL_AgoraSetVideoVisible(_divId, 0);
        }
    }
}
#endif
