using System;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>How the mouse drives the game.</summary>
    public enum ControlScheme
    {
        /// <summary>Default: cursor locked and hidden, mouse movement turns the camera; hold Ctrl for a cursor.</summary>
        MouseLook = 0,
        /// <summary>Sims-style: cursor always visible, left click walks to a point, right/middle drag turns the camera.</summary>
        ClickToMove = 1,
    }

    /// <summary>Player-facing control options (Settings → Điều khiển), stored per device in PlayerPrefs.</summary>
    public static class ControlSettings
    {
        private const string SensitivityKey = "NihongoLife.Controls.MouseSensitivity";
        private const string InvertKey = "NihongoLife.Controls.InvertY";
        private const string SchemeKey = "NihongoLife.Controls.Scheme";
        public const float MinSensitivity = 0.2f, MaxSensitivity = 3f;

        public static event Action Changed;

        public static float MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), MinSensitivity, MaxSensitivity);
            set { PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity)); Changed?.Invoke(); }
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(InvertKey, 0) == 1;
            set { PlayerPrefs.SetInt(InvertKey, value ? 1 : 0); Changed?.Invoke(); }
        }

        public static ControlScheme Scheme
        {
            get => (ControlScheme)Mathf.Clamp(PlayerPrefs.GetInt(SchemeKey, (int)ControlScheme.MouseLook), 0, 1);
            set { PlayerPrefs.SetInt(SchemeKey, (int)value); Changed?.Invoke(); }
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
