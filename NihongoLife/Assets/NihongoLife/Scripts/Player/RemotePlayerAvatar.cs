using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Interaction;

namespace NihongoLife.Player
{
    /// <summary>
    /// Another player in the world: their own character model (PlayableCharacterCatalog), a name plate, and smooth
    /// movement. Poses arrive ~6 times a second; the avatar is drawn <see cref="InterpolationDelay"/> behind the
    /// newest one and interpolates between the two samples around that time, so it glides instead of stepping, and
    /// briefly extrapolates when a packet is late. Press F near them for the social card (message, friend, group).
    /// </summary>
    public class RemotePlayerAvatar : MonoBehaviour, IInteractable
    {
        /// <summary>Default delay; the avatar widens it when poses arrive less often (busy shard, rate limit).</summary>
        public const float InterpolationDelay = 0.25f;
        private float _delay = InterpolationDelay;
        private float _avgGap = 1f / 6f;

        public string PlayerId { get; private set; }
        public string DisplayName { get; private set; }
        public string SceneName { get; private set; }
        public string CharacterId { get; private set; }
        public bool VisualVisible { get; private set; } = true;
        public int BufferedSamples => _samples.Count;

        private struct Sample { public float t; public Vector3 p; public float yaw; public int move; }
        private readonly List<Sample> _samples = new List<Sample>(24);

        private TextMeshPro _nameTag;
        private CharacterAnimationController _anim;
        private Vector3 _lastPosition;
        private float _speed;
        private GameObject _fallback;
        private static readonly Color NameTagColor = new Color(0.86f, 0.97f, 1f, 1f);

        // ──────────────────────── Setup ────────────────────────

        public void Setup(OnlinePlayerSnapshot snapshot)
        {
            PlayerId = snapshot.playerId;
            gameObject.layer = 0;
            var trigger = gameObject.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true; // F can find them; nobody bumps into them
            trigger.center = new Vector3(0f, 0.85f, 0f);
            trigger.height = 1.7f;
            trigger.radius = 0.35f;
            CreateNameTag();
            ApplyCharacter(snapshot.characterId); // even before presence says which one: default model, swapped later
            UpdateFromSnapshot(snapshot);
            transform.SetPositionAndRotation(snapshot.position, snapshot.rotation);
            _lastPosition = transform.position;
        }

        public void UpdateFromSnapshot(OnlinePlayerSnapshot snapshot)
        {
            DisplayName = string.IsNullOrWhiteSpace(snapshot.displayName) ? "Learner" : snapshot.displayName;
            SceneName = snapshot.sceneName;
            if (!string.IsNullOrEmpty(snapshot.characterId) && snapshot.characterId != CharacterId) ApplyCharacter(snapshot.characterId);
            if (_nameTag != null) _nameTag.text = NameLabel();

            float now = Time.unscaledTime;
            var s = new Sample { t = now, p = snapshot.position, yaw = snapshot.rotation.eulerAngles.y, move = snapshot.moveState };
            if (_samples.Count > 0)
            {
                var last = _samples[_samples.Count - 1];
                if ((s.p - last.p).sqrMagnitude < 0.0001f && Mathf.Abs(Mathf.DeltaAngle(s.yaw, last.yaw)) < 0.5f && s.move == last.move)
                {
                    // Idle keep-alive: refresh the time so we do not extrapolate, but do not grow the buffer.
                    last.t = now;
                    _samples[_samples.Count - 1] = last;
                    return;
                }
                if ((s.p - last.p).sqrMagnitude > 64f) _samples.Clear(); // teleport (door, respawn): snap
            }
            if (_samples.Count > 0 && s.move != 0)
            {
                float gap = now - _samples[_samples.Count - 1].t;
                if (gap < 2f) _avgGap = Mathf.Lerp(_avgGap, gap, 0.2f);
                _delay = Mathf.Clamp(_avgGap * 1.6f, 0.15f, 0.9f);
            }
            _samples.Add(s);
            if (_samples.Count > 20) _samples.RemoveAt(0);
            if (_samples.Count == 1) transform.SetPositionAndRotation(s.p, Quaternion.Euler(0f, s.yaw, 0f));
        }

        private string NameLabel()
        {
            string star = GameServices.TryGet(out SocialHub hub) && hub.IsFriend(PlayerId) ? "<color=#FFD45C>★</color> " : string.Empty;
            return star + DisplayName;
        }

