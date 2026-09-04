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

        private int _speedHash;
        private int _isTalkingHash;
        private int _bowHash;
        private int _pointHash;

        private void Awake()
        {
            _speedHash = Animator.StringToHash(speedParam);
            _isTalkingHash = Animator.StringToHash(isTalkingParam);
            _bowHash = Animator.StringToHash(bowParam);
            _pointHash = Animator.StringToHash(pointParam);
        }

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void SetSpeed(float speed)
        {
            if (_animator != null && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetFloat(_speedHash, speed, speedDampTime, Time.deltaTime);
            }
        }

        public void SetTalking(bool isTalking)
        {
            if (_animator != null && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetBool(_isTalkingHash, isTalking);
            }
        }

        public void TriggerBow()
        {
            if (_animator != null && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetTrigger(_bowHash);
            }
        }

        public void TriggerPoint()
        {
            if (_animator != null && _animator.gameObject.activeInHierarchy)
            {
                _animator.SetTrigger(_pointHash);
            }
        }
    }
}
