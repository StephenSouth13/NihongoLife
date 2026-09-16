using System;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Player
{
    public static class PlayableCharacterCatalog
    {
        public const string PlayerPrefsKey = "NihongoLife.SelectedPlayableCharacter";
        public const string DefaultId = "student";

        private static readonly PlayableCharacter[] Characters =
        {
            new PlayableCharacter("student", "Tanaka", "Học viên", "Characters/NL_Player", new Color(0.98f, 0.78f, 0.32f)),
            new PlayableCharacter("cashier", "Aki", "Gọn gàng", "Characters/NL_Cashier", new Color(0.38f, 0.74f, 0.95f)),
            new PlayableCharacter("guide", "Lilly", "Thân thiện", "Characters/NL_Guide", new Color(0.95f, 0.52f, 0.34f)),
            new PlayableCharacter("neighbor", "Ren", "Đời thường", "Characters/NL_Neighbor", new Color(0.54f, 0.86f, 0.54f))
        };

        public static int Count => Characters.Length;

        public static PlayableCharacter Get(int index)
        {
            if (Characters.Length == 0)
            {
                return default;
            }

            return Characters[Mathf.Clamp(index, 0, Characters.Length - 1)];
        }

        public static PlayableCharacter GetSelected()
        {
            string selectedId = PlayerPrefs.GetString(PlayerPrefsKey, DefaultId);
            return FindById(selectedId);
        }

        public static PlayableCharacter FindById(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? Characters[index] : Characters[0];
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                if (string.Equals(Characters[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public static void SaveSelected(string id)
        {
            PlayerPrefs.SetString(PlayerPrefsKey, string.IsNullOrWhiteSpace(id) ? DefaultId : id);
            PlayerPrefs.Save();
        }

        public static GameObject LoadPrefab(PlayableCharacter character)
        {
            return Resources.Load<GameObject>(character.ResourcePath);
        }

        public static bool ApplySelectedVisual(GameObject actor)
        {
            if (actor == null) return false;
            return ApplyVisual(actor, GetSelected());
        }

        public static bool ApplyVisual(GameObject actor, PlayableCharacter character)
        {
            var prefab = LoadPrefab(character);
            if (prefab == null)
            {
                Debug.LogWarning($"[PlayableCharacterCatalog] Missing prefab at Resources/{character.ResourcePath}.");
                return false;
            }

            Transform existing = actor.transform.Find("Visual");
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            var visual = UnityEngine.Object.Instantiate(prefab, actor.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.Destroy(collider);
            }

            var animation = actor.GetComponent<CharacterAnimationController>();
            if (animation != null)
            {
                animation.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            }

            return true;
        }
    }

    public readonly struct PlayableCharacter
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Tagline;
        public readonly string ResourcePath;
        public readonly Color AccentColor;

        public PlayableCharacter(string id, string displayName, string tagline, string resourcePath, Color accentColor)
        {
            Id = id;
            DisplayName = displayName;
            Tagline = tagline;
            ResourcePath = resourcePath;
            AccentColor = accentColor;
        }
    }
}
