using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Keeps the city's distant scenery around the camera so the player never sees the end of the map: the
    /// outer-district silhouettes repeat every <see cref="period"/> metres along the endless street (snapped,
    /// so they never visibly slide), the ground strips follow the camera along the street, and the hill ring
    /// plus the haze band follow it on both axes. The haze band is tinted with the live fog colour each frame
    /// so the horizon melts into the day/night sky instead of ending in a hard edge or a black void.
    /// Built by HorizonBuilder into 90_TestSandbox.
    /// </summary>
    public sealed class HorizonBackdrop : MonoBehaviour
    {
        [SerializeField] private Transform periodicRoot;
        [SerializeField] private float period = 100f;
        [SerializeField] private Transform followAlongStreet;
        [SerializeField] private Transform followCamera;
        [SerializeField] private Renderer[] hazeRenderers;
        [SerializeField] private Renderer[] windowRenderers;
        [SerializeField] private Color windowGlow = new Color(1.6f, 1.36f, 0.96f);

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private Camera _camera;

        private void LateUpdate()
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }
            Vector3 eye = _camera.transform.position;
            if (periodicRoot != null) periodicRoot.position = new Vector3(0f, 0f, Mathf.Round(eye.z / period) * period);
            if (followAlongStreet != null) followAlongStreet.position = new Vector3(0f, 0f, eye.z);
            if (followCamera != null) followCamera.position = new Vector3(eye.x, 0f, eye.z);

            _block ??= new MaterialPropertyBlock();
            // Outer-district windows light up as the ambient light falls (dusk → night).
            Color ambient = RenderSettings.ambientLight;
            float darkness = Mathf.InverseLerp(0.4f, 0.1f, (ambient.r + ambient.g + ambient.b) / 3f);
            if (windowRenderers != null)
                foreach (var block in windowRenderers)
                {
                    if (block == null) continue;
                    block.GetPropertyBlock(_block);
                    _block.SetColor(EmissionColor, windowGlow * darkness);
                    block.SetPropertyBlock(_block);
                }
            if (hazeRenderers == null) return;
            Color tint = RenderSettings.fogColor;
            tint.a = 1f;
            foreach (var haze in hazeRenderers)
            {
                if (haze == null) continue;
                haze.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, tint);
                haze.SetPropertyBlock(_block);
            }
        }
    }
}
