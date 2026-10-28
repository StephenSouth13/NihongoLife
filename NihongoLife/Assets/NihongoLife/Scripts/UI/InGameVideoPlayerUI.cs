using System.Collections;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

namespace NihongoLife.UI
{
    /// <summary>
    /// In-game video page: a list of channel videos (Resources/Video/nihongolife_videos.json) and a player built on
    /// Unity's VideoPlayer rendering into a RawImage. Direct mp4/HLS URLs and StreamingAssets files play inside the
    /// game (play/pause, seek, volume, fullscreen); YouTube-only entries show thumbnail, title, summary and transcript
    /// with an "Open on YouTube" fallback, because VideoPlayer cannot play YouTube pages. Bad URLs, offline and
    /// timeouts end in a clear error card with Retry — never an exception.
    /// </summary>
    public sealed class InGameVideoPlayerUI : MenuPopupBase
    {
        private const float W = 1380f, H = 870f, ListW = 380f;
        private const float PW = 896f, PH = 504f;

        private readonly VideoPlaybackStateMachine _sm = new VideoPlaybackStateMachine();
        private VideoEntry _entry;
        private RectTransform _list, _stage, _controls, _info;
        private RawImage _screen;
        private Image _thumbBg;
        private TextMeshProUGUI _overlay, _time, _title, _summary, _transcript;
        private Button _playBtn, _retryBtn, _externalBtn;
        private Slider _seek, _volume;
        private VideoPlayer _player;
        private RenderTexture _rt;
        private Texture2D _thumb;
        private bool _fullscreen, _seeking;
        private Vector2 _stagePos;
        private Coroutine _thumbJob;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        public VideoPlaybackStateMachine StateMachine => _sm;
        public VideoEntry Current => _entry;

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Video bài học"; en = "Video library"; ja = "動画ライブラリ"; }

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle", "Video từ kênh YouTube @NihongoLifeGame — xem ngay trong game.",
                "Videos from our YouTube channel @NihongoLifeGame — watch without leaving the game.",
                "YouTubeチャンネル @NihongoLifeGame の動画をゲーム内で。", 15f, UiTheme.Muted);
            UiKit.Place(sub.rectTransform, 36f, 60f, W - 160f, 24f);

            var listBg = UiKit.Card(card, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 32f, 108f, ListW, H - 140f);
            _list = UiKit.Scroll(listBg.transform, "List", out _, 10f, new RectOffset(10, 10, 10, 10));
            UiKit.Fill((RectTransform)_list.parent.parent);

