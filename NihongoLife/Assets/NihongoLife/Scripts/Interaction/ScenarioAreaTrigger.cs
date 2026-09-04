using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ScenarioAreaTrigger : MonoBehaviour
    {
        [SerializeField] private string areaId;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool oneShot = true;

        private bool _triggered;

        public string AreaId => areaId;

        private void Awake()
        {
            var collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered || !other.CompareTag(requiredTag)) return;

            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager == null || !scenarioManager.CanEnterArea(areaId)) return;

            _triggered = oneShot;
            scenarioManager.OnAreaEntered(areaId);
        }
    }
}
