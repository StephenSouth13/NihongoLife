using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NihongoLife.Home
{
    /// <summary>F7: Sims-style decoration of the player's room (45_HomeBedroom). Key V opens the catalogue; Buy puts a piece on the cursor, click places it, R rotates, Esc cancels.</summary>
    public sealed class RoomDecor : MonoBehaviour
    {
        public static RoomDecor Instance { get; private set; }
        public const float HalfW = 3.5f, HalfD = 2.9f, Grid = 0.25f, SellBack = 0.5f;

        private readonly List<RoomDecorItem> _placed = new List<RoomDecorItem>();
        private RoomDecorItem _ghost; private bool _ghostIsMove; private int _ghostPrice;
        private Transform _room; private DecorPopup _popup; private TextMeshProUGUI _hint;
        private NihongoLife.Player.PlayerController _player; private NihongoLife.Cameras.ThirdPersonCameraController _cam;
        private bool _playerWas, _camWas;
        private float _ghostX, _ghostZ, _ghostRot;
        private string _ghostWhy = "";
        public string LastMessage { get; private set; } = "";

        public IReadOnlyList<RoomDecorItem> Placed => _placed;
        public bool IsPlacing => _ghost != null;
        public DecorPopup Popup => _popup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != WorldLocationCatalog.HomeBedroomScene || Instance != null) return;
            new GameObject("RoomDecor").AddComponent<RoomDecor>();
        }

        private void Awake()
        {
            Instance = this;
            var canvasGo = new GameObject("RoomDecorCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 120;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            _popup = DecorPopup.GetOrCreate(canvasGo, null);
            _hint = UiKit.Label(canvasGo.transform, "PlaceHint", "", 22f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center, false);
            var r = _hint.rectTransform; r.anchorMin = new Vector2(0.1f, 0f); r.anchorMax = new Vector2(0.9f, 0f); r.pivot = new Vector2(0.5f, 0f); r.offsetMin = new Vector2(0, 40); r.offsetMax = new Vector2(0, 90);
            _hint.gameObject.SetActive(false);
        }

        private void Start() { StartCoroutine(LoadWhenReady()); }

        private System.Collections.IEnumerator LoadWhenReady()
        {
            for (float t = 0; t < 10f && HomeBedroomRuntime.Instance == null; t += Time.unscaledDeltaTime) yield return null;
            _room = HomeBedroomRuntime.Instance != null ? HomeBedroomRuntime.Instance.transform : transform;
            LoadSaved();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; if (IsPlacing) Unlock(); }

        private static PlayerProgressDto Progress => GameServices.TryGet(out IProgressRepository r) ? r.GetProgress() : null;

        public void Save()
        {
            var p = Progress; if (p == null) return;
            p.roomDecor = new List<RoomDecorRecord>();
            foreach (var d in _placed) p.roomDecor.Add(new RoomDecorRecord { id = d.Def.Id, x = d.X, z = d.Z, rot = d.Rot });
            if (GameServices.TryGet(out IProgressRepository repo)) repo.SaveProgress(p);
        }

        private void LoadSaved()
        {
            var p = Progress; if (p == null || p.roomDecor == null) return;
            foreach (var rec in p.roomDecor)
            {
                var def = RoomDecorCatalog.Find(rec.id); if (def == null) continue;
                _placed.Add(Spawn(def, rec.x, rec.z, rec.rot));
            }
        }

        public RoomDecorItem Spawn(DecorDef def, float x, float z, float rot)
        {
            if (_room == null) _room = HomeBedroomRuntime.Instance != null ? HomeBedroomRuntime.Instance.transform : transform;
            var go = new GameObject("Decor_" + def.Id);
            go.transform.SetParent(_room, false);
            def.Build(go.transform);
            var item = go.AddComponent<RoomDecorItem>(); item.Def = def;
            item.Renderers = go.GetComponentsInChildren<Renderer>();
            item.Originals = new Material[item.Renderers.Length][];
            for (int i = 0; i < item.Renderers.Length; i++) item.Originals[i] = item.Renderers[i].sharedMaterials;
            Move(item, x, z, rot);
            return item;
        }

        private static void Move(RoomDecorItem item, float x, float z, float rot)
        {
            item.X = x; item.Z = z; item.Rot = rot;
            item.transform.localPosition = new Vector3(x, 0f, z); item.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
        }

        private static Vector2 Half(DecorDef d, float rot) { bool sw = Mathf.Abs(Mathf.Sin(rot * Mathf.Deg2Rad)) > 0.5f; return (sw ? new Vector2(d.Size.y, d.Size.x) : d.Size) * 0.5f; }

        /// <summary>True when the piece fits inside the room walls, does not overlap other decor and is not inside the bed, desk or other solid furniture.</summary>
        public bool CanPlace(DecorDef def, float x, float z, float rot, RoomDecorItem ignore, out string why)
        {
            why = "";
            var h = Half(def, rot);
            if (Mathf.Abs(x) + h.x > HalfW || Mathf.Abs(z) + h.y > HalfD)
            { why = Loc.T("Vượt ra ngoài phòng.", "Outside the room.", "部屋の外です。"); return false; }
            foreach (var o in _placed)
            {
                if (o == ignore || o.Def.Flat != def.Flat) continue;
                var oh = Half(o.Def, o.Rot);
                if (Mathf.Abs(x - o.X) < h.x + oh.x - 0.02f && Mathf.Abs(z - o.Z) < h.y + oh.y - 0.02f)
                { why = Loc.T("Đang đè lên đồ khác.", "Overlaps another piece.", "ほかの家具と重なっています。"); return false; }
            }
            if (def.Flat) return true;
            var centre = _room.TransformPoint(new Vector3(x, 0.55f, z));
            var hits = Physics.OverlapBox(centre, new Vector3(h.x, 0.4f, h.y) * 0.96f, _room.rotation * Quaternion.Euler(0, rot, 0), ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (c.GetComponentInParent<RoomDecorItem>() != null || c.GetComponentInParent<NihongoLife.Player.PlayerController>() != null) continue;
                why = Loc.T("Vướng đồ nội thất (giường, bàn...).", "Blocked by furniture (bed, desk...).", "家具（ベッド、机…）にぶつかります。"); return false;
            }
            return true;
        }

        /// <summary>Pays and puts the piece on the cursor. Returns false (with LastMessage) when it can't be bought.</summary>
        public bool Buy(string id)
        {
            var def = RoomDecorCatalog.Find(id); var inv = PlayerInventory.Instance;
            if (def == null || inv == null) return false;
            if (IsPlacing) { LastMessage = Loc.T("Đặt món hiện tại trước.", "Place the current piece first.", "今の家具を先に置いてください。"); return false; }
            if (!inv.SpendYen(def.Price, Loc.Triple("Mua đồ trang trí phòng", "Room decoration", "部屋の家具"))) { LastMessage = Loc.F("Không đủ tiền ({0}).", "Not enough money ({0}).", "お金が足りません（{0}）。", Loc.Yen(def.Price)); return false; }
            BeginPlacing(Spawn(def, 0f, 0f, 0f), false, def.Price);
            LastMessage = Loc.T("Bấm để đặt, R xoay, Esc huỷ.", "Click to place, R rotates, Esc cancels.", "クリックで設置、Rで回転、Escで取消。");
            return true;
        }

        public bool StartMove(int index)
        {
            if (IsPlacing || index < 0 || index >= _placed.Count) return false;
            var item = _placed[index]; _placed.RemoveAt(index);
            BeginPlacing(item, true, 0);
            return true;
        }

        public int SellValue(DecorDef d) => Mathf.RoundToInt(d.Price * SellBack);

        public bool Sell(int index)
        {
            if (IsPlacing || index < 0 || index >= _placed.Count) return false;
            var item = _placed[index]; _placed.RemoveAt(index);
            int value = SellValue(item.Def);
            PlayerInventory.Instance?.AddYen(value, Loc.Triple("Bán đồ trang trí", "Sold decoration", "家具を売却"));
            Destroy(item.gameObject); Save();
            LastMessage = Loc.F("Đã bán, nhận {0}.", "Sold for {0}.", "売却：{0}。", Loc.Yen(value));
            return true;
        }

        private void BeginPlacing(RoomDecorItem item, bool isMove, int paid)
        {
            _ghost = item; _ghostIsMove = isMove; _ghostPrice = paid;
            _player = SceneRefs.Find<NihongoLife.Player.PlayerController>(); _cam = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
            _playerWas = _player != null && _player.InputLocked; _camWas = _cam != null && _cam.IsLocked;
            if (_hint != null) _hint.gameObject.SetActive(true);
            _popup?.Hide();
            _ghostX = item.X; _ghostZ = item.Z; _ghostRot = item.Rot;
            Tint(item, true);
        }

        private static void Tint(RoomDecorItem item, bool valid)
        {
            var m = NihongoLife.World.AmbientWildlife.MaterialFor(valid ? new Color(0.35f, 0.9f, 0.45f) : new Color(0.95f, 0.3f, 0.3f));
            foreach (var r in item.Renderers) { if (r == null) continue; var arr = new Material[r.sharedMaterials.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = m; r.sharedMaterials = arr; }
        }

        private static void Untint(RoomDecorItem item) { for (int i = 0; i < item.Renderers.Length; i++) if (item.Renderers[i] != null) item.Renderers[i].sharedMaterials = item.Originals[i]; }

        /// <summary>Puts the carried piece at a room-local spot (also used by tests). False when the spot is not allowed.</summary>
        public bool PlaceAt(float x, float z)
        {
            if (_ghost == null) return false;
            x = Mathf.Round(x / Grid) * Grid; z = Mathf.Round(z / Grid) * Grid;
            if (!CanPlace(_ghost.Def, x, z, _ghostRot, _ghost, out _ghostWhy)) { LastMessage = _ghostWhy; return false; }
            Move(_ghost, x, z, _ghostRot); Untint(_ghost); _placed.Add(_ghost); _ghost = null; Unlock(); Save();
            LastMessage = Loc.T("Đã đặt.", "Placed.", "設置しました。");
            return true;
        }

        public void Rotate() { if (_ghost != null) { _ghostRot = (_ghostRot + 90f) % 360f; _ghost.transform.localRotation = Quaternion.Euler(0, _ghostRot, 0); } }

        public void CancelPlacing()
        {
            if (_ghost == null) return;
            if (_ghostIsMove) { Untint(_ghost); _placed.Add(_ghost); }
            else { Destroy(_ghost.gameObject); PlayerInventory.Instance?.AddYen(_ghostPrice, Loc.Triple("Hoàn tiền đồ trang trí", "Decoration refund", "家具の返金")); }
            _ghost = null; Unlock(); Save();
            LastMessage = Loc.T("Đã huỷ.", "Cancelled.", "取り消しました。");
        }

        private void Unlock()
        {
            if (_player != null) _player.InputLocked = _playerWas;
            if (_cam != null) _cam.IsLocked = _camWas;
            if (_hint != null) _hint.gameObject.SetActive(false);
        }

        private void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (!IsPlacing)
            {
                if (kb.xKey.wasPressedThisFrame && _popup != null)
                {
                    if (_popup.IsOpen) _popup.Hide(); else if (!UiModalStack.BlocksHotkeys) _popup.Show();
                }
                return;
            }
            if (_player != null) _player.InputLocked = true;
            if (_cam != null) _cam.IsLocked = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var cam = Camera.main; var mouse = Mouse.current;
            if (cam != null && mouse != null && _room != null)
            {
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                var plane = new Plane(_room.up, _room.position);
                if (plane.Raycast(ray, out float dist))
                {
                    var local = _room.InverseTransformPoint(ray.GetPoint(dist));
                    _ghostX = Mathf.Round(local.x / Grid) * Grid; _ghostZ = Mathf.Round(local.z / Grid) * Grid;
                    Move(_ghost, _ghostX, _ghostZ, _ghostRot);
                }
            }
            bool ok = CanPlace(_ghost.Def, _ghostX, _ghostZ, _ghostRot, _ghost, out _ghostWhy);
            Tint(_ghost, ok);
            if (_hint != null)
                _hint.text = (ok ? "" : _ghostWhy + "   ") + Loc.T("Bấm trái: đặt  ·  R: xoay  ·  Esc: huỷ", "Left click: place  ·  R: rotate  ·  Esc: cancel", "左クリック：設置  ·  R：回転  ·  Esc：取消");
            if (kb.rKey.wasPressedThisFrame) Rotate();
            if (kb.escapeKey.wasPressedThisFrame) { CancelPlacing(); return; }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && ok) PlaceAt(_ghostX, _ghostZ);
        }
    }
}