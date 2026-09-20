using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.UI;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// A dining table with the full restaurant loop: open the menu and choose quantities, the staff
    /// repeats the order in Japanese, the dishes are served on the table, the player eats
    /// (いただきます, restores hunger/thirst) and finally pays (お会計). Everything shown or said comes
    /// from the RestaurantMenuDefinition at <see cref="menuResourcePath"/>, so tables in other
    /// restaurants only need a different path. Put the component on a GameObject with a trigger
    /// collider and (optionally) a child Transform used as <see cref="serveAnchor"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RestaurantTable : MonoBehaviour, IInteractable, IConditionalInteractable, IExitBlocker
    {
        public enum ServiceState { Idle, Preparing, Served, Eating, ReadyToPay, Paying }

        private class OrderLine
        {
            public RestaurantDish dish;
            public int quantity;
        }

        public const int MaxDistinctDishes = 6;
        public const int MaxPerDish = 9;
        public const int MaxTotalQuantity = 20;

        [SerializeField] private string menuResourcePath = "Restaurants/menu_sushi_hibari";
        [Tooltip("Where dishes appear. Empty = 0.8 m above this object.")]
        [SerializeField] private Transform serveAnchor;
        [SerializeField, Min(0f)] private float prepBaseSeconds = 3f;
        [SerializeField, Min(0f)] private float prepSecondsPerItem = 0.6f;
        [SerializeField, Min(0.5f)] private float eatSeconds = 4f;
        [SerializeField] private Vector2 slotSpacing = new Vector2(0.26f, 0.28f);

        private RestaurantMenuDefinition _menu;
        private ServiceState _state = ServiceState.Idle;
        private readonly List<OrderLine> _order = new List<OrderLine>();
        private readonly List<GameObject> _served = new List<GameObject>();
        private int _total;

        private static readonly List<RestaurantTable> ActiveTables = new List<RestaurantTable>();
        private float _nextReminderTime;

        public ServiceState State => _state;
        public int BillTotal => _total;

        /// <summary>An order exists that has not been paid yet (including still being prepared or eaten).</summary>
        public bool HasUnpaidBill => _state == ServiceState.Preparing || _state == ServiceState.Served || _state == ServiceState.Eating || _state == ServiceState.ReadyToPay;
        public bool BlocksExit => HasUnpaidBill;

        /// <summary>First table in the loaded scenes that still has an unpaid order, or null.</summary>
        public static RestaurantTable FindUnpaid()
        {
            for (int i = 0; i < ActiveTables.Count; i++)
            {
                if (ActiveTables[i] != null && ActiveTables[i].HasUnpaidBill) return ActiveTables[i];
            }

            return null;
        }

        /// <summary>Short localized status for the bill notification chip.</summary>
        public string GetBillStatus()
        {
            switch (_state)
            {
                case ServiceState.Preparing: return Localize("Món đang được chuẩn bị", "Your food is being prepared", "お料理を準備中です");
                case ServiceState.Served: return Localize("Món đã lên bàn", "Your food is on the table", "お料理が届きました");
                case ServiceState.Eating: return Localize("Đang dùng bữa", "Enjoy your meal", "お食事中です");
                case ServiceState.ReadyToPay: return Localize("Chờ thanh toán tại bàn", "Waiting for payment at the table", "テーブルでお会計をお願いします");
                default: return string.Empty;
            }
        }

        private void OnEnable()
        {
            if (!ActiveTables.Contains(this)) ActiveTables.Add(this);
            ExitGuard.Register(this);
        }

        private void Update()
        {
            if (_state == ServiceState.ReadyToPay && Time.time >= _nextReminderTime)
            {
                _nextReminderTime = Time.time + 30f;
                ShowStaff("bill_reminder", null, null, null, null,
                    "お客様、お会計をお願いします。", "おきゃくさま、おかいけいをおねがいします。",
                    "Quý khách ơi, xin mời thanh toán tại bàn ạ.", 6f);
            }
        }

        public void OnExitBlocked()
        {
            switch (_state)
            {
                case ServiceState.ReadyToPay:
                    ShowStaff("exit_blocked", null, null, null, null,
                        "お客様、お会計がまだです。テーブルでお願いします。", "おきゃくさま、おかいけいがまだです。テーブルでおねがいします。",
                        "Quý khách ơi, quý khách chưa thanh toán ạ. Xin thanh toán tại bàn. (Ở Nhật, rời quán mà không trả tiền - 食い逃げ - là vi phạm pháp luật.)", 8f);
                    break;
                case ServiceState.Preparing:
                    ShowStaff(null, null, null, null, null,
                        "お客様、お料理はもうすぐです。少々お待ちください。", "おきゃくさま、おりょうりはもうすぐです。しょうしょうおまちください。",
                        "Món của quý khách sắp xong rồi ạ. Xin đợi một chút.", 6f);
                    break;
                default:
                    ShowStaff(null, null, null, null, null,
                        "お客様、お料理とお会計がまだです。", "おきゃくさま、おりょうりとおかいけいがまだです。",
                        "Quý khách chưa dùng xong món và chưa thanh toán ạ. Hãy ăn xong (E ở bàn), thanh toán rồi hãy ra về.", 7f);
                    break;
            }
        }

        public bool IsInteractionAvailable => _state == ServiceState.Idle || _state == ServiceState.Served || _state == ServiceState.ReadyToPay;
        public Transform GetTransform() => transform;

        public string GetPromptJa()
        {
            switch (_state)
            {
                case ServiceState.Idle: return "注文する";
                case ServiceState.Served: return "いただきます";
                case ServiceState.ReadyToPay: return $"お会計（{_total:N0}円）";
                default: return "少々お待ちください";
            }
        }

        public string GetpromptEn()
        {
            switch (_state)
            {
                case ServiceState.Idle: return "Xem thực đơn và gọi món";
                case ServiceState.Served: return "Ăn (いただきます)";
                case ServiceState.ReadyToPay: return $"Thanh toán ¥{_total:N0}";
                default: return "Vui lòng đợi món";
            }
        }

        public void Interact(GameObject player)
        {
            if (!EnsureMenu()) return;

            switch (_state)
            {
                case ServiceState.Idle:
                    RestaurantMenuUI.GetOrCreate().Show(_menu, this);
                    break;
                case ServiceState.Served:
                    StartCoroutine(EatRoutine());
                    break;
                case ServiceState.ReadyToPay:
                    Pay();
                    break;
            }
        }

        // ──────────────────────── Ordering ────────────────────────

        /// <summary>Validates the cart, charges nothing yet (payment happens at お会計) and starts preparation.</summary>
        public bool TryPlaceOrder(RestaurantMenuDefinition menu, IReadOnlyList<KeyValuePair<string, int>> lines, out string error)
        {
            error = null;
            if (_state != ServiceState.Idle)
            {
                error = Localize("Bàn này đang có món.", "This table already has an order.", "このテーブルはご注文済みです。");
                return false;
            }

            var built = new List<OrderLine>();
            int total = 0;
            int quantity = 0;
            foreach (var pair in lines)
            {
                var dish = menu.FindDish(pair.Key);
                if (dish == null || pair.Value <= 0) continue;
                built.Add(new OrderLine { dish = dish, quantity = Mathf.Min(pair.Value, MaxPerDish) });
                total += dish.priceYen * Mathf.Min(pair.Value, MaxPerDish);
                quantity += Mathf.Min(pair.Value, MaxPerDish);
            }

            if (built.Count == 0)
            {
                error = Localize("Bạn chưa chọn món nào.", "You have not picked any dish.", "まだ何も選んでいません。");
                return false;
            }

            if (built.Count > MaxDistinctDishes || quantity > MaxTotalQuantity)
            {
                error = Localize("Quá nhiều món cho một bàn.", "Too many dishes for one table.", "一つのテーブルでは多すぎます。");
                return false;
            }

            var inventory = PlayerInventory.Instance;
            if (inventory != null && inventory.Yen < total)
            {
                error = Localize($"Không đủ tiền (cần ¥{total:N0}, có ¥{inventory.Yen:N0}).", $"Not enough money (need ¥{total:N0}, have ¥{inventory.Yen:N0}).", $"お金が足りません（{total:N0}円必要、所持金{inventory.Yen:N0}円）。");
                return false;
            }

            _menu = menu;
            _order.Clear();
            _order.AddRange(built);
            _total = total;
            _state = ServiceState.Preparing;

            var confirm = BuildOrderSentence();
            ShowStaff("order_confirm", "{order}", confirm.ja, confirm.reading, confirm.vi,
                "かしこまりました。{order}ですね。少々お待ちください。", "かしこまりました。{order}ですね。しょうしょうおまちください。",
                "Vâng ạ. {order}, đúng không ạ? Xin quý khách đợi một chút.", 6f);

            StartCoroutine(PrepareRoutine(Mathf.Min(prepBaseSeconds + prepSecondsPerItem * quantity, 12f)));
            return true;
        }

        /// <summary>The order as the staff repeats it, e.g. まぐろ二つ、サーモン一つ.</summary>
        public (string ja, string reading, string vi) BuildOrderSentence()
        {
            var ja = new StringBuilder();
            var reading = new StringBuilder();
            var vi = new StringBuilder();
            for (int i = 0; i < _order.Count; i++)
            {
                var line = _order[i];
                string sep = i == 0 ? string.Empty : "、";
                ja.Append(sep).Append(StripParentheses(line.dish.nameJa)).Append(JapaneseNumber.CountKanji(line.quantity));
                reading.Append(sep).Append(StripParentheses(string.IsNullOrWhiteSpace(line.dish.reading) ? line.dish.nameJa : line.dish.reading)).Append(JapaneseNumber.CountReading(line.quantity));
                vi.Append(i == 0 ? string.Empty : ", ").Append(line.quantity).Append(" x ").Append(StripParentheses(string.IsNullOrWhiteSpace(line.dish.nameVi) ? line.dish.nameJa : line.dish.nameVi));
            }

            return (ja.ToString(), reading.ToString(), vi.ToString());
        }

        private IEnumerator PrepareRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SpawnServedDishes();
            _state = ServiceState.Served;
            ShowStaff("served", null, null, null, null,
                "お待たせしました。ごゆっくりどうぞ。", "おまたせしました。ごゆっくりどうぞ。",
                "Để quý khách đợi lâu rồi. Xin mời dùng bữa thong thả.", 6f);
        }

        // ──────────────────────── Eating and paying ────────────────────────

        private IEnumerator EatRoutine()
        {
            _state = ServiceState.Eating;
            ShowStaff(null, null, null, null, null, "いただきます！", "いただきます！", "Mời cả nhà (câu nói trước khi ăn).", eatSeconds);

            float hunger = 0f;
            float thirst = 0f;
            foreach (var line in _order)
            {
                hunger += line.dish.hungerRestore * line.quantity;
                thirst += line.dish.thirstRestore * line.quantity;
            }

            // Plates disappear one by one while eating.
            float step = _served.Count > 0 ? eatSeconds / _served.Count : eatSeconds;
            for (int i = 0; i < _served.Count; i++)
            {
                yield return new WaitForSeconds(step);
                if (_served[i] != null) Destroy(_served[i]);
            }

            _served.Clear();
            PlayerStatus.Instance?.RestoreNeeds(hunger, thirst, 0f);
            _state = ServiceState.ReadyToPay;
            _nextReminderTime = Time.time + 25f;
            ShowStaff(null, null, null, null, null, "ごちそうさまでした。", "ごちそうさまでした。", "Cảm ơn vì bữa ăn (câu nói sau khi ăn). Bấm E để thanh toán.", 5f);
        }

        private void Pay()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory != null && !inventory.SpendYen(_total))
            {
                ShowStaff(null, null, null, null, null, "すみません、お金が足りないようです。", "すみません、おかねがたりないようです。",
                    $"Xin lỗi, dường như bạn không đủ tiền (cần ¥{_total:N0}). Bạn có thể làm ca ở quầy (JobPoint) để kiếm thêm tiền rồi quay lại thanh toán.", 6f);
                return;
            }

            string totalJa = JapaneseNumber.ToKanji(_total) + "円";
            string totalReading = JapaneseNumber.ToReading(_total) + "えん";
            ShowStaff("bill", "{total}", totalJa, totalReading, $"¥{_total:N0}",
                "お会計は{total}です。ありがとうございました。またお越しください。", "おかいけいは{total}です。ありがとうございました。またおこしください。",
                "Tổng cộng là {total}. Cảm ơn quý khách, xin mời quý khách quay lại lần sau.", 7f);

            _order.Clear();
            _total = 0;

            // Briefly unavailable so the interaction prompt refreshes from お会計 back to 注文する.
            _state = ServiceState.Paying;
            StartCoroutine(FinishPaying());
        }

        private IEnumerator FinishPaying()
        {
            yield return new WaitForSeconds(0.8f);
            _state = ServiceState.Idle;
        }

        // ──────────────────────── Serving visuals ────────────────────────

        private void SpawnServedDishes()
        {
            ClearServed();
            Transform anchor = serveAnchor != null ? serveAnchor : transform;
            Vector3 origin = serveAnchor != null ? Vector3.zero : Vector3.up * 0.8f;

            int columns = 3;
            for (int i = 0; i < _order.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var slot = new GameObject($"Served_{_order[i].dish.id}");
                slot.transform.SetParent(anchor, false);
                slot.transform.localPosition = origin + new Vector3((col - (columns - 1) * 0.5f) * slotSpacing.x, 0f, (row - 0.5f) * slotSpacing.y);
                BuildDishVisual(slot.transform, _order[i]);
                _served.Add(slot);
            }
        }

        private void BuildDishVisual(Transform slot, OrderLine line)
        {
            var dish = line.dish;
            float baseY = slot.position.y;
            if (dish.servedOnPlate)
            {
                if (_menu.plateModel != null)
                {
                    var plate = Instantiate(_menu.plateModel, slot);
                    baseY = FitModel(plate, _menu.plateModelEuler, _menu.plateModelSize, slot.position, out Bounds plateBounds) ? plateBounds.max.y - 0.004f : baseY;
                }
                else
                {
                    var plate = MakePrimitive(PrimitiveType.Cylinder, slot, new Vector3(0.26f, 0.008f, 0.26f), new Color(0.95f, 0.95f, 0.92f));
                    plate.transform.position = slot.position + Vector3.up * 0.008f;
                    baseY = slot.position.y + 0.012f;
                }
            }

            int copies = Mathf.Clamp(line.quantity, 1, 3);
            for (int c = 0; c < copies; c++)
            {
                Vector3 offset = copies == 1 ? Vector3.zero : new Vector3((c - (copies - 1) * 0.5f) * 0.1f, 0f, c % 2 == 0 ? 0.03f : -0.03f);
                Vector3 bottom = new Vector3(slot.position.x + offset.x, baseY, slot.position.z + offset.z);
                if (dish.servedModel != null)
                {
                    var visual = Instantiate(dish.servedModel, slot);
                    FitModel(visual, dish.servedModelEuler, dish.servedModelSize > 0f ? dish.servedModelSize : 0.12f, bottom, out _);
                }
                else
                {
                    var visual = MakePrimitive(PrimitiveType.Sphere, slot, new Vector3(0.09f, 0.05f, 0.09f), new Color(0.85f, 0.62f, 0.3f));
                    visual.transform.position = bottom + Vector3.up * 0.025f;
                }
            }
        }

        /// <summary>
        /// Rotates the model, scales it so its longest edge equals <paramref name="targetSize"/> meters
        /// (independent of the FBX import scale) and puts the bottom centre of its bounds at
        /// <paramref name="bottomCenter"/> in world space.
        /// </summary>
        private static bool FitModel(GameObject model, Vector3 euler, float targetSize, Vector3 bottomCenter, out Bounds fitted)
        {
            fitted = new Bounds(bottomCenter, Vector3.zero);
            model.transform.localRotation = Quaternion.Euler(euler);
            model.transform.localScale = Vector3.one;

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (longest <= 0.0001f) return false;

            model.transform.localScale = Vector3.one * (targetSize / longest);

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 delta = new Vector3(bottomCenter.x - bounds.center.x, bottomCenter.y - bounds.min.y, bottomCenter.z - bounds.center.z);
            model.transform.position += delta;
            bounds.center += delta;
            fitted = bounds;
            return true;
        }

        private static GameObject MakePrimitive(PrimitiveType type, Transform parent, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            Material defaultMaterial = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
            if (renderer != null && defaultMaterial != null)
            {
                var material = new Material(defaultMaterial);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                renderer.sharedMaterial = material;
            }

            return go;
        }

        private void ClearServed()
        {
            foreach (var go in _served)
            {
                if (go != null) Destroy(go);
            }

            _served.Clear();
        }

        private void OnDisable()
        {
            ActiveTables.Remove(this);
            ExitGuard.Unregister(this);

            // A disabled table must not stay stuck mid-service; coroutines stop with the object.
            if (_state == ServiceState.Preparing)
            {
                SpawnServedDishes();
                _state = ServiceState.Served;
            }
            else if (_state == ServiceState.Eating)
            {
                ClearServed();
                _state = ServiceState.ReadyToPay;
            }
            else if (_state == ServiceState.Paying)
            {
                _state = ServiceState.Idle;
            }
        }

        // ──────────────────────── Helpers ────────────────────────

        private bool EnsureMenu()
        {
            if (_menu == null) _menu = Resources.Load<RestaurantMenuDefinition>(menuResourcePath);
            if (_menu == null)
            {
                Debug.LogWarning($"[RestaurantTable] Menu asset not found at Resources/{menuResourcePath}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shows a staff line. Data-driven: if the menu asset has a service line for <paramref name="key"/>
        /// it wins; the fallback strings keep older menu assets working.
        /// </summary>
        private void ShowStaff(string key, string placeholder, string valueJa, string valueReading, string valueVi,
            string fallbackJa, string fallbackReading, string fallbackVi, float seconds)
        {
            var line = _menu != null && key != null ? _menu.GetServiceLine(key) : null;
            string ja = line != null && !string.IsNullOrWhiteSpace(line.ja) ? line.ja : fallbackJa;
            string reading = line != null && !string.IsNullOrWhiteSpace(line.reading) ? line.reading : fallbackReading;
            string vi = line != null && !string.IsNullOrWhiteSpace(line.vi) ? line.vi : fallbackVi;
            string en = line != null && !string.IsNullOrWhiteSpace(line.en) ? line.en : null;

            if (placeholder != null)
            {
                ja = ja.Replace(placeholder, valueJa ?? string.Empty);
                reading = reading.Replace(placeholder, valueReading ?? string.Empty);
                vi = vi.Replace(placeholder, valueVi ?? string.Empty);
                if (en != null) en = en.Replace(placeholder, valueVi ?? string.Empty);
            }

            string speaker = _menu != null && !string.IsNullOrWhiteSpace(_menu.staffNameJa) ? _menu.staffNameJa + "さん" : "店員";
            RestaurantMenuUI.GetOrCreate().ShowStaffLine(speaker, ja, reading, Localize(vi, en ?? vi, ja), seconds);
        }

        private static string Localize(string vi, string en, string ja) => RestaurantMenuUI.Localize(vi, en, ja);

        private static string StripParentheses(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : Regex.Replace(value, "[（(][^）)]*[）)]", string.Empty).Trim();
        }
    }
}
