using UnityEngine;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Interaction;

namespace NihongoLife.Home
{
    /// <summary>Wall switch by the entrance: 「電気をつける / 消す」. Toggles the main room lights and
    /// the emissive lamp shades; bedside/desk lamps listed in nightLights stay on when the room is dark.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomLightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private Light[] mainLights;
        [SerializeField] private Renderer[] emissiveShades;
        [SerializeField] private Transform toggle;
        [SerializeField] private bool lit = true;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public bool IsLit => lit;
        public string GetPromptJa() => lit ? "でんきを けす" : "でんきを つける";
        public string GetpromptEn() => lit ? "Tắt đèn" : "Bật đèn";
        public Transform GetTransform() => transform;

        public void Configure(Light[] lights, Renderer[] shades, Transform toggleKnob)
        {
            mainLights = lights;
            emissiveShades = shades;
            toggle = toggleKnob;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            Apply();
        }

        public void Interact(GameObject player)
        {
            SetLit(!lit, true);
            HomeBedroomRuntime.Instance?.ShowToast(lit ? "でんきを つけました" : "でんきを けしました", lit ? "Đã bật đèn." : "Đã tắt đèn.");
        }

        public void SetLit(bool value, bool playSound)
        {
            lit = value;
            Apply();
            if (playSound && GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.UiTick, 0.9f);
        }

        private void Apply()
        {
            if (mainLights != null)
                foreach (var light in mainLights) if (light != null) light.enabled = lit;
            if (emissiveShades != null)
                foreach (var shade in emissiveShades)
                {
                    if (shade == null) continue;
                    foreach (var material in shade.materials)
                    {
                        if (!material.HasProperty(EmissionColor)) continue;
                        material.EnableKeyword("_EMISSION");
                        material.SetColor(EmissionColor, lit ? new Color(1f, 0.85f, 0.6f) * 1.6f : Color.black);
                    }
                }
            if (toggle != null) toggle.localRotation = Quaternion.Euler(lit ? -12f : 12f, 0f, 0f);
        }
    }
}
