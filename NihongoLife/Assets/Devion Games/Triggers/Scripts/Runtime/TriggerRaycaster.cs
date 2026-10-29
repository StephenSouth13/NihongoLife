using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DevionGames
{
    /// <summary>
    /// This component raycast from camera and sends executes OnCameraRaycast in DisplayName component. 
    /// The DisplayName component will then check if CameraRaycast is included in Display Type and execute DoDisplayName.
    /// </summary>
    public class TriggerRaycaster : MonoBehaviour
    {
        [SerializeField]
        private LayerMask m_LayerMask = Physics.DefaultRaycastLayers;

        private Transform m_Transform;
        private GameObject m_LastCameraHit;

        private static bool m_PointerOverTrigger = false;

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            Camera camera = Camera.main;
            if (camera != null && camera.GetComponent<TriggerRaycaster>() == null)
                camera.gameObject.AddComponent<TriggerRaycaster>();
        }


        private void Start()
        {
            this.m_Transform = transform;    
        }

        private void Update()
        {

            Camera camera = Camera.main;
            if (camera == null) return;
            Ray ray = Cursor.lockState == CursorLockMode.Locked
                ? new Ray(this.m_Transform.position, this.m_Transform.forward)
                : camera.ScreenPointToRay(PointerPosition());

            RaycastHit hit;
            if (TriggerRaycaster.Raycast(ray, out hit, float.PositiveInfinity, this.m_LayerMask))
            {
                GameObject current = hit.collider.GetComponentInParent<BaseTrigger>().gameObject;

                if (m_LastCameraHit != current)
                {
                    if(this.m_LastCameraHit != null)
                        EventHandler.Execute(this.m_LastCameraHit, "OnPointerExitTrigger");

                    m_LastCameraHit = current;
                    EventHandler.Execute(m_LastCameraHit, "OnPointerEnterTrigger");
                }
                // The project reads input through the Input System package only (UnityEngine.Input throws).
                int button = -1;
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    if (mouse.leftButton.wasPressedThisFrame) button = 0;
                    if (mouse.rightButton.wasPressedThisFrame) button = 1;
                    if (mouse.middleButton.wasPressedThisFrame) button = 2;
                }

                if (button != -1)
                {
                    m_LastCameraHit = current;
                    EventHandler.Execute<int>(m_LastCameraHit, "OnPoinerClickTrigger", button);
                }
                
                TriggerRaycaster.m_PointerOverTrigger = true;
            }
            else
            {
                if (m_LastCameraHit != null)
                {
                    EventHandler.Execute(m_LastCameraHit, "OnPointerExitTrigger");
                    m_LastCameraHit = null;
                }
                TriggerRaycaster.m_PointerOverTrigger = false;
            }
        }
        

        private static Vector2 PointerPosition()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Pointer.current != null) return Pointer.current.position.ReadValue();
            return Vector2.zero;
        }

        public static bool Raycast(Vector3 origin, Vector3 direction,out RaycastHit hit, float maxDistance, int layerMask ) {
            return Raycast(new Ray(origin, direction), out hit, maxDistance, layerMask);
        }

        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, layerMask, QueryTriggerInteraction.Collide);
            hit = new RaycastHit();
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit current = hits[i];
                    if (current.collider.GetComponentInParent<BaseTrigger>() == null)
                        continue;
                    hit = current;
                    return true;
                }
                return false;
            }
            return false;
        }

        public static bool IsPointerOverTrigger()
        {
            return TriggerRaycaster.m_PointerOverTrigger;
        }

    }
}
