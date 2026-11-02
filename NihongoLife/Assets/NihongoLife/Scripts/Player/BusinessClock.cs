using NihongoLife.Core;
using NihongoLife.Progression;
using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>Runs the company's books in gameplay (BusinessLedger.Tick): staff pay and weekly costs need no button.</summary>
    public sealed class BusinessClock : MonoBehaviour
    {
        private static BusinessClock _instance;
        private float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("BusinessClock");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            _instance = go.AddComponent<BusinessClock>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 2f;
            var cycle = DayNightCycle.Active;
            if (cycle == null || !CareerClock.InGameplay) return;
            BusinessLedger.Tick(BusinessSystem.Instance, BusinessWorkers.Today());
        }
    }
}
