using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// E (gamepad d-pad left): hold to open the emote wheel and release on the emote you want; a quick
    /// tap repeats the last emote. The character gestures (bow, jump, point, talk) and a bubble with the
    /// emote icon and its Japanese phrase floats above the head.
    /// </summary>
    public class PlayerEmoteController : MonoBehaviour
    {
        private const float HoldToOpen = 0.18f;
        private const float Cooldown = 0.6f;

        private GameInputService _input;
        private PlayerController _player;
        private EmoteWheelUI _wheel;
        private float _pressedAt = -1f;
        private bool _wheelOpenedByHold;
        private int _last;
        private float _nextAllowed;
        private bool _lockedByWheel;

        public int LastEmote => _last;

        private void Start()
        {
            _input = GameInputService.GetOrCreate();
            _player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_input == null) return;
            bool wheelOpen = _wheel != null && _wheel.IsOpen;

            if (wheelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseWheel();
                _pressedAt = -1f;
                return;
            }

            if (_input.WasPressed(GameInputId.Emote) && CanEmote()) _pressedAt = Time.unscaledTime;
            if (_pressedAt < 0f) return;

            bool held = _input.IsPressed(GameInputId.Emote);
            if (held && !wheelOpen && Time.unscaledTime - _pressedAt >= HoldToOpen) OpenWheel();
            if (held) return;

            // Released.
            if (_wheelOpenedByHold)
            {
                int choice = _wheel.Selected;
                CloseWheel();
                if (choice >= 0) Play(choice);
            }
            else Play(_last); // quick tap: repeat the last emote
            _pressedAt = -1f;
        }

        private bool CanEmote()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return false;
            if (_player != null && _player.InputLocked) return false; // bag, map, shop… are open
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected == null || selected.GetComponent<TMP_InputField>() == null;
        }

        private void OpenWheel()
        {
            if (_wheel == null) _wheel = EmoteWheelUI.GetOrCreate();
            _wheel.SlotClicked -= HandleSlotClicked;
            _wheel.SlotClicked += HandleSlotClicked;
            _wheel.Open(_last);
            _wheelOpenedByHold = true;
            _lockedByWheel = true;
            ScenarioManager.Instance?.SetPlayerInputLocked(true);
            if (_player != null) _player.InputLocked = true;
        }

        private void HandleSlotClicked(int index)
        {
            CloseWheel();
            _pressedAt = -1f;
            Play(index);
        }

        private void CloseWheel()
        {
            if (_wheel != null) _wheel.Close(); // Unity null: the HUD may already be destroyed on scene unload
            _wheelOpenedByHold = false;
            if (!_lockedByWheel) return;
            _lockedByWheel = false;
            ScenarioManager.Instance?.SetPlayerInputLocked(false);
            if (_player != null) _player.InputLocked = false;
        }

        /// <summary>Plays emote <paramref name="index"/> of <see cref="EmoteCatalog.All"/> (also used by tests).</summary>
        public void Play(int index)
        {
            if (index < 0 || index >= EmoteCatalog.All.Count || Time.unscaledTime < _nextAllowed) return;
            _nextAllowed = Time.unscaledTime + Cooldown;
            _last = index;
            var emote = EmoteCatalog.All[index];
            EmoteBubble.Spawn(transform, emote);

            var animation = GetComponentInChildren<CharacterAnimationController>();
            if (animation == null) return;
            switch (emote.Gesture)
            {
                case EmoteGesture.Bow: animation.TriggerBow(); break;
                case EmoteGesture.Jump: animation.TriggerJump(); break;
                case EmoteGesture.Point: animation.TriggerPoint(); break;
                default: StartCoroutine(TalkBriefly(animation)); break;
            }
        }

        private static System.Collections.IEnumerator TalkBriefly(CharacterAnimationController animation)
        {
            animation.SetTalking(true);
            yield return new WaitForSeconds(1.4f);
            if (animation != null) animation.SetTalking(false);
        }

        private void OnDisable() => CloseWheel();
    }
}
