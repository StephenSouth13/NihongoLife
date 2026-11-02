using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Fashion
{
    [Serializable]
    public sealed class FashionPart
    {
        public string id;
        public LookWearPiece[] pieces = Array.Empty<LookWearPiece>();
    }

    [Serializable]
    public sealed class FashionPartUse
    {
        public string part, color;
    }

    [Serializable]
    public sealed class FashionCategory
    {
        public string id, vi, en, ja, ru;
        public string Name => Loc.IsRussian && !string.IsNullOrEmpty(ru) ? ru : Loc.Pick(vi, en, ja);
    }

    /// <summary>A complete set: the top and bottom colours plus its accessories (Resources/Fashion/outfits.json).</summary>
    [Serializable]
    public sealed class FashionOutfit
    {
        public string id, category, vi, en, ja, ru, top, bottom;
        public int price;
        public FashionPartUse[] wear = Array.Empty<FashionPartUse>();
        public string Name => Loc.IsRussian && !string.IsNullOrEmpty(ru) ? ru : Loc.Pick(vi, en, ja);
    }

    /// <summary>A customer's line at Hibari Mode and how the staff should answer (Resources/Fashion/fashion.json).</summary>
    [Serializable]
    public sealed class FashionRequest
    {
        public string lineJa, reading, vi, en, answerJa, answerVi, answerEn;
        public string[] wrongJa = Array.Empty<string>(), wrongVi = Array.Empty<string>(), wrongEn = Array.Empty<string>();
    }

    [Serializable]
    public sealed class FashionFloor
    {
        public string schema;
        public FashionRequest[] requests = Array.Empty<FashionRequest>();
        public FashionRequest[] fittings = Array.Empty<FashionRequest>();
    }

    [Serializable]
    public sealed class FashionCatalog
    {
        public string schema;
        public FashionPart[] parts = Array.Empty<FashionPart>();
        public FashionCategory[] categories = Array.Empty<FashionCategory>();
        public FashionOutfit[] outfits = Array.Empty<FashionOutfit>();

        private static FashionCatalog _catalog;
        private static FashionFloor _floor;
        private readonly Dictionary<string, LookOutfit> _looks = new Dictionary<string, LookOutfit>();

        public static FashionCatalog Load() => _catalog ??= Read<FashionCatalog>("Fashion/outfits") ?? new FashionCatalog();
        public static FashionFloor Floor => _floor ??= Read<FashionFloor>("Fashion/fashion") ?? new FashionFloor();

        private static T Read<T>(string path) where T : class
        {
            var text = Resources.Load<TextAsset>(path);
            return text != null ? JsonUtility.FromJson<T>(text.text) : null;
        }

        public FashionOutfit Outfit(string id) => outfits.FirstOrDefault(o => o.id == id);
        public IEnumerable<FashionOutfit> InCategory(string id) => outfits.Where(o => o.category == id);

        /// <summary>What <see cref="CharacterLook"/> paints and builds for an outfit: its colours and its pieces in their colours.</summary>
        public LookOutfit Look(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_looks.TryGetValue(id, out var cached)) return cached;
            var outfit = Outfit(id);
            if (outfit == null) return null;
            var pieces = new List<LookWearPiece>();
            foreach (var use in outfit.wear)
            {
                var part = parts.FirstOrDefault(p => p.id == use.part);
                if (part == null) continue;
                pieces.AddRange(part.pieces.Select(p => new LookWearPiece { bone = p.bone, shape = p.shape, pos = p.pos, scale = p.scale, rot = p.rot, tone = use.color }));
            }
            var look = new LookOutfit { id = id, tint = new LookTint(null, outfit.top, outfit.bottom), pieces = pieces.ToArray() };
            _looks[id] = look;
            return look;
        }
    }
}
