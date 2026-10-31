using System;
using System.Collections.Generic;

namespace NihongoLife.Core
{
    /// <summary>
    /// "Typing…" for live chat: who is typing in which conversation, expiring on its own when the signals stop,
    /// and a throttle so the local player sends at most one signal per <see cref="SendEvery"/> seconds.
    /// Plain C# (time is passed in), so it is unit tested without Play Mode.
    /// </summary>
    public sealed class ChatTyping
    {
        public const float Expire = 4f;
        public const float SendEvery = 2.5f;

        private readonly Dictionary<string, Dictionary<string, (string name, float until)>> _by =
            new Dictionary<string, Dictionary<string, (string, float)>>();
        private string _lastConversation;
        private float _lastSent = float.NegativeInfinity;

        /// <summary>True when the local player should broadcast "typing" now (first key, or after the throttle).</summary>
        public bool ShouldSend(string conversationId, float now)
        {
            if (string.IsNullOrEmpty(conversationId)) return false;
            if (conversationId == _lastConversation && now - _lastSent < SendEvery) return false;
            _lastConversation = conversationId;
            _lastSent = now;
            return true;
        }

        /// <summary>The local player sent a message: the next keystroke may signal again right away.</summary>
        public void ResetSend() { _lastConversation = null; _lastSent = float.NegativeInfinity; }

        public void Note(string conversationId, string userId, string name, float now)
        {
            if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(userId)) return;
            if (!_by.TryGetValue(conversationId, out var people)) _by[conversationId] = people = new Dictionary<string, (string, float)>();
            people[userId] = (string.IsNullOrWhiteSpace(name) ? "?" : name.Trim(), now + Expire);
        }

        /// <summary>A message from them ends their typing at once.</summary>
        public void Clear(string conversationId, string userId)
        {
            if (conversationId != null && _by.TryGetValue(conversationId, out var people)) people.Remove(userId);
        }

        public List<string> Active(string conversationId, float now)
        {
            var names = new List<string>();
            if (conversationId == null || !_by.TryGetValue(conversationId, out var people)) return names;
            List<string> stale = null;
            foreach (var kv in people)
            {
                if (kv.Value.until <= now) (stale ??= new List<string>()).Add(kv.Key);
                else names.Add(kv.Value.name);
            }
            if (stale != null) foreach (var id in stale) people.Remove(id);
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        /// <summary>"Lan is typing…", "Lan and Minh are typing…", "4 people are typing…", or "" when nobody is.</summary>
        public string Label(string conversationId, float now)
        {
            var names = Active(conversationId, now);
            switch (names.Count)
            {
                case 0: return string.Empty;
                case 1: return Loc.F("{0} đang nhập…", "{0} is typing…", "{0}さんが入力中…", names[0]);
                case 2: return Loc.F("{0} và {1} đang nhập…", "{0} and {1} are typing…", "{0}さんと{1}さんが入力中…", names[0], names[1]);
                default: return Loc.F("{0} người đang nhập…", "{0} people are typing…", "{0}人が入力中…", names.Count);
            }
        }
    }

    /// <summary>Message times: "14:05" today, "Hôm qua 14:05" yesterday, "28/10 14:05" before; day separators.</summary>
    public static class ChatTime
    {
        public static string Stamp(long utcTicks, DateTime nowLocal)
        {
            var t = new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();
            string hm = t.ToString("HH:mm");
            int days = (nowLocal.Date - t.Date).Days;
            if (days <= 0) return hm;
            if (days == 1) return Loc.T("Hôm qua", "Yesterday", "昨日") + " " + hm;
            return t.ToString("dd/MM") + " " + hm;
        }

        public static string DayLabel(long utcTicks, DateTime nowLocal)
        {
            var t = new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();
            int days = (nowLocal.Date - t.Date).Days;
            if (days <= 0) return Loc.T("Hôm nay", "Today", "今日");
            if (days == 1) return Loc.T("Hôm qua", "Yesterday", "昨日");
            return t.ToString("dd/MM/yyyy");
        }

        /// <summary>True when a separator belongs before <paramref name="current"/> (first message, or a new local day).</summary>
        public static bool NewDay(long previousUtcTicks, long currentUtcTicks)
        {
            if (previousUtcTicks <= 0) return true;
            var a = new DateTime(previousUtcTicks, DateTimeKind.Utc).ToLocalTime().Date;
            var b = new DateTime(currentUtcTicks, DateTimeKind.Utc).ToLocalTime().Date;
            return a != b;
        }
    }
}
