using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.NPC;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace NihongoLife.World
{
    /// <summary>One resident of the open world: who they are, which Kids-family body, where they live and what they say.</summary>
    public sealed class FolkSpec
    {
        public string Id, Name, Model, Hair, Beard; public int Age;
        /// <summary>Midori: the name of a child of the island root to stand beside ("" = near the arrival point).</summary>
        public string Place;
        public ScriptedNpcLine[] Lines;
        public FolkSpec(string id, string name, string model, string hair, string beard, int age, string place, params ScriptedNpcLine[] lines)
        { Id = id; Name = name; Model = model; Hair = hair; Beard = beard; Age = age; Place = place; Lines = lines; }
    }

    /// <summary>
    /// F3 / H6: people who live in the world. Midori Island gets eight residents (farmer, rancher, shopkeeper, angler, station
    /// attendant, a child, a grandmother, a hiker) and the town's empty houses get a resident each who potters about the front
    /// door by day and is "indoors" (hidden) at night. They are the same Kids-family bodies as everyone else, talk through
    /// <see cref="ScriptedNpcDialogue"/> (so every line exists in vi / en / ja / ru) and are cheap: no NavMesh, no physics bodies,
    /// and everyone far from the player is switched off.
    /// </summary>
    public sealed class TownFolk : MonoBehaviour
    {
        public static TownFolk Instance { get; private set; }
        public static bool Disabled;
        public static float? HourOverride;
        public const float ShowRadius = 75f, HideRadius = 95f, MaxCityFolk = 6;

        public readonly List<FolkWalker> Residents = new List<FolkWalker>();
        private bool _built;
        private Transform _player;

        private static ScriptedNpcLine L(string ja, string reading, string romaji, string vi, string en) => new ScriptedNpcLine(ja, reading, romaji, vi, en);

        public static readonly FolkSpec[] Midori =
        {
            new FolkSpec("midori_kenta", "Kenta", "kid_riku", "natural", "stubble", 52, "Farm",
                L("今年のにんじんは、よくできましたよ。", "ことしのにんじんは、よくできましたよ。", "Kotoshi no ninjin wa, yoku dekimashita yo.", "Cà rốt năm nay được mùa lắm đấy.", "This year's carrots came out really well."),
                L("畑の水やりは、朝がいちばんです。", "はたけのみずやりは、あさがいちばんです。", "Hatake no mizuyari wa, asa ga ichiban desu.", "Tưới ruộng vào buổi sáng là tốt nhất.", "Morning is the best time to water the field.")),
            new FolkSpec("midori_aoi", "Aoi", "kid_mio", "natural", "none", 30, "AnimalPen",
                L("この子たちは、毎朝ミルクをくれます。", "このこたちは、まいあさミルクをくれます。", "Kono kotachi wa, maiasa miruku o kuremasu.", "Mấy bé này cho sữa mỗi sáng.", "These ones give me milk every morning."),
                L("動物にやさしくしてくださいね。", "どうぶつにやさしくしてくださいね。", "Doubutsu ni yasashiku shite kudasai ne.", "Hãy nhẹ nhàng với các con vật nhé.", "Please be gentle with the animals.")),
            new FolkSpec("midori_mei", "Mei", "kid_hina", "natural", "none", 45, "MidoriStore",
                L("いらっしゃいませ。島の野菜は新鮮ですよ。", "いらっしゃいませ。しまのやさいはしんせんですよ。", "Irasshaimase. Shima no yasai wa shinsen desu yo.", "Xin chào quý khách. Rau của đảo rất tươi đấy.", "Welcome. The island's vegetables are fresh."),
                L("とれたものは、ここで売れますよ。", "とれたものは、ここでうれますよ。", "Toreta mono wa, koko de uremasu yo.", "Thứ thu hoạch được có thể bán ở đây.", "You can sell what you harvest right here.")),
            new FolkSpec("midori_genji", "Genji", "kid_haruto", "ash", "mustache", 64, "Lookout",
                L("今日は風がやさしいから、魚がよく釣れそうだ。", "きょうはかぜがやさしいから、さかながよくつれそうだ。", "Kyou wa kaze ga yasashii kara, sakana ga yoku tsuresou da.", "Hôm nay gió nhẹ, chắc câu được nhiều cá.", "The wind is gentle today. The fish should be biting."),
                L("釣りはあせらないことが大切だよ。", "つりはあせらないことがたいせつだよ。", "Tsuri wa aseranai koto ga taisetsu da yo.", "Câu cá quan trọng là đừng nóng vội.", "In fishing, the key is not to rush.")),
            new FolkSpec("midori_souta", "Souta", "kid_sota", "natural", "none", 28, "",
                L("ようこそ、みどり島へ。", "ようこそ、みどりじまへ。", "Youkoso, Midori-jima e.", "Chào mừng đến đảo Midori.", "Welcome to Midori Island."),
                L("帰りの電車は、駅でお待ちください。", "かえりのでんしゃは、えきでおまちください。", "Kaeri no densha wa, eki de omachi kudasai.", "Tàu về xin hãy đợi ở nhà ga.", "Please wait at the station for the train back.")),
            new FolkSpec("midori_yui", "Yui", "kid_yui", "natural", "none", 7, "Nature",
                L("ねえ、いっしょに鳥を見よう！", "ねえ、いっしょにとりをみよう！", "Nee, issho ni tori o miyou!", "Này, cùng ngắm chim đi!", "Hey, let's watch the birds together!"),
                L("りすが木にのぼるの、見た？", "りすがきにのぼるの、みた？", "Risu ga ki ni noboru no, mita?", "Cậu thấy sóc leo cây chưa?", "Have you seen a squirrel climb a tree?")),
            new FolkSpec("midori_tsuru", "Tsuru", "kid_hina", "silver", "none", 74, "Signs",
                L("この島は、むかしから静かなところですよ。", "このしまは、むかしからしずかなところですよ。", "Kono shima wa, mukashi kara shizuka na tokoro desu yo.", "Hòn đảo này từ xưa đã yên tĩnh lắm.", "This island has always been a quiet place."),
                L("お茶でも飲んでいきなさい。", "おちゃでものんでいきなさい。", "Ocha demo nonde ikinasai.", "Uống chén trà rồi hãy đi.", "Come, have some tea before you go.")),
            new FolkSpec("midori_daichi", "Daichi", "kid_yuto", "natural", "stubble", 35, "Station",
                L("展望台からの景色は最高だったよ。", "てんぼうだいからのけしきはさいこうだったよ。", "Tenboudai kara no keshiki wa saikou datta yo.", "Cảnh nhìn từ đài quan sát tuyệt lắm.", "The view from the lookout was the best."),
                L("道に迷ったら、看板を見てね。", "みちにまよったら、かんばんをみてね。", "Michi ni mayottara, kanban o mite ne.", "Nếu lạc đường thì hãy xem biển chỉ dẫn.", "If you get lost, check the signposts.")),
        };

        public static readonly FolkSpec[] City =
        {
            new FolkSpec("city_ume", "Ume", "kid_hina", "silver", "none", 62, "",
                L("うちの庭に、さくらの木があるんですよ。", "うちのにわに、さくらのきがあるんですよ。", "Uchi no niwa ni, sakura no ki ga arun desu yo.", "Vườn nhà tôi có một cây anh đào đấy.", "There's a cherry tree in my garden."),
                L("春になると、花がとてもきれいです。", "はるになると、はながとてもきれいです。", "Haru ni naru to, hana ga totemo kirei desu.", "Mỗi độ xuân về, hoa rất đẹp.", "In spring, the blossoms are beautiful.")),
            new FolkSpec("city_ken", "Ken", "kid_riku", "natural", "stubble", 41, "",
                L("今から仕事に行くところです。", "いまからしごとにいくところです。", "Ima kara shigoto ni iku tokoro desu.", "Tôi đang chuẩn bị đi làm.", "I'm just about to head off to work."),
                L("駅まで歩いて十分ですよ。", "えきまであるいてじっぷんですよ。", "Eki made aruite juppun desu yo.", "Đi bộ đến nhà ga mất mười phút.", "It's a ten-minute walk to the station.")),
            new FolkSpec("city_saki", "Saki", "kid_sakura", "natural", "none", 9, "",
                L("わたしの家は、あの青い屋根だよ。", "わたしのいえは、あのあおいやねだよ。", "Watashi no ie wa, ano aoi yane da yo.", "Nhà mình có mái màu xanh kia kìa.", "My house is the one with the blue roof."),
                L("いっしょにあそぼう！", "いっしょにあそぼう！", "Issho ni asobou!", "Chơi cùng nhau nhé!", "Let's play together!")),
            new FolkSpec("city_gen", "Gen", "kid_haruto", "ash", "mustache", 68, "",
                L("朝の散歩は、体にいいですよ。", "あさのさんぽは、からだにいいですよ。", "Asa no sanpo wa, karada ni ii desu yo.", "Đi dạo buổi sáng tốt cho sức khỏe đấy.", "A morning walk is good for your health."),
                L("コンビニの新しいおにぎりを食べましたか。", "コンビニのあたらしいおにぎりをたべましたか。", "Konbini no atarashii onigiri o tabemashita ka.", "Cháu ăn thử cơm nắm mới ở cửa hàng tiện lợi chưa?", "Have you tried the new rice balls at the konbini?")),
            new FolkSpec("city_mina", "Mina", "kid_mio", "natural", "none", 33, "",
                L("うちの犬が、あなたに会いたそうです。", "うちのいぬが、あなたにあいたそうです。", "Uchi no inu ga, anata ni aitasou desu.", "Chú chó nhà tôi có vẻ muốn gặp bạn.", "My dog seems to want to meet you."),
                L("夕方になると、公園が静かになりますよ。", "ゆうがたになると、こうえんがしずかになりますよ。", "Yuugata ni naru to, kouen ga shizuka ni narimasu yo.", "Chiều tối công viên sẽ yên tĩnh hơn.", "The park gets quiet in the evening.")),
            new FolkSpec("city_taro", "Taro", "kid_sota", "natural", "none", 8, "",
                L("ぼくは、毎日学校に通ってるんだ。", "ぼくは、まいにちがっこうにかよってるんだ。", "Boku wa, mainichi gakkou ni kayotteru n da.", "Mình đi học mỗi ngày đấy.", "I go to school every day."),
                L("しゅくだいが多くて、たいへんだよ。", "しゅくだいがおおくて、たいへんだよ。", "Shukudai ga ookute, taihen da yo.", "Bài tập nhiều quá, vất vả lắm.", "There's a lot of homework. It's tough.")),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; Disabled = false; HourOverride = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            RegisterLines();
            var go = new GameObject("TownFolk");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<TownFolk>();
            SceneManager.sceneLoaded += (s, m) => { if (Instance != null) Instance.ResetForScene(); };
        }

        /// <summary>Makes every resident's lines known to <see cref="ScriptedNpcDialogue"/> (safe to call twice).</summary>
        public static void RegisterLines()
        {
            foreach (var s in Midori.Concat(City)) ScriptedNpcDialogue.ByNpc[s.Id] = s.Lines;
        }

        public static bool SceneHasFolk(string scene) => scene == WorldLocationCatalog.CityScene || scene == WorldLocationCatalog.MidoriIslandScene;

        /// <summary>Whether residents are out of doors at this hour (pure; unit tested).</summary>
        public static bool IsOutdoors(float hour) => hour >= 6.5f && hour < 21.5f;

        public float CurrentHour => HourOverride ?? (AmbientWildlife.HourOverride ?? (AmbientWildlife.Instance != null ? AmbientWildlife.Instance.CurrentHour : 12f));

        private void ResetForScene()
        {
            foreach (var r in Residents) if (r != null) Destroy(r.gameObject);
            Residents.Clear(); _built = false; _player = null;
        }

        private void Update()
        {
            if (Disabled || !SceneHasFolk(SceneManager.GetActiveScene().name)) return;
            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                _player = pc.transform;
            }
            if (!_built)
            {
                if (Time.timeSinceLevelLoad < 3.5f) return;
                _built = true; Build();
            }
            bool out_ = IsOutdoors(CurrentHour);
            foreach (var r in Residents)
            {
                if (r == null) continue;
                float d = Vector3.Distance(_player.position, r.Home);
                bool want = out_ && (r.gameObject.activeSelf ? d < HideRadius : d < ShowRadius);
                if (r.gameObject.activeSelf != want) r.gameObject.SetActive(want);
            }
        }

        private void Build()
        {
            RegisterLines();
            bool midori = SceneManager.GetActiveScene().name == WorldLocationCatalog.MidoriIslandScene;
            var spots = midori ? MidoriSpots() : CitySpots();
            var specs = midori ? Midori : City;
            for (int i = 0; i < specs.Length && i < spots.Count; i++) Spawn(specs[i], spots[i]);
        }

        // ── Where people stand ──

        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        /// <summary>The ground under a point: the nearest hit below it that is not <paramref name="ignore"/> (a resident's own body).</summary>
        public static bool Ground(Vector3 from, out Vector3 point, Transform ignore = null)
        {
            point = from;
            int n = Physics.RaycastNonAlloc(from + Vector3.up * 6f, Vector3.down, Hits, 40f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i];
                if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
                if (h.collider.GetComponentInParent<FolkWalker>() != null || h.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (h.distance < best) { best = h.distance; point = h.point; found = true; }
            }
            return found;
        }

        public static bool Free(Vector3 groundPoint) =>
            !Physics.CheckCapsule(groundPoint + Vector3.up * 0.5f, groundPoint + Vector3.up * 1.5f, 0.35f, ~0, QueryTriggerInteraction.Ignore);

        /// <summary>The nearest open spot on a ring around a building (or group), preferring the side that faces <paramref name="toward"/>.</summary>
        public static bool DoorStep(Bounds b, Vector3 toward, out Vector3 spot)
        {
            spot = default; float best = float.MaxValue; bool found = false;
            float r0 = Mathf.Max(b.extents.x, b.extents.z);
            for (float extra = 1.8f; extra <= 7f && !found; extra += 2.4f)
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    var p = b.center + dir * (r0 + extra);
                    if (!Ground(new Vector3(p.x, b.min.y + 1.5f, p.z), out var g) || Mathf.Abs(g.y - b.min.y) > 1.2f || !Free(g)) continue;
                    float d = (g - toward).sqrMagnitude;
                    if (d < best) { best = d; spot = g; found = true; }
                }
            return found;
        }

        private static Bounds BoundsOf(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        private List<Vector3> MidoriSpots()
        {
            var list = new List<Vector3>();
            var root = GameObject.Find("MidoriIsland_Zone");
            var arrival = _player.position;
            foreach (var s in Midori)
            {
                Vector3 spot;
                var place = string.IsNullOrEmpty(s.Place) || root == null ? null : root.transform.Find(s.Place);
                bool ok;
                if (place == null || s.Place == "Station")
                {
                    ok = false; spot = arrival;
                    for (int k = 0; k < 24 && !ok; k++)
                    {
                        var o = new Vector3(Mathf.Cos(k * 0.9f), 0f, Mathf.Sin(k * 0.9f)) * (4f + k * 0.6f);
                        if (Ground(arrival + o, out var g) && Free(g)) { spot = g; ok = true; }
                    }
                    if (s.Place == "Station") { var sp = spot; if (Ground(arrival + new Vector3(-9f, 0f, 3f), out var g2) && Free(g2)) sp = g2; spot = sp; }
                }
                else ok = DoorStep(BoundsOf(place), arrival, out spot);
                list.Add(ok ? spot : arrival + new Vector3(3f + list.Count, 0f, 3f));
            }
            return list;
        }

        private List<Vector3> CitySpots()
        {
            var list = new List<Vector3>();
            var env = GameObject.Find("Environment");
            if (env == null) return list;
            var cands = new List<(Bounds b, float d)>();
            foreach (Transform tile in env.transform)
            {
                if (!tile.name.StartsWith("CityTile_")) continue;
                foreach (var t in tile.GetComponentsInChildren<Transform>(false))
                {
                    if (t == tile || !t.name.Contains("Building") || t.name.IndexOf("konbini", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    var b = BoundsOf(t);
                    if (b.size.y < 3f || b.size.x < 4f || b.size.z < 4f || b.size.x > 45f || b.size.z > 45f) continue;
                    float d = Vector3.Distance(new Vector3(b.center.x, 0f, b.center.z), new Vector3(_player.position.x, 0f, _player.position.z));
                    if (d > 8f && d < 130f) cands.Add((b, d));
                }
            }
            foreach (var c in cands.OrderBy(x => x.d))
            {
                if (list.Count >= MaxCityFolk) break;
                if (!DoorStep(c.b, _player.position, out var spot)) continue;
                if (list.Any(p => Vector3.Distance(p, spot) < 14f)) continue;
                list.Add(spot);
            }
            return list;
        }

        // ── Bodies ──

        private FolkWalker Spawn(FolkSpec s, Vector3 at)
        {
            var prefab = Resources.Load<GameObject>("Characters/Kids/NL_" + s.Model);
            if (prefab == null) return null;
            var go = Instantiate(prefab, at, Quaternion.identity, transform);
            go.name = "Folk_" + s.Id;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            var cap = go.AddComponent<CapsuleCollider>();
            float sy = Mathf.Max(0.01f, go.transform.lossyScale.y);
            cap.height = 1.6f / sy; cap.radius = 0.32f / sy; cap.center = new Vector3(0f, 0.8f / sy, 0f);
            var anim = go.AddComponent<CharacterAnimationController>();
            anim.SetAnimator(go.GetComponentInChildren<Animator>(true));
            CharacterLook.On(go, false).Fixed(s.Hair, s.Beard, s.Age);
            var npc = go.AddComponent<NPCController>();
            Set(npc, "npcId", s.Id); Set(npc, "displayName", s.Name); Set(npc, "role", s.Place);
            var w = go.AddComponent<FolkWalker>();
            w.Init(s, at, anim, npc, s.Age >= 60 ? 0.8f : 1.1f);
            Residents.Add(w);
            go.SetActive(false);
            return w;
        }

        private static void Set(NPCController npc, string field, string value) =>
            typeof(NPCController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(npc, value);
    }

    /// <summary>A resident's day: stand, look about, stroll a few metres around the doorstep (never into a wall), repeat. Stops to talk.</summary>
    public sealed class FolkWalker : MonoBehaviour
    {
        public FolkSpec Spec { get; private set; }
        public Vector3 Home { get; private set; }
        public NPCController Npc { get; private set; }
        public float Walked { get; private set; }
        public bool Walking { get; private set; }
        private CharacterAnimationController _anim;
        private Vector3 _target; private float _wait, _speed, _wanderRadius = 4.5f;

        public void Init(FolkSpec spec, Vector3 home, CharacterAnimationController anim, NPCController npc, float speed)
        { Spec = spec; Home = home; _anim = anim; Npc = npc; _speed = speed; _wait = Random.Range(0.5f, 3f); }

        private void Update()
        {
            if (Npc != null && Npc.IsInteracting) { Walking = false; _anim?.SetSpeed(0f); return; }
            if (Walking)
            {
                var to = _target - transform.position; to.y = 0f;
                if (to.magnitude < 0.2f || Blocked(to.normalized)) { Walking = false; _wait = Random.Range(2.5f, 7f); _anim?.SetSpeed(0f); return; }
                var step = to.normalized * (_speed * Time.deltaTime);
                var p = transform.position + step;
                if (TownFolk.Ground(p, out var g, transform) && Mathf.Abs(g.y - transform.position.y) < 0.6f) p = g; else { Walking = false; _wait = 2f; _anim?.SetSpeed(0f); return; }
                Walked += step.magnitude;
                transform.position = p;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), 8f * Time.deltaTime);
                _anim?.SetSpeed(_speed);
                return;
            }
            _anim?.SetSpeed(0f);
            _wait -= Time.deltaTime;
            if (_wait > 0f) return;
            for (int i = 0; i < 6; i++)
            {
                var o = Random.insideUnitCircle * _wanderRadius;
                var p = Home + new Vector3(o.x, 0f, o.y);
                if (!TownFolk.Ground(p, out var g, transform) || Mathf.Abs(g.y - Home.y) > 0.8f || !TownFolk.Free(g)) continue;
                if (Physics.Linecast(transform.position + Vector3.up * 0.5f, g + Vector3.up * 0.5f, out _, ~0, QueryTriggerInteraction.Ignore)) continue;
                _target = g; Walking = true; return;
            }
            _wait = 3f;
        }

        private bool Blocked(Vector3 dir) => Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, 0.6f, ~0, QueryTriggerInteraction.Ignore);
    }
}