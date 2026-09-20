using UnityEngine;

namespace NihongoLife.Core
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        
        [Header("Parameters")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string isTalkingParam = "IsTalking";
        [SerializeField] private string bowParam = "Bow";
        [SerializeField] private string pointParam = "Point";
        [SerializeField] private float speedDampTime = 0.12f;
        [SerializeField] private float fullSpeedReference = 1.15f;
        [SerializeField] private bool enableProceduralPresentationOnAnimatedRig = false;
        [SerializeField] private float idleBreathAmount = 0.012f;
        [SerializeField] private float walkBobAmount = 0.025f;
        [SerializeField] private float lookAtWeight = 0.42f;

        private int _speedHash;
        private int _isTalkingHash;
        private int _bowHash;
        private int _pointHash;
        private Transform _visualRoot;
        private Transform _head;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation = Quaternion.identity;
        private Transform _lookTarget;
        private float _currentSpeed;
        private float _gestureTimer;
        private float _phaseOffset;
        private bool _isTalking;
        private bool _hasSpeed;
        private bool _hasTalking;
        private bool _hasBow;
        private bool _hasPoint;
        private bool _rigWarningLogged;

        private void Awake()
        {
            _speedHash = Animator.StringToHash(speedParam);
            _isTalkingHash = Animator.StringToHash(isTalkingParam);
            _bowHash = Animator.StringToHash(bowParam);
            _pointHash = Animator.StringToHash(pointParam);
            _phaseOffset = Random.Range(0f, 10f);
            CacheRig();
            ValidateAnimator();
        }

        private void LateUpdate()
        {
            ApplyPresentationMotion();
            ApplyHeadLook();
            TickConversationGestures();
        }

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _animator.updateMode = AnimatorUpdateMode.Normal;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            CacheRig();
            ValidateAnimator();
        }

        public void SetSpeed(float speed)
        {
            _currentSpeed = Mathf.Max(0f, speed);
            if (_animator != null && _hasSpeed && _animator.gameObject.activeInHierarchy)
            {
                float normalizedSpeed = Mathf.Clamp01(_currentSpeed / Mathf.Max(0.01f, fullSpeedReference));
                _animator.SetFloat(_speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
            }
        }

        public void SetTalking(bool isTalking)
        {
            _isTalking = isTalking;
            if (isTalking)
            {
                _gestureTimer = Random.Range(0.4f, 1.3f);
            }

            if (_animator != null && _hasTalking && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetBool(_isTalkingHash, isTalking);
            }
        }

        public void SetLookTarget(Transform target)
        {
            _lookTarget = target;
        }

        public void TriggerBow()
        {
            if (_animator != null && _hasBow && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetTrigger(_bowHash);
            }
        }

        public void TriggerPoint()
        {
            if (_animator != null && _hasPoint && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetTrigger(_pointHash);
            }
        }

        private void CacheRig()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            _visualRoot = _animator != null ? _animator.transform : transform.Find("Visual");
            if (_visualRoot != null)
            {
                _baseLocalPosition = _visualRoot.localPosition;
                _baseLocalRotation = _visualRoot.localRotation;
            }

            _head = null;
            if (_animator != null && _animator.isHuman)
            {
                _head = _animator.GetBoneTransform(HumanBodyBones.Head);
            }
        }

        private void ValidateAnimator()
        {
            _hasSpeed = false;
            _hasTalking = false;
            _hasBow = false;
            _hasPoint = false;

            // Runtime-spawned actors receive their visual immediately after this component
            // is created, so a temporarily missing Animator is a valid initialization state.
            if (_animator == null) return;

            if (_animator.runtimeAnimatorController == null)
            {
                if (!_rigWarningLogged)
                {
                    Debug.LogError($"[CharacterAnimation] '{name}' has no usable Animator Controller.", this);
                    _rigWarningLogged = true;
                }
                return;
            }

            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == _speedHash && parameter.type == AnimatorControllerParameterType.Float) _hasSpeed = true;
                else if (parameter.nameHash == _isTalkingHash && parameter.type == AnimatorControllerParameterType.Bool) _hasTalking = true;
                else if (parameter.nameHash == _bowHash && parameter.type == AnimatorControllerParameterType.Trigger) _hasBow = true;
                else if (parameter.nameHash == _pointHash && parameter.type == AnimatorControllerParameterType.Trigger) _hasPoint = true;
            }

            if (!_hasSpeed || !_hasTalking || !_hasBow || !_hasPoint)
            {
                Debug.LogError($"[CharacterAnimation] '{name}' controller is missing one or more required parameters: Speed, IsTalking, Bow, Point.", this);
            }
        }

        private void ApplyPresentationMotion()
        {
            if (_visualRoot == null) return;
            if (_animator != null && !enableProceduralPresentationOnAnimatedRig) return;

            float time = Time.time + _phaseOffset;
            float idle = Mathf.Clamp01(1f - _currentSpeed);
            float walk = Mathf.Clamp01(_currentSpeed / 2.5f);
            float breath = Mathf.Sin(time * 1.55f) * idleBreathAmount * idle;
            float bob = Mathf.Abs(Mathf.Sin(time * 6.4f)) * walkBobAmount * walk;
            float talkSway = _isTalking ? Mathf.Sin(time * 3.2f) * 0.9f : 0f;

            _visualRoot.localPosition = _baseLocalPosition + Vector3.up * (breath + bob);
            _visualRoot.localRotation = _baseLocalRotation * Quaternion.Euler(0f, talkSway, Mathf.Sin(time * 1.1f) * 0.45f * idle);
        }

        private void ApplyHeadLook()
        {
            if (!_isTalking || _head == null || _lookTarget == null) return;

            Vector3 direction = _lookTarget.position + Vector3.up * 1.55f - _head.position;
            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _head.rotation = Quaternion.Slerp(_head.rotation, targetRotation, lookAtWeight * Time.deltaTime * 6f);
        }

        private void TickConversationGestures()
        {
            if (!_isTalking || _animator == null || !_animator.gameObject.activeInHierarchy) return;

            _gestureTimer -= Time.deltaTime;
            if (_gestureTimer > 0f) return;

            if (_hasPoint && Random.value > 0.35f)
            {
                TriggerPoint();
            }
            else if (_hasBow)
            {
                TriggerBow();
            }

            _gestureTimer = Random.Range(3.2f, 6.8f);
        }
    }
}
