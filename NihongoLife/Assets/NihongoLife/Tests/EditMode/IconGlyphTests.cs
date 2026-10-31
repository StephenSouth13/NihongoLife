using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>Catches "black square" UI: icon names with no sprite, and UI text characters the font cannot draw.</summary>
    public class IconGlyphTests
    {
        static readonly Regex[] IconCalls =
        {
            new Regex(@"UiKit\.(?:Icon|IconButton)\([^;]*?""([a-z_0-9]+)""", RegexOptions.Singleline),
            new Regex(@"UiKit\.Button\([^;]*?Kind\.\w+,\s*""([a-z_0-9]+)""", RegexOptions.Singleline),
            new Regex(@"UiIcons\.Get\(""([a-z_0-9]+)""\)"),
        };

        static IEnumerable<string> Sources(string sub) =>
            Directory.GetFiles(Path.Combine(Application.dataPath, "NihongoLife/Scripts", sub), "*.cs", SearchOption.AllDirectories);

        [Test] public void EveryUiIcon_HasASprite()
        {
            var missing = new SortedSet<string>();
            foreach (var f in Sources("UI"))
            {
                string s = File.ReadAllText(f);
                foreach (var re in IconCalls)
                    foreach (Match m in re.Matches(s))
                        if (!m.Groups[1].Value.StartsWith("coin") && UiIcons.Get(m.Groups[1].Value) == null)
                            missing.Add(m.Groups[1].Value + " (" + Path.GetFileName(f) + ")");
            }
            Assert.IsEmpty(missing, "icons without a sprite: " + string.Join(", ", missing));
        }

        [Test] public void Aliases_Resolve()
        {
            Assert.IsNotNull(UiIcons.Get("route"));
            Assert.IsNotNull(UiIcons.Get("writing"));
            Assert.IsNotNull(UiIcons.Get("certificate"));
        }

        static readonly Regex LocCall = new Regex(@"Loc\.[TF]\(\s*((?:""(?:[^""\\]|\\.)*""\s*,\s*){2}""(?:[^""\\]|\\.)*"")");
        static readonly Regex Literal = new Regex(@"""((?:[^""\\]|\\.)*)""");

        [Test] public void UiStrings_HaveGlyphsInTheFont()
        {
            var font = NLUi.ResolveFont();
            Assert.IsNotNull(font, "UI font");
            var bad = new SortedDictionary<char, string>();
            foreach (var f in Sources("UI"))
            {
                foreach (Match call in LocCall.Matches(File.ReadAllText(f)))
                    foreach (Match lit in Literal.Matches(call.Groups[1].Value))
                    {
                        string text = Regex.Unescape(lit.Groups[1].Value);
                        text = Regex.Replace(text, "<[^>]+>", "");
                        foreach (char c in text)
                        {
                            if (char.IsWhiteSpace(c) || char.IsControl(c) || char.IsSurrogate(c) || bad.ContainsKey(c)) continue;
                            if (!font.HasCharacter(c, true, true)) bad[c] = Path.GetFileName(f);
                        }
                    }
            }
            Assert.IsEmpty(bad, "characters the UI font cannot draw: " + string.Join(", ", bad.Select(kv => $"U+{(int)kv.Key:X4} '{kv.Key}' ({kv.Value})")));
        }
    }
}
