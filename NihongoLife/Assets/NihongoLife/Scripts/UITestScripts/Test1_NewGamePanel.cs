
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife
{
    public class Test1_NewGamePanel : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button signupButton;
        [SerializeField] private Button guestModeButton;
        [SerializeField] private Button backButton;

        [Header("Panels")]
        [SerializeField] private RectTransform loginPanel;
        [SerializeField] private RectTransform signupPanel;
        [SerializeField] private RectTransform guestModePanel;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float slideDistance = 1920f;

        private RectTransform currentPanel;
        private Coroutine panelAnimation;
        private bool isAnimating;

        private void Start()
        {
            loginPanel.gameObject.SetActive(true);
            signupPanel.gameObject.SetActive(false);
            guestModePanel.gameObject.SetActive(false);

            loginPanel.anchoredPosition = Vector2.zero;
            signupPanel.anchoredPosition = Vector2.zero;
            guestModePanel.anchoredPosition = Vector2.zero;

            currentPanel = loginPanel;
        }

        private void OnEnable()
        {
            backButton.onClick.AddListener(OnClickBackButton);
            signupButton.onClick.AddListener(OnClickSignupButton);
            guestModeButton.onClick.AddListener(OnClickGuestModeButton);
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveListener(OnClickBackButton);
            signupButton.onClick.RemoveListener(OnClickSignupButton);
            guestModeButton.onClick.RemoveListener(OnClickGuestModeButton);
        }

        #region Button Click Handlers
        private void OnClickBackButton()
        {
            if (currentPanel == loginPanel)
                return;

            StartPanelAnimation(loginPanel, false);
        }

        private void OnClickSignupButton()
        {
            StartPanelAnimation(signupPanel, true);
        }

        private void OnClickGuestModeButton()
        {
            StartPanelAnimation(guestModePanel, true);
        }
        #endregion

        #region Panel Animation
        private void StartPanelAnimation(RectTransform nextPanel, bool slideLeft)
        {
            if (isAnimating || nextPanel == currentPanel) return;

            if (panelAnimation != null) StopCoroutine(panelAnimation);

            panelAnimation = StartCoroutine(PanelsAnimation(currentPanel, nextPanel, slideLeft));
        }

        private IEnumerator PanelsAnimation(RectTransform oldPanel, RectTransform nextPanel, bool slideLeft)
        {
            isAnimating = true;

            float direction = slideLeft ? 1f : -1f;

            // Panel mới bắt đầu ngoài màn hình.
            nextPanel.gameObject.SetActive(true);
            nextPanel.anchoredPosition = new Vector2(direction * slideDistance, 0f);

            Vector2 oldStartPosition = oldPanel.anchoredPosition;
            Vector2 oldEndPosition = new Vector2(-direction * slideDistance, oldStartPosition.y);

            Vector2 newStartPosition = nextPanel.anchoredPosition;
            Vector2 newEndPosition = Vector2.zero;

            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsedTime / animationDuration);

                // Smooth easing.
                t = t * t * (3f - 2f * t);

                oldPanel.anchoredPosition = Vector2.Lerp(oldStartPosition, oldEndPosition, t);

                nextPanel.anchoredPosition = Vector2.Lerp(newStartPosition, newEndPosition, t);

                yield return null;
            }

            // Đưa các panel về vị trí cuối cùng.
            oldPanel.anchoredPosition = Vector2.zero;
            oldPanel.gameObject.SetActive(false);

            nextPanel.anchoredPosition = Vector2.zero;

            currentPanel = nextPanel;

            isAnimating = false;
            panelAnimation = null;
        }
        #endregion
    }
}
