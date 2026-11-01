using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>Tag on a UI object that a bake authored into the scene: its build scope and creation order (see UiBind).</summary>
    [DisallowMultipleComponent]
    public sealed class UiBakeId : MonoBehaviour
    {
        public string scope;
        public int order;
        /// <summary>Name the object was created with (code may rename it afterwards).</summary>
        public string made;
    }
}