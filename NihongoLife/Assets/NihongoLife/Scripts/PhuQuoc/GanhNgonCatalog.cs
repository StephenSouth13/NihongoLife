using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>A dish on Gánh Ngon's menu. No prices on purpose: the real menu's prices are not invented here.</summary>
    [Serializable]
    public sealed class GanhNgonDish
    {
        public string id, category, nameVi, nameEn, nameJa, readingJa, descVi, descEn, descJa;
        /// <summary>Opening minutes of the day when it is served (cơm niêu: lunch only). 0/0 = all opening hours.</summary>
        public int fromMinute, toMinute;
        public string Name => Loc.Pick(nameVi, nameEn, nameJa);
        public string Description => Loc.Pick(descVi, descEn, descJa);
    }

    /// <summary>A pre-written service phrase (no AI): the Japanese and English a waiter / receptionist says.</summary>
    [Serializable]
    public sealed class GanhNgonPhrase
    {
        public string id, situation, ja, readingJa, en, vi;
        public string Meaning => Loc.Pick(vi, en, ja);
    }

    [Serializable]
    public sealed class GanhNgonChoice
    {
        public string ja, en, vi, hintVi, hintEn, hintJa;
        public bool correct;
        public string Hint => Loc.Pick(hintVi, hintEn, hintJa);
    }

    /// <summary>One step of a scripted service dialogue: a guest line and the staff answers to pick from.</summary>
    [Serializable]
    public sealed class GanhNgonStep
    {
        public string id, speaker, lineJa, readingJa, lineEn, lineVi, questVi, questEn, questJa;
        /// <summary>Quest event raised when answered right (order / serve / talk ...), target from the dialogue.</summary>
        public string questEvent;
        public GanhNgonChoice[] choices = Array.Empty<GanhNgonChoice>();
    }

    /// <summary>A whole scripted scene for one job (waiter, receptionist, chef, bartender).</summary>
    [Serializable]
    public sealed class GanhNgonDialogue
    {
        public string id, job, guestVi, guestEn, guestJa, guestLanguage, target;
        public GanhNgonStep[] steps = Array.Empty<GanhNgonStep>();
        public string Guest => Loc.Pick(guestVi, guestEn, guestJa);
    }

    [Serializable]
    public sealed class GanhNgonStaff
    {
        public string id, job, nameVi, nameEn, nameJa, roleVi, roleEn, roleJa;
        public string Name => Loc.Pick(nameVi, nameEn, nameJa);
        public string Role => Loc.Pick(roleVi, roleEn, roleJa);
    }

    /// <summary>
    /// Gánh Ngon (Resources/PhuQuoc/ganh_ngon.json): Huy's real restaurant at 113 Trần Hưng Đạo, Dương Đông,
    /// Phú Quốc — grilled seafood BBQ, grilled beef, chicken-durian hotpot, fried durian, cơm niêu lunch
    /// (10:00–13:00), a pub / cocktail bar and 9 VIP rooms, open 10:00–23:00, guests from everywhere.
    /// The restaurant / service career (careers.json "hospitality") works its shift here. Everything the
    /// staff and guests say is pre-written in Vietnamese, English and Japanese.
    /// </summary>
    [Serializable]
    public sealed class GanhNgonCatalog
    {
        public const string ResourcePath = "PhuQuoc/ganh_ngon";
        public static readonly string[] Jobs = { "waiter", "receptionist", "chef", "bartender" };
        public static readonly string[] Categories = { "seafood_bbq", "grill", "hotpot", "durian", "com_nieu", "drink" };

        public string schema, nameVi, nameEn, nameJa, address, sourceNote;
        public int openMinute = 600, closeMinute = 1380, vipRooms = 9;
        public GanhNgonDish[] dishes = Array.Empty<GanhNgonDish>();
        public GanhNgonPhrase[] phrases = Array.Empty<GanhNgonPhrase>();
        public GanhNgonDialogue[] dialogues = Array.Empty<GanhNgonDialogue>();
        public GanhNgonStaff[] staff = Array.Empty<GanhNgonStaff>();

        private static GanhNgonCatalog _cached;

        public static GanhNgonCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? Parse(text.text) : new GanhNgonCatalog();
            return _cached;
        }

        public static GanhNgonCatalog Parse(string json) => JsonUtility.FromJson<GanhNgonCatalog>(json) ?? new GanhNgonCatalog();

        public GanhNgonDish Dish(string id) => dishes.FirstOrDefault(d => d.id == id);
        public GanhNgonDialogue Dialogue(string id) => dialogues.FirstOrDefault(d => d.id == id);
        public IEnumerable<GanhNgonDialogue> DialoguesFor(string job) => dialogues.Where(d => d.job == job);

        /// <summary>Minute of day 0..1439. Open 10:00–23:00.</summary>
        public bool IsOpen(int minuteOfDay) => minuteOfDay >= openMinute && minuteOfDay < closeMinute;

        /// <summary>Dishes on the menu at that minute (cơm niêu only at lunch).</summary>
        public IEnumerable<GanhNgonDish> MenuAt(int minuteOfDay) =>
            IsOpen(minuteOfDay) ? dishes.Where(d => d.toMinute <= d.fromMinute || (minuteOfDay >= d.fromMinute && minuteOfDay < d.toMinute)) : Enumerable.Empty<GanhNgonDish>();

        private static readonly string[] Forbidden = { "wildlife", "thú rừng", "rắn", "snake", "pangolin", "tê tê", "turtle", "rùa", "ba ba", "dơi", "bat meat", "kỳ đà", "nhím", "porcupine", "shark fin", "vây cá mập", "dugong", "price", "giá", "đ/", "vnd", "₫" };

        /// <summary>Problems in the data (empty when the file is valid).</summary>
        public List<string> Validate()
        {
            var e = new List<string>();
            if (schema != "nihongolife.phuquoc.ganhngon.v1") e.Add("schema must be nihongolife.phuquoc.ganhngon.v1");
            if (openMinute != 600 || closeMinute != 1380) e.Add("Gánh Ngon is open 10:00–23:00");
            if (vipRooms != 9) e.Add("Gánh Ngon has 9 VIP rooms");
            var ids = new HashSet<string>();
            foreach (var d in dishes)
            {
                if (string.IsNullOrEmpty(d.id) || !ids.Add(d.id)) e.Add("dish id missing or duplicate: " + d.id);
                if (!Categories.Contains(d.category)) e.Add($"dish {d.id}: unknown category {d.category}");
                if (new[] { d.nameVi, d.nameEn, d.nameJa, d.readingJa }.Any(string.IsNullOrWhiteSpace)) e.Add($"dish {d.id}: needs vi/en/ja names and a reading");
                string all = (d.nameVi + d.nameEn + d.descVi + d.descEn + d.id).ToLowerInvariant();
                foreach (var f in Forbidden) if (all.Contains(f)) e.Add($"dish {d.id}: forbidden word '{f}' (no wildlife dishes, no invented prices)");
            }
            foreach (var c in Categories.Take(5)) if (!dishes.Any(d => d.category == c)) e.Add("no dish in category " + c);
            var comNieu = dishes.Where(d => d.category == "com_nieu").ToList();
            if (comNieu.Any(d => d.fromMinute != 600 || d.toMinute != 780)) e.Add("cơm niêu is served 10:00–13:00 only");
            foreach (var p in phrases)
                if (new[] { p.ja, p.readingJa, p.en, p.vi }.Any(string.IsNullOrWhiteSpace)) e.Add("phrase " + p.id + ": needs ja, reading, en and vi");
            foreach (var job in Jobs)
                if (!dialogues.Any(d => d.job == job)) e.Add("no dialogue for job " + job);
            var dIds = new HashSet<string>();
            foreach (var d in dialogues)
            {
                if (string.IsNullOrEmpty(d.id) || !dIds.Add(d.id)) e.Add("dialogue id missing or duplicate: " + d.id);
                if (!Jobs.Contains(d.job)) e.Add($"dialogue {d.id}: unknown job {d.job}");
                if (d.steps.Length < 2) e.Add($"dialogue {d.id}: needs at least two steps");
                foreach (var s in d.steps)
                {
                    if (new[] { s.lineJa, s.readingJa, s.lineEn, s.lineVi }.Any(string.IsNullOrWhiteSpace)) e.Add($"{d.id}/{s.id}: line needs ja, reading, en, vi");
                    if (s.choices.Length < 2) e.Add($"{d.id}/{s.id}: needs at least two answers");
                    if (s.choices.Count(c => c.correct) != 1) e.Add($"{d.id}/{s.id}: needs exactly one correct answer");
                    foreach (var c in s.choices)
                    {
                        if (new[] { c.ja, c.en, c.vi }.Any(string.IsNullOrWhiteSpace)) e.Add($"{d.id}/{s.id}: an answer needs ja, en, vi");
                        if (!c.correct && (string.IsNullOrWhiteSpace(c.hintVi) || string.IsNullOrWhiteSpace(c.hintEn))) e.Add($"{d.id}/{s.id}: wrong answer '{c.en}' needs a vi/en hint");
                    }
                }
            }
            foreach (var job in Jobs) if (!staff.Any(s => s.job == job)) e.Add("no staff member for job " + job);
            return e;
        }
    }
}
