using System;
using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    public enum KanaPairType
    {
        HiraganaRomaji,
        KatakanaRomaji,
        KanjiReading,
        WordImage,
        JapaneseVietnamese
    }

    /// <summary>One matchable pair. Side A is always Japanese; side B is the reading (romaji / kana, the same in every
    /// interface language), the meaning (<see cref="b"/> Vietnamese, <see cref="bEn"/> English) or a picture
    /// (Resources path in <see cref="imageB"/>).</summary>
    [Serializable]
    public sealed class KanaPair
    {
        public string targetId;
        public string a;
        public string b;
        public string bEn;
        public string imageB;
        public string hintVi;

        /// <summary>Side B in the interface language: readings as they are, meanings translated (empty in the
        /// Japanese interface, where a Japanese↔meaning pair has no second language to match).</summary>
        public string Clue(KanaPairType type) => type switch
        {
            KanaPairType.HiraganaRomaji or KanaPairType.KatakanaRomaji or KanaPairType.KanjiReading => b,
            _ => Loc.IsJapanese ? "" : Loc.IsEnglish && !string.IsNullOrEmpty(bEn) ? bEn : b,
        };

        /// <summary>Meaning in the interface language, for notes and review lines (empty in Japanese).</summary>
        public string Meaning => Loc.Gloss(string.IsNullOrEmpty(hintVi) ? b : hintVi, bEn);
    }

    /// <summary>Data-driven learning content shared by the arcade games (Kana Match, Word Shooter, Kana Rain…).</summary>
    public sealed class KanaPairSet : ScriptableObject
    {
        public string id = "hiragana_basic";
        public KanaPairType type;
        public string titleJa = "ひらがな";
        public string titleVi = "Hiragana ↔ Romaji";
        public string titleEn = "Hiragana ↔ Romaji";
        public List<KanaPair> pairs = new();

        public string Title => Loc.Pick(titleVi, titleEn, titleJa);

        /// <summary>Japanese↔meaning sets need a second language: not offered in the Japanese interface.</summary>
        public bool AvailableInCurrentLanguage => !(Loc.IsJapanese && type == KanaPairType.JapaneseVietnamese);

        /// <summary>Label for side B on cards and prompts.</summary>
        public string SideBTag => type switch
        {
            KanaPairType.HiraganaRomaji or KanaPairType.KatakanaRomaji => "ROMAJI",
            KanaPairType.KanjiReading => "よみかた",
            KanaPairType.WordImage => Loc.T("HÌNH", "PICTURE", "え"),
            _ => Loc.T("TIẾNG VIỆT", "ENGLISH", "いみ"),
        };
    }
}
