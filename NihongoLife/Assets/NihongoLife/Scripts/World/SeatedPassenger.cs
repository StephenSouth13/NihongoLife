using System.Collections;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Commuter on a carriage bench. Plays Sit, then each frame after the animator poses the rig
    /// shifts the visual so the hips land on the cushion and no bone sits through the wall.
    /// The offset is on the visual child; root motion stays off so the seat transform does not walk away.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class SeatedPassenger : MonoBehaviour
    {
        [SerializeField] private bool alwaysOnBoard;

        [SerializeField] private int ambientRole;

        private Animator _animator;
        private Transform _visual;
        private Vector3 _visualBase;
        private bool _visualCached;
        private string _rigLabel = "";
        private int _poseVersion;
        private bool _sitting = true;

        /// <summary>1 = the older ambient rider who is offered a seat. Story talkers stay seated either way.</summary>
        public bool IsAmbientElder => ambientRole == 1;

        /// <summary>Passengers the player can talk to stay on every ride; the rest are shuffled per trip.</summary>
        public bool AlwaysOnBoard => alwaysOnBoard;

        public bool IsSitting => _sitting;

        public void Configure(bool keepOnEveryRide) => alwaysOnBoard = keepOnEveryRide;

        /// <summary>Stand up or sit back down. Standing restores the visual so the feet match the root.</summary>
        public void SetSitting(bool sitting)
        {
            _poseVersion++;
            _sitting = sitting;
            CacheVisual();
            var animation = GetComponent<CharacterAnimationController>();
            if (animation != null) animation.SetSitting(sitting);
            if (!sitting) ReleasePin();
            else FitTalkCollider();
        }

        private void OnEnable()
        {
            _sitting = true;
            _poseVersion++;
            CacheVisual();
            ReleasePin();
            FitTalkCollider();
            StartCoroutine(SitNextFrame(_poseVersion));
        }

        private IEnumerator SitNextFrame(int version)
        {
            yield return null; // the animator must be initialised before CrossFade
            if (version != _poseVersion || !_sitting) yield break;
            var animation = GetComponent<CharacterAnimationController>();
            if (animation != null) animation.SetSitting(true);
            yield return null; // one pose evaluation so the hip bone is in the sit clip
            if (version != _poseVersion || !_sitting) yield break;
            Pin();
        }

        private void LateUpdate()
        {
            if (!_sitting || !isActiveAndEnabled) return;
            Pin();
        }

        private void Pin()
        {
            CacheVisual();
            if (_visual == null || _visual == transform) return;
            _animator = _animator != null ? _animator : GetComponentInChildren<Animator>(true);
            Transform hips = _animator != null && _animator.isHuman ? _animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (hips == null) return;

            Vector3 hipLocal = transform.InverseTransformPoint(hips.position);
            Vector3 target = SeatAnchor.HipTarget(_visual.lossyScale.y, _rigLabel);
            target.z = SeatAnchor.SolvedHipZ(hipLocal.z, RearBoneZ(hipLocal.z), target.z);
            target = SeatAnchor.ClampHip(target);
            Vector3 delta = SeatAnchor.Correction(hipLocal, target);
            if (delta.sqrMagnitude < 0.0000001f) return;
            delta = Vector3.ClampMagnitude(delta, 0.75f);
            _visual.localPosition += delta;
            Vector3 fromBase = _visual.localPosition - _visualBase;
            if (fromBase.sqrMagnitude > 1.21f) _visual.localPosition = _visualBase + Vector3.ClampMagnitude(fromBase, 1.1f);
        }

        private float RearBoneZ(float hipZ)
        {
            if (_animator == null || !_animator.isHuman) return hipZ;
            float rear = hipZ;
            Consider(HumanBodyBones.Head, ref rear);
            Consider(HumanBodyBones.Hips, ref rear);
            Consider(HumanBodyBones.Spine, ref rear);
            Consider(HumanBodyBones.LeftFoot, ref rear);
            Consider(HumanBodyBones.RightFoot, ref rear);
            Consider(HumanBodyBones.LeftLowerLeg, ref rear);
            Consider(HumanBodyBones.RightLowerLeg, ref rear);
            return rear;
        }

        private void Consider(HumanBodyBones bone, ref float rear)
        {
            Transform joint = _animator.GetBoneTransform(bone);
            if (joint == null) return;
            float z = transform.InverseTransformPoint(joint.position).z;
            if (z < rear) rear = z;
        }

        private void CacheVisual()
        {
            if (_visualCached && _visual != null) return;
            _animator = GetComponentInChildren<Animator>(true);
            _visual = _animator != null ? _animator.transform : transform.Find("Visual");
            if (_visual == transform) _visual = transform.Find("Visual");
            if (_visual != null && _visual != transform)
            {
                _visualBase = _visual.localPosition;
                _visualCached = true;
                _rigLabel = transform.name;
                var joints = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < joints.Length; i++) _rigLabel += " " + joints[i].name;
                var mesh = GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (mesh != null && mesh.sharedMesh != null) _rigLabel += " " + mesh.sharedMesh.name;
                if (_animator != null && _animator.avatar != null) _rigLabel += " " + _animator.avatar.name;
            }
        }

        private void ReleasePin()
        {
            CacheVisual();
            if (_visual != null && _visual != transform) _visual.localPosition = _visualBase;
        }

        private void FitTalkCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            box.isTrigger = true;
            box.center = SeatAnchor.TalkCenter;
            box.size = SeatAnchor.TalkSize;
        }
    }
}
