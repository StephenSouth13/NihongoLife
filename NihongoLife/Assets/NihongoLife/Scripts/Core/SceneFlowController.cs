using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    public class SceneFlowController : MonoBehaviour, IGameService
    {
        public void Initialize()
        {
            Debug.Log("[SceneFlowController] Initialized.");
        }

        public void LoadScene(string sceneName)
        {
            Debug.Log($"[SceneFlowController] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}
