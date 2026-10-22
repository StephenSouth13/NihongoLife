using NihongoLife.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// Full-body 3D view of the real player model for the character profile (Tab). The player's "Visual" child
    /// is cloned onto a private stage far below the world (layer 30), lit by short-range lights so nothing in the
    /// scene is affected, and rendered into a RawImage only while the profile is open. Drag to rotate.
    /// </summary>
    public sealed class ProfilePreview : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        private const int StageLayer = 30;
        private static readonly Vector3 StagePosition = new Vector3(0f, -1200f, 0f);

        private RawImage _image;
        private RenderTexture _texture;
        private Camera _camera;
        private Transform _stage;
        private Transform _turntable;
        private GameObject _clone;
        private int _cloneSourceId;
        private float _yaw = 0f; // model faces +Z, the camera looks back at it from +Z
        private Vector2 _size = new Vector2(280f, 404f);
        private StudioLight.CameraHook _studio;
        private static readonly Color Backdrop = new Color(0.86f, 0.91f, 0.97f, 1f);

        public bool HasModel => _clone != null;
        public float Yaw => _yaw;

        public static ProfilePreview Create(RectTransform parent, Vector2 size)
        {
            var go = new GameObject("ProfilePreview", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = size.x; element.preferredHeight = size.y; element.minWidth = size.x; element.minHeight = size.y;
            var preview = go.AddComponent<ProfilePreview>();
            preview._size = size;
            preview._image = go.GetComponent<RawImage>();
            preview._image.color = Color.white;
            preview._image.raycastTarget = true;
            return preview;
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            _yaw -= eventData.delta.x * 0.45f;
            if (_turntable != null) _turntable.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void OnEnable() => SetActive(true);
        private void OnDisable() => SetActive(false);

        private void SetActive(bool active)
        {
            if (active) EnsureStage();
            if (_camera != null) _camera.enabled = active;
            if (_stage != null) _stage.gameObject.SetActive(active);
        }

        private void LateUpdate()
        {
            // Rebuild the clone if the player (or their selected character) changed.
            var player = SceneRefs.Find<PlayerController>();
            Transform visual = player != null ? player.transform.Find("Visual") : null;
            int id = visual != null ? visual.gameObject.GetInstanceID() : 0;
            if (id != _cloneSourceId) BuildClone(visual, id);
        }

        private void EnsureStage()
        {
            if (_stage != null) return;
            if (_image == null) _image = GetComponent<RawImage>(); // OnEnable can run before Create() assigns it
            // Same aspect as the picture on screen (the layout may not be built yet, so the requested size is used):
            // a texture of another shape squeezed the character.
            int w = Mathf.RoundToInt(_size.x * 2f), h = Mathf.RoundToInt(_size.y * 2f);
            _texture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "ProfilePreviewRT", antiAliasing = 4 };
            _image.texture = _texture;

            _stage = new GameObject("ProfilePreviewStage").transform;
            _stage.position = StagePosition;
            _turntable = new GameObject("Turntable").transform;
            _turntable.SetParent(_stage, false);
            _turntable.localRotation = Quaternion.Euler(0f, _yaw, 0f);

            var cameraGo = new GameObject("ProfilePreviewCamera");
            cameraGo.transform.SetParent(_stage, false);
            cameraGo.transform.localPosition = new Vector3(0f, 1.0f, 3.3f);
            cameraGo.transform.LookAt(_stage.position + Vector3.up * 0.92f);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Backdrop; // light, friendly studio backdrop instead of a near-black void
            _camera.cullingMask = 1 << StageLayer;
            _camera.fieldOfView = 30f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 12f;
            _camera.targetTexture = _texture;
            _camera.depth = -10f;
            _studio = new StudioLight.CameraHook(_camera, () => _clone != null ? _turntable : null);

            // Short-range key / rim lights: they cannot reach the world 1200 m above.
            AddLight(new Vector3(1.6f, 2.6f, 2.4f), 9f, new Color(1f, 0.95f, 0.88f), 7f);
            AddLight(new Vector3(-1.8f, 1.6f, 1.8f), 4f, new Color(0.7f, 0.8f, 1f), 7f);
            AddLight(new Vector3(0f, 2.4f, -2.2f), 6f, new Color(1f, 0.85f, 0.6f), 7f);
            // Floor disc so the character does not float in the void.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Floor";
            Destroy(floor.GetComponent<Collider>());
            floor.transform.SetParent(_stage, false);
            floor.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
            var r = floor.GetComponent<Renderer>();
            r.material.color = new Color(0.72f, 0.8f, 0.9f);
            floor.layer = StageLayer;
        }

        private void AddLight(Vector3 local, float intensity, Color color, float range)
        {
            var go = new GameObject("PreviewLight");
            go.transform.SetParent(_stage, false);
            go.transform.localPosition = local;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << StageLayer;
        }

        private void BuildClone(Transform visual, int id)
        {
            _cloneSourceId = id;
            if (_clone != null) Destroy(_clone);
            _clone = null;
            if (visual == null || _turntable == null) return;
            _clone = Instantiate(visual.gameObject, _turntable, false);
            _clone.name = "PreviewModel";
            _clone.transform.localPosition = Vector3.zero;
            _clone.transform.localRotation = Quaternion.identity;
            foreach (var behaviour in _clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in _clone.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach (var t in _clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = StageLayer;
            var animator = _clone.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var p in animator.parameters)
                    if (p.type == AnimatorControllerParameterType.Float && p.name == "Speed") animator.SetFloat("Speed", 0f);
            }
            FrameCamera();
        }

        /// <summary>Whole body in the picture whatever the character's size (adult ~1.75 m, kids ~1.35 m).</summary>
        private void FrameCamera()
        {
            if (_clone == null || _camera == null) return;
            float height = 1.7f;
            bool any = false;
            var bounds = new Bounds();
            foreach (var r in _clone.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            if (any) height = Mathf.Max(0.6f, bounds.max.y - _stage.position.y);
            float visible = height * 1.18f;
            float distance = visible * 0.5f / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float centreY = height * 0.5f;
            _camera.transform.localPosition = new Vector3(0f, centreY + height * 0.08f, distance);
            _camera.transform.LookAt(_stage.position + Vector3.up * centreY);
            _camera.farClipPlane = distance + 4f;
        }

        private void OnDestroy()
        {
            _studio?.Dispose();
            if (_stage != null) Destroy(_stage.gameObject);
            if (_texture != null) { _texture.Release(); Destroy(_texture); }
        }
    }
}
