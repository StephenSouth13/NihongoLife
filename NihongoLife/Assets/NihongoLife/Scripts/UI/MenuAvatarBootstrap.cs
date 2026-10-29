using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.UI
{
    /// <summary>Adds the living character to the main menu scene when it loads (no scene edit: the menu layout is untouched).</summary>
    public static class MenuAvatarBootstrap
    {
        public const string MenuScene = "01_MainMenu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Attach(SceneManager.GetActiveScene());
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) => Attach(scene);

        private static void Attach(Scene scene)
        {
            if (scene.name != MenuScene || Object.FindFirstObjectByType<MenuAvatarStage>() != null) return;
            new GameObject("MenuAvatarStage").AddComponent<MenuAvatarStage>();
        }
    }
}
