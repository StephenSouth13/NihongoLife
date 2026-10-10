using System;
using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>The end of the Midori pier: F (with a fishing rod in the bag) starts a fishing round.</summary>
    public sealed class FishingSpot : MonoBehaviour, IInteractable
    {
        [Tooltip("Where the float lands on the water.")] public Transform castTarget;

        public string GetPromptJa() => IslandLanguage.Primary(IslandCatalog.Load().places?.pier) is { Length: > 0 } p ? p : "つりば";
        public string GetpromptEn() => IslandEconomy.Owned(IslandFishing.RodId) > 0 ? "Câu cá" : "Câu cá (cần có cần câu)";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => IslandFishing.Begin(this, player);
    }

    /// <summary>
    /// One fishing round at a FishingSpot: cast (rod in the right hand, float flies onto the water) → wait for a bite →
    /// pull while the float is down (F or the button) → the fish is shown, then added to the bag once. Pulling too early,
    /// missing the bite window, Esc or a full bag give nothing. Fish, prices and odds come from island_catalog.json; the
    /// catch goes to the shared PlayerInventory and is sold at the Midori Store like produce.
    /// </summary>
    public sealed class IslandFishing : MonoBehaviour
    {
        public const string RodId = "tool_fishing_rod";
        public enum Phase { None, Casting, Waiting, Bite, Reeling, Showing }

        // Timing (seconds). Tests may shorten the wait and force the fish.
        public static float CastSeconds = 2.4f, LaunchAt = 1.2f, BiteWindow = 1.8f, ReelSeconds = 1.3f, ShowSeconds = 2.2f;
        public static float? BiteDelayOverride;
        public static string ForcedFishId;

        private static IslandFishing _instance;
        public static Phase Current => _instance != null ? _instance._phase : Phase.None;
        public static bool Active => Current != Phase.None;
        public static event Action<IslandFish> Caught;

        private Phase _phase;
        private FishingSpot _spot;
        private PlayerController _player;
        private CharacterAnimationController _animation;
        private GameObject _float, _shownFish;
        private LineRenderer _line;
        private bool _rewarded;
        private Coroutine _routine;
        private RectTransform _card;
        private TextMeshProUGUI _title, _detail;
        private Button _pullButton;
        private TextMeshProUGUI _pullLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; BiteDelayOverride = null; ForcedFishId = null; Caught = null; }

        /// <summary>Starts a round; returns an error message (shown to the player) or null.</summary>
        public static string Begin(FishingSpot spot, GameObject playerObject)
        {
            if (Active) { Pull(); return null; }
            if (TimedAction.Busy) return "Đang làm việc khác.";
            if (IslandEconomy.Owned(RodId) <= 0)
            {
                var rod = IslandCatalog.Load().Tool(RodId);
                string msg = $"Cần có {IslandLanguage.Primary(rod?.word)} (cần câu) — mua ở cửa hàng Midori (¥{rod?.price}).";
                IslandUI.Toast(msg, true);
                return msg;
            }
            var player = playerObject != null ? playerObject.GetComponentInParent<PlayerController>() : FindFirstObjectByType<PlayerController>();
            if (player == null) return "Không tìm thấy người chơi.";
            var host = Instance;
            host._spot = spot;
            host._player = player;
            host._animation = player.GetComponentInChildren<CharacterAnimationController>();
            host._rewarded = false;
            host._routine = host.StartCoroutine(host.Round());
            return null;
        }

        /// <summary>F / the button: pull the line. Only a pull while the float is down lands the fish.</summary>
        public static void Pull()
        {
            if (_instance == null) return;
            if (_instance._phase == Phase.Bite) _instance._phase = Phase.Reeling;
            else if (_instance._phase == Phase.Waiting) _instance.EndEarly("Chưa có cá cắn câu — kéo lên quá sớm.");
        }

        public static void Cancel()
        {
            if (_instance == null || !Active) return;
            _instance.EndEarly("Đã thu dây — không câu nữa.");
        }

        private static IslandFishing Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("FishingCanvas", 152);
                var ui = canvas.gameObject.AddComponent<IslandFishing>();
                var font = NLUi.ResolveFont();
                ui._card = NLUi.Panel(canvas.transform, "FishingCard", new Color(0.04f, 0.055f, 0.07f, 0.94f), new RectOffset(22, 22, 12, 14), 8f);
                NLUi.Anchor(ui._card, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(560f, 0f));
                ui._card.pivot = new Vector2(0.5f, 0f);
                NLUi.FitContent(ui._card);
                ui._title = NLUi.Label(ui._card, "Title", "", 20f, NLUi.Text, font, FontStyles.Bold);
                ui._detail = NLUi.Label(ui._card, "Detail", "", 15f, NLUi.Muted, font);
                ui._pullButton = NLUi.Button(ui._card, "Pull", "Kéo cần  [F]", font, Pull, NLUi.Gold, 19f, NLUi.Ink, 46f);
                ui._pullLabel = ui._pullButton.GetComponentInChildren<TextMeshProUGUI>();
                NLUi.Label(ui._card, "Hint", "Esc để thu dây", 13f, NLUi.Muted, font);
                ui._card.gameObject.SetActive(false);
                UiModalStack.Register(ui, () => Active, Cancel, "Fishing");
                _instance = ui;
                return ui;
            }
        }

        private void Update()
        {
            if (!Active) return;
            if ((_phase == Phase.Waiting || _phase == Phase.Bite) && GameInputService.Instance != null && GameInputService.Instance.WasPressed(GameInputId.Interact)) Pull();
            if (_line != null && _float != null && _animation != null && _animation.HeldProp != null)
            {
                var tip = _animation.HeldProp.transform.Find("Tip");
                _line.SetPosition(0, tip != null ? tip.position : _animation.HeldProp.transform.position);
                _line.SetPosition(1, _float.transform.position + Vector3.up * 0.05f);
            }
        }

        private IEnumerator Round()
        {
            var catalog = IslandCatalog.Load();
            _player.InputLocked = true;
            FaceWater();
            // Frame the cast: behind the shoulder, looking out over the water.
            var follow = FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (follow != null) follow.SetOrbit(_player.transform.eulerAngles.y - 28f, 14f, 4.2f);
            Show("Đang quăng câu…", IslandLanguage.Primary(catalog.Tool(RodId)?.verb), false);

            // Cast.
            _phase = Phase.Casting;
            bool posed = _animation != null && _animation.PlayWork("Fish_Cast");
            if (posed) _animation.HoldProp(IslandUI.HeldTool(RodId));
            yield return new WaitForSeconds(LaunchAt);
            SpawnFloat();
            Vector3 from = _float.transform.position, to = WaterPoint();
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                if (_phase != Phase.Casting) yield break;
                float k = t / 0.7f;
                _float.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
                yield return null;
            }
            _float.transform.position = to;
            yield return new WaitForSeconds(Mathf.Max(0f, CastSeconds - LaunchAt - 0.7f));
            if (_phase != Phase.Casting) yield break;

            // Wait for a bite.
            _phase = Phase.Waiting;
            if (posed) _animation.PlayWork("Fish_Wait");
            Show("Chờ cá cắn câu…", "Đừng kéo vội — đợi phao chìm xuống.", true, "Kéo cần  [F]");
            float delay = BiteDelayOverride ?? UnityEngine.Random.Range(2.5f, 6f);
            for (float t = 0f; t < delay; t += Time.deltaTime)
            {
                if (_phase != Phase.Waiting) yield break;
                Bob(to, t, 0.03f);
                yield return null;
            }

            // Bite: the float goes under; pull within the window.
            _phase = Phase.Bite;
            Show("！ Cá cắn câu — kéo ngay!", "Bấm F hoặc nút bên dưới.", true, "KÉO!  [F]");
            for (float t = 0f; t < BiteWindow && _phase == Phase.Bite; t += Time.deltaTime)
            {
                _float.transform.position = to + Vector3.down * (0.12f + 0.05f * Mathf.Sin(t * 22f));
                yield return null;
            }
            if (_phase == Phase.Bite) { EndEarly("Cá đã thoát… Lần sau kéo nhanh hơn nhé."); yield break; }
            if (_phase != Phase.Reeling) yield break;

            // Reel: lift the rod, the fish comes out of the water.
            var fish = PickFish(catalog);
            Show("Đang kéo cá lên…", "", false);
            if (posed) _animation.PlayWork("Fish_Cast", 0.15f);
            _shownFish = SpawnFish(fish);
            Vector3 start = to, end = ShowPoint();
            for (float t = 0f; t < ReelSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / ReelSeconds);
                if (_shownFish != null) _shownFish.transform.position = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                if (_float != null) _float.transform.position = _shownFish != null ? _shownFish.transform.position : end;
                yield return null;
            }

            // Land it — exactly once per round.
            _phase = Phase.Showing;
            if (posed) _animation.PlayWork("Work_Hold");
            if (_float != null) { Destroy(_float); _float = null; }
            if (_line != null) { Destroy(_line.gameObject); _line = null; }
            string result = Reward(fish);
            Show(result == null ? $"Câu được {IslandLanguage.Primary(fish.word)}!" : "Thả cá về biển", result ?? $"{fish.word.vi} · bán ¥{fish.sellPrice} ở cửa hàng Midori", false);
            for (float t = 0f; t < ShowSeconds; t += Time.deltaTime)
            {
                if (_shownFish != null) _shownFish.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }
            Finish();
        }

        private string Reward(IslandFish fish)
        {
            if (_rewarded || fish == null) return "Không có cá.";
            _rewarded = true;
            if (!IslandEconomy.CanStore(fish.id)) { IslandUI.Toast("Balo đầy — đã thả cá về biển.", true); return "Balo đầy — không giữ được cá."; }
            if (!IslandEconomy.Give(fish.id, 1)) return "Không cất được cá vào balo.";
            var record = IslandState.Record;
            record.fished++;
            IslandState.Save();
            IslandAchievements.Check();
            IslandState.Discover("fish:" + fish.id);
            IslandUI.WordToast(fish.word, "Câu được");
            NihongoLife.Progression.QuestService.Raise("fish", fish.id);
            HudFeed.Post($"Câu được {fish.word.ja} ({fish.word.vi}) — đã vào balo.", HudFeed.Kind.Reward, 4f);
            Caught?.Invoke(fish);
            return null;
        }

        private static IslandFish PickFish(IslandCatalog catalog)
        {
            var all = catalog.fish ?? Array.Empty<IslandFish>();
            if (all.Length == 0) return null;
            if (!string.IsNullOrEmpty(ForcedFishId)) return catalog.Fish(ForcedFishId) ?? all[0];
            int total = all.Sum(f => Mathf.Max(1, f.weight));
            int roll = UnityEngine.Random.Range(0, total);
            foreach (var f in all) { roll -= Mathf.Max(1, f.weight); if (roll < 0) return f; }
            return all[0];
        }

        private void EndEarly(string message)
        {
            if (_routine != null) StopCoroutine(_routine);
            HudFeed.Post(message, HudFeed.Kind.Warning, 3.5f);
            Finish();
        }

        private void Finish()
        {
            _routine = null;
            _phase = Phase.None;
            if (_animation != null) _animation.StopWork();
            if (_float != null) Destroy(_float);
            if (_shownFish != null) Destroy(_shownFish);
            if (_line != null) Destroy(_line.gameObject);
            _float = _shownFish = null; _line = null;
            if (_player != null) _player.InputLocked = false;
            _card.gameObject.SetActive(false);
        }

        // ─────────── visuals ───────────

        private void Show(string title, string detail, bool canPull, string pullLabel = null)
        {
            _title.text = title;
            _detail.text = detail ?? "";
            _pullButton.gameObject.SetActive(canPull);
            if (pullLabel != null) _pullLabel.text = pullLabel;
            _card.gameObject.SetActive(true);
            _card.SetAsLastSibling();
        }

        private void FaceWater()
        {
            Vector3 dir = WaterPoint() - _player.transform.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) _player.transform.rotation = Quaternion.LookRotation(dir);
        }

        private Vector3 WaterPoint()
        {
            if (_spot != null && _spot.castTarget != null) return _spot.castTarget.position;
            return _player.transform.position + _player.transform.forward * 7f + Vector3.down * 0.5f;
        }

        private Vector3 ShowPoint() => _player.transform.position + _player.transform.forward * 0.9f + Vector3.up * 1.25f;

        private void SpawnFloat()
        {
            var prefab = IslandModelLibrary.Instance != null ? IslandModelLibrary.Instance.Find("Bobber") : null;
            _float = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _float.name = "FishingFloat";
            foreach (var c in _float.GetComponentsInChildren<Collider>(true)) Destroy(c);
            if (prefab == null) _float.transform.localScale = Vector3.one * 0.12f;
            var tip = _animation != null && _animation.HeldProp != null ? _animation.HeldProp.transform.Find("Tip") : null;
            _float.transform.position = tip != null ? tip.position : _player.transform.position + Vector3.up * 2f;

            _line = new GameObject("FishingLine").AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.widthMultiplier = 0.008f;
            _line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default")) { color = new Color(0.95f, 0.95f, 0.92f) };
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Bob(Vector3 rest, float t, float amount) { if (_float != null) _float.transform.position = rest + Vector3.up * Mathf.Sin(t * 3.2f) * amount; }

        private static GameObject SpawnFish(IslandFish fish)
        {
            var prefab = fish != null && IslandModelLibrary.Instance != null ? IslandModelLibrary.Instance.Find("Fish_" + fish.id) : null;
            if (prefab == null) return null;
            var go = Instantiate(prefab);
            go.name = "CaughtFish_" + fish.id;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            go.transform.rotation = Quaternion.Euler(0f, 90f, -10f);
            return go;
        }
    }
}
