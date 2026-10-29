using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Music
{
    [Serializable] public sealed class SongNote { public int midi; public string note; public float start, duration; }

    /// <summary>A melody to play (Resources/Music/songs.json): notes in quarter-note beats, MIDI numbers (C4 = 60).</summary>
    [Serializable]
    public sealed class SongDefinition
    {
        public string id, title, title_vi, title_ja, title_ru, composer, key, time_signature, level, instrument, notes_licence;
        public float tempo_bpm = 90f;
        public int difficulty = 1;
        public SongNote[] notes = Array.Empty<SongNote>();
        public string Title => Loc.Code switch { "vi" => Or(title_vi), "ja" => Or(title_ja), "ru" => Or(title_ru), _ => title };
        private string Or(string s) => string.IsNullOrEmpty(s) ? title : s;
        public float SecondsPerBeat => 60f / Mathf.Max(30f, tempo_bpm);
    }

    [Serializable]
    public sealed class GuitarChord
    {
        public string name, category;
        /// <summary>frets[0] = 6th string (low E) … frets[5] = 1st string; −1 muted, 0 open.</summary>
        public int[] frets = Array.Empty<int>(), fingers = Array.Empty<int>(), midi = Array.Empty<int>();
        public int baseFret = 1;
    }

    [Serializable] public sealed class GuitarProgression { public string id, name, roman, key, genre; public string[] chords = Array.Empty<string>(); }

    [Serializable] public sealed class TheoryQuestion { public string id, q, explain; public string[] options = Array.Empty<string>(); public int answer; }

    [Serializable]
    public sealed class TheoryLesson
    {
        public string id, level, title, intro;
        public int order, minutes;
        public string[] points = Array.Empty<string>(), practiceSongs = Array.Empty<string>();
        public TheoryQuestion[] quiz = Array.Empty<TheoryQuestion>();
    }

    [Serializable]
    public sealed class Artwork
    {
        public string id, title, artist, date, medium, museum, licence, credit, sourceUrl;
        public Texture2D Picture => Resources.Load<Texture2D>("Art/Images/" + id);
    }

    /// <summary>
    /// The music and art content: 22 melodies, 42 guitar chord shapes with progressions, 24 music-theory lessons in
    /// vi / en / ja / ru with quizzes, and 39 paintings (Met Open Access CC0, Wikimedia Commons public domain).
    /// Imported by Tools/music/import_group_b.py; validated by MusicCatalogTests.
    /// </summary>
    public static class MusicCatalog
    {
        [Serializable] private sealed class SongFile { public SongDefinition[] songs = Array.Empty<SongDefinition>(); }
        [Serializable] private sealed class GuitarFile { public int[] tuningMidi = Array.Empty<int>(); public GuitarChord[] chords = Array.Empty<GuitarChord>(); public GuitarProgression[] progressions = Array.Empty<GuitarProgression>(); }
        [Serializable] private sealed class TheoryFile { public string language; public TheoryLesson[] lessons = Array.Empty<TheoryLesson>(); }
        [Serializable] private sealed class ArtFile { public Artwork[] artworks = Array.Empty<Artwork>(); }

        private static SongFile _songs;
        private static GuitarFile _guitar;
        private static ArtFile _art;
        private static readonly Dictionary<string, TheoryFile> Theory = new Dictionary<string, TheoryFile>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _songs = null; _guitar = null; _art = null; Theory.Clear(); }

        private static T Load<T>(string path) where T : class, new()
        {
            var text = Resources.Load<TextAsset>(path);
            if (text == null) return new T();
            try { return JsonUtility.FromJson<T>(text.text) ?? new T(); }
            catch (ArgumentException) { return new T(); }
        }

        public static SongDefinition[] Songs => (_songs ??= Load<SongFile>("Music/songs")).songs ?? Array.Empty<SongDefinition>();
        public static GuitarChord[] Chords => (_guitar ??= Load<GuitarFile>("Music/guitar")).chords ?? Array.Empty<GuitarChord>();
        public static GuitarProgression[] Progressions => (_guitar ??= Load<GuitarFile>("Music/guitar")).progressions ?? Array.Empty<GuitarProgression>();
        public static Artwork[] Artworks => (_art ??= Load<ArtFile>("Art/artworks")).artworks ?? Array.Empty<Artwork>();

        public static SongDefinition Song(string id) => Songs.FirstOrDefault(s => s.id == id);
        public static GuitarChord Chord(string name) => Chords.FirstOrDefault(c => c.name == name);
        public static Artwork ArtworkById(string id) => Artworks.FirstOrDefault(a => a.id == id);

        /// <summary>The lessons in the interface language (vi, en, ja or ru).</summary>
        public static TheoryLesson[] Lessons(string language = null)
        {
            string code = language ?? Loc.Code;
            if (!Theory.TryGetValue(code, out var file))
            {
                file = Load<TheoryFile>("Music/Theory/theory_" + code);
                if (file.lessons == null || file.lessons.Length == 0) file = Load<TheoryFile>("Music/Theory/theory_en");
                Theory[code] = file;
            }
            return file.lessons ?? Array.Empty<TheoryLesson>();
        }

        public static string NoteName(int midi)
        {
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            return names[((midi % 12) + 12) % 12] + (midi / 12 - 1);
        }
    }
}
