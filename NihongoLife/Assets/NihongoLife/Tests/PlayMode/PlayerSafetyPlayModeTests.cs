using System.Collections;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Jumping cannot climb a wall, and a player who leaves the floor of a zone comes back.</summary>
    public class PlayerSafetyPlayModeTests
    {
        private static IEnumerator EnterZone(string scene, string spawn)
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1.5f);
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            Assert.NotNull(flow);
            flow.TransferZone(scene, spawn, scene);
            for (float t = 0f; t < 25f && (flow.IsLoading || SceneManager.GetActiveScene().name != scene); t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Sushi_APlayerPushedThroughTheFloorComesBack()
        {
            yield return EnterZone("30_SushiRestaurant", "sushi_entrance");
            var player = Object.FindFirstObjectByType<PlayerController>();
            var body = player.GetComponent<CharacterController>();
            yield return new WaitForSecondsRealtime(2.5f); // long enough to remember a footing
            int before = WorldBoundsGuard.ZoneRescues;
            Vector3 stood = player.transform.position;
            body.enabled = false;
            player.transform.position = stood + Vector3.down * 2f + Vector3.back * 12f;
            body.enabled = true;
            for (float t = 0f; t < 6f && WorldBoundsGuard.ZoneRescues == before; t += Time.unscaledDeltaTime) yield return null;
            Assert.Greater(WorldBoundsGuard.ZoneRescues, before, "The fall was caught");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Less(Vector3.Distance(player.transform.position, stood), 3f, "Back on the floor near where the player stood");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Sushi_TheDoorwayStopsAWalkOut()
        {
            yield return EnterZone("30_SushiRestaurant", "sushi_entrance");
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = GameInputService.GetOrCreate();
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            input.SetMobileMove(Vector2.up);
            float furthest = player.transform.position.z;
            for (float t = 0f; t < 5f; t += Time.unscaledDeltaTime)
            {
                var camera = Camera.main;
                if (camera != null) input.SetMobileMove(new Vector2(0f, Vector3.Dot(camera.transform.forward, Vector3.back) >= 0f ? 1f : -1f));
                furthest = Mathf.Min(furthest, player.transform.position.z);
                yield return null;
            }
            input.SetMobileMove(Vector2.zero);
            Assert.Greater(furthest, -9.1f, "Stopped at the door");
            Assert.Greater(player.transform.position.y, -0.5f, "Still on the floor");
        }
    }
}
