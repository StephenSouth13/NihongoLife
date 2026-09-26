using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace NihongoLife.Editor.UI
{
    public class MobileControlsBuilder : EditorWindow
    {
        [MenuItem("NihongoLife/Mobile/Generate Mobile Joystick UI")]
        public static void GenerateJoystick()
        {
            // 1. Check/Create EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 2. Create Canvas
            GameObject canvasGo = new GameObject("MobileControlsCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99; // On top of everything
            
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 3. Create Left Joystick (Movement)
            GameObject bg = new GameObject("Joystick_BG");
            bg.transform.SetParent(canvasGo.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.4f);
            
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(0, 0);
            bgRect.pivot = new Vector2(0, 0);
            bgRect.anchoredPosition = new Vector2(100, 100);
            bgRect.sizeDelta = new Vector2(250, 250);

            // Handle (The stick)
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(bg.transform, false);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(1, 1, 1, 0.8f);
            
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(100, 100);

            // Add Input System On-Screen Stick Component
            OnScreenStick stick = bg.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 100f;

            Selection.activeGameObject = canvasGo;
            Debug.Log("[MobileControlsBuilder] Mobile Joystick Canvas generated successfully! Make sure you have the Input System package installed.");
        }
    }
}
