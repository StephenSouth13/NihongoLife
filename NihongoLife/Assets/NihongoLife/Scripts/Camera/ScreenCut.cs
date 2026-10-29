using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Cameras
{
    /// <summary>
    /// A quick fade-to-black/fade-in around a teleport (shop doors): the player is moved and the camera snapped while the
    /// screen is dark, so the camera never glides through walls or whips to the new spot.
    /// </summary>
    public class ScreenCut : MonoBehaviour
    {
        private static ScreenCut _instance;
        private CanvasGroup _group;
        public static bool Running { get; private set; }
        /// <summary>Screen alpha right now (0 = clear, 1 = black). Used by tests.</summary>
        public static float Alpha => _instance != null && _instance._group != null ? _instance._group.alpha : 0f;

        public static void Run(Action atBlack, float fadeOut = 0.14f, float fadeIn = 0.22f)
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenCut") { hideFlags = HideFlags.None };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenCut>();
                _instance.Build();
            }
            if (Running) { atBlack?.Invoke(); return; }
            _instance.StartCoroutine(_instance.Routine(atBlack, fadeOut, fadeIn));
        }

        /// <summary>
        /// H1: on entering a world scene the camera, lights and streamed tiles settle over the first ~0.4 s (measured: frame change 8x the
        /// steady level in frames 1-8), which showed as a flicker. The screen stays black while that happens and then fades in.
        /// </summary>
        public static void RevealFromBlack(float hold = 0.9f, float fade = 0.5f)
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenCut");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenCut>();
                _instance.Build();
            }
            if (Running) return;
            _instance._group.alpha = 1f;
            _instance.StartCoroutine(_instance.Reveal(hold, fade));
        }

        private IEnumerator Reveal(float hold, float fade)
        {
            Running = true;
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime) { _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fade); yield return null; }
            _group.alpha = 0f;
            Running = false;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f; _group.blocksRaycasts = false; _group.interactable = false;
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = img.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
        }

        private IEnumerator Routine(Action atBlack, float fadeOut, float fadeIn)
        {
            Running = true;
            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime) { _group.alpha = Mathf.SmoothStep(0f, 1f, t / fadeOut); yield return null; }
            _group.alpha = 1f;
            try { atBlack?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            yield return null; yield return null;   // let the camera settle on the new spot before it is revealed
            for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime) { _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fadeIn); yield return null; }
            _group.alpha = 0f;
            Running = false;
        }
    }
}