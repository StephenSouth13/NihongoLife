using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Course
{
    /// <summary>
    /// Loader for the school curriculum shipped in Resources/Content/School (40 units x 4 languages, alphabets, grammar,
    /// placement tests, level certificates, the learner-choice route model). Everything is read lazily and cached per language.
    /// </summary>
    public static class SchoolCatalog
    {
        public const string Root = "Content/School/";
        public static readonly string[] Targets = { "vi", "en", "ja", "ru" };
        public static readonly string[] Levels = { "A1", "A2", "B1", "B2", "C1" };
        public const string IeltsStyleId = "cert_english_ielts_style";

        private static readonly Dictionary<string, List<CourseUnit>> Units_ = new Dictionary<string, List<CourseUnit>>();
        private static readonly Dictionary<string, List<CourseGrammar>> Grammar_ = new Dictionary<string, List<CourseGrammar>>();
        private static readonly Dictionary<string, CourseAlphabet> Alphabets_ = new Dictionary<string, CourseAlphabet>();
        private static readonly Dictionary<string, CoursePlacement> Placement_ = new Dictionary<string, CoursePlacement>();
        private static readonly Dictionary<string, CourseCertificate> Certificates_ = new Dictionary<string, CourseCertificate>();
        private static Dictionary<string, object> _options;

        public static void ClearCache()
        {
            Units_.Clear(); Grammar_.Clear(); Alphabets_.Clear(); Placement_.Clear(); Certificates_.Clear(); _options = null;
        }

        public static Dictionary<string, object> Read(string path)
        {
            var asset = Resources.Load<TextAsset>(Root + path);
            if (asset == null) return null;
            var parsed = MiniJson.ParseObject(asset.text);
            Resources.UnloadAsset(asset);
            return parsed;
        }

        public static bool IsTarget(string code) => System.Array.IndexOf(Targets, code) >= 0;

        // ---- units ----

        public static List<CourseUnit> Units(string target)
        {
            if (!IsTarget(target)) return new List<CourseUnit>();
            if (Units_.TryGetValue(target, out var cached)) return cached;
            var list = new List<CourseUnit>();
            var file = Read("units/units_" + target);
            foreach (var node in CourseQuestionParser.Items(file, "units"))
            {
                var u = CourseQuestionParser.Obj(node);
                if (u != null) list.Add(ParseUnit(u, target));
            }
            list.Sort((a, b) => a.No.CompareTo(b.No));
            Units_[target] = list;
            return list;
        }

        public static CourseUnit Unit(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 2) return null;
            return Units(id.Substring(0, 2)).Find(u => u.Id == id);
        }

        private static CourseUnit ParseUnit(Dictionary<string, object> u, string target)
        {
            var unit = new CourseUnit
            {
                Id = u.Str("id"), Target = target, Theme = u.Str("theme"), Level = u.Str("level"), No = (int)u.Num("no"),
                GrammarId = u.Str("grammar_id"), Title = CourseText.From(u.ContainsKey("title") ? u["title"] : null),
            };
            foreach (var n in CourseQuestionParser.Items(u, "words"))
            {
                var w = CourseQuestionParser.Obj(n);
                if (w == null) continue;
                unit.Words.Add(new CourseWord
                {
                    Id = w.Str("id"), Text = w.Str("text"), Reading = w.Str("reading"),
                    Gloss = CourseText.From(w.ContainsKey("gloss") ? w["gloss"] : null), Speech = CourseSpeech.From(w.ContainsKey("tts") ? w["tts"] : null),
                });
            }
            foreach (var n in CourseQuestionParser.Items(u, "dialogue"))
            {
                var l = CourseQuestionParser.Obj(n);
                if (l == null) continue;
                unit.Dialogue.Add(new CourseLine
                {
                    Id = l.Str("id"), Speaker = l.Str("speaker"), Text = l.Str("text"),
                    Translation = CourseText.From(l.ContainsKey("translation") ? l["translation"] : null), Speech = CourseSpeech.From(l.ContainsKey("tts") ? l["tts"] : null),
                });
            }
            string readingId = null;
            var reading = u.Obj("reading");
            if (reading != null)
            {
                readingId = reading.Str("id");
                unit.ReadingTitle = CourseText.From(reading.ContainsKey("title") ? reading["title"] : null);
                unit.ReadingText = reading.Str("text");
            }
            var speaking = u.Obj("speaking");
            if (speaking != null)
            {
                unit.SpeakingPrompt = CourseText.From(speaking.ContainsKey("prompt") ? speaking["prompt"] : null);
                unit.SpeakingModelLineIds = CourseQuestionParser.Strings(speaking, "model_line_ids");
            }
            foreach (var n in CourseQuestionParser.Items(u, "exercises"))
            {
                var ex = CourseQuestionParser.Obj(n);
                if (ex == null) continue;
                unit.Exercises.Add(CourseQuestionParser.Exercise(ex, target, id => id == readingId ? unit.ReadingText : null));
            }
            return unit;
        }

        // ---- grammar ----

        public static List<CourseGrammar> Grammar(string target)
        {
            if (!IsTarget(target)) return new List<CourseGrammar>();
            if (Grammar_.TryGetValue(target, out var cached)) return cached;
            var list = new List<CourseGrammar>();
            var file = Read("units/grammar_" + target);
            foreach (var node in CourseQuestionParser.Items(file, "points"))
            {
                var g = CourseQuestionParser.Obj(node);
                if (g == null) continue;
                var example = g.Obj("example");
                list.Add(new CourseGrammar
                {
                    Id = g.Str("id"), Pattern = g.Str("pattern"), Level = g.Str("level"), UnitNo = (int)g.Num("unit_no"),
                    Title = CourseText.From(g.ContainsKey("title") ? g["title"] : null), Explanation = CourseText.From(g.ContainsKey("explanation") ? g["explanation"] : null),
                    ExampleText = example?.Str("text"), ExampleTranslation = CourseText.From(example != null && example.ContainsKey("translation") ? example["translation"] : null),
                });
            }
            list.Sort((a, b) => a.UnitNo.CompareTo(b.UnitNo));
            Grammar_[target] = list;
            return list;
        }

        public static CourseGrammar GrammarPoint(string target, string id) => Grammar(target).Find(g => g.Id == id);

        // ---- alphabets ----

        public static string[] AlphabetIds(string target)
        {
            switch (target)
            {
                case "ja": return new[] { "ja_hiragana", "ja_katakana" };
                case "en": return new[] { "en_alphabet_phonics" };
                case "ru": return new[] { "ru_cyrillic" };
                case "vi": return new[] { "vi_alphabet_tones" };
                default: return new string[0];
            }
        }

        public static CourseAlphabet Alphabet(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Alphabets_.TryGetValue(id, out var cached)) return cached;
            var f = Read("alphabets/" + id);
            if (f == null) return null;
            string target = f.Str("target");
            var a = new CourseAlphabet
            {
                Id = id, Target = target, Kind = f.Str("kind"),
                Title = CourseText.From(f.ContainsKey("title") ? f["title"] : null), Intro = CourseText.From(f.ContainsKey("intro") ? f["intro"] : null),
            };
            foreach (var n in CourseQuestionParser.Items(f, "items"))
            {
                var i = CourseQuestionParser.Obj(n);
                if (i == null) continue;
                var ex = i.Obj("example");
                string exWord = ex?.Str("word");
                if (exWord == null)
                {
                    var words = CourseQuestionParser.Items(i, "words");
                    if (words.Count > 0) exWord = words[0] as string;
                }
                a.Glyphs.Add(new CourseGlyph
                {
                    Id = i.Str("id"), Char = i.Str("char"), Kind = i.Str("kind"),
                    Sound = i.Str("romaji") ?? i.Str("sound") ?? (i.Str("kind") == "digraph" ? i.Str("char") : null) ?? i.Str("letter_name") ?? i.Str("char"), Name = i.Str("letter_name") ?? i.Str("letter_name_ipa"), ToneName = i.Str("tone_name"),
                    ExampleWord = exWord, ExampleReading = ex?.Str("romaji"),
                    ExampleGloss = CourseText.From(ex != null && ex.ContainsKey("gloss") ? ex["gloss"] : null), Speech = CourseSpeech.From(i.ContainsKey("tts") ? i["tts"] : null),
                });
            }
            foreach (var n in CourseQuestionParser.Items(f, "lessons"))
            {
                var l = CourseQuestionParser.Obj(n);
                if (l == null) continue;
                a.Lessons.Add(new CourseAlphabetLesson
                {
                    Id = l.Str("id"), Title = CourseText.From(l.ContainsKey("title") ? l["title"] : null), Tip = CourseText.From(l.ContainsKey("tip") ? l["tip"] : null),
                    ItemIds = CourseQuestionParser.Strings(l, "item_ids"),
                });
            }
            foreach (var n in CourseQuestionParser.Items(f, "quiz"))
            {
                var q = CourseQuestionParser.Obj(n);
                if (q != null) a.Quiz.Add(CourseQuestionParser.AlphabetQuestion(q, target));
            }
            Alphabets_[id] = a;
            return a;
        }

        // ---- placement ----

        public static CoursePlacement Placement(string target)
        {
            if (!IsTarget(target)) return null;
            if (Placement_.TryGetValue(target, out var cached)) return cached;
            var f = Read("placement/placement_" + target);
            if (f == null) return null;
            var p = new CoursePlacement
            {
                Id = f.Str("id"), Target = target,
                Instructions = CourseText.From(f.ContainsKey("instructions") ? f["instructions"] : null), DontKnow = CourseText.From(f.ContainsKey("dont_know") ? f["dont_know"] : null),
                PassPerLevel = (int)(f.Obj("scoring")?.Num("pass_per_level", 3) ?? 3),
            };
            foreach (var n in CourseQuestionParser.Items(f, "questions"))
            {
                var q = CourseQuestionParser.Obj(n);
                if (q != null) p.Questions.Add(CourseQuestionParser.PlacementQuestion(q, target));
            }
            foreach (var n in CourseQuestionParser.Items(f, "levels"))
            {
                var l = CourseQuestionParser.Obj(n);
                if (l != null) p.Levels.Add(new CoursePlacementLevel { Cefr = l.Str("cefr"), Label = l.Str("label"), StartUnitId = l.Str("start_unit_id"), CertificateId = l.Str("certificate_id") });
            }
            Placement_[target] = p;
            return p;
        }

        // ---- certificates ----

        /// <summary>Certificate ids of a language, easiest first.</summary>
        public static string[] CertificateIds(string target)
        {
            switch (target)
            {
                case "ja": return new[] { "cert_jlpt_n5", "cert_jlpt_n4", "cert_jlpt_n3", "cert_jlpt_n2", "cert_jlpt_n1" };
                case "en": return new[] { "cert_english_a1", "cert_english_a2", "cert_english_b1", "cert_english_b2", "cert_english_c1", IeltsStyleId };
                case "ru": return new[] { "cert_torfl_a1", "cert_torfl_a2", "cert_torfl_b1", "cert_torfl_b2", "cert_torfl_c1" };
                case "vi": return new[] { "cert_vi_bac1", "cert_vi_bac2", "cert_vi_bac3", "cert_vi_bac4", "cert_vi_bac5" };
                default: return new string[0];
            }
        }

        public static CourseCertificate Certificate(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Certificates_.TryGetValue(id, out var cached)) return cached;
            var f = Read("certificates/" + id);
            if (f == null) return null;
            var c = id == IeltsStyleId ? ParseIeltsStyle(f) : ParseCertificate(f);
            Certificates_[id] = c;
            return c;
        }

        private static CourseCertificate Header(Dictionary<string, object> f)
        {
            return new CourseCertificate
            {
                Id = f.Str("id"), Track = f.Str("track"), Target = f.Str("target"), Level = f.Str("level"), Cefr = f.Str("cefr"), Disclaimer = f.Str("disclaimer") ?? f.Str("notes_pass_ratio"),
                Title = CourseText.From(f.ContainsKey("title") ? f["title"] : null), Label = CourseText.From(f.ContainsKey("label") ? f["label"] : null),
                TimeLimitMinutes = (int)f.Num("time_limit_minutes"), PassRatio = f.Num("pass_ratio", 0.6),
            };
        }

        private static CourseCertificate ParseCertificate(Dictionary<string, object> f)
        {
            var c = Header(f);
            var passages = new Dictionary<string, Dictionary<string, object>>();
            foreach (var n in CourseQuestionParser.Items(f, "passages"))
            {
                var p = CourseQuestionParser.Obj(n);
                if (p != null) passages[p.Str("id")] = p;
            }
            var byId = new Dictionary<string, Dictionary<string, object>>();
            foreach (var n in CourseQuestionParser.Items(f, "questions"))
            {
                var q = CourseQuestionParser.Obj(n);
                if (q != null) byId[q.Str("id")] = q;
            }
            foreach (var n in CourseQuestionParser.Items(f, "sections"))
            {
                var s = CourseQuestionParser.Obj(n);
                if (s == null) continue;
                var section = new CourseSection { Id = s.Str("id"), Label = CourseText.From(s.ContainsKey("label") ? s["label"] : null), Directions = s.Str("directions") };
                foreach (var qid in CourseQuestionParser.Strings(s, "question_ids"))
                    if (byId.TryGetValue(qid, out var q)) section.Questions.Add(CourseQuestionParser.CertificateQuestion(q, c.Target, section.Id, passages));
                c.Sections.Add(section);
            }
            return c;
        }

        private static CourseCertificate ParseIeltsStyle(Dictionary<string, object> f)
        {
            var c = Header(f);
            c.PassRatio = 0.6;
            if (c.Label.IsEmpty) c.Label = c.Title;
            foreach (var n in CourseQuestionParser.Items(f, "sections"))
            {
                var s = CourseQuestionParser.Obj(n);
                if (s == null) continue;
                c.TimeLimitMinutes += (int)s.Num("time_limit_minutes");
                string kind = s.Str("section");
                var section = new CourseSection { Id = s.Str("id"), Label = CourseText.From(s.ContainsKey("label") ? s["label"] : null), Directions = s.Str("directions") };
                if (kind == "reading")
                {
                    foreach (var tn in CourseQuestionParser.Items(s, "texts"))
                    {
                        var t = CourseQuestionParser.Obj(tn);
                        if (t == null) continue;
                        string body = t.Str("text");
                        Dictionary<string, string> headings = null;
                        var paragraphs = t.Obj("paragraphs");
                        if (paragraphs != null)
                        {
                            var sb = new System.Text.StringBuilder();
                            foreach (var kv in paragraphs) sb.Append(kv.Key).Append("  ").Append(kv.Value).Append("\n\n");
                            body = sb.ToString().TrimEnd();
                            headings = new Dictionary<string, string>();
                            foreach (var hn in CourseQuestionParser.Items(t, "headings"))
                            {
                                var h = CourseQuestionParser.Obj(hn);
                                if (h != null) headings[h.Str("id")] = h.Str("text");
                            }
                        }
                        foreach (var inode in CourseQuestionParser.Items(t, "items"))
                        {
                            var it = CourseQuestionParser.Obj(inode);
                            if (it != null) section.Questions.Add(CourseQuestionParser.IeltsItem(it, section.Id, body, t.Str("title"), false, null, null, headings));
                        }
                    }
                }
                else if (kind == "listening")
                {
                    foreach (var pn in CourseQuestionParser.Items(s, "parts"))
                    {
                        var part = CourseQuestionParser.Obj(pn);
                        if (part == null) continue;
                        var lines = new List<string>();
                        string locale = null;
                        foreach (var an in CourseQuestionParser.Items(part, "audio_text"))
                        {
                            var a = CourseQuestionParser.Obj(an);
                            if (a == null) continue;
                            lines.Add(a.Str("text"));
                            locale = a.Str("locale");
                        }
                        foreach (var inode in CourseQuestionParser.Items(part, "items"))
                        {
                            var it = CourseQuestionParser.Obj(inode);
                            if (it != null) section.Questions.Add(CourseQuestionParser.IeltsItem(it, section.Id, string.Join("\n", lines), null, true, lines, locale, null));
                        }
                    }
                }
                else if (kind == "writing")
                {
                    foreach (var tn in CourseQuestionParser.Items(s, "tasks"))
                    {
                        var t = CourseQuestionParser.Obj(tn);
                        if (t == null) continue;
                        var card = new CoursePromptCard { Id = t.Str("id"), Kind = t.Str("kind"), Prompt = t.Str("prompt"), MinWords = (int)t.Num("min_words"), Minutes = (int)t.Num("minutes"), Title = t.Str("kind") };
                        var table = t.Obj("table");
                        if (table != null)
                        {
                            card.Lines.Add(string.Join("  |  ", CourseQuestionParser.Strings(table, "columns")));
                            foreach (var rn in CourseQuestionParser.Items(table, "rows"))
                                if (rn is List<object> row) { var cells = new List<string>(); foreach (var cell in row) cells.Add(cell as string ?? ""); card.Lines.Add(string.Join("  |  ", cells)); }
                        }
                        c.Prompts.Add(card);
                    }
                    c.SelfCheck = CourseQuestionParser.Strings(s, "self_check");
                    continue;
                }
                else if (kind == "speaking")
                {
                    foreach (var key in new[] { "part1", "part2", "part3" })
                        foreach (var pn in CourseQuestionParser.Items(s, key))
                        {
                            var p = CourseQuestionParser.Obj(pn);
                            if (p == null) continue;
                            var card = new CoursePromptCard { Id = p.Str("id"), Kind = key, Title = p.Str("topic") ?? p.Str("for") ?? key, Prompt = p.Str("cue_card") };
                            card.Lines.AddRange(CourseQuestionParser.Strings(p, "questions"));
                            card.Lines.AddRange(CourseQuestionParser.Strings(p, "bullets"));
                            if (!string.IsNullOrEmpty(p.Str("explain"))) card.Lines.Add(p.Str("explain"));
                            card.Minutes = (int)(p.Num("speak_seconds") / 60);
                            c.Prompts.Add(card);
                        }
                    continue;
                }
                if (section.Questions.Count > 0) c.Sections.Add(section);
            }
            return c;
        }

        // ---- learner-choice model ----

        public static Dictionary<string, object> Options => _options ?? (_options = Read("model/learner_choice_options") ?? new Dictionary<string, object>());
    }
}