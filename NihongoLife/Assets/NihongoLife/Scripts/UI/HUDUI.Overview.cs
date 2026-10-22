using System.Linq;
using NihongoLife.Save;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>Partial of HUDUI: overhead camera controls. The MonoBehaviour entry point stays HUDUI.cs.</summary>
    public partial class HUDUI : MonoBehaviour
    {
        // ─────────── Overview camera controls (Sims-style UCP: rotate ◀ ▶, zoom + -) ───────────

        private RectTransform _overviewPanel;
        private NihongoLife.Cameras.ThirdPersonCameraController _overviewCamera;

        private void ToggleOverviewCamera()
        {
            if (_overviewCamera == null) _overviewCamera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (_overviewCamera == null) return;
            if (!_overviewCamera.SetOverview(!_overviewCamera.OverviewMode)) HudFeed.Post(NihongoLife.Core.Loc.T("Góc nhìn trên cao chỉ dùng ngoài trời.", "The overhead view only works outdoors.", "上からの視点は屋外だけで使えます。"), HudFeed.Kind.Info, 3f, "overview");
        }

        private void UpdateOverviewControls()
        {
            if (_overviewCamera == null || !_overviewCamera.isActiveAndEnabled) _overviewCamera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
            bool show = _overviewCamera != null && _overviewCamera.OverviewMode && !UiModalStack.AnyOpen;
            if (show && _overviewPanel == null)
            {
                var font = NLUi.ResolveFont();
                _overviewPanel = NLUi.Panel(transform, "OverviewControls", new Color(0.04f, 0.05f, 0.07f, 0.82f), new RectOffset(10, 10, 10, 10), 8f, vertical: false);
                NLUi.Anchor(_overviewPanel, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(0f, 0f));
                _overviewPanel.pivot = new Vector2(1f, 0.5f);
                NLUi.FitContent(_overviewPanel, width: true, height: true);
                void Add(string name, string label, System.Action click)
                {
                    var b = NLUi.Button(_overviewPanel, name, label, font, () => click(), new Color(0.16f, 0.2f, 0.26f), 22f, Color.white, 48f);
                    NLUi.Size(b, 52f, 48f);
                }
                Add("RotateLeft", "◀", () => _overviewCamera?.RotateOverview(-1));
                Add("RotateRight", "▶", () => _overviewCamera?.RotateOverview(1));
                Add("ZoomIn", "+", () => _overviewCamera?.ZoomOverview(-1));
                Add("ZoomOut", "-", () => _overviewCamera?.ZoomOverview(1));
                Add("Exit", "H", () => _overviewCamera?.SetOverview(false));
            }
            if (_overviewPanel != null && _overviewPanel.gameObject.activeSelf != show) _overviewPanel.gameObject.SetActive(show);
        }
    }
}
