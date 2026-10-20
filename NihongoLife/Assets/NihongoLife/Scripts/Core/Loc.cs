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
                if (_language == null) _language = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, (int)GameLanguage.Vietnamese), 0, 2);
                return _language.Value;
            }
        }

        public static bool IsVietnamese => Language == GameLanguage.Vietnamese;
        public static bool IsEnglish => Language == GameLanguage.English;
        public static bool IsJapanese => Language == GameLanguage.Japanese;
        /// <summary>"vi", "en" or "ja".</summary>
        public static string Code => Language switch { GameLanguage.English => "en", GameLanguage.Japanese => "ja", _ => "vi" };

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
        public static string T(string vi, string en, string ja) => Language switch
        {
            GameLanguage.English => en,
            GameLanguage.Japanese => ja,
            _ => vi
        };

        /// <summary><see cref="T"/> then string.Format with the arguments.</summary>
        public static string F(string vi, string en, string ja, params object[] args) => string.Format(T(vi, en, ja), args);

        /// <summary>A gloss (meaning) of learned Japanese in the interface language; empty when the interface is Japanese.</summary>
        public static string Gloss(string vi, string en) => Language switch
        {
            GameLanguage.English => en ?? "",
            GameLanguage.Japanese => "",
            _ => vi ?? ""
        };

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
            var watcher = owner.GetComponent<LocWatcher>() ?? owner.gameObject.AddComponent<LocWatcher>();
            watcher.Add(redraw);
        }

        private const char TripleMark = '';

        /// <summary>
        /// Text stored now and shown later (wallet history, logs): keeps all three languages so it is shown in whatever
        /// language is chosen when it is read. Read it back with <see cref="Resolve"/>.
        /// </summary>
        public static string Triple(string vi, string en, string ja) => $"{TripleMark}{vi}{TripleMark}{en}{TripleMark}{ja}";

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
            return parts.Length >= 4 ? Pick(parts[1], parts[2], parts[3]) : stored.Trim(TripleMark);
        }

        /// <summary>Thousands-separated yen, e.g. ¥12,400.</summary>
        public static string Yen(int amount) => (amount < 0 ? "−¥" : "¥") + Mathf.Abs(amount).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

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
