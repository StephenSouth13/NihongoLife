using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.Trailer
{
    public class TrailerDirector : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private List<TrailerShotDefinition> shots = new List<TrailerShotDefinition>();
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop;
        [SerializeField] private bool sortShotsByOrder = true;

        [Header("Scene References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private TextMeshProUGUI japaneseText;
        [SerializeField] private TextMeshProUGUI englishText;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeSeconds = 0.45f;
        [SerializeField] private string skipDestinationScene = "01_MainMenu";

        private readonly List<CharacterAnimationController> _talkingCharacters = new List<CharacterAnimationController>();
        private Coroutine _playback;
        private bool _advanceRequested;
        private bool _skipRequested;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            EnsureRuntimeOverlay();
            DisableLegacyLogoPlaceholder();
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            Stop();
            _advanceRequested = false;
            _skipRequested = false;
            _playback = StartCoroutine(PlaySequence());
        }

        public void NextShot()
        {
            _advanceRequested = true;
        }

        public void SkipTrailer()
        {
            if (_skipRequested) return;

            _skipRequested = true;
            Stop();
            SceneManager.LoadScene(skipDestinationScene);
        }

        public void Stop()
        {
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }

            StopTalkingCharacters();
        }

        public void SetShots(List<TrailerShotDefinition> newShots)
        {
            shots = newShots ?? new List<TrailerShotDefinition>();
        }

        private IEnumerator PlaySequence()
        {
            if (targetCamera == null || shots == null || shots.Count == 0)
            {
                yield break;
            }

            var orderedShots = new List<TrailerShotDefinition>(shots);
            if (sortShotsByOrder)
            {
                orderedShots.Sort((a, b) => GetShotOrder(a).CompareTo(GetShotOrder(b)));
            }

            do
            {
                foreach (var shot in orderedShots)
                {
                    if (_skipRequested) yield break;
                    if (shot == null)
                    {
                        continue;
                    }

                    yield return PlayShot(shot);
                }
            }
            while (loop);

            _playback = null;
        }

        private IEnumerator PlayShot(TrailerShotDefinition shot)
        {
            _advanceRequested = false;
            yield return Fade(1f, 0f, fadeSeconds);

            Transform focus = ResolveFocusTarget(shot);
            TriggerCue(shot, focus);
            SetOverlay(shot);

            Vector3 startPosition = shot.ResolveStartPosition();
            Vector3 endPosition = shot.ResolveEndPosition();
            Quaternion startRotation = shot.ResolveStartRotation();
            Quaternion endRotation = shot.ResolveEndRotation();
            float duration = Mathf.Max(0.1f, shot.durationSeconds);
            float elapsed = 0f;

            while (elapsed < duration && !_advanceRequested && !_skipRequested)
            {
                float rawT = Mathf.Clamp01(elapsed / duration);
                float t = shot.easing != null ? Mathf.Clamp01(shot.easing.Evaluate(rawT)) : rawT;

                targetCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                if (shot.lookAtFocusTarget && focus != null)
                {
                    LookAtTarget(targetCamera.transform, focus);
                }
                else
                {
                    targetCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_skipRequested) yield break;

            targetCamera.transform.position = endPosition;
            if (shot.lookAtFocusTarget && focus != null)
            {
                LookAtTarget(targetCamera.transform, focus);
            }
            else
            {
                targetCamera.transform.rotation = endRotation;
            }

            if (shot.fadeToBlackAfterShot)
            {
                yield return Fade(0f, 1f, fadeSeconds);
            }
        }

        private void TriggerCue(TrailerShotDefinition shot, Transform focus)
        {
            if (focus == null || shot.animationCueToTrigger == TrailerAnimationCue.None)
            {
                return;
            }

            var animation = focus.GetComponentInParent<CharacterAnimationController>();
            if (animation == null)
            {
                animation = focus.GetComponentInChildren<CharacterAnimationController>();
            }

            if (animation == null)
            {
                Debug.LogWarning($"[TrailerDirector] Focus target '{focus.name}' has no CharacterAnimationController for cue {shot.animationCueToTrigger}.");
                return;
            }

            switch (shot.animationCueToTrigger)
            {
                case TrailerAnimationCue.Talk:
                    animation.SetTalking(true);
                    if (!_talkingCharacters.Contains(animation))
                    {
                        _talkingCharacters.Add(animation);
                    }
                    break;
                case TrailerAnimationCue.Bow:
                    animation.TriggerBow();
                    break;
                case TrailerAnimationCue.Point:
                    animation.TriggerPoint();
                    break;
            }
        }

        private Transform ResolveFocusTarget(TrailerShotDefinition shot)
        {
            if (shot.focusTarget != null)
            {
                return shot.focusTarget;
            }

            if (string.IsNullOrWhiteSpace(shot.focusTargetName))
            {
                return null;
            }

            var target = GameObject.Find(shot.focusTargetName);
            if (target == null)
            {
                Debug.LogWarning($"[TrailerDirector] Could not find focus target named '{shot.focusTargetName}'.");
                return null;
            }

            return target.transform;
        }

        private void SetOverlay(TrailerShotDefinition shot)
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = string.IsNullOrWhiteSpace(shot.textJa) && string.IsNullOrWhiteSpace(shot.textEn) ? 0f : 1f;
            }

            if (japaneseText != null)
            {
                japaneseText.text = shot.textJa;
            }

            if (englishText != null)
            {
                englishText.text = shot.textEn;
            }
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            if (fadeGroup == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0.01f, seconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fadeGroup.alpha = to;
        }

        private void EnsureRuntimeOverlay()
        {
            if (overlayGroup != null && japaneseText != null && englishText != null && fadeGroup != null)
            {
                return;
            }

            var canvasObject = new GameObject("TrailerRuntimeOverlay");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var overlay = new GameObject("TextOverlay");
            overlay.transform.SetParent(canvasObject.transform, false);
            overlayGroup = overlay.AddComponent<CanvasGroup>();

            japaneseText = CreateOverlayText(overlay.transform, "JapaneseText", 54f, new Vector2(0f, 142f));
            englishText = CreateOverlayText(overlay.transform, "EnglishText", 28f, new Vector2(0f, 84f));
            englishText.color = new Color(0.92f, 0.96f, 1f, 1f);

            var fade = new GameObject("FadeToBlack");
            fade.transform.SetParent(canvasObject.transform, false);
            var fadeRect = fade.AddComponent<RectTransform>();
            Stretch(fadeRect);
            var image = fade.AddComponent<Image>();
            image.color = Color.black;
            fadeGroup = fade.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;

            CreateControlButton(canvasObject.transform, "NextButton", "Next  >", new Vector2(-210f, -54f), NextShot);
            CreateControlButton(canvasObject.transform, "SkipButton", "Skip", new Vector2(-54f, -54f), SkipTrailer);
            EnsureEventSystem();
        }

        private static void CreateControlButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(138f, 48f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.06f, 0.09f, 0.12f, 0.9f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var text = CreateOverlayText(go.transform, "Label", 24f, Vector2.zero);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            text.raycastTarget = false;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var eventSystem = new GameObject("TrailerEventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void DisableLegacyLogoPlaceholder()
        {
            var placeholder = GameObject.Find("NihongoLifeLogoBlock");
            if (placeholder != null)
            {
                placeholder.SetActive(false);
            }
        }

        private static TextMeshProUGUI CreateOverlayText(Transform parent, string name, float fontSize, Vector2 anchoredPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(1260f, 72f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void LookAtTarget(Transform cameraTransform, Transform focus)
        {
            Vector3 target = focus.position + Vector3.up * 1.45f;
            Vector3 direction = target - cameraTransform.position;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            cameraTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void StopTalkingCharacters()
        {
            foreach (var character in _talkingCharacters)
            {
                if (character != null)
                {
                    character.SetTalking(false);
                }
            }

            _talkingCharacters.Clear();
        }

        private static int GetShotOrder(TrailerShotDefinition shot)
        {
            return shot != null ? shot.order : int.MaxValue;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