            float x = 32f + ListW + 24f;
            _stage = UiKit.Card(card, "Stage", Color.black, 16, 0f, true).rectTransform;
            _stagePos = new Vector2(x, 108f);
            UiKit.Place(_stage, x, 108f, PW, PH);
            _thumbBg = UiKit.Image(_stage, "Thumb", new Color(1f, 1f, 1f, 0f));
            UiKit.Fill(_thumbBg.rectTransform);
            _thumbBg.preserveAspect = true;
            var screenGo = new GameObject("Screen", typeof(RectTransform), typeof(RawImage));
            screenGo.transform.SetParent(_stage, false);
            _screen = screenGo.GetComponent<RawImage>();
            _screen.color = Color.white; _screen.raycastTarget = false; _screen.enabled = false;
            UiKit.Fill(_screen.rectTransform);
            _overlay = UiKit.Label(_stage, "Overlay", "", 20f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UiKit.Fill(_overlay.rectTransform, 40f, 40f, 40f, 110f);
            _retryBtn = UiKit.Button(_stage, "Retry", "Thử lại", "Retry", "再試行", Retry, UiKit.Kind.Primary, "refresh");
            UiKit.Pin((RectTransform)_retryBtn.transform, new Vector2(0.5f, 0f), -120f, 40f, 200f, 50f);
            _externalBtn = UiKit.Button(_stage, "External", "Mở trên YouTube", "Open on YouTube", "YouTubeで開く", OpenExternal, UiKit.Kind.Accent, "public");
            UiKit.Pin((RectTransform)_externalBtn.transform, new Vector2(0.5f, 0f), 120f, 40f, 240f, 50f);

            _controls = UiKit.Card(card, "Controls", UiTheme.Surface, 14, 0f, true).rectTransform;
            UiKit.Place(_controls, x, 108f + PH + 10f, PW, 58f);
            _playBtn = UiKit.IconButton(_controls, "Play", "play_arrow", TogglePause, UiKit.Kind.Primary);
            UiKit.Place((RectTransform)_playBtn.transform, 8f, 7f, 44f, 44f);
            _time = UiKit.Label(_controls, "Time", "0:00 / 0:00", 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Place(_time.rectTransform, 58f, 7f, 120f, 44f);
            _seek = MakeSlider(_controls, "Seek", 186f, PW - 186f - 300f);
            _seek.onValueChanged.AddListener(OnSeekChanged);
            var vol = UiKit.IconButton(_controls, "Mute", "volume_up", ToggleMute, UiKit.Kind.Ghost);
            UiKit.Place((RectTransform)vol.transform, PW - 290f, 7f, 44f, 44f);
            _volume = MakeSlider(_controls, "Volume", PW - 240f, 120f);
            _volume.value = PlayerPrefs.GetFloat("NihongoLife.VideoVolume", 0.8f);
            _volume.onValueChanged.AddListener(OnVolumeChanged);
            var full = UiKit.IconButton(_controls, "Fullscreen", "visibility", ToggleFullscreen, UiKit.Kind.Ghost);
            UiKit.Place((RectTransform)full.transform, PW - 108f, 7f, 44f, 44f);
            var ext = UiKit.IconButton(_controls, "OpenExternal", "public", OpenExternal, UiKit.Kind.Ghost);
            UiKit.Place((RectTransform)ext.transform, PW - 56f, 7f, 44f, 44f);

            _info = UiKit.Card(card, "Info", UiTheme.Surface, 16, 0f, false).rectTransform;
            float infoY = 108f + PH + 80f;
            UiKit.Place(_info, x, infoY, PW, H - infoY - 32f);
            _title = UiKit.Label(_info, "Title", "", 19f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(_title.rectTransform, 18f, 12f, PW - 36f, 50f);
            var scrollContent = UiKit.Scroll(_info, "TextScroll", out _, 6f, new RectOffset(0, 0, 0, 0));
            var scrollRoot = (RectTransform)scrollContent.parent.parent;
            UiKit.Fill(scrollRoot, 18f, 64f, 18f, 10f);
            _summary = UiKit.Label(scrollContent, "Summary", "", 14.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            _summary.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _transcript = UiKit.Label(scrollContent, "Transcript", "", 14f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            _transcript.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _sm.Changed += _ => RefreshChrome();
            RefreshChrome();
        }

        private Slider MakeSlider(RectTransform parent, string name, float x, float width)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            UiKit.Place(rt, x, 19f, width, 20f);
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f;
            foreach (var img in go.GetComponentsInChildren<Image>(true))
            {
                img.sprite = UiKit.Rounded(6); img.type = Image.Type.Sliced;
                img.color = img.name == "Fill" ? UiTheme.Teal : img.name == "Handle" ? UiTheme.Text : UiTheme.Sunken;
            }
            return slider;
        }

        protected override void OnOpened()
        {
            DrawList();
            if (_entry == null && VideoCatalog.All.Count > 0) Select(VideoCatalog.All[0].id);
            else RefreshInfo();
        }

        protected override void OnClosed() => Stop();
        protected override void OnDestroyed() { Stop(); if (_thumb != null) Destroy(_thumb); }
        protected override void OnLanguageApplied() { if (IsOpen) { DrawList(); RefreshInfo(); RefreshChrome(); } }

        protected override bool HandleEscape()
        {
            if (!_fullscreen) return false;
            ToggleFullscreen();
            return true;
        }

        private void DrawList()
        {
            if (_list == null) return;
            UiKit.Clear(_list);
            var store = LearningProgressStore.Shared;
            if (VideoCatalog.All.Count == 0)
            {
                var empty = UiKit.Label(_list, "Empty", Loc.T("Chưa có video.", "No videos yet.", "動画はまだありません。"), 15f, UiTheme.Muted);
                UiKit.Size(empty, -1f, 40f);
                return;
            }
            foreach (var v in VideoCatalog.All)
            {
                var video = v;
                bool on = _entry != null && _entry.id == v.id;
                var row = UiKit.Card(_list, "Video_" + v.id, on ? UiTheme.Raised : UiTheme.Surface, 14);
                UiKit.Size(row, -1f, 92f);
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = row;
                btn.onClick.AddListener(() => Select(video.id));
                if (on) { var bar = UiKit.Image(row.transform, "Sel", UiTheme.Teal, UiKit.Rounded(3)); UiKit.Place(bar.rectTransform, 0f, 16f, 5f, 60f); }
                var kind = VideoCatalog.Classify(v);
                var ic = UiKit.Icon(row.transform, kind == VideoSourceKind.YouTubeOnly ? "public" : "play_circle", on ? UiTheme.Teal : UiTheme.Muted, 30f);
                UiKit.Place(ic.rectTransform, 14f, 14f, 30f, 30f);
                var t = UiKit.Label(row.transform, "Title", VideoCatalog.Title(v), 14.5f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
                UiKit.Place(t.rectTransform, 54f, 10f, ListW - 90f, 46f);
                string meta = (kind == VideoSourceKind.YouTubeOnly ? "YouTube" : Loc.T("Phát trong game", "Plays in-game", "ゲーム内再生"))
                              + (v.durationSeconds > 0 ? " · " + VideoPlaybackStateMachine.FormatTime(v.durationSeconds) : "")
                              + (store.IsVideoWatched(v.id) ? "  · " + Loc.T("Đã xem", "Watched", "視聴済み") : "");
                var m = UiKit.Label(row.transform, "Meta", meta, 12.5f, store.IsVideoWatched(v.id) ? UiTheme.Good : UiTheme.Muted);
                UiKit.Place(m.rectTransform, 54f, 60f, ListW - 90f, 22f);
            }
        }

        public void Select(string id)
        {
            var v = VideoCatalog.Find(id);
            if (v == null) return;
            Stop();
            _entry = v;
            DrawList();
            RefreshInfo();
            Load();
        }

        private void Load()
        {
            if (_entry == null) return;
            bool online = Application.internetReachability != NetworkReachability.NotReachable;
            var kind = _sm.Load(_entry, online);
            LoadThumbnail();
            if (_sm.State != VideoPlaybackState.Preparing) return;
            string url = VideoCatalog.ResolvePlayableUrl(_entry, Application.streamingAssetsPath);
            EnsurePlayer();
            int gen = _sm.Generation;
            try
            {
                _player.source = VideoSource.Url;
                _player.url = url;
                _player.Prepare();
            }
            catch (System.Exception e) { _sm.OnPlayerError(gen, e.Message); }
        }

        private void EnsurePlayer()
        {
            if (_player != null) return;
            var go = new GameObject("InGameVideoPlayer");
            go.transform.SetParent(transform, false);
            _player = go.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.waitForFirstFrame = true;
            _player.skipOnDrop = true;
            _player.isLooping = false;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
            _player.prepareCompleted += p => { if (_sm.OnPrepared(_sm.Generation)) StartPlayback(); };
            _player.errorReceived += (p, msg) => _sm.OnPlayerError(_sm.Generation, msg);
            _player.loopPointReached += p => { if (_sm.OnEnded(_sm.Generation) && _entry != null) { LearningProgressStore.Shared.MarkVideoWatched(_entry.id); DrawList(); } };
        }

        private void StartPlayback()
        {
            int w = (int)Mathf.Max(16, _player.width), h = (int)Mathf.Max(16, _player.height);
            if (_rt == null || _rt.width != w || _rt.height != h)
            {
                if (_rt != null) { _rt.Release(); Destroy(_rt); }
                _rt = new RenderTexture(w, h, 0) { name = "InGameVideoRT" };
            }
            _player.targetTexture = _rt;
            _screen.texture = _rt;
            FitScreen(w, h);
            ApplyVolume();
            _player.Play();
        }

        private void FitScreen(float w, float h)
        {
            var fit = _screen.GetComponent<AspectRatioFitter>() ?? _screen.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = h > 0 ? w / h : 16f / 9f;
        }

        public void TogglePause()
        {
            var before = _sm.State;
            if (!_sm.TogglePause()) { if (before == VideoPlaybackState.Error) Retry(); return; }
            if (_player == null) return;
            if (_sm.State == VideoPlaybackState.Playing) { if (before == VideoPlaybackState.Ended) _player.time = 0; _player.Play(); }
            else _player.Pause();
        }

        private void OnSeekChanged(float value)
        {
            if (_seeking || _player == null || !_sm.CanSeek) return;
            double length = _player.length;
            _player.time = VideoPlaybackStateMachine.SeekTime(value, length);
        }

        private void OnVolumeChanged(float v) { PlayerPrefs.SetFloat("NihongoLife.VideoVolume", VideoPlaybackStateMachine.ClampVolume(v)); ApplyVolume(); }
        private void ToggleMute() { _volume.value = _volume.value > 0.01f ? 0f : 0.8f; }

        private void ApplyVolume()
        {
            if (_player == null) return;
            float v = VideoPlaybackStateMachine.ClampVolume(_volume != null ? _volume.value : 1f);
            for (ushort t = 0; t < System.Math.Max((ushort)1, _player.audioTrackCount); t++) _player.SetDirectAudioVolume(t, v);
        }

        public void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen;
            if (_fullscreen)
            {
                UiKit.Place(_stage, 12f, 12f, W - 24f, H - 24f - 70f);
                UiKit.Place(_controls, 12f, H - 12f - 62f, W - 24f, 58f);
            }
            else
            {
                UiKit.Place(_stage, _stagePos.x, _stagePos.y, PW, PH);
                UiKit.Place(_controls, _stagePos.x, _stagePos.y + PH + 10f, PW, 58f);
            }
            _stage.SetAsLastSibling(); _controls.SetAsLastSibling();
        }

        public void Retry() { if (_entry != null) { Stop(); Load(); } }

        public void OpenExternal()
        {
            string url = _entry?.youtubeUrl;
            if (string.IsNullOrWhiteSpace(url)) url = VideoCatalog.Data.channelUrl;
            if (!string.IsNullOrWhiteSpace(url) && (url.StartsWith("https://") || url.StartsWith("http://"))) Application.OpenURL(url);
        }

        private void Stop()
        {
            _sm.Close();
            if (_player != null) { try { _player.Stop(); } catch (System.Exception) { } }
            if (_screen != null) _screen.enabled = false;
            if (_fullscreen) ToggleFullscreen();
        }

        protected override void OnOpenUpdate()
        {
            if (_sm.Tick(Time.unscaledDeltaTime) && _player != null) _player.Stop();
            if (_player != null && (_sm.State == VideoPlaybackState.Playing || _sm.State == VideoPlaybackState.Paused))
            {
                double len = _player.length, t = _player.time;
                _seeking = true;
                if (len > 0) _seek.SetValueWithoutNotify((float)(t / len));
                _seeking = false;
                _time.text = VideoPlaybackStateMachine.FormatTime(t) + " / " + VideoPlaybackStateMachine.FormatTime(len);
            }
        }

        private void RefreshChrome()
        {
            if (_overlay == null) return;
            var s = _sm.State;
            _screen.enabled = s == VideoPlaybackState.Playing || s == VideoPlaybackState.Paused || s == VideoPlaybackState.Ended;
            _thumbBg.enabled = !_screen.enabled;
            _retryBtn.gameObject.SetActive(s == VideoPlaybackState.Error);
            bool hasYoutube = _entry != null && VideoCatalog.IsYouTube(_entry.youtubeUrl);
            _externalBtn.gameObject.SetActive(hasYoutube && (s == VideoPlaybackState.ExternalOnly || s == VideoPlaybackState.Error));
            _controls.gameObject.SetActive(s != VideoPlaybackState.ExternalOnly);
            _playBtn.interactable = s != VideoPlaybackState.Preparing && s != VideoPlaybackState.Idle;
            var icon = _playBtn.transform.GetComponentsInChildren<Image>(true);
            foreach (var img in icon) if (img.name.StartsWith("Icon_")) img.sprite = UiIcons.Get(s == VideoPlaybackState.Playing ? "pause" : s == VideoPlaybackState.Ended ? "replay" : "play_arrow");
            _overlay.text = s switch
            {
                VideoPlaybackState.Preparing => Loc.T("Đang tải video…", "Loading video…", "動画を読み込み中…"),
                VideoPlaybackState.ExternalOnly => Loc.T("Video này đang ở trên YouTube. Xem tóm tắt bên dưới hoặc mở trên YouTube.",
                    "This video is hosted on YouTube. Read the summary below or open it on YouTube.",
                    "この動画はYouTubeで公開中です。下の要約を読むか、YouTubeで開いてください。"),
                VideoPlaybackState.Error => ErrorText(),
                VideoPlaybackState.Ended => Loc.T("Đã xem xong", "Finished", "視聴完了"),
                VideoPlaybackState.Idle when _entry == null => Loc.T("Chọn một video bên trái.", "Pick a video on the left.", "左から動画を選んでください。"),
                _ => "",
            };
            if (s == VideoPlaybackState.Ended) _screen.enabled = true;
        }

        private string ErrorText()
        {
            string reason = _sm.Error switch
            {
                VideoErrorKind.Offline => Loc.T("Không có kết nối mạng.", "You're offline.", "オフラインです。"),
                VideoErrorKind.BadUrl => Loc.T("Đường dẫn video không hợp lệ.", "The video link is invalid.", "動画のリンクが無効です。"),
                VideoErrorKind.Timeout => Loc.T("Tải video quá lâu.", "The video took too long to load.", "読み込みに時間がかかりすぎました。"),
                VideoErrorKind.NoSource => Loc.T("Video chưa có nguồn phát.", "This video has no source yet.", "この動画には再生ソースがありません。"),
                _ => Loc.T("Không phát được video này.", "This video couldn't be played.", "この動画を再生できませんでした。"),
            };
            return reason;
        }

        private void RefreshInfo()
        {
            if (_title == null) return;
            _title.text = _entry == null ? "" : VideoCatalog.Title(_entry);
            _summary.text = _entry == null ? "" : VideoCatalog.Summary(_entry);
            string tr = _entry?.transcript;
            _transcript.text = string.IsNullOrWhiteSpace(tr) ? "" : "<b>" + Loc.T("Phụ đề / lời thoại", "Transcript", "字幕・スクリプト") + "</b>\n" + tr
                + (string.IsNullOrWhiteSpace(_entry.attribution) ? "" : "\n\n" + _entry.attribution);
            if (_entry != null && string.IsNullOrWhiteSpace(tr) && !string.IsNullOrWhiteSpace(_entry.attribution)) _transcript.text = _entry.attribution;
        }

        private void LoadThumbnail()
        {
            if (_thumbJob != null) { StopCoroutine(_thumbJob); _thumbJob = null; }
            _thumbBg.sprite = null; _thumbBg.color = new Color(1f, 1f, 1f, 0f);
            string url = VideoCatalog.ThumbnailUrl(_entry);
            if (url == null || Application.internetReachability == NetworkReachability.NotReachable || !isActiveAndEnabled) return;
            _thumbJob = StartCoroutine(FetchThumb(url, _entry.id));
        }

        private IEnumerator FetchThumb(string url, string id)
        {
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success || _entry == null || _entry.id != id || _thumbBg == null) yield break;
                if (_thumb != null) Destroy(_thumb);
                _thumb = DownloadHandlerTexture.GetContent(req);
                if (_thumb == null) yield break;
                _thumbBg.sprite = Sprite.Create(_thumb, new Rect(0, 0, _thumb.width, _thumb.height), new Vector2(0.5f, 0.5f));
                _thumbBg.color = new Color(1f, 1f, 1f, 0.55f);
            }
        }
    }
}
