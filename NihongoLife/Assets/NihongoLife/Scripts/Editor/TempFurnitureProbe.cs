#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Temporary: renders furniture models to learn their default facing. Deleted after the bedroom build.
public static class TempFurnitureProbe
{
    const string Kit = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
    static readonly string[] Models = { "bedSingle", "desk", "chairDesk", "bookcaseOpen", "bookcaseClosedDoors", "kitchenSink", "kitchenStoveElectric", "kitchenFridgeSmall", "cabinetTelevision", "televisionModern", "tableCoffee", "laptop", "lampSquareTable", "sideTableDrawers", "coatRackStanding", "lampSquareCeiling", "kitchenCabinetUpper", "pillow" };

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var sb = new StringBuilder();
        int i = 0;
        foreach (var m in Models)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + m + ".fbx");
            if (src == null) { sb.AppendLine(m + " MISSING"); continue; }
            var holder = new GameObject(m);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.SetParent(holder.transform, false);
            var b = Bounds(go);
            float k = 1.6f / Mathf.Max(b.size.x, b.size.y, b.size.z);
            go.transform.localScale *= k;
            b = Bounds(go);
            go.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            holder.transform.position = new Vector3((i % 6) * 3f, 0, -(i / 6) * 3f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = holder.transform.position + new Vector3(0, 0.02f, 1.1f);
            marker.transform.localScale = new Vector3(1.2f, 0.04f, 0.12f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.color = Color.red; marker.GetComponent<Renderer>().sharedMaterial = mat;
            sb.AppendLine($"{m} rawsize={b.size / k}");
            i++;
        }
        float x = 18f;
        var light = new GameObject("L").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, 30, 0);
        RenderSettings.ambientLight = Color.gray;
        Shot("persp", new Vector3(7.5f, 9f, -16f), new Vector3(7.5f, 0f, -3f), x);
        Shot("top", new Vector3(7.5f, 20f, -3f), new Vector3(7.5f, 0, -2.99f), x);
        File.WriteAllText("C:/Users/Admin/AppData/Local/Temp/claude/d--VTC-Academy-NihongoLife-NihongoLife/5b7a3a00-a453-4306-a50c-33d0c7ec46c9/scratchpad/probe.txt", sb.ToString());
        EditorApplication.Exit(0);
    }

    public static void Room()
    {
        EditorSceneManager.OpenScene("Assets/NihongoLife/Scenes/45_HomeBedroom.unity");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.33f, 0.36f);
        var ceiling = GameObject.Find("HomeBedroom_YourRoom/Room/Shell/Ceiling").GetComponent<Renderer>();
        RoomShot("room_door", new Vector3(-2.2f, 2.0f, -2.6f), new Vector3(0.6f, 0.7f, 1.6f), false);
        RoomShot("room_desk", new Vector3(3.0f, 1.9f, -2.5f), new Vector3(-1.5f, 0.6f, 1.6f), false);
        RoomShot("room_bed", new Vector3(0.8f, 1.8f, 0.8f), new Vector3(-2.6f, 0.7f, 2.0f), false);
        RoomShot("room_kitchen", new Vector3(0.4f, 1.7f, -1.6f), new Vector3(-3.4f, 0.8f, -0.6f), false);
        RoomShot("room_back", new Vector3(0.6f, 1.8f, 2.4f), new Vector3(0f, 0.9f, -3f), false);
        ceiling.enabled = false;
        RoomShot("room_top", new Vector3(0f, 12f, 0f), new Vector3(0f, 0f, 0.001f), true);
        EditorApplication.Exit(0);
    }

    static void RoomShot(string name, Vector3 pos, Vector3 look, bool ortho)
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled = false;
        var cam = new GameObject("C").AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look);
        cam.orthographic = ortho; cam.orthographicSize = 3.4f; cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f;
        cam.backgroundColor = Color.black; cam.clearFlags = CameraClearFlags.SolidColor;
        var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var t = new Texture2D(1600, 900, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        File.WriteAllBytes($"C:/Users/Admin/AppData/Local/Temp/claude/d--VTC-Academy-NihongoLife-NihongoLife/5b7a3a00-a453-4306-a50c-33d0c7ec46c9/scratchpad/{name}.png", t.EncodeToPNG());
        Object.DestroyImmediate(cam.gameObject);
    }

    static void Shot(string name, Vector3 pos, Vector3 look, float width)
    {
        var cam = new GameObject("C").AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look);
        cam.orthographic = name == "top"; cam.orthographicSize = 5.5f; cam.fieldOfView = 40f;
        cam.backgroundColor = new Color(0.8f, 0.85f, 0.9f); cam.clearFlags = CameraClearFlags.SolidColor;
        var rt = new RenderTexture(1800, 1100, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var t = new Texture2D(1800, 1100, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 1800, 1100), 0, 0);
        File.WriteAllBytes($"C:/Users/Admin/AppData/Local/Temp/claude/d--VTC-Academy-NihongoLife-NihongoLife/5b7a3a00-a453-4306-a50c-33d0c7ec46c9/scratchpad/probe_{name}.png", t.EncodeToPNG());
        Object.DestroyImmediate(cam.gameObject);
    }

    static Bounds Bounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
}
#endif
