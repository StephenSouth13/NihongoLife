using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// Buying, selling and starter items on the island. Uses the one existing wallet and bag (PlayerInventory):
    /// there is no separate farm currency. Every operation is synchronous and checks the bag before taking money,
    /// so repeated clicks cannot double-charge or duplicate items.
    /// </summary>
    public static class IslandEconomy
    {
        public enum Result { Ok, NoMoney, BagFull, NotOwned, Unknown }

        public static int Owned(string itemId) => PlayerInventory.Instance != null ? PlayerInventory.Instance.GetItemQuantity(itemId) : 0;

        public static int Yen => PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;

        public static bool CanStore(string itemId)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return false;
            return inventory.HasItem(itemId) || inventory.Items.Count < inventory.MaxSlots;
        }

        public static bool Give(string itemId, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            var catalog = IslandCatalog.Load();
            if (inventory == null || !catalog.TryDescribe(itemId, out string ja, out string vi, out int price)) return false;
            return inventory.AddItem(itemId, ja, vi, price, quantity);
        }

        public static Result Buy(string itemId, int unitPrice, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || quantity <= 0) return Result.Unknown;
            if (!CanStore(itemId)) return Result.BagFull;
            int total = unitPrice * quantity;
            if (!inventory.SpendYen(total)) return Result.NoMoney;
            if (!Give(itemId, quantity)) { inventory.AddYen(total); return Result.BagFull; }
            return Result.Ok;
        }

        public static Result Sell(string itemId, int unitPrice, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || quantity <= 0) return Result.Unknown;
            if (inventory.GetItemQuantity(itemId) < quantity) return Result.NotOwned;
            if (!inventory.RemoveItem(itemId, quantity)) return Result.NotOwned;
            inventory.AddYen(unitPrice * quantity);
            var record = IslandState.Record;
            record.sold += quantity;
            record.earned += unitPrice * quantity;
            IslandState.Save();
            if (record.sold >= 1) IslandAchievements.Check();
            return Result.Ok;
        }

        /// <summary>First arrival: a hoe, a watering can and three carrot seeds (data-driven, given once).</summary>
        public static bool GiveStarterKit()
        {
            var record = IslandState.Record;
            if (record.starterKitGiven) return false;
            var catalog = IslandCatalog.Load();
            foreach (var item in catalog.starterKit ?? Array.Empty<IslandKitItem>())
                if (Owned(item.itemId) == 0 || item.itemId.StartsWith("seed_")) Give(item.itemId, item.quantity);
            record.starterKitGiven = true;
            IslandState.Save();
            return true;
        }
    }

    public static class IslandAchievements
    {
        public sealed class Def { public string Id, Vi, Ja; public Func<IslandRecord, bool> Done; }

        public static readonly Def[] All =
        {
            new Def { Id = "first_harvest", Ja = "はじめての しゅうかく", Vi = "Lần thu hoạch đầu tiên", Done = r => r.harvested >= 1 },
            new Def { Id = "farmer", Ja = "のうか デビュー", Vi = "Thu hoạch 10 lần", Done = r => r.harvested >= 10 },
            new Def { Id = "first_sale", Ja = "はじめての うりあげ", Vi = "Bán nông sản lần đầu", Done = r => r.sold >= 1 },
            new Def { Id = "animal_friend", Ja = "どうぶつの ともだち", Vi = "Làm quen cả 5 con vật", Done = r => r.animalsMet.Count >= 5 },
            new Def { Id = "word_collector", Ja = "ことば あつめ", Vi = "Học 15 từ trên đảo", Done = r => r.words.Count >= 15 },
        };

        public static event Action<Def> Unlocked;

        public static void Check()
        {
            var record = IslandState.Record;
            foreach (var def in All)
                if (def.Done(record) && IslandState.Unlock(def.Id)) Unlocked?.Invoke(def);
        }
    }

    // ─────────── Farm plot ───────────

    /// <summary>
    /// One plot of the field. States (saved in FarmPlotRecord, so they survive leaving the island):
    /// untilled → tilled (hoe) → seed planted → watered → grows for the crop's secondsPerStage → needs water again →
    /// … → ready (stage 3) → harvested (soil must be tilled again). The shovel clears a plot.
    /// Growth is computed from saved UTC timestamps, so crops keep growing while the player is away.
    /// </summary>
    public sealed class FarmPlot : MonoBehaviour, IInteractable, IInteractionPriority
    {
        public enum Phase { Untilled, Tilled, NeedsWater, Growing, Ready }

        [SerializeField] private string plotId = "plot_1";
        [SerializeField] private int number = 1;
        [SerializeField] private Renderer soil;
        [SerializeField] private Transform cropAnchor;
        [SerializeField] private TextMeshPro marker;

        private GameObject _cropModel;
        private string _shownModel;
        private float _nextTick;
        private static readonly Color GrassSoil = new Color(0.45f, 0.52f, 0.28f);
        private static readonly Color DrySoil = new Color(0.55f, 0.38f, 0.24f);
        private static readonly Color WetSoil = new Color(0.33f, 0.22f, 0.15f);
        private MaterialPropertyBlock _block;

        public string PlotId => plotId;
        public int Number => number;
        public FarmPlotRecord Record => IslandState.Plot(plotId);
        public IslandCrop Crop => string.IsNullOrEmpty(Record.cropId) ? null : IslandCatalog.Load().Crop(Record.cropId);

        public void Configure(string id, int plotNumber, Renderer soilRenderer, Transform anchor, TextMeshPro statusMarker)
        {
            plotId = id; number = plotNumber; soil = soilRenderer; cropAnchor = anchor; marker = statusMarker;
        }

        public Phase CurrentPhase
        {
            get
            {
                Advance();
                var r = Record;
                if (!r.tilled) return Phase.Untilled;
                if (string.IsNullOrEmpty(r.cropId)) return Phase.Tilled;
                if (r.stage >= 3) return Phase.Ready;
                return r.watered ? Phase.Growing : Phase.NeedsWater;
            }
        }

        /// <summary>0..1 progress of the current growing stage.</summary>
        public float StageProgress
        {
            get
            {
                var r = Record; var crop = Crop;
                if (crop == null || !r.watered || r.stage >= 3) return r.stage >= 3 ? 1f : 0f;
                double seconds = (IslandState.UtcNow - new DateTime(r.stageStartTicks, DateTimeKind.Utc)).TotalSeconds;
                return Mathf.Clamp01((float)(seconds / Mathf.Max(1f, crop.secondsPerStage)));
            }
        }

        public float SecondsLeft
        {
            get { var crop = Crop; return crop == null ? 0f : (1f - StageProgress) * crop.secondsPerStage; }
        }

        /// <summary>Applies growth that happened since the last check (also while the player was away).</summary>
        public void Advance()
        {
            var r = Record; var crop = Crop;
            if (crop == null || !r.watered || r.stage >= 3) return;
            var start = new DateTime(r.stageStartTicks, DateTimeKind.Utc);
            if ((IslandState.UtcNow - start).TotalSeconds < crop.secondsPerStage) return;
            r.stage++;
            r.watered = false;     // each stage needs fresh water (the ready stage does not)
            IslandState.Save();
        }

        // ── Actions (return a short Vietnamese error, or null on success) ──

        public string Till()
        {
            if (Record.tilled) return "Đất đã được xới rồi.";
            if (IslandEconomy.Owned("tool_hoe") == 0) return "Cần cái cuốc (くわ) — mua ở cửa hàng Midori.";
            var r = Record; r.tilled = true; r.cropId = null; r.stage = 0; r.watered = false;
            IslandState.Save(); Refresh(true);
            return null;
        }

        public string Plant(string cropId)
        {
            var crop = IslandCatalog.Load().Crop(cropId);
            if (crop == null) return "Không có loại hạt này.";
            if (!Record.tilled) return "Hãy xới đất bằng cuốc trước.";
            if (!string.IsNullOrEmpty(Record.cropId)) return "Ô này đã có cây.";
            if (!PlayerInventory.Instance.RemoveItem(crop.SeedItemId)) return $"Hết hạt {crop.word.vi} — mua thêm ở cửa hàng.";
            var r = Record; r.cropId = cropId; r.stage = 0; r.watered = false;
            IslandState.Discover("crop:" + cropId);
            IslandState.Save(); Refresh(true);
            return null;
        }

        public string Water()
        {
            var phase = CurrentPhase;
            if (phase == Phase.Untilled || phase == Phase.Tilled) return "Chưa có gì để tưới — hãy gieo hạt trước.";
            if (phase == Phase.Ready) return "Cây đã chín, thu hoạch thôi!";
            if (phase == Phase.Growing) return "Cây vừa được tưới, đợi cây lớn đã.";
            if (IslandEconomy.Owned("tool_watering_can") == 0) return "Cần bình tưới (じょうろ).";
            var r = Record; r.watered = true; r.stageStartTicks = IslandState.UtcNow.Ticks;
            IslandState.Save(); Refresh(true);
            return null;
        }

        public string Harvest(out int amount)
        {
            amount = 0;
            if (CurrentPhase != Phase.Ready) return "Cây chưa chín.";
            var crop = Crop;
            if (!IslandEconomy.CanStore(crop.ProduceItemId)) return "Balo đầy — bán bớt nông sản trước.";
            if (!IslandEconomy.Give(crop.ProduceItemId, crop.yield)) return "Balo đầy — bán bớt nông sản trước.";
            amount = crop.yield;
            var r = Record; r.cropId = null; r.stage = 0; r.watered = false; r.tilled = false;
            var island = IslandState.Record; island.harvested++;
            IslandState.Discover("crop:" + crop.id);
            IslandState.Save(); Refresh(true);
            IslandAchievements.Check();
            return null;
        }

        public string Clear()
        {
            if (IslandEconomy.Owned("tool_shovel") == 0) return "Cần cái xẻng (シャベル).";
            if (string.IsNullOrEmpty(Record.cropId)) return "Ô đất đang trống.";
            var r = Record; r.cropId = null; r.stage = 0; r.watered = false; r.tilled = true;
            IslandState.Save(); Refresh(true);
            return null;
        }

        // ── Interaction ──

        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? $"Field {number}" : $"はたけ {number}";
        public string GetpromptEn() => $"Ô ruộng {number} — {PhaseLabel(CurrentPhase)}";
        public Transform GetTransform() => transform;
        public float InteractionPriority => 0.4f;
        public void Interact(GameObject player) => IslandUI.OpenFarm(this);

        public static string PhaseLabel(Phase phase) => phase switch
        {
            Phase.Untilled => "cần xới đất",
            Phase.Tilled => "sẵn sàng gieo hạt",
            Phase.NeedsWater => "cần tưới nước",
            Phase.Growing => "đang lớn",
            Phase.Ready => "đã chín, thu hoạch được",
            _ => "",
        };

        // ── Visuals ──

        private void Start() => Refresh(true);

        private void Update()
        {
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 0.5f;
            int stageBefore = Record.stage;
            Advance();
            Refresh(Record.stage != stageBefore);
        }

        public void Refresh(bool force)
        {
            var r = Record;
            var phase = CurrentPhase;
            if (soil != null)
            {
                _block ??= new MaterialPropertyBlock();
                soil.GetPropertyBlock(_block);
                Color c = !r.tilled ? GrassSoil : (phase == Phase.Growing ? WetSoil : DrySoil);
                _block.SetColor("_BaseColor", c);
                soil.SetPropertyBlock(_block);
                soil.transform.localScale = new Vector3(soil.transform.localScale.x, r.tilled ? 0.18f : 0.08f, soil.transform.localScale.z);
            }
            string model = null;
            var crop = Crop;
            if (crop != null) model = $"{crop.model}_{Mathf.Clamp(r.stage + 1, 1, 4)}";
            if (force || model != _shownModel)
            {
                if (_cropModel != null) Destroy(_cropModel);
                _cropModel = null;
                _shownModel = model;
                var prefab = model != null ? IslandModelLibrary.Instance?.Find(model) : null;
                if (prefab != null && cropAnchor != null)
                {
                    _cropModel = Instantiate(prefab, cropAnchor, false);
                    _cropModel.name = model;
                    foreach (var collider in _cropModel.GetComponentsInChildren<Collider>()) Destroy(collider);
                }
            }
            if (marker != null)
            {
                marker.text = phase == Phase.NeedsWater ? "<color=#2F7FC0>みず</color>" : phase == Phase.Ready ? "<color=#E0A020>★</color>" : "";
                marker.gameObject.SetActive(marker.text.Length > 0);
            }
        }
    }

    /// <summary>Model prefabs for crop stages, produce and tools (filled by IslandBuilder; no runtime asset lookups).</summary>
    public sealed class IslandModelLibrary : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string name; public GameObject prefab; }
        [SerializeField] private List<Entry> entries = new();
        public static IslandModelLibrary Instance { get; private set; }

        public void Set(List<Entry> list) => entries = list;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public GameObject Find(string name)
        {
            foreach (var e in entries) if (e.name == name) return e.prefab;
            return null;
        }
    }

    // ─────────── Animals ───────────

    /// <summary>
    /// A farm animal that wanders inside its pen (walk/idle clips via the Animator "Speed" parameter), turns to the
    /// player when talked to, and can be fed island produce or petted. Names and phrases follow the target language.
    /// </summary>
    public sealed class IslandAnimal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string animalId = "cow";
        [SerializeField] private Vector3 penCenter;
        [SerializeField] private float penRadius = 4f;
        [SerializeField] private float walkSpeed = 0.9f;

        private Animator _animator;
        private Vector3 _target;
        private float _waitUntil;
        private float _busyUntil;
        private Transform _lookAt;

        public string AnimalId => animalId;
        public IslandAnimalDef Def => IslandCatalog.Load().Animal(animalId);

        public void Configure(string id, Vector3 center, float radius) { animalId = id; penCenter = center; penRadius = radius; }

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _target = transform.position;
            _waitUntil = Time.time + UnityEngine.Random.Range(0.5f, 3f);
        }

        private void Update()
        {
            if (Time.time < _busyUntil)
            {
                SetSpeed(0f);
                if (_lookAt != null) Face(_lookAt.position, 4f);
                return;
            }
            Vector3 to = _target - transform.position; to.y = 0f;
            if (to.magnitude < 0.25f)
            {
                SetSpeed(0f);
                if (Time.time >= _waitUntil)
                {
                    Vector2 r = UnityEngine.Random.insideUnitCircle * penRadius;
                    _target = new Vector3(penCenter.x + r.x, transform.position.y, penCenter.z + r.y);
                    _waitUntil = Time.time + UnityEngine.Random.Range(3f, 8f);
                }
                return;
            }
            Face(_target, 2.5f);
            transform.position += transform.forward * walkSpeed * Time.deltaTime;
            SetSpeed(1f);
        }

        private void Face(Vector3 point, float speed)
        {
            Vector3 d = point - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), speed * Time.deltaTime);
        }

        private void SetSpeed(float s)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null) _animator.SetFloat("Speed", s, 0.15f, Time.deltaTime);
        }

        public void Hold(Transform player, float seconds)
        {
            _lookAt = player;
            _busyUntil = Time.time + seconds;
        }

        public string Feed(out string food)
        {
            food = null;
            var def = Def;
            if (def.foods == null || def.foods.Length == 0) return "Bé này không ăn nông sản — hãy vuốt ve nhé.";
            foreach (string f in def.foods)
                if (IslandEconomy.Owned(f) > 0) { food = f; break; }
            if (food == null) return $"Cần {string.Join(" hoặc ", def.foods.Select(f => IslandCatalog.Load().Crop(f)?.word.vi ?? f))} để cho ăn — thu hoạch ở ruộng nhé.";
            PlayerInventory.Instance.RemoveItem(food);
            if (_animator != null) _animator.SetTrigger("Eat");
            var record = IslandState.Record; record.fed++;
            Meet();
            IslandState.Save();
            return null;
        }

        public void Pet()
        {
            if (_animator != null) _animator.SetTrigger("Jump");
            Meet();
        }

        public void Meet()
        {
            var record = IslandState.Record;
            if (!record.animalsMet.Contains(animalId)) { record.animalsMet.Add(animalId); IslandState.Save(); }
            IslandState.Discover("animal:" + animalId);
            IslandAchievements.Check();
        }

        public string GetPromptJa() => IslandLanguage.Primary(Def?.word);
        public string GetpromptEn() => $"Làm quen với {Def?.word.vi}";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            Hold(player.transform, 6f);
            Meet();
            IslandUI.OpenAnimal(this);
        }
    }

    /// <summary>The Midori Store counter (shop with Agriculture / Fashion / Technology / Sell).</summary>
    public sealed class IslandShopCounter : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? "Midori Store" : "みどりしょうてん";
        public string GetpromptEn() => "Mở cửa hàng Midori (P)";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => IslandUI.OpenShop();
    }

    /// <summary>A sign or landmark that teaches its name once (station, field, barn, lookout…).</summary>
    public sealed class IslandWordSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private string placeId = "farm";
        public void Configure(string id) => placeId = id;
        private IslandWord Word
        {
            get
            {
                var p = IslandCatalog.Load().places;
                return placeId switch { "station" => p.station, "farm" => p.farm, "shop" => p.shop, "barn" => p.barn, "view" => p.view, _ => p.island };
            }
        }
        public string GetPromptJa() => IslandLanguage.Primary(Word);
        public string GetpromptEn() => "Học tên địa điểm";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            if (IslandState.Discover("place:" + placeId)) IslandUI.WordToast(Word, "Địa điểm mới");
            else IslandUI.WordToast(Word, "Địa điểm");
            IslandAchievements.Check();
        }
    }
}
