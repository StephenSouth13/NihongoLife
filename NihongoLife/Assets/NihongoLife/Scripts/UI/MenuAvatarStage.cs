using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// The chosen character on the main menu, alive: breathing idle, a bow, a point and a chat gesture in turn while it slowly
    /// turns toward the buttons. A real 3D model in the menu scene (no picture), standing on the right of the screen, lit by its
    /// own warm light. Follows the character picked in "Choose character". Created by <see cref="MenuAvatarBootstrap"/>.
    /// </summary>
    public sealed class MenuAvatarStage : MonoBehaviour
    {
        private const float ScreenHeightShare = 0.62f;
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

        private void Start() { Rebuild(); }

        private void Update()
        {
            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + 0.7f;
                if (_shownId != PlayableCharacterCatalog.GetSelected().Id) Rebuild();
            }
            if (_figure == null) return;
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
        }

        private void Rebuild()
        {
            if (_figure != null) Destroy(_figure);
            var camera = Camera.main;
            var character = PlayableCharacterCatalog.GetSelected();
            var prefab = PlayableCharacterCatalog.LoadPrefab(character);
            if (camera == null || prefab == null) return;
            _shownId = character.Id;
            _figure = Instantiate(prefab, transform);
            _figure.name = "MenuAvatar_" + character.Id;
            foreach (var c in _figure.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var b in _figure.GetComponentsInChildren<MonoBehaviour>(true))
                if (b != null && !(b is CharacterAnimationController) && b.GetType().Namespace != null && b.GetType().Namespace.StartsWith("NihongoLife.Player")) Destroy(b);
            foreach (var rb in _figure.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
            var cc = _figure.GetComponent<CharacterController>(); if (cc != null) Destroy(cc);
            _anim = _figure.GetComponent<CharacterAnimationController>();
            if (_anim == null) { _anim = _figure.AddComponent<CharacterAnimationController>(); _anim.SetAnimator(_figure.GetComponentInChildren<Animator>(true)); }
            Place(camera);
            var lightGo = new GameObject("MenuAvatarLight");
            lightGo.transform.SetParent(_figure.transform, false);
            lightGo.transform.position = _anchor + (camera.transform.position - _anchor).normalized * 1.8f + Vector3.up * 1.8f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point; light.range = 6f; light.intensity = 2.2f; light.color = new Color(1f, 0.93f, 0.82f);
            _nextBeat = Time.unscaledTime + 1f;
        }
    }
}
