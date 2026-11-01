using System;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Learning;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Owner "max settings" test preset: every online/chat/voice feature on, highest graphics and audio, debug overlay.
    /// Toggle: Settings → "Max test preset", or Ctrl+Shift+F9 anywhere, or launch with -nlmax. Turning it off restores
    /// the exact values the player had before.
    /// </summary>
    public static class OwnerMaxPreset
    {
        public const string ActiveKey = "NL.MaxPreset.Active";
        public const string BackupKey = "NL.MaxPreset.Backup";
        public const string OverlayKey = "NL.MaxPreset.Overlay";

        public static event Action Changed;

        [Serializable]
        public class Snapshot
        {
            public float bgm = 0.5f, sfx = 0.8f, ambience = 0.5f;
            public bool musicOn = true, ambienceOn = true, reduceMotion, selfMuted, openMic;
            public int mic = 100, speaker = 100, quality = -1, webPreset = 1, vSync = 1, targetFps = -1;
        }

        public static bool Active => PlayerPrefs.GetInt(ActiveKey, 0) == 1;
        public static bool OverlayOn => PlayerPrefs.GetInt(OverlayKey, 0) == 1;

        public static Snapshot Capture() => new Snapshot
        {
            bgm = PlayerPrefs.GetFloat("Volume_BGM", 0.5f),
            sfx = PlayerPrefs.GetFloat("Volume_SFX", 0.8f),
            ambience = AudioPrefs.AmbienceVolume,
            musicOn = AudioPrefs.MusicOn,
            ambienceOn = AudioPrefs.AmbienceOn,
            reduceMotion = MotionSettings.ReduceMotion,
            selfMuted = VoicePrefs.SelfMuted,
            openMic = VoicePrefs.OpenMic,
            mic = VoicePrefs.MicVolume,
            speaker = VoicePrefs.SpeakerVolume,
            quality = QualitySettings.GetQualityLevel(),
            webPreset = WebGLGraphicsSettings.Preset,
            vSync = QualitySettings.vSyncCount,
            targetFps = Application.targetFrameRate
        };

        /// <summary>The values the preset applies (graphics fields only used when graphics is true).</summary>
        public static Snapshot Max() => new Snapshot
        {
            bgm = 1f, sfx = 1f, ambience = 1f, musicOn = true, ambienceOn = true, reduceMotion = false,
            selfMuted = false, openMic = true, mic = 100, speaker = 100,
            quality = QualitySettings.names.Length - 1, webPreset = 2, vSync = 1, targetFps = -1
        };

        public static void Toggle() => SetActive(!Active);

        public static void SetActive(bool on, bool graphics = true)
        {
            if (on == Active) return;
            if (on)
            {
                PlayerPrefs.SetString(BackupKey, JsonUtility.ToJson(Capture()));
                Write(Max(), graphics);
                PlayerPrefs.SetInt(OverlayKey, 1);
            }
            else
            {
                string json = PlayerPrefs.GetString(BackupKey, "");
                Snapshot back = string.IsNullOrEmpty(json) ? new Snapshot() : JsonUtility.FromJson<Snapshot>(json);
                Write(back, graphics);
                PlayerPrefs.DeleteKey(BackupKey);
                PlayerPrefs.SetInt(OverlayKey, 0);
            }
            PlayerPrefs.SetInt(ActiveKey, on ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ToggleOverlay()
        {
            PlayerPrefs.SetInt(OverlayKey, OverlayOn ? 0 : 1);
            Changed?.Invoke();
        }

        static void Write(Snapshot s, bool graphics)
        {
            PlayerPrefs.SetFloat("Volume_BGM", s.bgm);
            PlayerPrefs.SetFloat("Volume_SFX", s.sfx);
            if (GameServices.TryGet(out IAudioService audio)) { audio.SetBGMVolume(s.bgm); audio.SetSFXVolume(s.sfx); }
            AudioPrefs.AmbienceVolume = s.ambience;
            AudioPrefs.MusicOn = s.musicOn;
            AudioPrefs.AmbienceOn = s.ambienceOn;
            MotionSettings.ReduceMotion = s.reduceMotion;
            VoicePrefs.SelfMuted = s.selfMuted;
            VoicePrefs.OpenMic = s.openMic;
            VoicePrefs.MicVolume = s.mic;
            VoicePrefs.SpeakerVolume = s.speaker;
            if (!graphics) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLGraphicsSettings.SetPreset(s.webPreset);
#else
            if (s.quality >= 0 && s.quality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(s.quality, true);
            QualitySettings.vSyncCount = s.vSync;
            Application.targetFrameRate = s.targetFps;
#endif
        }

        public static string Label() => Loc.T(
            Active ? "Chế độ test tối đa: Bật" : "Chế độ test tối đa: Tắt",
            Active ? "Max test preset: On" : "Max test preset: Off",
            Active ? "最大テスト設定: オン" : "最大テスト設定: オフ");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-nlmax") >= 0) SetActive(true);
            var go = UiBind.Make("OwnerDebugOverlay");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.Need<OwnerDebugOverlay>();
        }
    }

    /// <summary>Small IMGUI corner panel: FPS, quality, voice and network state. Ctrl+Shift+F9 = preset, F9 = overlay.</summary>
    public class OwnerDebugOverlay : MonoBehaviour
    {
        float _fps, _acc; int _frames; GUIStyle _style;

        void Update()
        {
            _acc += Time.unscaledDeltaTime; _frames++;
            if (_acc >= 0.5f) { _fps = _frames / _acc; _acc = 0f; _frames = 0; }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            bool ctrl = kb.ctrlKey.isPressed;
            bool shift = kb.shiftKey.isPressed;
            if (kb.f9Key.wasPressedThisFrame)
            {
                if (ctrl && shift) OwnerMaxPreset.Toggle();
                else if (OwnerMaxPreset.Active) OwnerMaxPreset.ToggleOverlay();
            }
        }

        void OnGUI()
        {
            if (!OwnerMaxPreset.OverlayOn) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = true };
            var v = VoiceChatService.Instance;
            string voice = v == null ? "-" : $"{v.Status}{(v.Reconnecting ? " (reconnecting)" : "")}{(v.LocalSpeaking ? " ●" : "")}";
            string q = QualitySettings.names.Length > 0 ? QualitySettings.names[QualitySettings.GetQualityLevel()] : "?";
            string text = $"<b>MAX TEST</b>  {_fps:0} fps\nQuality: {q}  {Screen.width}x{Screen.height}\n" +
                          $"Net: {Application.internetReachability}\nVoice: {voice}\n" +
                          $"Mic: {(VoicePrefs.OpenMic ? "open" : "PTT")}{(VoicePrefs.SelfMuted ? " muted" : "")} {VoicePrefs.MicVolume}% / spk {VoicePrefs.SpeakerVolume}%\n" +
                          $"Mem: {GC.GetTotalMemory(false) / (1024 * 1024)} MB   F9 hide · Ctrl+Shift+F9 off";
            GUI.Box(new Rect(8, 8, 330, 108), text, _style);
        }
    }
}
