using System.Collections.Generic;

namespace NihongoLife.Interaction
{
    /// <summary>Something that must be settled before the player may leave a zone (e.g. an unpaid restaurant bill).</summary>
    public interface IExitBlocker
    {
        bool BlocksExit { get; }

        /// <summary>Called when the player tries to leave while <see cref="BlocksExit"/> is true; explain why.</summary>
        void OnExitBlocked();
    }

    /// <summary>Registry checked by ScenePortal before a zone exit. Blockers register while enabled.</summary>
    public static class ExitGuard
    {
        private static readonly List<IExitBlocker> Blockers = new List<IExitBlocker>();

        public static void Register(IExitBlocker blocker)
        {
            if (blocker != null && !Blockers.Contains(blocker)) Blockers.Add(blocker);
        }

        public static void Unregister(IExitBlocker blocker)
        {
            Blockers.Remove(blocker);
        }

        /// <summary>True (after notifying the blocker) if leaving must be refused.</summary>
        public static bool TryBlock()
        {
            for (int i = 0; i < Blockers.Count; i++)
            {
                var blocker = Blockers[i];
                if (blocker != null && blocker.BlocksExit)
                {
                    blocker.OnExitBlocked();
                    return true;
                }
            }

            return false;
        }
    }
}
