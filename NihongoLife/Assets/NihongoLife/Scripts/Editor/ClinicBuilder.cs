#if UNITY_EDITOR
using System;
using System.Linq;
using NihongoLife.Clinic;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Progression;
using NihongoLife.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Hibari Clinic (approved scene 35_HibariClinic, 内科・小児科), a Japanese neighbourhood clinic:
    ///  • entrance with a slipper rack and opening hours, reception (受付) with Nurse Sato, the medicine window (お薬),
    ///    the waiting room (benches, call-number board, TV, magazines, water), the consultation room (診察室) with
    ///    Dr Mori, an exam bed behind a curtain and a sink, the health-check corner (scale, ring eye chart) and two
    ///    recovery beds (a player who faints wakes up here — spawn "clinic_bed");
    ///  • patient spots (ClinicSpot) and the clinic shift's stations (JobStation: check-in, triage, medicine) with
    ///    patients who only appear during a shift, and the job board;
    ///  • the entrance on StreetBuilding_S_3 in 90_TestSandbox (opposite Sushi Hibari), portal + return spawn;
    ///  • build settings.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ClinicBuilder.Build
    /// </summary>
    public static class ClinicBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/35_HibariClinic.unity";
        private const string CityPath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/Clinic";
        private const string Kit = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string Quest = "job_clinic_shift";
        private const string CityBuilding = "StreetBuilding_S_3";
        public static readonly Vector3 O = new Vector3(650f, 0f, 0f);
        private const float HalfX = 10f, HalfZ = 8f, Height = 3.2f;

        private static Material _floor, _wall, _ceiling, _mint, _wood, _white, _glass, _curtain, _dark, _sign, _lamp, _rubber, _chart;
        private static Material _walkway, _rug, _water, _red, _yellow, _blue, _green, _orange, _skyArt;

        public static void Build()
        {
            try
            {
                Materials();
                BuildZone();
                BuildCityEntrance();
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                Debug.Log("[ClinicBuilder] Hibari Clinic built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>Rebuilds only the clinic scene (interior, staff, townspeople's route), leaving the town untouched.
        /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ClinicBuilder.BuildInterior</summary>
        public static void BuildInterior()
        {
            try
            {
                Materials();
                BuildZone();
                AssetDatabase.SaveAssets();
                Debug.Log("[ClinicBuilder] Hibari Clinic interior rebuilt.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void Materials()
        {
            _floor = Lit(MatDir, "Clinic_Floor", new Color(0.82f, 0.86f, 0.84f), 0.55f);
            _wall = Lit(MatDir, "Clinic_Wall", new Color(0.96f, 0.96f, 0.94f), 0.25f);
            _ceiling = Lit(MatDir, "Clinic_Ceiling", new Color(0.98f, 0.98f, 0.97f), 0.1f);
            _mint = Lit(MatDir, "Clinic_Mint", new Color(0.36f, 0.72f, 0.64f), 0.35f);
            _wood = Lit(MatDir, "Clinic_Wood", new Color(0.72f, 0.56f, 0.38f), 0.3f);
            _white = Lit(MatDir, "Clinic_White", Color.white, 0.5f);
            _glass = Lit(MatDir, "Clinic_Glass", new Color(0.8f, 0.92f, 0.96f, 0.25f), 0.95f, transparent: true);
            _curtain = Lit(MatDir, "Clinic_Curtain", new Color(0.62f, 0.82f, 0.86f), 0.1f);
            _dark = Lit(MatDir, "Clinic_Dark", new Color(0.15f, 0.17f, 0.2f), 0.4f);
            _sign = Lit(MatDir, "Clinic_Sign", new Color(0.1f, 0.38f, 0.42f), 0.3f);
            _lamp = Lit(MatDir, "Clinic_Lamp", Color.white, 0.2f, new Color(1f, 1f, 0.96f) * 1.6f);
            _rubber = Lit(MatDir, "Clinic_Rubber", new Color(0.3f, 0.33f, 0.36f), 0.2f);
            _chart = Lit(MatDir, "Clinic_EyeChart", new Color(0.99f, 0.99f, 0.97f), 0.1f, new Color(0.25f, 0.25f, 0.24f));
            _walkway = Lit(MatDir, "Clinic_Walkway", new Color(0.62f, 0.84f, 0.78f), 0.4f);
            _rug = Lit(MatDir, "Clinic_Rug", new Color(0.93f, 0.86f, 0.72f), 0.05f);
            _water = Lit(MatDir, "Clinic_AquariumWater", new Color(0.35f, 0.7f, 0.92f, 0.45f), 0.95f, new Color(0.05f, 0.18f, 0.28f), transparent: true);
            _red = Lit(MatDir, "Clinic_ToyRed", new Color(0.9f, 0.32f, 0.3f), 0.3f);
            _yellow = Lit(MatDir, "Clinic_ToyYellow", new Color(0.98f, 0.82f, 0.3f), 0.3f);
            _blue = Lit(MatDir, "Clinic_ToyBlue", new Color(0.3f, 0.55f, 0.92f), 0.3f);
            _green = Lit(MatDir, "Clinic_ToyGreen", new Color(0.4f, 0.78f, 0.45f), 0.3f);
            _orange = Lit(MatDir, "Clinic_Fish", new Color(1f, 0.55f, 0.15f), 0.6f, new Color(0.35f, 0.15f, 0f));
            _skyArt = Lit(MatDir, "Clinic_ArtSky", new Color(0.55f, 0.78f, 0.95f), 0.1f, new Color(0.12f, 0.16f, 0.2f));
        }

        private static Vector3 P(float x, float y, float z) => O + new Vector3(x, y, z);

        // ─────────── The clinic ───────────

        private static void BuildZone()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HibariClinic_Zone");
            root.AddComponent<SceneZoneVisibility>();
            var shell = Group(root.transform, "Shell");
            var furniture = Group(root.transform, "Furniture");
            var staff = Group(root.transform, "Staff");
            var spots = Group(root.transform, "PatientSpots");
            var job = Group(root.transform, "ClinicShift");
            var signs = Group(root.transform, "Signs");

            // Shell: floor, walls (door gap south), ceiling light panels, a mint wainscot band.
            Block(shell, "Floor", P(0f, -0.1f, 0f), new Vector3(HalfX * 2f, 0.2f, HalfZ * 2f), _floor, true);
            Block(shell, "Ceiling", P(0f, Height, 0f), new Vector3(HalfX * 2f, 0.2f, HalfZ * 2f), _ceiling, false);
            Block(shell, "Wall_N", P(0f, Height / 2f, HalfZ), new Vector3(HalfX * 2f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_W", P(-HalfX, Height / 2f, 0f), new Vector3(0.25f, Height, HalfZ * 2f), _wall, true);
            Block(shell, "Wall_E", P(HalfX, Height / 2f, 0f), new Vector3(0.25f, Height, HalfZ * 2f), _wall, true);
            Block(shell, "Wall_S_L", P(-5.75f, Height / 2f, -HalfZ), new Vector3(8.5f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_S_R", P(5.75f, Height / 2f, -HalfZ), new Vector3(8.5f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_S_Header", P(0f, 2.75f, -HalfZ), new Vector3(3f, 0.9f, 0.25f), _wall, true);
            Block(shell, "EntranceGlass", P(0f, 1.15f, -HalfZ - 0.06f), new Vector3(3f, 2.3f, 0.04f), _glass, false);
            foreach (var (name, x, z, w, d) in new[] { ("Band_N", 0f, HalfZ - 0.14f, HalfX * 2f, 0.03f), ("Band_W", -HalfX + 0.14f, 0f, 0.03f, HalfZ * 2f), ("Band_E", HalfX - 0.14f, 0f, 0.03f, HalfZ * 2f) })
                Block(shell, name, P(x, 0.5f, z), new Vector3(w, 1f, d), _mint, false);
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = -1; iz <= 1; iz++)
                    Block(shell, $"LightPanel_{ix}_{iz}", P(ix * 6f, Height - 0.12f, iz * 5f), new Vector3(2.2f, 0.04f, 0.9f), _lamp, false);

            // Consultation room (north-west, x −10…−1, z 1…8): partition with a door, desk, exam bed, curtain, sink.
            Block(shell, "Partition_S_A", P(-7.5f, Height / 2f, 1f), new Vector3(5f, Height, 0.15f), _wall, true);
            Block(shell, "Partition_S_B", P(-1.75f, Height / 2f, 1f), new Vector3(1.5f, Height, 0.15f), _wall, true);
            Block(shell, "Partition_S_Header", P(-4f, 2.85f, 1f), new Vector3(3f, 0.7f, 0.15f), _wall, true);
            Block(shell, "Partition_E", P(-1f, Height / 2f, 4.5f), new Vector3(0.15f, Height, 7f), _wall, true);
            Block(shell, "ConsultDoorCurtain", P(-4f, 2.35f, 1.02f), new Vector3(2.8f, 0.5f, 0.03f), _curtain, false);
            Text(signs, "Sign_Consult", "しんさつしつ  診察室\n<size=55%>Phòng khám bệnh · Consultation</size>", P(-4f, 2.85f, 0.895f), 0f, 0.2f, Color.white, 3f);
            Block(signs, "Sign_Consult_Back", P(-4f, 2.85f, 0.912f), new Vector3(2.9f, 0.62f, 0.02f), _sign, false);
            var desk = Furn(furniture, "desk", "DoctorDesk", P(-7.6f, 0f, 5.2f), 90f, new Vector3(1.6f, 0.8f, 0.8f), true);
            Furn(furniture, "computerScreen", "DoctorScreen", P(-7.85f, 0.78f, 5.2f), 90f, new Vector3(0.5f, 0.45f, 0.2f), false);
            Furn(furniture, "computerKeyboard", "DoctorKeyboard", P(-7.45f, 0.78f, 5.2f), 90f, new Vector3(0.45f, 0.05f, 0.18f), false);
            Furn(furniture, "chairDesk", "DoctorChair", P(-8.6f, 0f, 5.2f), 90f, new Vector3(0.6f, 1.05f, 0.6f), true);
            Furn(furniture, "chairRounded", "PatientStool", P(-6.4f, 0f, 4.5f), -90f, new Vector3(0.5f, 0.55f, 0.5f), true);
            Furn(furniture, "bedSingle", "ExamBed", P(-3.2f, 0f, 6.6f), 90f, new Vector3(1.0f, 0.65f, 2.0f), true);
            Block(furniture, "ExamBedPaper", P(-3.2f, 0.66f, 6.6f), new Vector3(1.9f, 0.01f, 0.55f), _white, false);
            Block(furniture, "ExamCurtain", P(-3.2f, 1.3f, 5.3f), new Vector3(2.4f, 2.2f, 0.03f), _curtain, false);
            Block(furniture, "CurtainRail", P(-3.2f, 2.45f, 5.3f), new Vector3(2.5f, 0.04f, 0.04f), _rubber, false);
            Furn(furniture, "bathroomSink", "ConsultSink", P(-9.55f, 0f, 2.2f), 90f, new Vector3(0.6f, 1f, 0.5f), true);
            Furn(furniture, "bookcaseClosedDoors", "MedicalCabinet", P(-9.55f, 0f, 7.2f), 90f, new Vector3(1.0f, 1.9f, 0.45f), true);
            Text(signs, "Poster_Hands", "てあらい・うがい\n<size=60%>Rửa tay · súc miệng</size>", P(-9.84f, 1.7f, 3.6f), -90f, 0.14f, _sign.color, 1.4f);

            // Health-check corner and recovery beds (north-east, x 1…10, z 1…8).
            Block(shell, "Partition_HC_S", P(6.5f, Height / 2f, 1f), new Vector3(7f, Height, 0.15f), _wall, true);
            Text(signs, "Sign_HealthCheck", "けんこうしんだん  健康診断\n<size=55%>Khám sức khoẻ · Health check</size>", P(1.9f, 2.6f, 0.9f), 0f, 0.18f, _sign.color, 3.2f);
            Block(furniture, "Scale_Base", P(3.2f, 0.05f, 3.6f), new Vector3(0.7f, 0.1f, 0.7f), _rubber, true);
            Block(furniture, "Scale_Pole", P(3.2f, 1.05f, 3.95f), new Vector3(0.06f, 2.0f, 0.06f), _white, false);
            Block(furniture, "Scale_Head", P(3.2f, 1.7f, 3.75f), new Vector3(0.3f, 0.03f, 0.4f), _white, false);
            Text(furniture, "Scale_Label", "しんちょう・たいじゅう", P(3.2f, 2.2f, 3.94f), 0f, 0.1f, _sign.color, 1.4f);
            Block(furniture, "EyeChart", P(6.8f, 1.5f, HalfZ - 0.15f), new Vector3(0.9f, 1.4f, 0.03f), _chart, false);
            Text(furniture, "EyeChart_Rings", "⊂  ∪\n⊃  ∩  ⊂\n∪ ⊃ ∩ ⊂", P(6.8f, 1.55f, HalfZ - 0.18f), 0f, 0.16f, _dark.color, 0.8f);
            Block(furniture, "EyeTestLine", P(6.8f, 0.006f, 2.8f), new Vector3(0.9f, 0.01f, 0.08f), _mint, false);
            Text(furniture, "EyeTestLabel", "しりょくけんさ", P(6.8f, 2.4f, HalfZ - 0.18f), 0f, 0.12f, _sign.color, 1.4f);
            Furn(furniture, "bedSingle", "RecoveryBed_1", P(9.0f, 0f, 3.0f), 0f, new Vector3(1.0f, 0.65f, 2.0f), true);
            Furn(furniture, "bedSingle", "RecoveryBed_2", P(9.0f, 0f, 6.2f), 0f, new Vector3(1.0f, 0.65f, 2.0f), true);
            Block(furniture, "RecoveryCurtain", P(7.9f, 1.3f, 4.6f), new Vector3(0.03f, 2.2f, 2.2f), _curtain, false);
            Furn(furniture, "sideTableDrawers", "RecoveryTable", P(9.4f, 0f, 1.6f), 0f, new Vector3(0.5f, 0.6f, 0.45f), true);

            // Reception (受付) and the medicine window (お薬) along z = −3.4, staff behind them.
            Block(furniture, "Counter", P(-1.5f, 0.55f, -3.4f), new Vector3(8f, 1.1f, 0.6f), _wood, true);
            Block(furniture, "CounterTop", P(-1.5f, 1.12f, -3.4f), new Vector3(8.1f, 0.04f, 0.7f), _white, false);
            Block(furniture, "CounterGlass", P(1.8f, 1.6f, -3.4f), new Vector3(1.4f, 0.9f, 0.02f), _glass, false);
            Block(shell, "StaffBackWall", P(-1.5f, Height / 2f, -0.6f), new Vector3(8f, Height, 0.15f), _wall, true);
            Block(shell, "StaffSideWall_W", P(-5.5f, Height / 2f, -2f), new Vector3(0.15f, Height, 2.8f), _wall, true);
            Furn(furniture, "bookcaseClosed", "MedicineShelf", P(1.8f, 0f, -0.9f), 180f, new Vector3(1.6f, 1.9f, 0.4f), true);
            Furn(furniture, "computerScreen", "ReceptionScreen", P(-3.2f, 1.14f, -3.25f), 180f, new Vector3(0.45f, 0.4f, 0.18f), false);
            Block(signs, "Sign_Reception_Back", P(-3f, 2.5f, -3.38f), new Vector3(2.4f, 0.55f, 0.03f), _sign, false);
            Text(signs, "Sign_Reception", "うけつけ  受付\n<size=55%>Quầy tiếp nhận · Reception</size>", P(-3f, 2.5f, -3.42f), 0f, 0.2f, Color.white, 2.3f);
            Block(signs, "Sign_Medicine_Back", P(1.8f, 2.5f, -3.38f), new Vector3(1.8f, 0.55f, 0.03f), _mint, false);
            Text(signs, "Sign_Medicine", "おくすり  お薬\n<size=55%>Nhận thuốc · Medicine</size>", P(1.8f, 2.5f, -3.42f), 0f, 0.2f, Color.white, 1.8f);
            Text(signs, "Sign_Card", "ほけんしょう を おだしください\n<size=70%>Xin xuất trình thẻ bảo hiểm</size>", P(-3f, 1.32f, -3.72f), 0f, 0.07f, _sign.color, 1.4f);

            // Waiting room (south-east): benches, call board, TV, magazines, water, plants.
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 2; i++)
                    Furn(furniture, "benchCushion", $"WaitingBench_{row}_{i}", P(4.8f + i * 2.4f, 0f, -5.4f + row * 2.2f), 180f, new Vector3(2.0f, 0.85f, 0.6f), true);
            Block(furniture, "CallBoard", P(HalfX - 0.15f, 2.2f, -4.2f), new Vector3(0.05f, 0.7f, 1.6f), _dark, false);
            Text(furniture, "CallBoard_Text", "しんさつ ばんごう\n<size=180%><color=#7CF0C8>2</color></size>", P(HalfX - 0.19f, 2.2f, -4.2f), 90f, 0.12f, Color.white, 1.5f);
            Furn(furniture, "televisionModern", "WaitingTV", P(HalfX - 0.3f, 1.2f, -1.8f), -90f, new Vector3(1.2f, 0.75f, 0.12f), false);
            Furn(furniture, "bookcaseOpenLow", "MagazineRack", P(3.0f, 0f, -7.55f), 0f, new Vector3(1.2f, 0.8f, 0.35f), true);
            Furn(furniture, "kitchenFridgeSmall", "WaterDispenser", P(8.9f, 0f, -7.3f), 0f, new Vector3(0.5f, 1.2f, 0.45f), true);
            Furn(furniture, "pottedPlant", "Plant_E", P(9.3f, 0f, -0.4f), 0f, new Vector3(0.6f, 1.4f, 0.6f), true);
            Furn(furniture, "pottedPlant", "Plant_W", P(-9.3f, 0f, -7.2f), 0f, new Vector3(0.6f, 1.4f, 0.6f), true);
            Text(signs, "Poster_Mask", "マスクを つけましょう\n<size=60%>Hãy đeo khẩu trang</size>", P(HalfX - 0.2f, 1.6f, -6.4f), 90f, 0.13f, _sign.color, 1.4f);

            // Entrance: slipper rack, opening hours, the big clinic sign over the waiting room.
            Furn(furniture, "bookcaseOpenLow", "SlipperRack", P(-2.4f, 0f, -7.55f), 0f, new Vector3(1.2f, 0.8f, 0.35f), true);
            for (int i = 0; i < 4; i++) Block(furniture, "Slippers_" + i, P(-2.85f + i * 0.3f, 0.43f, -7.5f), new Vector3(0.22f, 0.05f, 0.28f), _mint, false);
            Text(signs, "Sign_Slippers", "スリッパに はきかえて ください\n<size=70%>Xin đổi sang dép đi trong nhà</size>", P(-2.4f, 1.15f, -7.83f), 180f, 0.08f, _sign.color, 1.6f);
            Block(signs, "Hours_Back", P(-6.6f, 1.6f, -7.85f), new Vector3(1.5f, 1.0f, 0.02f), _white, false);
            Text(signs, "Hours", "しんりょう じかん\n<size=80%>月〜土  9:00〜18:00\n日・祝 やすみ</size>\n<size=65%>Giờ khám: T2–T7 9:00–18:00</size>", P(-6.6f, 1.6f, -7.82f), 180f, 0.1f, _sign.color, 1.4f);
            Block(signs, "MainSign_Back", P(4.6f, 2.55f, HalfZ * -1f + 0.16f), new Vector3(4.6f, 0.7f, 0.03f), _sign, false);
            Text(signs, "MainSign", "ひばりクリニック  <size=60%>内科・小児科</size>\n<size=50%>Phòng khám Hibari · Nội khoa · Nhi khoa</size>", P(4.6f, 2.55f, -HalfZ + 0.2f), 180f, 0.22f, Color.white, 4.4f);

            Decor(shell, furniture, signs);

            // Staff: Nurse Sato at reception, the pharmacist at the window, Dr Mori at the desk.
            Person(staff, "Nurse_Sato", P(-3f, 0f, -2.6f), 180f, "さとう · Điều dưỡng Sato", "かんごし");
            Person(staff, "Pharmacist", P(1.8f, 0f, -2.6f), 180f, "やくざいし · Dược sĩ", "おくすり");
            Person(staff, "Doctor_Mori", P(-8.4f, 0f, 4.4f), 90f, "もり せんせい · Bác sĩ Mori", "いんちょう");

            // Patient spots (the player is the patient).
            Spot(spots, "Spot_Reception", P(-3f, 1f, -4.4f), new Vector3(2.2f, 2f, 1.4f), ClinicSpotKind.Reception);
            Spot(spots, "Spot_Medicine", P(1.8f, 1f, -4.4f), new Vector3(1.6f, 2f, 1.4f), ClinicSpotKind.Pharmacy);
            Spot(spots, "Spot_Doctor", P(-6.4f, 1f, 4.5f), new Vector3(2.2f, 2f, 2.2f), ClinicSpotKind.Doctor);
            Spot(spots, "Spot_Scale", P(3.2f, 1f, 3.2f), new Vector3(1.4f, 2f, 1.4f), ClinicSpotKind.Scale);
            Spot(spots, "Spot_EyeTest", P(6.8f, 1f, 2.5f), new Vector3(1.4f, 2f, 1.2f), ClinicSpotKind.EyeChart);

            // The training room corner (研修室) for the medical career: a study desk with books and a model, a
            // whiteboard with today's case, an anatomy chart. The cases: Resources/Clinic/medical_training.json.
            var training = Group(root.transform, "TrainingCorner");
            Block(training, "StudyDesk", P(0.2f, 0.38f, 6.9f), new Vector3(2.2f, 0.76f, 0.8f), _wood, true);
            Block(training, "StudyChair_A", P(-0.4f, 0.24f, 6.05f), new Vector3(0.5f, 0.48f, 0.5f), _rubber, false);
            Block(training, "StudyChair_B", P(0.8f, 0.24f, 6.05f), new Vector3(0.5f, 0.48f, 0.5f), _rubber, false);
            Block(training, "Textbooks", P(-0.4f, 0.86f, 7.0f), new Vector3(0.5f, 0.2f, 0.34f), _sign, false);
            Block(training, "Notebook", P(0.6f, 0.78f, 6.85f), new Vector3(0.36f, 0.03f, 0.26f), _white, false);
            Block(training, "Stethoscope", P(1.0f, 0.79f, 7.0f), new Vector3(0.22f, 0.04f, 0.22f), _dark, false);
            Block(training, "Whiteboard", P(0.2f, 1.75f, 7.86f), new Vector3(2.6f, 1.3f, 0.06f), _chart, false);
            // North wall: the text faces the room (yaw 0, read looking north).
            Text(training, "WhiteboardText", "研修室  <size=70%>Đào tạo y khoa</size>\n<size=60%>きょうの しょうれい · Ca lâm sàng hôm nay\nもんしん → しんだん → ちりょう</size>", P(0.2f, 1.85f, 7.82f), 0f, 0.16f, new Color(0.12f, 0.2f, 0.3f), 2.4f);
            Block(training, "AnatomyChart", P(-1.9f, 1.7f, 7.86f), new Vector3(0.9f, 1.3f, 0.04f), _skyArt, false);
            Text(training, "AnatomyLabel", "からだの しくみ", P(-1.9f, 2.45f, 7.82f), 0f, 0.09f, new Color(0.12f, 0.2f, 0.3f), 1f);
            Spot(spots, "Spot_Training", P(0.2f, 1f, 5.9f), new Vector3(2.0f, 2f, 1.2f), ClinicSpotKind.Training);

            // Townspeople who come for a check-up on their own (ClinicPatientFlow walks them along these points).
            var route = Group(root.transform, "PatientRoute");
            foreach (var (name, x, z, yaw) in new[]
            {
                ("Door", 0f, -7.2f, 0f), ("ReceptionWait", -1.2f, -5.7f, 0f), ("Reception", -3f, -4.15f, 0f), ("Aisle", 3.2f, -4.3f, 0f),
                ("Hall_E", 3.4f, -1.6f, 0f), ("Hall_N", 0.6f, 0.2f, -90f), ("Corridor", -3.8f, 0.2f, 0f), ("ConsultIn", -3.8f, 2.3f, -60f),
                ("ConsultApproach", -5.6f, 4.5f, -90f), ("Stool", -6.4f, 4.5f, -90f), ("MedicineApproach", 3.2f, -2.6f, 180f),
                ("MedicineSide", 3.1f, -4.15f, -90f), ("Medicine", 1.8f, -4.15f, 0f), ("Exit", 0f, -7.6f, 180f),
            })
                Point(route, name, P(x, 0f, z), yaw);
            int seat = 0;
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 2; i++)
                    foreach (float dx in new[] { 0f }) // each "bench" model is one chair
                        Point(route, "Seat_" + seat++, P(4.8f + i * 2.4f + dx + 0.12f, 0f, -5.4f + row * 2.2f), 90f); // the chairs face the TV / call board (east)
            root.AddComponent<ClinicPatientFlow>();

            // The clinic shift: patients who appear only during a shift, and the job board by the door.
            JobSiteBuilder.Materials();
            JobPatient(job, "Patient_CheckIn_A", P(-4.4f, 0f, -4.9f), 0f, JobStationKind.ClinicCheckIn, "checkin_a");
            JobPatient(job, "Patient_CheckIn_B", P(-1.6f, 0f, -4.9f), 0f, JobStationKind.ClinicCheckIn, "checkin_b");
            JobPatient(job, "Patient_Triage_A", P(4.8f, 0f, -5.0f), 180f, JobStationKind.ClinicTriage, "triage_a");
            JobPatient(job, "Patient_Triage_B", P(7.2f, 0f, -2.8f), 180f, JobStationKind.ClinicTriage, "triage_b");
            JobPatient(job, "Patient_Medicine", P(3.1f, 0f, -4.9f), 0f, JobStationKind.ClinicMedicine, "medicine");
            JobSiteBuilder.Board(job, P(7.4f, 0f, -7.75f), 180f, Quest, "クリニック スタッフ ぼしゅう", "Làm ở phòng khám · ¥750/ca");

            // Spawns and the exit.
            Spawn(root.transform, WorldLocationCatalog.ClinicEntrance, P(0f, 0.05f, -6.6f), 25f); // looking across reception into the waiting room
            Spawn(root.transform, WorldLocationCatalog.ClinicBed, P(8.1f, 0.05f, 3.0f), 270f);
            var exit = Trigger(root.transform, "ExitToCity", P(0f, 1.1f, -HalfZ + 0.55f), new Vector3(2.8f, 2.2f, 1f));
            var portal = exit.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.CityScene, WorldLocationCatalog.CityClinicReturn, "街へ戻る / Trở lại thành phố", true);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "そとに でる";
            so.FindProperty("promptEn").stringValue = "Ra phố";
            so.ApplyModifiedPropertiesWithoutUndo();

            // A clinic is bright and white: one light under every ceiling panel, plus the zone's own ambient.
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = -1; iz <= 1; iz++)
                    PointLight(root.transform, $"PanelLight_{ix}_{iz}", P(ix * 6f, Height - 0.35f, iz * 5f), 9f, 2.6f, new Color(1f, 0.99f, 0.96f));
            PointLight(root.transform, "Light_Consult", P(-5.5f, 2.8f, 4.5f), 8f, 1.8f, new Color(1f, 1f, 0.97f));
            root.AddComponent<IndoorAmbience>().Configure(new Color(0.74f, 0.76f, 0.78f));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.66f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            var preview = new GameObject("PreviewCamera") { tag = "MainCamera" };
            preview.transform.SetParent(root.transform);
            preview.transform.position = P(0f, 2.8f, -7.4f);
            preview.transform.rotation = Quaternion.LookRotation(P(0f, 1f, 1f) - preview.transform.position);
            preview.AddComponent<Camera>().fieldOfView = 64f;
            preview.AddComponent<AudioListener>();
            preview.AddComponent<NihongoLife.Cameras.ZonePreviewCamera>();

            foreach (var r in shell.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
            foreach (var r in signs.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
        }

        private static void Point(Transform parent, string name, Vector3 position, float yaw)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>What makes it feel like a real, cared-for clinic: a mint walkway from the door to reception, a warm rug
        /// under the benches, a wooden handrail round the walls, a kids' corner (小児科) with a play mat and blocks, an
        /// aquarium, pictures, flowers on the counter and a warm light over the waiting room.</summary>
        private static void Decor(Transform shell, Transform furniture, Transform signs)
        {
            Block(shell, "Walkway", P(0f, 0.006f, -5.6f), new Vector3(1.6f, 0.012f, 4.2f), _walkway, false);
            Block(shell, "Walkway_Reception", P(-1.5f, 0.006f, -4.2f), new Vector3(6.8f, 0.012f, 1.2f), _walkway, false);
            Block(shell, "WaitingRug", P(6.5f, 0.006f, -4.3f), new Vector3(6.4f, 0.012f, 4.6f), _rug, false);
            foreach (var (name, x, z, w, d) in new[] { ("Rail_N", 0f, HalfZ - 0.17f, HalfX * 2f, 0.06f), ("Rail_W", -HalfX + 0.17f, 0f, 0.06f, HalfZ * 2f), ("Rail_E", HalfX - 0.17f, 0f, 0.06f, HalfZ * 2f) })
                Block(shell, name, P(x, 1.0f, z), new Vector3(w, 0.06f, d), _wood, false);

            // Kids' corner by the entrance (x -9..-5, z -7.6..-4.4).
            Block(furniture, "KidsMat", P(-7.1f, 0.012f, -5.9f), new Vector3(3.4f, 0.024f, 2.8f), _yellow, false);
            var toys = new[] { _red, _blue, _green, _yellow, _blue, _red };
            for (int i = 0; i < toys.Length; i++)
                Block(furniture, "ToyBlock_" + i, P(-8.2f + (i % 3) * 0.35f, 0.1f + (i / 3) * 0.16f, -5.2f + (i % 2) * 0.1f), Vector3.one * 0.16f, toys[i], false, i * 17f);
            Block(furniture, "KidsTable", P(-6.6f, 0.25f, -6.3f), new Vector3(0.9f, 0.05f, 0.6f), _white, true);
            foreach (var (lx, lz) in new[] { (-0.4f, -0.25f), (0.4f, -0.25f), (-0.4f, 0.25f), (0.4f, 0.25f) })
                Block(furniture, "KidsTableLeg", P(-6.6f + lx, 0.12f, -6.3f + lz), new Vector3(0.05f, 0.24f, 0.05f), _blue, false);
            Block(furniture, "PictureBook_A", P(-6.75f, 0.29f, -6.3f), new Vector3(0.24f, 0.03f, 0.18f), _red, false, 12f);
            Block(furniture, "PictureBook_B", P(-6.4f, 0.29f, -6.25f), new Vector3(0.24f, 0.03f, 0.18f), _green, false, -20f);
            Block(signs, "Sign_Kids_Back", P(-7.1f, 1.9f, -7.86f), new Vector3(2.0f, 0.5f, 0.02f), _yellow, false);
            Text(signs, "Sign_Kids", "キッズコーナー\n<size=60%>Góc trẻ em</size>", P(-7.1f, 1.9f, -7.83f), 180f, 0.16f, _sign.color, 2f);

            // Aquarium on a low cabinet against the west wall.
            Block(furniture, "AquariumBase", P(-9.55f, 0.4f, -2.6f), new Vector3(0.6f, 0.8f, 1.8f), _wood, true);
            Block(furniture, "AquariumWater", P(-9.55f, 1.12f, -2.6f), new Vector3(0.5f, 0.62f, 1.7f), _water, false);
            Block(furniture, "AquariumSand", P(-9.55f, 0.83f, -2.6f), new Vector3(0.5f, 0.04f, 1.7f), _rug, false);
            Block(furniture, "AquariumLid", P(-9.55f, 1.45f, -2.6f), new Vector3(0.54f, 0.04f, 1.74f), _dark, false);
            for (int i = 0; i < 4; i++)
                Block(furniture, "Fish_" + i, P(-9.42f, 0.98f + (i % 2) * 0.22f, -3.2f + i * 0.4f), new Vector3(0.04f, 0.06f, 0.13f), _orange, false);
            Block(furniture, "AquariumWeed", P(-9.6f, 1.0f, -1.95f), new Vector3(0.05f, 0.3f, 0.05f), _green, false);
            PointLight(furniture, "AquariumGlow", P(-9.2f, 1.2f, -2.6f), 1.6f, 0.5f, new Color(0.5f, 0.8f, 1f));

            // Pictures on the waiting room's south wall and the kids' corner wall.
            foreach (var (name, x, z, side) in new[] { ("Picture_S1", 5.6f, -HalfZ + 0.16f, false), ("Picture_S2", 7.8f, -HalfZ + 0.16f, false), ("Picture_W", -HalfX + 0.16f, -5.4f, true) })
            {
                Vector3 frame = side ? new Vector3(0.04f, 0.7f, 0.95f) : new Vector3(0.95f, 0.7f, 0.04f);
                Vector3 inward = side ? new Vector3(0.025f, 0f, 0f) : new Vector3(0f, 0f, 0.025f);
                Block(furniture, name + "_Frame", P(x, 1.75f, z), frame, _wood, false);
                Block(furniture, name + "_Sky", P(x, 1.82f, z) + inward, side ? new Vector3(0.02f, 0.42f, 0.8f) : new Vector3(0.8f, 0.42f, 0.02f), _skyArt, false);
                Block(furniture, name + "_Hill", P(x, 1.55f, z) + inward * 1.2f, side ? new Vector3(0.02f, 0.16f, 0.8f) : new Vector3(0.8f, 0.16f, 0.02f), _green, false);
            }

            // Flowers at reception, a warm light over the benches.
            Cylinder(furniture, "Vase", P(-4.6f, 1.25f, -3.35f), new Vector3(0.14f, 0.24f, 0.14f), _mint, false);
            var petals = new[] { (-0.04f, 0f, _red), (0.05f, 0.03f, _yellow), (0f, -0.05f, _red) };
            foreach (var (fx, fz, m) in petals)
                Block(furniture, "Flower", P(-4.6f + fx, 1.45f, -3.35f + fz), Vector3.one * 0.08f, m, false);
            PointLight(furniture, "WaitingWarmLight", P(6.5f, 2.7f, -4.3f), 7f, 1.2f, new Color(1f, 0.9f, 0.76f));
        }

        private static void Spot(Transform parent, string name, Vector3 position, Vector3 size, ClinicSpotKind kind) =>
            Trigger(parent, name, position, size).AddComponent<ClinicSpot>().Configure(kind);

        private static void JobPatient(Transform parent, string name, Vector3 position, float yaw, JobStationKind kind, string id)
        {
            var visual = JobSiteBuilder.Person(parent, name + "_Visual", position, yaw);
            var trigger = Trigger(parent, name, position + Vector3.up, new Vector3(1.3f, 2f, 1.3f));
            trigger.AddComponent<JobStation>().Configure(Quest, kind, id, "かんじゃさん", "Bệnh nhân", visual);
        }

        private static void Person(Transform parent, string name, Vector3 position, float yaw, string nameplate, string role)
        {
            var person = JobSiteBuilder.Person(parent, name, position, yaw);
            person.SetActive(true);
            person.transform.position = position;
            var plate = Text(person.transform, "Nameplate", $"{nameplate}\n<size=70%>{role}</size>", new Vector3(0f, 2.15f, 0f), 0f, 0.11f, new Color(0.95f, 0.95f, 0.9f), 2f);
            plate.gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
        }

        private static void Spawn(Transform parent, string id, Vector3 position, float yaw)
        {
            var spawn = new GameObject("Spawn_" + id);
            spawn.transform.SetParent(parent);
            spawn.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            spawn.AddComponent<SceneSpawnPoint>().Configure(id);
        }

        private static GameObject Furn(Transform parent, string model, string name, Vector3 position, float yaw, Vector3 targetSize, bool collider)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing furniture model " + model);
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            Bounds b = BoundsOf(instance);
            float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, b.size.x), targetSize.y / Mathf.Max(0.01f, b.size.y), targetSize.z / Mathf.Max(0.01f, b.size.z));
            instance.transform.localScale *= scale;
            b = BoundsOf(instance);
            instance.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            holder.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            foreach (var r in holder.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
            if (collider)
            {
                Bounds world = BoundsOf(holder.gameObject);
                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = holder.InverseTransformPoint(world.center);
                Vector3 size = holder.InverseTransformVector(world.size);
                box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
            return holder.gameObject;
        }

        // ─────────── The entrance in the town ───────────

        private static void BuildCityEntrance()
        {
            var scene = EditorSceneManager.OpenScene(CityPath, OpenSceneMode.Single);
            var building = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == CityBuilding && t.parent != null && t.parent.name == "CityTile_0");
            if (building == null) throw new InvalidOperationException(CityBuilding + " not found in CityTile_0");
            var destinations = scene.GetRootGameObjects().First(r => r.name == "Town_Destinations").transform;
            var portals = scene.GetRootGameObjects().First(r => r.name == "AdditiveZonePortals").transform;
            foreach (var stale in new[] { destinations.Find("Entrance_Clinic"), portals.Find("ClinicPortal"), portals.Find("Spawn_" + WorldLocationCatalog.CityClinicReturn) })
                if (stale != null) Object.DestroyImmediate(stale.gameObject);

            // The south bus stop (bench, bin) and its tree stood right in front of the clinic door: move them along the
            // pavement, once (only while they are still in front of the building).
            foreach (var t in building.parent.Cast<Transform>().Where(t => t.name.Contains("Stop_South") || t.name == "Tree_S_-2").ToList())
                if (Mathf.Abs(t.position.x - building.position.x) < 3.5f) t.position += new Vector3(5f, 0f, 0f);

            Bounds b = BoundsOf(building.gameObject);
            float frontZ = b.max.z; // the south row faces north, toward Sakura-dōri
            var group = Group(destinations, "Entrance_Clinic");
            group.SetPositionAndRotation(new Vector3(building.position.x, 0f, frontZ), Quaternion.Euler(0f, 180f, 0f));
            // Local: +X along the facade, −Z toward the street.
            Block(group, "Canopy", new Vector3(0f, 2.9f, -1.0f), new Vector3(5.6f, 0.16f, 2.2f), _white, true);
            Block(group, "CanopyStripe", new Vector3(0f, 2.8f, -2.08f), new Vector3(5.6f, 0.12f, 0.05f), _mint, false);
            foreach (float x in new[] { -2.6f, 2.6f })
                Block(group, "Post_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.4f, -1.95f), new Vector3(0.14f, 2.8f, 0.14f), _white, true);
            Block(group, "SignBoard", new Vector3(0f, 3.6f, -2.1f), new Vector3(5f, 0.9f, 0.14f), _white, true);
            Block(group, "SignStripe", new Vector3(0f, 3.17f, -2.18f), new Vector3(5f, 0.08f, 0.02f), _mint, false);
            Block(group, "Plus_H", new Vector3(-1.95f, 3.6f, -2.18f), new Vector3(0.5f, 0.16f, 0.02f), _mint, false);
            Block(group, "Plus_V", new Vector3(-1.95f, 3.6f, -2.18f), new Vector3(0.16f, 0.5f, 0.02f), _mint, false);
            Text(group, "Sign_JA", "ひばりクリニック", new Vector3(0.35f, 3.72f, -2.2f), 0f, 0.38f, _sign.color, 4f);
            Text(group, "Sign_VI", "内科・小児科 · Phòng khám Hibari", new Vector3(0.35f, 3.38f, -2.2f), 0f, 0.14f, _sign.color, 4f);
            Block(group, "Door", new Vector3(0f, 1.15f, -0.05f), new Vector3(2.4f, 2.3f, 0.06f), _glass, false);
            Block(group, "DoorFrame", new Vector3(0f, 2.35f, -0.08f), new Vector3(2.6f, 0.1f, 0.08f), _mint, false);
            Text(group, "Door_Hours", "9:00〜18:00 · 日祝休診", new Vector3(0f, 1.9f, -0.1f), 0f, 0.09f, _sign.color, 2f);
            PointLight(group, "CanopyLight", new Vector3(0f, 2.6f, -1.2f), 5f, 0.6f, new Color(1f, 0.98f, 0.92f));

            var portalObject = Trigger(portals, "ClinicPortal", Vector3.zero, new Vector3(2.4f, 2.2f, 1.2f));
            portalObject.transform.SetPositionAndRotation(new Vector3(group.position.x, 1.1f, frontZ + 0.6f), Quaternion.Euler(0f, 180f, 0f));
            var portal = portalObject.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.ClinicScene, WorldLocationCatalog.ClinicEntrance, "ひばりクリニック / Phòng khám Hibari", false);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "クリニックに はいる";
            so.FindProperty("promptEn").stringValue = "Vào phòng khám";
            so.ApplyModifiedPropertiesWithoutUndo();
            var spawn = new GameObject("Spawn_" + WorldLocationCatalog.CityClinicReturn);
            spawn.transform.SetParent(portals);
            spawn.transform.SetPositionAndRotation(new Vector3(group.position.x, 0.08f, frontZ + 2.4f), Quaternion.identity);
            spawn.AddComponent<SceneSpawnPoint>().Configure(WorldLocationCatalog.CityClinicReturn);

            foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + CityPath);
        }
    }
}
#endif
