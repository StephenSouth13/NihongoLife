using System;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>On-screen control options. Mode 0 follows the device, 1 forces them on, 2 forces them off.</summary>
    public static class TouchControlPrefs
    {
        const string ModeKey = "nl.touch.mode";
        const string SizeKey = "nl.touch.size";
        const string OpacityKey = "nl.touch.opacity";
        const string HandKey = "nl.touch.left";

        public static event Action Changed;

        public static int Mode
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, 2);
            set { PlayerPrefs.SetInt(ModeKey, Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static float Size
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(SizeKey, 1f), 0.7f, 1.45f);
            set { PlayerPrefs.SetFloat(SizeKey, Mathf.Clamp(value, 0.7f, 1.45f)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static float Opacity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(OpacityKey, 0.88f), 0.35f, 1f);
            set { PlayerPrefs.SetFloat(OpacityKey, Mathf.Clamp(value, 0.35f, 1f)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static bool LeftHanded
        {
            get => PlayerPrefs.GetInt(HandKey, 0) == 1;
            set { PlayerPrefs.SetInt(HandKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static void CycleMode() => Mode = (Mode + 1) % 3;

        public static bool WantsVisible
        {
            get
            {
                if (Mode == 2) return false;
                if (Mode == 1) return true;
                if (UnityEngine.Device.Application.isMobilePlatform) return true;
                if (UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld) return true;
                if (WebGLPointer.Coarse) return true;
#if UNITY_EDITOR
                if (Screen.width > 0 && Screen.height > Screen.width && Screen.width <= 640) return true;
#endif
                return false;
            }
        }

        /// <summary>What the player chooses is how they play: with a phone's touch buttons, or with keyboard and mouse.
        /// Automatic says which one it found.</summary>
        public static string ModeVi() => Mode switch { 1 => "Chế độ chơi: Điện thoại", 2 => "Chế độ chơi: Máy tính", _ => WantsVisible ? "Chế độ chơi: Điện thoại (tự nhận)" : "Chế độ chơi: Máy tính (tự nhận)" };
        public static string ModeEn() => Mode switch { 1 => "Play on: Phone", 2 => "Play on: Computer", _ => WantsVisible ? "Play on: Phone (detected)" : "Play on: Computer (detected)" };
        public static string ModeJa() => Mode switch { 1 => "プレイ: スマホ", 2 => "プレイ: パソコン", _ => WantsVisible ? "プレイ: スマホ（自動）" : "プレイ: パソコン（自動）" };

        /// <summary>The picture beside the mode: a phone or a keyboard.</summary>
        public static string ModeIcon => WantsVisible ? "smartphone" : "keyboard";
        public static string HandVi() => LeftHanded ? "Tay thuận: Trái" : "Tay thuận: Phải";
        public static string HandEn() => LeftHanded ? "Handed: Left" : "Handed: Right";
        public static string HandJa() => LeftHanded ? "利き手: 左" : "利き手: 右";
    }
}
