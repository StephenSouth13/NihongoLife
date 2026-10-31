using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Screen size and safe area as the UI sees them. Normally the real screen; layout tests and the menu screenshot
    /// tool set <see cref="Override"/> to lay the canvas out for a phone, 21:9 or 4:3 without changing the window.
    /// </summary>
    public static class UiScreen
    {
        public static Vector2Int? Override;
        public static Rect? SafeAreaOverride;
        /// <summary>Canvas scale factor to use while <see cref="Override"/> is set (world-space layout tests).</summary>
        public static float? CanvasScaleOverride;
        public static int Width => Override?.x ?? Screen.width;
        public static int Height => Override?.y ?? Screen.height;
        public static Rect SafeArea => Override.HasValue ? SafeAreaOverride ?? new Rect(0f, 0f, Override.Value.x, Override.Value.y) : Screen.safeArea;
        /// <summary>Forces the phone profile on or off (tests, screenshots). Null = decide from the device.</summary>
        public static bool? PhoneOverride;

        /// <summary>
        /// Mobile UI profile: a small screen, a phone or tablet (also mobile WebGL), or touch controls switched "On" in
        /// Settings / the main menu on a screen up to 1300 px tall. Phone canvases use a small reference size, so text
        /// and buttons come out finger-sized.
        /// </summary>
        public static bool IsPhone
        {
            get
            {
                if (PhoneOverride.HasValue) return PhoneOverride.Value;
                int shortSide = Mathf.Min(Width, Height);
                if (shortSide <= 500) return true;
                if (shortSide > 1300) return false;
                if (UnityEngine.Device.Application.isMobilePlatform || UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld) return true;
                return NihongoLife.Core.TouchControlPrefs.Mode == 1;
            }
        }
    }
}
