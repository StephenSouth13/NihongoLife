using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.UI
{
    /// <summary>
    /// Turn-by-turn hint to a place picked on the map: a HUD pill (arrow turning with the camera, name,
    /// distance) and a bobbing gold arrow on the ground in front of the player. Clears itself on arrival,
    /// when another scene becomes active, or when a new target is chosen.
    /// </summary>
    public sealed class WaypointGuide : MonoBehaviour
    {
        private const float ArriveDistance = 3.5f;
        private static WaypointGuide _instance;

        private RectTransform _pill;
        private RectTransform _arrow;
        private TextMeshProUGUI _label;
        private TextMeshProUGUI _distance;
        private Transform _groundArrow;
        private TextMeshPro _groundText;
        private Transform _player;
        private string _scene;

        public static bool IsActive => _instance != null && _instance.Active;
        public static string TargetLabel => _instance != null ? _instance.Label : null;
        public static Vector3 TargetPosition => _instance != null ? _instance.Target : Vector3.zero;

        public bool Active { get; private set; }
        public string Label { get; private set; }
        public Vector3 Target { get; private set; }

        public static void Show(string label, Vector3 worldTarget)
        {
            if (_instance == null)
            {
                var host = new GameObject("WaypointGuide");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<WaypointGuide>();
                _instance.Build();
            }
            _instance.SetTarget(label, worldTarget);
        }

        public static void Clear()
        {
            if (_instance != null) _instance.SetActive(false);
        }

        private void Build()
        {
            var font = NLUi.ResolveFont();
            var canvas = NLUi.CreateCanvas("WaypointCanvas", 96, transform);
            _pill = NLUi.Panel(canvas.transform, "Waypoint", NLUi.Ink, new RectOffset(18, 22, 8, 8), 12f, vertical: false);
            ((UnityEngine.UI.HorizontalLayoutGroup)_pill.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.Anchor(_pill, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(0f, 52f));
            NLUi.FitContent(_pill, width: true, height: false);
            var arrowHolder = new GameObject("ArrowHolder", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement)).GetComponent<RectTransform>();
            arrowHolder.SetParent(_pill, false);
            var le = arrowHolder.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = 34f; le.preferredHeight = 34f;
            var arrow = NLUi.Label(arrowHolder, "Arrow", "▲", 26f, NLUi.Gold, font, FontStyles.Bold, TextAlignmentOptions.Center);
            _arrow = arrow.rectTransform;
            NLUi.Stretch(_arrow);
            _label = NLUi.Label(_pill, "Label", "", 19f, NLUi.Text, font, FontStyles.Bold, TextAlignmentOptions.Left);
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _distance = NLUi.Label(_pill, "Distance", "", 18f, NLUi.Gold, font, FontStyles.Bold, TextAlignmentOptions.Right);
            _distance.textWrappingMode = TextWrappingModes.NoWrap;

            // TextMeshPro swaps the Transform for a RectTransform, so take the transform after adding it.
            _groundText = new GameObject("GroundArrow").AddComponent<TextMeshPro>();
            _groundArrow = _groundText.transform;
            _groundArrow.SetParent(transform, false);
            _groundText.text = "▲";
            _groundText.fontSize = 9f;
            _groundText.alignment = TextAlignmentOptions.Center;
            _groundText.color = new Color(1f, 0.78f, 0.25f, 0.9f);
            _groundText.rectTransform.sizeDelta = new Vector2(2f, 2f);
            SetActive(false);
        }

        private void SetTarget(string label, Vector3 worldTarget)
        {
            Label = label;
            Target = worldTarget;
            _scene = SceneManager.GetActiveScene().name;
            _label.text = label;
            SetActive(true);
        }

        private void SetActive(bool active)
        {
            Active = active;
            if (_pill != null) _pill.gameObject.SetActive(active);
            if (_groundArrow != null) _groundArrow.gameObject.SetActive(active);
        }

        private void LateUpdate()
        {
            if (!Active) return;
            if (SceneManager.GetActiveScene().name != _scene) { SetActive(false); return; }
            if (_player == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                _player = player.transform;
            }
            Vector3 to = Target - _player.position; to.y = 0f;
            float distance = to.magnitude;
            _distance.text = $"{distance:0} m";
            if (distance < ArriveDistance)
            {
                SetActive(false);
                return;
            }
            var camera = Camera.main;
            Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up) : _player.forward;
            float angle = Vector3.SignedAngle(forward, to, Vector3.up);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);

            Vector3 dir = to / distance;
            _groundArrow.position = _player.position + dir * 1.4f + Vector3.up * (0.08f + Mathf.Sin(Time.time * 4f) * 0.04f);
            _groundArrow.rotation = Quaternion.LookRotation(Vector3.down, dir);
        }
    }
}
