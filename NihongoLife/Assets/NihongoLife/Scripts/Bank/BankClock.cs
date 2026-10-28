using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Progression;
using UnityEngine;

namespace NihongoLife.Bank
{
    /// <summary>Once a second in gameplay: loan reminders, overdue notices and late fees (BankService.Tick).</summary>
    public sealed class BankClock : MonoBehaviour
    {
        private static BankClock _instance;
        private float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("BankClock");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<BankClock>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            var cycle = DayNightCycle.Active;
            if (cycle == null || !CareerClock.InGameplay) return;
            BankService.Tick(cycle.Day);
            if (PlayerStatus.Instance != null) BankService.HungerTip(cycle.Day, PlayerStatus.Instance.Hunger);
        }
    }
}
