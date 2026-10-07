using System;
using System.Collections;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Scoring;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// Runs one mini-game at a time on top of the normal world (no scene reload):
    /// lock the player and remember where they stood → move the camera to the machine → start the
    /// IMiniGame in a full-screen overlay → show the result → submit it to the existing ScoringManager,
    /// LearningMasteryManager and PlayerStatus → close → restore camera and PlayerController.
    /// </summary>
    public sealed class MiniGameController : MonoBehaviour
    {
        public static MiniGameController Instance { get; private set; }
        public static event Action<MiniGameResult> ResultSubmitted;

        private Canvas _canvas;
        private RectTransform _gameRoot;
        private RectTransform _resultPanel;
        private UnityEngine.UI.Button _closeButton;
        private TextMeshProUGUI _resultTitle;
        private TextMeshProUGUI _resultBody;
        private TMP_FontAsset _font;
        private IMiniGame _game;
        private MonoBehaviour _gameBehaviour;
        private MiniGameLauncher _launcher;
        private PlayerController _player;
        private ThirdPersonCameraController _followCamera;
        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private Vector3 _savedCameraPosition;
        private Quaternion _savedCameraRotation;

        public bool IsRunning => _game != null || (_resultPanel != null && _resultPanel.gameObject.activeSelf);
        public bool IsShowingResult => _resultPanel != null && _resultPanel.gameObject.activeSelf;
        public IMiniGame CurrentGame => _game;
        public MiniGameResult LastResult { get; private set; }

        public static MiniGameController GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("MiniGameController");
            return go.AddComponent<MiniGameController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _font = NLUi.ResolveFont();
            var hud = FindFirstObjectByType<HUDUI>();
            _canvas = NLUi.CreateCanvas("MiniGameCanvas", 800, hud != null ? hud.transform : transform);
            var dim = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            dim.transform.SetParent(_canvas.transform, false);
            NLUi.Stretch(dim.rectTransform);
            dim.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);
            _gameRoot = new GameObject("GameRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _gameRoot.SetParent(_canvas.transform, false);
            NLUi.Stretch(_gameRoot);
            BuildResultPanel();
            _closeButton = NLUi.CloseButton(_canvas.transform, _font, CloseOrAbort, 56f, 22f);
            _canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        // ─────────── Launch ───────────

        public bool Launch(MiniGameLauncher launcher, GameObject playerObject)
        {
            if (IsRunning || launcher == null || launcher.Definition == null) return false;
            var game = CreateGame(launcher.Definition.kind);
            if (game == null) return false;

            _launcher = launcher;
            _player = playerObject != null ? playerObject.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                _savedPosition = _player.transform.position;
                _savedRotation = _player.transform.rotation;
                _player.InputLocked = true;
            }
            ScenarioManager.Instance?.SetPlayerInputLocked(true);
            FocusCamera(launcher.ViewPoint);

            _canvas.gameObject.SetActive(true);
            _canvas.transform.SetAsLastSibling();
            _resultPanel.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _game = game;
            _game.Finished += HandleFinished;
            _game.Begin(launcher.Definition, _gameRoot);
            return true;
        }

        private IMiniGame CreateGame(MiniGameKind kind)
        {
            foreach (Transform child in _gameRoot) Destroy(child.gameObject);
            if (_gameBehaviour != null) Destroy(_gameBehaviour);
            switch (kind)
            {
                case MiniGameKind.KanaMatch:
                    var kana = gameObject.AddComponent<KanaMatchGame>();
                    _gameBehaviour = kana;
                    return kana;
                default:
                    return null; // Word Shooter / Order Rush: definitions exist, gameplay comes next
            }
        }

        /// <summary>The "×" button: quits the running game (counts as an abort) or closes the result card.</summary>
        public void CloseOrAbort()
        {
            if (IsShowingResult) { Close(); return; }
            _game?.Abort();
        }

        private void Update()
        {
            if (_closeButton != null && _closeButton.transform.GetSiblingIndex() != _closeButton.transform.parent.childCount - 1)
                _closeButton.transform.SetAsLastSibling();
            if (_game == null || IsShowingResult) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) _game.Abort();
        }

        // ─────────── Result ───────────

        private void HandleFinished(MiniGameResult result)
        {
            if (_game != null) _game.Finished -= HandleFinished;
            _game = null;
            LastResult = result;
            if (result.completed) Submit(result);
            ShowResult(result);
        }

        /// <summary>Hands a result to the systems Nihongo Life already persists (score events, mastery
        /// records in the progress repository, player EXP/knowledge). No mini-game keeps its own save.</summary>
        public static void Submit(MiniGameResult result)
        {
            string source = "minigame." + result.gameId;
            var scoring = ScoringManager.Instance;
            if (scoring != null)
            {
                scoring.AddScore("Vocabulary", result.correctCount * 5, $"{result.gameId}: {result.correctCount} đúng", source);
                scoring.AddScore("ResponseAccuracy", Mathf.RoundToInt(result.accuracy * 100f), $"{result.gameId}: chính xác {result.accuracy:P0}", source);
                scoring.AddScore("TaskCompletion", result.completed ? 100 : 0, $"{result.gameId}: hoàn thành trong {result.completionSeconds:0}s", source);
                if (result.incorrectCount > 0)
                    scoring.AddScore("Vocabulary", -2 * result.incorrectCount, $"{result.gameId}: {result.incorrectCount} lần sai", source);
                result.submittedToScoring = true;
            }

            var mastery = LearningMasteryManager.Instance;
            if (mastery != null && GameServices.Get<NihongoLife.Save.IProgressRepository>() != null)
            {
                foreach (string target in result.learningTargetIds)
                    mastery.RegisterUsage(target, result.masteredTargetIds.Contains(target));
                result.submittedToMastery = true;
            }

            var status = PlayerStatus.Instance;
            if (status != null)
            {
                status.AddExp(result.expReward);
                status.AddKnowledge(result.knowledgeReward);
            }
            ResultSubmitted?.Invoke(result);
        }

        private void BuildResultPanel()
        {
            _resultPanel = NLUi.Panel(_canvas.transform, "ResultPanel", NLUi.Ink, new RectOffset(40, 40, 30, 30), 14f);
            NLUi.Anchor(_resultPanel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 0f));
            NLUi.FitContent(_resultPanel);
            _resultTitle = NLUi.Label(_resultPanel, "Title", "", 40f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Divider(_resultPanel);
            _resultBody = NLUi.Label(_resultPanel, "Body", "", 21f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Left);
            NLUi.Button(_resultPanel, "Continue", "つづける · Tiếp tục khám phá", _font, Close, new Color(0.16f, 0.45f, 0.32f), 22f, null, 58f);
            _resultPanel.gameObject.SetActive(false);
        }

        private void ShowResult(MiniGameResult result)
        {
            _resultPanel.gameObject.SetActive(true);
            _resultPanel.SetAsLastSibling();
            string stars = result.completed ? $"\n<size=80%><color=#F2B233>{new string('★', result.stars)}</color><color=#3A4250>{new string('★', 3 - result.stars)}</color></size>" : string.Empty;
            _resultTitle.text = (result.completed ? "クリア！ <size=60%><color=#E8EEF6>Hoàn thành</color></size>" : "ちゅうだん <size=60%><color=#E8EEF6>Đã dừng</color></size>") + stars;
            var body = new System.Text.StringBuilder();
            body.AppendLine($"<color=#A8B4C4>Điểm</color>  <b>{result.score}</b>      <color=#A8B4C4>Chính xác</color>  <b>{result.accuracy:P0}</b>      <color=#A8B4C4>Thời gian</color>  <b>{result.completionSeconds:0}s</b>");
            body.AppendLine($"<color=#A8B4C4>Đúng</color>  {result.correctCount}      <color=#A8B4C4>Sai</color>  {result.incorrectCount}");
            if (result.completed)
            {
                body.AppendLine($"<color=#7BD88F>+{result.expReward} EXP   +{result.knowledgeReward} Kiến thức   · đã ghi vào hồ sơ học tập</color>");
                body.AppendLine($"<color=#F2B233>チケット +{result.tickets}</color>  <color=#A8B4C4>(đổi quà ở quầy けいひん)</color>   ·   <color=#A8B4C4>Combo cao nhất</color> x{result.bestCombo}" +
                                (result.newRecord ? "   <color=#FF7FB0><b>きろく こうしん！ Kỷ lục mới</b></color>" : string.Empty));
            }
            if (result.mistakes.Count > 0)
            {
                body.AppendLine("\n<color=#F2B233>Cần ôn lại</color>");
                foreach (var mistake in result.mistakes.GetRange(0, Mathf.Min(4, result.mistakes.Count)))
                    body.AppendLine("  · " + mistake.note);
            }
            _resultBody.text = body.ToString();
        }

        public void Close()
        {
            if (_game != null) { _game.Finished -= HandleFinished; _game.Abort(); _game = null; }
            foreach (Transform child in _gameRoot) Destroy(child.gameObject);
            if (_gameBehaviour != null) { Destroy(_gameBehaviour); _gameBehaviour = null; }
            _resultPanel.gameObject.SetActive(false);
            _canvas.gameObject.SetActive(false);
            RestoreCamera();
            if (_player != null)
            {
                var body = _player.GetComponent<CharacterController>();
                if (body != null) body.enabled = false;
                _player.transform.SetPositionAndRotation(_savedPosition, _savedRotation);
                if (body != null) body.enabled = true;
                _player.InputLocked = false;
            }
            ScenarioManager.Instance?.SetPlayerInputLocked(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _launcher = null;
        }

        // ─────────── Camera ───────────

        private void FocusCamera(Transform viewPoint)
        {
            var camera = Camera.main;
            if (camera == null) return;
            _followCamera = camera.GetComponent<ThirdPersonCameraController>();
            _savedCameraPosition = camera.transform.position;
            _savedCameraRotation = camera.transform.rotation;
            if (_followCamera != null) _followCamera.enabled = false;
            if (viewPoint != null) StartCoroutine(MoveCamera(camera.transform, viewPoint.position, viewPoint.rotation));
        }

        private static IEnumerator MoveCamera(Transform camera, Vector3 position, Quaternion rotation)
        {
            Vector3 fromP = camera.position;
            Quaternion fromR = camera.rotation;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                camera.SetPositionAndRotation(Vector3.Lerp(fromP, position, k), Quaternion.Slerp(fromR, rotation, k));
                yield return null;
            }
            camera.SetPositionAndRotation(position, rotation);
        }

        private void RestoreCamera()
        {
            StopAllCoroutines();
            var camera = Camera.main;
            if (camera != null)
                camera.transform.SetPositionAndRotation(_savedCameraPosition, _savedCameraRotation);
            if (_followCamera != null) _followCamera.enabled = true;
            _followCamera = null;
        }
    }
}
