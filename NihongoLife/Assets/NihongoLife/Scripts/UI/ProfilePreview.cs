using NihongoLife.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        public bool HasModel => _clone != null;
        public float Yaw => _yaw;

        public static ProfilePreview Create(RectTransform parent, Vector2 size)
        {
            var go = new GameObject("ProfilePreview", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = size.x; element.preferredHeight = size.y; element.minWidth = size.x; element.minHeight = size.y;
            var preview = go.AddComponent<ProfilePreview>();
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
            var player = FindFirstObjectByType<PlayerController>();
            Transform visual = player != null ? player.transform.Find("Visual") : null;
            int id = visual != null ? visual.gameObject.GetInstanceID() : 0;
            if (id != _cloneSourceId) BuildClone(visual, id);
        }

        private void EnsureStage()
        {
            if (_stage != null) return;
            if (_image == null) _image = GetComponent<RawImage>(); // OnEnable can run before Create() assigns it
            var rect = (RectTransform)transform;
            int w = Mathf.Max(256, Mathf.RoundToInt(rect.rect.width > 1f ? rect.rect.width * 1.5f : 600f));
            int h = Mathf.Max(256, Mathf.RoundToInt(rect.rect.height > 1f ? rect.rect.height * 1.5f : 780f));
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
            _camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
            _camera.cullingMask = 1 << StageLayer;
            _camera.fieldOfView = 30f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 12f;
            _camera.targetTexture = _texture;
            _camera.depth = -10f;

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
            r.material.color = new Color(0.16f, 0.2f, 0.26f);
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
        }

        private void OnDestroy()
        {
            if (_stage != null) Destroy(_stage.gameObject);
            if (_texture != null) { _texture.Release(); Destroy(_texture); }
        }
    }
}
