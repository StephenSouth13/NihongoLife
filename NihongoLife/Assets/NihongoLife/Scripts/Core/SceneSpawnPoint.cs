using UnityEngine;

namespace NihongoLife.Core
{
    public class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id = "default";
        public string Id => id;
        public void Configure(string value) => id = string.IsNullOrWhiteSpace(value) ? "default" : value;
    }
}
