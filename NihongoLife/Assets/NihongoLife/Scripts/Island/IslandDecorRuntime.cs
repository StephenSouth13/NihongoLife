using System.Collections.Generic;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>A decoration standing on the island (the placement it was spawned from).</summary>
    public sealed class DecorPiece : MonoBehaviour
    {
        public DecorPlacement Placement;
        public IslandDecorDef Def;
    }

    /// <summary>
    /// Lives on 60_MidoriIsland (added by IslandDecorBuilder). Spawns the island's decorations from the record — the
    /// player's own, or the friend's while visiting — and runs the decorating mode: the camera lifts to the overview,
    /// a stock bar lists what was bought, a ghost follows the pointer with a green/red ring (free / blocked), Q/E or
    /// the wheel rotate, a click places; clicking a placed piece picks it up to move or store it. Places are refused
    /// on paths, the plaza, fields, buildings, other pieces, interaction zones and the beach edge.
    /// While visiting it shows the visit bar instead: whose island it is, the hearts, ♥ and "go back".
    /// </summary>
    public sealed class IslandDecorRuntime : MonoBehaviour
    {
        [SerializeField] private Vector3 origin = new Vector3(-1200f, 0f, 0f);
        [SerializeField] private float radiusX = 50f, radiusZ = 42f;

        public static IslandDecorRuntime Instance { get; private set; }
        public bool Editing { get; private set; }
        public IReadOnlyList<DecorPiece> Pieces => _pieces;
        public string Selected => _selected?.id;
        public bool GhostValid { get; private set; }
        /// <summary>False lets a test drive the ghost with <see cref="AimAt"/> / <see cref="Confirm"/> instead of the mouse.</summary>
        public bool PointerDriven { get; set; } = true;

        private readonly List<DecorPiece> _pieces = new();
        private readonly List<Bounds> _keepOut = new();
        private IslandDecorDef _selected;
        private DecorPiece _moving;
        private GameObject _ghost;
        private Transform _ring;
        private Material _ringGood, _ringBad;
        private float _yaw;
        private Vector3 _ghostPos;
        private Canvas _canvas;
        private RectTransform _bar, _visitBar;
        private PlayerController _player;
        private ThirdPersonCameraController _follow;
        private Vector3 _camFocus, _camSavedPos;
        private Quaternion _camSavedRot;
        private float _camDistance = 26f;
        private const float CamPitch = 55f, CamMin = 10f, CamMax = 60f;

        public void Configure(Vector3 islandOrigin, float rx, float rz) { origin = islandOrigin; radiusX = rx; radiusZ = rz; }

        private void OnEnable() { Instance = this; IslandVisit.Changed += OnVisitChanged; IslandState.Changed += OnStateChanged; }
        private void OnDisable()
        {
            IslandVisit.Changed -= OnVisitChanged;
            IslandState.Changed -= OnStateChanged;
            if (Editing) EndEdit();
            if (Instance == this) Instance = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (!IslandVisit.Active) IslandShowcaseService.Publish(); // leaving the island: friends see the latest layout
        }

        private void Start()
        {
            CollectKeepOut();
            Respawn();
            _shownVisiting = IslandVisit.Active;
            BuildCanvas();
            OnVisitChanged();
        }

        private void OnStateChanged() { if (_bar != null && Editing) BuildBar(); }

        // ─────────── Spawning ───────────

        /// <summary>Rebuilds every piece from the current island record.</summary>
        public void Respawn()
        {
            foreach (var p in _pieces) if (p != null) Destroy(p.gameObject);
            _pieces.Clear();
            foreach (var placement in IslandState.Record.decor.ToList()) Spawn(placement);
        }

        private DecorPiece Spawn(DecorPlacement placement)
        {
            var def = IslandCatalog.Load().Decor(placement.id);
            var go = Make(def, false);
            if (go == null) return null;
            go.name = "Decor_" + def.id;
            go.transform.SetPositionAndRotation(origin + new Vector3(placement.x, 0f, placement.z), Quaternion.Euler(0f, placement.yaw, 0f));
            var piece = go.AddComponent<DecorPiece>();
            piece.Placement = placement;
            piece.Def = def;
            _pieces.Add(piece);
            return piece;
        }

        private GameObject Make(IslandDecorDef def, bool ghost)
        {
            if (def == null) return null;
            var prefab = IslandModelLibrary.Instance != null ? IslandModelLibrary.Instance.Find("decor:" + def.id) : null;
            GameObject go;
            if (prefab != null) go = Instantiate(prefab, transform);
            else
            {
                // No prefab yet (builder not run): a simple stand-in so the layout still shows.
                go = GameObject.CreatePrimitive(def.category == "plant" ? PrimitiveType.Capsule : PrimitiveType.Cube);
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(def.footprint * 0.6f, def.height * 0.5f, def.footprint * 0.6f);
            }
            if (ghost) foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            return go;
        }

        // ─────────── Keep-out ───────────

        /// <summary>Paths and the plaza have no colliders: they are kept free by their drawn bounds.</summary>
        private void CollectKeepOut()
        {
            _keepOut.Clear();
            foreach (var r in gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                if (r.name.StartsWith("Path_") || r.name == "Plaza" || r.name.StartsWith("Soil") || r.name.StartsWith("PlayMat"))
                {
                    var b = r.bounds;
                    b.Expand(new Vector3(0.6f, 10f, 0.6f));
                    _keepOut.Add(b);
                }
        }

        /// <summary>Whether <paramref name="def"/> fits at world position <paramref name="p"/> (ignoring <paramref name="ignore"/>).</summary>
        public bool CanPlace(IslandDecorDef def, Vector3 p, DecorPiece ignore = null)
        {
            if (def == null) return false;
            Vector3 local = p - origin;
            float ex = local.x / (radiusX - 3f), ez = local.z / (radiusZ - 3f);
            if (ex * ex + ez * ez > 1f) return false;
            float half = Mathf.Max(0.3f, def.footprint * 0.5f);
            foreach (var b in _keepOut)
                if (p.x + half > b.min.x && p.x - half < b.max.x && p.z + half > b.min.z && p.z - half < b.max.z) return false;
            foreach (var hit in Physics.OverlapBox(p + Vector3.up * 1.1f, new Vector3(half, 0.95f, half), Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit == null) continue;
                if (_ghost != null && hit.transform.IsChildOf(_ghost.transform)) continue;
                if (ignore != null && hit.transform.IsChildOf(ignore.transform)) continue;
                if (hit.GetComponentInParent<PlayerController>() != null) continue;
                return false;
            }
            return true;
        }

        // ─────────── Editing ───────────

        public void BeginEdit()
        {
            if (Editing || IslandVisit.Active) return;
            Editing = true;
            _player = SceneRefs.Find<PlayerController>();
            if (_player != null) _player.InputLocked = true;
            // The decorating camera: looks down on the island and pans freely (the follow camera rests meanwhile).
            var camera = Camera.main;
            if (camera != null)
            {
                _follow = camera.GetComponent<ThirdPersonCameraController>();
                if (_follow != null) _follow.enabled = false;
                _camSavedPos = camera.transform.position; _camSavedRot = camera.transform.rotation;
            }
            _camFocus = _player != null ? new Vector3(_player.transform.position.x, origin.y, _player.transform.position.z) : origin;
            UiModalStack.Register(this, () => Editing, EndEdit, "Island decorating");
            IslandUI.CloseShop();
            BuildBar();
        }

        public void EndEdit()
        {
            if (!Editing) return;
            CancelGhost();
            Editing = false;
            if (_player != null) _player.InputLocked = false;
            if (Camera.main != null) Camera.main.transform.SetPositionAndRotation(_camSavedPos, _camSavedRot);
            if (_follow != null) _follow.enabled = true;
            _follow = null;
            if (_bar != null) _bar.gameObject.SetActive(false);
            IslandShowcaseService.Publish();
        }

        /// <summary>Picks a stock item to place (bar click, or tests).</summary>
        public void Select(string id)
        {
            CancelGhost();
            _selected = IslandCatalog.Load().Decor(id);
            if (_selected == null || IslandDecor.Stock(_selected.id) <= 0) { _selected = null; return; }
            MakeGhost(_selected);
            BuildBar();
        }

        private void MakeGhost(IslandDecorDef def)
        {
            _ghost = Make(def, true);
            if (_ghost == null) return;
            _ghost.name = "DecorGhost";
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.name = "Ring";
            ring.transform.SetParent(_ghost.transform, false);
            float d = Mathf.Max(0.8f, def.footprint) / Mathf.Max(0.01f, _ghost.transform.lossyScale.x);
            ring.transform.localScale = new Vector3(d, 0.01f / Mathf.Max(0.01f, _ghost.transform.lossyScale.y), d);
            ring.transform.localPosition = Vector3.up * 0.09f / Mathf.Max(0.01f, _ghost.transform.lossyScale.y); // above the plaza and paths
            _ring = ring.transform;
            if (_ringGood == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                _ringGood = new Material(shader) { color = new Color(0.3f, 0.9f, 0.45f) };
                _ringBad = new Material(shader) { color = new Color(0.95f, 0.3f, 0.3f) };
                _ringGood.SetColor("_BaseColor", _ringGood.color);
                _ringBad.SetColor("_BaseColor", _ringBad.color);
            }
        }

        private void CancelGhost()
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = null;
            if (_moving != null) { _moving.gameObject.SetActive(true); _moving = null; }
            _selected = null;
        }

        /// <summary>Moves the ghost to island-local (x, z) (pointer, or tests) and reports whether it fits there.</summary>
        public bool AimAt(float x, float z)
        {
            if (_ghost == null) return false;
            _ghostPos = origin + new Vector3(Mathf.Round(x * 2f) / 2f, 0f, Mathf.Round(z * 2f) / 2f);
            _ghost.transform.SetPositionAndRotation(_ghostPos, Quaternion.Euler(0f, _yaw, 0f));
            GhostValid = CanPlace(_selected, _ghostPos, _moving);
            if (_ring != null) _ring.GetComponent<Renderer>().sharedMaterial = GhostValid ? _ringGood : _ringBad;
            return GhostValid;
        }

        public void Rotate(float degrees) { _yaw = Mathf.Repeat(_yaw + degrees, 360f); if (_ghost != null) _ghost.transform.rotation = Quaternion.Euler(0f, _yaw, 0f); }

        /// <summary>Places (or drops the moved piece) where the ghost is. Returns true when it was placed.</summary>
        public bool Confirm()
        {
            if (_ghost == null || _selected == null || !GhostValid) { if (_ghost != null) NihongoLife.Audio.GameAudioCue.UiError.Play(); return false; }
            Vector3 local = _ghostPos - origin;
            if (_moving != null)
            {
                IslandDecor.Move(_moving.Placement, local.x, local.z, _yaw);
                _moving.transform.SetPositionAndRotation(_ghostPos, Quaternion.Euler(0f, _yaw, 0f));
                _moving.gameObject.SetActive(true);
                _moving = null;
                Destroy(_ghost); _ghost = null; _selected = null;
            }
            else
            {
                var placement = IslandDecor.Place(_selected.id, local.x, local.z, _yaw);
                if (placement == null) return false;
                Spawn(placement);
                IslandState.Discover("decor:" + _selected.id);
                if (IslandDecor.Stock(_selected.id) <= 0) { Destroy(_ghost); _ghost = null; _selected = null; }
            }
            NihongoLife.Audio.GameAudioCue.Pickup.Play();
            BuildBar();
            return true;
        }

        /// <summary>Picks up a placed piece to move it (click, or tests).</summary>
        public void PickUp(DecorPiece piece)
        {
            if (piece == null || !Editing) return;
            CancelGhost();
            _moving = piece;
            _selected = piece.Def;
            _yaw = piece.Placement.yaw;
            piece.gameObject.SetActive(false);
            MakeGhost(piece.Def);
            AimAt(piece.Placement.x, piece.Placement.z);
            BuildBar();
        }

        /// <summary>Puts the piece being moved back into the stock.</summary>
        public void StoreMoving()
        {
            if (_moving == null) return;
            IslandDecor.Store(_moving.Placement);
            _pieces.Remove(_moving);
            Destroy(_moving.gameObject);
            _moving = null;
            CancelGhost();
            BuildBar();
        }

        /// <summary>Centres the decorating camera on island-local (x, z).</summary>
        public void FocusOn(float x, float z, float distance = -1f)
        {
            _camFocus = ClampToIsland(origin + new Vector3(x, 0f, z));
            if (distance > 0f) _camDistance = Mathf.Clamp(distance, CamMin, CamMax);
            PlaceCamera();
        }

        private Vector3 ClampToIsland(Vector3 p)
        {
            Vector3 l = p - origin;
            float e = Mathf.Sqrt(l.x * l.x / (radiusX * radiusX) + l.z * l.z / (radiusZ * radiusZ));
            if (e > 1f) l /= e;
            return origin + new Vector3(l.x, 0f, l.z);
        }

        private void PlaceCamera()
        {
            var camera = Camera.main;
            if (camera == null || !Editing) return;
            camera.transform.rotation = Quaternion.Euler(CamPitch, 0f, 0f);
            camera.transform.position = _camFocus - camera.transform.forward * _camDistance;
        }

        private void LateUpdate()
        {
            if (!Editing) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && !UiModalStack.IsTyping)
            {
                Vector2 pan = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) pan.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) pan.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) pan.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) pan.x -= 1f;
                if (pan != Vector2.zero) _camFocus = ClampToIsland(_camFocus + new Vector3(pan.x, 0f, pan.y).normalized * (_camDistance * 0.9f * Time.unscaledDeltaTime));
            }
            var mouse = Mouse.current;
            if (PointerDriven && mouse != null && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f) _camDistance = Mathf.Clamp(_camDistance * (scroll > 0f ? 0.88f : 1.14f), CamMin, CamMax);
            }
            PlaceCamera();
        }

        private void Update()
        {
            if (!Editing) return;
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame) Rotate(-45f);
                if (keyboard.eKey.wasPressedThisFrame) Rotate(45f);
                if (keyboard.deleteKey.wasPressedThisFrame) StoreMoving();
            }
            if (!PointerDriven || mouse == null || Camera.main == null) return;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (_ghost != null && new Plane(Vector3.up, origin).Raycast(ray, out float t))
            {
                Vector3 p = ray.GetPoint(t) - origin;
                AimAt(p.x, p.z);
            }
            if (overUi) return;
            if (mouse.rightButton.wasPressedThisFrame) { CancelGhost(); BuildBar(); }
            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (_ghost != null) { Confirm(); return; }
            // Flowers and flat rocks only have a trigger, so look through every hit for the nearest piece.
            var piece = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Collide)
                .OrderBy(h => h.distance).Select(h => h.collider.GetComponentInParent<DecorPiece>()).FirstOrDefault(p => p != null);
            if (piece != null) PickUp(piece);
        }

        // ─────────── UI ───────────

        private void BuildCanvas()
        {
            _canvas = NLUi.CreateCanvas("IslandDecorCanvas", 140);
            Loc.Watch(this, () => { if (Editing) BuildBar(); OnVisitChanged(); });
        }

        private void BuildBar()
        {
            if (_canvas == null) return;
            if (_bar != null) Destroy(_bar.gameObject);
            var card = UiKit.Card(_canvas.transform, "DecorBar", UiTheme.Ink, 22, 0.4f);
            _bar = card.rectTransform;
            UiKit.Pin(_bar, new Vector2(0.5f, 0f), 0f, 22f, 1240f, 210f);
            UiKit.Place(UiKit.Label(_bar, "Title", "Trang trí đảo", "Decorate your island", "島をかざる", 22f, UiTheme.Gold, FontStyles.Bold), 24f, 14f, 400f, 30f);
            UiKit.Place(UiKit.Label(_bar, "Hint", () => _moving != null
                    ? Loc.T("Đang di chuyển — bấm để đặt chỗ mới · Q/E xoay · Delete cất vào kho · chuột phải huỷ", "Moving — click to put it down · Q/E rotate · Delete stores it · right-click cancels", "移動中 — クリックで置く · Q/Eで回転 · Deleteでしまう · 右クリックで取消")
                    : _selected != null
                        ? Loc.T("Bấm lên đảo để đặt · vòng xanh = được, đỏ = vướng · Q/E xoay · WASD di chuyển camera, lăn chuột phóng to · chuột phải huỷ", "Click the island to place · green ring = free, red = blocked · Q/E rotate · WASD moves the camera, wheel zooms · right-click cancels", "島をクリックして置く · 緑=OK 赤=だめ · Q/Eで回転 · WASDでカメラ移動、ホイールでズーム · 右クリックで取消")
                        : Loc.T("Chọn một món trong kho bên dưới, hoặc bấm vào món đã đặt để di chuyển / cất · WASD di chuyển camera, lăn chuột phóng to.", "Pick an item from your stock below, or click a placed piece to move or store it · WASD moves the camera, wheel zooms.", "下から えらぶか、置いたものを クリックして 移動・収納 · WASDでカメラ移動、ホイールでズーム。"),
                14f, UiTheme.TextSoft), 24f, 54f, 1190f, 22f);
            UiKit.Place(UiKit.Label(_bar, "Count", () => Loc.F("{0}/{1} món trên đảo", "{0}/{1} pieces placed", "{0}/{1}個", IslandDecor.Placed.Count, IslandDecor.MaxPieces), 14f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight), 900f, 16f, 160f, 24f);

            var shop = UiKit.Button(_bar, "DecorShop", "Mua đồ trang trí", "Buy decorations", "かざりを買う", () => { EndEdit(); IslandUI.OpenShopAt("decor", null); }, UiKit.Kind.Secondary, "storefront", 15f);
            UiKit.Pin((RectTransform)shop.transform, new Vector2(1f, 1f), -150f, -12f, 200f, 40f);
            var done = UiKit.Button(_bar, "DecorDone", "Xong", "Done", "おわり", EndEdit, UiKit.Kind.Primary, "check_circle", 16f);
            UiKit.Pin((RectTransform)done.transform, new Vector2(1f, 1f), -18f, -12f, 120f, 40f);
            if (_moving != null)
            {
                var store = UiKit.Button(_bar, "StorePiece", "Cất vào kho", "Store it", "しまう", StoreMoving, UiKit.Kind.Danger, "inventory_2", 15f);
                UiKit.Pin((RectTransform)store.transform, new Vector2(1f, 1f), -360f, -12f, 160f, 40f);
            }

            var row = UiKit.Rect(_bar, "Stock");
            UiKit.Place(row, 24f, 78f, 1192f, 118f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f; layout.childControlWidth = layout.childControlHeight = false; layout.childAlignment = TextAnchor.MiddleLeft;
            var stock = IslandDecor.StockList().ToList();
            if (stock.Count == 0)
            {
                var empty = UiKit.Label(row, "Empty", "Kho trống — mua cây, hoa, ghế… ở cửa hàng Midori (tab Trang trí).", "Your stock is empty — buy trees, flowers, benches… at the Midori Store (Decor tab).", "まだ ありません — みどりしょうてんの「かざり」で買えます。", 16f, UiTheme.Muted);
                empty.rectTransform.sizeDelta = new Vector2(900f, 40f);
            }
            foreach (var (def, count) in stock)
            {
                bool on = _selected != null && _moving == null && _selected.id == def.id;
                var tile = UiKit.Card(row, "Stock_" + def.id, on ? UiKit.Alpha(UiTheme.Gold, 0.3f) : UiTheme.Surface, 14);
                tile.rectTransform.sizeDelta = new Vector2(112f, 112f);
                if (on) UiKit.Border(tile.rectTransform, UiTheme.Gold, 14);
                var icon = UiKit.Image(tile.transform, "Icon", Color.white, ItemIcons.Get(def.ItemId) ?? UiIcons.Get(def.icon) ?? UiIcons.Get("yard"));
                icon.preserveAspect = true;
                UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 1f), 0f, -6f, 64f, 60f);
                UiKit.Pin(UiKit.Label(tile.transform, "Name", IslandLanguage.Primary(def.word), 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform, new Vector2(0.5f, 0f), 0f, 22f, 104f, 22f);
                UiKit.Pin(UiKit.Label(tile.transform, "Count", "× " + count, 13f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform, new Vector2(0.5f, 0f), 0f, 4f, 104f, 18f);
                string id = def.id;
                tile.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(id));
            }
        }

        private bool _shownVisiting;

        private void OnVisitChanged()
        {
            if (_canvas == null) return;
            if (_visitBar != null) Destroy(_visitBar.gameObject);
            bool visiting = IslandVisit.Active;
            if (visiting != _shownVisiting)
            {
                // Switched islands in place (visit started or ended while on Midori Island).
                _shownVisiting = visiting;
                if (Editing) EndEdit();
                Respawn();
                IslandUI.CloseShop(); IslandUI.CloseFarm(); IslandUI.CloseAnimal();
                if (visiting) IslandUI.HideToolbar(); else if (IslandRuntime.Active != null) IslandUI.ShowToolbar();
            }
            if (!visiting) return;
            var v = IslandVisit.Current;
            var card = UiKit.Card(_canvas.transform, "VisitBar", UiTheme.Ink, 22, 0.4f);
            _visitBar = card.rectTransform;
            UiKit.Pin(_visitBar, new Vector2(0.5f, 1f), 0f, -18f, 720f, 76f);
            var avatar = UiKit.Avatar(_visitBar, "Avatar", v.display_name, 48f, UiTheme.Teal, Color.white);
            UiKit.Pin(avatar.rectTransform, new Vector2(0f, 0.5f), 16f, 0f, 48f, 48f);
            UiKit.Place(UiKit.Label(_visitBar, "Owner", Loc.F("Đảo của {0}", "{0}'s island", "{0}さんの島", v.display_name), 22f, UiTheme.Text, FontStyles.Bold), 76f, 10f, 330f, 30f);
            var layout = v.Layout;
            UiKit.Place(UiKit.Label(_visitBar, "Stats", Loc.F("{0} đồ trang trí · {1} lần thu hoạch · {2} từ", "{0} decorations · {1} harvests · {2} words", "かざり{0} · 収穫{1} · ことば{2}", layout.decor?.Count ?? 0, layout.harvested, layout.words), 14f, UiTheme.TextSoft), 76f, 42f, 330f, 22f);
            var like = UiKit.Button(_visitBar, "Like", v.likes.ToString(), () => IslandVisit.Like(), IslandVisit.Liked ? UiKit.Kind.Danger : UiKit.Kind.Secondary, "favorite", 17f);
            like.interactable = !IslandVisit.Liked;
            UiKit.Pin((RectTransform)like.transform, new Vector2(1f, 0.5f), -170f, 0f, 110f, 46f);
            var back = UiKit.Button(_visitBar, "LeaveVisit", "Về", "Leave", "かえる", IslandVisit.End, UiKit.Kind.Primary, "logout", 17f);
            UiKit.Pin((RectTransform)back.transform, new Vector2(1f, 0.5f), -16f, 0f, 140f, 46f);
        }
    }

    internal static class AudioCueExtensions
    {
        public static void Play(this NihongoLife.Audio.GameAudioCue cue)
        {
            if (GameServices.TryGet(out NihongoLife.Audio.IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }
    }
}
