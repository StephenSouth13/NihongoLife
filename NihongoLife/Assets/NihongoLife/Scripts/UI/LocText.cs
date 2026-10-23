using System;
using NihongoLife.Core;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// A label that follows the interface language: holds its text in vi / en / ja (or a function that builds it) and
    /// rewrites itself when <see cref="Loc.Changed"/> fires, also when it was hidden at the time (on enable).
    /// Code that later writes the label's text directly should call <see cref="Set(TMP_Text,Func{string})"/> instead,
    /// or <see cref="Clear"/> first, so a language switch does not bring an old text back.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocText : MonoBehaviour
    {
        private TMP_Text _text;
        private string _vi, _en, _ja;
        private Func<string> _build;
        private GameLanguage _shown = (GameLanguage)(-1);
        private bool _subscribed;

        public static TMP_Text Set(TMP_Text text, string vi, string en, string ja)
        {
            if (text == null) return null;
            var loc = text.GetComponent<LocText>().OrNull() ?? text.gameObject.AddComponent<LocText>();
            loc._text = text;
            loc._vi = vi; loc._en = en; loc._ja = ja; loc._build = null;
            loc.Subscribe();
            loc.Apply();
            return text;
        }

        public static TMP_Text Set(TMP_Text text, Func<string> build)
        {
            if (text == null) return null;
            var loc = text.GetComponent<LocText>().OrNull() ?? text.gameObject.AddComponent<LocText>();
            loc._text = text;
            loc._build = build; loc._vi = loc._en = loc._ja = null;
            loc.Subscribe();
            loc.Apply();
            return text;
        }

        /// <summary>Stops following the language (the caller owns the text from now on).</summary>
        public static void Clear(TMP_Text text)
        {
            var loc = text != null ? text.GetComponent<LocText>() : null;
            if (loc == null) return;
            loc._build = null; loc._vi = loc._en = loc._ja = null;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            Loc.Changed += Apply;
            _subscribed = true;
        }

        private void OnEnable() { if (_shown != Loc.Language) Apply(); }
        private void OnDestroy() { Loc.Changed -= Apply; _subscribed = false; }

        private void Apply()
        {
            if (this == null) { Loc.Changed -= Apply; return; }
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text == null) return;
            if (_build != null)
            {
                try { _text.text = _build(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
            else if (_vi != null || _en != null || _ja != null) _text.text = Loc.T(_vi, _en, _ja);
            else return;
            _shown = Loc.Language;
        }
    }
}
