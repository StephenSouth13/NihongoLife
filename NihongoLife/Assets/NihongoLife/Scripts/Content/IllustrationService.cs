using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NihongoLife.Content
{
    /// <summary>
    /// Companion pictures for dictionary words. A picture is shown in the handbook dictionary only after the learner has answered a
    /// question about that word correctly (the quiz feedback card shows it and unlocks it). Art: Twemoji (CC-BY 4.0), see Docs/CREDITS.md.
    /// </summary>
    public static class IllustrationService
    {
        private const string PrefKey = "illu_unlocked";
        [Serializable] private sealed class Entry { public string id, en, vi, ru, ja, reading; }
        [Serializable] private sealed class EntryFile { public Entry[] entries; }

        private static Dictionary<string, string> _images;                  // concept id -> png name
        private static Dictionary<string, string> _byText;                  // normalized word -> concept id
        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private static HashSet<string> _unlocked;
        public static string Credit { get; private set; } = "Twemoji graphics, CC-BY 4.0";

        public static int Count { get { Load(); return _images.Count; } }

        private static void Load()
        {
            if (_images != null) return;
            _images = new Dictionary<string, string>();
            _byText = new Dictionary<string, string>();
            var man = Resources.Load<TextAsset>("Content/Illustrations/illustrations");
            if (man == null) return;
            var credit = Regex.Match(man.text, "\"credit\"\\s*:\\s*\"([^\"]*)\"");
            if (credit.Success) Credit = credit.Groups[1].Value;
            foreach (Match m in Regex.Matches(man.text, "\"(w\\d+)\"\\s*:\\s*\"(tw_[0-9a-f_\\-]+)\""))
                _images[m.Groups[1].Value] = m.Groups[2].Value;
            var ents = Resources.Load<TextAsset>("Content/Dictionary/entries");
            if (ents == null) return;
            var file = JsonUtility.FromJson<EntryFile>(ents.text);
            if (file?.entries == null) return;
            foreach (var e in file.entries)
            {
                if (e == null || !_images.ContainsKey(e.id)) continue;
                foreach (var field in new[] { e.en, e.vi, e.ru, e.ja }  /* not the reading: homophones (e.g. hai = lung / yes) would give wrong pictures */)
                    foreach (var part in Split(field)) if (!_byText.ContainsKey(part)) _byText[part] = e.id;
            }
        }

        private static IEnumerable<string> Split(string field)
        {
            if (string.IsNullOrEmpty(field)) yield break;
            foreach (var raw in field.Split(',', ';', '/', '\u3001'))
            {
                var n = Clean(raw);
                if (n.Length > 0) yield return n;
            }
        }

        /// <summary>Lower-case, katakana folded, brackets and punctuation removed, "to " / "a " / "the " prefixes dropped.</summary>
        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Regex.Replace(s, "\\([^)]*\\)|\\uFF08[^\\uFF09]*\\uFF09", " ");
            var sb = new StringBuilder();
            foreach (char c in DictionaryService.Normalize(s))
                if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(c);
            var t = Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
            foreach (var pre in new[] { "to ", "a ", "an ", "the " }) if (t.StartsWith(pre) && t.Length > pre.Length + 1) { t = t.Substring(pre.Length); break; }
            return t;
        }

        public static bool Has(string conceptId) { Load(); return !string.IsNullOrEmpty(conceptId) && _images.ContainsKey(conceptId); }

        /// <summary>Concept id of the first candidate text that has a picture, or null.</summary>
        public static string Find(params string[] candidates)
        {
            Load();
            if (candidates == null) return null;
            foreach (var c in candidates)
                foreach (var part in Split(c))
                    if (_byText.TryGetValue(part, out var id)) return id;
            return null;
        }

        public static Sprite GetSprite(string conceptId)
        {
            Load();
            if (string.IsNullOrEmpty(conceptId) || !_images.TryGetValue(conceptId, out var name)) return null;
            if (_sprites.TryGetValue(name, out var sp)) return sp;
            var tex = Resources.Load<Texture2D>("Content/Illustrations/" + name);
            if (tex != null) { tex.filterMode = FilterMode.Bilinear; sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f); }
            _sprites[name] = sp;
            return sp;
        }

        private static void LoadUnlocked()
        {
            if (_unlocked != null) return;
            _unlocked = new HashSet<string>();
            foreach (var s in PlayerPrefs.GetString(PrefKey, "").Split(',')) if (s.Length > 0) _unlocked.Add(s);
        }

        public static bool IsUnlocked(string conceptId) { LoadUnlocked(); return !string.IsNullOrEmpty(conceptId) && _unlocked.Contains(conceptId); }
        public static int UnlockedCount { get { LoadUnlocked(); return _unlocked.Count; } }

        /// <summary>Unlocks the picture; true when it was newly unlocked.</summary>
        public static bool Unlock(string conceptId)
        {
            Load(); LoadUnlocked();
            if (!Has(conceptId) || !_unlocked.Add(conceptId)) return false;
            PlayerPrefs.SetString(PrefKey, string.Join(",", _unlocked));
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Quiz hook: a correct answer for this question unlocks the picture of the word it is about.</summary>
        public static string UnlockFor(params string[] candidates)
        {
            var id = Find(candidates);
            if (id != null) Unlock(id);
            return id;
        }

        /// <summary>Test hook: forget all unlocks and caches.</summary>
        public static void ResetForTests() { _unlocked = null; _images = null; _byText = null; _sprites.Clear(); PlayerPrefs.DeleteKey(PrefKey); }
    }
}