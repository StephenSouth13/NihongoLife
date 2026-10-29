using System.IO;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>Renders a picture from a free camera into Bao_Cao/&lt;topic&gt;/ (visual proof of a play-mode test).</summary>
    public static class TestShots
    {
        public static string Folder(string topic) => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao", topic));

        public static void Shot(string topic, string name, Vector3 at, Vector3 look, float fov = 50f)
        {
            var go = new GameObject("TestShotCamera");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.03f;
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            string folder = Folder(topic);
            Directory.CreateDirectory(folder);
            try { File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG()); }
            catch (IOException e) { Debug.LogWarning("Capture " + name + " skipped (the file is open elsewhere): " + e.Message); }
            RenderTexture.active = null;
            camera.targetTexture = null;
            rt.Release();
            Object.Destroy(tex);
            Object.Destroy(go);
        }

        /// <summary>A picture of a character from the front-right at chest height.</summary>
        public static void Portrait(string topic, string name, Transform body, float distance = 1.6f, float height = 0.8f)
        {
            Vector3 front = body.forward * distance + body.right * distance * 0.45f;
            Shot(topic, name, body.position + front + Vector3.up * (height + 0.25f), body.position + Vector3.up * height);
        }
    }
}
