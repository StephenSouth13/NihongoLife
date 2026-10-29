using System.Linq;
using NihongoLife.Music;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the music &amp; art content (Tools/music/import_group_b.py): melodies in order and in a piano's range, guitar
    /// shapes whose MIDI notes match the tuning and the frets, progressions made of known chords, the theory course
    /// identical in vi / en / ja / ru with valid answers, and every painting with its picture and licence.
    /// </summary>
    public class MusicCatalogTests
    {
        [Test]
        public void Songs_AreOrderedMelodiesInRange()
        {
            Assert.GreaterOrEqual(MusicCatalog.Songs.Length, 20, "songs.json did not load");
            foreach (var s in MusicCatalog.Songs)
            {
                Assert.IsFalse(string.IsNullOrEmpty(s.title) || string.IsNullOrEmpty(s.title_vi) || string.IsNullOrEmpty(s.title_ja) || string.IsNullOrEmpty(s.title_ru), s.id + ": titles");
                Assert.Greater(s.notes.Length, 4, s.id);
                Assert.Greater(s.tempo_bpm, 30f, s.id);
                for (int i = 0; i < s.notes.Length; i++)
                {
                    Assert.That(s.notes[i].midi, Is.InRange(21, 108), s.id + " note " + i);
                    Assert.Greater(s.notes[i].duration, 0f, s.id + " note " + i);
                    if (i > 0) Assert.GreaterOrEqual(s.notes[i].start, s.notes[i - 1].start, s.id + ": notes in order");
                }
            }
        }

        [Test]
        public void GuitarChords_MatchTheTuning_AndProgressionsUseKnownChords()
        {
            int[] tuning = { 40, 45, 50, 55, 59, 64 };
            Assert.GreaterOrEqual(MusicCatalog.Chords.Length, 40);
            foreach (var c in MusicCatalog.Chords)
            {
                Assert.AreEqual(6, c.frets.Length, c.name);
                for (int s = 0; s < 6; s++)
                    Assert.AreEqual(c.frets[s] < 0 ? -1 : tuning[s] + c.frets[s], c.midi[s], c.name + " string " + (6 - s));
            }
            Assert.Greater(MusicCatalog.Progressions.Length, 10);
            foreach (var p in MusicCatalog.Progressions)
                foreach (var chord in p.chords) Assert.IsNotNull(MusicCatalog.Chord(chord), p.id + ": " + chord);
        }

        [Test]
        public void Theory_IsTheSameCourseInFourLanguages()
        {
            var vi = MusicCatalog.Lessons("vi");
            Assert.AreEqual(24, vi.Length, "24 lessons");
            foreach (var lang in new[] { "en", "ja", "ru" })
            {
                var other = MusicCatalog.Lessons(lang);
                CollectionAssert.AreEqual(vi.Select(l => l.id).ToArray(), other.Select(l => l.id).ToArray(), lang + ": same lessons");
                for (int i = 0; i < vi.Length; i++)
                {
                    Assert.AreEqual(vi[i].quiz.Length, other[i].quiz.Length, lang + "/" + vi[i].id + ": same quiz");
                    for (int q = 0; q < vi[i].quiz.Length; q++) Assert.AreEqual(vi[i].quiz[q].answer, other[i].quiz[q].answer, lang + "/" + vi[i].quiz[q].id + ": same answer");
                }
            }
            foreach (var l in vi)
                foreach (var q in l.quiz) Assert.That(q.answer, Is.InRange(0, q.options.Length - 1), q.id);
        }

        [Test]
        public void Gallery_HasPicturesAndLicences()
        {
            Assert.GreaterOrEqual(MusicCatalog.Artworks.Length, 30);
            foreach (var a in MusicCatalog.Artworks)
            {
                Assert.IsNotNull(a.Picture, a.id + ": picture");
                Assert.IsFalse(string.IsNullOrWhiteSpace(a.licence), a.id + ": licence");
                Assert.IsTrue(a.licence.Contains("CC0") || a.licence.ToLowerInvariant().Contains("public domain"), a.id + ": CC0 or public domain only");
                Assert.IsFalse(a.title.Contains("QS:") || a.title.Contains("label") || a.credit.Contains("QS:"), a.id + ": a clean title, not a Wikimedia label dump");
                Assert.IsFalse(a.date.Contains("between") || a.date.Contains("publication"), a.id + ": a tidy date");
            }
        }

        [Test]
        public void DjMix_ExactlyOneRecordMixesIn()
        {
            var random = new System.Random(7);
            for (int i = 0; i < 200; i++)
            {
                var (playing, options, right) = MusicShop.MakeMix(random);
                Assert.AreEqual(3, options.Length);
                for (int o = 0; o < options.Length; o++)
                    Assert.AreEqual(o == right, MusicShop.Compatible(playing, options[o]), $"{playing.Label} → {options[o].Label}");
            }
            // The Camelot wheel wraps: 12A mixes into 1A, 8A into 8B (relative major), not into 3A.
            Assert.IsTrue(MusicShop.Compatible(new MusicShop.Track("a", 12, false, 120), new MusicShop.Track("b", 1, false, 122)));
            Assert.IsTrue(MusicShop.Compatible(new MusicShop.Track("a", 8, false, 120), new MusicShop.Track("b", 8, true, 118)));
            Assert.IsFalse(MusicShop.Compatible(new MusicShop.Track("a", 8, false, 120), new MusicShop.Track("b", 3, false, 120)));
            Assert.IsFalse(MusicShop.Compatible(new MusicShop.Track("a", 8, false, 120), new MusicShop.Track("b", 8, false, 150)), "tempo too far");
        }
    }
}
