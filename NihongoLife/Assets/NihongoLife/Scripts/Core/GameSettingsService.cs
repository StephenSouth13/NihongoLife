using System;
using UnityEngine;

namespace NihongoLife.Core
{
    public class GameSettingsService : MonoBehaviour, IGameService
    {
        private const string LanguageKey = "NihongoLife.UiLanguage";

        public event Action<GameLanguage> OnLanguageChanged;

        public GameLanguage Language { get; private set; } = GameLanguage.Vietnamese;

        public void Initialize()
        {
            Language = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.Vietnamese), 0, 2);
        }

        public void SetLanguage(GameLanguage language)
        {
            if (Language == language) return;

            Language = language;
            PlayerPrefs.SetInt(LanguageKey, (int)language);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke(Language);
        }

        public string Text(string vi, string en, string ja)
        {
            return Language switch
            {
                GameLanguage.English => en,
                GameLanguage.Japanese => ja,
                _ => vi
            };
        }
    }
}
