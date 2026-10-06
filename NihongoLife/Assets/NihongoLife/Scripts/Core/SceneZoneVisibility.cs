using UnityEngine;

namespace NihongoLife.Core
{
    public class SceneZoneVisibility : MonoBehaviour
    {
        [SerializeField] private bool hideWhenZoneChanges = true;
        public bool HideWhenZoneChanges => hideWhenZoneChanges;
    }
}
