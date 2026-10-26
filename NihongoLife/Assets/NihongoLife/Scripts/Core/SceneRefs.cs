using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    /// <summary>
    /// Cached scene lookups for the runtime game. A live result is reused; a destroyed or deactivated
    /// object is searched again, and the cache is cleared when a scene unloads. Inactive-inclusive
    /// lookups are not cached. Editor builders keep their own direct finds.
    /// </summary>
    public static class SceneRefs
    {
        static readonly Dictionary<Type, UnityEngine.Object> Typed = new Dictionary<Type, UnityEngine.Object>();
        static readonly Dictionary<string, GameObject> Named = new Dictionary<string, GameObject>();
        static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (_hooked)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                _hooked = false;
            }
            Clear();
        }

        /// <summary>First active object of type <typeparamref name="T"/>, including a cached hit that is still active.</summary>
        public static T Find<T>(FindObjectsInactive inactive = FindObjectsInactive.Exclude) where T : UnityEngine.Object
        {
            EnsureHook();
            Type key = typeof(T);
            if (inactive == FindObjectsInactive.Exclude && TryLive(key, out T cached))
                return cached;

            T found = UnityEngine.Object.FindFirstObjectByType<T>(inactive);
            if (inactive == FindObjectsInactive.Exclude)
            {
                if (found != null) Typed[key] = found;
                else Typed.Remove(key);
            }
            return found;
        }

        /// <summary><see cref="GameObject.Find(string)"/> for active objects, reused while the object stays active.</summary>
        public static GameObject FindNamed(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureHook();
            if (Named.TryGetValue(name, out GameObject cached) && cached != null && cached.activeInHierarchy)
                return cached;

            GameObject found = GameObject.Find(name);
            if (found != null) Named[name] = found;
            else Named.Remove(name);
            return found;
        }

        public static void Clear()
        {
            Typed.Clear();
            Named.Clear();
        }

        static bool TryLive<T>(Type key, out T result) where T : UnityEngine.Object
        {
            result = null;
            if (!Typed.TryGetValue(key, out UnityEngine.Object cached) || cached == null)
                return false;
            if (cached is Component component && !component.gameObject.activeInHierarchy)
                return false;
            if (cached is GameObject gameObject && !gameObject.activeInHierarchy)
                return false;
            result = cached as T;
            return result != null;
        }

        static void EnsureHook()
        {
            if (_hooked) return;
            _hooked = true;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneUnloaded(Scene _) => Clear();
    }
}
