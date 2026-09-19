using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    public class QuestDirectionMarker : MonoBehaviour
    {
        [SerializeField] private Transform player;

        private Transform _target;
        private GameObject _marker;
        private GameObject _arrow;
        private RectTransform _hudIndicator;
        private TextMeshProUGUI _arrowText;
        private TextMeshProUGUI _distanceText;
        private float _nextTargetRefresh;

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
            EnsureVisuals();
            RefreshTarget();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextTargetRefresh)
            {
                _nextTargetRefresh = Time.unscaledTime + 0.25f;
                RefreshTarget();
            }

            if (_target == null || player == null || Camera.main == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            _marker.transform.position = _target.position + Vector3.up * 0.04f;
            _marker.transform.Rotate(Vector3.up, 80f * Time.deltaTime, Space.World);

            Vector3 direction = _target.position - player.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.01f) return;

            _arrow.transform.position = player.position + Vector3.up * 0.05f + direction.normalized * 1.25f;
            _arrow.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            UpdateHudIndicator(direction);
        }

        public void ShowCurrentObjectiveTarget()
        {
            RefreshTarget();
            if (_target == null)
            {
                Debug.Log("[QuestDirectionMarker] Current objective has no target yet.");
            }
        }

        public void Hide()
        {
            _target = null;
            SetVisible(false);
        }

        private void EnsureVisuals()
        {
            if (_marker == null)
            {
                _marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _marker.name = "QuestWorldMarker";
                _marker.transform.localScale = new Vector3(0.38f, 0.025f, 0.38f);
                _marker.GetComponent<Renderer>().sharedMaterial = CreateMaterial("QuestMarkerMat", new Color(1f, 0.72f, 0.16f, 1f));
                Destroy(_marker.GetComponent<Collider>());
            }

            if (_arrow == null)
            {
                _arrow = new GameObject("QuestDirectionArrow");
                _arrow.name = "QuestDirectionArrow";
                Material arrowMaterial = CreateMaterial("QuestArrowMat", new Color(1f, 0.72f, 0.16f, 1f));
                CreateArrowWing(_arrow.transform, "Left", new Vector3(-0.16f, 0f, 0f), Quaternion.Euler(0f, -38f, 0f), arrowMaterial);
                CreateArrowWing(_arrow.transform, "Right", new Vector3(0.16f, 0f, 0f), Quaternion.Euler(0f, 38f, 0f), arrowMaterial);
            }

            if (_hudIndicator == null && transform is RectTransform hudRoot)
            {
                var indicator = new GameObject("QuestWaypoint", typeof(RectTransform), typeof(Image));
                indicator.transform.SetParent(hudRoot, false);
                _hudIndicator = (RectTransform)indicator.transform;
                _hudIndicator.sizeDelta = new Vector2(220f, 58f);
                indicator.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.055f, 0.9f);

                _arrowText = CreateHudText(indicator.transform, "Arrow", ">", new Vector2(-88f, 0f), new Vector2(38f, 38f), 30f);
                _arrowText.color = new Color(1f, 0.76f, 0.2f, 1f);
                _distanceText = CreateHudText(indicator.transform, "Distance", "0 m", new Vector2(20f, 0f), new Vector2(164f, 36f), 16f);
            }
        }

        private void RefreshTarget()
        {
            _target = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentTargetTransform : null;
        }

        private void UpdateHudIndicator(Vector3 worldDirection)
        {
            if (_hudIndicator == null) return;

            _hudIndicator.anchorMin = _hudIndicator.anchorMax = new Vector2(0.5f, 0.92f);
            _hudIndicator.anchoredPosition = Vector2.zero;

            Vector3 localDirection = Camera.main.transform.InverseTransformDirection(worldDirection.normalized);
            float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            if (_arrowText != null) _arrowText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            if (_distanceText != null)
            {
                Vector3 targetPosition = _target.position;
                _distanceText.text = $"{worldDirection.magnitude:0} m  X:{targetPosition.x:0} Z:{targetPosition.z:0}";
            }
        }

        private static TextMeshProUGUI CreateHudText(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateArrowWing(Transform parent, string name, Vector3 position, Quaternion rotation, Material material)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = name;
            wing.transform.SetParent(parent, false);
            wing.transform.localPosition = position;
            wing.transform.localRotation = rotation;
            wing.transform.localScale = new Vector3(0.09f, 0.025f, 0.48f);
            wing.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(wing.GetComponent<Collider>());
        }

        private void SetVisible(bool visible)
        {
            if (_marker != null) _marker.SetActive(visible);
            if (_arrow != null) _arrow.SetActive(visible);
            if (_hudIndicator != null) _hudIndicator.gameObject.SetActive(visible);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            return new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"))
            {
                name = name,
                color = color
            };
        }
    }
}
