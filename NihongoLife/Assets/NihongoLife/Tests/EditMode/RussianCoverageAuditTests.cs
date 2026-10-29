using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>
    /// Audit (a triple may start at any string literal, so a leading name argument does not hide it): every ("vi", "en", "ja") string triple in the runtime scripts must have its English text in the Russian table
    /// (ru_table.json + ru_table_extra.json). The missing ones are listed in Bao_Cao/russian-ui/coverage_missing.txt and the
    /// number may only go down (new UI text must come with a Russian line).
    /// </summary>
    public sealed class RussianCoverageAuditTests
    {
        private const int AllowedMissing = 0;
        private static readonly Regex Triple = new Regex("(?=\"((?:[^\"\\\\\\n]|\\\\.)*)\"\\s*,\\s*\"((?:[^\"\\\\\\n]|\\\\.)*)\"\\s*,\\s*\"((?:[^\"\\\\\\n]|\\\\.)*)\")", RegexOptions.Compiled);
        private static readonly Regex Cjk = new Regex("[\\u3040-\\u30ff\\u4e00-\\u9fff\\uff00-\\uffef]", RegexOptions.Compiled);
        private static readonly Regex Latin = new Regex("[A-Za-z]{2}", RegexOptions.Compiled);
        private static readonly Regex ViMarks = new Regex("[\\u00c0-\\u1ef9]", RegexOptions.Compiled);

        [Test]
        public void EveryUiTriple_HasARussianLine()
        {
            RuTable.Reload();
            var missing = new SortedDictionary<string, string>();
            int total = 0;
            foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "NihongoLife/Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Editor/")) continue;
                foreach (Match m in Triple.Matches(File.ReadAllText(file)))
                {
                    string en = m.Groups[2].Value.Replace("\\\"", "\"").Replace("\\n", "\n");
                    if (!Cjk.IsMatch(m.Groups[3].Value) || Cjk.IsMatch(en) || !Latin.IsMatch(en) || ViMarks.IsMatch(en)) continue;
                    total++;
                    if (!RuTable.TryGet(en, out _)) missing[en] = Path.GetFileName(file);
                }
            }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/russian-ui"));
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder("triples checked: " + total + "   missing a Russian line: " + missing.Count + "\n");
            foreach (var kv in missing) sb.AppendLine(kv.Value + "\t" + kv.Key.Replace("\n", "\\n"));
            File.WriteAllText(Path.Combine(dir, "coverage_missing.txt"), sb.ToString());
            Assert.LessOrEqual(missing.Count, AllowedMissing, "missing Russian lines, see Bao_Cao/russian-ui/coverage_missing.txt");
        }
    }
}
