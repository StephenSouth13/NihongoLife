using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// Extra Kids clips the game swaps in on the fly (KidMoodAnimator): running, tired, hurt, fainting. Built by
    /// KidsCharactersSetup.BuildAnimationSet into Resources/Characters/Kids/NL_KidAnimationSet.asset.
    /// The *Key clips are the NL_Humanoid states' original motions (what an override replaces).
    /// </summary>
    public sealed class KidAnimationSet : ScriptableObject
    {
        [System.Serializable]
        public sealed class Gender
        {
            public AnimationClip walk, idle, run, walkTired, idleTired, walkHurt, idleHurt, faint;
        }

        public AnimationClip walkKey, idleKey, layKey;
        public Gender boy = new Gender(), girl = new Gender();

        private static KidAnimationSet _loaded;
        public static KidAnimationSet Load() => _loaded != null ? _loaded : (_loaded = Resources.Load<KidAnimationSet>("Characters/Kids/NL_KidAnimationSet"));
    }
}
