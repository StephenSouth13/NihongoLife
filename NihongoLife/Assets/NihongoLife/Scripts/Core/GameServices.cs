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
            if (Services.ContainsKey(type))
            {
                Debug.LogWarning($"[GameServices] Service {type.Name} is already registered. Overwriting.");
                Services[type] = service;
            }
            else
            {
                Services.Add(type, service);
            }
        }

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

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
