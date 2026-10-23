using System.Collections.Generic;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Weather on top of DayNightCycle (same AppRoot, same clock): each in-game day is clear, overcast, or has one
    /// rain spell of 1.5-4 hours, fading in and out over half an hour. OutdoorSky turns it into sky, light, fog and
    /// the rain itself; this component owns the gameplay side:
    /// · the player standing in the rain recovers energy at half speed (PlayerStatus reads PlayerInRain);
    /// · NPCs standing outdoors are hidden while it rains and come back afterwards — except the one the current
    ///   story step needs and anyone in a conversation, so rain never blocks the story;
    /// · the HUD says when it starts and stops.
    /// </summary>
    public sealed class WeatherCycle : MonoBehaviour
    {
        public enum Kind { Clear, Overcast, Rain }

        public static WeatherCycle Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] private float rainChance = 0.35f;
        [SerializeField, Range(0f, 1f)] private float overcastChance = 0.3f;
        [SerializeField] private int seed = 20261011;

        private DayNightCycle _cycle;
        private int _plannedDay = -1;
        private Kind _dayKind;
        private float _rainStart, _rainEnd;
        private Kind? _forced;
        private float _forcedBlend;
        private bool _wasRaining;
        private float _nextNpcCheck;
        private readonly List<GameObject> _hiddenNpcs = new();

        /// <summary>0 clear sky … 1 fully overcast.</summary>
        public float Overcast { get; private set; }
        /// <summary>0 dry … 1 heavy rain.</summary>
        public float Rain { get; private set; }
        /// <summary>0 … 1: a rainbow after a daytime shower (opposite the sun), fading in and out.</summary>
        public float Rainbow { get; private set; }
        private float _rainbowUntilHour = -1f;
        private float _rainbowFrom = -1f;
        private float _rainbowScheduledUntil = -1f;
        private bool _rainbowForced;
        [SerializeField, Range(0f, 1f)] private float rainbowChance = 0.6f;

        /// <summary>Tests/debug: show (or hide) a rainbow now, for one in-game hour.</summary>
        public void ForceRainbow(bool on)
        {
            _rainbowForced = on;
            _rainbowUntilHour = on && _cycle != null ? _cycle.Hour + 1f : -1f;
            if (!on) Rainbow = 0f;
        }
        public bool IsRaining => Rain > 0.15f;
        public Kind Today => _forced ?? _dayKind;
        public int HiddenNpcCount => _hiddenNpcs.Count;
        /// <summary>The player is outdoors (no roof, not inside a shop) while it rains.</summary>
        public static bool PlayerInRain { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        /// <summary>Forces a weather (tests, debug, story beats); null returns to the daily plan.</summary>
        public void Force(Kind? kind, bool instant = true)
        {
            _forced = kind;
            _forcedBlend = instant ? 1f : 0f;
            Evaluate();
            _cycle?.SetHour(_cycle.Hour);
        }

        private void Update()
        {
            if (_cycle == null) _cycle = GetComponent<DayNightCycle>().OrNull() ?? SceneRefs.Find<DayNightCycle>();
            if (_forced.HasValue && _forcedBlend < 1f) _forcedBlend = Mathf.Min(1f, _forcedBlend + Time.deltaTime / 4f);
            Evaluate();
            UpdatePlayerExposure();
            if (Time.unscaledTime >= _nextNpcCheck)
            {
                _nextNpcCheck = Time.unscaledTime + 1f;
                UpdateNpcs();
            }
            UpdateRainbow();
            if (IsRaining != _wasRaining)
            {
                // A shower that ends in daylight often leaves a rainbow behind.
                float hourNow = _cycle != null ? _cycle.Hour : 12f;
                if (!IsRaining && hourNow >= 8f && hourNow <= 17f && Random.value < rainbowChance) _rainbowUntilHour = hourNow + 1f;
                _wasRaining = IsRaining;
                NihongoLife.UI.HudFeed.Post(IsRaining
                    ? NihongoLife.Core.Loc.T("Trời bắt đầu mưa — mọi người ngoài phố đã vào trú. Đứng ngoài mưa bạn hồi thể lực chậm hơn.", "It's starting to rain — people outside have taken shelter. Standing in the rain slows your stamina recovery.", "雨が降ってきました。外の人は雨宿りしています。雨の中では元気の回復が遅くなります。")
                    : NihongoLife.Core.Loc.T("Trời tạnh mưa.", "The rain has stopped.", "雨がやみました。"), IsRaining ? NihongoLife.UI.HudFeed.Kind.Warning : NihongoLife.UI.HudFeed.Kind.Info, 4f, "weather");
            }
        }

        private void UpdateRainbow()
        {
            float hour = _cycle != null ? _cycle.Hour : 12f;
            bool postRain = _rainbowUntilHour > 0f && hour < _rainbowUntilHour;
            bool scheduled = _rainbowFrom >= 0f && hour >= _rainbowFrom && hour < _rainbowScheduledUntil;
            bool active = (postRain || scheduled) && hour >= 7.5f && hour <= 18f && Rain < 0.35f;
            if (!postRain && !_rainbowForced) _rainbowUntilHour = -1f;
            float target = active || (_rainbowForced && _rainbowUntilHour > 0f) ? 1f : 0f;
            Rainbow = Mathf.MoveTowards(Rainbow, target, Time.deltaTime / 6f);
        }

        private void Evaluate()
        {
            float hour = _cycle != null ? _cycle.Hour : 12f;
            int day = _cycle != null ? _cycle.Day : 0;
            if (day != _plannedDay) PlanDay(day);
            float overcast, rain;
            if (_forced.HasValue)
            {
                overcast = _forced == Kind.Clear ? 0f : _forced == Kind.Overcast ? 0.65f : 1f;
                rain = _forced == Kind.Rain ? 1f : 0f;
                overcast *= _forcedBlend; rain *= _forcedBlend;
            }
            else
            {
                overcast = _dayKind == Kind.Overcast ? 0.6f : 0f;
                rain = 0f;
                if (_dayKind == Kind.Rain)
                {
                    // Clouds build an hour before the rain and clear an hour after; the rain itself fades over 30 min.
                    float clouds = Mathf.Clamp01(Mathf.Min(hour - (_rainStart - 1f), (_rainEnd + 1f) - hour));
                    rain = Mathf.Clamp01(Mathf.Min(hour - _rainStart, _rainEnd - hour) / 0.5f);
                    overcast = Mathf.Max(0.35f, clouds);
                }
            }
            Overcast = overcast;
            Rain = rain;
        }

        private void PlanDay(int day)
        {
            _plannedDay = day;
            var random = new System.Random(seed + day * 7919);
            double roll = random.NextDouble();
            _dayKind = roll < rainChance ? Kind.Rain : roll < rainChance + overcastChance ? Kind.Overcast : Kind.Clear;
            _rainStart = 8f + (float)random.NextDouble() * 12f;
            _rainEnd = _rainStart + 1.5f + (float)random.NextDouble() * 2.5f;
            // A clear or overcast day sometimes puts a rainbow up on its own, not only after rain.
            _rainbowFrom = -1f;
            _rainbowScheduledUntil = -1f;
            if (_dayKind != Kind.Rain && random.NextDouble() < 0.22)
            {
                _rainbowFrom = 9f + (float)random.NextDouble() * 6f;
                _rainbowScheduledUntil = _rainbowFrom + 1.1f;
            }
        }

        // ─────────── Gameplay ───────────

        /// <summary>Under a roof (anything solid overhead) or inside a shop's indoor zone.</summary>
        public static bool IsSheltered(Vector3 position)
        {
            if (NihongoLife.Cameras.StoreCameraZone.Contains(position)) return true;
            return Physics.Raycast(position + Vector3.up * 2.1f, Vector3.up, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void UpdatePlayerExposure()
        {
            var outdoors = OutdoorSky.Active;
            var player = PlayerStatus.Instance;
            PlayerInRain = IsRaining && outdoors != null && outdoors.DrivesAmbientAndFog && player != null && !IsSheltered(player.transform.position);
        }

        private void UpdateNpcs()
        {
            var outdoors = OutdoorSky.Active;
            bool hide = IsRaining && outdoors != null && outdoors.DrivesAmbientAndFog;
            if (!hide)
            {
                foreach (var npc in _hiddenNpcs) if (npc != null) npc.SetActive(true);
                _hiddenNpcs.Clear();
                return;
            }
            var node = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentNode : null;
            string needed = node == null ? null : node.nodeType == ScenarioNodeType.TalkToNPC ? node.targetNpcId : node.speakerId;
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
            {
                if (npc.gameObject.scene != outdoors.gameObject.scene || npc.IsInteracting) continue;
                if (!string.IsNullOrEmpty(needed) && npc.NpcId == needed) continue;
                if (IsSheltered(npc.transform.position)) continue;
                npc.gameObject.SetActive(false);
                _hiddenNpcs.Add(npc.gameObject);
            }
            // The story moved on to someone who is hidden: bring them back right away.
            for (int i = _hiddenNpcs.Count - 1; i >= 0; i--)
            {
                var go = _hiddenNpcs[i];
                if (go == null) { _hiddenNpcs.RemoveAt(i); continue; }
                var npc = go.GetComponent<NPCController>();
                if (npc != null && !string.IsNullOrEmpty(needed) && npc.NpcId == needed) { go.SetActive(true); _hiddenNpcs.RemoveAt(i); }
            }
        }

        private void OnDisable()
        {
            foreach (var npc in _hiddenNpcs) if (npc != null) npc.SetActive(true);
            _hiddenNpcs.Clear();
            PlayerInRain = false;
        }
    }
}
