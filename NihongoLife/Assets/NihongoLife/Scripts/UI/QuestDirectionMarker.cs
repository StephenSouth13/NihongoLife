using NihongoLife.Scenario;
using UnityEngine;

namespace NihongoLife.UI
{
    public class QuestDirectionMarker : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float markerHeight = 2.9f;
        [SerializeField] private float arrowDistance = 2.1f;

        private Transform _target;
        private GameObject _marker;
        private GameObject _arrow;

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
            EnsureVisuals();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (_target == null || player == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            _marker.transform.position = _target.position + Vector3.up * markerHeight;
            _marker.transform.Rotate(Vector3.up, 80f * Time.deltaTime, Space.World);

            Vector3 direction = _target.position - player.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.01f) return;

            _arrow.transform.position = player.position + Vector3.up * 1.4f + direction.normalized * arrowDistance;
            _arrow.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        }

        public void ShowCurrentObjectiveTarget()
        {
            _target = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentTargetTransform : null;
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
                _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _marker.name = "QuestWorldMarker";
                _marker.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                _marker.GetComponent<Renderer>().sharedMaterial = CreateMaterial("QuestMarkerMat", new Color(1f, 0.72f, 0.16f, 1f));
                Destroy(_marker.GetComponent<Collider>());
            }

            if (_arrow == null)
            {
                _arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _arrow.name = "QuestDirectionArrow";
                _arrow.transform.localScale = new Vector3(0.18f, 0.55f, 0.18f);
                _arrow.GetComponent<Renderer>().sharedMaterial = CreateMaterial("QuestArrowMat", new Color(0.2f, 0.85f, 1f, 1f));
                Destroy(_arrow.GetComponent<Collider>());
            }
        }

        private void SetVisible(bool visible)
        {
            if (_marker != null) _marker.SetActive(visible);
            if (_arrow != null) _arrow.SetActive(visible);
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
