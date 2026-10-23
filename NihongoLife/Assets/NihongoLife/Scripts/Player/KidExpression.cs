using NihongoLife.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Player
{
    /// <summary>
    /// Face plate and a short mood bubble for the Kids rigs. The packs ship with almost no facial
    /// blendshapes, so the eyes and mouth are drawn into small textures at runtime (no Blender in this
    /// environment — nothing was authored in Blender). Plates are shared, unlit and transparent.
    /// One plate and one bubble quad per character; textures are shared across everyone.
    /// </summary>
    public sealed class KidExpression : MonoBehaviour
    {
        private static Texture2D[] _textures;
        private static Material[] _materials;
        private const int Face = 64;

        private SocialMood _shown = SocialMood.Neutral;
        private SocialMood _override;
        private float _overrideUntil;
        private float _bubbleUntil;
        private float _nextEmote;
        private Transform _head;
        private Transform _face;
        private Transform _bubble;
        private Renderer _faceRenderer;
        private Renderer _bubbleRenderer;

        public static KidExpression On(GameObject actor)
        {
            if (actor == null) return null;
            return actor.GetComponent<KidExpression>().OrNull() ?? actor.AddComponent<KidExpression>();
        }

        public void Play(SocialMood mood, float seconds = 3.5f)
        {
            _override = mood;
            _overrideUntil = Time.time + Mathf.Max(0.2f, seconds);
            Show(mood, true);
        }

        private void LateUpdate()
        {
            EnsureFace();
            SocialMood mood = _overrideUntil > Time.time ? _override : MoodNow();
            if (mood != _shown) Show(mood, mood != SocialMood.Neutral);
            Place();
            if (_bubble != null) _bubble.gameObject.SetActive(Time.time < _bubbleUntil);
        }

        /// <summary>
        /// A bow / point gesture replaces the body animation, so it only plays while the character is free: not
        /// sleeping or fainted (input locked), sitting, working, or talking. (A mood from studying used to make the
        /// player stand up and point while lying in bed.)
        /// </summary>
        private bool FreeToEmote()
        {
            var seat = GetComponent<NihongoLife.World.SeatedPassenger>().OrNull();
            if (seat != null && seat.IsSitting) return false;
            var player = GetComponent<PlayerController>().OrNull();
            if (player != null && player.InputLocked) return false;
            var animation = GetComponent<CharacterAnimationController>().OrNull();
            if (animation != null && !string.IsNullOrEmpty(animation.CurrentWork)) return false;
            var dialogue = NihongoLife.Dialogue.DialogueManager.Instance;
            return dialogue == null || !dialogue.IsOpen;
        }

        private SocialMood MoodNow()
        {
            if (GetComponent<PlayerController>() != null || GetComponent<PlayerStatus>() != null)
                return SocialStanding.Dominant;
            return _shown;
        }

        private void Show(SocialMood mood, bool flash)
        {
            _shown = mood;
            EnsureFace();
            var material = MaterialFor(mood);
            if (_faceRenderer != null && material != null) _faceRenderer.sharedMaterial = material;
            if (_bubbleRenderer != null && material != null) _bubbleRenderer.sharedMaterial = material;
            if (flash)
            {
                _bubbleUntil = Time.time + 3.2f;
                if (Time.time >= _nextEmote && FreeToEmote())
                {
                    var animation = GetComponent<CharacterAnimationController>();
                    if (animation != null && (mood == SocialMood.Embarrassed || mood == SocialMood.Proud))
                    {
                        animation.TriggerBow();
                        _nextEmote = Time.time + 6f;
                    }
                    else if (animation != null && (mood == SocialMood.Inspired || mood == SocialMood.Confident))
                    {
                        animation.TriggerPoint();
                        _nextEmote = Time.time + 6f;
                    }
                }
            }
        }

        private void EnsureFace()
        {
            if (_face != null) return;
            var animator = GetComponentInChildren<Animator>();
            _head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            _face = Quad("FacePlate", out _faceRenderer);
            _bubble = Quad("MoodBubble", out _bubbleRenderer);
            _bubble.gameObject.SetActive(false);
            Show(_shown, false);
        }

        private Transform Quad(string name, out Renderer renderer)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            var collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = Vector3.one * 0.01f;
            renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
            return quad.transform;
        }

        private void Place()
        {
            Vector3 head = _head != null ? _head.position : transform.position + Vector3.up * 1.35f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            float scale = Mathf.Clamp(transform.lossyScale.y, 0.6f, 1.8f);
            if (_face != null)
            {
                _face.position = head + forward * (0.12f * scale) + Vector3.up * (0.02f * scale);
                _face.rotation = Quaternion.LookRotation(forward, Vector3.up);
                _face.localScale = Vector3.one * (0.16f * scale);
            }
            if (_bubble != null && _bubble.gameObject.activeSelf)
            {
                _bubble.position = head + Vector3.up * (0.34f * scale);
                var cam = Camera.main;
                Vector3 toCam = cam != null ? _bubble.position - cam.transform.position : forward;
                if (toCam.sqrMagnitude < 0.001f) toCam = forward;
                _bubble.rotation = Quaternion.LookRotation(toCam, Vector3.up);
                _bubble.localScale = Vector3.one * (0.22f * scale);
            }
        }

        private static Material MaterialFor(SocialMood mood)
        {
            int index = Mathf.Clamp((int)mood, 0, 8);
            if (_materials != null && _materials[index] != null) return _materials[index];
            Texture2D texture = TextureFor(mood);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Texture");
            if (shader == null || texture == null) return null;
            var material = new Material(shader) { name = "KidFace_" + mood };
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            _materials ??= new Material[9];
            _materials[index] = material;
            return material;
        }

        private static Texture2D TextureFor(SocialMood mood)
        {
            int index = Mathf.Clamp((int)mood, 0, 8);
            _textures ??= new Texture2D[9];
            if (_textures[index] != null) return _textures[index];
            var texture = new Texture2D(Face, Face, TextureFormat.RGBA32, false) { name = "KidFace_" + mood, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[Face * Face];
            DrawFace(pixels, mood);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _textures[index] = texture;
            return texture;
        }

        private static void DrawFace(Color32[] pixels, SocialMood mood)
        {
            Color32 skin = new Color32(255, 224, 196, 230);
            Color32 line = new Color32(55, 40, 36, 255);
            Color32 blush = new Color32(230, 120, 130, 150);
            for (int y = 0; y < Face; y++)
            for (int x = 0; x < Face; x++)
            {
                float dx = (x + 0.5f) / Face - 0.5f;
                float dy = (y + 0.5f) / Face - 0.5f;
                pixels[y * Face + x] = dx * dx + dy * dy < 0.22f ? skin : new Color32(0, 0, 0, 0);
            }
            bool tired = mood == SocialMood.Tired;
            bool angry = mood == SocialMood.Annoyed || mood == SocialMood.Tense;
            Eye(pixels, 22, tired ? 34 : 36, tired, angry, line);
            Eye(pixels, 42, tired ? 34 : 36, tired, angry, line);
            if (mood == SocialMood.Embarrassed || mood == SocialMood.Proud)
            {
                Disc(pixels, 16, 28, 5, blush);
                Disc(pixels, 48, 28, 5, blush);
            }
            Mouth(pixels, mood, line);
        }

        private static void Eye(Color32[] pixels, int cx, int cy, bool tired, bool angry, Color32 ink)
        {
            if (tired)
            {
                Stroke(pixels, cx - 4, cy, cx + 4, cy, ink);
                return;
            }
            Disc(pixels, cx, cy, 3, new Color32(255, 255, 255, 255));
            Disc(pixels, cx, cy, 2, ink);
            Disc(pixels, cx - 1, cy + 1, 1, new Color32(255, 255, 255, 255));
            if (angry) Stroke(pixels, cx - 4, cy + 4, cx + 3, cy + 1, ink);
        }

        private static void Mouth(Color32[] pixels, SocialMood mood, Color32 ink)
        {
            int y = 22;
            switch (mood)
            {
                case SocialMood.Happy:
                case SocialMood.Proud:
                case SocialMood.Inspired:
                    Arc(pixels, 32, y, 8, true, ink);
                    break;
                case SocialMood.Embarrassed:
                    Arc(pixels, 32, y - 1, 5, true, ink);
                    break;
                case SocialMood.Tense:
                case SocialMood.Annoyed:
                    Arc(pixels, 32, y + 4, 7, false, ink);
                    break;
                case SocialMood.Tired:
                    Stroke(pixels, 26, y, 38, y, ink);
                    break;
                case SocialMood.Confident:
                    Stroke(pixels, 26, y + 1, 38, y + 1, ink);
                    Disc(pixels, 38, y + 1, 1, ink);
                    break;
                default:
                    Stroke(pixels, 27, y, 37, y, ink);
                    break;
            }
        }

        private static void Arc(Color32[] pixels, int cx, int cy, int radius, bool smile, Color32 ink)
        {
            for (int i = -radius; i <= radius; i++)
            {
                float t = i / (float)radius;
                int y = cy + Mathf.RoundToInt((smile ? 1f : -1f) * (1f - t * t) * radius * 0.55f);
                Plot(pixels, cx + i, y, ink);
                Plot(pixels, cx + i, y + 1, ink);
            }
        }

        private static void Disc(Color32[] pixels, int cx, int cy, int radius, Color32 color)
        {
            int r2 = radius * radius;
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= r2) Plot(pixels, cx + x, cy + y, color);
        }

        private static void Stroke(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                Plot(pixels, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), color);
            }
        }

        private static void Plot(Color32[] pixels, int x, int y, Color32 color)
        {
            if ((uint)x >= Face || (uint)y >= Face) return;
            int i = y * Face + x;
            if (color.a >= 250 || pixels[i].a == 0) { pixels[i] = color; return; }
            byte a = color.a;
            pixels[i] = new Color32(
                (byte)((color.r * a + pixels[i].r * (255 - a)) / 255),
                (byte)((color.g * a + pixels[i].g * (255 - a)) / 255),
                (byte)((color.b * a + pixels[i].b * (255 - a)) / 255),
                (byte)Mathf.Max(pixels[i].a, a));
        }
    }

    /// <summary>Attaches a face plate to humanoids already in the scene. Skips menus and caps the count for WebGL.</summary>
    public sealed class KidExpressionCrowd : MonoBehaviour
    {
        private const int Cap = 24;

        private void OnEnable() => SceneManager.sceneLoaded += OnScene;
        private void OnDisable() => SceneManager.sceneLoaded -= OnScene;
        private void Start() => Attach(SceneManager.GetActiveScene());

        private void OnScene(Scene scene, LoadSceneMode mode) => Attach(scene);

        private static void Attach(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            string name = scene.name;
            if (name.Contains("Menu") || name.Contains("Bootstrap")) return;
            int added = 0;
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (added >= Cap || animator == null || !animator.isHuman) continue;
                if (animator.gameObject.layer == 30 || animator.transform.position.y < -200f) continue; // HUD portrait stage
                Transform host = animator.transform;
                var controller = animator.GetComponentInParent<CharacterAnimationController>();
                if (controller != null) host = controller.transform;
                if (host.GetComponent<KidExpression>() != null) continue;
                string label = host.name + animator.name;
                bool kid = label.IndexOf("kid", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("NL_", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("Commuter", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || label.IndexOf("Passenger", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || controller != null;
                if (!kid) continue;
                host.gameObject.AddComponent<KidExpression>();
                added++;
            }
        }
    }
}
