using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// Keeps a Kid's soles on the floor. The Kids clips lower the hips a little below the model's own pose, so the
    /// shoes sank 2–3.5 cm into the ground. While the character stands idle (not sitting, lying or working), this
    /// measures the lowest point of the skinned meshes against the character's root (which stands on the floor) and
    /// eases the model up or down until they meet. The offset is kept while walking, sitting or lying.
    /// Added to the player, other online players and NPCs.
    /// </summary>
    public sealed class FootGrounder : MonoBehaviour
    {
        private const float MaxOffset = 0.15f;
        private Transform _model;
        private Animator _animator;
        private SkinnedMeshRenderer[] _skins;
        private float _baseY, _offset, _nextCheck;
        private NihongoLife.Core.CharacterAnimationController _anim;

        public float Offset => _offset;
        /// <summary>Soles above the root, metres, at the last idle check (≈0 once settled).</summary>
        public float LastGap { get; private set; }

        private float _nextBind;

        private void LateUpdate()
        {
            // The model only changes when the character is swapped: re-check it a few times a second, at once if lost.
            if (_animator == null || Time.unscaledTime >= _nextBind)
            {
                _nextBind = Time.unscaledTime + 0.5f;
                var animator = GetComponentInChildren<Animator>();
                if (animator != _animator) Bind(animator);
            }
            if (_model == null || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.2f;
            if (!IsStandingIdle()) return;

            float soles = float.MaxValue;
            foreach (var s in _skins) if (s != null && s.enabled && s.gameObject.activeInHierarchy) soles = Mathf.Min(soles, s.bounds.min.y);
            if (soles == float.MaxValue) return;
            // A model placed higher on purpose (staff on a step behind a counter) keeps that height.
            LastGap = soles - (transform.position.y + _baseY * _model.parent.lossyScale.y);
            if (Mathf.Abs(LastGap) < 0.005f) return;
            _offset = Mathf.Clamp(_offset - LastGap * 0.6f, -MaxOffset, MaxOffset);
            var p = _model.localPosition;
            p.y = _baseY + _offset / Mathf.Max(0.001f, _model.parent.lossyScale.y);
            _model.localPosition = p;
        }

        private void Bind(Animator animator)
        {
            _animator = animator;
            _model = null;
            if (animator == null) return;
            // The direct child of this character that holds the model (Visual for the player, Model for NPCs).
            Transform t = animator.transform;
            while (t.parent != null && t.parent != transform) t = t.parent;
            if (t.parent != transform) return;
            _model = t;
            _baseY = t.localPosition.y;
            _offset = 0f;
            _skins = t.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _anim = GetComponent<NihongoLife.Core.CharacterAnimationController>();
        }

        private bool IsStandingIdle()
        {
            if (_animator == null || !_animator.isActiveAndEnabled) return false;
            if (_anim != null && _anim.CurrentWork != null) return false;
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (_animator.IsInTransition(0) || !info.IsName("Idle")) return false;
            // On the floor: the root must actually stand on something right below it.
            return Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, 0.45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
