using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Cameras
{
    /// <summary>
    /// H1 (map flicker) root cause: the world camera ran with near 0.03 / far 1000. A 24-bit depth buffer then resolves only
    /// ~5 mm at 50 m and ~45 mm at 150 m, so every pair of ground slabs a few millimetres apart (road, kerb, grass, paint)
    /// z-fights and shimmers as the camera moves. Keeping near/far tight gives ~10x the precision.
    /// Applies to the main world camera only (UI preview cameras are not touched), re-checked when a scene loads and once a second.
    /// </summary>
    public sealed class DepthPrecision : MonoBehaviour
    {
        public const float Near = 0.12f, Far = 450f;
        private float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<DepthPrecision>() != null) return;
            var go = new GameObject("DepthPrecision");
            DontDestroyOnLoad(go);
            go.AddComponent<DepthPrecision>();
            SceneManager.sceneLoaded += (s, m) => { Apply(); if (NihongoLife.World.AmbientWildlife.SceneHasWildlife(s.name)) ScreenCut.RevealFromBlack(); };
            Apply();
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            Apply();
        }

        public static void Apply()
        {
            var cam = Camera.main;
            if (cam == null || cam.targetTexture != null || cam.orthographic) return;
            if (cam.nearClipPlane < Near) cam.nearClipPlane = Near;
            if (cam.farClipPlane > Far) cam.farClipPlane = Far;
        }
    }
}