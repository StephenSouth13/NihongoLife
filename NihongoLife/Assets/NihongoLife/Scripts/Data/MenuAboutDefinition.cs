using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Data
{
    [Serializable]
    public class MenuAboutLink
    {
        public string label;
        public string url;
    }

    /// <summary>Optional localized caption for one screenshot, matched by texture file name (without extension).</summary>
    [Serializable]
    public class MenuGalleryCaption
    {
        public string textureName;
        public string captionVi;
        public string captionEn;
        public string captionJa;
    }

    /// <summary>
    /// Content of the main-menu About popup: creator profile, links, credits and screenshot captions.
    /// Screenshots themselves are not listed here: every PNG/JPG placed in
    /// Assets/NihongoLife/Resources/&lt;galleryResourceFolder&gt; appears in the gallery automatically
    /// (sorted by file name). Add a caption entry only to give a screenshot a localized caption.
    /// </summary>
    public class MenuAboutDefinition : ScriptableObject
    {
        [Header("Creator")]
        public string displayName = "quachthanhlong.com";
        public string avatarText = "QL";
        public string websiteUrl = "https://quachthanhlong.com";
        public string websiteButtonVi = "Mở website";
        public string websiteButtonEn = "Visit website";
        public string websiteButtonJa = "サイトを開く";
        public string roleVi = "Người tạo ra Nihongo Life";
        public string roleEn = "Creator of Nihongo Life";
        public string roleJa = "Nihongo Life の制作者";
        [TextArea(3, 8)] public string bioVi;
        [TextArea(3, 8)] public string bioEn;
        [TextArea(3, 8)] public string bioJa;
        public List<MenuAboutLink> links = new List<MenuAboutLink>();

        [Header("Credits")]
        public List<string> credits = new List<string>();

        [Header("Screenshot gallery")]
        public string galleryResourceFolder = "MenuGallery";
        [Min(0f)] public float autoAdvanceSeconds = 5f;
        public List<MenuGalleryCaption> captions = new List<MenuGalleryCaption>();

        public MenuGalleryCaption FindCaption(string textureName)
        {
            if (string.IsNullOrEmpty(textureName) || captions == null) return null;
            return captions.Find(c => c != null && string.Equals(c.textureName, textureName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
