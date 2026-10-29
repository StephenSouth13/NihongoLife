using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NihongoLife.Content
{
    /// <summary>
    /// Word lookup over Resources/Content/Dictionary/{from}-{to}.json (languages vi, en, ja, ru; 12 directions, original
    /// content, see docs/content/dictionary_design.md). Only the chosen pair file is loaded, lazily, and cached.
    /// Lookups ignore case, accents on Latin/Cyrillic text are kept, katakana is folded to hiragana and Japanese
    /// headwords are also found by their reading.
    /// </summary>
    public static class DictionaryService
    {
        public static readonly string[] Languages = { "vi", "en", "ja", "ru" };

        public sealed class Hit
        {
            public string Headword, Meaning, Reading, PartOfSpeech, Id;
            public string[] Domains;
        }

        [Serializable] private sealed class Sense { public string id, t, p, r; public string[] d; }
        [Serializable] private sealed class Item { public string k; public Sense[] v; }
        [Serializable] private sealed class File { public string from, to; public int count; public Item[] entries; }

        private sealed class Pair
        {
            public List<Item> items = new List<Item>();
            public List<string> keys = new List<string>();
            public Dictionary<string, List<int>> exact = new Dictionary<string, List<int>>();
        }

        private static readonly Dictionary<string, Pair> _pairs = new Dictionary<string, Pair>();

        public static bool Supports(string from, string to) =>
            from != to && Array.IndexOf(Languages, from) >= 0 && Array.IndexOf(Languages, to) >= 0;

        public static int HeadwordCount(string from, string to) => Load(from, to).items.Count;

        public static void ClearCache() => _pairs.Clear();

        /// <summary>Up to <paramref name="max"/> hits: exact headword (or reading) first, then prefix, then contains.</summary>
        public static List<Hit> Search(string from, string to, string query, int max = 30)
        {
            var result = new List<Hit>();
            string q = Normalize(query);
            if (q.Length == 0 || !Supports(from, to)) return result;
            var pair = Load(from, to);
            var seen = new HashSet<int>();
            if (pair.exact.TryGetValue(q, out var exact)) foreach (int i in exact) if (seen.Add(i)) Add(result, pair.items[i], max);
            for (int pass = 0; pass < 2 && result.Count < max; pass++)
                for (int i = 0; i < pair.keys.Count && result.Count < max; i++)
                {
                    if (seen.Contains(i)) continue;
                    string key = pair.keys[i];
                    if (pass == 0 ? key.StartsWith(q, StringComparison.Ordinal) : key.IndexOf(q, StringComparison.Ordinal) >= 0)
                    { seen.Add(i); Add(result, pair.items[i], max); }
                }
            return result;
        }

        private static void Add(List<Hit> hits, Item item, int max)
        {
            foreach (var sense in item.v)
            {
                if (hits.Count >= max) return;
                hits.Add(new Hit { Headword = item.k, Meaning = sense.t, Reading = sense.r, PartOfSpeech = sense.p, Id = sense.id, Domains = sense.d ?? Array.Empty<string>() });
            }
        }

        private static Pair Load(string from, string to)
        {
            string name = from + "-" + to;
            if (_pairs.TryGetValue(name, out var cached)) return cached;
            var pair = new Pair();
            _pairs[name] = pair;
            var asset = Resources.Load<TextAsset>("Content/Dictionary/" + name);
            if (asset == null) { Debug.LogWarning("DictionaryService: missing " + name); return pair; }
            var file = JsonUtility.FromJson<File>(asset.text);
            if (file == null || file.entries == null) return pair;
            foreach (var item in file.entries)
            {
                if (item == null || string.IsNullOrEmpty(item.k) || item.v == null) continue;
                int index = pair.items.Count;
                pair.items.Add(item);
                string key = Normalize(item.k);
                pair.keys.Add(key);
                Index(pair, key, index);
                foreach (var sense in item.v)
                    if (!string.IsNullOrEmpty(sense.r)) Index(pair, Normalize(sense.r), index);
            }
            return pair;
        }

        private static void Index(Pair pair, string key, int index)
        {
            if (!pair.exact.TryGetValue(key, out var list)) pair.exact[key] = list = new List<int>();
            if (!list.Contains(index)) list.Add(index);
        }

        /// <summary>Lower-case, trimmed, katakana folded to hiragana.</summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            foreach (char c in text.Trim().ToLowerInvariant())
                sb.Append(c >= '\u30A1' && c <= '\u30F6' ? (char)(c - 0x60) : c);
            return sb.ToString();
        }
    }
}
