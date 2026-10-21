using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// "A new place is open" — the moment a stage of the map opens (LocationUnlocks.Unlocked): the guide photo of the
    /// place, its name, why it opened (the story line of the stage), the story quest that leads there, and buttons to
    /// open the map or carry on. Unlocks that happen during a dialogue, the result card or another window wait in a
    /// queue and show one after the other once the screen is free. Plays the unlock jingle.
    /// </summary>
    public sealed class MapUnlockCardUI : MonoBehaviour
    {
        private static MapUnlockCardUI _host;
        private static readonly Queue<LocationUnlock> Pending = new();
        public static MapUnlockCardUI Open { get; private set; }
        public static int QueueCount => Pending.Count;
        public LocationUnlock Stage { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            LocationUnlocks.Unlocked -= Enqueue;
            LocationUnlocks.Unlocked += Enqueue;
            if (_host != null) return;
            var go = new GameObject("MapUnlockHost");
            DontDestroyOnLoad(go);
            _host = go.AddComponent<MapUnlockCardUI>();
        }

        private static void Enqueue(LocationUnlock stage)
        {
            if (stage != null) Pending.Enqueue(stage);
        }

        /// <summary>Tests: show a stage's card right away.</summary>
        public static void ShowNow(LocationUnlock stage)
        {
            if (Open != null) Close();
            Show(stage);
        }

        public static void Close()
        {
            if (Open == null) return;
            Destroy(Open.gameObject);
            Open = null;
        }

        private void Update()
        {
            if (this != _host || Open != null || Pending.Count == 0) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "01_MainMenu" || scene == "00_Bootstrap") return;
            bool busy = UiModalStack.AnyOpen
                || (NihongoLife.Dialogue.DialogueManager.Instance != null && NihongoLife.Dialogue.DialogueManager.Instance.IsOpen)
                || (FindFirstObjectByType<ResultUI>() is ResultUI result && result.isActiveAndEnabled)
                || FreePlayCardUI.Open != null;
            if (!busy) Show(Pending.Dequeue());
        }

        private static void Show(LocationUnlock stage)
        {
            var canvas = NLUi.CreateCanvas("MapUnlockCard", 330);
            Open = canvas.gameObject.AddComponent<MapUnlockCardUI>();
            Open.Stage = stage;
            Open.Build(canvas.transform, stage);
            UiModalStack.Register(Open, () => Open != null, Close, "Map unlock");
            NihongoLife.Audio.Soundscape.Jingle("unlock");
            HudFeed.Post(Loc.F("Đã mở: {0}", "Now open: {0}", "解放：{0}", WorldLocationCatalog.Get(stage.sceneName).DisplayName), HudFeed.Kind.Reward, 5f, "unlock");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy() { if (Open == this) Open = null; }

        private void Build(Transform root, LocationUnlock stage)
        {
            var dim = UiKit.Image(root, "Dim", UiTheme.Backdrop, null, true);
            UiKit.Fill(dim.rectTransform);
            var card = UiKit.Card(root, "Card", UiTheme.Ink, 26, 0.5f).rectTransform;
            UiKit.Pin(card, new Vector2(0.5f, 0.5f), 0f, 0f, 980f, 560f);
            UiKit.Border(card, UiKit.Alpha(UiTheme.Gold, 0.45f), 26);

            // Photo of the place on the left.
            var photoFrame = UiKit.Card(card, "PhotoFrame", UiTheme.Sunken, 18).rectTransform;
            UiKit.Place(photoFrame, 34f, 34f, 430f, 492f);
            var tex = Resources.Load<Texture2D>("Guide/" + stage.guideKey);
            var photo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            photo.transform.SetParent(photoFrame, false);
            photo.texture = tex;
            photo.color = tex != null ? Color.white : new Color(0.15f, 0.18f, 0.22f);
            photo.raycastTarget = false;
            UiKit.Fill(photo.rectTransform, 8f, 8f, 8f, 8f);
            if (tex != null)
            {
                // Cover the frame without stretching.
                float frameAspect = 414f / 476f, texAspect = tex.width / (float)tex.height;
                photo.uvRect = texAspect > frameAspect ? new Rect((1f - frameAspect / texAspect) / 2f, 0f, frameAspect / texAspect, 1f) : new Rect(0f, (1f - texAspect / frameAspect) / 2f, 1f, texAspect / frameAspect);
            }

            float x = 494f, w = 452f;
            UiKit.Place(UiKit.Label(card, "Kicker", "KHU VỰC MỚI ĐÃ MỞ", "A NEW PLACE IS OPEN", "新しい場所が開きました", 15f, UiTheme.Gold, FontStyles.Bold), x, 42f, w, 22f);
            var place = WorldLocationCatalog.Get(stage.sceneName);
            UiKit.Place(UiKit.Label(card, "PlaceJa", place.JapaneseName, 40f, UiTheme.Text, FontStyles.Bold), x, 70f, w, 56f);
            if (!Loc.IsJapanese) UiKit.Place(UiKit.Label(card, "Place", place.DisplayName, 19f, UiTheme.TextSoft), x, 126f, w, 28f);
            var stageTitle = UiKit.Label(card, "Stage", LocationUnlocks.StageTitle(stage), 17f, UiTheme.Teal, FontStyles.Bold);
            UiKit.Place(stageTitle, x, 166f, w, 26f);
            var story = UiKit.Label(card, "Story", LocationUnlocks.StageStory(stage), 17f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(story, x, 198f, w, 110f);

            if (!string.IsNullOrEmpty(stage.gatewayScenarioId))
            {
                var quest = UiKit.Card(card, "Quest", UiTheme.Surface, 14).rectTransform;
                UiKit.Place(quest, x, 318f, w, 92f);
                UiKit.Pin(UiKit.Icon(quest, "menu_book", UiTheme.Gold, 26f).rectTransform, new Vector2(0f, 0.5f), 18f, 0f, 26f, 26f);
                UiKit.Place(UiKit.Label(quest, "QuestKicker", "NHIỆM VỤ CỐT TRUYỆN MỚI", "NEW STORY QUEST", "新しいストーリー", 13f, UiTheme.Gold, FontStyles.Bold), 58f, 14f, w - 70f, 20f);
                var title = UiKit.Label(quest, "QuestTitle", LocationUnlocks.QuestTitle(stage.gatewayScenarioId), 18f, UiTheme.Text, FontStyles.Bold);
                title.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Place(title, 58f, 36f, w - 70f, 26f);
                UiKit.Place(UiKit.Label(quest, "QuestHint", "Bắt đầu trong Sổ nhiệm vụ (N)", "Start it from the task journal (N)", "タスク帳（N）から始めましょう", 13f, UiTheme.Muted), 58f, 62f, w - 70f, 20f);
            }

            var map = UiKit.Button(card, "OpenMap", "Xem bản đồ", "Open the map", "地図を見る", () =>
            {
                Close();
                var mapUi = FindFirstObjectByType<WorldMapUI>();
                if (mapUi != null) mapUi.SetVisible(true);
            }, UiKit.Kind.Secondary, "map", 16f);
            UiKit.Pin((RectTransform)map.transform, new Vector2(1f, 0f), -260f, 36f, 200f, 52f);
            var ok = UiKit.Button(card, "Continue", "Tuyệt vời!", "Great!", "やった！", Close, UiKit.Kind.Primary, "check_circle", 17f);
            UiKit.Pin((RectTransform)ok.transform, new Vector2(1f, 0f), -34f, 36f, 210f, 52f);
        }
    }
}
