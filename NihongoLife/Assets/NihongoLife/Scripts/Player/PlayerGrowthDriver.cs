using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Player
{
    /// <summary>
    /// Grows the player's body with level (CharacterGrowth curve), relative to the model's own size at level 1 so the
    /// kid-toon at level 1 is unchanged. Scales only the Animator's visual child (never the CharacterController's
    /// transform) and fits the controller height. Checks the level once a second, not every frame.
    /// Attached automatically to the PlayerController after each scene load. Tests: set Disabled.
    /// </summary>
    public sealed class PlayerGrowthDriver : MonoBehaviour
    {
        public static bool Disabled;
        public const float MaxGrowth = 1.25f; // doors, counters and the camera rig were built for the kid size; cap the adult
        private Transform _visual; private Vector3 _baseScale; private float _baseCcHeight; private Vector3 _baseCcCenter;
        private CharacterController _cc; private int _level = -1; private float _timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Attach(); }
        private static void OnLoaded(Scene s, LoadSceneMode m) => Attach();
        private static void Attach()
        {
            if (Disabled) return;
            var p = Object.FindFirstObjectByType<PlayerController>();
            if (p != null && p.GetComponent<PlayerGrowthDriver>() == null) p.gameObject.AddComponent<PlayerGrowthDriver>();
        }

        public static float FactorFor(int level) => Mathf.Min(MaxGrowth, CharacterGrowth.HeightAt(CharacterGrowth.AgeForLevel(level)) / CharacterGrowth.HeightAt(8));

        private void Start()
        {
            var anim = GetComponentInChildren<Animator>(true);
            if (anim == null || anim.transform == transform) { enabled = false; return; }
            _visual = anim.transform; _baseScale = _visual.localScale;
            _cc = GetComponent<CharacterController>();
            if (_cc != null) { _baseCcHeight = _cc.height; _baseCcCenter = _cc.center; }
        }

        private void Update()
        {
            if (Disabled || _visual == null) return;
            _timer -= Time.deltaTime; if (_timer > 0f) return; _timer = 1f;
            int level = PlayerStatus.Instance != null ? PlayerStatus.Instance.Level : 1;
            if (level == _level) return;
            _level = level;
            float k = FactorFor(level);
            _visual.localScale = _baseScale * k;
            if (_cc != null) { _cc.height = _baseCcHeight * k; _cc.center = new Vector3(_baseCcCenter.x, _baseCcCenter.y * k, _baseCcCenter.z); }
        }
    }
}