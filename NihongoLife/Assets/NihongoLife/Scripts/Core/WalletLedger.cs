using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Every change of the wallet with its reason (shop, clinic fee, quest reward, ticket…). The HUD shows each one
    /// flying off the yen chip; a click on the chip lists the last ones. Kept between sessions (PlayerPrefs, last 30).
    /// </summary>
    public static class WalletLedger
    {
        [Serializable] public struct Entry { public int amount; public string reason; public int balance; public long ticks; }
        [Serializable] private sealed class Store { public List<Entry> entries = new List<Entry>(); }

        private const string Key = "NL.WalletLedger.v1";
        private const int Keep = 30;
        private static Store _store;

        public static event Action<Entry> Recorded;

        public static IReadOnlyList<Entry> Entries { get { Load(); return _store.entries; } }

        public static void Record(int amount, string reason, int balance)
        {
            if (amount == 0) return;
            Load();
            var e = new Entry { amount = amount, reason = string.IsNullOrWhiteSpace(reason) ? "Khác" : reason, balance = balance, ticks = DateTime.Now.Ticks };
            _store.entries.Insert(0, e);
            if (_store.entries.Count > Keep) _store.entries.RemoveRange(Keep, _store.entries.Count - Keep);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_store));
            Recorded?.Invoke(e);
        }

        private static void Load()
        {
            if (_store != null) return;
            try { _store = JsonUtility.FromJson<Store>(PlayerPrefs.GetString(Key, "")); } catch (Exception) { _store = null; }
            if (_store == null || _store.entries == null) _store = new Store();
        }
    }
}
