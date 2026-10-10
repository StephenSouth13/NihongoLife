using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.Island
{
    // ─────────── Catalog (Resources/Island/island_catalog.json) ───────────

    [Serializable] public sealed class IslandWord { public string ja, reading, en, vi; }
    [Serializable] public sealed class IslandSound { public string ja, en, vi; }
    [Serializable] public sealed class IslandKitItem { public string itemId; public int quantity; }

    [Serializable]
    public sealed class IslandCrop
    {
        public string id, model;
        public float secondsPerStage;
        public int seedPrice, sellPrice, yield;
        public IslandWord word;
        public string SeedItemId => "seed_" + id;
        public string ProduceItemId => id;
    }

    /// <summary>A tool; <c>durability</c> = uses before it breaks (0 = never wears out).</summary>
    [Serializable] public sealed class IslandTool { public string id, action; public int price, durability; public IslandWord word, verb; }
    /// <summary>A fish caught at the pier: model name (fish pack FBX), sell price and catch weight (relative odds).</summary>
    [Serializable] public sealed class IslandFish { public string id, model; public int sellPrice, weight; public IslandWord word; }
    [Serializable] public sealed class IslandAnimalDef { public string id, model; public string[] foods; public IslandWord word; public IslandSound sound; }
    [Serializable] public sealed class IslandProduct { public string itemId, category, note; public int price; public IslandWord word; }

    [Serializable]
    public sealed class IslandVerbs { public IslandWord plant, harvest, feed, pet, buy, sell, fish; }

    [Serializable]
    public sealed class IslandPlaces { public IslandWord island, station, farm, shop, barn, view, pier; }

    [Serializable]
    public sealed class IslandCatalog
    {
        public string schema;
        public int ticketPrice;
        public IslandKitItem[] starterKit;
        public IslandCrop[] crops;
        public IslandTool[] tools;
        public IslandFish[] fish;
        public IslandVerbs verbs;
        public IslandAnimalDef[] animals;
        public IslandProduct[] products;
        public IslandPlaces places;

        private static IslandCatalog _cached;

        /// <summary>Prices, growth times, rewards and vocabulary all come from data, never from scripts.</summary>
        public static IslandCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>("Island/island_catalog");
            _cached = text != null ? JsonUtility.FromJson<IslandCatalog>(text.text) : new IslandCatalog();
            return _cached;
        }

        public IslandCrop Crop(string id) => crops?.FirstOrDefault(c => c.id == id);
        public IslandTool Tool(string id) => tools?.FirstOrDefault(t => t.id == id);
        public IslandTool ToolFor(string action) => tools?.FirstOrDefault(t => t.action == action);
        public IslandAnimalDef Animal(string id) => animals?.FirstOrDefault(a => a.id == id);
        public IslandFish Fish(string id) => fish?.FirstOrDefault(f => f.id == id);
        public IslandCrop CropBySeed(string seedItemId) => crops?.FirstOrDefault(c => c.SeedItemId == seedItemId);

        /// <summary>Japanese / Vietnamese names the inventory shows for an island item id.</summary>
        public bool TryDescribe(string itemId, out string ja, out string vi, out int price)
        {
            ja = vi = null; price = 0;
            foreach (var c in crops ?? Array.Empty<IslandCrop>())
            {
                if (c.SeedItemId == itemId) { ja = c.word.ja + "の たね"; vi = "Hạt giống " + c.word.vi; price = c.seedPrice; return true; }
                if (c.ProduceItemId == itemId) { ja = c.word.ja; vi = Capitalise(c.word.vi); price = c.sellPrice; return true; }
            }
            var caught = Fish(itemId);
            if (caught != null) { ja = caught.word.ja; vi = Capitalise(caught.word.vi); price = caught.sellPrice; return true; }
            var tool = Tool(itemId);
            if (tool != null) { ja = tool.word.ja; vi = Capitalise(tool.word.vi); price = tool.price; return true; }
            var product = products?.FirstOrDefault(p => p.itemId == itemId);
            if (product != null) { ja = product.word.ja; vi = Capitalise(product.word.vi); price = product.price; return true; }
            return false;
        }

        private static string Capitalise(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    // ─────────── Target learning language ───────────

    public enum TargetLanguage { Japanese, English }

    /// <summary>
    /// The language the player is learning on the island (Japanese by default, English as the second track).
    /// Vietnamese stays the helper language and is shown on demand. Saved in the player's progress.
    /// </summary>
    public static class IslandLanguage
    {
        public static event Action<TargetLanguage> Changed;

        public static TargetLanguage Target
        {
            get
            {
                var record = IslandState.Record;
                return record != null && record.targetLanguage == "en" ? TargetLanguage.English : TargetLanguage.Japanese;
            }
            set
            {
                var record = IslandState.Record;
                if (record == null) return;
                string code = value == TargetLanguage.English ? "en" : "ja";
                if (record.targetLanguage == code) return;
                record.targetLanguage = code;
                IslandState.Save();
                Changed?.Invoke(value);
            }
        }

        public static string Primary(IslandWord w) => w == null ? "" : Target == TargetLanguage.English ? w.en : w.ja;
        /// <summary>Reading aid for the primary line (romaji for Japanese; none for English).</summary>
        public static string Reading(IslandWord w) => w == null || Target == TargetLanguage.English ? "" : w.reading;
        public static string Helper(IslandWord w) => w?.vi ?? "";
        public static string Sound(IslandSound s) => s == null ? "" : Target == TargetLanguage.English ? s.en : s.ja;
        public static string LanguageLabel => Target == TargetLanguage.English ? "English" : "日本語";
    }

    // ─────────── Saved island progress (inside PlayerProgressDto → local save + cloud progress_json) ───────────

    [Serializable]
    public sealed class FarmPlotRecord
    {
        public string plotId;
        public bool tilled;
        public string cropId;
        public int stage;              // 0 = seed in the ground … 3 = ready to harvest
        public bool watered;           // watered for the current stage
        public long stageStartTicks;   // UTC ticks when the current (watered) stage began growing
    }

    [Serializable]
    public sealed class IslandRecord
    {
        public string targetLanguage = "ja";
        public bool starterKitGiven;
        public bool welcomed;
        public List<FarmPlotRecord> plots = new();
        public List<string> words = new();       // vocabulary discovered (catalog ids, e.g. "crop:carrot")
        public List<string> animalsMet = new();
        public List<string> achievements = new();
        public int harvested;
        public int sold;
        public int earned;
        public int fed;
        public int fished;
        public List<ToolWear> toolWear = new();   // uses left on the tool currently in use, per tool item
    }

    [Serializable]
    public sealed class ToolWear { public string itemId; public int usesLeft; }

    /// <summary>Access to the island record inside the shared progress save, plus the farm clock.</summary>
    public static class IslandState
    {
        private static IslandRecord _fallback;
        /// <summary>Test hook: fast-forwards the farm clock without touching real time.</summary>
        public static TimeSpan ClockOffset = TimeSpan.Zero;
        public static DateTime UtcNow => DateTime.UtcNow + ClockOffset;

        public static event Action Changed;

        private static PlayerProgressDto Progress =>
            GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        public static IslandRecord Record
        {
            get
            {
                var progress = Progress;
                if (progress == null) return _fallback ??= new IslandRecord();
                if (progress.island == null) progress.island = new IslandRecord();
                return progress.island;
            }
        }

        public static void Save()
        {
            if (GameServices.TryGet(out IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                if (progress.island == null) progress.island = Record;
                repository.SaveProgress(progress);
            }
            Changed?.Invoke();
        }

        public static FarmPlotRecord Plot(string plotId)
        {
            var record = Record;
            var plot = record.plots.Find(p => p.plotId == plotId);
            if (plot == null) { plot = new FarmPlotRecord { plotId = plotId }; record.plots.Add(plot); }
            return plot;
        }

        /// <summary>Records a newly met word; returns true the first time (for the discovery toast).</summary>
        public static bool Discover(string key)
        {
            var record = Record;
            if (record.words.Contains(key)) return false;
            record.words.Add(key);
            Save();
            NihongoLife.Progression.QuestService.Raise("learn", key);
            return true;
        }

        public static bool Unlock(string achievement)
        {
            var record = Record;
            if (record.achievements.Contains(achievement)) return false;
            record.achievements.Add(achievement);
            Save();
            return true;
        }
    }
}
