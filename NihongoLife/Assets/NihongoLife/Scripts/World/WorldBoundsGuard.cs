using UnityEngine;

namespace NihongoLife.World
{
    public class WorldBoundsGuard : MonoBehaviour
    {
        [SerializeField] private Vector2 halfExtents = new Vector2(63f, 46f);
        [SerializeField] private bool limitForwardAxis = false;
        [SerializeField] private float fallY = -4f;
        [SerializeField] private Vector3 safeSpawn = new Vector3(0f, 0.08f, -13.5f);

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            bool outOfBounds = Mathf.Abs(position.x) > halfExtents.x
                || (limitForwardAxis && Mathf.Abs(position.z) > halfExtents.y)
                || position.y < fallY;
            if (!outOfBounds) return;

            Vector3 rescued = new Vector3(
                Mathf.Clamp(position.x, -halfExtents.x + 2f, halfExtents.x - 2f),
                safeSpawn.y,
                limitForwardAxis ? Mathf.Clamp(position.z, -halfExtents.y + 2f, halfExtents.y - 2f) : position.z
            );

            if (position.y < fallY)
            {
                rescued = safeSpawn;
            }

            if (_controller != null) _controller.enabled = false;
            transform.position = rescued;
            if (_controller != null) _controller.enabled = true;
            Debug.Log($"[WorldBoundsGuard] Rescued player to {rescued}.");
        }
    }
}
