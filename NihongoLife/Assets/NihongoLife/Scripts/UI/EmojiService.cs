using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace NihongoLife.UI
{
    /// <summary>
    /// F5: chat emoji. Twemoji PNGs (CC-BY 4.0, see Docs/CREDITS.md) are packed at runtime into one atlas and a TMP sprite asset.
    /// Chat text keeps plain, network-safe shortcodes (":joy:", ":)"); <see cref="Render"/> turns shortcodes, ASCII emoticons and
    /// typed Unicode emoji into &lt;sprite&gt; tags. The player's own text is wrapped in noparse so nobody can inject rich text.
    /// </summary>
    public static class EmojiService
    {
        public const int Cell = 72, Columns = 8;

        /// <summary>(codepoints as Twemoji file name, shortcode). The order is the order in the picker.</summary>
        public static readonly (string code, string name)[] Catalog =
        {
            ("1f642","smile"),("1f604","grin"),("1f603","smiley"),("1f600","grinning"),("1f601","beam"),("1f602","joy"),("1f605","sweat_smile"),("1f606","laughing"),
            ("1f609","wink"),("1f60a","blush"),("1f60b","yum"),("1f60d","heart_eyes"),("1f970","love"),("1f618","kiss"),("1f60e","cool"),("1f60f","smirk"),
            ("1f914","thinking"),("1f610","neutral"),("1f644","eyeroll"),("1f614","pensive"),("1f60c","relieved"),("1f61c","tongue"),("1f61d","squint"),("1f917","hug"),
            ("1f622","cry"),("1f62d","sob"),("1f628","fearful"),("1f631","scream"),("1f62e","wow"),("1f620","angry"),("1f621","rage"),("1f926","facepalm"),
            ("1f634","sleeping"),("1f4a4","zzz"),("1f607","angel"),("1f973","party"),("1f44d","thumbsup"),("1f44e","thumbsdown"),("1f44b","wave"),("1f44f","clap"),
            ("1f64c","hooray"),("1f64f","pray"),("1f4aa","muscle"),("2764","heart"),("1f525","fire"),("1f389","tada"),("1f4af","hundred"),("2b50","star"),
            ("1f31f","sparkle"),("1f381","gift"),("1f4b0","moneybag"),("1f338","blossom"),("1f340","clover"),("1f35c","ramen"),("1f363","sushi"),("2615","coffee"),
            ("1f37a","beer"),("1f3b5","music"),("1f436","dog"),("1f431","cat"),("1f40e","horse"),("1f1fb-1f1f3","vn"),
        };

        private static readonly (string text, string name)[] Emoticons =
        {
            (":)", "smile"), (":-)", "smile"), (":D", "grin"), (":(", "cry"), (":-(", "cry"), ("<3", "heart"), (";)", "wink"), (":P", "tongue"), (":p", "tongue"), (":O", "wow"),
        };

        private static TMP_SpriteAsset _asset;
        private static Texture2D _atlas;
        private static readonly Dictionary<string, int> IndexByName = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> NameByUnicode = new Dictionary<string, string>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static bool _built;

        public static int Count => Catalog.Length;
        /// <summary>Emoji drawn by the last <see cref="Render"/> call.</summary>
        public static int LastCount { get; private set; }
        public static bool Ready => _built && _asset != null;
        public static TMP_SpriteAsset Asset { get { Ensure(); return _asset; } }
        public static Texture2D Atlas { get { Ensure(); return _atlas; } }
        public static string NameAt(int i) => Catalog[i].name;

        /// <summary>Test hook: forget everything so the next call rebuilds.</summary>
        public static void ResetForTests()
        {
            if (_asset != null) UnityEngine.Object.Destroy(_asset);
            if (_atlas != null) UnityEngine.Object.Destroy(_atlas);
            _asset = null; _atlas = null; _built = false;
            IndexByName.Clear(); NameByUnicode.Clear(); Sprites.Clear();
        }

        public static void Ensure()
        {
            if (_built && _asset != null && _atlas != null) return;
            ResetForTests();
            int rows = (Catalog.Length + Columns - 1) / Columns;
            _atlas = new Texture2D(Columns * Cell, rows * Cell, TextureFormat.RGBA32, false) { name = "NLEmojiAtlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color32[_atlas.width * _atlas.height];
            _atlas.SetPixels32(clear);
            var glyphs = new List<TMP_SpriteGlyph>();
            var chars = new List<TMP_SpriteCharacter>();
            var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            for (int i = 0; i < Catalog.Length; i++)
            {
                var (code, name) = Catalog[i];
                int x = (i % Columns) * Cell, y = (i / Columns) * Cell;
                var bytes = Resources.Load<TextAsset>("Content/Emoji/e_" + code);
                if (bytes != null && ImageConversion.LoadImage(tmp, bytes.bytes, false) && tmp.width == Cell && tmp.height == Cell)
                    _atlas.SetPixels32(x, y, Cell, Cell, tmp.GetPixels32());
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(Cell, Cell, 0f, Cell * 0.82f, Cell),
                    glyphRect = new GlyphRect(x, y, Cell, Cell),
                    scale = 1f,
                    atlasIndex = 0,
                };
                glyphs.Add(glyph);
                uint unicode = UnicodeOf(code, i);
                chars.Add(new TMP_SpriteCharacter(unicode, glyph) { name = name, scale = 1f });
                IndexByName[name] = i;
                string key = UnicodeString(code);
                if (key != null) NameByUnicode[key] = name;
            }
            UnityEngine.Object.Destroy(tmp);
            _atlas.Apply(false, false);

            _asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            _asset.name = "NLEmoji";
            _asset.spriteSheet = _atlas;
            // Tables first, material last: TMP "upgrades" (and clears) an asset that has a material but no version string.
            _asset.spriteGlyphTable.AddRange(glyphs);
            _asset.spriteCharacterTable.AddRange(chars);
            _asset.faceInfo = new FaceInfo { pointSize = Cell, scale = 1f, lineHeight = Cell, ascentLine = Cell * 0.82f, descentLine = -Cell * 0.18f, baseline = 0f, capLine = Cell * 0.7f, meanLine = Cell * 0.5f };
            _asset.UpdateLookupTables();
            _asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(_asset.name);
            var baseAsset = Resources.Load<TMP_SpriteAsset>("Sprite Assets/EmojiOne");
            Material mat = baseAsset != null && baseAsset.material != null ? new Material(baseAsset.material) : new Material(Shader.Find("TextMeshPro/Sprite"));
            mat.mainTexture = _atlas;
            _asset.material = mat;
            _built = true;
        }

        private static uint UnicodeOf(string code, int index)
        {
            if (code.Contains("-")) return 0xE000u + (uint)index;
            return Convert.ToUInt32(code, 16);
        }

        private static string UnicodeString(string code)
        {
            var sb = new StringBuilder();
            foreach (var part in code.Split('-')) sb.Append(char.ConvertFromUtf32(Convert.ToInt32(part, 16)));
            return sb.ToString();
        }

        public static bool Has(string name) { Ensure(); return IndexByName.ContainsKey(name); }

        /// <summary>A Sprite of one emoji for buttons and previews.</summary>
        public static Sprite GetSprite(string name)
        {
            Ensure();
            if (!IndexByName.TryGetValue(name, out int i)) return null;
            if (Sprites.TryGetValue(name, out var s) && s != null) return s;
            s = Sprite.Create(_atlas, new Rect((i % Columns) * Cell, (i / Columns) * Cell, Cell, Cell), new Vector2(0.5f, 0.5f), 100f);
            s.name = "emoji_" + name;
            Sprites[name] = s;
            return s;
        }

        /// <summary>The shortcode text the picker inserts.</summary>
        public static string Shortcode(string name) => ":" + name + ":";

        /// <summary>
        /// Plain chat text to TMP rich text. Safe for player input: everything that is not an emoji is inside noparse.
        /// </summary>
        public static string Render(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            Ensure();
            var sb = new StringBuilder(raw.Length + 32);
            var seg = new StringBuilder();
            int emojiCount = 0;
            void Flush()
            {
                if (seg.Length == 0) return;
                sb.Append("<noparse>").Append(seg.ToString().Replace("</noparse>", string.Empty)).Append("</noparse>");
                seg.Clear();
            }
            void Emit(string name) { Flush(); sb.Append("<sprite name=\"").Append(name).Append("\">"); emojiCount++; }

            int i = 0;
            while (i < raw.Length)
            {
                char c = raw[i];
                if (c == ':')
                {
                    int end = raw.IndexOf(':', i + 1);
                    if (end > i + 1 && end - i - 1 <= 24)
                    {
                        string name = raw.Substring(i + 1, end - i - 1).ToLowerInvariant();
                        if (IndexByName.ContainsKey(name)) { Emit(name); i = end + 1; continue; }
                    }
                }
                if (c == ':' || c == ';' || c == '<')
                {
                    bool hit = false;
                    foreach (var (text, name) in Emoticons)
                    {
                        if (string.CompareOrdinal(raw, i, text, 0, text.Length) != 0) continue;
                        // ":D" / ":P" / ":O" only when they end the word (not ":Dx").
                        int after = i + text.Length;
                        if (char.IsLetter(text[text.Length - 1]) && after < raw.Length && char.IsLetterOrDigit(raw[after])) continue;
                        Emit(name); i = after; hit = true; break;
                    }
                    if (hit) continue;
                }
                if (c >= 0x2190)
                {
                    int matched = 0; string mname = null;
                    foreach (int len in new[] { 4, 2, 1 })
                    {
                        if (i + len > raw.Length) continue;
                        if (NameByUnicode.TryGetValue(raw.Substring(i, len), out mname)) { matched = len; break; }
                    }
                    if (matched > 0)
                    {
                        i += matched;
                        if (i < raw.Length && raw[i] == '\uFE0F') i++;
                        Emit(mname);
                        continue;
                    }
                    if (c == '\uFE0F' || c == '\u200D') { i++; continue; }
                }
                seg.Append(c);
                i++;
            }
            Flush();
            LastCount = emojiCount;
            return sb.ToString();
        }

        /// <summary>How many emoji sprites <see cref="Render"/> would draw.</summary>
        public static int CountIn(string raw)
        {
            Render(raw);
            return LastCount;
        }

        /// <summary>Show chat text with emoji on a TMP label.</summary>
        public static void Apply(TMP_Text label, string raw)
        {
            if (label == null) return;
            Ensure();
            label.spriteAsset = _asset;
            label.richText = true;
            label.text = Render(raw);
        }
    }
}