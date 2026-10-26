using System;
using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Hibari Station's automatic ticket gates (改札): two lanes, each closed by a pair of flaps. Walking into a lane
    /// from the hall with a ticket taps it (StationTravelController PassGate) and the flaps open; without a ticket
    /// they stay shut and the station says where to buy one. From the platform side the flaps always open, so
    /// nobody is ever locked onto the platform. A railing either side means the gates are the only way through.
    /// Built by StationGateBuilder; the root's local +Z is the hall, −Z the platform.
    /// </summary>
    public sealed class TicketGate : MonoBehaviour
    {
        [Serializable]
        public sealed class Lane
        {
            public float x;                 // lane centre, root-local
            public Transform leftFlap, rightFlap;
            public Collider barrier;        // solid while the flaps are shut
            [NonSerialized] public float open;
            [NonSerialized] public Vector3 leftClosed, rightClosed;
            [NonSerialized] public bool refused;
        }

        [SerializeField] private List<Lane> lanes = new();
        [SerializeField] private float laneHalfWidth = 0.65f;
        [SerializeField] private float approach = 2.2f;   // how far before the flaps the lane starts reacting
        [SerializeField] private float flapTravel = 0.32f; // how far each flap slides into its cabinet

        private Transform _player;
        private StationTravelController _station;
        private float _nextLookup;

        public IReadOnlyList<Lane> Lanes => lanes;
        public bool IsOpen(int lane) => lane >= 0 && lane < lanes.Count && lanes[lane].open > 0.5f;

        public void Configure(List<Lane> gateLanes, float halfWidth)
        {
            lanes = gateLanes;
            laneHalfWidth = halfWidth;
        }

        private void Awake()
        {
            foreach (var lane in lanes)
            {
                if (lane.leftFlap != null) lane.leftClosed = lane.leftFlap.localPosition;
                if (lane.rightFlap != null) lane.rightClosed = lane.rightFlap.localPosition;
            }
        }

        private void Update()
        {
            if ((_player == null || _station == null) && Time.unscaledTime >= _nextLookup)
            {
                _nextLookup = Time.unscaledTime + 0.5f;
                var controller = SceneRefs.Find<NihongoLife.Player.PlayerController>();
                _player = controller != null ? controller.transform : null;
                _station = SceneRefs.Find<StationTravelController>();
            }
            Vector3 p = _player != null ? transform.InverseTransformPoint(_player.position) : new Vector3(999f, 0f, 999f);
            foreach (var lane in lanes)
            {
                bool inLane = Mathf.Abs(p.x - lane.x) < laneHalfWidth + 0.15f && Mathf.Abs(p.z) < approach;
                bool want = false;
                if (inLane)
                {
                    if (p.z < -0.2f || lane.open > 0.5f && Mathf.Abs(p.z) < 1.1f) want = true; // leaving the platform, or already between the flaps
                    else if (_station != null)
                    {
                        if (!_station.GatePassed && _station.HasTicket) _station.Execute(StationAction.PassGate, _player.gameObject); // tap the ticket
                        if (_station.GatePassed) want = true;
                        else if (!lane.refused) { lane.refused = true; _station.Execute(StationAction.PassGate, _player.gameObject); } // "you need a ticket"
                    }
                }
                else lane.refused = false;

                lane.open = Mathf.MoveTowards(lane.open, want ? 1f : 0f, Time.deltaTime * (want ? 5f : 2.5f));
                if (lane.leftFlap != null) lane.leftFlap.localPosition = lane.leftClosed + Vector3.left * (flapTravel * lane.open);
                if (lane.rightFlap != null) lane.rightFlap.localPosition = lane.rightClosed + Vector3.right * (flapTravel * lane.open);
                if (lane.barrier != null) lane.barrier.enabled = lane.open < 0.5f;
            }
        }
    }
}
