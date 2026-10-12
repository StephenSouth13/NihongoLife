using System;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Player
{
    public static class PlayableCharacterCatalog
    {
        public const string PlayerPrefsKey = "NihongoLife.SelectedPlayableCharacter";
        public const string PlayerNamePrefsKey = "NihongoLife.PlayerDisplayName";
        /// <summary>The kids from Kids Characters for Survival are the player characters; Haruto is the default.</summary>
        public const string DefaultId = "kid_haruto";
        private const string KidsDefaultMigratedKey = "NihongoLife.KidsDefaultMigrated";
        public const string DefaultPlayerName = "Gakusei";

        private static readonly PlayableCharacter[] Characters =
        {
            // Kids Characters for Survival (ThirdParty/New Chac), built by KidsCharactersSetup.Build. They replace the
            // old "COMING SOON" slots, which only borrowed the cashier/guide/neighbor NPC models.
            new PlayableCharacter("kid_haruto", "Haruto", "Học sinh · ハルト", "Characters/Kids/NL_kid_haruto", new Color(0.38f, 0.74f, 0.95f), true),
            new PlayableCharacter("kid_yui", "Yui", "Học sinh · ユイ", "Characters/Kids/NL_kid_yui", new Color(0.95f, 0.52f, 0.62f), true),
            new PlayableCharacter("kid_sota", "Sota", "Học sinh · ソウタ", "Characters/Kids/NL_kid_sota", new Color(0.54f, 0.86f, 0.54f), true),
            new PlayableCharacter("kid_hina", "Hina", "Học sinh · ヒナ", "Characters/Kids/NL_kid_hina", new Color(0.98f, 0.66f, 0.3f), true),
            new PlayableCharacter("kid_riku", "Riku", "Học sinh · リク", "Characters/Kids/NL_kid_riku", new Color(0.45f, 0.6f, 0.98f), true),
            new PlayableCharacter("kid_mio", "Mio", "Học sinh · ミオ", "Characters/Kids/NL_kid_mio", new Color(0.78f, 0.55f, 0.95f), true),
            new PlayableCharacter("kid_yuto", "Yuto", "Học sinh · ユウト", "Characters/Kids/NL_kid_yuto", new Color(0.4f, 0.8f, 0.8f), true),
            new PlayableCharacter("kid_sakura", "Sakura", "Học sinh · サクラ", "Characters/Kids/NL_kid_sakura", new Color(0.98f, 0.6f, 0.75f), true)
            // The adult Mixamo student (Tanaka/Remy) is gone: everyone in the game is a Kid now.
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
            // Saves made before the kids arrived stored the old default ("student"): move them to the new default once.
            if (PlayerPrefs.GetInt(KidsDefaultMigratedKey, 0) == 0)
            {
                PlayerPrefs.SetInt(KidsDefaultMigratedKey, 1);
                if (PlayerPrefs.GetString(PlayerPrefsKey, DefaultId) == "student") PlayerPrefs.SetString(PlayerPrefsKey, DefaultId);
                PlayerPrefs.Save();
            }
            string selectedId = PlayerPrefs.GetString(PlayerPrefsKey, DefaultId);
            if (selectedId == "student") { selectedId = DefaultId; PlayerPrefs.SetString(PlayerPrefsKey, DefaultId); }
            PlayableCharacter selected = FindById(selectedId);
            return selected.IsAvailable ? selected : FindById(DefaultId);
        }

        public static PlayableCharacter FindById(string id)
        {
            int index = IndexOf(id);
            if (index >= 0) return Characters[index];
            int fallback = IndexOf(DefaultId);
            return Characters[fallback >= 0 ? fallback : 0];
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

        public static void SavePlayerName(string playerName)
        {
            PlayerPrefs.SetString(PlayerNamePrefsKey, string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName.Trim());
            PlayerPrefs.Save();
        }

        public static string GetPlayerName()
        {
            string savedName = PlayerPrefs.GetString(PlayerNamePrefsKey, DefaultPlayerName);
            return string.IsNullOrWhiteSpace(savedName) || IsPlaceholderName(savedName) ? DefaultPlayerName : savedName.Trim();
        }

        /// <summary>Names that only mean "nobody chose a name yet" (guest saves, anonymous accounts, old defaults).</summary>
        public static bool IsPlaceholderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            string n = name.Trim();
            return n == DefaultPlayerName || n == "Học viên Nihongo"
                || string.Equals(n, "Guest", StringComparison.OrdinalIgnoreCase) || string.Equals(n, "Player", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The character's name shown on the HUD: the name chosen on this device (character select, or the profile
        /// name copied in at sign-in) → the name in the save → the signed-in account's name → the default.
        /// </summary>
        public static string ResolvePlayerName(string savedDisplayName)
        {
            string chosen = GetPlayerName();
            if (!IsPlaceholderName(chosen)) return chosen;
            if (!IsPlaceholderName(savedDisplayName)) return savedDisplayName.Trim();
            if (GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated && !IsPlaceholderName(auth.DisplayName)) return auth.DisplayName.Trim();
            return DefaultPlayerName;
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

            // Running, tired, hurt and fainting clips from the Kids pack (player; other online players get the run).
            if ((actor.GetComponent<PlayerController>() != null || actor.GetComponent<RemotePlayerAvatar>() != null) && actor.GetComponent<KidMoodAnimator>() == null)
                actor.AddComponent<KidMoodAnimator>();

            // Only the player wears the ground ring (NPCs have name plates), in the character's own colour.
            if (actor.GetComponent<PlayerController>() != null)
            {
                var marker = actor.GetComponent<PlayerMarker>() ?? actor.AddComponent<PlayerMarker>();
                marker.Configure(character.AccentColor, 0.55f);
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
        public readonly bool IsAvailable;

        public PlayableCharacter(string id, string displayName, string tagline, string resourcePath, Color accentColor, bool isAvailable)
        {
            Id = id;
            DisplayName = displayName;
            Tagline = tagline;
            ResourcePath = resourcePath;
            AccentColor = accentColor;
            IsAvailable = isAvailable;
        }
    }
}
