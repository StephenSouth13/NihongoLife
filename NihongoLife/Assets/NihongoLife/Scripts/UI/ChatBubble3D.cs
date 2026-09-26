using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Core;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    public class ChatBubble3D : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);
        [SerializeField] private float visibleDuration = 5f;

        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _textMesh;
        private Coroutine _hideCoroutine;
        private Transform _mainCameraTransform;
        
        public string playerDisplayName;

        private void Start()
        {
            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;

            // 1. Create Canvas
            GameObject canvasGo = new GameObject("ChatBubbleCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = offset;
            
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400, 150);
            canvasRect.localScale = Vector3.one * 0.006f;

            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            // 2. Create Background
            GameObject bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvasGo.transform, false);
            Image bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.95f); // Dark elegant theme
            bg.type = Image.Type.Sliced;
            bg.sprite = UIStyleKit.RoundedSprite();
            
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            // 3. Create Text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(bgGo.transform, false);
            _textMesh = textGo.AddComponent<TextMeshProUGUI>();
            _textMesh.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.fontSize = 28;
            _textMesh.textWrappingMode = TextWrappingModes.Normal;
            
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 15);
            textRect.offsetMax = new Vector2(-15, -15);

            // 4. Auto-Detect Player Name
            var remote = GetComponentInParent<RemotePlayerAvatar>();
            if (remote != null) 
            {
                playerDisplayName = remote.DisplayName;
            }
            else if (GetComponentInParent<PlayerController>() != null)
            {
                if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo))
                {
                    playerDisplayName = repo.GetProgress()?.displayName ?? "Learner";
                }
            }

            // 5. Subscribe to Chat
            if (GameServices.TryGet(out IOnlineWorldService online))
            {
                online.OnChatMessageReceived += HandleChatMessage;
            }
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out IOnlineWorldService online))
            {
                online.OnChatMessageReceived -= HandleChatMessage;
            }
        }

        private void LateUpdate()
        {
            // Billboard effect: Always face camera
            if (_mainCameraTransform != null && _canvasGroup.alpha > 0)
            {
                canvasGo.transform.rotation = _mainCameraTransform.rotation;
            }
        }

        private GameObject canvasGo => _canvasGroup.gameObject;

        private void HandleChatMessage(OnlineChatMessage msg)
        {
            if (string.IsNullOrEmpty(playerDisplayName)) return;
            
            if (msg.senderDisplayName == playerDisplayName)
            {
                ShowMessage(msg.text);
            }
        }

        public void ShowMessage(string msg)
        {
            _textMesh.text = msg;
            
            // Auto size background based on text length
            float newHeight = Mathf.Clamp(msg.Length * 1.5f + 80f, 100f, 300f);
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400, newHeight);

            _canvasGroup.alpha = 1f;

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(visibleDuration);
            float t = 0;
            while(t < 0.5f)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
        }
    }
}
