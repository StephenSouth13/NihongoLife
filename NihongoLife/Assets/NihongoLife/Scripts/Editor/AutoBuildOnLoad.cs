using UnityEditor;
using UnityEngine;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Runs the safe, idempotent NihongoLife build steps automatically once per Editor
    /// session, so nobody has to open NihongoLife/Characters or NihongoLife/Trailer and
    /// click a menu item by hand. Only calls entry points that skip-if-already-exists —
    /// destructive "Force Rebuild" menu items are intentionally never auto-run here.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBuildOnLoad
    {
        private const string HasRunSessionKey = "NihongoLife.AutoBuildOnLoad.HasRun";

        static AutoBuildOnLoad()
        {
            if (SessionState.GetBool(HasRunSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(HasRunSessionKey, true);

            // Defer past Editor startup so this never competes with the initial domain load.
            EditorApplication.delayCall += RunSafeAutoBuild;
        }

        private static void RunSafeAutoBuild()
        {
            try
            {
                // skipIfExists by default — only fills in missing prefabs/animator/new
                // Mixamo characters dropped into Assets/ThirdParty/Mixamo/Characters.
                CharacterBuilder.BuildCharacterSystem();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AutoBuildOnLoad] Character auto-build skipped: {ex.Message}");
            }

            try
            {
                // No-ops if Assets/NihongoLife/Scenes/99_Trailer.unity already exists.
                TrailerSceneBuilder.BuildTrailerSceneSafe();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AutoBuildOnLoad] Trailer auto-build skipped: {ex.Message}");
            }
        }
    }
}
