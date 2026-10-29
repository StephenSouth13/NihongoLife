using System.Collections.Generic;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NihongoLife.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Renders each career's person (a Kids-family variant in the career's uniform) into a small RenderTexture for the career screen.
    /// The people stand far below the world (nothing else is in range of the stage camera), are lit by two local point lights, and
    /// are animated by hand with unscaled time so they move while the game is paused. Each one idles (the Animator's breathing),
    /// and works its signature gesture while hovered, selected or current, and in a staggered cycle otherwise.
    /// Only avatars whose card is inside the scroll view are drawn, at a capped rate, so the screen stays cheap (also in WebGL).
    /// </summary>
    public sealed class CareerAvatarStage : MonoBehaviour
    {
        public sealed class Avatar
        {
            public string careerId, variantId, gesture, alt;
            public RenderTexture Texture;
            public RawImage Image;
            public RectTransform Clip;
            public bool Hot;
            public int Renders;
            public float WorkSeconds, IdleSeconds;
            public GestureLayer Layer => layer;
            public GameObject Person => go;
            public bool Working => working;
            public float Clock { get => clock; set => clock = value; }

            internal GameObject go;
            internal GestureLayer layer;
            internal Animator animator;
            internal Vector3 center;
            internal Vector3 camPos;
            internal float height;
            internal int slot;
            internal bool working, useAlt;
            internal float clock, nextRender;
        }

        public const float StageY = -3000f;
        public const float Spacing = 8f;
        public const float RenderInterval = 1f / 20f;

        private static CareerAvatarStage _instance;
        private readonly List<Avatar> _avatars = new List<Avatar>();
        private Camera _cam;
        private Light _key, _fill;
        private int _nextSlot;
        private int _rendersThisFrame;

        public static CareerAvatarStage Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("CareerAvatarStage");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<CareerAvatarStage>();
                _instance.Init();
                return _instance;
            }
        }

        public static bool Exists => _instance != null;
        public IReadOnlyList<Avatar> Avatars => _avatars;
        public int RendersLastFrame { get; private set; }
        public bool Paused { get; set; }

        private void Init()
        {
            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.enabled = false;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 14f;
            _cam.fieldOfView = 26f;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _cam.useOcclusionCulling = false;
            _cam.cameraType = CameraType.Game;
            _key = MakeLight("StageKey", new Color(1f, 0.96f, 0.9f), 2.4f);
            _fill = MakeLight("StageFill", new Color(0.7f, 0.8f, 1f), 0.9f);
        }

        private Light MakeLight(string name, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = 9f; l.shadows = LightShadows.None;
            l.enabled = false; // switched on only while rendering an avatar: it must never touch the game world
            return l;
        }

        /// <summary>Makes (or reuses) the avatar of a career and shows it in <paramref name="image"/>; null when the career has no look or the variant is missing.</summary>
        public Avatar Bind(string careerId, RawImage image, RectTransform clip, int width = 384, int height = 276)
        {
            var avatar = _avatars.Find(a => a.careerId == careerId);
            if (avatar == null)
            {
                var look = CareerIndustries.Look(careerId);
                if (look == null) return null;
                avatar = Create(look, width, height);
                if (avatar == null) return null;
            }
            avatar.Image = image; avatar.Clip = clip;
            if (image != null) { image.texture = avatar.Texture; image.color = Color.white; image.raycastTarget = false; }
            return avatar;
        }

        public Avatar Find(string careerId) => _avatars.Find(a => a.careerId == careerId);

        private Avatar Create(CareerLook look, int width, int height)
        {
            int slot = _nextSlot++;
            var pos = new Vector3(slot * Spacing, StageY, 0f);
            var go = CharacterVariants.Spawn(look.variant, pos, 0f, transform);
            if (go == null) return null;
            var animator = go.GetComponentInChildren<Animator>();
            if (animator == null) { Destroy(go); return null; }
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.enabled = false;
            var layer = GestureLayer.On(go);
            layer.Manual = true;
            animator.Update(1f / 60f);

            var avatar = new Avatar { careerId = look.career, variantId = look.variant, gesture = look.gesture, alt = look.alt, go = go, layer = layer, animator = animator, slot = slot };
            avatar.Texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32) { name = "Avatar_" + look.career, antiAliasing = 1, useMipMap = false };
            avatar.Texture.Create();

            // Frame the person (head to feet, with room for the props beside them).
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(pos + Vector3.up, Vector3.zero);
            bool first = true;
            foreach (var r in renderers)
            {
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }
            avatar.height = Mathf.Max(0.8f, bounds.size.y);
            avatar.center = new Vector3(pos.x, bounds.min.y + avatar.height * 0.5f, pos.z);
            float aspect = width / (float)height;
            float vHalf = avatar.height * 0.5f * 1.16f;
            float hHalf = Mathf.Max(avatar.height * 0.5f * 0.78f, 0.62f);
            float dist = Mathf.Max(vHalf / Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad), hHalf / (Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect));
            var dir = Quaternion.Euler(-4f, 24f, 0f) * Vector3.forward; // in front of the person, a little to their left, slightly above
            avatar.camPos = avatar.center + dir * dist;
            avatar.clock = slot * 0.9f;
            avatar.nextRender = 0f;
            _avatars.Add(avatar);
            return avatar;
        }

        /// <summary>Destroys every avatar and its texture (the career window closed).</summary>
        public void Clear()
        {
            foreach (var a in _avatars)
            {
                if (a.go != null) Destroy(a.go);
                if (a.Texture != null) { a.Texture.Release(); Destroy(a.Texture); }
            }
            _avatars.Clear();
            _nextSlot = 0;
        }

        public static void ClearIfAny() { if (_instance != null) _instance.Clear(); }

        /// <summary>Advances every avatar by <paramref name="dt"/> (what Update does with unscaled time; public so a test can step it).</summary>
        public void Step(float dt)
        {
            foreach (var a in _avatars) StepAvatar(a, dt);
        }

        private void StepAvatar(Avatar a, float dt)
        {
            if (a.go == null) return;
            a.clock += dt;
            // Work while hot; otherwise a staggered 2.6 s idle / 3.4 s work cycle so the cards are alive but not all moving at once.
            bool wantWork = a.Hot || (a.clock % 6f) >= 2.6f;
            if (wantWork)
            {
                a.WorkSeconds += dt;
                if (!a.working || !a.layer.IsPlaying)
                {
                    if (a.working && !a.layer.IsPlaying) a.useAlt = !a.useAlt && !string.IsNullOrEmpty(a.alt);
                    a.layer.Play(a.useAlt ? a.alt : a.gesture);
                    a.working = true;
                }
            }
            else
            {
                a.IdleSeconds += dt;
                if (a.working) { a.layer.Stop(0.35f); a.working = false; }
            }
            a.animator.Update(dt);
            a.layer.Step(dt);
        }

        private void Update()
        {
            if (Paused || _avatars.Count == 0) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            RendersLastFrame = 0;
            foreach (var a in _avatars)
            {
                if (a.go == null) continue;
                bool visible = IsVisible(a);
                if (visible) StepAvatar(a, dt);
                if (visible && Time.unscaledTime >= a.nextRender && RendersLastFrame < 3) { RenderAvatar(a); RendersLastFrame++; }
            }
        }

        /// <summary>True when the avatar's image is on screen inside its clip rectangle (so off-screen cards cost nothing).</summary>
        public static bool IsVisible(Avatar a)
        {
            if (a.Image == null || !a.Image.isActiveAndEnabled) return false;
            if (a.Clip == null) return true;
            var corners = new Vector3[4];
            a.Image.rectTransform.GetWorldCorners(corners);
            var r = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
            a.Clip.GetWorldCorners(corners);
            var clip = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
            return r.Overlaps(clip);
        }

        public void RenderAvatar(Avatar a)
        {
            if (a.go == null || a.Texture == null) return;
            a.nextRender = Time.unscaledTime + RenderInterval;
            var t = _cam.transform;
            t.position = a.camPos;
            t.rotation = Quaternion.LookRotation((a.center + Vector3.up * a.height * 0.02f) - a.camPos, Vector3.up);
            _key.transform.position = a.center + new Vector3(1.8f, 1.6f, 2.6f);
            _fill.transform.position = a.center + new Vector3(-2.2f, 0.6f, 2.2f);
            _key.enabled = _fill.enabled = true;
            _cam.targetTexture = a.Texture;
            _cam.Render();
            _cam.targetTexture = null;
            _key.enabled = _fill.enabled = false;
            a.Renders++;
        }
    }
}