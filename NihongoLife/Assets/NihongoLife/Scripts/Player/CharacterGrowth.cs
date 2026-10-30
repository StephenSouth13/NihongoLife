using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// The character grows up with the game: body height follows a real growth curve from the player's level
    /// (level → age), with proportions that change like a person's (a child's head is large for the body, an adult's
    /// is about 1/7.5 of the height), so it is not a uniform blow-up.
    /// Lives on its own child — the visual root — never on the CharacterController's transform (AGENTS.md: never
    /// move/scale a component's own transform for a side effect); the controller's height is set separately.
    /// Hook: PlayerController adds it to the model child and calls Apply on level change (see
    /// Docs/Expansion/edu-v1/CHARACTER_GROWTH_HOOK.md).
    /// </summary>
    public sealed class CharacterGrowth : MonoBehaviour
    {
        public const float AdultHeight = 1.68f;   // metres (VN/JP average adult); the town's doors and counters fit it
        [SerializeField] private float modelHeightAtScaleOne = 1.2f; // the kid-toon models are ~1.2 m tall unscaled
        public float CurrentHeight { get; private set; }

        /// <summary>Age for a level: level 1 = 8 years, ~1 year per 2 levels, adult (18) at level 21.</summary>
        public static int AgeForLevel(int level) => Mathf.Clamp(8 + Mathf.Max(0, level - 1) / 2, 8, 80);

        /// <summary>Height in metres at an age (simplified WHO growth: 1.28 m at 8, growth spurt 11–15, 1.68 m from 18).</summary>
        public static float HeightAt(int age)
        {
            if (age >= 18) return AdultHeight;
            float t = Mathf.InverseLerp(8f, 18f, age);
            float s = t * t * (3f - 2f * t);              // spurt in the middle years
            return Mathf.Lerp(1.28f, AdultHeight, s);
        }

        /// <summary>Head share of the body: 1/6 at 8 → 1/7.5 adult; the head bone is scaled to keep it.</summary>
        public static float HeadRatio(int age) => Mathf.Lerp(1f / 6f, 1f / 7.5f, Mathf.InverseLerp(8f, 18f, age));

        public void Apply(int level, Animator animator = null)
        {
            int age = AgeForLevel(level);
            CurrentHeight = HeightAt(age);
            float body = CurrentHeight / Mathf.Max(0.1f, modelHeightAtScaleOne);
            transform.localScale = Vector3.one * body;    // uniform: skinned meshes stay undistorted
            if (animator != null && animator.isHuman)
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                // The models are child-proportioned (head ~1/5); shrink the head toward the age's share.
                if (head != null) head.localScale = Vector3.one * Mathf.Clamp(HeadRatio(age) / (1f / 5f), 0.6f, 1f);
            }
        }

        /// <summary>CharacterController height/centre for the grown body (on the controller, not this transform).</summary>
        public static void FitController(CharacterController cc, float height)
        {
            if (cc == null) return;
            cc.height = height; cc.center = new Vector3(0f, height * 0.5f, 0f); cc.radius = Mathf.Clamp(height * 0.19f, 0.22f, 0.34f);
        }
    }
}
