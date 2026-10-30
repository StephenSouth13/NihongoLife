using System.Collections;
using System.IO;
using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>Takes a small screenshot (320x180 PNG in persistentDataPath/album) a few seconds after a new album page is added
    /// and stores its path on the page. Skipped in batch mode (tests), so the album falls back to text cards there.</summary>
    public sealed class AlbumSnapshotter : MonoBehaviour
    {
        public const float Delay = 3f;
        private static AlbumSnapshotter _runner;

        public static string Folder => Path.Combine(Application.persistentDataPath, "album");

        public static void Request(string pageId)
        {
            if (Application.isBatchMode || !Application.isPlaying) return;
            if (_runner == null)
            {
                var go = new GameObject("AlbumSnapshotter") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<AlbumSnapshotter>();
            }
            _runner.StartCoroutine(_runner.Capture(pageId));
        }

        private IEnumerator Capture(string pageId)
        {
            yield return new WaitForSecondsRealtime(Delay);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot == null) yield break;
            var rt = RenderTexture.GetTemporary(320, 180);
            Graphics.Blit(shot, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var small = new Texture2D(320, 180, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            small.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(shot);
            try
            {
                Directory.CreateDirectory(Folder);
                string path = Path.Combine(Folder, pageId + ".png");
                File.WriteAllBytes(path, small.EncodeToPNG());
                var page = StudyProgressStore.Current.album.pages.Find(p => p.id == pageId);
                if (page != null) { page.image = path; StudyProgressStore.Save(); }
            }
            catch (System.Exception e) { Debug.LogWarning("[Album] snapshot failed: " + e.Message); }
            finally { Destroy(small); }
        }
    }
}
