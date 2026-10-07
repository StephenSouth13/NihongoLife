using System.Collections;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>Commuter sitting on a carriage bench: plays the humanoid Sit state whenever enabled.</summary>
    public sealed class SeatedPassenger : MonoBehaviour
    {
        [SerializeField] private bool alwaysOnBoard;

        /// <summary>Passengers the player can talk to stay on every ride; the rest are shuffled per trip.</summary>
        public bool AlwaysOnBoard => alwaysOnBoard;

        public void Configure(bool keepOnEveryRide) => alwaysOnBoard = keepOnEveryRide;

        private void OnEnable() => StartCoroutine(SitNextFrame());

        private IEnumerator SitNextFrame()
        {
            yield return null; // the animator must be initialised before CrossFade
            var animation = GetComponent<CharacterAnimationController>();
            if (animation != null) animation.SetSitting(true);
        }
    }
}
