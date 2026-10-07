using System;
using System.Collections.Generic;
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

    /// <summary>One matchable pair. Side A is always Japanese; side B is the reading, the meaning or a
    /// picture (Resources path in <see cref="imageB"/>).</summary>
    [Serializable]
    public sealed class KanaPair
    {
        public string targetId;
        public string a;
        public string b;
        public string imageB;
        public string hintVi;
    }

    /// <summary>Data-driven learning content for Kana Match (and later Word Shooter).</summary>
    public sealed class KanaPairSet : ScriptableObject
    {
        public string id = "hiragana_basic";
        public KanaPairType type;
        public string titleJa = "ひらがな";
        public string titleVi = "Hiragana ↔ Romaji";
        public List<KanaPair> pairs = new();
    }
}
