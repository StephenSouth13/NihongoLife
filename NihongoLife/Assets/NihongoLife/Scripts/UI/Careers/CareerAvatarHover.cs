using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    /// <summary>Put on a career picture: while the pointer is over it the person works instead of following the idle cycle.</summary>
    public sealed class CareerAvatarHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public CareerAvatarStage.Avatar Avatar;
        public bool Always;
        public bool Over { get; private set; }

        public void OnPointerEnter(PointerEventData eventData) { Over = true; Apply(); }
        public void OnPointerExit(PointerEventData eventData) { Over = false; Apply(); }
        private void OnDisable() { Over = false; }
        private void Apply() { if (Avatar != null) Avatar.Hot = Over || Always; }
    }
}