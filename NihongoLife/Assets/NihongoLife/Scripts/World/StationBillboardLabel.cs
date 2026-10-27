using UnityEngine;

namespace NihongoLife.World
{
    public sealed class StationBillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        }
    }
}
