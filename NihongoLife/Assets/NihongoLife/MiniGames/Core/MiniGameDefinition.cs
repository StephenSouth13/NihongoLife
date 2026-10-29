using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    public enum MiniGameKind
    {
        KanaMatch,
        WordShooter,
        OrderRush,
        KanaRain,
        UfoCatcher,
        BasketCount,
        WordWhack,
        EchoPads,
        KanaSnake,
        KanaBreaker,
        DanceArrows,
        TenseTyper,
        WheelQuiz,
        AirHockey
    }

    /// <summary>Data for one arcade machine: which mini-game, which learning content, how long a round lasts.</summary>
    public sealed class MiniGameDefinition : ScriptableObject
    {
        public string id = "kana_match";
        public MiniGameKind kind = MiniGameKind.KanaMatch;
        public string titleJa = "かなマッチ";
        public string titleVi = "Kana Match";
        public string titleEn = "Kana Match";
        [TextArea] public string descriptionVi = "Lật thẻ, ghép cặp chữ với cách đọc.";
        [TextArea] public string descriptionEn = "Flip cards and match each character with its reading.";
        [TextArea] public string descriptionJa = "カードをめくって、文字と読み方を合わせよう。";
        public bool playable = true;
        [Tooltip("Content the player can pick before a round (pair sets).")]
        public List<KanaPairSet> contentSets = new();
        public int pairsPerRound = 8;
        public float timeLimitSeconds = 120f;
        public int expPerCorrect = 3;
        public int knowledgePerRound = 4;

        /// <summary>The machine's name in the interface language (the Japanese title in the Japanese interface).</summary>
        public string Title => Loc.Pick(titleVi, string.IsNullOrEmpty(titleEn) ? titleVi : titleEn, titleJa);
        public string Description => Loc.Pick(descriptionVi, string.IsNullOrEmpty(descriptionEn) ? descriptionVi : descriptionEn, string.IsNullOrEmpty(descriptionJa) ? descriptionVi : descriptionJa);
    }
}
