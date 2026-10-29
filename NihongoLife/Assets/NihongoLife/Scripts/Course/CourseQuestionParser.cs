using System;
using System.Collections.Generic;
using NihongoLife.Core;

namespace NihongoLife.Course
{
    /// <summary>Turns the school JSON (units, alphabets, placement, certificates) into <see cref="CourseQuestion"/>s.</summary>
    public static class CourseQuestionParser
    {
        public static List<object> Items(Dictionary<string, object> d, string key) => d != null ? d.Arr(key) ?? new List<object>() : new List<object>();
        public static Dictionary<string, object> Obj(object node) => node as Dictionary<string, object>;

        public static List<string> Strings(Dictionary<string, object> d, string key)
        {
            var list = new List<string>();
            foreach (var n in Items(d, key)) if (n is string s) list.Add(s);
            return list;
        }

        private static void ReadAudio(Dictionary<string, object> item, CourseQuestion q)
        {
            var audio = item.Obj("audio");
            if (audio == null) return;
            q.StemAudio = audio.Str("text");
            q.StemAudioLocale = audio.Str("locale");
        }

        public static List<CourseChoice> Choices(Dictionary<string, object> item, string key = "choices")
        {
            var list = new List<CourseChoice>();
            foreach (var n in Items(item, key))
            {
                var c = Obj(n);
                if (c == null) continue;
                list.Add(new CourseChoice { Id = c.Str("id"), Text = CourseText.From(c.ContainsKey("text") ? c["text"] : null) });
            }
            return list;
        }

        /// <summary>One unit exercise (8 per unit) -> its questions.</summary>
        public static CourseExercise Exercise(Dictionary<string, object> ex, string target, Func<string, string> passage)
        {
            var result = new CourseExercise
            {
                Id = ex.Str("id"), Type = ex.Str("type"), Skill = ex.Str("skill"),
                Prompt = CourseText.From(ex.ContainsKey("prompt") ? ex["prompt"] : null),
            };
            string passageId = ex.Str("passage_id");
            foreach (var node in Items(ex, "items"))
            {
                var it = Obj(node);
                if (it == null) continue;
                var q = new CourseQuestion { Id = it.Str("id"), Skill = result.Skill, SourceType = result.Type, Target = target, Prompt = result.Prompt };
                switch (result.Type)
                {
                    case "multiple_choice":
                    case "listening":
                        q.Kind = CourseQuestionKind.Choice;
                        q.Stem = CourseText.From(it.ContainsKey("stem") ? it["stem"] : null);
                        ReadAudio(it, q);
                        q.Choices = Choices(it);
                        q.AnswerId = it.Str("answer");
                        break;
                    case "matching":
                        q.Kind = CourseQuestionKind.Matching;
                        foreach (var p in Items(it, "pairs"))
                        {
                            var po = Obj(p);
                            if (po == null) continue;
                            q.Pairs.Add(new CoursePair { Id = po.Str("id"), Left = CourseText.From(po["left"]), Right = CourseText.From(po["right"]) });
                        }
                        break;
                    case "typing":
                        q.Kind = CourseQuestionKind.Typing;
                        q.Stem = CourseText.From(it.ContainsKey("stem") ? it["stem"] : null);
                        q.Accepted = Strings(it, "accepted_answers");
                        q.IgnoreCase = it.Bool("ignore_case", true);
                        q.IgnorePunctuation = it.Bool("ignore_punctuation", true);
                        q.IgnoreAccents = it.Bool("accent_insensitive", false);
                        break;
                    case "fill_in":
                        q.Blank = it.Str("text_with_blank");
                        q.Translation = CourseText.From(it.ContainsKey("translation") ? it["translation"] : null);
                        if (it.Str("mode") == "typing")
                        {
                            q.Kind = CourseQuestionKind.Typing;
                            q.Accepted = Strings(it, "accepted_answers");
                            if (q.Accepted.Count == 0 && !string.IsNullOrEmpty(it.Str("answer"))) q.Accepted.Add(it.Str("answer"));
                        }
                        else
                        {
                            q.Kind = CourseQuestionKind.Choice;
                            q.Choices = Choices(it);
                            q.AnswerId = it.Str("answer_choice_id");
                        }
                        break;
                    case "ordering":
                        q.Kind = CourseQuestionKind.Ordering;
                        q.Tokens = Strings(it, "tokens_shuffled");
                        q.AnswerTokens = Strings(it, "answer_tokens");
                        q.Translation = CourseText.From(it.ContainsKey("translation") ? it["translation"] : null);
                        string full = it.Str("answer_text") ?? "";
                        q.TokenSeparator = string.Join(" ", q.AnswerTokens) == full ? " " : "";
                        q.Accepted.Add(full);
                        break;
                    case "reading":
                        if (!string.IsNullOrEmpty(passageId) && passage != null) q.Context = passage(passageId);
                        if (it.Str("kind") == "true_false")
                        {
                            q.Kind = CourseQuestionKind.TrueFalse;
                            q.Stem = CourseText.From(it.ContainsKey("statement") ? it["statement"] : null);
                            q.AnswerBool = it.Bool("answer");
                        }
                        else
                        {
                            q.Kind = CourseQuestionKind.Choice;
                            q.Stem = CourseText.From(it.ContainsKey("stem") ? it["stem"] : null);
                            q.Choices = Choices(it);
                            q.AnswerId = it.Str("answer");
                        }
                        break;
                    default:
                        continue;
                }
                result.Questions.Add(q);
            }
            return result;
        }

