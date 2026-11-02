using System;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>What the player keeps on screen: the status card, the task tracker and the [F] hint. Remembered on the device.</summary>
    public static class HudPrefs
    {
        private const string StatusKey = "NihongoLife.Hud.StatusHidden", QuestKey = "NihongoLife.Hud.QuestHidden", PromptKey = "NihongoLife.Hud.PromptHidden";

        public static event Action Changed;

        public static bool StatusHidden { get => PlayerPrefs.GetInt(StatusKey, 0) == 1; set => Set(StatusKey, value); }
        public static bool QuestHidden { get => PlayerPrefs.GetInt(QuestKey, 0) == 1; set => Set(QuestKey, value); }
        public static bool PromptHidden { get => PlayerPrefs.GetInt(PromptKey, 0) == 1; set => Set(PromptKey, value); }

        private static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Tests start from the full HUD.</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(StatusKey);
            PlayerPrefs.DeleteKey(QuestKey);
            PlayerPrefs.DeleteKey(PromptKey);
            Changed?.Invoke();
        }
    }
}
