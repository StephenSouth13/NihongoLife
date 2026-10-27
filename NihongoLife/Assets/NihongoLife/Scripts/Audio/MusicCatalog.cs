using System;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Audio
{
    /// <summary>A background track (Resources/Audio/Music/&lt;key&gt;) as the handbook's 音楽 chapter shows it.</summary>
    [Serializable]
    public sealed class MusicTrack
    {
        public string key, ja, titleVi, titleEn, moodVi, moodEn, whereVi, whereEn, credit;
        public string Title => Loc.Pick(titleVi, titleEn, ja);
        public string Mood => Loc.Pick(moodVi, moodEn, moodEn);
        public string Where => Loc.Pick(whereVi, whereEn, whereEn);
    }

    /// <summary>Every background track (Resources/Audio/music.json): where it plays, its mood and credit.</summary>
    [Serializable]
    public sealed class MusicCatalog
    {
        public string schema;
        public MusicTrack[] tracks = Array.Empty<MusicTrack>();

        private static MusicCatalog _cached;

        public static MusicCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>("Audio/music");
            _cached = text != null ? JsonUtility.FromJson<MusicCatalog>(text.text) ?? new MusicCatalog() : new MusicCatalog();
            return _cached;
        }

        public MusicTrack Track(string key) => tracks.FirstOrDefault(t => t.key == key);
    }
}
