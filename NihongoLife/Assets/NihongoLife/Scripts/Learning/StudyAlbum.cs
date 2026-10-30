using System;
using System.Collections.Generic;

namespace NihongoLife.Learning
{
    /// <summary>Photo/stamp album scaffold: places visited and moments captured, each with a vocabulary caption.
    /// Serializable for the progress save; no duplicates; newest first.</summary>
    [Serializable]
    public sealed class StudyAlbum
    {
        [Serializable]
        public sealed class Page { public string id; public string place; public string captionJa; public string captionLocal; public int day; public string image; }

        public List<Page> pages = new List<Page>();
        public int Count => pages.Count;

        public bool Has(string id) => pages.Exists(p => p.id == id);

        public bool Add(string id, string place, string captionJa, string captionLocal, int day)
        {
            if (string.IsNullOrEmpty(id) || Has(id)) return false;
            pages.Insert(0, new Page { id = id, place = place, captionJa = captionJa, captionLocal = captionLocal, day = day });
            return true;
        }

        public List<Page> ByPlace(string place) => pages.FindAll(p => p.place == place);
    }
}
