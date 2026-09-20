using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.Core
{
    public enum GameInputId
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Sprint,
        Jump,
        Interact,
        Inventory,
        Character,
        Chat,
        Voice,
        Attack,
        DropItem,
        Pause
    }

    public class GameInputService : MonoBehaviour, IGameService
    {
        private const string OverridesKey = "NihongoLife.InputOverrides.v1";
        private static GameInputService _instance;
        private readonly Dictionary<GameInputId, (InputAction action, int binding)> _bindings = new();
        private InputActionMap _gameplay;
        private InputAction _move;
        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

        public event Action OnBindingsChanged;
        public static GameInputService Instance => _instance;
        public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

        public static GameInputService GetOrCreate()
        {
            if (_instance != null) return _instance;
            var existing = FindFirstObjectByType<GameInputService>();
            if (existing != null)
            {
                existing.Initialize();
                return existing;
            }

            var go = new GameObject("GameInputService");
            DontDestroyOnLoad(go);
            var service = go.AddComponent<GameInputService>();
            service.Initialize();
            GameServices.Register<GameInputService>(service);
            return service;
        }

        public void Initialize()
        {
            if (_gameplay != null) return;
            _instance = this;
            BuildActions();
            string overrides = PlayerPrefs.GetString(OverridesKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(overrides)) _gameplay.LoadBindingOverridesFromJson(overrides);
            _gameplay.Enable();
        }

        private void BuildActions()
        {
            _gameplay = new InputActionMap("Gameplay");
            _move = _gameplay.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddBinding("<Gamepad>/leftStick");
            int composite = _move.AddCompositeBinding("2DVector", processors: "NormalizeVector2")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d").bindingIndex;
            _bindings[GameInputId.MoveUp] = (_move, composite + 1);
            _bindings[GameInputId.MoveDown] = (_move, composite + 2);
            _bindings[GameInputId.MoveLeft] = (_move, composite + 3);
            _bindings[GameInputId.MoveRight] = (_move, composite + 4);

            AddButton(GameInputId.Sprint, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            AddButton(GameInputId.Jump, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AddButton(GameInputId.Interact, "Interact", "<Keyboard>/f", "<Gamepad>/buttonWest");
            AddButton(GameInputId.Inventory, "Inventory", "<Keyboard>/b", "<Gamepad>/dpad/up");
            AddButton(GameInputId.Character, "Character", "<Keyboard>/tab", "<Gamepad>/select");
            AddButton(GameInputId.Chat, "Chat", "<Keyboard>/enter");
            AddButton(GameInputId.Voice, "Voice", "<Keyboard>/v");
            AddButton(GameInputId.Attack, "Attack", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            AddButton(GameInputId.DropItem, "DropItem", "<Keyboard>/g", "<Gamepad>/dpad/down");
            AddButton(GameInputId.Pause, "Pause", "<Keyboard>/escape", "<Gamepad>/start");
        }

        private void AddButton(GameInputId id, string name, string keyboardPath, string gamepadPath = null)
        {
            var action = _gameplay.AddAction(name, InputActionType.Button);
            int binding = action.AddBinding(keyboardPath).bindingIndex;
            if (!string.IsNullOrEmpty(gamepadPath)) action.AddBinding(gamepadPath);
            _bindings[id] = (action, binding);
        }

        public bool IsPressed(GameInputId id) => _bindings.TryGetValue(id, out var entry) && entry.action.IsPressed();
        public bool WasPressed(GameInputId id) => _bindings.TryGetValue(id, out var entry) && entry.action.WasPressedThisFrame();

        public string GetBindingLabel(GameInputId id)
        {
            if (!_bindings.TryGetValue(id, out var entry)) return "-";
            return entry.action.GetBindingDisplayString(entry.binding, out _, out _);
        }

        public void StartRebind(GameInputId id, Action<bool> completed)
        {
            if (!_bindings.TryGetValue(id, out var entry))
            {
                completed?.Invoke(false);
                return;
            }

            _rebindOperation?.Cancel();
            _gameplay.Disable();
            _rebindOperation = entry.action.PerformInteractiveRebinding(entry.binding)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(operation => FinishRebind(operation, false, completed))
                .OnComplete(operation => FinishRebind(operation, true, completed));
            _rebindOperation.Start();
        }

        private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation, bool save, Action<bool> completed)
        {
            operation.Dispose();
            _rebindOperation = null;
            _gameplay.Enable();
            if (save)
            {
                PlayerPrefs.SetString(OverridesKey, _gameplay.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
                OnBindingsChanged?.Invoke();
            }
            completed?.Invoke(save);
        }

        public void ResetBindings()
        {
            _rebindOperation?.Cancel();
            _gameplay.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(OverridesKey);
            PlayerPrefs.Save();
            OnBindingsChanged?.Invoke();
        }

        private void OnDestroy()
        {
            _rebindOperation?.Dispose();
            _gameplay?.Dispose();
            if (_instance == this) _instance = null;
        }
    }
}
