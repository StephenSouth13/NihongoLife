using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Island;
using NihongoLife.Progression;
using NihongoLife.MiniGames;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Shop;
using NihongoLife.UI;
using NihongoLife.World;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.Trailer
{
    /// <summary>
    /// The NihongoLife trailer, shot inside the real game: title card → Hibari-chō from a crane → walking the
    /// street → an N5 dialogue → konbini shopping → emote → the bedroom → Hibari Station → the train ride and
    /// window view → the classroom → the Game Center and Kana Match → end card. Cinematic letterbox,
    /// Japanese/Vietnamese captions, dip-to-black cuts and an original theme. Driven by game time, so a
    /// recorder can run it at a fixed frame rate (Time.captureFramerate) and export a video.
    /// </summary>
    public sealed class TrailerReel : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private string sceneAfterTrailer = "01_MainMenu";

        private static readonly string[] HudAllowList = { "DialogueCanvas", "MiniGameCanvas", "EmoteWheelCanvas" };

        private Canvas _overlay;
        private Image _fade;
        private RectTransform _caption;
        private TextMeshProUGUI _captionJa;
        private TextMeshProUGUI _captionVi;
        private CanvasGroup _captionGroup;
        private RectTransform _titleCard;
        private CanvasGroup _titleGroup;
        private AudioSource _music;
        private Camera _camera;
        private PlayerController _player;
        private Scene _zone;
        private TMP_FontAsset _font;

        /// <summary>False while scenes load behind a black frame — the recorder skips those frames.</summary>
        public bool ShouldRecord { get; private set; }
        public bool Finished { get; private set; }
        public Camera ActiveCamera => _camera;
        public float Elapsed { get; private set; }
        public string CurrentShot { get; private set; }
        public bool ReturnToMenuWhenDone { get; set; } = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void Start()
        {
            if (playOnStart) StartCoroutine(Run());
        }

        public void Play() => StartCoroutine(Run());

        private void Update()
        {
            if (ShouldRecord) Elapsed += Time.deltaTime;
        }

        private void LateUpdate() => HideGameChrome();

        // ─────────── Sequence ───────────

        private IEnumerator Run()
        {
            Black(1f);
            yield return LoadCity();
            StartMusic();

            yield return Shot("title", 4.0f, TitleCard());
            yield return Shot("city", 5.0f, CityCrane(), "ひばり町へ ようこそ", "Một thị trấn Nhật Bản để sống và học");
            yield return Shot("walk", 4.0f, StreetWalk(), "あるいて、みつけて、はなそう", "Đi bộ, khám phá, trò chuyện");
            yield return Shot("dialogue", 4.5f, Dialogue(), "えらんで こたえる 会話", "Hội thoại N5 — chọn câu đúng ngữ cảnh");
            yield return Shot("konbini", 4.5f, Konbini(), "コンビニで かいもの", "Tự đi chợ bằng tiếng Nhật");
            yield return Shot("job", 6.0f, KonbiniJob(), "アルバイトで はたらく", "Làm thêm: xếp hàng, chỉ đường, tính tiền");
            yield return Shot("journal", 4.5f, Journal(), "タスクと レベル", "Sổ nhiệm vụ: việc làm, lương, cấp độ");

            yield return Zone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance);
            yield return Shot("station", 4.0f, Station(), "えきで きっぷを かう", "Mua vé, hỏi đường ở ga Hibari");
            yield return Shot("train", 5.5f, Train(), "でんしゃで みどりじまへ", "Đi tàu tới Đảo Midori");
            yield return Zone(WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation);
            yield return Shot("island", 6.0f, IslandCrane(), "みどりじま", "Đảo Midori — nông trại giữa biển");
            yield return Shot("farm", 6.5f, Farming(), "たがやして、うえて、みずを あげる", "Xới, gieo, tưới — dụng cụ quyết định tốc độ");
            yield return Shot("grow", 4.5f, Growth(), "そだてて、しゅうかく", "Cây lớn theo thời gian thật");
            yield return Shot("animals", 5.0f, Animals(), "どうぶつと なかよく", "Chăm sóc bò, alpaca, lừa và ngựa");
            yield return Shot("sell", 4.5f, SellProduce(), "うって、かって、くらす", "Bán nông sản, mua hạt giống mới");

            yield return Zone(WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance);
            yield return Shot("classroom", 4.0f, Classroom(), "ひばり日本語学院", "Lớp học & thi thử JLPT · IELTS");
            yield return Zone(WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance);
            yield return Shot("kana", 4.5f, KanaMatch(), "かなマッチ", "Lật thẻ, ghép cặp, học chữ");
            yield return Shot("end", 7.0f, EndCard());

            Finished = true;
            ShouldRecord = false;
            if (ReturnToMenuWhenDone && Application.CanStreamedLevelBeLoaded(sceneAfterTrailer))
            {
                Destroy(_overlay.gameObject);
                SceneManager.LoadScene(sceneAfterTrailer);
                Destroy(gameObject);
            }
        }

        /// <summary>Runs one shot: fade in, the body (camera move + staging), caption, fade out at the end.</summary>
        private IEnumerator Shot(string id, float seconds, IEnumerator body, string ja = null, string vi = null)
        {
            CurrentShot = id;
            CancelDialogue();
            ShouldRecord = true;
            StartCoroutine(Fade(1f, 0f, 0.35f));
            if (ja != null) StartCoroutine(Caption(ja, vi, seconds));
            float start = Time.time;
            var runner = StartCoroutine(body);
            while (Time.time - start < seconds - 0.35f) yield return null;
            yield return Fade(0f, 1f, 0.35f);
            StopCoroutine(runner);
            CancelDialogue();
        }

        // ─────────── Shots ───────────

        private void UiOnly(bool on)
        {
            if (_camera == null) return;
            if (on)
            {
                _savedMask = _camera.cullingMask;
                _savedClear = _camera.clearFlags;
                _camera.cullingMask = LayerMask.GetMask("UI");
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = Color.black;
            }
            else if (_savedMask != 0)
            {
                _camera.cullingMask = _savedMask;
                _camera.clearFlags = _savedClear;
            }
        }

        private int _savedMask;
        private CameraClearFlags _savedClear;

        private IEnumerator TitleCard()
        {
            UiOnly(true);
            _titleCard.gameObject.SetActive(true);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
            {
                _titleGroup.alpha = t;
                _titleCard.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, t);
                yield return null;
            }
            yield return Wait(2.2f);
            for (float t = 1f; t > 0f; t -= Time.deltaTime / 0.5f) { _titleGroup.alpha = t; yield return null; }
            _titleCard.gameObject.SetActive(false);
            UiOnly(false);
        }

        private IEnumerator CityCrane()
        {
            PlacePlayer(new Vector3(0f, 0.08f, -9f), 0f);
            yield return Move(new Vector3(34f, 30f, -48f), new Vector3(0f, 2f, 2f), new Vector3(7f, 6.5f, -21f), new Vector3(-1f, 1.6f, -2f), 6f);
        }

        private IEnumerator StreetWalk()
        {
            var anim = _player.GetComponentInChildren<CharacterAnimationController>();
            Vector3 from = new Vector3(0.8f, 0.08f, -14f), to = new Vector3(0.8f, 0.08f, -6.5f);
            float duration = 5f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                Vector3 p = Vector3.Lerp(from, to, t);
                PlacePlayer(p, 0f);
                anim?.SetSpeed(1.1f);
                _camera.transform.position = p + new Vector3(-3.6f, 1.7f, 1.6f);
                _camera.transform.rotation = Quaternion.LookRotation(p + Vector3.up * 1.2f + Vector3.forward * 0.6f - _camera.transform.position);
                yield return null;
            }
        }

        private IEnumerator Dialogue()
        {
            var guide = FindObjectsByType<NPCController>(FindObjectsSortMode.None).FirstOrDefault(n => n.NpcId == "npc_guide");
            Vector3 npc = guide != null ? guide.transform.position : new Vector3(-3f, 0f, -6f);
            Vector3 face = guide != null ? guide.transform.forward : Vector3.back;
            Vector3 stand = npc + face * 1.6f; stand.y = 0.08f;
            PlacePlayer(stand, Quaternion.LookRotation(npc - stand).eulerAngles.y);
            _player.GetComponentInChildren<CharacterAnimationController>()?.SetSpeed(0f);
            Vector3 side = Vector3.Cross(Vector3.up, (npc - stand).normalized);
            Vector3 cam = stand - (npc - stand).normalized * 1.6f + side * 0.9f + Vector3.up * 1.7f;
            _camera.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(npc + Vector3.up * 1.5f - cam));
            yield return Wait(0.4f);
            DialogueManager.Instance?.StartConversation(new List<ScenarioNode>
            {
                new ScenarioNode
                {
                    id = "trailer_hello", nodeType = ScenarioNodeType.Dialogue, speakerName = "リリー · Lilly",
                    textJa = "こんにちは！ひばり町へ ようこそ。", textReading = "こんにちは！ひばりちょうへ ようこそ。", textEn = "Xin chào! Chào mừng bạn tới Hibari-chō.",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { textJa = "はじめまして。よろしく おねがいします。", textEn = "Rất vui được gặp chị." },
                        new DialogueChoice { textJa = "おはよう。", textEn = "Chào (thân mật)." },
                        new DialogueChoice { textJa = "こんばんは。", textEn = "Chào buổi tối." },
                    }
                }
            }, "trailer_hello", null);
            yield return Move(cam, npc + Vector3.up * 1.5f, cam + side * 0.5f + (npc - stand).normalized * 0.4f, npc + Vector3.up * 1.45f, 4.5f);
        }

        private IEnumerator Konbini()
        {
            PlacePlayer(new Vector3(-1.4f, 0.08f, 3.0f), -90f);
            var shop = KonbiniShopUI.GetOrCreate();
            StartCoroutine(Delayed(2.2f, () => shop.OpenSection(KonbiniSection.Onigiri)));
            yield return Move(new Vector3(2.8f, 2.4f, 1.4f), new Vector3(-2f, 1.2f, 4.5f), new Vector3(1.6f, 1.9f, 2.2f), new Vector3(-2.2f, 1.1f, 3.4f), 5.5f);
        }

        private IEnumerator KonbiniJob()
        {
            KonbiniShopUI.GetOrCreate().Close();
            QuestService.Accept("job_konbini_shift");
            var station = FindObjectsByType<JobStation>(FindObjectsSortMode.None).FirstOrDefault(j => j.StationId == "customer_a");
            if (station == null) yield break;
            Vector3 f = station.transform.forward; f.y = 0f; f.Normalize();
            Vector3 stand = station.transform.position - f * 1.3f; stand.y = 0.08f;
            PlacePlayer(stand, Quaternion.LookRotation(f).eulerAngles.y);
            Vector3 side = Vector3.Cross(Vector3.up, f);
            Vector3 cam = stand - f * 2.0f + side * 0.9f + Vector3.up * 1.9f;
            _camera.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(station.transform.position + Vector3.up * 0.4f - cam));
            yield return Wait(0.9f);
            station.Interact(_player.gameObject);
            StartCoroutine(Move(cam, station.transform.position + Vector3.up * 0.4f, cam + f * 0.6f, station.transform.position + Vector3.up * 0.5f, 4.5f));
            yield return Wait(3.2f);
            int right = JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct);
            JobQuizCard.Answer(right);
        }

        private IEnumerator Journal()
        {
            JobQuizCard.Close();
            PlacePlayer(new Vector3(0.8f, 0.08f, -10f), 0f);
            Vector3 cam = new Vector3(4f, 3.2f, -16f);
            _camera.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(0f, 1.5f, -6f) - cam));
            TaskJournalUI.Open("job_konbini_shift");
            yield return Move(cam, new Vector3(0f, 1.5f, -6f), cam + new Vector3(-1.5f, 0.2f, 1f), new Vector3(-1f, 1.5f, -6f), 4.5f);
        }

        private IEnumerator IslandCrane()
        {
            TaskJournalUI.Close();
            IslandEconomy.Give("tool_hoe", 1);
            IslandEconomy.Give("tool_watering_can", 1);
            IslandEconomy.Give("seed_carrot", 3);
            Vector3 o = new Vector3(-1200f, 0f, 0f);
            PlacePlayer(o + new Vector3(-1.5f, 0.3f, -29.6f), 0f);
            yield return Move(o + new Vector3(-30f, 38f, -75f), o + new Vector3(0f, 0f, 0f), o + new Vector3(8f, 16f, -40f), o + new Vector3(-6f, 0f, 2f), 6f);
        }

        private FarmPlot _plot;

        private IEnumerator Farming()
        {
            _plot = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).FirstOrDefault(p => p.Number == 1);
            if (_plot == null) yield break;
            var r = _plot.Record; r.tilled = false; r.cropId = null; r.stage = 0; r.watered = false; _plot.Refresh(true);
            Vector3 pos = _plot.transform.position;
            PlacePlayer(pos + new Vector3(0f, 0.1f, -1.6f), 0f);
            Vector3 cam = pos + new Vector3(-3.4f, 2.6f, -4.2f);
            _camera.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(pos + Vector3.up * 0.6f - cam));
            StartCoroutine(Move(cam, pos + Vector3.up * 0.6f, cam + new Vector3(1.2f, -0.3f, 0.8f), pos + Vector3.up * 0.5f, 6.2f));
            IslandUI.OpenFarm(_plot);
            yield return Wait(0.6f);
            yield return Work("Đang xới đất…", "くわ", 1.6f, () => _plot.Till());
            IslandUI.OpenFarm(_plot);
            yield return Wait(0.4f);
            yield return Work("Đang gieo hạt…", "tay", 1.2f, () => _plot.Plant("carrot"));
            IslandUI.OpenFarm(_plot);
            yield return Wait(0.3f);
            yield return Work("Đang tưới nước…", "じょうろ", 1.5f, () => _plot.Water());
            IslandUI.OpenFarm(_plot);
        }

        private static IEnumerator Work(string label, string tool, float seconds, Action done)
        {
            TimedAction.Run(label, tool, seconds, done);
            while (TimedAction.Busy) yield return null;
        }

        private IEnumerator Growth()
        {
            IslandUI.CloseFarm();
            if (_plot == null) yield break;
            Vector3 pos = _plot.transform.position;
            Vector3 cam = pos + new Vector3(1.6f, 1.3f, -2.4f);
            StartCoroutine(Move(cam, pos + Vector3.up * 0.25f, cam + new Vector3(-0.8f, 0.4f, -0.4f), pos + Vector3.up * 0.4f, 4.2f));
            var crop = _plot.Crop;
            for (int stage = 0; stage < 3 && crop != null; stage++)
            {
                yield return Wait(0.9f);
                IslandState.ClockOffset += TimeSpan.FromSeconds(crop.secondsPerStage + 1);
                _plot.Advance();
                if (_plot.CurrentPhase == FarmPlot.Phase.NeedsWater) _plot.Water();
                _plot.Refresh(true);
            }
        }

        private IEnumerator Animals()
        {
            var cow = FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).FirstOrDefault(a => a.AnimalId == "cow");
            Vector3 c = new Vector3(-1200f + 18f, 0f, 6f);
            PlacePlayer(c + new Vector3(0f, 0.1f, -3.5f), 0f);
            yield return Move(c + new Vector3(-7f, 3.2f, -8f), c + Vector3.up * 0.8f, c + new Vector3(3f, 2.4f, -7f), c + Vector3.up * 0.9f, 3.2f);
            if (cow != null) IslandUI.OpenAnimal(cow);
            Vector3 p = _camera.transform.position;
            yield return Move(p, c + Vector3.up * 0.9f, p + new Vector3(0.6f, 0f, 0.6f), c + Vector3.up * 1f, 1.6f);
        }

        private IEnumerator SellProduce()
        {
            IslandUI.CloseAnimal();
            if (_plot != null && _plot.CurrentPhase == FarmPlot.Phase.Ready) _plot.Harvest(out _);
            if (IslandEconomy.Owned("carrot") == 0) IslandEconomy.Give("carrot", 2);
            Vector3 o = new Vector3(-1200f, 0f, 0f);
            Vector3 cam = o + new Vector3(4.5f, 2.4f, -20.5f);
            _camera.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(o + new Vector3(9f, 1.5f, -18f) - cam));
            IslandUI.OpenShop();
            IslandUI.SelectShopTab("sell");
            IslandUI.SelectShopItem("carrot");
            yield return Wait(2.2f);
            IslandUI.ConfirmShop();
            yield return Wait(2f);
        }

        private IEnumerator Emote()
        {
            KonbiniShopUI.GetOrCreate().Close();
            Vector3 spot = new Vector3(-8f, 0.08f, -4.5f);
            PlacePlayer(spot, 180f);
            Vector3 cam = spot + new Vector3(0.6f, 1.6f, -2.8f);
            _camera.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(spot + Vector3.up * 1.6f - cam));
            var emotes = _player.GetComponent<PlayerEmoteController>();
            var wheel = EmoteWheelUI.GetOrCreate();
            wheel.Open(1);
            yield return Wait(1.3f);
            wheel.Close();
            emotes?.Play(1);
            yield return Move(cam, spot + Vector3.up * 1.7f, cam + new Vector3(-0.4f, 0.2f, 0.5f), spot + Vector3.up * 1.9f, 2.4f);
        }

        private IEnumerator Bedroom()
        {
            yield return Move(new Vector3(2.9f, 2.3f, -2.5f), new Vector3(-1.4f, 0.7f, 1.2f), new Vector3(1.6f, 1.8f, -2.7f), new Vector3(-2.4f, 0.9f, 1.0f), 4.5f);
        }

        private IEnumerator Station()
        {
            yield return Move(new Vector3(804.5f, 2.3f, 10.6f), new Vector3(790.5f, 1.3f, 8.4f), new Vector3(803f, 2.2f, 9.4f), new Vector3(799f, 1.2f, -1.5f), 4.5f);
        }

        private IEnumerator Train()
        {
            var scenery = FindObjectsByType<TrainWindowScenery>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (scenery == null) yield break;
            var carriage = scenery.transform.parent.gameObject;
            carriage.SetActive(true);
            scenery.Throttle = 1f;
            foreach (var passenger in carriage.GetComponentsInChildren<SeatedPassenger>(true)) passenger.gameObject.SetActive(true);
            var spawn = carriage.transform.Find("TravelCarriageSpawn");
            if (spawn != null) PlacePlayer(spawn.position + spawn.forward * 2.5f, spawn.eulerAngles.y);
            Vector3 c = carriage.transform.TransformPoint(new Vector3(800f, 0f, 42f));
            yield return Move(c + new Vector3(-6.2f, 1.8f, 0f), c + new Vector3(4f, 1.1f, 0f), c + new Vector3(-3.6f, 1.6f, 0.2f), c + new Vector3(5f, 1.1f, -0.3f), 4.0f);
            var window = carriage.GetComponentsInChildren<Camera>(true).FirstOrDefault(cam => cam.name == "TrainWindowCamera");
            if (window != null)
            {
                _camera.transform.SetPositionAndRotation(window.transform.position, window.transform.rotation);
                yield return Move(window.transform.position, window.transform.position + window.transform.forward * 10f,
                    window.transform.position + window.transform.right * 0.3f, window.transform.position + window.transform.forward * 10f + window.transform.right * 2f, 3.5f);
            }
        }

        private IEnumerator Classroom()
        {
            IslandUI.CloseShop();
            yield return Move(new Vector3(1006.2f, 2.9f, -6.0f), new Vector3(999f, 1.2f, 3.5f), new Vector3(1004.2f, 2.2f, -4.2f), new Vector3(999.5f, 1.6f, 5.5f), 5f);
        }

        private IEnumerator Arcade()
        {
            yield return Move(new Vector3(300f, 1.9f, -8.2f), new Vector3(300f, 1.6f, 3f), new Vector3(299f, 1.7f, -3.4f), new Vector3(296.4f, 1.6f, 2f), 4f);
        }

        private IEnumerator KanaMatch()
        {
            var launcher = FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None).FirstOrDefault(l => l.Definition != null && l.Definition.id == "kana_match");
            if (launcher == null) yield break;
            PlacePlayer(launcher.transform.position + Vector3.back * 0.2f, 0f);
            if (!MiniGameController.GetOrCreate().Launch(launcher, _player.gameObject)) yield break;
            var game = MiniGameController.Instance.CurrentGame as KanaMatchGame;
            if (game == null) yield break;
            game.StartRound(launcher.Definition.contentSets.First(s => s.type == KanaPairType.HiraganaRomaji));
            yield return Wait(0.9f);   // a glimpse of the 3-2-1 intro
            game.SkipCountdown();
            var cards = game.Cards.ToList();
            for (int pair = 0; pair < 6; pair++)
            {
                var both = Enumerable.Range(0, cards.Count).Where(i => cards[i].PairIndex == pair).ToArray();
                game.Pick(both[0]);
                yield return Wait(0.18f);
                game.Pick(both[1]);
                yield return Wait(0.42f);
            }
        }

        private IEnumerator EndCard()
        {
            MiniGameController.Instance?.Close();
            Black(1f);
            UiOnly(true);
            var card = BuildEndCard();
            var group = card.GetComponent<CanvasGroup>();
            StartCoroutine(Fade(1f, 1f, 0.01f));
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
            {
                group.alpha = t;
                card.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, t);
                yield return null;
            }
            yield return Wait(5f);
        }

        // ─────────── Scenes ───────────

        private IEnumerator LoadCity()
        {
            ShouldRecord = false;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene, LoadSceneMode.Single);
            for (int i = 0; i < 90 && FindFirstObjectByType<PlayerController>() == null; i++) yield return null;
            yield return null;
            _player = FindFirstObjectByType<PlayerController>();
            _player.InputLocked = true;
            var detector = _player.GetComponent<InteractionDetector>();
            if (detector != null) detector.enabled = false;
            _camera = Camera.main;
            var follow = _camera.GetComponent<ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            yield return new WaitForSecondsRealtime(0.6f);
            CancelDialogue();
        }

        private IEnumerator Zone(string sceneName, string spawnId)
        {
            ShouldRecord = false;
            Black(1f);
            var city = SceneManager.GetSceneByName(WorldLocationCatalog.CityScene);
            var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!load.isDone) yield return null;
            var zone = SceneManager.GetSceneByName(sceneName);
            foreach (var root in city.GetRootGameObjects())
                if (root.GetComponent<SceneZoneVisibility>() != null) root.SetActive(false);
            SceneManager.SetActiveScene(zone);
            foreach (var root in zone.GetRootGameObjects())
                foreach (var preview in root.GetComponentsInChildren<ZonePreviewCamera>(true)) preview.gameObject.SetActive(false);
            if (_zone.IsValid() && _zone.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(_zone);
                while (unload != null && !unload.isDone) yield return null;
            }
            _zone = zone;
            var spawn = zone.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneSpawnPoint>(true)).FirstOrDefault(s => s.Id == spawnId);
            if (spawn != null) PlacePlayer(spawn.transform.position, spawn.transform.eulerAngles.y);
            yield return new WaitForSecondsRealtime(0.5f);
            CancelDialogue();
        }

        // ─────────── Helpers ───────────

        private IEnumerator Move(Vector3 fromPos, Vector3 fromLook, Vector3 toPos, Vector3 toLook, float seconds)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                Vector3 p = Vector3.Lerp(fromPos, toPos, k);
                Vector3 look = Vector3.Lerp(fromLook, toLook, k);
                _camera.transform.SetPositionAndRotation(p, Quaternion.LookRotation(look - p));
                yield return null;
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
        }

        private static IEnumerator Delayed(float seconds, Action action)
        {
            yield return Wait(seconds);
            action();
        }

        private void PlacePlayer(Vector3 position, float yaw)
        {
            if (_player == null) return;
            var body = _player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            _player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (body != null) body.enabled = true;
        }

        private static void CancelDialogue()
        {
            var dm = DialogueManager.Instance;
            for (int i = 0; i < 8 && dm != null && dm.IsOpen; i++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
        }

        /// <summary>Only the showcase UIs (dialogue, mini-game, emote wheel) stay visible during the trailer.</summary>
        private void HideGameChrome()
        {
            var hud = FindFirstObjectByType<HUDUI>();
            if (hud == null) return;
            foreach (Transform child in hud.transform)
                if (Array.IndexOf(HudAllowList, child.name) < 0 && child.gameObject.activeSelf) child.gameObject.SetActive(false);
            var hudImage = hud.GetComponent<Image>();
            if (hudImage != null) hudImage.enabled = false;
        }

        private void StartMusic()
        {
            var clip = Resources.Load<AudioClip>("Audio/Trailer/nihongolife_theme");
            if (clip == null) return;
            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = clip;
            _music.volume = 0.85f;
            _music.Play();
        }

        // ─────────── Overlay ───────────

        private void BuildOverlay()
        {
            _font = NLUi.ResolveFont();
            _overlay = NLUi.CreateCanvas("TrailerOverlay", 3000, transform);
            var root = (RectTransform)_overlay.transform;
            foreach (bool top in new[] { true, false })
            {
                var bar = new GameObject(top ? "LetterboxTop" : "LetterboxBottom", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                bar.SetParent(root, false);
                bar.anchorMin = new Vector2(0f, top ? 0.885f : 0f);
                bar.anchorMax = new Vector2(1f, top ? 1f : 0.115f);
                bar.offsetMin = bar.offsetMax = Vector2.zero;
                bar.GetComponent<Image>().color = Color.black;
            }

            _caption = NLUi.Panel(root, "Caption", new Color(0f, 0f, 0f, 0f), new RectOffset(0, 0, 0, 0), 2f);
            _caption.anchorMin = _caption.anchorMax = new Vector2(0.5f, 0.0575f);
            _caption.pivot = new Vector2(0.5f, 0.5f);
            _caption.anchoredPosition = Vector2.zero;
            _caption.sizeDelta = new Vector2(1600f, 0f);
            NLUi.FitContent(_caption);
            _captionJa = NLUi.Label(_caption, "Ja", "", 40f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _captionVi = NLUi.Label(_caption, "Vi", "", 22f, new Color(1f, 0.82f, 0.4f), _font, FontStyles.Normal, TextAlignmentOptions.Center);
            foreach (var label in new[] { _captionJa, _captionVi })
            {
                var shadow = label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(3f, -3f);
            }
            _captionGroup = _caption.gameObject.AddComponent<CanvasGroup>();
            _captionGroup.alpha = 0f;

            var mark = NLUi.Label(root, "Watermark", "NihongoLife", 20f, new Color(1f, 1f, 1f, 0.55f), _font, FontStyles.Bold, TextAlignmentOptions.Right);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(1f, 0.885f);
            mark.rectTransform.pivot = new Vector2(1f, 0f);
            mark.rectTransform.anchoredPosition = new Vector2(-48f, -44f);
            mark.rectTransform.sizeDelta = new Vector2(400f, 34f);

            _titleCard = new GameObject("TitleCard", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            _titleCard.SetParent(root, false);
            NLUi.Stretch(_titleCard);
            _titleGroup = _titleCard.GetComponent<CanvasGroup>();
            var titleBack = new GameObject("Back", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            titleBack.transform.SetParent(_titleCard, false);
            NLUi.Stretch(titleBack.rectTransform);
            titleBack.color = new Color(0.05f, 0.07f, 0.12f, 1f);
            Logo(_titleCard, "Logo", new Vector2(0f, 70f), 760f);
            var tagline = NLUi.Label(_titleCard, "Tagline", "ひばり町で、日本語と暮らそう\n<size=55%><color=#E8EEF6>Sống ở Nhật — học tiếng Nhật</color></size>", 46f, new Color(1f, 0.82f, 0.4f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            tagline.rectTransform.anchorMin = tagline.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            tagline.rectTransform.anchoredPosition = new Vector2(0f, -120f);
            tagline.rectTransform.sizeDelta = new Vector2(1400f, 160f);
            _titleCard.gameObject.SetActive(false);

            _fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _fade.transform.SetParent(root, false);
            NLUi.Stretch(_fade.rectTransform);
            _fade.color = Color.black;
            _fade.raycastTarget = false;
            _titleCard.SetAsLastSibling();
            SetLayer(_overlay.transform, LayerMask.NameToLayer("UI"));
        }

        private RectTransform BuildEndCard()
        {
            var root = (RectTransform)_overlay.transform;
            var card = new GameObject("EndCard", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            card.SetParent(root, false);
            NLUi.Stretch(card);
            var back = new GameObject("Back", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(card, false);
            NLUi.Stretch(back.rectTransform);
            back.color = new Color(0.05f, 0.07f, 0.12f, 1f);
            card.GetComponent<CanvasGroup>().alpha = 0f;
            Logo(card, "Logo", new Vector2(0f, 150f), 820f);
            var lines = NLUi.Label(card, "Lines",
                "ひばり町で、日本語と暮らそう\n<size=55%><color=#E8EEF6>Game mô phỏng cuộc sống 3D · học tiếng Nhật N5</color></size>\n\n<size=45%><color=#A8B4C4>Hội thoại · Làm thêm · Tàu điện · Đảo Midori · Nông trại · Luyện thi · Kana Match</color></size>",
                44f, new Color(1f, 0.82f, 0.4f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            lines.rectTransform.anchorMin = lines.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lines.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            lines.rectTransform.sizeDelta = new Vector2(1500f, 260f);
            var credit = NLUi.Label(card, "Credit", "Project 3 Capstone · VTC Academy · 2026", 22f, new Color(1f, 1f, 1f, 0.7f), _font, FontStyles.Normal, TextAlignmentOptions.Center);
            credit.rectTransform.anchorMin = credit.rectTransform.anchorMax = new Vector2(0.5f, 0.17f);
            credit.rectTransform.sizeDelta = new Vector2(1200f, 40f);
            _fade.transform.SetAsLastSibling();
            card.SetAsLastSibling();
            SetLayer(card, LayerMask.NameToLayer("UI"));
            return card;
        }

        private static void SetLayer(Transform root, int layer)
        {
            if (layer < 0) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        private static void Logo(RectTransform parent, string name, Vector2 position, float width)
        {
            var texture = Resources.Load<Texture2D>("Trailer/trailer_logo");
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = position;
            if (texture != null)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                image.rectTransform.sizeDelta = new Vector2(width, width * texture.height / texture.width);
            }
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void Black(float alpha) => _fade.color = new Color(0f, 0f, 0f, alpha);

        private IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                Black(Mathf.Lerp(from, to, t));
                yield return null;
            }
            Black(to);
        }

        private IEnumerator Caption(string ja, string vi, float seconds)
        {
            _captionJa.text = ja;
            _captionVi.text = vi;
            yield return Wait(0.45f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
            {
                _captionGroup.alpha = t;
                _caption.anchoredPosition = new Vector2(0f, Mathf.Lerp(-10f, 0f, t));
                yield return null;
            }
            yield return Wait(Mathf.Max(0.5f, seconds - 1.8f));
            for (float t = 1f; t > 0f; t -= Time.deltaTime / 0.35f) { _captionGroup.alpha = t; yield return null; }
            _captionGroup.alpha = 0f;
        }
    }
}
