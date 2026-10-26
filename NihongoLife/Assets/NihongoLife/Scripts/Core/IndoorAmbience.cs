using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    /// <summary>
    /// Light of an indoor zone. Zones load over the town, whose OutdoorSky sets the ambient light and fog every frame
    /// from the time of day, and whose sun / moon shines through walls that cast no shadow — so a clinic at night
    /// went dark purple. While this zone is the active scene its own ambient applies (no fog) and the town's sun is
    /// dimmed; the sun's intensity comes back when the zone is left or unloaded. Applied after the day cycle's
    /// Update (execution order) and again in LateUpdate, so every camera — the game's and a test's capture — sees it.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class IndoorAmbience : MonoBehaviour
    {
        [SerializeField] private Color ambient = new Color(0.72f, 0.74f, 0.76f);

        private Light _dimmed;
        private float _savedIntensity;

        public void Configure(Color color) => ambient = color;

        private void Update() => Apply();
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (gameObject.scene != SceneManager.GetActiveScene()) { Restore(); return; }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = false;
            var sun = RenderSettings.sun;
            if (sun == null || sun.gameObject.scene == gameObject.scene) return;
            if (_dimmed != sun) { Restore(); _dimmed = sun; _savedIntensity = sun.intensity; }
            else if (sun.intensity > 0f) _savedIntensity = sun.intensity; // the day cycle moved it on
            sun.intensity = 0f;
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (_dimmed != null && _dimmed.intensity <= 0f) _dimmed.intensity = _savedIntensity;
            _dimmed = null;
        }
    }
}
