using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NihongoLife.World
{
    public sealed class SushiRestaurantRuntime : MonoBehaviour
    {
        private const float CenterX = 500f;
        private static readonly Color DarkWood = new(0.18f, 0.075f, 0.035f);
        private static readonly Color WarmWood = new(0.48f, 0.22f, 0.08f);
        private Transform _generated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneSupport()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != WorldLocationCatalog.SushiRestaurantScene) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "SushiRestaurant_Zone") continue;
                if (root.GetComponent<SushiRestaurantRuntime>() == null) root.AddComponent<SushiRestaurantRuntime>();
                break;
            }
        }

        private void Awake()
        {
            Transform old = transform.Find("SushiProduction_Runtime");
            if (old != null) Destroy(old.gameObject);
            _generated = new GameObject("SushiProduction_Runtime").transform;
            _generated.SetParent(transform, false);

            BuildSafetyGeometry();
            BuildChefStation();
            BuildRestaurantIdentity();
            BuildDiners();
            StartCoroutine(StabilizePlayerSpawn());
        }

        /// <summary>Customers at the counter (DinerFlow): stools along it, the register by the door; the player keeps
        /// the middle seat and the two tables.</summary>
        private void BuildDiners()
        {
            var seats = new List<(Vector3, Quaternion)>();
            foreach (float dx in new[] { -4.2f, -2.4f, 2.4f, 4.2f })
            {
                var seat = new Vector3(CenterX + dx, 0f, 2.3f);
                Block("CounterStool_" + dx, seat + new Vector3(0f, 0.22f, 0f), new Vector3(0.12f, 0.44f, 0.12f), DarkWood, false);
                Block("CounterStoolTop_" + dx, seat + new Vector3(0f, 0.46f, 0f), new Vector3(0.46f, 0.07f, 0.46f), WarmWood, false);
                seats.Add((seat, Quaternion.identity)); // facing the counter (+Z)
            }
            Block("Register", new Vector3(CenterX + 5.4f, 0.5f, -6.3f), new Vector3(1.3f, 1f, 0.6f), WarmWood, true);
            Block("RegisterTill", new Vector3(CenterX + 5.4f, 1.12f, -6.3f), new Vector3(0.45f, 0.25f, 0.35f), new Color(0.2f, 0.2f, 0.22f), false);
            var chef = _generated.Find("SushiChef_Ota");
            var diners = gameObject.AddComponent<DinerFlow>();
            diners.Configure(new DinerFlow.Setup
            {
                door = new Vector3(CenterX, 0f, -8.3f),
                inside = new Vector3(CenterX + 1.2f, 0f, -4f),
                register = new Vector3(CenterX + 4.3f, 0f, -6.3f),
                registerFacing = Quaternion.Euler(0f, 90f, 0f),
                seats = seats.ToArray(),
                cook = chef,
                cashier = chef,
            }, NihongoLife.UI.NLUi.ResolveFont());
        }

        private IEnumerator StabilizePlayerSpawn()
        {
            yield return null;
            yield return new WaitForFixedUpdate();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;

            bool invalid = player.transform.position.y < -0.2f
                || Mathf.Abs(player.transform.position.x - CenterX) > 8.2f
                || Mathf.Abs(player.transform.position.z) > 9.2f;
            if (!invalid) yield break;

            Transform spawn = SceneRefs.FindNamed("Spawn_sushi_entrance")?.transform;
            Vector3 target = spawn != null ? spawn.position : new Vector3(CenterX, 0.25f, -6.2f);
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(target, Quaternion.identity);
            if (controller != null) controller.enabled = true;
        }

        private void BuildSafetyGeometry()
        {
            GameObject catchFloor = Block("SafetyCatchFloor", new Vector3(CenterX, -0.7f, 0f), new Vector3(17f, 1f, 19f), Color.black, true);
            catchFloor.GetComponent<Renderer>().enabled = false;
            InvisibleWall("SafetyNorth", new Vector3(CenterX, 1.4f, 8.65f), new Vector3(15.6f, 2.8f, 0.35f));
            InvisibleWall("SafetySouthLeft", new Vector3(CenterX - 5.1f, 1.4f, -8.65f), new Vector3(5.5f, 2.8f, 0.35f));
            InvisibleWall("SafetySouthRight", new Vector3(CenterX + 5.1f, 1.4f, -8.65f), new Vector3(5.5f, 2.8f, 0.35f));
            InvisibleWall("SafetyWest", new Vector3(CenterX - 7.65f, 1.4f, 0f), new Vector3(0.35f, 2.8f, 17f));
            InvisibleWall("SafetyEast", new Vector3(CenterX + 7.65f, 1.4f, 0f), new Vector3(0.35f, 2.8f, 17f));
        }

        private void BuildChefStation()
        {
            Block("ChefPrepMat", new Vector3(CenterX, 1.13f, 6.95f), new Vector3(2.4f, 0.08f, 0.85f), new Color(0.72f, 0.54f, 0.32f), false);
            Block("ChefKnife", new Vector3(CenterX + 0.55f, 1.22f, 6.85f), new Vector3(0.08f, 0.05f, 0.62f), new Color(0.65f, 0.7f, 0.73f), false);
            Block("FishTray", new Vector3(CenterX - 0.7f, 1.2f, 6.9f), new Vector3(0.7f, 0.08f, 0.52f), new Color(0.1f, 0.26f, 0.31f), false);
            for (int i = -1; i <= 1; i++)
                Block("PreparedSushi_" + i, new Vector3(CenterX - 0.7f + i * 0.2f, 1.31f, 6.9f), new Vector3(0.13f, 0.1f, 0.16f), new Color(0.92f, 0.45f, 0.35f), false);

            GameObject prefab = Resources.Load<GameObject>("Characters/NL_Cashier");
            if (prefab == null) return;
            GameObject chef = Instantiate(prefab, _generated);
            chef.name = "SushiChef_Ota";
            chef.transform.SetPositionAndRotation(new Vector3(CenterX, 0.02f, 7.75f), Quaternion.Euler(0f, 180f, 0f));
            foreach (Collider collider in chef.GetComponentsInChildren<Collider>()) collider.enabled = false;
            CapsuleCollider interaction = chef.AddComponent<CapsuleCollider>();
            interaction.center = new Vector3(0f, 1f, 0f);
            interaction.height = 2f;
            interaction.radius = 0.45f;
            interaction.isTrigger = true;
            chef.layer = 6;
            chef.AddComponent<SushiChefInteractable>();
            chef.AddComponent<SushiChefWorkMotion>();
        }

        private void BuildRestaurantIdentity()
        {
            Block("NorennHeader", new Vector3(CenterX, 3.35f, -8.45f), new Vector3(4.6f, 0.7f, 0.16f), DarkWood, false);
            Block("CounterFrontAccent", new Vector3(CenterX, 0.62f, 3.22f), new Vector3(10.7f, 0.72f, 0.12f), WarmWood, false);
            for (int i = -2; i <= 2; i++)
                Block("PendantGlow_" + i, new Vector3(CenterX + i * 2.1f, 3.1f, 3.4f), new Vector3(0.45f, 0.12f, 0.45f), new Color(1f, 0.68f, 0.25f), false, true);
        }

        private void InvisibleWall(string name, Vector3 position, Vector3 size)
        {
            GameObject wall = Block(name, position, size, Color.black, true);
            wall.GetComponent<Renderer>().enabled = false;
        }

        private GameObject Block(string name, Vector3 position, Vector3 size, Color color, bool collider, bool unlit = false)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(_generated);
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = size;
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Simple Lit");
            var material = new Material(shader) { color = color };
            Renderer renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            block.GetComponent<Collider>().enabled = collider;
            return block;
        }
    }

    public sealed class SushiChefWorkMotion : MonoBehaviour
    {
        private Vector3 _origin;
        private void Start() => _origin = transform.position;
        private void Update()
        {
            float shift = Mathf.Sin(Time.time * 1.4f) * 0.08f;
            transform.position = _origin + Vector3.right * shift;
            transform.rotation = Quaternion.Euler(0f, 180f + Mathf.Sin(Time.time * 0.7f) * 7f, 0f);
        }
    }

    public sealed class SushiChefInteractable : MonoBehaviour, IInteractable
    {
        private int _line;
        private static string ChefName => NihongoLife.Core.Loc.T("Bác Ota", "Chef Ota", "太田さん");
        public string GetPromptJa() => "職人と話す";
        public string GetpromptEn() => NihongoLife.Core.Loc.T("Nói chuyện với đầu bếp", "Talk to the chef", "職人と話す");
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (KitchenWork()) return;
            RestaurantMenuUI ui = RestaurantMenuUI.GetOrCreate();
            switch (_line++ % 3)
            {
                case 0:
                    ui.ShowStaffLine(ChefName, "いらっしゃいませ。", "Irasshaimase.", Pick("Chào mừng quý khách.", "Welcome to Sushi Hibari."), 5f);
                    break;
                case 1:
                    ui.ShowStaffLine(ChefName, "おすすめはサーモンです。", "Osusume wa saamon desu.", Pick("Món đề xuất hôm nay là sushi cá hồi.", "Today's recommendation is salmon sushi."), 6f);
                    break;
                default:
                    ui.ShowStaffLine(ChefName, "ご注文はお決まりですか。", "Gochuumon wa okimari desu ka?", Pick("Bạn đã chọn món chưa? Hãy xem thực đơn hoặc ngồi vào bàn.", "Are you ready to order? Check the menu or take a seat."), 7f);
                    break;
            }
        }

        // ─── The kitchen shift (job_kitchen_shift, the chef career's work): prep (仕込み) and cooking, each a question ───

        private static readonly (string ja, string vi, string en, (string label, string detail, bool correct, string hint)[] choices)[] Prep =
        {
            ("さかなを さばく ほうちょうは どれですか。", "Dao nào dùng để làm cá?", "Which knife do you use to fillet fish?", new[]
            {
                ("出刃包丁（でばぼうちょう）", "Dao 出刃 — lưỡi dày, chặt xương cá", true, null),
                ("菜切り包丁（なきりぼうちょう）", "Dao thái rau", false, "Dao 菜切り chỉ để thái rau; cá dùng 出刃."),
                ("パンきりナイフ", "Dao cắt bánh mì", false, "Dao răng cưa làm nát thịt cá."),
            }),
            ("すしめしの あわせずに いれるのは？", "Giấm trộn cơm sushi gồm gì?", "What goes into sushi-rice vinegar?", new[]
            {
                ("す・さとう・しお", "Giấm, đường, muối", true, null),
                ("しょうゆ・みりん", "Xì dầu, rượu mirin", false, "Đó là nước sốt teriyaki, không phải giấm cơm sushi."),
                ("ソース・マヨネーズ", "Sốt, mayonnaise", false, "Cơm sushi chỉ trộn giấm, đường và muối."),
            }),
            ("まないたを つかった あとは？", "Dùng thớt xong thì làm gì?", "After using the chopping board?", new[]
            {
                ("あらって しょうどくします。", "Rửa và khử trùng.", true, null),
                ("そのまま つぎの さかなを きります。", "Thái luôn con cá khác.", false, "Thớt bẩn lây vi khuẩn sang món khác (食中毒)."),
                ("みずで さっと ながすだけ。", "Chỉ xả qua nước.", false, "Cần rửa xà phòng rồi khử trùng (nước nóng / cồn / clo)."),
            }),
            ("なまの さかなは なんどで ほぞんしますか。", "Cá sống bảo quản ở nhiệt độ nào?", "At what temperature do you keep raw fish?", new[]
            {
                ("10ど いかの れいぞうこ", "Tủ lạnh dưới 10 °C", true, null),
                ("へやの おんど", "Nhiệt độ phòng", false, "Cá sống để ngoài nhanh hỏng — luôn giữ lạnh."),
                ("あたたかい ところ", "Chỗ ấm", false, "Chỗ ấm là nơi vi khuẩn sinh sôi nhanh nhất."),
            }),
        };

        private static readonly (string ja, string vi, string en, (string label, string detail, bool correct, string hint)[] choices)[] Cook =
        {
            ("にぎりの シャリは どのくらいの おおきさ？", "Cơm của một miếng nigiri to cỡ nào?", "How big is the rice of one nigiri?", new[]
            {
                ("ひとくちで たべられる おおきさ", "Vừa một miếng ăn (15–20 g)", true, null),
                ("おにぎりと おなじ おおきさ", "To bằng cơm nắm", false, "Nigiri nhỏ, khoảng 15–20 g cơm."),
                ("できるだけ おおきく", "Càng to càng tốt", false, "To quá thì vỡ và lấn vị cá."),
            }),
            ("りょうりが できました。おきゃくさまに なんと いいますか。", "Món xong rồi. Bạn nói gì với khách?", "The dish is ready. What do you say to the guest?", new[]
            {
                ("おまちどおさまでした。", "Xin lỗi đã để quý khách chờ — món đây ạ.", true, null),
                ("いただきます。", "Mời cả nhà ăn.", false, "Câu này khách nói trước khi ăn."),
                ("いらっしゃいませ。", "Kính chào quý khách.", false, "Câu này nói khi khách vừa vào."),
            }),
            ("わさびは どこに つけますか。", "Wasabi đặt ở đâu trong miếng nigiri?", "Where does the wasabi go in a nigiri?", new[]
            {
                ("ネタと シャリの あいだ", "Giữa lát cá và cơm", true, null),
                ("シャリの したに", "Dưới đáy cơm", false, "Wasabi kẹp giữa cá và cơm."),
                ("ネタの うえに たくさん", "Thật nhiều trên mặt cá", false, "Wasabi chỉ một chút, giữa cá và cơm."),
            }),
            ("おきゃくさまに えびアレルギーが あります。", "Khách bị dị ứng tôm.", "The guest is allergic to shrimp.", new[]
            {
                ("えびを つかわず、べつの どうぐで つくります。", "Không dùng tôm, làm bằng dụng cụ riêng.", true, null),
                ("すこしなら だいじょうぶです。", "Một chút không sao.", false, "Dị ứng có thể nguy hiểm tính mạng — một chút cũng không được."),
                ("えびを とりのぞけば いいです。", "Nhặt tôm ra là được.", false, "Vẫn còn dính — phải làm lại bằng dụng cụ sạch."),
            }),
        };

        private const string KitchenShift = "job_kitchen_shift";

        /// <summary>During a kitchen shift (or a chef's career task) the chef sets a prep or a cooking question.</summary>
        private static bool KitchenWork()
        {
            string quest = null;
            foreach (var q in NihongoLife.Progression.QuestService.Active)
                if (q != null && (q.id == KitchenShift || q.career == "chef")) { quest = q.id; break; }
            if (quest == null) return false;
            if (NihongoLife.Progression.QuestService.ReadyToReport(quest))
            {
                NihongoLife.Progression.QuestService.Raise("talk", "npc_sushi_chef");
                RestaurantMenuUI.GetOrCreate().ShowStaffLine(ChefName, "おつかれさま。よく できた。", "Otsukaresama. Yoku dekita.", Pick("Vất vả rồi. Làm tốt lắm.", "Good work today."), 5f);
                return true;
            }
            bool prep = NihongoLife.Progression.QuestService.ObjectiveOpen(quest, "prep");
            var set = prep ? Prep : Cook;
            var (ja, vi, en, options) = set[Random.Range(0, set.Length)];
            var choices = new System.Collections.Generic.List<JobQuizCard.Choice>();
            foreach (var (label, detail, correct, hint) in options)
                choices.Add(new JobQuizCard.Choice { Label = label, Detail = NihongoLife.Core.Loc.IsJapanese ? null : detail, Correct = correct, WrongHint = hint });
            for (int i = choices.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (choices[i], choices[j]) = (choices[j], choices[i]); }
            JobQuizCard.Show(prep ? "仕込み · Sơ chế" : "調理 · Nấu", ChefName, ja, null, NihongoLife.Core.Loc.T(vi, en, ja), NihongoLife.Core.Loc.T("Chọn câu đúng:", "Pick the right answer:", "正しいのは？"), choices,
                () => NihongoLife.Progression.QuestService.Raise(prep ? "prep" : "cook", "sushi_kitchen"));
            return true;
        }

        /// <summary>Meaning of the chef's Japanese line in the interface language (none in the Japanese interface).</summary>
        private static string Pick(string vi, string en) => NihongoLife.Core.Loc.Gloss(vi, en);
    }
}
