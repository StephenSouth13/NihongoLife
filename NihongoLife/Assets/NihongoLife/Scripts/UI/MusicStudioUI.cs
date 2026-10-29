using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Music;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The music room: play a piano (two octaves on screen, the keyboard's A–' row for white keys and W E T Y U O P for
    /// black ones, Z / X shift the octave) and a guitar (chord shapes, Space strums, 1–4 pick a chord of the
    /// progression); learn a melody note by note (the next key glows, a wrong key is counted); listen to it; study music
    /// theory (24 lessons in vi / en / ja / ru with quizzes); and look at paintings in the gallery. Content:
    /// Music/MusicCatalog. Sounds: Music/InstrumentSynth (made in code, nothing downloaded).
    /// Above all the character plays a song by itself (AutoPlay): the keys light up, the player changes the tempo
    /// (50–160 %), the volume, piano or guitar and loop while it plays (← → tempo, ↑ ↓ volume). Seated at an instrument
    /// of the Hibiki music shop (SetPerformer) the window shrinks to a bar at the bottom so the character is seen playing,
    /// its hands following the beat (gestures piano_play / guitar_strum). On the shop's stage (OpenStage) the character
    /// performs: a show near the song's own tempo (80–125 %) pleases the audience — a fan more and "perform" (the
    /// musician's shift). Playing by hand on the keyboard stays, for fun and practice (best accuracy is kept).
    /// </summary>
    public sealed class MusicStudioUI : MonoBehaviour
    {
        public enum Tab { Piano, Guitar, Songs, Theory, Gallery }

        private const string TheoryKey = "NL.MusicTheoryDone";
        private const string BestKey = "NL.MusicBest.";
        private const string FansKey = "NL.MusicFans";
        /// <summary>Accuracy that counts a song as learnt (it may be performed) and a gig as good.</summary>
        public const int LearntScore = 80;
        private static readonly Key[] WhiteKeys = { Key.A, Key.S, Key.D, Key.F, Key.G, Key.H, Key.J, Key.K, Key.L, Key.Semicolon, Key.Quote };
        private static readonly (Key key, int semitone)[] BlackKeys = { (Key.W, 1), (Key.E, 3), (Key.T, 6), (Key.Y, 8), (Key.U, 10), (Key.O, 13), (Key.P, 15) };
        private static readonly int[] WhiteSemitones = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23 };
        private static readonly Color Ivory = new Color(0.97f, 0.96f, 0.92f), Ebony = new Color(0.12f, 0.12f, 0.14f), Glow = new Color(0.98f, 0.78f, 0.25f);

        private static MusicStudioUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _tabs, _body;
        private AudioSource _audio;
        private PlayerController _player;
        private bool _wasLocked;
        private Tab _tab = Tab.Piano;
        private int _base = 60;
        private readonly Dictionary<int, Image> _pianoKeys = new Dictionary<int, Image>();
        private readonly Dictionary<int, Color> _keyColors = new Dictionary<int, Color>();
        private SongDefinition _practice;
        private int _step, _mistakes;
        private TextMeshProUGUI _status;
        private int _progression;
        private string _chord;
        private string _lesson;
        private int _question;
        private Artwork _painting;
        private bool _stage;
        private bool _street;
        private string _result;
        private RectTransform _card, _mini;
        private SongDefinition _auto;
        private string _lastAuto;
        private float _autoBeat, _tempo = 1f, _volume = 0.8f;
        private int _autoNote, _shownNote = -1;
        private bool _loop, _guitar, _autoGig, _autoFinished;
        private GameObject _performer;
        private TextMeshProUGUI _autoStatus;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeInHierarchy;
        public static Tab CurrentTab => _instance != null ? _instance._tab : Tab.Piano;
        /// <summary>Tests: the melody being practised, the next note's index, the wrong keys so far.</summary>
        public static (string song, int step, int mistakes) Practice => _instance != null && _instance._practice != null ? (_instance._practice.id, _instance._step, _instance._mistakes) : (null, 0, 0);
        /// <summary>Best practice accuracy of a song (0 = never finished).</summary>
        public static int Best(string songId) => PlayerPrefs.GetInt(BestKey + songId, 0);
        public static bool Learnt(string songId) => Best(songId) >= LearntScore;
        public static int Fans => PlayerPrefs.GetInt(FansKey, 0);
        public static bool OnStage => _instance != null && _instance._stage;
        /// <summary>A street performance with the player's own guitar (MusicShop.Busk): passers-by tip a good song.</summary>
        public static bool OnStreet => _instance != null && _instance._street;
        /// <summary>Tips of the last street song (tests).</summary>
        public static int LastTips { get; private set; }
        /// <summary>The last finished melody: its accuracy, whether it was a gig and whether it was good.</summary>
        public static (int score, bool gig, bool good) LastResult { get; private set; }

        /// <summary>Tests: forget a song's best score and the fans.</summary>
        public static void ForgetSong(string songId) { PlayerPrefs.DeleteKey(BestKey + songId); PlayerPrefs.DeleteKey(FansKey); }

        public const float MinTempo = 0.5f, MaxTempo = 1.6f;
        /// <summary>A show between these tempos (× the song's own) pleases the audience.</summary>
        public const float GoodSlow = 0.8f, GoodFast = 1.25f;
        public static bool IsAutoPlaying => _instance != null && _instance._auto != null;
        public static string AutoSong => IsAutoPlaying ? _instance._auto.id : null;
        public static float AutoProgress => IsAutoPlaying && _instance._auto.notes.Length > 0 ? (float)_instance._autoNote / _instance._auto.notes.Length : 0f;
        public static bool Mini => _instance != null && _instance._mini != null && _instance._mini.gameObject.activeSelf;
        public static float Tempo
        {
            get => Instance._tempo;
            set { var ui = Instance; ui._tempo = Mathf.Clamp(Mathf.Round(value * 20f) / 20f, MinTempo, MaxTempo); ui.ApplyGestureBpm(); ui.RefreshAuto(true); }
        }
        public static float Volume
        {
            get => Instance._volume;
            set { var ui = Instance; ui._volume = Mathf.Clamp01(Mathf.Round(value * 20f) / 20f); ui._audio.volume = ui._volume; ui.RefreshAuto(true); }
        }
        public static bool Loop { get => Instance._loop; set { Instance._loop = value; Instance.Redraw(); } }
        public static bool UseGuitar { get => Instance._guitar; set { Instance._guitar = value; Instance.ApplyGesture(); Instance.Redraw(); } }

        /// <summary>The music shop seats the player at an instrument: they play the songs (hands follow the music) and the
        /// window shrinks to a bar so they can be seen.</summary>
        public static void SetPerformer(GameObject who, bool guitar)
        {
            var ui = Instance;
            ui.StopGesture();
            ui._performer = who;
            ui._guitar = guitar;
        }

        private static MusicStudioUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("MusicStudioCanvas", 128);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<MusicStudioUI>();
                _instance._font = NLUi.ResolveFont();
                _instance._audio = canvas.gameObject.AddComponent<AudioSource>();
                _instance._audio.playOnAwake = false;
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Rebuild(); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "MusicStudio");
                return _instance;
            }
        }

        public static void Open(Tab tab = Tab.Piano)
        {
            var ui = Instance;
            ui._tab = tab;
            if (!ui._root.gameObject.activeSelf)
            {
                ui._root.gameObject.SetActive(true);
                ui._player = SceneRefs.Find<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }
            ui._root.SetAsLastSibling();
            ui.Rebuild();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _instance.StopAllCoroutines();
            _instance._auto = null;
            _instance._autoFinished = false;
            _instance.StopGesture();
            _instance._performer = null;
            _instance._painting = null; // the gallery opens on its grid next time
            _instance.SetMini(false);
            _instance._root.gameObject.SetActive(false);
            _instance._stage = false;
            _instance._street = false;
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
        }

        private void Build(RectTransform canvas)
        {
            _root = new GameObject("MusicStudio", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.72f);
            var card = NLUi.Panel(_root, "Card", new Color(0.09f, 0.08f, 0.12f, 0.98f));
            _card = card;
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(1180f, 720f);
            card.gameObject.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);
            _tabs = NLUi.Group(card, "Tabs", false, 8f, TextAnchor.MiddleLeft, false);
            _tabs.anchorMin = new Vector2(0f, 1f); _tabs.anchorMax = new Vector2(1f, 1f); _tabs.pivot = new Vector2(0.5f, 1f);
            _tabs.offsetMin = new Vector2(28f, -92f); _tabs.offsetMax = new Vector2(-80f, -20f);
            var scrollRoot = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollRoot.SetParent(card, false);
            NLUi.Stretch(scrollRoot);
            scrollRoot.offsetMin = new Vector2(28f, 20f); scrollRoot.offsetMax = new Vector2(-20f, -100f);
            _body = UiKit.Scroll(scrollRoot, "Content", out var scroll, 12f, new RectOffset(2, 16, 4, 16));
            NLUi.Stretch((RectTransform)scroll.transform);
            NLUi.CloseButton(card, _font, Close, 46f, 16f);
            // The bar shown while the seated character plays: the 3D scene stays visible above it.
            _mini = NLUi.Panel(_root, "MiniPlayer", new Color(0.09f, 0.08f, 0.12f, 0.94f), new RectOffset(22, 22, 14, 14), 8f);
            _mini.anchorMin = _mini.anchorMax = _mini.pivot = new Vector2(1f, 0f); // bottom right: clear of the status panel (bottom left)
            _mini.anchoredPosition = new Vector2(-24f, 24f);
            _mini.sizeDelta = new Vector2(1040f, 176f);
            _mini.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Redraws whichever view is shown (the bar or the full window).</summary>
        private void Redraw()
        {
            if (!IsOpen) return;
            if (Mini) BuildMini(); else Rebuild();
        }

        private void SetMini(bool on)
        {
            if (_mini == null) return;
            _mini.gameObject.SetActive(on);
            _card.gameObject.SetActive(!on);
            var shade = _root.GetComponent<Image>();
            shade.color = new Color(0.01f, 0.015f, 0.02f, on ? 0f : 0.72f);
            shade.raycastTarget = !on;
            if (on) BuildMini();
        }

        private void BuildMini()
        {
            UiKit.Clear(_mini);
            AutoControls(_mini, true);
        }

        private void Rebuild()
        {
            UiKit.Clear(_tabs);
            UiKit.Clear(_body);
            _pianoKeys.Clear();
            _keyColors.Clear();
            foreach (Tab t in System.Enum.GetValues(typeof(Tab)))
            {
                var tab = t;
                var b = NLUi.Button(_tabs, "Tab_" + t, TabName(t), _font, () => { _tab = tab; Rebuild(); }, t == _tab ? NLUi.Gold : NLUi.Card, 17f, t == _tab ? NLUi.Ink : NLUi.Text, 48f);
                NLUi.Size(b, 190f, 48f);
            }
            switch (_tab)
            {
                case Tab.Piano: BuildPiano(); break;
                case Tab.Guitar: BuildGuitar(); break;
                case Tab.Songs: BuildSongs(); break;
                case Tab.Theory: BuildTheory(); break;
                default: BuildGallery(); break;
            }
        }

        private static string TabName(Tab t) => t switch
        {
            Tab.Piano => Loc.T("Piano", "Piano", "ピアノ"),
            Tab.Guitar => Loc.T("Guitar", "Guitar", "ギター"),
            Tab.Songs => Loc.T("Bài hát", "Songs", "曲"),
            Tab.Theory => Loc.T("Nhạc lý", "Theory", "音楽理論"),
            _ => Loc.T("Phòng tranh", "Gallery", "ギャラリー"),
        };

        // ─────────── Piano ───────────

        private void BuildPiano()
        {
            if (_auto != null || _autoFinished) AutoControls(NLUi.Panel(_body, "NowPlaying", NLUi.Card, new RectOffset(18, 18, 12, 12), 8f), false);
            string title = _practice == null ? Loc.T("Piano tự do — hoặc chọn bài ở thẻ Bài hát để nhân vật tự đàn", "Free play — or pick a song in Songs and the character plays it", "自由に弾く — 「曲」でえらぶと自動で演奏")
                : Loc.F("Đang tập: {0}", "Practising: {0}", "練習中：{0}", _practice.Title);
            NLUi.Label(_body, "Title", title, 22f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_body, "Help", Loc.T("Bấm phím trên màn hình hoặc bàn phím: A S D F G H J K L ; ' (phím trắng), W E T Y U O P (phím đen). Z / X đổi quãng tám.",
                "Click the keys or use the keyboard: A S D F G H J K L ; ' (white), W E T Y U O P (black). Z / X change octave.",
                "画面の鍵盤か、キーボードの A S D F G H J K L ; '（白鍵）W E T Y U O P（黒鍵）。Z / X でオクターブ。"), 15f, NLUi.Muted, _font);
            var board = new GameObject("Keyboard", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            board.SetParent(_body, false);
            board.GetComponent<LayoutElement>().preferredHeight = 240f;
            const float w = 70f;
            for (int i = 0; i < WhiteSemitones.Length; i++)
                PianoKey(board, _base + WhiteSemitones[i], new Vector2(i * w, 0f), new Vector2(w - 4f, 230f), Ivory, i < WhiteKeys.Length ? KeyLabel(WhiteKeys[i]) : "");
            for (int octave = 0; octave < 2; octave++)
                foreach (int s in new[] { 1, 3, 6, 8, 10 })
                {
                    int white = System.Array.IndexOf(WhiteSemitones, octave * 12 + s - 1);
                    var mapping = BlackKeys.FirstOrDefault(b => b.semitone == octave * 12 + s);
                    PianoKey(board, _base + octave * 12 + s, new Vector2(white * w + w * 0.62f, 0f), new Vector2(w * 0.72f, 140f), Ebony, mapping.semitone == octave * 12 + s ? KeyLabel(mapping.key) : "");
                }
            _status = NLUi.Label(_body, "Status", "", 18f, NLUi.Text, _font, FontStyles.Bold);
            var row = NLUi.Group(_body, "Actions", false, 8f, TextAnchor.MiddleLeft, false);
            NLUi.Button(row, "OctaveDown", "− " + Loc.T("Quãng tám", "Octave", "オクターブ"), _font, () => { _base = Mathf.Max(36, _base - 12); Rebuild(); }, NLUi.Card, 15f, NLUi.Text, 42f);
            NLUi.Button(row, "OctaveUp", "+ " + Loc.T("Quãng tám", "Octave", "オクターブ"), _font, () => { _base = Mathf.Min(84, _base + 12); Rebuild(); }, NLUi.Card, 15f, NLUi.Text, 42f);
            if (_practice != null)
            {
                NLUi.Button(row, "Listen", Loc.T("Nghe mẫu", "Listen", "お手本"), _font, () => StartCoroutine(PlaySong(_practice)), NLUi.Card, 15f, NLUi.Text, 42f);
                NLUi.Button(row, "StopPractice", Loc.T("Dừng tập", "Stop", "やめる"), _font, () => { _practice = null; Rebuild(); }, NLUi.Card, 15f, NLUi.Text, 42f);
            }
            ShowNext();
        }

        private static string KeyLabel(Key key) => key switch { Key.Semicolon => ";", Key.Quote => "'", _ => key.ToString() };

        private void PianoKey(RectTransform board, int midi, Vector2 pos, Vector2 size, Color color, string label)
        {
            var key = new GameObject("Key_" + midi, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            key.SetParent(board, false);
            key.anchorMin = key.anchorMax = key.pivot = new Vector2(0f, 1f);
            key.anchoredPosition = pos;
            key.sizeDelta = size;
            var image = key.GetComponent<Image>();
            image.color = color;
            int note = midi;
            key.GetComponent<Button>().onClick.AddListener(() => Press(note));
            var text = NLUi.Label(key, "Name", MusicCatalog.NoteName(midi) + (string.IsNullOrEmpty(label) ? "" : "\n<size=80%>" + label + "</size>"), 12f, color.grayscale > 0.5f ? NLUi.Ink : Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Bottom);
            NLUi.Stretch(text.rectTransform, 4f);
            _pianoKeys[midi] = image;
            _keyColors[midi] = color;
        }

        /// <summary>A key played (screen, keyboard or test): the sound, a flash, and the practice step.</summary>
        public static void Press(int midi)
        {
            var ui = Instance;
            ui._audio.PlayOneShot(InstrumentSynth.Clip(InstrumentSynth.Voice.Piano, midi));
            if (ui._pianoKeys.TryGetValue(midi, out var image)) ui.StartCoroutine(ui.Flash(image, midi));
            if (ui._practice == null || ui._step >= ui._practice.notes.Length) return;
            int want = ui._practice.notes[ui._step].midi;
            if (midi == want) ui._step++;
            else ui._mistakes++;
            if (ui._step >= ui._practice.notes.Length) ui.Finish();
            ui.ShowNext();
        }

        private IEnumerator Flash(Image image, int midi)
        {
            image.color = Glow;
            yield return new WaitForSecondsRealtime(0.15f);
            if (image != null) image.color = _keyColors.TryGetValue(midi, out var c) ? c : Ivory;
            ShowNext();
        }

        private void ShowNext()
        {
            if (_status == null) return;
            foreach (var pair in _pianoKeys) pair.Value.color = _keyColors[pair.Key];
            if (_practice == null) { _status.text = ""; return; }
            if (_step >= _practice.notes.Length)
            {
                int total = _practice.notes.Length;
                int score = Mathf.RoundToInt(100f * total / Mathf.Max(1, total + _mistakes));
                _status.text = Loc.F("Xong! Độ chính xác {0}% ({1} phím sai).", "Done! Accuracy {0}% ({1} wrong keys).", "できた！正確さ {0}%（ミス {1}）", score, _mistakes)
                    + (string.IsNullOrEmpty(_result) ? "" : "\n" + _result);
                return;
            }
            int next = _practice.notes[_step].midi;
            if (_pianoKeys.TryGetValue(next, out var key)) key.color = Color.Lerp(_keyColors[next], Glow, 0.7f);
            _status.text = Loc.F("Nốt {0}/{1}: {2}", "Note {0}/{1}: {2}", "{0}/{1}：{2}", _step + 1, _practice.notes.Length, MusicCatalog.NoteName(next));
        }

        /// <summary>A melody played by hand to the end: its best accuracy is kept (✓ from 80 %).</summary>
        private void Finish()
        {
            int total = _practice.notes.Length;
            int score = Mathf.RoundToInt(100f * total / Mathf.Max(1, total + _mistakes));
            bool good = score >= LearntScore;
            LastResult = (score, false, good);
            bool first = !Learnt(_practice.id) && good;
            if (score > Best(_practice.id)) PlayerPrefs.SetInt(BestKey + _practice.id, score);
            _result = first ? Loc.T("Bạn đã tự đàn thuộc bài này!", "You can play this one by hand now!", "自分で弾けるように なりました！") : "";
        }

        /// <summary>The shop's stage: the song list in performing mode.</summary>
        public static void OpenStage()
        {
            var ui = Instance;
            ui._practice = null;
            Open(Tab.Songs);
            ui._street = false;
            ui._stage = true;
            ui.Rebuild();
        }

        /// <summary>The town: the song list in performing mode, played standing with the player's own guitar.</summary>
        public static void OpenStreet()
        {
            var ui = Instance;
            ui._practice = null;
            Open(Tab.Songs);
            ui._stage = false;
            ui._street = true;
            ui._guitar = true;
            ui.Rebuild();
        }

        /// <summary>A show on the stage: the character plays the song for the audience (judged by the tempo at the end).</summary>
        public static bool Perform(string songId)
        {
            if (MusicCatalog.Song(songId) == null) return false;
            AutoPlay(songId, true);
            return true;
        }

        // ─────────── The character plays ───────────

        /// <summary>The character plays a song by itself; the keys light up; tempo, volume, instrument and loop change live.</summary>
        public static void AutoPlay(string songId, bool gig = false)
        {
            var song = MusicCatalog.Song(songId);
            if (song == null) return;
            var ui = Instance;
            if (!IsOpen) Open(Tab.Piano);
            ui._practice = null;
            ui._auto = song;
            ui._lastAuto = song.id;
            ui._autoBeat = -0.5f; // a breath before the first note
            ui._autoNote = 0;
            ui._shownNote = -1;
            ui._autoGig = gig;
            ui._autoFinished = false;
            ui._result = null;
            ui._audio.volume = ui._volume;
            int low = song.notes.Length > 0 ? song.notes.Min(n => n.midi) : 60;
            ui._base = Mathf.Clamp(low - ((low % 12) + 12) % 12, 36, 84);
            ui._tab = Tab.Piano;
            ui.ApplyGesture();
            if (ui._performer != null) ui.SetMini(true);
            else ui.Rebuild();
        }

        public static void StopAuto()
        {
            if (_instance == null) return;
            _instance._auto = null;
            _instance._autoFinished = false;
            _instance.StopGesture();
            _instance.Redraw();
        }

        /// <summary>Tests: the song played to its end at once.</summary>
        public static void SkipToEnd()
        {
            if (!IsAutoPlaying) return;
            var ui = _instance;
            ui._autoNote = ui._auto.notes.Length;
            ui._autoBeat = Beats(ui._auto) + 1f;
            ui.TickAuto(0f);
        }

        private static float Beats(SongDefinition song) => song.notes.Length == 0 ? 0f : song.notes.Max(n => n.start + n.duration);

        private void TickAuto(float dt)
        {
            if (_auto == null) return;
            _autoBeat += dt * _tempo / Mathf.Max(0.05f, _auto.SecondsPerBeat);
            var notes = _auto.notes;
            while (_autoNote < notes.Length && notes[_autoNote].start <= _autoBeat)
            {
                int midi = notes[_autoNote++].midi;
                _audio.PlayOneShot(InstrumentSynth.Clip(_guitar ? InstrumentSynth.Voice.Guitar : InstrumentSynth.Voice.Piano, midi), 0.9f);
                if (_pianoKeys.TryGetValue(midi, out var image) && image != null) StartCoroutine(Flash(image, midi));
            }
            if (_autoNote >= notes.Length && _autoBeat >= Beats(_auto))
            {
                if (_loop && !_autoGig) { _autoNote = 0; _autoBeat = -0.5f; }
                else { FinishAuto(); return; }
            }
            RefreshAuto(false);
        }

        /// <summary>The end of a song: a show is judged by its tempo (the audience likes 80–125 % of the song's own).</summary>
        private void FinishAuto()
        {
            bool gig = _autoGig;
            _auto = null;
            _autoGig = false;
            _autoFinished = true;
            StopGesture();
            if (gig)
            {
                bool good = _tempo >= GoodSlow - 0.001f && _tempo <= GoodFast + 0.001f;
                LastResult = (Mathf.RoundToInt(_tempo * 100f), true, good);
                if (_street)
                {
                    int listeners = Listeners();
                    LastTips = good ? 40 + 30 * Mathf.Min(listeners, 8) : (listeners > 0 ? 10 : 0);
                    var inventory = NihongoLife.Player.PlayerInventory.Instance;
                    if (inventory != null && LastTips > 0) inventory.AddYen(LastTips, Loc.Triple("Biểu diễn đường phố", "Busking tips", "ストリートライブの チップ"));
                    if (good)
                    {
                        PlayerPrefs.SetInt(FansKey, Fans + 1);
                        NihongoLife.Progression.QuestService.Raise("perform", "music_street");
                    }
                    _result = good
                        ? Loc.F("{0} người dừng lại nghe — tiền thưởng {1}. +1 người hâm mộ.", "{0} people stopped to listen — tips {1}. +1 fan.", "{0}人が 聞いてくれた — チップ {1}。ファン +1", listeners, Loc.Yen(LastTips))
                        : (_tempo < GoodSlow
                            ? Loc.T("Chậm quá — người qua đường đi luôn. Thử nhịp 80–125%.", "Too slow — people walk on. Try 80–125%.", "おそすぎて みんな 行ってしまった。80〜125% で。")
                            : Loc.T("Nhanh quá — chẳng ai theo kịp. Thử nhịp 80–125%.", "Too fast — nobody can follow. Try 80–125%.", "はやすぎます。80〜125% で。"));
                    HudFeed.Post(_result, good ? HudFeed.Kind.Reward : HudFeed.Kind.Info, 5f, "music");
                    Redraw();
                    return;
                }
                if (good)
                {
                    PlayerPrefs.SetInt(FansKey, Fans + 1);
                    NihongoLife.Progression.QuestService.Raise("perform", "music_stage");
                    _result = Loc.F("Khán giả vỗ tay! +1 người hâm mộ (tổng {0}).", "The audience applauds! +1 fan ({0} in all).", "はくしゅ！ファン +1（{0}人）", Fans);
                }
                else _result = _tempo < GoodSlow
                    ? Loc.T("Chậm quá — khán giả ngáp ngắn ngáp dài. Thử nhịp 80–125% nhé.", "Too slow — the audience yawns. Try 80–125%.", "おそすぎて お客さんが あくび。80〜125% で。")
                    : Loc.T("Nhanh quá — khán giả không theo kịp. Thử nhịp 80–125% nhé.", "Too fast — the audience can't follow. Try 80–125%.", "はやすぎて ついていけません。80〜125% で。");
                HudFeed.Post(_result, good ? HudFeed.Kind.Reward : HudFeed.Kind.Info, 4f, "music");
            }
            else
            {
                LastResult = (100, false, true);
                _result = Loc.T("Đàn xong rồi!", "Finished playing!", "えんそう おわり！");
            }
            Redraw();
        }

        /// <summary>People within 12 m of the performer (counted once, at the end of a street song).</summary>
        private int Listeners()
        {
            if (_performer == null) return 0;
            Vector3 at = _performer.transform.position;
            int n = 0;
            foreach (var animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (animator == null || !animator.isHuman || !animator.gameObject.activeInHierarchy) continue;
                if (animator.transform.IsChildOf(_performer.transform) || _performer.transform.IsChildOf(animator.transform)) continue;
                if ((animator.transform.position - at).sqrMagnitude < 144f) n++;
            }
            return n;
        }

        private void RefreshAuto(bool force)
        {
            if (_autoStatus == null || (!force && _autoNote == _shownNote)) return;
            _shownNote = _autoNote;
            _autoStatus.text = AutoStatusText();
        }

        private string AutoStatusText()
        {
            string speed = Loc.F("Nhịp {0}% ({1} BPM)", "Tempo {0}% ({1} BPM)", "テンポ {0}%（{1} BPM）", Mathf.RoundToInt(_tempo * 100f), Mathf.RoundToInt((_auto ?? MusicCatalog.Song(_lastAuto))?.tempo_bpm * _tempo ?? 0f));
            string volume = Loc.F("Âm lượng {0}%", "Volume {0}%", "音量 {0}%", Mathf.RoundToInt(_volume * 100f));
            if (_auto == null) return (_result ?? "") + "   ·   " + speed + " · " + volume;
            return Loc.F("Nốt {0}/{1}", "Note {0}/{1}", "{0}/{1}", _autoNote, _auto.notes.Length) + "   ·   " + speed + " · " + volume
                + (_autoGig ? "   ·   " + (_street ? Loc.T("Đang diễn đường phố!", "Busking!", "ストリートライブ中！") : Loc.T("Đang diễn!", "On stage!", "ライブ中！")) : "");
        }

        /// <summary>Now playing: the song, its progress, and the knobs — slower / faster, quieter / louder, piano / guitar, loop, stop.</summary>
        private void AutoControls(RectTransform parent, bool mini)
        {
            var song = _auto ?? MusicCatalog.Song(_lastAuto);
            if (song == null) return;
            string who = _performer != null ? Loc.T("Nhân vật đang đàn", "Your character plays", "演奏中") : Loc.T("Đang phát", "Playing", "再生中");
            NLUi.Label(parent, "NowTitle", (_auto != null ? who + ": " : Loc.T("Vừa đàn: ", "Just played: ", "演奏した曲：")) + $"<b>{song.Title}</b>  <size=75%><color=#A8B4C4>{song.composer}</color></size>", mini ? 20f : 19f, NLUi.Gold, _font);
            _autoStatus = NLUi.Label(parent, "NowStatus", AutoStatusText(), 15f, NLUi.Text, _font);
            _shownNote = _autoNote;
            var row = NLUi.Group(parent, "Knobs", false, 6f, TextAnchor.MiddleLeft, false);
            void Knob(string name, string label, System.Action act, Color color) => NLUi.Size(NLUi.Button(row, name, label, _font, () => act(), color, 13f, color == NLUi.Gold ? NLUi.Ink : NLUi.Text, 40f), -1f, 40f);
            if (_auto != null)
            {
                Knob("Slower", "◀ " + Loc.T("Chậm", "Slower", "おそく"), () => Tempo -= 0.1f, NLUi.CardHover);
                Knob("Faster", Loc.T("Nhanh", "Faster", "はやく") + " ▶", () => Tempo += 0.1f, NLUi.CardHover);
                Knob("Quieter", Loc.T("Nhỏ", "Quieter", "小さく") + " −", () => Volume -= 0.1f, NLUi.CardHover);
                Knob("Louder", Loc.T("To", "Louder", "大きく") + " +", () => Volume += 0.1f, NLUi.CardHover);
                if (!_street) Knob("Instrument", _guitar ? "Guitar → Piano" : "Piano → Guitar", () => UseGuitar = !_guitar, NLUi.CardHover);
                if (!_autoGig) Knob("Loop", Loc.T("Lặp lại: ", "Loop: ", "くりかえし：") + (_loop ? Loc.T("bật", "on", "オン") : Loc.T("tắt", "off", "オフ")), () => Loop = !_loop, _loop ? NLUi.Gold : NLUi.CardHover);
                Knob("Stop", "■ " + Loc.T("Dừng", "Stop", "とめる"), StopAuto, NLUi.Card);
            }
            else
            {
                string again = song.id;
                bool stage = _stage || _street;
                Knob("Again", "▶ " + Loc.T("Đàn lại", "Play again", "もう一度"), () => AutoPlay(again, stage), NLUi.Gold);
            }
            if (mini) Knob("Songs", Loc.T("Chọn bài khác", "Other songs", "ほかの曲"), () => { SetMini(false); _tab = Tab.Songs; Rebuild(); }, NLUi.Card);
            NLUi.Label(parent, "Keys", Loc.T("Phím ← → đổi nhịp · ↑ ↓ âm lượng · Esc đóng", "← → tempo · ↑ ↓ volume · Esc closes", "← → テンポ · ↑ ↓ 音量 · Esc とじる"), 12f, NLUi.Muted, _font);
        }

        private void ApplyGesture()
        {
            if (_performer == null || _auto == null) return;
            var layer = NihongoLife.Gestures.GestureLayer.On(_performer);
            string id = _guitar ? "guitar_strum" : "piano_play";
            if (layer.Current != id) { layer.ResetMotion(); layer.Play(id); } // the player was just seated: not "walking away"
            ApplyGestureBpm();
        }

        private void ApplyGestureBpm()
        {
            if (_performer == null || _auto == null) return;
            var layer = _performer.GetComponent<NihongoLife.Gestures.GestureLayer>();
            if (layer != null) layer.SetBpm(_auto.tempo_bpm * _tempo);
        }

        private void StopGesture()
        {
            if (_performer != null) NihongoLife.Gestures.WorkGestures.Stop(_performer);
        }

        /// <summary>Practise a melody on the piano: the keyboard is moved so the song fits, the next key glows.</summary>
        public static void PractiseSong(string songId)
        {
            var song = MusicCatalog.Song(songId);
            if (song == null) return;
            var ui = Instance;
            ui._auto = null;
            ui._autoFinished = false;
            ui.StopGesture();
            ui.SetMini(false);
            ui._result = null;
            ui._practice = song;
            ui._step = 0;
            ui._mistakes = 0;
            int low = song.notes.Length > 0 ? song.notes.Min(n => n.midi) : 60;
            ui._base = Mathf.Clamp(low - ((low % 12) + 12) % 12, 36, 84);
            Open(Tab.Piano);
        }

        private IEnumerator PlaySong(SongDefinition song)
        {
            float spb = song.SecondsPerBeat;
            float start = Time.unscaledTime;
            foreach (var note in song.notes.OrderBy(n => n.start))
            {
                float at = start + note.start * spb;
                while (Time.unscaledTime < at) yield return null;
                _audio.PlayOneShot(InstrumentSynth.Clip(song.instrument == "guitar" ? InstrumentSynth.Voice.Guitar : InstrumentSynth.Voice.Piano, note.midi));
                if (_pianoKeys.TryGetValue(note.midi, out var image)) StartCoroutine(Flash(image, note.midi));
            }
        }

        // ─────────── Guitar ───────────

        private void BuildGuitar()
        {
            var progressions = MusicCatalog.Progressions;
            if (progressions.Length == 0) return;
            _progression = (_progression % progressions.Length + progressions.Length) % progressions.Length;
            var p = progressions[_progression];
            _chord ??= p.chords.FirstOrDefault();
            NLUi.Label(_body, "Title", $"{p.name}  <size=70%><color=#A8B4C4>{p.roman} · {p.key} · {p.genre}</color></size>", 24f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_body, "Help", Loc.T("Phím 1–4 chọn hợp âm của vòng hợp âm, Space để quạt dây. Bấm hợp âm để nghe.",
                "Keys 1–4 pick a chord of the progression, Space strums. Click a chord to hear it.",
                "1〜4でコード、スペースでストローク。"), 15f, NLUi.Muted, _font);
            var row = NLUi.Group(_body, "Progression", false, 8f, TextAnchor.MiddleLeft, false);
            NLUi.Button(row, "Prev", "<", _font, () => { _progression--; _chord = null; Rebuild(); }, NLUi.Card, 20f, NLUi.Text, 48f);
            for (int i = 0; i < p.chords.Length; i++)
            {
                string name = p.chords[i];
                var b = NLUi.Button(row, "Chord_" + name, $"{i + 1}  {name}", _font, () => { _chord = name; Strum(name); Rebuild(); }, name == _chord ? NLUi.Gold : NLUi.Card, 20f, name == _chord ? NLUi.Ink : NLUi.Text, 48f);
                NLUi.Size(b, 110f, 48f);
            }
            NLUi.Button(row, "Next", ">", _font, () => { _progression++; _chord = null; Rebuild(); }, NLUi.Card, 20f, NLUi.Text, 48f);
            var chord = MusicCatalog.Chord(_chord);
            if (chord != null)
            {
                string[] strings = { "E", "A", "D", "G", "B", "e" };
                var lines = new List<string>();
                for (int s = 5; s >= 0; s--)
                {
                    int fret = chord.frets[s];
                    lines.Add($"{strings[s]} |" + (fret < 0 ? "  x" : fret == 0 ? "  o" : $"  {fret}") + (chord.fingers.Length > s && chord.fingers[s] > 0 ? $"  ({Loc.T("ngón", "finger", "指")} {chord.fingers[s]})" : ""));
                }
                var card = NLUi.Panel(_body, "Diagram", NLUi.Card, new RectOffset(18, 18, 12, 14), 4f);
                NLUi.Label(card, "Name", chord.name + $"  <size=70%><color=#A8B4C4>{chord.category}</color></size>", 26f, NLUi.Gold, _font, FontStyles.Bold);
                NLUi.Label(card, "Frets", "<mspace=0.6em>" + string.Join("\n", lines) + "</mspace>", 18f, NLUi.Text, _font);
                NLUi.Button(_body, "Strum", Loc.T("Quạt dây (Space)", "Strum (Space)", "ストローク（Space）"), _font, () => Strum(_chord), NLUi.Gold, 18f, NLUi.Ink, 48f);
            }
            NLUi.Label(_body, "AllTitle", Loc.T("Tất cả hợp âm", "All chords", "すべてのコード"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            var grid = new GameObject("AllChords", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            grid.SetParent(_body, false);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(96f, 44f); layout.spacing = new Vector2(6f, 6f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 10;
            NLUi.FitContent(grid);
            foreach (var c in MusicCatalog.Chords)
            {
                string name = c.name;
                NLUi.Button(grid, "Any_" + name, name, _font, () => { _chord = name; Strum(name); Rebuild(); }, name == _chord ? NLUi.Gold : NLUi.Card, 15f, name == _chord ? NLUi.Ink : NLUi.Text, 44f);
            }
        }

        /// <summary>Strums a chord: its strings from low to high, a few milliseconds apart.</summary>
        public static void Strum(string chordName)
        {
            var chord = MusicCatalog.Chord(chordName);
            if (chord == null) return;
            Instance.StartCoroutine(Instance.StrumRoutine(chord));
        }

        private IEnumerator StrumRoutine(GuitarChord chord)
        {
            foreach (int midi in chord.midi)
            {
                if (midi < 0) continue;
                _audio.PlayOneShot(InstrumentSynth.Clip(InstrumentSynth.Voice.Guitar, midi), 0.8f);
                yield return new WaitForSecondsRealtime(0.022f);
            }
        }

        // ─────────── Songs ───────────

        private void BuildSongs()
        {
            if (_street)
            {
                NLUi.Label(_body, "Title", Loc.T("Biểu diễn đường phố — chọn bài", "Busking — pick a song", "ストリートライブ — 曲を えらぶ"), 24f, NLUi.Gold, _font, FontStyles.Bold);
                NLUi.Label(_body, "StreetHelp", Loc.F("Nhân vật đứng đàn bằng cây guitar của bạn. Hết bài, người qua đường trong khoảng 12 m cho tiền thưởng nếu nhịp {0}–{1}% — càng đông người nghe càng nhiều. Đứng gần ga, quảng trường hay khu mua sắm nhé.",
                    "Your character plays your guitar where you stand. At the end of a song people within about 12 m tip you if the tempo is {0}–{1}% — the bigger the crowd, the more. Try the station, the plaza or the shops.",
                    "その場で ギターを ひきます。テンポ {0}〜{1}% なら 12m 以内の 人が チップを くれます。", Mathf.RoundToInt(GoodSlow * 100f), Mathf.RoundToInt(GoodFast * 100f)), 15f, NLUi.Muted, _font);
            }
            else if (_stage)
            {
                NLUi.Label(_body, "Title", Loc.T("Sân khấu Hibiki — chọn bài để biểu diễn", "The Hibiki stage — pick a song to perform", "ひびきステージ — 演奏する曲を えらぶ"), 24f, NLUi.Gold, _font, FontStyles.Bold);
                NLUi.Label(_body, "StageHelp", Loc.F("Nhân vật tự đàn cho khán giả. Giữ nhịp {0}–{1}% bản gốc thì khán giả thích: +1 người hâm mộ và tính một buổi diễn. Người hâm mộ: {2}.",
                    "Your character plays for the audience. Keep the tempo at {0}–{1}% of the original and they love it: +1 fan and the show counts as a gig. Fans: {2}.",
                    "キャラクターが演奏します。テンポ {0}〜{1}% なら お客さんが よろこびます。ファン：{2}人", Mathf.RoundToInt(GoodSlow * 100f), Mathf.RoundToInt(GoodFast * 100f), Fans), 15f, NLUi.Muted, _font);
            }
            else
            {
                NLUi.Label(_body, "Title", Loc.T("Bài hát — nhân vật tự đàn", "Songs — your character plays them", "曲 — キャラクターが演奏"), 24f, NLUi.Gold, _font, FontStyles.Bold);
                NLUi.Label(_body, "SongsHelp", Loc.T("Chọn bài: nhân vật đàn, bạn chỉnh nhịp nhanh / chậm, âm lượng, piano hay guitar. Muốn tự đàn thì bấm «Tự đàn bằng bàn phím».",
                    "Pick a song: your character plays it while you set the tempo, the volume, piano or guitar. To play it yourself, use “Play it with the keyboard”.",
                    "曲をえらぶと キャラクターが演奏。テンポ・音量・楽器を かえられます。"), 15f, NLUi.Muted, _font);
            }
            foreach (var song in MusicCatalog.Songs.OrderBy(s => s.difficulty).ThenBy(s => s.Title))
            {
                var s = song;
                var card = NLUi.Panel(_body, "Song_" + s.id, NLUi.Card, new RectOffset(18, 18, 10, 12), 6f);
                int best = Best(s.id);
                string mark = best >= LearntScore ? $"  <color=#7CE0A0>✓ {best}%</color>" : best > 0 ? $"  <color=#E0C070>{best}%</color>" : "";
                NLUi.Label(card, "Head", $"<b>{s.Title}</b>{mark}  <size=75%><color=#A8B4C4>{s.composer} · {s.key} · {s.tempo_bpm:0} bpm · {new string('★', Mathf.Clamp(s.difficulty, 1, 5))}</color></size>", 18f, NLUi.Text, _font);
                var row = NLUi.Group(card, "Actions", false, 8f, TextAnchor.MiddleLeft, false);
                if (_stage || _street) NLUi.Button(row, "Perform", "▶ " + Loc.T("Biểu diễn", "Perform", "演奏する"), _font, () => Perform(s.id), NLUi.Gold, 15f, NLUi.Ink, 40f);
                else NLUi.Button(row, "AutoPlay", "▶ " + Loc.T("Nhân vật đàn bài này", "Play this song", "この曲を演奏"), _font, () => AutoPlay(s.id), NLUi.Gold, 15f, NLUi.Ink, 40f);
                NLUi.Button(row, "Practise", Loc.T("Tự đàn bằng bàn phím", "Play it with the keyboard", "キーボードで弾く"), _font, () => PractiseSong(s.id), NLUi.CardHover, 15f, NLUi.Text, 40f);
            }
            NLUi.Label(_body, "Licence", Loc.T("Giai điệu là tác phẩm thuộc phạm vi công cộng; dữ liệu nốt do NihongoLife soạn (CC0).", "The melodies are public-domain works; the note data is NihongoLife's own (CC0).", "曲はパブリックドメイン、音符データはCC0。"), 13f, NLUi.Muted, _font);
        }

        // ─────────── Theory ───────────

        private static HashSet<string> TheoryDone()
        {
            var raw = PlayerPrefs.GetString(TheoryKey, "");
            return new HashSet<string>(raw.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        private void BuildTheory()
        {
            var lessons = MusicCatalog.Lessons();
            var done = TheoryDone();
            var lesson = lessons.FirstOrDefault(l => l.id == _lesson);
            if (lesson == null)
            {
                NLUi.Label(_body, "Title", Loc.F("Nhạc lý — {0}/{1} bài đã học", "Music theory — {0}/{1} lessons done", "音楽理論 — {0}/{1}", done.Count(d => lessons.Any(l => l.id == d)), lessons.Length), 24f, NLUi.Gold, _font, FontStyles.Bold);
                foreach (var l in lessons.OrderBy(l => l.order))
                {
                    var item = l;
                    NLUi.Button(_body, "Lesson_" + l.id, $"{(done.Contains(l.id) ? "✓ " : "")}{l.order}. {l.title}  <size=75%><color=#A8B4C4>{l.level} · {l.minutes}′</color></size>", _font,
                        () => { _lesson = item.id; _question = 0; Rebuild(); }, done.Contains(l.id) ? new Color(0.16f, 0.3f, 0.22f) : NLUi.Card, 17f, NLUi.Text, 46f);
                }
                return;
            }
            NLUi.Button(_body, "Back", "< " + Loc.T("Danh sách bài", "All lessons", "一覧へ"), _font, () => { _lesson = null; Rebuild(); }, NLUi.Card, 15f, NLUi.Text, 40f);
            NLUi.Label(_body, "Title", $"{lesson.order}. {lesson.title}", 26f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_body, "Intro", lesson.intro, 17f, NLUi.Text, _font);
            NLUi.Label(_body, "Points", string.Join("\n", lesson.points.Select(p => "• " + p)), 16f, NLUi.Text, _font).lineSpacing = 6f;
            if (_question < lesson.quiz.Length)
            {
                var q = lesson.quiz[_question];
                var card = NLUi.Panel(_body, "Quiz", NLUi.Card, new RectOffset(18, 18, 12, 14), 8f);
                NLUi.Label(card, "Question", Loc.F("Câu {0}/{1}: ", "Question {0}/{1}: ", "問{0}/{1}：", _question + 1, lesson.quiz.Length) + q.q, 18f, NLUi.Text, _font, FontStyles.Bold);
                var feedback = NLUi.Label(card, "Feedback", "", 15f, NLUi.Soft, _font);
                for (int i = 0; i < q.options.Length; i++)
                {
                    int choice = i;
                    NLUi.Button(card, "Option_" + i, q.options[i], _font, () => Answer(lesson, q, choice, feedback), NLUi.CardHover, 16f, NLUi.Text, 42f);
                }
            }
            else NLUi.Label(_body, "Done", Loc.T("Đã hoàn thành bài này!", "Lesson complete!", "このレッスンは完了！"), 20f, NLUi.Good, _font, FontStyles.Bold);
            if (lesson.practiceSongs.Length > 0 && MusicCatalog.Song(lesson.practiceSongs[0]) != null)
                NLUi.Button(_body, "PractiseSong", Loc.F("Tập bài: {0}", "Practise: {0}", "練習：{0}", MusicCatalog.Song(lesson.practiceSongs[0]).Title), _font, () => PractiseSong(lesson.practiceSongs[0]), NLUi.Gold, 16f, NLUi.Ink, 44f);
        }

        /// <summary>Answers the current question of a lesson; the last right answer completes the lesson.</summary>
        public static bool AnswerCurrent(int choice)
        {
            var ui = Instance;
            var lesson = MusicCatalog.Lessons().FirstOrDefault(l => l.id == ui._lesson);
            if (lesson == null || ui._question >= lesson.quiz.Length) return false;
            return ui.Answer(lesson, lesson.quiz[ui._question], choice, null);
        }

        public static void OpenLesson(string lessonId) { Instance._lesson = lessonId; Instance._question = 0; Open(Tab.Theory); }

        /// <summary>A painting's card (from the gallery wall in the music shop): the picture, artist, date, museum, licence.</summary>
        public static void ShowPainting(string artworkId)
        {
            var art = MusicCatalog.ArtworkById(artworkId);
            if (art == null) return;
            Instance._painting = art;
            Open(Tab.Gallery);
        }

        public static string ShownPainting => _instance != null && _instance._painting != null && IsOpen && _instance._tab == Tab.Gallery ? _instance._painting.id : null;

        private bool Answer(TheoryLesson lesson, TheoryQuestion q, int choice, TextMeshProUGUI feedback)
        {
            if (choice != q.answer)
            {
                if (feedback != null) feedback.text = Loc.T("Chưa đúng — ", "Not quite — ", "ちがいます — ") + q.explain;
                return false;
            }
            _question++;
            if (_question >= lesson.quiz.Length)
            {
                var done = TheoryDone();
                if (done.Add(lesson.id))
                {
                    PlayerPrefs.SetString(TheoryKey, string.Join(",", done));
                    var status = PlayerStatus.Instance;
                    if (status != null) status.AddKnowledge(5);
                }
            }
            Rebuild();
            return true;
        }

        public static bool IsLessonDone(string lessonId) => TheoryDone().Contains(lessonId);

        // ─────────── Gallery ───────────

        private void BuildGallery()
        {
            if (_painting != null)
            {
                var a = _painting;
                NLUi.Button(_body, "Back", "< " + Loc.T("Phòng tranh", "Gallery", "ギャラリー"), _font, () => { _painting = null; Rebuild(); }, NLUi.Card, 15f, NLUi.Text, 40f);
                var frame = new GameObject("Painting", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement)).GetComponent<RawImage>();
                frame.transform.SetParent(_body, false);
                var tex = a.Picture;
                frame.texture = tex;
                float aspect = tex != null ? tex.width / (float)tex.height : 1f;
                frame.GetComponent<LayoutElement>().preferredHeight = 420f;
                frame.GetComponent<LayoutElement>().preferredWidth = 420f * aspect;
                var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth; fitter.aspectRatio = aspect;
                NLUi.Label(_body, "Title", $"<b>{a.title}</b>", 24f, NLUi.Gold, _font);
                NLUi.Label(_body, "Info", $"{a.artist} · {a.date}\n{a.medium}\n{a.museum}", 16f, NLUi.Text, _font);
                NLUi.Label(_body, "Licence", a.licence + (string.IsNullOrEmpty(a.credit) ? "" : " — " + a.credit), 13f, NLUi.Muted, _font);
                return;
            }
            NLUi.Label(_body, "Title", Loc.T("Phòng tranh — kiệt tác thuộc phạm vi công cộng", "Gallery — public-domain masterpieces", "ギャラリー — パブリックドメインの名画"), 24f, NLUi.Gold, _font, FontStyles.Bold);
            var grid = new GameObject("Paintings", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            grid.SetParent(_body, false);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(200f, 230f); layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 5;
            NLUi.FitContent(grid);
            foreach (var art in MusicCatalog.Artworks)
            {
                var a = art;
                var cell = NLUi.Panel(grid, "Art_" + a.id, NLUi.Card);
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.GetComponent<Image>();
                button.onClick.AddListener(() => { _painting = a; Rebuild(); });
                var picture = new GameObject("Thumb", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                picture.transform.SetParent(cell, false);
                picture.texture = a.Picture;
                picture.raycastTarget = false;
                var r = picture.rectTransform;
                r.anchorMin = new Vector2(0f, 0.28f); r.anchorMax = new Vector2(1f, 1f); r.offsetMin = new Vector2(8f, 0f); r.offsetMax = new Vector2(-8f, -8f);
                var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = a.Picture != null ? a.Picture.width / (float)a.Picture.height : 1f;
                var label = NLUi.Label(cell, "Name", $"<b>{a.title}</b>\n<size=80%>{a.artist}</size>", 12f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Top);
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = new Vector2(1f, 0.28f);
                label.rectTransform.offsetMin = new Vector2(6f, 4f); label.rectTransform.offsetMax = new Vector2(-6f, -2f);
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        // ─────────── The computer keyboard ───────────

        private void Update()
        {
            if (!IsOpen) return;
            if (_auto != null) TickAuto(Time.unscaledDeltaTime);
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (_auto != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) Tempo -= 0.1f;
                if (keyboard.rightArrowKey.wasPressedThisFrame) Tempo += 0.1f;
                if (keyboard.downArrowKey.wasPressedThisFrame) Volume -= 0.1f;
                if (keyboard.upArrowKey.wasPressedThisFrame) Volume += 0.1f;
            }
            if (Mini) return;
            if (_tab == Tab.Piano)
            {
                for (int i = 0; i < WhiteKeys.Length; i++) if (keyboard[WhiteKeys[i]].wasPressedThisFrame) Press(_base + WhiteSemitones[i]);
                foreach (var (key, semitone) in BlackKeys) if (keyboard[key].wasPressedThisFrame) Press(_base + semitone);
                if (keyboard.zKey.wasPressedThisFrame) { _base = Mathf.Max(36, _base - 12); Rebuild(); }
                if (keyboard.xKey.wasPressedThisFrame) { _base = Mathf.Min(84, _base + 12); Rebuild(); }
            }
            else if (_tab == Tab.Guitar)
            {
                var progressions = MusicCatalog.Progressions;
                if (progressions.Length > 0)
                {
                    var p = progressions[(_progression % progressions.Length + progressions.Length) % progressions.Length];
                    Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
                    for (int i = 0; i < digits.Length && i < p.chords.Length; i++)
                        if (keyboard[digits[i]].wasPressedThisFrame) { _chord = p.chords[i]; Strum(_chord); Rebuild(); }
                }
                if (keyboard.spaceKey.wasPressedThisFrame && _chord != null) Strum(_chord);
            }
        }
    }
}
