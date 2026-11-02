using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.MiniGames
{
    /// <summary>A button that counts while it is held down, for controls that steer rather than click.</summary>
    public sealed class ArcadeHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => Held = true;
        public void OnPointerUp(PointerEventData eventData) => Held = false;
        public void OnPointerExit(PointerEventData eventData) => Held = false;
        private void OnDisable() => Held = false;
    }
}
