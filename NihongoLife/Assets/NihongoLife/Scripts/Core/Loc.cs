using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// The one interface language for the whole game (Vietnamese, English or Japanese). Every label, button, hint,
    /// toast and gloss goes through <see cref="T"/> so a screen is never half one language and half another.
    /// Japanese being learned (dialogue lines, vocabulary) is content, not interface: it stays Japanese and is glossed
    /// in this language only (no gloss when the interface is Japanese).
    /// <see cref="Set"/> switches at any time; <see cref="Changed"/> tells every open screen to redraw.
    /// </summary>
    public static class Loc
    {
        public const string PrefsKey = "NihongoLife.UiLanguage";
        private static GameLanguage? _language;

        /// <summary>Raised after the language changed (screens redraw their text).</summary>
        public static event Action Changed;

        public static GameLanguage Language
        {
            get
            {
                if (_language == null) _language = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, (int)GameLanguage.Vietnamese), 0, 3);
                return _language.Value;
            }
        }

        public static bool IsVietnamese => Language == GameLanguage.Vietnamese;
        public static bool IsEnglish => Language == GameLanguage.English;
        public static bool IsJapanese => Language == GameLanguage.Japanese;
        /// <summary>"vi", "en" or "ja".</summary>
        public static string Code => Language switch { GameLanguage.English => "en", GameLanguage.Japanese => "ja", GameLanguage.Russian => "ru", _ => "vi" };
        public static bool IsRussian => Language == GameLanguage.Russian;
        /// <summary>True for English and Russian: data that exists only as vi / en / ja is shown in English for Russian (see <see cref="Data"/>).</summary>
        public static bool EnglishData => Language == GameLanguage.English || Language == GameLanguage.Russian;
        /// <summary>English data text for the interface: the Russian line when the interface is Russian and <see cref="RuTable"/> has one, else the English text.</summary>
        public static string Data(string en) => Language == GameLanguage.Russian && !string.IsNullOrEmpty(en) && RuTable.TryGet(en, out var ru) ? ru : en;

        public static void Set(GameLanguage language)
        {
            if (_language == language) return;
            _language = language;
            PlayerPrefs.SetInt(PrefsKey, (int)language);
            PlayerPrefs.Save();
            var handlers = Changed;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception e) { Debug.LogException(e); } // one broken screen must not stop the others redrawing
            }
        }

        /// <summary>The text in the current interface language.</summary>
        public static string T(string vi, string en, string ja) => Currency(Language switch
        {
            GameLanguage.English => en,
            GameLanguage.Japanese => ja,
            GameLanguage.Russian => RuTable.Translate(en, vi),
            _ => vi
        });

        /// <summary>
        /// G3: a yen amount written into text ("¥12,400", "−¥160") is shown in the language's currency (en $, ru rub., vi dong); Japanese keeps yen.
        /// Used by <see cref="T"/>, <see cref="F"/> and <see cref="Gloss"/>, so no screen mixes a yen sign into another language.
        /// </summary>
        public static string Currency(string text)
        {
            if (string.IsNullOrEmpty(text) || Language == GameLanguage.Japanese || text.IndexOf('¥') < 0) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, "¥\\s?(\\d[\\d,\\.]*\\d|\\d)", m =>
            {
                string num = m.Groups[1].Value.Replace(",", "");
                return double.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var yen) ? Money(yen) : m.Value;
            });
        }

        /// <summary><see cref="T"/> then string.Format with the arguments.</summary>
        public static string F(string vi, string en, string ja, params object[] args) => Currency(string.Format(T(vi, en, ja), args));

        /// <summary>A gloss (meaning) of learned Japanese in the interface language; empty when the interface is Japanese.</summary>
        public static string Gloss(string vi, string en) => Currency(Language switch
        {
            GameLanguage.English => en ?? "",
            GameLanguage.Russian => en ?? "",
            GameLanguage.Japanese => "",
            _ => vi ?? ""
        });

        /// <summary>Picks the field for the current language from a vi/en/ja triple; falls back vi → en → ja when one is empty.</summary>
        public static string Pick(string vi, string en, string ja)
        {
            string value = T(vi, en, ja);
            if (!string.IsNullOrEmpty(value)) return value;
            return !string.IsNullOrEmpty(vi) && !IsJapanese ? vi : !string.IsNullOrEmpty(en) ? en : ja ?? vi ?? "";
        }

        /// <summary>Calls <paramref name="redraw"/> whenever the language changes, for as long as <paramref name="owner"/> lives.</summary>
        public static void Watch(Component owner, Action redraw)
        {
            if (owner == null || redraw == null) return;
            var watcher = owner.GetComponent<LocWatcher>().OrNull() ?? owner.gameObject.AddComponent<LocWatcher>();
            watcher.Add(redraw);
        }

        private const char TripleMark = '';

        /// <summary>
        /// Text stored now and shown later (wallet history, logs): keeps all three languages so it is shown in whatever
        /// language is chosen when it is read. Read it back with <see cref="Resolve"/>.
        /// </summary>
        public static string Triple(string vi, string en, string ja) => $"{TripleMark}{vi}{TripleMark}{en}{TripleMark}{ja}";

        /// <summary>
        /// <see cref="Triple"/> for templates with {0}-style holes: formats the three languages and also stores the Russian text
        /// (the English template looked up in <see cref="RuTable"/>, then formatted), so stored messages can be shown in Russian too.
        /// </summary>
        public static string TripleF(string vi, string en, string ja, params object[] args)
        {
            string ruTemplate = RuTable.TryGet(en, out var found) ? found : en;
            return $"{TripleMark}{string.Format(vi, args)}{TripleMark}{string.Format(en, args)}{TripleMark}{string.Format(ja, args)}{TripleMark}{string.Format(ruTemplate, args)}";
        }

        /// <summary>True when the text was stored with <see cref="Triple"/>.</summary>
        /// <summary>True when the text contains kana or kanji.</summary>
        public static bool IsJapaneseText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text) if ((c >= '぀' && c <= 'ヿ') || (c >= '一' && c <= '鿿')) return true;
            return false;
        }

        public static bool IsTriple(string stored) =>!string.IsNullOrEmpty(stored) && stored[0] == TripleMark;

        /// <summary>Text from <see cref="Triple"/> in the current language (other text is returned as is).</summary>
        public static string Resolve(string stored)
        {
            if (string.IsNullOrEmpty(stored) || stored[0] != TripleMark) return stored ?? "";
            var parts = stored.Split(TripleMark);
            if (parts.Length >= 5 && Language == GameLanguage.Russian && parts[4].Length > 0) return parts[4];
            return parts.Length >= 4 ? Pick(parts[1], parts[2], parts[3]) : stored.Trim(TripleMark);
        }

        /// <summary>
        /// G3: money in the language's currency. Prices are stored in yen; the display converts: ja yen, en dollar (100 yen = 1 USD),
        /// ru ruble (1 yen = 1 RUB), vi dong (1 yen = 170 VND); exact multiples, so sums add up. Banknote-style grouping per language.
        /// </summary>
        public static string Money(double yen)
        {
            bool neg = yen < 0; double a = System.Math.Abs(yen);
            string body;
            switch (Language)
            {
                case GameLanguage.English: body = "$" + (a / 100.0).ToString("N2", System.Globalization.CultureInfo.InvariantCulture); break;
                case GameLanguage.Russian: body = System.Math.Round(a).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '\u00A0') + "\u00A0руб."; break;
                case GameLanguage.Vietnamese: body = System.Math.Round(a * 170.0).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + "đ"; break;
                default: body = "¥" + System.Math.Round(a).ToString("N0", System.Globalization.CultureInfo.InvariantCulture); break;
            }
            return neg ? "-" + body : body;
        }

        /// <summary>Money in the language's currency (see <see cref="Money"/>), e.g. ¥12,400 / 2.108.000đ / $124.00.</summary>
        public static string Yen(int amount) => Money(amount);

        /// <summary>Tests and tools: forget the cached value (re-read from PlayerPrefs next time).</summary>
        public static void ResetCache() => _language = null;
    }

    /// <summary>Holds the redraw callbacks of one GameObject (see <see cref="Loc.Watch"/>).</summary>
    [DisallowMultipleComponent]
    public sealed class LocWatcher : MonoBehaviour
    {
        private readonly List<Action> _redraws = new List<Action>();
        private bool _subscribed;

        // Subscribed on Add, not Awake: a screen built while hidden (inactive) must still redraw.
        public void Add(Action redraw)
        {
            _redraws.Add(redraw);
            if (!_subscribed) { Loc.Changed += Redraw; _subscribed = true; }
        }

        private void OnDestroy() { Loc.Changed -= Redraw; _subscribed = false; }

        private void Redraw()
        {
            if (this == null) { Loc.Changed -= Redraw; return; } // destroyed without OnDestroy (never activated)
            for (int i = 0; i < _redraws.Count; i++)
            {
                try { _redraws[i](); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }
    }
}
