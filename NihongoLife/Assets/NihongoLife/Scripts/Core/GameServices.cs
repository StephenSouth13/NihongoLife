using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    public static class GameServices
    {
        private static readonly Dictionary<Type, IGameService> Services = new Dictionary<Type, IGameService>();

        public static void Register<T>(T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (Services.TryGetValue(type, out var previous))
            {
                // A destroyed Unity service (its AppRoot is gone — a new play session, the next test) is replaced quietly.
                bool dead = previous is UnityEngine.Object unityObject && unityObject == null;
                if (!dead && !ReferenceEquals(previous, service)) Debug.LogWarning($"[GameServices] Service {type.Name} is already registered. Overwriting.");
                Services[type] = service;
            }
            else
            {
                Services.Add(type, service);
            }
        }

        /// <summary>Swaps a service on purpose (e.g. the intro trailer's in-memory save) without the overwrite warning.</summary>
        public static void Replace<T>(T service) where T : class, IGameService => Services[typeof(T)] = service;

        public static T Get<T>() where T : class, IGameService
        {
            var type = typeof(T);
            if (Services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            Debug.LogError($"[GameServices] Service {type.Name} is not registered!");
            return null;
        }

        public static bool TryGet<T>(out T result) where T : class, IGameService
        {
            var type = typeof(T);
            if (Services.TryGetValue(type, out var service))
            {
                result = service as T;
                return result != null;
            }

            result = null;
            return false;
        }

        /// <summary>Removes one service (tests that stand in a fake put the real one back or remove theirs).</summary>
        public static void Unregister<T>() where T : class, IGameService => Services.Remove(typeof(T));

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
