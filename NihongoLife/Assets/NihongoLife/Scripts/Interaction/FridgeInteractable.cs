using System.Collections;
using UnityEngine;
using NihongoLife.Audio;
using NihongoLife.Core;

namespace NihongoLife.Interaction
{
    /// <summary>Small runtime interaction for a display fridge. The model stays static;
    /// only a named door child is animated, so stock props cannot inherit the rotation.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class FridgeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform door;
        [SerializeField] private Vector3 openLocalEuler = new Vector3(0f, 72f, 0f);
        [SerializeField] private string promptJa = "冷蔵庫を開ける";
        [SerializeField] private string promptEn = "Mở tủ lạnh";
        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _open;
        private Coroutine _animation;

        public string GetPromptJa() => _open ? "冷蔵庫を閉める" : promptJa;
        public string GetpromptEn() => _open ? "Đóng tủ lạnh" : promptEn;
        public Transform GetTransform() => transform;

        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
            if (door == null)
            {
                door = transform.Find("Door") ?? transform.Find("FridgeDoor") ?? transform;
            }
            _closedRotation = door.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(openLocalEuler);
            foreach (var body in GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        public void Interact(GameObject player)
        {
            _open = !_open;
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(Animate(_open));
            if (GameServices.TryGet(out IAudioService audio))
                audio.PlayCue(_open ? GameAudioCue.DoorOpen : GameAudioCue.DoorClose, 0.65f);
        }

        private IEnumerator Animate(bool opening)
        {
            Quaternion start = door.localRotation;
            Quaternion target = opening ? _openRotation : _closedRotation;
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                door.localRotation = Quaternion.Slerp(start, target, elapsed / 0.35f);
                yield return null;
            }
            door.localRotation = target;
        }
    }
}
