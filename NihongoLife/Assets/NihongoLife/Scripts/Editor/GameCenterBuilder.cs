#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.MiniGames;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Game Center vertical slice (approved scene 50_GameCenter):
    ///  • mini-game content assets (Kana Match pair sets + three mini-game definitions),
    ///  • the 50_GameCenter zone from Kenney Mini Arcade (one shared colormap material, static, no shadows),
    ///  • the neon entrance + portal + return spawn on StreetBuilding_S_4 in 90_TestSandbox (only the
    ///    Game Center pieces are added/replaced; nothing else in the city is touched),
    ///  • build settings.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.GameCenterBuilder.Build
    /// </summary>
    public static class GameCenterBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/50_GameCenter.unity";
        private const string CityPath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string DataDir = "Assets/NihongoLife/MiniGames/Data";
        private const string MatDir = "Assets/NihongoLife/Materials/GameCenter";
        private const string Arcade = "Assets/ThirdParty/Minigame/kenney_mini-arcade/Models/FBX format/";
        private static readonly Vector3 O = new Vector3(300f, 0f, 0f);
        private const float HalfX = 11f, HalfZ = 9f, Height = 4.4f;

        private static Material _kenney, _floor, _floorLine, _wall, _ceiling, _neonPink, _neonCyan, _neonGold, _dark, _glass, _signBack;

        public static void Build()
        {
            try
            {
                CreateMaterials();
                var definitions = BuildContent();
                BuildZone(definitions);
                BuildCityEntrance();
                UpdateBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("[GameCenterBuilder] Game Center built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void CreateMaterials()
        {
            var colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(Arcade + "Textures/colormap.png");
            _kenney = Lit(MatDir, "GC_KenneyArcade", Color.white, 0.25f);
            if (colormap != null) { _kenney.SetTexture("_BaseMap", colormap); _kenney.mainTexture = colormap; }
            _kenney.enableInstancing = true;
            _floor = Lit(MatDir, "GC_Floor", new Color(0.12f, 0.1f, 0.2f), 0.35f);
            _floorLine = Lit(MatDir, "GC_FloorLine", new Color(0.25f, 0.85f, 0.95f), 0.4f, new Color(0.1f, 0.6f, 0.75f));
            _wall = Lit(MatDir, "GC_Wall", new Color(0.2f, 0.17f, 0.32f), 0.3f);
            _ceiling = Lit(MatDir, "GC_Ceiling", new Color(0.06f, 0.06f, 0.1f), 0.2f);
            _neonPink = Lit(MatDir, "GC_NeonPink", new Color(1f, 0.35f, 0.75f), 0.5f, new Color(1f, 0.25f, 0.7f) * 2.2f);
            _neonCyan = Lit(MatDir, "GC_NeonCyan", new Color(0.3f, 0.95f, 1f), 0.5f, new Color(0.2f, 0.85f, 1f) * 2.2f);
            _neonGold = Lit(MatDir, "GC_NeonGold", new Color(1f, 0.8f, 0.3f), 0.5f, new Color(1f, 0.7f, 0.2f) * 2f);
            _dark = Lit(MatDir, "GC_Dark", new Color(0.04f, 0.04f, 0.07f), 0.6f);
            _glass = Lit(MatDir, "GC_Glass", new Color(0.55f, 0.75f, 1f, 0.25f), 0.95f, transparent: true);
            _signBack = Lit(MatDir, "GC_SignBack", new Color(0.08f, 0.07f, 0.16f), 0.4f);
        }

        // ─────────── Content ───────────

        private static Dictionary<string, MiniGameDefinition> BuildContent()
        {
            EnsureFolder(DataDir);
            var hiragana = PairSet("pairs_hiragana", "hiragana_basic", KanaPairType.HiraganaRomaji, "ひらがな", "Hiragana ↔ Romaji", "kana.hiragana.",
                ("あ", "a"), ("い", "i"), ("う", "u"), ("え", "e"), ("お", "o"), ("か", "ka"), ("き", "ki"), ("く", "ku"),
                ("け", "ke"), ("こ", "ko"), ("さ", "sa"), ("し", "shi"), ("す", "su"), ("せ", "se"), ("そ", "so"), ("た", "ta"));
            var katakana = PairSet("pairs_katakana", "katakana_basic", KanaPairType.KatakanaRomaji, "カタカナ", "Katakana ↔ Romaji", "kana.katakana.",
                ("ア", "a"), ("イ", "i"), ("ウ", "u"), ("エ", "e"), ("オ", "o"), ("カ", "ka"), ("キ", "ki"), ("ク", "ku"),
                ("ケ", "ke"), ("コ", "ko"), ("サ", "sa"), ("シ", "shi"), ("ス", "su"), ("セ", "se"), ("ソ", "so"), ("タ", "ta"));
            var kanji = PairSet("pairs_kanji_n5", "kanji_n5", KanaPairType.KanjiReading, "かんじ N5", "Kanji ↔ cách đọc", "kanji.",
                ("山", "やま"), ("川", "かわ"), ("木", "き"), ("水", "みず"), ("火", "ひ"), ("月", "つき"), ("人", "ひと"), ("口", "くち"),
                ("目", "め"), ("耳", "みみ"), ("手", "て"), ("足", "あし"), ("花", "はな"), ("雨", "あめ"), ("空", "そら"), ("犬", "いぬ"));
            var words = PairSet("pairs_word_image", "word_image", KanaPairType.WordImage, "たべもの", "Từ ↔ Hình (đồ ăn)", "vocab.food.");
            words.pairs = new List<KanaPair>
            {
                Word("りんご", "ringo", "táo"), Word("バナナ", "banana", "chuối"), Word("パン", "pan", "bánh mì"), Word("ケーキ", "keeki", "bánh kem"),
                Word("にんじん", "ninjin", "cà rốt"), Word("さかな", "sakana", "cá"), Word("たまご", "tamago", "trứng"), Word("ぶどう", "budou", "nho"),
                Word("おちゃ", "ocha", "trà"), Word("コーヒー", "koohii", "cà phê"), Word("おにぎり", "onigiri", "cơm nắm"), Word("すし", "sushi", "sushi"),
                Word("すいか", "suika", "dưa hấu"), Word("いちご", "ichigo", "dâu tây"), Word("アイス", "aisu", "kem"), Word("トマト", "tomato", "cà chua"),
            };
            EditorUtility.SetDirty(words);
            var meaning = PairSet("pairs_ja_vi", "ja_vi_daily", KanaPairType.JapaneseVietnamese, "ことば", "Nhật ↔ Việt (giao tiếp)", "vocab.daily.",
                ("こんにちは", "Xin chào"), ("ありがとう", "Cảm ơn"), ("すみません", "Xin lỗi"), ("おはよう", "Chào buổi sáng"),
                ("おやすみ", "Chúc ngủ ngon"), ("いただきます", "Mời ăn cơm"), ("がっこう", "Trường học"), ("えき", "Nhà ga"),
                ("でんしゃ", "Tàu điện"), ("ともだち", "Bạn bè"), ("せんせい", "Giáo viên"), ("みず", "Nước"),
                ("おかね", "Tiền"), ("いえ", "Nhà"), ("ねこ", "Con mèo"), ("いぬ", "Con chó"));

            var result = new Dictionary<string, MiniGameDefinition>
            {
                ["kana_match"] = Definition("minigame_kana_match", "kana_match", MiniGameKind.KanaMatch, "かなマッチ", "Kana Match",
                    "Lật thẻ và ghép cặp: chữ ↔ cách đọc, từ ↔ hình, tiếng Nhật ↔ tiếng Việt.", true, 8, 150f,
                    hiragana, katakana, kanji, words, meaning),
                ["word_shooter"] = Definition("minigame_word_shooter", "word_shooter", MiniGameKind.WordShooter, "ワードシューター", "Word Shooter",
                    "Nghe hoặc đọc một từ rồi bắn đúng mục tiêu (sắp ra mắt).", false, 10, 90f, words, kanji, meaning),
                ["order_rush"] = Definition("minigame_order_rush", "order_rush", MiniGameKind.OrderRush, "オーダーラッシュ", "Order Rush",
                    "Khách gọi món bằng tiếng Nhật — chọn đúng món trước khi hết giờ (sắp ra mắt).", false, 6, 120f, words),
            };
            return result;
        }

        private static KanaPair Word(string ja, string image, string vi) =>
            new KanaPair { targetId = "vocab.food." + image, a = ja, b = vi, imageB = "MiniGames/Words/" + image, hintVi = vi };

        private static KanaPairSet PairSet(string file, string id, KanaPairType type, string titleJa, string titleVi, string prefix, params (string a, string b)[] pairs)
        {
            string path = $"{DataDir}/{file}.asset";
            var set = AssetDatabase.LoadAssetAtPath<KanaPairSet>(path);
            if (set == null) { set = ScriptableObject.CreateInstance<KanaPairSet>(); AssetDatabase.CreateAsset(set, path); }
            set.id = id; set.type = type; set.titleJa = titleJa; set.titleVi = titleVi;
            set.pairs = pairs.Select(p => new KanaPair { targetId = prefix + (type == KanaPairType.KanjiReading || type == KanaPairType.JapaneseVietnamese ? p.a : p.b), a = p.a, b = p.b, hintVi = p.b }).ToList();
            EditorUtility.SetDirty(set);
            return set;
        }

        private static MiniGameDefinition Definition(string file, string id, MiniGameKind kind, string ja, string vi, string description, bool playable, int pairs, float time, params KanaPairSet[] sets)
        {
            string path = $"{DataDir}/{file}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<MiniGameDefinition>(path);
            if (definition == null) { definition = ScriptableObject.CreateInstance<MiniGameDefinition>(); AssetDatabase.CreateAsset(definition, path); }
            definition.id = id; definition.kind = kind; definition.titleJa = ja; definition.titleVi = vi; definition.descriptionVi = description;
            definition.playable = playable; definition.pairsPerRound = pairs; definition.timeLimitSeconds = time;
            definition.contentSets = sets.ToList();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        // ─────────── Zone ───────────

        private static void BuildZone(Dictionary<string, MiniGameDefinition> definitions)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("GameCenter_Zone");
            root.AddComponent<SceneZoneVisibility>();
            var shell = Group(root.transform, "Shell");
            var machines = Group(root.transform, "Machines");
            var decor = Group(root.transform, "Decor");

            // Shell: floor with neon lane lines, walls with an entrance gap, ceiling with light strips.
            Block(shell, "Floor", O + new Vector3(0f, -0.1f, 0f), new Vector3(HalfX * 2f, 0.2f, HalfZ * 2f), _floor, true);
            for (int i = -4; i <= 4; i++)
                Block(shell, "FloorLine_" + i, O + new Vector3(i * 2.4f, 0.005f, 0f), new Vector3(0.05f, 0.01f, HalfZ * 2f - 1f), _floorLine, false);
            Block(shell, "Wall_N", O + new Vector3(0f, Height / 2f, HalfZ), new Vector3(HalfX * 2f, Height, 0.3f), _wall, true);
            Block(shell, "Wall_W", O + new Vector3(-HalfX, Height / 2f, 0f), new Vector3(0.3f, Height, HalfZ * 2f), _wall, true);
            Block(shell, "Wall_E", O + new Vector3(HalfX, Height / 2f, 0f), new Vector3(0.3f, Height, HalfZ * 2f), _wall, true);
            Block(shell, "Wall_S_L", O + new Vector3(-6.25f, Height / 2f, -HalfZ), new Vector3(9.5f, Height, 0.3f), _wall, true);
            Block(shell, "Wall_S_R", O + new Vector3(6.25f, Height / 2f, -HalfZ), new Vector3(9.5f, Height, 0.3f), _wall, true);
            Block(shell, "Wall_S_Header", O + new Vector3(0f, 3.5f, -HalfZ), new Vector3(3f, 1.8f, 0.3f), _wall, true);
            Block(shell, "EntranceGlass", O + new Vector3(0f, 1.3f, -HalfZ - 0.05f), new Vector3(3f, 2.6f, 0.05f), _glass, true);
            Block(shell, "Ceiling", O + new Vector3(0f, Height, 0f), new Vector3(HalfX * 2f, 0.2f, HalfZ * 2f), _ceiling, false);
            foreach (float z in new[] { -5f, 0f, 5f })
                Block(shell, "CeilingStrip_" + z, O + new Vector3(0f, Height - 0.12f, z), new Vector3(HalfX * 2f - 1.5f, 0.05f, 0.18f), z == 0f ? _neonPink : _neonCyan, false);
            Block(shell, "NeonTrim_N", O + new Vector3(0f, Height - 0.4f, HalfZ - 0.17f), new Vector3(HalfX * 2f - 0.6f, 0.06f, 0.04f), _neonPink, false);
            Block(shell, "NeonTrim_W", O + new Vector3(-HalfX + 0.17f, Height - 0.4f, 0f), new Vector3(0.04f, 0.06f, HalfZ * 2f - 0.6f), _neonCyan, false);
            Block(shell, "NeonTrim_E", O + new Vector3(HalfX - 0.17f, Height - 0.4f, 0f), new Vector3(0.04f, 0.06f, HalfZ * 2f - 0.6f), _neonCyan, false);

            // Big sign on the north wall.
            Block(decor, "MainSign_Back", O + new Vector3(0f, 3.3f, HalfZ - 0.2f), new Vector3(8.4f, 1.3f, 0.08f), _signBack, false);
            Block(decor, "MainSign_Frame", O + new Vector3(0f, 3.3f, HalfZ - 0.24f), new Vector3(8.6f, 1.45f, 0.02f), _neonPink, false);
            Text(decor, "MainSign_JA", "ゲームセンター ひばり", O + new Vector3(0f, 3.45f, HalfZ - 0.27f), 0f, 0.5f, new Color(1f, 0.55f, 0.85f), 8.2f);
            Text(decor, "MainSign_VI", "GAME CENTER HIBARI · Trung tâm trò chơi học tiếng Nhật", O + new Vector3(0f, 2.95f, HalfZ - 0.27f), 0f, 0.16f, new Color(0.6f, 0.95f, 1f), 8.2f);

            // Featured mini-game row (centre): Kana Match is playable, the other two are announced.
            Featured(machines, decor, definitions["kana_match"], "arcade-machine", O + new Vector3(-3.6f, 0f, 1.6f), _neonPink, "NEW!");
            Featured(machines, decor, definitions["word_shooter"], "arcade-machine", O + new Vector3(0f, 0f, 1.6f), _neonCyan, "じゅんびちゅう");
            Featured(machines, decor, definitions["order_rush"], "cash-register", O + new Vector3(3.6f, 0f, 1.6f), _neonGold, "じゅんびちゅう");

            // Decorative machines around the room (all share the Kenney colormap material).
            for (int i = 0; i < 5; i++) Machine(machines, "arcade-machine", $"Cabinet_W{i}", O + new Vector3(-HalfX + 0.9f, 0f, -5.4f + i * 2.4f), 90f, 1.95f);
            Machine(machines, "claw-machine", "Claw_1", O + new Vector3(HalfX - 1.1f, 0f, -4.6f), -90f, 2.15f);
            Machine(machines, "claw-machine", "Claw_2", O + new Vector3(HalfX - 1.1f, 0f, -2.2f), -90f, 2.15f);
            Machine(machines, "claw-machine", "Claw_3", O + new Vector3(HalfX - 1.1f, 0f, 0.2f), -90f, 2.15f);
            Machine(machines, "prize-wheel", "PrizeWheel", O + new Vector3(HalfX - 1.0f, 0f, 3.2f), -90f, 2.4f);
            Machine(machines, "dance-machine", "DanceMachine", O + new Vector3(-7f, 0f, HalfZ - 1.4f), 180f, 2.3f);
            Machine(machines, "air-hockey", "AirHockey", O + new Vector3(-2.2f, 0f, 6.2f), 90f, 1.0f);
            Machine(machines, "basketball-game", "Basketball_1", O + new Vector3(2.6f, 0f, HalfZ - 1.5f), 180f, 2.4f);
            Machine(machines, "basketball-game", "Basketball_2", O + new Vector3(4.9f, 0f, HalfZ - 1.5f), 180f, 2.4f);
            Machine(machines, "pinball", "Pinball_1", O + new Vector3(HalfX - 1.2f, 0f, HalfZ - 1.6f), -90f, 1.5f);
            Machine(machines, "gambling-machine", "MedalGame", O + new Vector3(7.6f, 0f, 5.2f), 180f, 1.9f);
            // Prize counter (south-east) and vending corner (south-west).
            Machine(machines, "cash-register", "PrizeCounter", O + new Vector3(7.6f, 0f, -6.6f), 0f, 1.15f);
            Machine(machines, "prizes", "PrizeShelf", O + new Vector3(8.6f, 0f, -HalfZ + 0.7f), 0f, 2.1f);
            Machine(machines, "character-employee", "Staff_Aoi", O + new Vector3(8.4f, 0f, -7.6f), 0f, 1.7f, collider: false);
            Machine(machines, "vending-machine", "Vending_1", O + new Vector3(-HalfX + 0.9f, 0f, -HalfZ + 1.0f), 0f, 2.0f);
            Machine(machines, "vending-machine", "Vending_2", O + new Vector3(-HalfX + 2.3f, 0f, -HalfZ + 1.0f), 0f, 2.0f);
            Machine(machines, "ticket-machine", "TicketMachine", O + new Vector3(-5.4f, 0f, -HalfZ + 0.8f), 0f, 1.7f);
            Machine(machines, "character-gamer", "Gamer_1", O + new Vector3(-HalfX + 2.0f, 0f, -0.6f), -90f, 1.6f, collider: false);
            Text(decor, "PrizeSign", "けいひん · Quầy đổi quà", O + new Vector3(7.6f, 2.5f, -5.6f), 0f, 0.2f, new Color(1f, 0.85f, 0.4f), 3f);
            var prize = Trigger(machines, "PrizeCounterInteraction", O + new Vector3(7.6f, 1f, -5.6f), new Vector3(2.2f, 2f, 1.4f));
            prize.AddComponent<PrizeCounter>();

            // Zone signs, posters and a welcome board so every corner reads as a Japanese arcade.
            NeonSign(decor, "Sign_UFO", "UFOキャッチャー", "Máy gắp thú", O + new Vector3(HalfX - 0.2f, 3.2f, -2.2f), 90f, _neonPink);
            NeonSign(decor, "Sign_Dance", "ダンス", "Máy nhảy", O + new Vector3(-7f, 3.4f, HalfZ - 0.25f), 0f, _neonCyan);
            NeonSign(decor, "Sign_Retro", "レトロゲーム", "Tủ game cổ điển", O + new Vector3(-HalfX + 0.2f, 3.2f, -0.6f), -90f, _neonGold);
            NeonSign(decor, "Sign_Pinball", "ピンボール", "Pinball", O + new Vector3(HalfX - 0.2f, 3.2f, 5.6f), 90f, _neonCyan);
            NeonSign(decor, "Sign_Welcome", "ようこそ！", "Chào mừng tới Game Center", O + new Vector3(0f, 3.3f, -HalfZ + 0.2f), 180f, _neonPink);
            Poster(decor, "Poster_Kana", "かなマッチ\n<size=60%>NEW! 5 bộ thẻ</size>", O + new Vector3(-3.6f, 1.9f, HalfZ - 0.17f), 0f, new Color(0.95f, 0.35f, 0.65f));
            Poster(decor, "Poster_Tickets", "チケット → けいひん\n<size=60%>Đổi vé lấy quà</size>", O + new Vector3(3.6f, 1.9f, HalfZ - 0.17f), 0f, new Color(0.25f, 0.65f, 0.85f));
            foreach (var (x, color) in new[] { (-3.6f, new Color(1f, 0.45f, 0.8f)), (0f, new Color(0.4f, 0.9f, 1f)), (3.6f, new Color(1f, 0.8f, 0.35f)) })
            {
                var spot = new GameObject("FeaturedSpot_" + x).AddComponent<Light>();
                spot.transform.SetParent(root.transform);
                spot.transform.SetPositionAndRotation(O + new Vector3(x, Height - 0.3f, 0.2f), Quaternion.Euler(70f, 0f, 0f));
                spot.type = LightType.Spot; spot.range = 8f; spot.spotAngle = 48f; spot.intensity = 3.2f; spot.color = color;
                spot.shadows = LightShadows.None;
            }

            // Lights: neon pools, no shadows (WebGL).
            PointLight(root.transform, "Light_Pink", O + new Vector3(-5f, 3.6f, 2f), 11f, 1.6f, new Color(1f, 0.45f, 0.8f));
            PointLight(root.transform, "Light_Cyan", O + new Vector3(5f, 3.6f, 2f), 11f, 1.6f, new Color(0.4f, 0.9f, 1f));
            PointLight(root.transform, "Light_Warm_S", O + new Vector3(0f, 3.6f, -5f), 12f, 1.5f, new Color(1f, 0.85f, 0.65f));
            PointLight(root.transform, "Light_Warm_N", O + new Vector3(0f, 3.6f, 6f), 11f, 1.2f, new Color(1f, 0.8f, 0.6f));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.4f, 0.52f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            // Flow: spawn just inside the door, exit back to the matching city doorway.
            var spawn = new GameObject("Spawn_" + WorldLocationCatalog.GameCenterEntrance);
            spawn.transform.SetParent(root.transform);
            spawn.transform.SetPositionAndRotation(O + new Vector3(0f, 0.05f, -6.9f), Quaternion.identity);
            spawn.AddComponent<SceneSpawnPoint>().Configure(WorldLocationCatalog.GameCenterEntrance);
            var exit = new GameObject("ExitToCity") { layer = InteractableLayer };
            exit.transform.SetParent(root.transform);
            exit.transform.position = O + new Vector3(0f, 1.1f, -HalfZ + 0.6f);
            var box = exit.AddComponent<BoxCollider>();
            box.size = new Vector3(2.8f, 2.2f, 1f);
            box.isTrigger = true;
            var portal = exit.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.CityScene, WorldLocationCatalog.CityGameCenterReturn, "街へ戻る / Trở lại thành phố", true);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "そとに でる";
            so.FindProperty("promptEn").stringValue = "Ra phố";
            so.ApplyModifiedPropertiesWithoutUndo();

            var preview = new GameObject("PreviewCamera") { tag = "MainCamera" };
            preview.transform.SetParent(root.transform);
            preview.transform.position = O + new Vector3(0f, 3.4f, -8.2f);
            preview.transform.rotation = Quaternion.LookRotation(O + new Vector3(0f, 1.2f, 2f) - preview.transform.position);
            preview.AddComponent<Camera>().fieldOfView = 62f;
            preview.AddComponent<AudioListener>();
            preview.AddComponent<NihongoLife.Cameras.ZonePreviewCamera>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            // Static-batch geometry only: TextMeshPro rebuilds its mesh when the dynamic font atlas grows, and a
            // batched copy would keep stale UVs (glyphs vanished from the marquees).
            foreach (var t in shell.GetComponentsInChildren<Transform>(true).Concat(decor.GetComponentsInChildren<Transform>(true)))
            {
                bool text = t.GetComponent<TextMeshPro>() != null;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, text ? 0 : StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
        }

        private static void NeonSign(Transform parent, string name, string ja, string vi, Vector3 position, float yaw, Material neon)
        {
            var group = Group(parent, name);
            group.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Block(group, "Back", Vector3.zero, new Vector3(3.0f, 0.75f, 0.06f), _signBack, false);
            Block(group, "Frame", new Vector3(0f, 0f, 0.04f), new Vector3(3.12f, 0.87f, 0.02f), neon, false);
            Text(group, "JA", ja, new Vector3(0f, 0.09f, -0.08f), 0f, 0.24f, Color.white, 2.9f);
            Text(group, "VI", vi, new Vector3(0f, -0.2f, -0.08f), 0f, 0.09f, new Color(0.8f, 0.86f, 0.95f), 2.9f);
        }

        private static void Poster(Transform parent, string name, string value, Vector3 position, float yaw, Color color)
        {
            var mat = Lit(MatDir, "GC_Poster_" + name, color, 0.3f, color * 0.35f);
            var group = Group(parent, name);
            group.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Block(group, "Paper", Vector3.zero, new Vector3(1.4f, 1.0f, 0.02f), mat, false);
            Text(group, "Text", value, new Vector3(0f, 0f, -0.03f), 0f, 0.16f, Color.white, 1.3f);
        }

        private static void Featured(Transform machines, Transform decor, MiniGameDefinition definition, string model, Vector3 position, Material neon, string badge)
        {
            var cabinet = Machine(machines, model, "Featured_" + definition.id, position, 180f, model == "cash-register" ? 1.2f : 2.0f);
            // Marquee above the machine.
            Block(decor, $"Marquee_{definition.id}", position + new Vector3(0f, 2.75f, 0f), new Vector3(2.2f, 0.7f, 0.04f), _signBack, false);
            Block(decor, $"MarqueeFrame_{definition.id}", position + new Vector3(0f, 2.75f, 0.04f), new Vector3(2.32f, 0.82f, 0.02f), neon, false);
            // Text sits clearly in front of the board so it never z-fights when the camera is close.
            Text(decor, $"MarqueeJA_{definition.id}", definition.titleJa, position + new Vector3(0f, 2.86f, -0.12f), 0f, 0.22f, Color.white, 2.1f);
            Text(decor, $"MarqueeVI_{definition.id}", $"{definition.titleVi} · {badge}", position + new Vector3(0f, 2.58f, -0.12f), 0f, 0.1f, definition.playable ? new Color(1f, 0.85f, 0.4f) : new Color(0.7f, 0.75f, 0.85f), 2.1f);
            // Play mat in front of the machine: dark with a thin neon border.
            Vector3 mat = position + new Vector3(0f, 0.012f, -1.15f);
            Block(decor, $"PlayMat_{definition.id}", mat, new Vector3(1.6f, 0.01f, 1.1f), _signBack, false);
            Block(decor, $"PlayMatEdgeF_{definition.id}", mat + new Vector3(0f, 0.004f, -0.55f), new Vector3(1.6f, 0.012f, 0.04f), neon, false);
            Block(decor, $"PlayMatEdgeL_{definition.id}", mat + new Vector3(-0.8f, 0.004f, 0f), new Vector3(0.04f, 0.012f, 1.1f), neon, false);
            Block(decor, $"PlayMatEdgeR_{definition.id}", mat + new Vector3(0.8f, 0.004f, 0f), new Vector3(0.04f, 0.012f, 1.1f), neon, false);

            var launcher = new GameObject("MiniGameLauncher_" + definition.id) { layer = InteractableLayer };
            launcher.transform.SetParent(cabinet.transform.parent);
            launcher.transform.position = position + new Vector3(0f, 1f, -1.0f);
            var trigger = launcher.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.6f, 2f, 1.4f);
            var view = new GameObject("ViewPoint").transform;
            view.SetParent(launcher.transform, false);
            view.position = position + new Vector3(0f, 1.6f, -1.9f);
            view.rotation = Quaternion.LookRotation(position + new Vector3(0f, 1.35f, 0f) - view.position);
            launcher.AddComponent<MiniGameLauncher>().Configure(definition, view);
        }

        /// <summary>Kenney model scaled uniformly to <paramref name="height"/>, resting on the floor, centred on
        /// <paramref name="position"/>; every renderer uses the shared colormap material.</summary>
        private static GameObject Machine(Transform parent, string model, string name, Vector3 position, float yaw, float height, bool collider = true)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Arcade + model + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing arcade model " + model);
            var holder = Group(parent, name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = Enumerable.Repeat(_kenney, r.sharedMaterials.Length).ToArray();
            Bounds b = BoundsOf(instance);
            instance.transform.localScale *= height / Mathf.Max(0.01f, b.size.y);
            b = BoundsOf(instance);
            Vector3 anchor = holder.position;
            instance.transform.position -= new Vector3(b.center.x - anchor.x, b.min.y - anchor.y, b.center.z - anchor.z);
            holder.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (collider)
            {
                Bounds world = BoundsOf(instance);
                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = holder.InverseTransformPoint(world.center);
                Vector3 size = holder.InverseTransformVector(world.size);
                box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
            GameObjectUtility.SetStaticEditorFlags(holder.gameObject, StaticEditorFlags.BatchingStatic);
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            return holder.gameObject;
        }

        // ─────────── City entrance ───────────

        private static void BuildCityEntrance()
        {
            var scene = EditorSceneManager.OpenScene(CityPath, OpenSceneMode.Single);
            var building = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "StreetBuilding_S_4" && t.parent != null && t.parent.name == "CityTile_0");
            if (building == null) throw new InvalidOperationException("StreetBuilding_S_4 not found in CityTile_0");
            var destinations = scene.GetRootGameObjects().First(r => r.name == "Town_Destinations").transform;
            var portals = scene.GetRootGameObjects().First(r => r.name == "AdditiveZonePortals").transform;
            foreach (var stale in new[] { destinations.Find("Entrance_GameCenter"), portals.Find("GameCenterPortal"), portals.Find("Spawn_" + WorldLocationCatalog.CityGameCenterReturn) })
                if (stale != null) Object.DestroyImmediate(stale.gameObject);

            Bounds b = BoundsOf(building.gameObject);
            float frontZ = b.max.z; // the south row faces north, toward the main street
            var group = Group(destinations, "Entrance_GameCenter");
            group.SetPositionAndRotation(new Vector3(building.position.x, 0f, frontZ), Quaternion.Euler(0f, 180f, 0f));
            // Local space: +X along the facade, −Z toward the street.
            Block(group, "Canopy", new Vector3(0f, 3.1f, -1.2f), new Vector3(7.4f, 0.2f, 2.6f), _signBack, false);
            Block(group, "CanopyNeon", new Vector3(0f, 2.98f, -2.48f), new Vector3(7.4f, 0.08f, 0.06f), _neonPink, false);
            foreach (float x in new[] { -3.5f, 3.5f })
                Block(group, "Pillar_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.5f, -2.3f), new Vector3(0.25f, 3.0f, 0.25f), _neonCyan, true);
            Block(group, "SignBoard", new Vector3(0f, 3.85f, -2.45f), new Vector3(6.2f, 1.1f, 0.15f), _signBack, false);
            Block(group, "SignFrame", new Vector3(0f, 3.85f, -2.53f), new Vector3(6.4f, 1.25f, 0.02f), _neonPink, false);
            Text(group, "Sign_JA", "ゲームセンター", new Vector3(0f, 3.98f, -2.6f), 0f, 0.5f, new Color(1f, 0.45f, 0.82f), 6f);
            Text(group, "Sign_VI", "GAME CENTER HIBARI · Trung tâm trò chơi", new Vector3(0f, 3.55f, -2.6f), 0f, 0.15f, new Color(0.55f, 0.92f, 1f), 6f);
            Block(group, "EntranceGlass", new Vector3(0f, 1.3f, -0.05f), new Vector3(3.2f, 2.6f, 0.06f), _dark, false);
            Block(group, "DoorFrame", new Vector3(0f, 2.7f, -0.1f), new Vector3(3.4f, 0.12f, 0.08f), _neonCyan, false);
            Text(group, "Door_Text", "いらっしゃいませ！", new Vector3(0f, 2.2f, -0.12f), 0f, 0.13f, new Color(1f, 0.85f, 0.4f), 3f);
            Machine(group, "claw-machine", "Street_Claw", group.TransformPoint(new Vector3(-2.6f, 0f, -0.9f)), 180f, 2.1f);
            Machine(group, "vending-machine", "Street_Vending", group.TransformPoint(new Vector3(2.6f, 0f, -0.7f)), 180f, 2.0f);
            PointLight(group, "NeonGlow_Pink", new Vector3(-2f, 1.6f, -3.2f), 5f, 0.55f, new Color(1f, 0.4f, 0.8f));
            PointLight(group, "NeonGlow_Cyan", new Vector3(2f, 1.6f, -3.2f), 5f, 0.55f, new Color(0.4f, 0.9f, 1f));

            var portalObject = new GameObject("GameCenterPortal") { layer = InteractableLayer };
            portalObject.transform.SetParent(portals);
            portalObject.transform.SetPositionAndRotation(new Vector3(group.position.x, 1.1f, frontZ + 0.6f), Quaternion.Euler(0f, 180f, 0f));
            var box = portalObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.6f, 2.2f, 1.2f);
            box.isTrigger = true;
            var portal = portalObject.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance, "ゲームセンター / Game Center", false);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "ゲームセンターに はいる";
            so.FindProperty("promptEn").stringValue = "Vào Game Center";
            so.ApplyModifiedPropertiesWithoutUndo();

            var spawn = new GameObject("Spawn_" + WorldLocationCatalog.CityGameCenterReturn);
            spawn.transform.SetParent(portals);
            spawn.transform.SetPositionAndRotation(new Vector3(group.position.x, 0.08f, frontZ + 2.4f), Quaternion.identity);
            spawn.AddComponent<SceneSpawnPoint>().Configure(WorldLocationCatalog.CityGameCenterReturn);

            foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + CityPath);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