        public static CourseQuestion AlphabetQuestion(Dictionary<string, object> it, string target)
        {
            var q = new CourseQuestion
            {
                Id = it.Str("id"), Skill = "alphabet", SourceType = it.Str("type"), Target = target, GroupId = it.Str("lesson_id"),
                Kind = CourseQuestionKind.Choice, Prompt = CourseText.From(it.ContainsKey("prompt") ? it["prompt"] : null),
                Stem = CourseText.From(it.ContainsKey("stem") ? it["stem"] : null), Choices = Choices(it), AnswerId = it.Str("answer"),
            };
            ReadAudio(it, q);
            if (!string.IsNullOrEmpty(it.Str("reading"))) q.Translation = CourseText.Of(it.Str("reading"));
            return q;
        }

        public static CourseQuestion PlacementQuestion(Dictionary<string, object> it, string target)
        {
            return new CourseQuestion
            {
                Id = it.Str("id"), Skill = "placement", SourceType = "placement", Target = target, GroupId = it.Str("level"),
                Kind = CourseQuestionKind.Choice, Stem = CourseText.Of(it.Str("stem")), Choices = Choices(it), AnswerId = it.Str("answer"), AllowSkip = true,
            };
        }

        public static CourseQuestion CertificateQuestion(Dictionary<string, object> it, string target, string sectionId, Dictionary<string, Dictionary<string, object>> passages)
        {
            var q = new CourseQuestion
            {
                Id = it.Str("id"), Skill = sectionId, SourceType = "certificate", Target = target, GroupId = sectionId,
                Kind = CourseQuestionKind.Choice, Stem = CourseText.Of(it.Str("stem")), Choices = Choices(it), AnswerId = it.Str("answer"),
            };
            string pid = it.Str("passage_id");
            if (!string.IsNullOrEmpty(pid) && passages.TryGetValue(pid, out var p))
            {
                if (p.Str("kind") == "listening")
                {
                    q.ContextIsListening = true;
                    q.Context = p.Str("script");
                    foreach (var n in Items(p, "audio_text"))
                    {
                        var a = Obj(n);
                        if (a == null) continue;
                        q.AudioLines.Add(a.Str("text"));
                        q.AudioLocale = a.Str("locale");
                    }
                }
                else q.Context = p.Str("text");
            }
            return q;
        }

        /// <summary>Items of the IELTS-style practice (reading / listening parts).</summary>
        public static CourseQuestion IeltsItem(Dictionary<string, object> it, string sectionId, string context, string title, bool listening, List<string> audioLines, string audioLocale,
            Dictionary<string, string> headings)
        {
            var q = new CourseQuestion { Id = it.Str("id"), Skill = sectionId, SourceType = "ielts_style", Target = "en", GroupId = sectionId, Context = context, ContextTitle = title, ContextIsListening = listening };
            if (audioLines != null) { q.AudioLines = audioLines; q.AudioLocale = audioLocale; }
            string type = it.Str("type");
            q.Stem = CourseText.Of(it.Str("stem"));
            if (it.ContainsKey("accepted_answers"))
            {
                q.Kind = CourseQuestionKind.Typing;
                q.Accepted = Strings(it, "accepted_answers");
            }
            else
            {
                q.Kind = CourseQuestionKind.Choice;
                q.AnswerId = it.Str("answer");
                if (type == "matching_heading" && headings != null)
                {
                    q.Stem = CourseText.Of("Paragraph " + it.Str("paragraph"));
                    foreach (var kv in headings) q.Choices.Add(new CourseChoice { Id = kv.Key, Text = CourseText.Of(kv.Value) });
                }
                else q.Choices = Choices(it);
            }
            return q;
        }
    }
}