using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.MiniGames
{
    /// <summary>Mouse position inside a UI rectangle, for the arcade games that follow the pointer (paddle, mallet).</summary>
    public static class ArcadePointer
    {
        /// <summary>The pointer in <paramref name="rect"/>'s space measured from its bottom-left corner. False when there is no mouse or it is outside the canvas.</summary>
        public static bool TryGet(RectTransform rect, out Vector2 fromBottomLeft)
        {
            fromBottomLeft = default;
            if (rect == null || Mouse.current == null) return false;
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Mouse.current.position.ReadValue(), camera, out Vector2 local)) return false;
            fromBottomLeft = local - rect.rect.min;
            return true;
        }
    }
}
