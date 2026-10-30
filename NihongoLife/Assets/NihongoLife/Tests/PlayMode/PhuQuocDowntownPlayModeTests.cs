using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.PhuQuoc;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Dương Đông downtown (PhuQuocDowntownBuilder): straight street at 1:1, walkable, aligned façades, walkers without colliders.</summary>
    public class PhuQuocDowntownPlayModeTests
    {
        [UnityTest]
        public IEnumerator Downtown_IsStraightWalkableAndAlive()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
            PlayerController player = null;
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                if (player != null && flow != null && !flow.IsLoading && SceneManager.GetActiveScene().name == WorldLocationCatalog.PhuQuocIslandScene) break;
                yield return null;
            }
            Assert.NotNull(player);
            yield return new WaitForSecondsRealtime(1.5f);
            var down = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "DuongDong_Downtown"); Debug.Log("[DowntownTest] found=" + (down != null) + " path=" + (down != null ? down.parent?.name : "-") + " roots=" + string.Join(",", SceneManager.GetActiveScene().GetRootGameObjects().Select(r => r.name)));
            Assert.NotNull(down, "Downtown built (run PhuQuocDowntownBuilder.Build)");

            // Every north-row façade sits on one building line.
            var lots = down.Find("Shophouses").Cast<Transform>().Where(t => t.name.StartsWith("Lot_N")).ToList();
            Assert.Greater(lots.Count, 15);
            float z0 = lots[0].localPosition.z;
            Assert.IsTrue(lots.All(l => Mathf.Abs(l.localPosition.z - z0) < 0.01f), "one building line");

            // Walk 12 m east along the north pavement with the real CharacterController.
            var cc = player.GetComponent<CharacterController>();
            Vector3 start = player.transform.position;
            for (float t = 0f; t < 4f; t += Time.deltaTime) { cc.Move((Vector3.right * 3f + Vector3.down * 2f) * Time.deltaTime); yield return null; }
            Assert.Greater(player.transform.position.x - start.x, 9f, "the pavement is free to walk");
            Assert.Less(Mathf.Abs(player.transform.position.y - start.y), 0.6f, "stays on the pavement");

            var life = Object.FindFirstObjectByType<PhuQuocStreetLife>();
            Assert.NotNull(life);
            Assert.Greater(life.Bodies.Count, 5, "tourists walk the street");
            Assert.IsTrue(life.Bodies.All(b => b.GetComponentsInChildren<Collider>().Length == 0), "walkers never push the player");
        }
    }
}
