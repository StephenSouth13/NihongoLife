using System.Collections;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;

namespace NihongoLife.Home
{
    /// <summary>
    /// The bed in the player's room. F → lie down on the mattress, screen fades to night, the in-game
    /// clock jumps to the next morning, sleepiness/energy are restored and saved, then the player gets
    /// up beside the bed. Movement and the camera are locked for the whole sequence so it cannot be
    /// interrupted half way (the old version only trickled energy for 5 s and never moved the player).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BedroomRestInteractable : MonoBehaviour, IInteractable, IConditionalInteractable
    {
        [Tooltip("Pose the player takes while lying on the mattress (position = hips, forward = toward the feet).")]
        [SerializeField] private Transform lieAnchor;
        [Tooltip("Where the player stands after waking up.")]
        [SerializeField] private Transform standAnchor;
        [SerializeField] private float wakeUpHour = 7f;
        [SerializeField] private float sleepHours = 8f;

        private bool _busy;

        public bool IsInteractionAvailable => !_busy;
        public string GetPromptJa() => "ねる";
        public string GetpromptEn() => "Đi ngủ";
        public Transform GetTransform() => transform;

        public void Configure(Transform lie, Transform stand)
        {
            lieAnchor = lie;
            standAnchor = stand;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public void Interact(GameObject player)
        {
            if (_busy || player == null) return;
            StartCoroutine(SleepRoutine(player));
        }

        private IEnumerator SleepRoutine(GameObject player)
        {
            _busy = true;
            var controller = player.GetComponent<PlayerController>();
            var body = player.GetComponent<CharacterController>();
            var animation = player.GetComponentInChildren<CharacterAnimationController>();
            var room = HomeBedroomRuntime.Instance;
            var status = PlayerStatus.Instance;
            float restBefore = status != null ? status.Restfulness : 0f;
            float energyBefore = status != null ? status.CurrentEnergy : 0f;

            if (controller != null) controller.InputLocked = true;
            if (body != null) body.enabled = false;

            if (lieAnchor != null) player.transform.SetPositionAndRotation(lieAnchor.position, lieAnchor.rotation);
            if (animation != null) animation.SetResting(true);
            StartCoroutine(SettleOnMattress(player));
            if (room != null) room.SetRoomLights(false, true);

            if (room != null) yield return room.FadeToNight("おやすみなさい", "Chúc ngủ ngon…");
            else yield return new WaitForSeconds(1.5f);

            if (status != null) status.Sleep(sleepHours);
            var cycle = FindFirstObjectByType<DayNightCycle>();
            if (cycle != null) cycle.SetHour(wakeUpHour);

            yield return new WaitForSeconds(0.6f);

            if (animation != null) animation.SetResting(false);
            if (standAnchor != null) player.transform.SetPositionAndRotation(standAnchor.position, standAnchor.rotation);
            if (room != null) room.SetRoomLights(true, true);

            string summary = status == null ? string.Empty :
                $"Tỉnh táo {restBefore:0} → {status.Restfulness:0}   ·   Năng lượng {energyBefore:0} → {status.CurrentEnergy:0}";
            if (room != null)
            {
                room.ResetDailyActivities();
                yield return room.FadeToMorning("おはようございます！", $"Chào buổi sáng! {wakeUpHour:00}:00", summary);
            }

            if (body != null) body.enabled = true;
            if (controller != null) controller.InputLocked = false;
            _busy = false;
        }

        /// <summary>
        /// The lie anchor is the hips of an adult body; characters with other proportions (the chibi kids, whose big head
        /// rests lower than their hips) are lifted so the lowest point of the body rests on the mattress, never in it.
        /// </summary>
        private IEnumerator SettleOnMattress(GameObject player)
        {
            yield return new WaitForSeconds(0.35f); // the lying pose has blended in
            if (lieAnchor == null || player == null) yield break;
            float mattress = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(lieAnchor.position + Vector3.up * 1.2f, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(player.transform)) mattress = Mathf.Max(mattress, hit.point.y);
            if (float.IsNegativeInfinity(mattress)) yield break;
            float lowest = float.PositiveInfinity;
            foreach (var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>()) lowest = Mathf.Min(lowest, skin.bounds.min.y);
            if (float.IsPositiveInfinity(lowest)) yield break;
            float lift = mattress - 0.01f - lowest;
            if (lift > 0f) player.transform.position += Vector3.up * lift;
        }
    }
}
