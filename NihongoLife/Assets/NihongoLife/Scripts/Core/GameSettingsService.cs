using System;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>Settings service; the interface language itself lives in <see cref="Loc"/> (one source for every screen).</summary>
    public class GameSettingsService : MonoBehaviour, IGameService
    {
        public event Action<GameLanguage> OnLanguageChanged;

        public GameLanguage Language => Loc.Language;

        public void Initialize()
        {
            Loc.Changed -= RaiseChanged;
            Loc.Changed += RaiseChanged;
        }

        private void OnDestroy() => Loc.Changed -= RaiseChanged;

        private void RaiseChanged() => OnLanguageChanged?.Invoke(Loc.Language);

        public void SetLanguage(GameLanguage language) => Loc.Set(language);

        public string Text(string vi, string en, string ja) => Loc.T(vi, en, ja);
    }
}
