using System;
using NihongoLife.Core;
using TMPro;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// A baked direction signpost (RouteSignBuilder). Every board keeps the text on its front and back face in the interface language
    /// (Japanese first, then Vietnamese / English / Russian), with an arrow for each viewer and the distance to the target.
    /// The labels come from Resources/Navigation/route_signs.json, so the sign follows a language change.
    /// </summary>
    public sealed class RouteSignPost : MonoBehaviour
    {
        [Serializable]
        public sealed class BoardView
        {
            public string id;
            public Vector3 target;
            public TextMeshPro front;
            public TextMeshPro back;
        }

        private const string Arrows = "\u2191\u2197\u2192\u2198\u2193\u2199\u2190\u2196";

        [SerializeField] private string sceneName;
        [SerializeField] private string postId;
        [SerializeField] private BoardView[] boards = Array.Empty<BoardView>();

        public string PostId => postId;
        public BoardView[] Boards => boards;

        public void Configure(string scene, string id, BoardView[] views)
        {
            sceneName = scene; postId = id; boards = views;
        }

        private void Awake()
        {
            Refresh();
            Loc.Watch(this, Refresh);
        }

        /// <summary>The line on a board's front (read by someone walking along the post's forward) or back face.</summary>
        public string Line(int index, bool front)
        {
            var view = boards[index];
            var data = RouteSignCatalog.Board(sceneName, postId, view.id);
            string label = data != null && data.label != null ? data.label.Bilingual : view.id;
            Vector3 delta = view.target - transform.position;
            float meters = new Vector2(delta.x, delta.z).magnitude;
            return $"{Arrow(delta, front ? 0f : 180f)}  {label}  <size=75%>{meters:0}m</size>";
        }

        public void Refresh()
        {
            for (int i = 0; i < boards.Length; i++)
            {
                var view = boards[i];
                if (view.front != null) Apply(view.front, Line(i, true));
                if (view.back != null) Apply(view.back, Line(i, false));
            }
        }

        private static void Apply(TextMeshPro text, string value)
        {
            if (text.enableAutoSizing == false)
            {
                // The board is a fixed width: long Vietnamese or Russian labels shrink instead of running off the board.
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = text.fontSize * 0.45f;
                text.enableAutoSizing = true;
            }
            text.text = value;
        }

        /// <summary>Arrow for a viewer who faces the post's forward rotated by viewerYaw (0 = along the post's +Z, 180 = the opposite way).</summary>
        private string Arrow(Vector3 delta, float viewerYaw)
        {
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg - transform.eulerAngles.y - viewerYaw;
            int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
            return Arrows[index].ToString();
        }
    }
}
