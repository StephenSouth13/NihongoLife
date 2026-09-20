using System.Collections;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Audio;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Player
{
    public enum CivicOffense
    {
        AssaultNpc,
        Littering
    }

    [RequireComponent(typeof(PlayerController))]
    public class PlayerWorldActionController : MonoBehaviour
    {
        [SerializeField] private float attackRange = 2.1f;
        [SerializeField] private float attackRadius = 0.35f;
        [SerializeField] private LayerMask attackLayers = ~0;

        private PlayerConductSystem _conduct;

        private void Awake()
        {
            _conduct = PlayerConductSystem.GetOrCreate();
        }

        public void TryAttack()
        {
            Vector3 origin = transform.position + Vector3.up * 1.25f;
            if (!Physics.SphereCast(origin, attackRadius, transform.forward, out RaycastHit hit,
                    attackRange, attackLayers, QueryTriggerInteraction.Ignore)) return;

            var npc = hit.collider.GetComponentInParent<NPCController>();
            if (npc == null) return;
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Punch, 0.8f);
            npc.ReactToAttack(transform);
            _conduct.Report(CivicOffense.AssaultNpc);
        }

        public void TryDropLastItem()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || !inventory.TryGetLastItem(out InventoryEntry entry)) return;

            Vector3 dropPosition = transform.position + transform.forward * 1.15f + Vector3.up * 0.2f;
            if (!InteractiveItem.TrySpawnDropped(entry, dropPosition, Quaternion.identity)) return;
            if (!inventory.RemoveItem(entry.itemId)) return;
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Drop, 0.75f);
            if (entry.isLitter) _conduct.Report(CivicOffense.Littering);
        }
    }

    public class PlayerConductSystem : MonoBehaviour
    {
        private const string OffenseKey = "NihongoLife.Conduct.Offenses";
        private const string RestrictedUntilKey = "NihongoLife.Conduct.RestrictedUntil";
        private static PlayerConductSystem _instance;
        private CanvasGroup _warningOverlay;
        private Coroutine _warningRoutine;

        public static PlayerConductSystem GetOrCreate()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("PlayerConductSystem");
            DontDestroyOnLoad(go);
            return go.AddComponent<PlayerConductSystem>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public bool IsTemporarilyRestricted
        {
            get
            {
                string raw = PlayerPrefs.GetString(RestrictedUntilKey, "0");
                return long.TryParse(raw, out long ticks) && System.DateTime.UtcNow.Ticks < ticks;
            }
        }

        public void Report(CivicOffense offense)
        {
            int count = PlayerPrefs.GetInt(OffenseKey, 0) + 1;
            PlayerPrefs.SetInt(OffenseKey, count);

            int fine = offense == CivicOffense.AssaultNpc ? 500 : 100;
            PlayerInventory.Instance?.ApplyFine(fine);

            // Repeated serious violations restrict gameplay locally. A real account ban
            // must be validated by the authoritative backend, never by the client alone.
            if (count >= 5)
            {
                long until = System.DateTime.UtcNow.AddMinutes(15).Ticks;
                PlayerPrefs.SetString(RestrictedUntilKey, until.ToString());
            }
            PlayerPrefs.Save();
            ShowWarning(offense == CivicOffense.AssaultNpc ? 1.15f : 0.45f);
            Debug.LogWarning($"[Conduct] {offense}: fine Y{fine}, offense count {count}.");
        }

        private void ShowWarning(float holdSeconds)
        {
            EnsureOverlay();
            if (_warningRoutine != null) StopCoroutine(_warningRoutine);
            _warningRoutine = StartCoroutine(FadeWarning(holdSeconds));
        }

        private void EnsureOverlay()
        {
            if (_warningOverlay != null) return;
            var canvasGo = new GameObject("ConductWarningCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var imageGo = new GameObject("WarningFade");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = imageGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageGo.AddComponent<Image>().color = new Color(0.12f, 0f, 0f, 0.84f);
            _warningOverlay = imageGo.AddComponent<CanvasGroup>();
            _warningOverlay.alpha = 0f;
            _warningOverlay.blocksRaycasts = false;
        }

        private IEnumerator FadeWarning(float holdSeconds)
        {
            for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime)
            {
                _warningOverlay.alpha = t / 0.12f;
                yield return null;
            }
            _warningOverlay.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdSeconds);
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _warningOverlay.alpha = 1f - t / 0.5f;
                yield return null;
            }
            _warningOverlay.alpha = 0f;
        }
    }
}
