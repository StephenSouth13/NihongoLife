using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Progression
{
    /// <summary>An industry groups careers on the choose-a-career screen (Resources/Progression/career_industries.json).</summary>
    [Serializable]
    public sealed class IndustryDefinition
    {
        public string id, color;
        public string nameVi, nameEn, nameJa, nameRu;
        public string blurbVi, blurbEn, blurbJa, blurbRu;
        public string[] careers = Array.Empty<string>();

        public string Name => Pick(nameVi, nameEn, nameJa, nameRu);
        public string Blurb => Pick(blurbVi, blurbEn, blurbJa, blurbRu);
        public Color Tint => ColorUtility.TryParseHtmlString(color, out var c) ? c : new Color(0.7f, 0.7f, 0.7f);

        private static string Pick(string vi, string en, string ja, string ru)
        {
            switch (Loc.Code)
            {
                case "en": return en;
                case "ja": return string.IsNullOrEmpty(ja) ? en : ja;
                case "ru": return string.IsNullOrEmpty(ru) ? en : ru;
                default: return string.IsNullOrEmpty(vi) ? en : vi;
            }
        }
    }

    /// <summary>How a career looks on its card: the uniform (a Kids-family variant), the work loop and a second move.</summary>
    [Serializable]
    public sealed class CareerLook
    {
        public string career, industry, variant, gesture, alt, prop;
    }

    [Serializable]
    public sealed class CareerIndustryFile
    {
        public IndustryDefinition[] industries = Array.Empty<IndustryDefinition>();
        public CareerLook[] looks = Array.Empty<CareerLook>();
    }

    public static class CareerIndustries
    {
        public const string Path = "Progression/career_industries";
        private static CareerIndustryFile _file;

        public static CareerIndustryFile File
        {
            get
            {
                if (_file != null) return _file;
                var asset = Resources.Load<TextAsset>(Path);
                _file = asset != null ? JsonUtility.FromJson<CareerIndustryFile>(asset.text) ?? new CareerIndustryFile() : new CareerIndustryFile();
                return _file;
            }
        }

        public static void Reset() => _file = null;

        public static CareerLook Look(string careerId) => File.looks.FirstOrDefault(l => l.career == careerId);

        public static IndustryDefinition IndustryOf(string careerId) => File.industries.FirstOrDefault(i => i.careers.Contains(careerId));

        /// <summary>The industries that hold at least one career of the catalog, in file order, each with its careers in the industry's order.
        /// A career that no industry lists goes into a trailing "other" group so nothing is ever hidden.</summary>
        public static List<(IndustryDefinition industry, List<CareerDefinition> careers)> Group(IEnumerable<CareerDefinition> all)
        {
            var list = all.ToList();
            var used = new HashSet<string>();
            var result = new List<(IndustryDefinition, List<CareerDefinition>)>();
            foreach (var industry in File.industries)
            {
                var members = industry.careers.Select(id => list.FirstOrDefault(c => c.id == id)).Where(c => c != null).ToList();
                if (members.Count == 0) continue;
                foreach (var m in members) used.Add(m.id);
                result.Add((industry, members));
            }
            var rest = list.Where(c => !used.Contains(c.id)).ToList();
            if (rest.Count > 0)
                result.Add((new IndustryDefinition { id = "other", color = "#9AA7B8", nameVi = "Khác", nameEn = "Other", nameJa = "その他", nameRu = "Другое", careers = rest.Select(c => c.id).ToArray() }, rest));
            return result;
        }
    }
}