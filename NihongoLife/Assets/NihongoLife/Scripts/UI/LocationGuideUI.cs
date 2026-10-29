using System;
using System.Collections.Generic;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// The place guide (opened from the map) is the 場所 chapter of the handbook (<see cref="HandbookUI"/>). Each place
    /// has a photo taken in the game (Resources/Guide/{TYPE}.jpg, shot by GuidePhotosPlayModeTests), what it means in
    /// Japanese daily life, how to use it in the game, a few phrases and whether it is unlocked yet. Text lives in
    /// Resources/Guide/locations.json.
    /// </summary>
    public static class LocationGuideUI
    {
        [Serializable] public sealed class Place
        {
            public string type, ja, vi, en, about, aboutEn, aboutJa; public string[] how, howEn, howJa; public string[][] phrases; public string[] phrasesEn;

            /// <summary>Name, description, how-to lines and phrase meanings in the interface language.</summary>
            public string Name => Loc.Pick(vi, en, ja);
            public string About => Loc.Pick(about, aboutEn, aboutJa);
            public string[] How => Loc.IsJapanese && howJa?.Length > 0 ? howJa : Loc.EnglishData && howEn?.Length > 0 ? Array.ConvertAll(howEn, Loc.Data) : how;
            public string PhraseMeaning(int i) => Loc.IsJapanese ? "" : Loc.EnglishData && phrasesEn != null && i < phrasesEn.Length ? Loc.Data(phrasesEn[i]) : phrases[i][1];
        }

        private static List<Place> _places;

        public static bool IsOpen => HandbookUI.IsOpen && HandbookUI.CurrentChapter == HandbookUI.Chapter.Places;
        public static string ShownType => IsOpen ? HandbookUI.CurrentEntry : null;

        public static void Open(string type = null) => HandbookUI.Open(HandbookUI.Chapter.Places, type ?? "KONBINI");

        public static void Close()
        {
            if (IsOpen) HandbookUI.Close();
        }

        /// <summary>Every place of locations.json, in its order.</summary>
        public static IReadOnlyList<Place> Places
        {
            get
            {
                if (_places != null) return _places;
                _places = new List<Place>();
                var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Guide/locations");
                if (asset == null) return _places;
                // JsonUtility cannot read string[][]: read with MiniJson.
                var root = MiniJson.ParseObject(asset.text);
                foreach (var o in root.Arr("places") ?? new List<object>())
                {
                    if (!(o is Dictionary<string, object> d)) continue;
                    var place = new Place { type = d.Str("type"), ja = d.Str("ja"), vi = d.Str("vi"), en = d.Str("en"), about = d.Str("about"), aboutEn = d.Str("aboutEn"), aboutJa = d.Str("aboutJa") };
                    place.how = Strings(d, "how");
                    place.howEn = Strings(d, "howEn");
                    place.howJa = Strings(d, "howJa");
                    place.phrasesEn = Strings(d, "phrasesEn");
                    var phrases = new List<string[]>();
                    foreach (var ph in d.Arr("phrases") ?? new List<object>())
                        if (ph is List<object> pair && pair.Count >= 2) phrases.Add(new[] { pair[0] as string, pair[1] as string });
                    place.phrases = phrases.ToArray();
                    _places.Add(place);
                }
                return _places;
            }
        }

        private static string[] Strings(Dictionary<string, object> d, string key)
        {
            var list = new List<string>();
            foreach (var item in d.Arr(key) ?? new List<object>()) list.Add(item as string);
            return list.ToArray();
        }
    }
}
