using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NihongoLife.UI
{
    /// <summary>
    /// The chosen character on the main menu, alive: breathing idle, a bow, a point and a chat gesture in turn while it slowly
    /// turns toward the buttons. Every playable kid is placed in 01_MainMenu under this object (authored in the scene, nothing is
    /// created at runtime); only the selected one is active. It follows the character picked in "Choose character", and a tap or
    /// click on the figure switches to the next kid (and saves that choice).
    /// </summary>
    public sealed class MenuAvatarStage : MonoBehaviour
    {
        private const float ScreenHeightShare = 0.62f;
        [SerializeField] private GameObject[] figures = new GameObject[0];
        [SerializeField] private string[] figureIds = new string[0];
        [SerializeField] private Light figureLight;

        private GameObject _figure;
        private CharacterAnimationController _anim;
        private string _shownId;
        private float _nextBeat, _nextCheck;
        private int _beat;
        private Vector3 _anchor;
        private float _baseYaw;

        public bool HasFigure => _figure != null;
        public string ShownId => _shownId;
        public Transform Figure => _figure != null ? _figure.transform : null;
        public int FigureCount => figures != null ? figures.Length : 0;

        private void Start() { Show(PlayableCharacterCatalog.GetSelected().Id); }

        /// <summary>Shows the next (step 1) or previous (step -1) playable kid and saves it as the selected character.</summary>
        public void Cycle(int step)
        {
            int count = PlayableCharacterCatalog.Count;
            if (count == 0) return;
            int index = PlayableCharacterCatalog.IndexOf(_shownId);
            for (int i = 0; i < count; i++)
            {
                index = ((index + step) % count + count) % count;
                var next = PlayableCharacterCatalog.Get(index);
                if (!next.IsAvailable || IndexOfFigure(next.Id) < 0) continue;
                PlayableCharacterCatalog.SaveSelected(next.Id);
                Show(next.Id);
                return;
            }
        }

        private int IndexOfFigure(string id)
        {
            if (figureIds == null) return -1;
            for (int i = 0; i < figureIds.Length && i < figures.Length; i++)
                if (figures[i] != null && string.Equals(figureIds[i], id, System.StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private void Show(string id)
        {
            int index = IndexOfFigure(id);
            if (index < 0) index = IndexOfFigure(PlayableCharacterCatalog.DefaultId);
            for (int i = 0; i < figures.Length; i++) if (figures[i] != null) figures[i].SetActive(i == index);
            _figure = index >= 0 ? figures[index] : null;
            _shownId = index >= 0 ? figureIds[index] : id;
            _anim = _figure != null ? _figure.GetComponent<CharacterAnimationController>() : null;
            if (figureLight != null) figureLight.enabled = _figure != null;
            _beat = 0;
            _nextBeat = Time.unscaledTime + 1f;
            var camera = Camera.main;
            if (_figure != null && camera != null) Place(camera);
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + 0.7f;
                string selected = PlayableCharacterCatalog.GetSelected().Id;
                if (_shownId != selected && IndexOfFigure(selected) >= 0) Show(selected);
            }
            if (_figure == null) return;
            if (TappedFigure()) { Cycle(1); return; }
            _figure.transform.rotation = Quaternion.Euler(0f, _baseYaw + Mathf.Sin(Time.unscaledTime * 0.55f) * 14f, 0f);
            if (_anim != null && Time.unscaledTime >= _nextBeat)
            {
                _nextBeat = Time.unscaledTime + 3.2f;
                switch (_beat++ % 4)
                {
                    case 0: _anim.SetTalking(true); break;
                    case 1: _anim.SetTalking(false); _anim.TriggerBow(); break;
                    case 2: _anim.TriggerPoint(); break;
                    default: _anim.SetTalking(true); break;
                }
            }
        }

        /// <summary>A press this frame on the figure's on-screen area that no menu button or pop-up took.</summary>
        private bool TappedFigure()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return false;
            if (UiModalStack.AnyOpen) return false;
            var camera = Camera.main;
            if (camera == null) return false;
            Vector2 p = pointer.position.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointer.deviceId)) return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
            return ScreenRect(camera).Contains(p);
        }

        /// <summary>The figure's renderers projected to the screen.</summary>
        public Rect ScreenRect(Camera camera)
        {
            if (_figure == null || camera == null) return Rect.zero;
            bool any = false; var b = new Bounds();
            foreach (var r in _figure.GetComponentsInChildren<Renderer>(false))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return Rect.zero;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var s = camera.WorldToScreenPoint(corner);
                xMin = Mathf.Min(xMin, s.x); yMin = Mathf.Min(yMin, s.y); xMax = Mathf.Max(xMax, s.x); yMax = Mathf.Max(yMax, s.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void LateUpdate()
        {
            if (_figure == null) return;
            var camera = Camera.main;
            if (camera != null) Place(camera);
        }

        /// <summary>Keeps the figure at the right-hand side of the view, however the menu camera is posed or the window is resized.</summary>
        private void Place(Camera camera)
        {
            float fov = camera.fieldOfView * Mathf.Deg2Rad;
            float distance = Mathf.Max(2.5f, 1.65f / (ScreenHeightShare * 2f * Mathf.Tan(fov * 0.5f)));
            _anchor = camera.ViewportToWorldPoint(new Vector3(0.72f, 0.5f - ScreenHeightShare * 0.5f - 0.04f, distance));
            _figure.transform.position = _anchor;
            Vector3 toCamera = camera.transform.position - _anchor; toCamera.y = 0f;
            _baseYaw = toCamera.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toCamera).eulerAngles.y - 12f : 0f;
            if (figureLight != null)
                figureLight.transform.position = _anchor + (camera.transform.position - _anchor).normalized * 1.8f + Vector3.up * 1.8f;
        }
    }
}
