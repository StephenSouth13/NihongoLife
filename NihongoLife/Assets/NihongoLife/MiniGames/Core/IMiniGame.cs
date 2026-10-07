using System;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    /// <summary>A mini-game shown inside the MiniGameController overlay. It owns its own UI under
    /// <paramref name="root"/> and raises <see cref="Finished"/> exactly once (completed or aborted).</summary>
    public interface IMiniGame
    {
        string GameId { get; }
        void Begin(MiniGameDefinition definition, RectTransform root);
        void Abort();
        event Action<MiniGameResult> Finished;
    }
}
