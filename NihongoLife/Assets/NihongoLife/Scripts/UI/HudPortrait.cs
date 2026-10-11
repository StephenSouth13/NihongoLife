using NihongoLife.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The head-and-shoulders portrait in the status widget, rendered from the player's real 3D model. The player's
    /// "Visual" child is cloned onto a private stage far below the world (layer 30, lit by short-range lights that
    /// cannot reach the scene) and photographed once into a small RenderTexture; then the stage is switched off.
    /// It is re-shot only when the player's model changes (another character chosen, scene change), so the HUD
    /// costs no extra rendering per frame.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class HudPortrait : MonoBehaviour
    {
        private const int StageLayer = 30;
        private const int TextureSize = 256;
        private static readonly Vector3 StagePosition = new Vector3(0f, -1600f, 40f);
        private static readonly Color Backdrop = new Color(0.33f, 0.52f, 0.62f, 1f);

        private RawImage _image;
        private RenderTexture _texture;
        private Camera _camera;
        private Transform _stage;
        private GameObject _clone;
        private int _sourceId;
        private int _shootInFrames = -1;
        private float _nextCheck;

        public bool HasPortrait { get; private set; }
        public int ShotCount { get; private set; }
        public Texture Texture => _texture;

        public static HudPortrait Create(RectTransform parent)
        {
            var go = new GameObject("PortraitImage", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            NLUi.Stretch((RectTransform)go.transform);
            var portrait = go.AddComponent<HudPortrait>();
            portrait._image = go.GetComponent<RawImage>();
            portrait._image.raycastTarget = false;
            portrait._image.color = new Color(1f, 1f, 1f, 0f); // invisible until the first shot
            return portrait;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + 0.75f;
                var player = FindFirstObjectByType<PlayerController>();
                var visual = player != null ? player.transform.Find("Visual") : null;
                int id = visual != null ? visual.gameObject.GetInstanceID() : 0;
                bool lost = _texture != null && !_texture.IsCreated();
                if (id != _sourceId || lost) Rebuild(visual, id);
            }
            if (_shootInFrames >= 0 && _shootInFrames-- == 0) Shoot();
        }

        /// <summary>Forces a new photo (e.g. after the outfit changed in place).</summary>
        public void Refresh()
        {
            _sourceId = -1;
            _nextCheck = 0f;
        }

        private void Rebuild(Transform visual, int id)
        {
            _sourceId = id;
            if (_clone != null) Destroy(_clone);
            _clone = null;
            if (visual == null) return;
            EnsureStage();
            _stage.gameObject.SetActive(true);
            _clone = Instantiate(visual.gameObject, _stage, false);
            _clone.name = "PortraitModel";
            _clone.transform.localPosition = Vector3.zero;
            _clone.transform.localRotation = Quaternion.identity;
            _clone.transform.localScale = visual.lossyScale;
            foreach (var behaviour in _clone.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in _clone.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach (var t in _clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = StageLayer;
            foreach (var renderer in _clone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
            var animator = _clone.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var p in animator.parameters)
                    if (p.type == AnimatorControllerParameterType.Float && p.name == "Speed") animator.SetFloat("Speed", 0f);
            }
            // Let the Animator settle into its idle pose before the photo.
            _shootInFrames = 3;
        }

        private void Shoot()
        {
            if (_clone == null || _camera == null) return;
            Frame();
            using (StudioLight.Use(_clone.transform)) _camera.Render();
            _image.texture = _texture;
            _image.color = Color.white;
            HasPortrait = true;
            ShotCount++;
            // Nothing on the stage needs to exist between photos.
            _stage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Face-and-shoulders framing measured on the actual head: from the head bone (base of the skull) to the top of
        /// the model. Proportions differ a lot — an adult's head is ~1/7 of the body, the chibi kids' ~1/2 — so the head
        /// size, not the body height, sets the distance; the camera sits at eye level (a camera above the eyes put the
        /// kids' fringe and caps over their eyes).
        /// </summary>
        private void Frame()
        {
            var animator = _clone.GetComponentInChildren<Animator>(true);
            Transform headBone = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            var bounds = new Bounds(_stage.position + Vector3.up, Vector3.zero);
            bool any = false;
            foreach (var r in _clone.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            float height = any ? Mathf.Max(0.5f, bounds.max.y - _stage.position.y) : 1.7f;
            float baseY = headBone != null ? headBone.position.y : bounds.max.y - height * 0.16f;
            float headSize = Mathf.Clamp(bounds.max.y - baseY, height * 0.1f, height * 0.6f);
            // Eyes sit a little under the middle of the head; frame from the shoulders to just above the crown.
            float eyeY = baseY + headSize * 0.42f;
            Vector3 centre = new Vector3(bounds.center.x, baseY + headSize * 0.38f, bounds.center.z);
            float visible = headSize * 1.75f;
            const float fov = 26f;
            float distance = visible * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) + bounds.extents.z * 0.5f;
            var forward = _clone.transform.forward;
            _camera.transform.position = new Vector3(centre.x, eyeY, centre.z) + forward * distance;
            _camera.transform.LookAt(centre);
            _camera.fieldOfView = fov;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = distance + 3f;
        }

        private void EnsureStage()
        {
            if (_stage != null) return;
            _texture = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32) { name = "HudPortraitRT", antiAliasing = 4 };
            _texture.Create();

            _stage = new GameObject("HudPortraitStage").transform;
            DontDestroyOnLoad(_stage.gameObject);
            _stage.position = StagePosition;

            var cameraGo = new GameObject("HudPortraitCamera");
            cameraGo.transform.SetParent(_stage, false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.enabled = false; // rendered manually, once per model
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Backdrop;
            _camera.cullingMask = 1 << StageLayer;
            _camera.targetTexture = _texture;
            _camera.allowHDR = false;
            _camera.allowMSAA = true;

            // Soft key, fill and warm rim light that only touch the stage layer.
            AddLight(new Vector3(0.9f, 2.2f, 1.6f), 5.5f, new Color(1f, 0.95f, 0.88f), 4f);
            AddLight(new Vector3(-1.1f, 1.6f, 1.2f), 2.6f, new Color(0.72f, 0.82f, 1f), 4f);
            AddLight(new Vector3(0f, 2.1f, -1.3f), 3.5f, new Color(1f, 0.82f, 0.6f), 4f);
        }

        private void AddLight(Vector3 local, float intensity, Color color, float range)
        {
            var go = new GameObject("PortraitLight");
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

        private void OnDestroy()
        {
            if (_stage != null) Destroy(_stage.gameObject);
            if (_texture != null) { _texture.Release(); Destroy(_texture); }
        }
    }
}
