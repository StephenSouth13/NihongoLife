using UnityEngine;

namespace NihongoLife.World
{
    public class EndlessCityLooper : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] tileRoots;
        [SerializeField] private float tileLength = 100f;

        private void Start()
        {
            if (player == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null) player = playerGo.transform;
            }
        }

        private void LateUpdate()
        {
            if (player == null || tileRoots == null || tileRoots.Length == 0) return;

            for (int i = 0; i < tileRoots.Length; i++)
            {
                Transform tile = tileRoots[i];
                if (tile == null) continue;

                float delta = player.position.z - tile.position.z;
                if (delta > tileLength * 1.5f)
                {
                    tile.position += Vector3.forward * tileLength * tileRoots.Length;
                }
                else if (delta < -tileLength * 1.5f)
                {
                    tile.position -= Vector3.forward * tileLength * tileRoots.Length;
                }
            }
        }
    }
}
