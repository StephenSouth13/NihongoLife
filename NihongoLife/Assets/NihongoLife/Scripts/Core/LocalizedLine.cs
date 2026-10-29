using System;

namespace NihongoLife.Core
{
    /// <summary>
    /// One line of text in the four interface languages, as authored in JSON (sign labels, timetable place names).
    /// Russian falls back to the translation table through Loc.T when a line has no "ru" of its own.
    /// </summary>
    [Serializable]
    public sealed class LocalizedLine
    {
        public string vi;
        public string en;
        public string ja;
        public string ru;

        public LocalizedLine() { }

        public LocalizedLine(string vi, string en, string ja, string ru = null)
        {
            this.vi = vi; this.en = en; this.ja = ja; this.ru = ru;
        }

        /// <summary>The line in the interface language.</summary>
        public string Text => Loc.IsRussian && !string.IsNullOrEmpty(ru) ? ru : Loc.Pick(vi, en, ja);

        /// <summary>Japanese first (it is what the player is learning), then the interface language; just Japanese in Japanese.</summary>
        public string Bilingual
        {
            get
            {
                if (string.IsNullOrEmpty(ja) || Loc.IsJapanese) return Text;
                string local = Text;
                return local == ja ? ja : ja + "  \u00b7  " + local;
            }
        }

        public bool HasAllLanguages => !string.IsNullOrWhiteSpace(vi) && !string.IsNullOrWhiteSpace(en) && !string.IsNullOrWhiteSpace(ja) && !string.IsNullOrWhiteSpace(ru);
    }
}
