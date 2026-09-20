using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class ZonePreviewCamera : MonoBehaviour
    {
        private void Awake()
        {
            // Standalone zone editing gets a useful Game view. During additive gameplay,
            // the persistent player camera remains authoritative.
            if (SceneManager.sceneCount <= 1) return;

            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camera.gameObject == gameObject) continue;
                gameObject.SetActive(false);
                return;
            }
        }
    }
}
