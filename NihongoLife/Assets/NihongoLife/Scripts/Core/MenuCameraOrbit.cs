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
        [SerializeField] private float startAngle = -32f;

        private float _angle;

        private void Awake()
        {
            _angle = startAngle;
        }

        public void Configure(float orbitRadius, float cameraHeight, float speed, float initialAngle, float targetLookHeight)
        {
            radius = orbitRadius;
            height = cameraHeight;
            orbitSpeed = speed;
            startAngle = initialAngle;
            lookHeight = targetLookHeight;
            _angle = startAngle;
            ApplyPosition();
        }

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
