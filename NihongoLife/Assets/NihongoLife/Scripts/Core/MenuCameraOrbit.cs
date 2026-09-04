using UnityEngine;

namespace NihongoLife.Core
{
    public class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float radius = 36f;
        [SerializeField] private float height = 13f;
        [SerializeField] private float orbitSpeed = 4.5f;
        [SerializeField] private float lookHeight = 2.4f;

        private float _angle = -32f;

        public void SetTarget(Transform orbitTarget)
        {
            target = orbitTarget;
            ApplyPosition();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            _angle += orbitSpeed * Time.deltaTime;
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (target == null) return;

            float radians = _angle * Mathf.Deg2Rad;
            Vector3 center = target.position;
            transform.position = center + new Vector3(Mathf.Sin(radians) * radius, height, Mathf.Cos(radians) * radius);
            transform.LookAt(center + Vector3.up * lookHeight);
        }
    }
}
