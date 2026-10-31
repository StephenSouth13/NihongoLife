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
        public void Songs_AreGroupedByRegion_WithKnownInstruments()
        {
            var regions = MusicCatalog.Regions.Select(r => r.id).ToArray();
            var instruments = MusicCatalog.Instruments.Select(i => i.id).ToArray();
            Assert.AreEqual(MusicCatalog.Songs.Length, MusicCatalog.Songs.Select(s => s.id).Distinct().Count(), "unique ids");
            foreach (var s in MusicCatalog.Songs)
            {
                CollectionAssert.Contains(regions, s.region, s.id + ": region");
                Assert.IsNotEmpty(s.instruments, s.id + ": instruments");
                Assert.AreEqual(s.instrument, s.instruments[0], s.id + ": the first instrument is the one it plays on");
                foreach (var i in s.instruments) CollectionAssert.Contains(instruments, i, s.id + ": instrument " + i);
                string licence = (s.notes_licence ?? "").ToLowerInvariant();
                Assert.IsTrue(licence.Contains("public domain") || licence.Contains("cc0"), s.id + ": public domain / CC0 only");
                Assert.IsFalse(licence.Contains("cc by-nc") || licence.Contains("all rights reserved"), s.id);
            }
            Assert.GreaterOrEqual(MusicCatalog.SongsIn("vietnam").Count(), 3, "Vietnamese folk songs");
            Assert.GreaterOrEqual(MusicCatalog.SongsIn("japan").Count(), 5, "Japanese songs");
            Assert.GreaterOrEqual(MusicCatalog.SongsIn("russia").Count(), 4, "Russian songs");
            Assert.GreaterOrEqual(MusicCatalog.SongsIn("classical").Count(), 5, "classical");
            Assert.GreaterOrEqual(MusicCatalog.SongsIn("children").Count(), 5, "children's songs");
            // Folk songs sound on their own instruments, not on the piano.
            foreach (var region in new[] { "vietnam", "russia" })
                foreach (var s in MusicCatalog.SongsIn(region))
                    Assert.AreNotEqual(InstrumentSynth.Voice.Piano, InstrumentSynth.VoiceFor(s.instrument), s.id);
            foreach (var s in MusicCatalog.SongsIn("vietnam"))
                Assert.IsTrue(s.instruments.All(i => i == "dan_tranh" || i == "dan_bau" || i == "sao_truc" || i == "dan_nguyet"), s.id + ": Vietnamese instruments");
            Assert.IsNotNull(MusicCatalog.Song("ly-cay-da"));
            Assert.IsNotNull(MusicCatalog.Song("kojo-no-tsuki"));
            Assert.IsNotNull(MusicCatalog.Song("kalinka"));
        }

        [Test]
        public void Regions_AndInstruments_AreNamedInFourLanguages()
        {
            foreach (var r in MusicCatalog.Regions)
                Assert.IsFalse(string.IsNullOrEmpty(r.vi) || string.IsNullOrEmpty(r.en) || string.IsNullOrEmpty(r.ja) || string.IsNullOrEmpty(r.ru), r.id);
            foreach (var i in MusicCatalog.Instruments)
                Assert.IsFalse(string.IsNullOrEmpty(i.vi) || string.IsNullOrEmpty(i.en) || string.IsNullOrEmpty(i.ja) || string.IsNullOrEmpty(i.ru), i.id);
        }

        [Test]
        public void EveryVoice_SynthesisesAnAudibleCleanNote()
        {
            foreach (InstrumentSynth.Voice voice in System.Enum.GetValues(typeof(InstrumentSynth.Voice)))
                foreach (int midi in new[] { 48, 69, 84 })
                {
                    var clip = InstrumentSynth.Clip(voice, midi);
                    Assert.IsNotNull(clip, voice + " " + midi);
                    var data = new float[clip.samples];
                    clip.GetData(data, 0);
                    float peak = data.Max(System.Math.Abs);
                    Assert.Greater(peak, 0.02f, voice + " " + midi + ": audible");
                    Assert.Less(peak, 1f, voice + " " + midi + ": no clipping");
                    Assert.IsFalse(data.Any(float.IsNaN), voice + " " + midi);
                }
            Assert.AreEqual(InstrumentSynth.Voice.DanTranh, InstrumentSynth.VoiceFor("dan_tranh"));
            Assert.AreEqual(InstrumentSynth.Voice.Shamisen, InstrumentSynth.VoiceFor("shamisen"));
            Assert.AreEqual(InstrumentSynth.Voice.Piano, InstrumentSynth.VoiceFor(null));
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
