using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    public enum MiniGameKind
    {
        KanaMatch,
        WordShooter,
        OrderRush
    }

    /// <summary>Data for one arcade machine: which mini-game, which learning content, how long a round lasts.</summary>
    public sealed class MiniGameDefinition : ScriptableObject
    {
        public string id = "kana_match";
        public MiniGameKind kind = MiniGameKind.KanaMatch;
        public string titleJa = "かなマッチ";
        public string titleVi = "Kana Match";
        [TextArea] public string descriptionVi = "Lật thẻ, ghép cặp chữ với cách đọc.";
        public bool playable = true;
        [Tooltip("Content the player can pick before a round (pair sets for Kana Match).")]
        public List<KanaPairSet> contentSets = new();
        public int pairsPerRound = 8;
        public float timeLimitSeconds = 120f;
        public int expPerCorrect = 3;
        public int knowledgePerRound = 4;
    }
}
