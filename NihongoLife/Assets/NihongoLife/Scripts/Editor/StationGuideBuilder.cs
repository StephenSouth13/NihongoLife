#if UNITY_EDITOR
using System;
using System.Linq;
using NihongoLife.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Hibari Station's hall: a standing guide board (路線図・周辺案内) with the line — every stop, its fare, the minutes
    /// from Hibari and what is there (the school, Minato's sushi, Midori's farm, the airport and the flight to Phú Quốc)
    /// — and the areas of the town; plus a Hibari Shinkin Bank ATM (Bank/BankAtm) beside it. The line is read from
    /// StationTravelController.Line, so a new stop updates the board on the next build.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.StationGuideBuilder.Build
    /// </summary>
    public static class StationGuideBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/20_StationDistrict.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/Station";
        /// <summary>Against the hall's north wall, east of the stairs passage (the ticket counter and machine are west),
        /// facing the gates: read by someone looking north, +Z.</summary>
        private static readonly Vector3 At = new Vector3(811f, 0f, 11.3f);

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var zone = scene.GetRootGameObjects().First(r => r.name == "StationDistrict_Zone").transform;
                foreach (Transform old in zone.Cast<Transform>().Where(t => t.name == "StationGuide" || t.name == "StationATM").ToArray())
                    Object.DestroyImmediate(old.gameObject);
                var guide = Group(zone, "StationGuide");
                guide.position = At + Vector3.down * 0.68f; // the hall's ceiling is low: the board top stays under it
                guide.rotation = Quaternion.identity; // faces the hall: read by someone looking north, back from the gates

                var navy = Lit(MatDir, "Guide_Navy", new Color(0.1f, 0.16f, 0.3f), 0.3f);
                var panel = Lit(MatDir, "Guide_Panel", new Color(0.97f, 0.97f, 0.95f), 0.25f, new Color(0.22f, 0.22f, 0.22f));
                var steel = Lit(MatDir, "Guide_Steel", new Color(0.6f, 0.62f, 0.65f), 0.7f);
                var line = Lit(MatDir, "Guide_Line", new Color(0.13f, 0.6f, 0.36f), 0.3f);
                var dot = Lit(MatDir, "Guide_Dot", Color.white, 0.3f);
                var gold = Lit(MatDir, "Guide_Gold", new Color(0.96f, 0.72f, 0.24f), 0.3f);

                // The stand: two posts, a navy frame, the white panel (2.8 m × 1.7 m).
                Block(guide, "PostL", new Vector3(-1.5f, 1.0f, 0f), new Vector3(0.12f, 2.0f, 0.12f), steel, false);
                Block(guide, "PostR", new Vector3(1.5f, 1.0f, 0f), new Vector3(0.12f, 2.0f, 0.12f), steel, false);
                Block(guide, "Frame", new Vector3(0f, 1.75f, 0f), new Vector3(3.1f, 1.95f, 0.1f), navy, true);
                Block(guide, "Panel", new Vector3(0f, 1.72f, -0.055f), new Vector3(2.9f, 1.72f, 0.02f), panel, false);
                Block(guide, "Header", new Vector3(0f, 2.48f, -0.07f), new Vector3(2.9f, 0.22f, 0.02f), navy, false);
                Text(guide, "Title", "ろせんず · Bản đồ tuyến & khu vực · Route map", new Vector3(0f, 2.48f, -0.085f), 0f, 0.13f, Color.white, 2.8f);

                // The line, left to right, a dot per stop with its name, fare and minutes from Hibari.
                var stops = StationTravelController.Line;
                float x0 = -1.2f, x1 = 1.2f, y = 2.02f;
                Block(guide, "Line", new Vector3(0f, y, -0.07f), new Vector3(x1 - x0, 0.05f, 0.01f), line, false);
                for (int i = 0; i < stops.Length; i++)
                {
                    var stop = stops[i];
                    float x = Mathf.Lerp(x0, x1, i / (float)(stops.Length - 1));
                    var marker = Cylinder(guide, "Stop_" + stop.Id, new Vector3(x, y, -0.075f), new Vector3(0.13f, 0.01f, 0.13f), i == 0 ? gold : dot, false);
                    marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    string fare = stop.Price > 0 ? $"¥{stop.Price:N0} · {i * 6}分" : "ここ · Bạn ở đây";
                    var label = Text(guide, "StopName_" + stop.Id, $"<b>{stop.Ja}</b>\n<size=70%>{stop.Vi}\n<color=#1F5E3A>{fare}</color></size>", new Vector3(x, y - 0.2f, -0.085f), 0f, 0.07f, new Color(0.1f, 0.12f, 0.16f), 0.62f);
                    label.textWrappingMode = TextWrappingModes.Normal;
                    var place = Text(guide, "StopPlace_" + stop.Id, stop.Place, new Vector3(x, y + 0.14f, -0.085f), 0f, 0.05f, new Color(0.3f, 0.36f, 0.44f), 0.62f);
                    place.textWrappingMode = TextWrappingModes.Normal;
                }
                // The areas of the town and beyond.
                var areas = Text(guide, "Areas",
                    "<b>ひばり町 · Hibari-chō</b>  ひばりマート (konbini, ATM) · ひばり寿司 · ひばりハイツ (nhà) · ひばりクリニック · ゲームセンター · こうえん\n" +
                    "<b>がくえんまえ</b>  Trường Nhật ngữ Hibari — lớp học, kỳ thi JLPT / IELTS\n" +
                    "<b>ミナト</b>  Phố cảng, chợ cá, Sushi Hibari  ·  <b>みどりじま</b>  Đảo Xanh: nông trại, câu cá\n" +
                    "<b>くうこう</b>  Sân bay → bay thẳng tới <b>Phú Quốc (PQC)</b> — Dương Đông, nhà hàng Gánh Ngon, biển và bãi cát",
                    new Vector3(0f, 1.27f, -0.085f), 0f, 0.058f, new Color(0.12f, 0.15f, 0.2f), 2.7f, TextAlignmentOptions.Left);
                areas.textWrappingMode = TextWrappingModes.Normal;
                Text(guide, "Hint", "きっぷは けんばいき / Mua vé ở máy hoặc quầy anh Kimura · 2ばんせん", new Vector3(0f, 0.95f, -0.085f), 0f, 0.055f, new Color(0.6f, 0.38f, 0.1f), 2.7f);

                // A Hibari Shinkin ATM beside the board.
                var atm = Group(zone, "StationATM");
                atm.position = At + new Vector3(-3.6f, 0f, -0.1f);
                atm.rotation = Quaternion.identity;
                var body = Lit(MatDir, "ATM_Body", new Color(0.86f, 0.87f, 0.88f), 0.5f);
                var screen = Lit(MatDir, "ATM_Screen", new Color(0.1f, 0.25f, 0.45f), 0.8f, new Color(0.1f, 0.3f, 0.6f) * 1.2f);
                Block(atm, "Body", new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 1.6f, 0.7f), body, true);
                Block(atm, "Top", new Vector3(0f, 1.72f, 0.05f), new Vector3(0.9f, 0.24f, 0.6f), gold, false);
                Block(atm, "Screen", new Vector3(0f, 1.15f, -0.33f), new Vector3(0.6f, 0.38f, 0.04f), screen, false);
                Block(atm, "Keypad", new Vector3(0f, 0.85f, -0.36f), new Vector3(0.45f, 0.06f, 0.18f), navy, false);
                Text(atm, "Label", "ATM  ひばり信用金庫\n<size=70%>Ngân hàng · vay · trả nợ</size>", new Vector3(0f, 1.72f, -0.26f), 0f, 0.07f, new Color(0.1f, 0.12f, 0.16f), 0.86f);
                var use = Trigger(atm, "UseATM", new Vector3(0f, 1f, -0.7f), new Vector3(1.2f, 2f, 1f));
                use.AddComponent<NihongoLife.Bank.BankAtm>();

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                Debug.Log("[StationGuideBuilder] Route map board and ATM placed in Hibari Station's hall.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
#endif
