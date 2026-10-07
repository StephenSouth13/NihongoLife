using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// Keeps the player's back straight. The Mixamo idle/walk clips bend the spine sideways and roll the
    /// hips a lot, which reads as a slouchy, swaying walk. After the Animator has posed the rig, each spine
    /// segment (hips → spine → chest → neck) is pulled part of the way back to the character's vertical axis.
    /// Legs, arms and the step cycle are untouched.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class PostureStabilizer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float hips = 0f; // legs hang off the hips: rotating them would slide the feet
        [SerializeField, Range(0f, 1f)] private float spine = 0.6f;
        [SerializeField, Range(0f, 1f)] private float chest = 0.5f;
        [SerializeField, Range(0f, 1f)] private float neck = 0.35f;
        [SerializeField, Range(0f, 1f)] private float keepForwardLean = 0.6f;

        private Animator _animator;
        private Transform _hips, _spine, _chest, _neck, _head;

        public bool IsActive => _hips != null && _spine != null;

        private void LateUpdate()
        {
            if (!Resolve()) return;
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Sit") || state.IsName("Lay")) return; // seated / lying poses stay as authored
            Vector3 up = transform.up;
            Straighten(_hips, _spine, up, hips);
            Straighten(_spine, _chest != null ? _chest : _neck, up, spine);
            if (_chest != null) Straighten(_chest, _neck, up, chest);
            if (_neck != null) Straighten(_neck, _head, up, neck);
        }

        /// <summary>Rotates <paramref name="bone"/> so the direction to <paramref name="child"/> moves toward
        /// the character's up axis by <paramref name="amount"/>; most of a natural forward lean is kept.</summary>
        private void Straighten(Transform bone, Transform child, Vector3 up, float amount)
        {
            if (bone == null || child == null || amount <= 0f) return;
            Vector3 dir = child.position - bone.position;
            if (dir.sqrMagnitude < 1e-6f) return;
            dir.Normalize();
            // Split the deviation into sideways (removed strongly) and forward lean (mostly kept).
            Vector3 forward = transform.forward;
            float lean = Vector3.Dot(dir, forward);
            Vector3 target = (up + forward * lean * keepForwardLean).normalized;
            Vector3 corrected = Vector3.Slerp(dir, target, amount);
            bone.rotation = Quaternion.FromToRotation(dir, corrected) * bone.rotation;
        }

        private bool Resolve()
        {
            if (_animator != null && _animator.isActiveAndEnabled && _hips != null) return true;
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null || !_animator.isHuman || _animator.avatar == null) return false;
            _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _chest = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (_chest == null) _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _neck = _animator.GetBoneTransform(HumanBodyBones.Neck);
            _head = _animator.GetBoneTransform(HumanBodyBones.Head);
            return _hips != null && _spine != null;
        }
    }
}