        private void ApplyCharacter(string characterId)
        {
            CharacterId = characterId;
            var character = PlayableCharacterCatalog.FindById(string.IsNullOrEmpty(characterId) ? PlayableCharacterCatalog.DefaultId : characterId);
            _anim = GetComponent<CharacterAnimationController>() ?? gameObject.AddComponent<CharacterAnimationController>();
            if (PlayableCharacterCatalog.ApplyVisual(gameObject, character))
            {
                if (_fallback != null) Destroy(_fallback);
                var animator = GetComponentInChildren<Animator>(true);
                // Off-screen remote players skip bone updates — many avatars stay cheap.
                if (animator != null) animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                    if (r != null && r.GetComponent<TextMeshPro>() == null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else if (_fallback == null)
            {
                _fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _fallback.name = "Visual";
                Destroy(_fallback.GetComponent<Collider>());
                _fallback.transform.SetParent(transform, false);
                _fallback.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                _fallback.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
            }
            if (_nameTag != null) _nameTag.transform.localPosition = new Vector3(0f, NameHeight(), 0f);
            SetVisualVisible(VisualVisible);
        }

        private float NameHeight()
        {
            var visual = transform.Find("Visual");
            if (visual == null) return 2.0f;
            bool any = false;
            var b = new Bounds(transform.position, Vector3.zero);
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? Mathf.Clamp(b.max.y - transform.position.y + 0.25f, 1.2f, 2.6f) : 2.0f;
        }

        // ──────────────────────── Update ────────────────────────

        private void Update()
        {
            if (_samples.Count > 0)
            {
                float renderTime = Time.unscaledTime - _delay;
                Vector3 pos; float yaw;
                if (_samples.Count == 1 || renderTime <= _samples[0].t)
                {
                    pos = _samples[0].p; yaw = _samples[0].yaw;
                }
                else
                {
                    int i = _samples.Count - 1;
                    while (i > 0 && _samples[i - 1].t > renderTime) i--;
                    var b = _samples[i];
                    var a = _samples[Mathf.Max(0, i - 1)];
                    if (renderTime >= b.t)
                    {
                        // Late packet: keep going the way they were moving, for at most a quarter second.
                        var prev = _samples.Count >= 2 ? _samples[_samples.Count - 2] : b;
                        float dt = Mathf.Max(0.001f, b.t - prev.t);
                        Vector3 velocity = b.move == 0 ? Vector3.zero : (b.p - prev.p) / dt;
                        pos = b.p + velocity * Mathf.Min(renderTime - b.t, 0.25f);
                        yaw = b.yaw;
                    }
                    else
                    {
                        float k = Mathf.InverseLerp(a.t, b.t, renderTime);
                        pos = Vector3.Lerp(a.p, b.p, k);
                        yaw = Mathf.LerpAngle(a.yaw, b.yaw, k);
                    }
                    // Drop samples that are fully in the past.
                    while (_samples.Count > 2 && _samples[1].t < renderTime) _samples.RemoveAt(0);
                }
                transform.position = pos;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 1f - Mathf.Exp(-14f * Time.deltaTime));
            }

            float frameSpeed = Time.deltaTime > 0f ? Vector3.ProjectOnPlane(transform.position - _lastPosition, Vector3.up).magnitude / Time.deltaTime : 0f;
            _speed = Mathf.Lerp(_speed, frameSpeed, 1f - Mathf.Exp(-10f * Time.deltaTime));
            _lastPosition = transform.position;
            if (_anim != null && VisualVisible) _anim.SetSpeed(_speed < 0.08f ? 0f : _speed);

            var cam = Camera.main;
            if (_nameTag != null && cam != null && VisualVisible)
            {
                _nameTag.transform.rotation = Quaternion.LookRotation(_nameTag.transform.position - cam.transform.position);
                float d = Vector3.Distance(cam.transform.position, transform.position);
                _nameTag.alpha = Mathf.Clamp01(1.4f - d / 28f);
            }
        }

        /// <summary>RemotePlayerManager hides far avatars beyond the visible cap.</summary>
        public void SetVisualVisible(bool visible)
        {
            VisualVisible = visible;
            var visual = transform.Find("Visual");
            if (visual != null && visual.gameObject.activeSelf != visible) visual.gameObject.SetActive(visible);
            if (_nameTag != null) _nameTag.gameObject.SetActive(visible);
        }

        private void CreateNameTag()
        {
            var go = new GameObject("NameTag");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            go.transform.localScale = Vector3.one * 0.07f;
            _nameTag = go.AddComponent<TextMeshPro>();
            var font = NihongoLife.UI.NLUi.ResolveFont(); // names are Vietnamese / Japanese
            if (font != null) _nameTag.font = font;
            _nameTag.fontSize = 34f;
            _nameTag.alignment = TextAlignmentOptions.Center;
            _nameTag.color = NameTagColor;
            _nameTag.outlineWidth = 0.22f;
            _nameTag.outlineColor = new Color32(10, 30, 50, 200);
            _nameTag.enableAutoSizing = false;
            _nameTag.rectTransform.sizeDelta = new Vector2(40f, 6f);
        }

        // ──────────────────────── IInteractable ────────────────────────

        public string GetPromptJa() => $"{DisplayName} と交流する";
        public string GetpromptEn() => "Gặp người chơi này — nhắn tin, kết bạn, mời nhóm";
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            NihongoLife.UI.SocialChatWindow.ShowPlayerCard(PlayerId, DisplayName);
        }
    }
}
