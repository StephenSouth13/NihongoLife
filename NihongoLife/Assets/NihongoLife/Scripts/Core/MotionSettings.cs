using System;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>Player preference for menu and transition motion. Stored locally. WebGL respects it the same way.</summary>
    public static class MotionSettings
    {
        public const string PrefKey = "nl.reduceMotion";

        public static event Action Changed;

        public static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt(PrefKey, 0) == 1;
            set
            {
                int next = value ? 1 : 0;
                if (PlayerPrefs.GetInt(PrefKey, 0) == next) return;
                PlayerPrefs.SetInt(PrefKey, next);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static void Toggle() => ReduceMotion = !ReduceMotion;

        public static string LabelVi() => ReduceMotion ? "Giảm chuyển động: Bật" : "Giảm chuyển động: Tắt";
        public static string LabelEn() => ReduceMotion ? "Reduce motion: On" : "Reduce motion: Off";
        public static string LabelJa() => ReduceMotion ? "モーションを減らす: オン" : "モーションを減らす: オフ";
    }
}
