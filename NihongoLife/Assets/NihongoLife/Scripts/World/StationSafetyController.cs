using System.Collections;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.World
{
    public sealed class StationSafetyController : MonoBehaviour
    {
        [SerializeField] private Transform safeSpawn;
        [SerializeField] private Vector3 districtCenter = new Vector3(800f, 0f, -2f);
        [SerializeField] private Vector3 districtHalfExtents = new Vector3(57f, 12f, 17f);
        [SerializeField] private Vector3 carriageCenter = new Vector3(800f, 2f, 42f);
        [SerializeField] private Vector3 carriageHalfExtents = new Vector3(11f, 6f, 8f);
        [SerializeField] private Vector3 trackCenter = new Vector3(800f, 0f, -3.6f);
        [SerializeField] private Vector3 trackHalfExtents = new Vector3(21f, 5f, 3.5f);
        [SerializeField] private float fallY = -2.5f;

        private PlayerController _player;
        private float _nextSearchTime;
        private float _nextRescueTime;
        private bool _rescuing;
        private CanvasGroup _rescueFade;

        public void Configure(Transform spawn)
        {
            safeSpawn = spawn;
        }

        private void Awake()
        {
            if (safeSpawn == null) safeSpawn = GameObject.Find("Spawn_station_entrance")?.transform;
            EnsureBarrier("Boundary_West", new Vector3(742.5f, 2f, -2f), new Vector3(1f, 4f, 35f));
            EnsureBarrier("Boundary_East", new Vector3(857.5f, 2f, -2f), new Vector3(1f, 4f, 35f));
            EnsureBarrier("Boundary_South", new Vector3(800f, 2f, -19.5f), new Vector3(116f, 4f, 1f));
            EnsureBarrier("Boundary_North", new Vector3(800f, 2f, 15.5f), new Vector3(116f, 4f, 1f));
            EnsureBarrier("PlatformEdge_Left", new Vector3(789f, 1.1f, 0.15f), new Vector3(20f, 2.2f, 0.35f));
            EnsureBarrier("PlatformEdge_Right", new Vector3(811f, 1.1f, 0.15f), new Vector3(20f, 2.2f, 0.35f));
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                if (Time.unscaledTime < _nextSearchTime) return;
                _player = FindFirstObjectByType<PlayerController>();
                _nextSearchTime = Time.unscaledTime + 0.5f;
                if (_player == null) return;
            }

            Vector3 position = _player.transform.position;
            bool inDistrict = ContainsXZ(position, districtCenter, districtHalfExtents);
            bool inCarriage = ContainsXZ(position, carriageCenter, carriageHalfExtents);
            bool onTracks = ContainsXZ(position, trackCenter, trackHalfExtents);
            bool unsafePosition = position.y < fallY || (!inDistrict && !inCarriage) || (onTracks && !inCarriage);
            if (!unsafePosition || _rescuing || Time.unscaledTime < _nextRescueTime) return;

            StartCoroutine(RescuePlayer(position));
        }

        private IEnumerator RescuePlayer(Vector3 unsafePosition)
        {
            _rescuing = true;
            EnsureRescueFade();
            yield return FadeTo(1f, 0.18f);
            bool fellNearTracks = Mathf.Abs(unsafePosition.x - trackCenter.x) <= trackHalfExtents.x + 4f;
            Vector3 target = fellNearTracks
                ? new Vector3(Mathf.Clamp(unsafePosition.x, 786f, 814f), 0.38f, 1.9f)
                : safeSpawn != null ? safeSpawn.position : new Vector3(800f, 0.38f, 6.8f);
            Quaternion rotation = fellNearTracks
                ? Quaternion.Euler(0f, 180f, 0f)
                : safeSpawn != null ? safeSpawn.rotation : Quaternion.identity;
            CharacterController controller = _player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            _player.transform.SetPositionAndRotation(target, rotation);
            if (controller != null) controller.enabled = true;
            _nextRescueTime = Time.unscaledTime + 1f;
            Debug.LogWarning($"[StationSafety] Rescued player from unsafe position {unsafePosition} to {target}.");
            yield return new WaitForSecondsRealtime(0.08f);
            yield return FadeTo(0f, 0.3f);
            _rescuing = false;
        }

        private void EnsureRescueFade()
        {
            if (_rescueFade != null) return;
            GameObject overlay = new GameObject("StationSafetyFade");
            overlay.transform.SetParent(transform, false);
            Canvas canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            overlay.AddComponent<CanvasScaler>();
            GameObject imageObject = new GameObject("Fade");
            imageObject.transform.SetParent(overlay.transform, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageObject.AddComponent<Image>().color = Color.black;
            _rescueFade = imageObject.AddComponent<CanvasGroup>();
            _rescueFade.alpha = 0f;
            _rescueFade.blocksRaycasts = false;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = _rescueFade.alpha;
            float elapsed = 0f;
            _rescueFade.blocksRaycasts = target > 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _rescueFade.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _rescueFade.alpha = target;
            if (target <= 0f) _rescueFade.blocksRaycasts = false;
        }

        private static bool ContainsXZ(Vector3 point, Vector3 center, Vector3 halfExtents)
        {
            return Mathf.Abs(point.x - center.x) <= halfExtents.x
                && Mathf.Abs(point.z - center.z) <= halfExtents.z;
        }

        private void EnsureBarrier(string barrierName, Vector3 position, Vector3 size)
        {
            Transform existing = transform.Find(barrierName);
            GameObject barrier = existing != null ? existing.gameObject : new GameObject(barrierName);
            barrier.layer = 2;
            barrier.transform.SetParent(transform);
            barrier.transform.SetPositionAndRotation(position, Quaternion.identity);
            BoxCollider collider = barrier.GetComponent<BoxCollider>();
            if (collider == null) collider = barrier.AddComponent<BoxCollider>();
            collider.size = size;
        }
    }
}
