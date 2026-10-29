using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Gestures
{
    /// <summary>One way of greeting, thanking or parting: the body language of a culture (a gesture of the library) and its words.</summary>
    [Serializable]
    public sealed class Greeting
    {
        public string id, gesture, ctx, culture, cultureVi, cultureEn, cultureJa, cultureRu, native, roman, vi, en, ja, ru;

        public string Culture => Loc.Code switch { "en" => cultureEn, "ja" => cultureJa, "ru" => cultureRu, _ => cultureVi };
        public string Meaning => Loc.Code switch { "en" => en, "ja" => ja, "ru" => ru, _ => vi };
    }

    [Serializable] public sealed class GreetingFile { public string schema; public Greeting[] greetings = Array.Empty<Greeting>(); }

    /// <summary>
    /// Greetings by culture and context (Resources/Animation/greetings.json). Contexts: formal, casual, thanks, farewell, celebrate.
    /// <see cref="Cycle"/> walks through the cultures one tap after another, so the player sees a bow, a wai, a namaste, a handshake ...
    /// </summary>
    public static class GreetingCatalog
    {
        public const string Path = "Animation/greetings";
        private static GreetingFile _file;
        private static readonly Dictionary<string, int> Cursor = new Dictionary<string, int>();

        public static IReadOnlyList<Greeting> All
        {
            get
            {
                if (_file == null)
                {
                    var asset = Resources.Load<TextAsset>(Path);
                    _file = asset != null ? JsonUtility.FromJson<GreetingFile>(asset.text) ?? new GreetingFile() : new GreetingFile();
                }
                return _file.greetings;
            }
        }

        public static void Reset() { _file = null; Cursor.Clear(); }

        public static Greeting Find(string id) => All.FirstOrDefault(g => g.id == id);
        public static IEnumerable<Greeting> InContext(string ctx) => All.Where(g => g.ctx == ctx);
        public static IEnumerable<Greeting> OfCulture(string culture) => All.Where(g => g.culture == culture);

        /// <summary>The next greeting of the context (round robin), or the one of the asked culture when given.</summary>
        public static Greeting Pick(string ctx, string culture = null)
        {
            var list = All.Where(g => g.ctx == ctx && (culture == null || g.culture == culture)).ToList();
            if (list.Count == 0) list = All.Where(g => g.ctx == ctx).ToList();
            if (list.Count == 0) return null;
            return list[Mathf.Abs(Cursor.TryGetValue(ctx, out var c) ? c : 0) % list.Count];
        }

        /// <summary>Takes the next greeting of the context and moves the cursor on.</summary>
        public static Greeting Cycle(string ctx)
        {
            var g = Pick(ctx);
            Cursor[ctx] = (Cursor.TryGetValue(ctx, out var c) ? c : 0) + 1;
            return g;
        }

        /// <summary>Plays the greeting's gesture on <paramref name="who"/> (crossfades into it).</summary>
        public static bool Perform(GameObject who, Greeting greeting)
        {
            if (who == null || greeting == null) return false;
            return GestureLayer.On(who).Play(greeting.gesture);
        }
    }
}