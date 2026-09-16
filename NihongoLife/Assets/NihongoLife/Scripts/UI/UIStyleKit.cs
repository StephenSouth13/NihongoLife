using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Small shared visual-polish helpers for runtime-built UI (rounded-corner sprite
    /// generated procedurally so no external art asset is needed, consistent button
    /// styling, and a simple fade/scale-in for popup panels). Used by MainMenuUI and can
    /// be reused by any other runtime-built menu.
    /// </summary>
    public static class UIStyleKit
    {
        public static readonly Color AccentGold = new Color(0.95f, 0.72f, 0.25f, 1f);
        public static readonly Color AccentGoldHover = new Color(1f, 0.8f, 0.38f, 1f);
        public static readonly Color AccentGoldPressed = new Color(0.82f, 0.6f, 0.16f, 1f);

        public static readonly Color PanelBase = new Color(0.085f, 0.1f, 0.115f, 0.93f);
        public static readonly Color PanelHover = new Color(0.15f, 0.18f, 0.2f, 0.96f);
        public static readonly Color PanelPressed = new Color(0.05f, 0.06f, 0.07f, 0.96f);

        private const int SpriteSize = 64;
        private const float CornerRadius = 18f;
        private static Sprite _roundedSprite;

        /// <summary>Procedurally generated rounded-rect sprite, built once and cached.</summary>
        public static Sprite RoundedSprite()
        {
            if (_roundedSprite != null)
            {
                return _roundedSprite;
            }

            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[SpriteSize * SpriteSize];
            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    bool nearLeft = px < CornerRadius;
                    bool nearRight = px > SpriteSize - CornerRadius;
                    bool nearTop = py < CornerRadius;
                    bool nearBottom = py > SpriteSize - CornerRadius;

                    float alpha = 1f;
                    if ((nearLeft || nearRight) && (nearTop || nearBottom))
                    {
                        float cx = nearLeft ? CornerRadius : SpriteSize - CornerRadius;
                        float cy = nearTop ? CornerRadius : SpriteSize - CornerRadius;
                        float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                        alpha = Mathf.Clamp01(CornerRadius - dist + 0.5f);
                    }

                    pixels[y * SpriteSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _roundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, SpriteSize, SpriteSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius));
            _roundedSprite.name = "UIStyleKit_RoundedRect";
            return _roundedSprite;
        }

        /// <summary>Rounded corners + consistent hover/press colors + hover-scale feedback.</summary>
        public static void StyleButton(Button button, Color baseColor, Color hoverColor, Color pressedColor)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RoundedSprite();
                image.type = Image.Type.Sliced;
                image.color = baseColor;
            }

            // ColorBlock multiplies onto the Image color; ButtonColorSwap below sets the
            // Image color directly on hover/press, so keep every ColorBlock entry neutral.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var hover = button.GetComponent<ButtonColorSwap>();
            if (hover == null)
            {
                hover = button.gameObject.AddComponent<ButtonColorSwap>();
            }
            hover.Configure(image, baseColor, hoverColor, pressedColor);

            if (button.GetComponent<UIHoverScale>() == null)
            {
                var scale = button.gameObject.AddComponent<UIHoverScale>();
                scale.hoverScale = 1.045f;
                scale.pressScale = 0.96f;
            }
        }

        /// <summary>Rounded panel background with a thin accent top bar for a "designed" feel.</summary>
        public static void StylePanel(RectTransform panelRect, Color baseColor)
        {
            if (panelRect == null) return;

            var image = panelRect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RoundedSprite();
                image.type = Image.Type.Sliced;
                image.color = baseColor;
            }

            if (panelRect.Find("AccentBar") != null) return;

            var bar = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            var barRect = (RectTransform)bar.transform;
            barRect.SetParent(panelRect, false);
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(-24f, 4f);
            barRect.anchoredPosition = new Vector2(0f, -14f);
            bar.GetComponent<Image>().color = AccentGold;
        }

        /// <summary>Quick scale+fade pop-in whenever a panel/popup is shown.</summary>
        public static void PlayShowAnimation(GameObject target)
        {
            if (target == null) return;
            var runner = target.GetComponent<UIPopupAnimator>();
            if (runner == null)
            {
                runner = target.AddComponent<UIPopupAnimator>();
            }
            runner.PlayIn();
        }
    }

    /// <summary>Swaps a target Image's color on hover/press instead of relying on ColorBlock
    /// multiply, so procedurally-set base colors stay exact and consistent everywhere.</summary>
    public class ButtonColorSwap : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        private Image _image;
        private Color _base;
        private Color _hover;
        private Color _pressed;

        public void Configure(Image image, Color baseColor, Color hoverColor, Color pressedColor)
        {
            _image = image;
            _base = baseColor;
            _hover = hoverColor;
            _pressed = pressedColor;
            if (_image != null) _image.color = _base;
        }

        /// <summary>Lets callers (e.g. a "selected" toggle state) change the resting color
        /// without losing hover/press feedback.</summary>
        public void SetBaseColor(Color baseColor)
        {
            _base = baseColor;
            if (_image != null) _image.color = _base;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _hover;
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _base;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _pressed;
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _base;
        }
    }

    /// <summary>Small non-linear scale+alpha pop-in coroutine for popup panels.</summary>
    public class UIPopupAnimator : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private Coroutine _running;

        public void PlayIn()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }

            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(AnimateIn());
        }

        private IEnumerator AnimateIn()
        {
            const float duration = 0.16f;
            var rect = transform as RectTransform;
            Vector3 endScale = Vector3.one;
            Vector3 startScale = endScale * 0.92f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (rect != null) rect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                _canvasGroup.alpha = eased;
                yield return null;
            }

            if (rect != null) rect.localScale = endScale;
            _canvasGroup.alpha = 1f;
            _running = null;
        }
    }
}
