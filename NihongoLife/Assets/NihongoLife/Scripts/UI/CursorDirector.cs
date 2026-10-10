using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.UI
{
    /// <summary>
    /// The single owner of the mouse cursor. Runs after every other script each frame and decides between:
    /// · Gameplay mode (default MouseLook scheme): cursor locked + hidden, mouse movement turns the camera;
    /// · Cursor mode: cursor visible and free — while Ctrl is held, while any window / dialogue / exam is open,
    ///   while the player is locked (cutscene, travel), when the window has lost focus, or always in the
    ///   Sims-style ClickToMove scheme.
    /// Older scripts that still set Cursor.lockState are simply overruled here, so the cursor can never get stuck.
    /// WebGL: the browser only grants pointer lock after a click inside the canvas; until then right-drag still
    /// turns the camera (ThirdPersonCameraController falls back to drag when the lock is not held).
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class CursorDirector : MonoBehaviour
    {
        private static CursorDirector _instance;
        private PlayerController _player;
        private ThirdPersonCameraController _camera;
        private float _nextLookup;

        /// <summary>True while the camera should follow raw mouse movement (cursor locked).</summary>
        public static bool GameplayLook { get; private set; }
        /// <summary>Frame on which gameplay look last started (the camera skips that frame's delta: no jump).</summary>
        public static int LookStartedFrame { get; private set; }
        /// <summary>True while the player holds the temporary-cursor key (Ctrl by default).</summary>
        public static bool CursorKeyHeld { get; private set; }
        /// <summary>Test hook: batchmode has no focused window; tests set this to exercise the gameplay-mode rules.</summary>
        public static bool AssumeFocusForTests;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("CursorDirector");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CursorDirector>();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            // Losing focus always releases the cursor (alt-tab, browser tab switch).
            GameplayLook = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            var input = GameInputService.Instance;
            CursorKeyHeld = (input != null && input.IsPressed(GameInputId.FreeCursor))
                || (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed));

            bool look = WantsLook();
            if (look != GameplayLook)
            {
                GameplayLook = look;
                if (look) LookStartedFrame = Time.frameCount;
            }

            if (look)
            {
                if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private bool WantsLook()
        {
            if (ControlSettings.Scheme != ControlScheme.MouseLook) return false;
            if ((Application.isBatchMode || !Application.isFocused) && !AssumeFocusForTests) return false;
            if (Time.unscaledTime >= _nextLookup || _player == null || _camera == null)
            {
                _nextLookup = Time.unscaledTime + 0.5f;
                if (_player == null) _player = FindFirstObjectByType<PlayerController>();
                if (_camera == null) _camera = FindFirstObjectByType<ThirdPersonCameraController>();
            }
            if (_player == null || _camera == null || !_camera.isActiveAndEnabled) return false;
            if (_player.InputLocked || _camera.IsLocked) return false;
            if (CursorKeyHeld) return false;
            if (UiModalStack.AnyOpen || UiModalStack.IsTyping) return false;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return false;
            return true;
        }
    }
}
