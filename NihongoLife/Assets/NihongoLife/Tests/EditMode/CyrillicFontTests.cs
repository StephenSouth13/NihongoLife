using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>The project font (Noto Sans JP, dynamic atlas) can draw all of Russian, Vietnamese and Japanese.</summary>
    public sealed class CyrillicFontTests
    {
        private const string FontPath = "Assets/NihongoLife/Fonts/NotoSansJP SDF.asset";
        private const string Source = "Assets/NihongoLife/Fonts/NotoSansJP.otf";

        [Test]
        public void SourceFont_HasEveryRussianLetter_AndVietnameseAndKana()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Source);
            Assert.NotNull(font);
            string missing = "";
            foreach (char c in "\u0410\u0411\u0412\u0413\u0414\u0415\u0401\u0416\u0417\u0418\u0419\u041A\u041B\u041C\u041D\u041E\u041F\u0420\u0421\u0422\u0423\u0424\u0425\u0426\u0427\u0428\u0429\u042A\u042B\u042C\u042D\u042E\u042F" +
                             "\u0430\u0431\u0432\u0433\u0434\u0435\u0451\u0436\u0437\u0438\u0439\u043A\u043B\u043C\u043D\u043E\u043F\u0440\u0441\u0442\u0443\u0444\u0445\u0446\u0447\u0448\u0449\u044A\u044B\u044C\u044D\u044E\u044F" +
                             "\u00AB\u00BB\u2116\u2014" + "\u1EA1\u1EB7\u01A1\u01B0\u0111" + "\u3042\u30A2\u65E5")
                if (!font.HasCharacter(c)) missing += c;
            Assert.AreEqual("", missing, "glyphs missing in the project font: " + missing);
        }

        [Test]
        public void TmpFontAsset_IsDynamic_AndCanAddCyrillic()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Assert.NotNull(asset);
            Assert.AreEqual(AtlasPopulationMode.Dynamic, asset.atlasPopulationMode, "dynamic: Cyrillic is added when first shown");
            Assert.IsTrue(asset.HasCharacter('\u042F', true, true), "Cyrillic Ya");
            Assert.IsTrue(asset.HasCharacter('\u0451', true, true), "Cyrillic yo");
        }
    }
}
