using System;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// The set rod (AFK rod): bought at the Midori Store, then planted on the shore beside a fishing spot. It fishes by itself;
    /// when a fish bites the float goes under and a red marker shows - press F at the rod to pull it up (the fish is lost if
    /// nobody does within <see cref="BiteWindow"/> seconds). It re-arms after each catch and wears one use per fish.
    /// Needs fishing level 2 (<see cref="FishingSkill"/>): the skill is learned, not given.
    /// </summary>
    public sealed class IslandAfkRod : MonoBehaviour, IInteractable, IConditionalInteractable
    {
        public const string ToolId = "tool_afk_rod";
        public enum State { Empty, Waiting, Bite }

        /// <summary>Seconds a set rod waits for a bite (before the skill scale). Tests may override.</summary>
        public static float BiteMin = 35f, BiteMax = 70f, BiteWindow = 25f;
        public static float? BiteDelayOverride;
        public static event Action<IslandFish> Caught;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { BiteDelayOverride = null; Caught = null; BiteMin = 35f; BiteMax = 70f; BiteWindow = 25f; }

        public State Current { get; private set; } = State.Empty;
        public int CaughtHere { get; private set; }
        public FishingSpot Spot => _spot;
        public bool IsInteractionAvailable => true;

        private FishingSpot _spot;
        private GameObject _pole, _float, _mark;
        private LineRenderer _line;
        private Vector3 _floatRest;
        private float _biteAt, _escapeAt, _phase;

        public static IslandAfkRod EnsureFor(FishingSpot spot)
        {
            if (spot == null) return null;
            var existing = spot.GetComponentInChildren<IslandAfkRod>(true);
            if (existing != null) return existing;
            var go = new GameObject("AfkRodStand");
            go.transform.SetParent(spot.transform, false);
            go.transform.localPosition = new Vector3(2.0f, 0.2f, 0.4f);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.9f;
            var rod = go.AddComponent<IslandAfkRod>();
            rod._spot = spot;
            return rod;
        }

        public string GetPromptJa() => Current == State.Bite ? "引き上げる" : Current == State.Waiting ? "置き竿を回収" : "置き竿を置く";

        public string GetpromptEn()
        {
            switch (Current)
            {
                case State.Bite: return Loc.T("Kéo cần câu gác lên!", "Pull up the set rod!", "置き竿を引き上げる!");
                case State.Waiting: return Loc.T("Cần câu gác: đang chờ cá (F: thu cần)", "Set rod: waiting for a bite (F: take it back)", "置き竿: 待機中 (F: 回収)");
                default:
                    if (IslandEconomy.Owned(ToolId) <= 0) return Loc.T("Cần câu gác (cần mua ở cửa hàng Midori)", "Set rod (buy one at the Midori Store)", "置き竿 (ミドリストアで購入)");
                    if (FishingSkill.Level < FishingSkill.AfkRodLevel) return Loc.F("Cần câu gác (cần kỹ năng câu cá cấp {0})", "Set rod (needs fishing skill level {0})", "置き竿 (釣りスキルLv{0}が必要)", FishingSkill.AfkRodLevel);
                    return Loc.T("Đặt cần câu gác", "Plant the set rod", "置き竿を置く");
            }
        }

        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (IslandVisit.ReadOnlyNotice()) return;
            switch (Current)
            {
                case State.Empty: Plant(); break;
                case State.Waiting: TakeBack(Loc.T("Đã thu cần câu gác.", "You took the set rod back.", "置き竿を回収した。")); break;
                case State.Bite: PullUp(); break;
            }
        }

        private float NextDelay()
        {
            float d = BiteDelayOverride ?? UnityEngine.Random.Range(BiteMin, BiteMax);
            return BiteDelayOverride.HasValue ? d : d * FishingSkill.BiteTimeScale(FishingSkill.Level);
        }

        private bool Plant()
        {
            if (IslandEconomy.Owned(ToolId) <= 0 || !IslandTools.Usable(ToolId))
            {
                var tool = IslandCatalog.Load().Tool(ToolId);
                IslandUI.Toast(Loc.F("Cần có {0} - mua ở cửa hàng Midori ({1}).", "You need a {0} - buy one at the Midori Store ({1}).", "{0}が必要です - ミドリストアで購入 ({1})。", IslandLanguage.Primary(tool?.word), Loc.Yen(tool?.price ?? 0)), true);
                return false;
            }
            if (FishingSkill.Level < FishingSkill.AfkRodLevel)
            {
                IslandUI.Toast(Loc.F("Kỹ năng câu cá cấp {0} mới dùng được cần câu gác - hãy câu thêm bằng cần thường ({1}/{2} con).", "The set rod needs fishing skill level {0} - keep fishing with the basic rod ({1}/{2} fish).", "置き竿には釣りスキルLv{0}が必要です - 普通の竿でもっと釣ろう ({1}/{2}匹)。", FishingSkill.AfkRodLevel, IslandState.Record.fished, FishingSkill.Thresholds[FishingSkill.AfkRodLevel - 1]), true);
                return false;
            }
            Build();
            Current = State.Waiting;
            _biteAt = Time.time + NextDelay();
            HudFeed.Post(Loc.T("Đã đặt cần câu gác - cứ đi làm việc khác, khi có cá cắn sẽ có dấu đỏ.", "Set rod planted - go about your business; a red marker shows when a fish bites.", "置き竿を置いた - 魚が掛かると赤い印が出る。"), HudFeed.Kind.Info, 4f);
            return true;
        }

        private void TakeBack(string message)
        {
            Clear();
            Current = State.Empty;
            if (!string.IsNullOrEmpty(message)) HudFeed.Post(message, HudFeed.Kind.Info, 3f);
        }

        private void PullUp()
        {
            var catalog = IslandCatalog.Load();
            var fish = IslandFishing.PickFish(catalog);
            string failure = IslandFishing.AwardFish(fish);
            string broke = failure == null ? IslandTools.Wear(ToolId) : null;   // one use per fish; Wear announces a broken rod itself
            if (failure == null) { CaughtHere++; Caught?.Invoke(fish); }
            else HudFeed.Post(failure, HudFeed.Kind.Warning, 3.5f);
            if (IslandEconomy.Owned(ToolId) > 0 && IslandTools.Usable(ToolId) && (broke == null || IslandEconomy.Owned(ToolId) > 0)) Rearm();
            else TakeBack(null);
        }

        private void Rearm()
        {
            SetMark(false);
            Current = State.Waiting;
            _biteAt = Time.time + NextDelay();
        }

        private void Update()
        {
            if (Current == State.Empty) return;
            _phase += Time.deltaTime;
            if (_float != null)
            {
                float bob = Current == State.Bite ? -0.1f - 0.05f * Mathf.Sin(_phase * 22f) : Mathf.Sin(_phase * 3.2f) * 0.03f;
                _float.transform.position = _floatRest + Vector3.up * bob;
            }
            if (_line != null && _pole != null && _float != null)
            {
                _line.SetPosition(0, _pole.transform.position + _pole.transform.up * 0.8f);
                _line.SetPosition(1, _float.transform.position + Vector3.up * 0.05f);
            }
            if (_mark != null) _mark.transform.position = _floatRest + Vector3.up * (1.0f + 0.1f * Mathf.Sin(_phase * 6f));
            if (Current == State.Waiting && Time.time >= _biteAt)
            {
                Current = State.Bite;
                _escapeAt = Time.time + BiteWindow;
                SetMark(true);
                HudFeed.Post(Loc.T("Cần câu gác có cá cắn! Đến bấm F để kéo lên.", "A fish is on the set rod! Go press F to pull it up.", "置き竿に魚が掛かった! Fで引き上げよう。"), HudFeed.Kind.Reward, 4f);
            }
            else if (Current == State.Bite && Time.time >= _escapeAt)
            {
                HudFeed.Post(Loc.T("Cá đã thoát khỏi cần câu gác.", "The fish got away from the set rod.", "魚が置き竿から逃げた。"), HudFeed.Kind.Warning, 3f);
                Rearm();
            }
        }

        // ───── visuals ─────

        private void Build()
        {
            Clear();
            Vector3 right = _spot != null ? _spot.transform.right : Vector3.right;
            Vector3 forward = _spot != null && _spot.castTarget != null ? Vector3.ProjectOnPlane(_spot.castTarget.position - transform.position, Vector3.up).normalized : (_spot != null ? _spot.transform.forward : Vector3.forward);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 water = _spot != null && _spot.castTarget != null ? _spot.castTarget.position + right * 2f : transform.position + forward * 6f;
            _floatRest = new Vector3(water.x, _spot != null && _spot.castTarget != null ? _spot.castTarget.position.y : transform.position.y - 0.5f, water.z);

            _pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _pole.name = "AfkRodPole";
            Destroy(_pole.GetComponent<Collider>());
            _pole.transform.SetParent(transform, false);
            _pole.transform.localScale = new Vector3(0.04f, 0.8f, 0.04f);
            _pole.transform.position = transform.position + Vector3.up * 0.35f;
            _pole.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(55f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            Tint(_pole, new Color(0.45f, 0.3f, 0.15f));
            _pole.transform.position = transform.position + Vector3.up * 0.2f;

            _float = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _float.name = "AfkRodFloat";
            Destroy(_float.GetComponent<Collider>());
            _float.transform.SetParent(transform, true);
            _float.transform.localScale = Vector3.one * 0.14f;
            _float.transform.position = _floatRest;
            Tint(_float, new Color(0.95f, 0.25f, 0.2f));

            _line = new GameObject("AfkRodLine").AddComponent<LineRenderer>();
            _line.transform.SetParent(transform, false);
            _line.positionCount = 2;
            _line.widthMultiplier = 0.008f;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null) _line.material = new Material(shader) { color = new Color(0.95f, 0.95f, 0.92f) };
        }

        private void SetMark(bool on)
        {
            if (on && _mark == null)
            {
                _mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _mark.name = "AfkRodBiteMark";
                Destroy(_mark.GetComponent<Collider>());
                _mark.transform.SetParent(transform, true);
                _mark.transform.localScale = new Vector3(0.12f, 0.45f, 0.12f);
                Tint(_mark, new Color(1f, 0.15f, 0.1f));
            }
            if (_mark != null) _mark.SetActive(on);
        }

        private static void Tint(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Clear()
        {
            if (_pole != null) Destroy(_pole);
            if (_float != null) Destroy(_float);
            if (_mark != null) Destroy(_mark);
            if (_line != null) Destroy(_line.gameObject);
            _pole = _float = _mark = null; _line = null;
        }

        private void OnDestroy() => Clear();
    }
}