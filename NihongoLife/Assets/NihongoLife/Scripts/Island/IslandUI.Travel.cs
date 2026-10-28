using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>Partial of IslandUI: return-trip train screen. The MonoBehaviour entry point stays IslandUI.cs.</summary>
    public sealed partial class IslandUI : MonoBehaviour
    {
        // ─────────── Return trip screen ───────────

        public static IEnumerator TravelScreen(string from, string to, float seconds)
        {
            var ui = Instance;
            ui.Discard(ui._travel);
            var root = new GameObject("TravelScreen", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            root.SetParent(ui._canvas.transform, false);
            NLUi.Stretch(root);
            root.GetComponent<Image>().color = new Color(0.07f, 0.15f, 0.11f, 0.97f);
            ui._travel = root;
            var icon = UiKit.Icon(root, "train", new Color(0.95f, 0.85f, 0.55f), 64f);
            UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), 0f, 190f, 64f, 64f);
            var title = NLUi.Label(root, "Title", Loc.T("Tuyến Midori", "Midori Line", "みどり線"), 30f, new Color(0.95f, 0.85f, 0.55f), ui._font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 50f));
            var sub = NLUi.Label(root, "Sub", $"{from}  →  {to}", 24f, Color.white, ui._font, FontStyles.Normal, TextAlignmentOptions.Center);
            NLUi.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(900f, 40f));
            var track = UiKit.Image(root, "Track", new Color(1f, 1f, 1f, 0.2f), UiKit.Rounded(3));
            UiKit.Pin(track.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 700f, 6f);
            var fill = UiKit.Image(track.transform, "Fill", new Color(0.95f, 0.85f, 0.55f), UiKit.Rounded(3));
            var fr = fill.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0f, 1f); fr.offsetMin = fr.offsetMax = Vector2.zero;
            var train = UiKit.Icon(root, "train", new Color(0.86f, 0.93f, 0.82f), 40f);
            var note = NLUi.Label(root, "Note", Loc.T("Tàu đang chạy…", "The train is on its way…", "でんしゃは はしって います…"), 18f, new Color(0.8f, 0.86f, 0.8f), ui._font, FontStyles.Italic, TextAlignmentOptions.Center);
            NLUi.Anchor(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 40f));
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                UiKit.Pin(train.rectTransform, new Vector2(0.5f, 0.5f), Mathf.Lerp(-350f, 350f, k), 28f, 40f, 40f);
                fr.anchorMax = new Vector2(k, 1f);
                yield return null;
            }
        }

        public static void EndTravelScreen()
        {
            if (_instance != null && _instance._travel != null) Destroy(_instance._travel.gameObject);
        }
    }
}
