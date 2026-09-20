using System.Collections;
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
            StartCoroutine(StabilizePlayerSpawn());
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

            Transform spawn = GameObject.Find("Spawn_sushi_entrance")?.transform;
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
        public string GetPromptJa() => "職人と話す";
        public string GetpromptEn() => "Nói chuyện với đầu bếp";
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            RestaurantMenuUI ui = RestaurantMenuUI.GetOrCreate();
            switch (_line++ % 3)
            {
                case 0:
                    ui.ShowStaffLine("Ota", "いらっしゃいませ。", "Irasshaimase.", Pick("Chào mừng quý khách.", "Welcome to Sushi Hibari."), 5f);
                    break;
                case 1:
                    ui.ShowStaffLine("Ota", "おすすめはサーモンです。", "Osusume wa saamon desu.", Pick("Món đề xuất hôm nay là sushi cá hồi.", "Today's recommendation is salmon sushi."), 6f);
                    break;
                default:
                    ui.ShowStaffLine("Ota", "ご注文はお決まりですか。", "Gochuumon wa okimari desu ka?", Pick("Bạn đã chọn món chưa? Hãy xem thực đơn hoặc ngồi vào bàn.", "Are you ready to order? Check the menu or take a seat."), 7f);
                    break;
            }
        }

        private static string Pick(string vi, string en)
        {
            if (!GameServices.TryGet(out GameSettingsService settings)) return vi;
            return settings.Language == GameLanguage.English ? en : vi;
        }
    }
}
