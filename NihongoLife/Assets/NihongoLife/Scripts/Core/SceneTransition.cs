using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// One set of timings/curves for every scene change (SceneFlowController fades, the loading card wipe), so
    /// transitions feel the same everywhere. Pure math: unit-tested and safe in Edit Mode.
    /// </summary>
    public static class SceneTransition
    {
        public const float FadeInSeconds = 0.38f;
        public const float FadeOutSeconds = 0.5f;
        public const float ReadyHoldSeconds = 0.18f;
        /// <summary>Horizontal wipe distance (reference px) of the loading card as it fades.</summary>
        public const float WipeDistance = 60f;

        /// <summary>Ease-in-out (smoothstep) of t in 0..1, clamped.</summary>
        public static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>Alpha while fading in (0→1) or out (1→0) after <paramref name="elapsed"/> seconds.</summary>
        public static float Alpha(float elapsed, bool fadingIn)
        {
            float d = fadingIn ? FadeInSeconds : FadeOutSeconds;
            float e = Ease(d <= 0f ? 1f : elapsed / d);
            return fadingIn ? e : 1f - e;
        }

        /// <summary>Wipe offset matching <see cref="Alpha"/>: slides in from the left, out to the right.</summary>
        public static float WipeOffset(float alpha, bool fadingIn) => (1f - Mathf.Clamp01(alpha)) * WipeDistance * (fadingIn ? -1f : 1f);

        /// <summary>Monotonic display progress: never goes backwards, maps Unity's 0..0.9 load progress to 0..1.</summary>
        public static float DisplayProgress(float previous, float asyncProgress) => Mathf.Max(Mathf.Clamp01(previous), Mathf.Clamp01(asyncProgress / 0.9f));
    }
}
