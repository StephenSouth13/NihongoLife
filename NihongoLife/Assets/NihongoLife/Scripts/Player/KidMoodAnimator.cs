using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Player
{
    /// <summary>
    /// Lets a Kid's body show the state of the player: the real run cycle when sprinting, a shuffling tired walk when
    /// sleepy or out of energy, a limping walk when health is low, and a proper faint when collapsing
    /// (StatusConsequences). It swaps the clips behind the Walk / Idle / Lay states on a per-character override,
    /// so the shared NL_Humanoid controller and every transition stay as they are.
    /// Added by PlayableCharacterCatalog.ApplyVisual to the player and to other online players (run only).
    /// </summary>
    public sealed class KidMoodAnimator : MonoBehaviour
    {
        public enum Mood { Normal, Run, Tired, Hurt, Faint }

        public Mood Current { get; private set; } = Mood.Normal;

        private Animator _animator;
        private AnimatorOverrideController _overrides;
        private KidAnimationSet.Gender _clips;
        private KidAnimationSet _set;
        private PlayerController _player;
        private StatusConsequences _consequences;
        private Vector3 _lastPosition;
        private float _speed, _nextCheck;

        private float _nextBind;

        private void Update()
        {
            if (_animator == null || Time.unscaledTime >= _nextBind) // a new model (character changed)
            {
                _nextBind = Time.unscaledTime + 0.5f;
                var animator = GetComponentInChildren<Animator>();
                if (animator != _animator) Bind(animator);
            }
            if (_overrides == null) return;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            _speed = Mathf.Lerp(_speed, Vector3.ProjectOnPlane(transform.position - _lastPosition, Vector3.up).magnitude / dt, 0.2f);
            _lastPosition = transform.position;

            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.15f;
            Mood mood = Decide();
            if (mood != Current) Apply(mood);
        }

        private void Bind(Animator animator)
        {
            _animator = animator;
            _overrides = null;
            Current = Mood.Normal;
            _set = KidAnimationSet.Load();
            if (_set == null || animator == null || animator.runtimeAnimatorController == null) return;
            var source = animator.runtimeAnimatorController;
            if (source.name.IndexOf("Kid", System.StringComparison.OrdinalIgnoreCase) < 0) return; // not a Kid rig
            _clips = source.name.IndexOf("Girl", System.StringComparison.OrdinalIgnoreCase) >= 0 ? _set.girl : _set.boy;
            if (animator.GetComponent<KidAnimationEvents>() == null) animator.gameObject.AddComponent<KidAnimationEvents>();
            _overrides = new AnimatorOverrideController(source) { name = source.name }; // same name: code and tests read it to know the rig
            animator.runtimeAnimatorController = _overrides;
            _player = GetComponent<PlayerController>();
            _consequences = GetComponent<StatusConsequences>().OrNull() ?? SceneRefs.Find<StatusConsequences>();
            Apply(Mood.Normal); // spell the Kid walk/idle out on this layer too, so later swaps always restore them
        }

        private Mood Decide()
        {
            if (_player == null) return _speed > 3.6f ? Mood.Run : Mood.Normal; // other online players: run only
            if (_consequences != null && _consequences.IsBusy) return Mood.Faint;
            if (_player.IsRunning) return Mood.Run;
            var status = PlayerStatus.Instance;
            if (status == null) return Mood.Normal;
            if (status.CurrentHealth <= status.MaxHealth * 0.25f) return Mood.Hurt;
            if (status.Sleepiness >= 85f || status.CurrentEnergy <= 10f) return Mood.Tired;
            return Mood.Normal;
        }

        private void Apply(Mood mood)
        {
            Current = mood;
            AnimationClip walk = _clips.walk, idle = _clips.idle;
            switch (mood)
            {
                case Mood.Run: walk = _clips.run; break;
                case Mood.Tired: walk = _clips.walkTired; idle = _clips.idleTired; break;
                case Mood.Hurt: walk = _clips.walkHurt; idle = _clips.idleHurt; break;
            }
            if (_set.walkKey != null && walk != null) _overrides[_set.walkKey] = walk;
            if (_set.idleKey != null && idle != null) _overrides[_set.idleKey] = idle;
            if (_set.layKey != null) _overrides[_set.layKey] = mood == Mood.Faint ? _clips.faint : null;
        }

        /// <summary>Tests: the clip now behind the Walk state.</summary>
        public string WalkClipName => _overrides != null && _set != null && _set.walkKey != null && _overrides[_set.walkKey] != null ? _overrides[_set.walkKey].name : "";
        public string IdleClipName => _overrides != null && _set != null && _set.idleKey != null && _overrides[_set.idleKey] != null ? _overrides[_set.idleKey].name : "";
        public string LayClipName => _overrides != null && _set != null && _set.layKey != null && _overrides[_set.layKey] != null ? _overrides[_set.layKey].name : "";
    }
}
