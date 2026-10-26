using System.Collections;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Cheap ambient life for Hibari Station. A couple of people pace the platform and one steps up to the
    /// train while it boards. Inside the carriage one passenger walks to a seat, one leaves at a stop,
    /// and a younger rider gives up their seat to an older one — the player can be the one who offers it.
    /// No NavMesh and no extra lights. Added when the station wakes; the scene does not need a menu step.
    /// </summary>
    public sealed class StationCarriageLife : MonoBehaviour
    {
        private StationTravelController _station;
        private SeatedPassenger _elder;
        private SeatedPassenger _youth;
        private SeatedPassenger _boarder;
        private SeatedPassenger _alighter;
        private bool _playerAsked;
        private bool _yielded;

        private void Start()
        {
            _station = GetComponent<StationTravelController>();
            SpawnPlatformWalkers();
        }

        /// <summary>Called after the ride enables the carriage and shuffles who is aboard.</summary>
        public void OnBoarded()
        {
            _playerAsked = false;
            _yielded = false;
            AssignRoles();
            if (_boarder != null) StartCoroutine(BoardBeat());
            if (_elder != null && _youth != null) StartCoroutine(YieldBeat());
            if (_alighter != null) StartCoroutine(AlightBeat());
        }

        public void PlayerYields() => _playerAsked = true;

        private void AssignRoles()
        {
            _elder = _youth = _boarder = _alighter = null;
            if (_station == null || _station.CarriageInterior == null) return;
            var all = _station.CarriageInterior.GetComponentsInChildren<SeatedPassenger>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].IsAmbientElder && !all[i].gameObject.activeSelf) all[i].gameObject.SetActive(true);
            Wake(all, "Guide");
            for (int i = 0; i < all.Length; i++)
            {
                var passenger = all[i];
                if (passenger == null || !passenger.gameObject.activeInHierarchy) continue;
                if (passenger.GetComponent<TrainPassengerTalk>() != null) continue;
                if (_elder == null && LooksElder(passenger)) { _elder = passenger; continue; }
                if (_youth == null) { _youth = passenger; continue; }
                if (_boarder == null) { _boarder = passenger; continue; }
                if (_alighter == null) { _alighter = passenger; break; }
            }
            if (_elder == null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var passenger = all[i];
                    if (passenger == null || !passenger.gameObject.activeInHierarchy || passenger.GetComponent<TrainPassengerTalk>() != null) continue;
                    if (passenger == _youth || passenger == _boarder || passenger == _alighter) continue;
                    _elder = passenger;
                    break;
                }
            }
        }

        private static void Wake(SeatedPassenger[] all, string token)
        {
            int awake = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].gameObject.activeInHierarchy && all[i].GetComponent<TrainPassengerTalk>() == null) awake++;
            if (awake >= 4) return;
            for (int pass = 0; pass < 2 && awake < 4; pass++)
            {
                for (int i = 0; i < all.Length && awake < 4; i++)
                {
                    var passenger = all[i];
                    if (passenger == null || passenger.gameObject.activeInHierarchy || passenger.GetComponent<TrainPassengerTalk>() != null) continue;
                    if (pass == 0 && !passenger.IsAmbientElder && !LooksNamed(passenger, token)) continue;
                    passenger.gameObject.SetActive(true);
                    awake++;
                }
            }
        }

        private IEnumerator BoardBeat()
        {
            Transform body = _boarder.transform;
            Vector3 seat = body.position;
            Quaternion facing = body.rotation;
            _boarder.SetSitting(false);
            body.position = seat + body.forward * 1.65f;
            KidExpression.On(body.gameObject).Play(SocialMood.Neutral, 0.2f);
            yield return MoveTo(body, seat, 1.15f);
            if (body == null) yield break;
            body.rotation = facing;
            _boarder.SetSitting(true);
        }

        private IEnumerator YieldBeat()
        {
            Transform elder = _elder.transform;
            Transform youth = _youth.transform;
            Vector3 seat = youth.position;
            Quaternion facing = youth.rotation;
            _elder.SetSitting(false);
            Vector3 stand = seat + youth.forward * 0.95f + youth.right * 0.7f;
            stand.y = seat.y;
            elder.position = stand;
            Vector3 look = seat - stand;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) elder.rotation = Quaternion.LookRotation(look);
            KidExpression.On(elder.gameObject).Play(SocialMood.Tired, 8f);
            var offer = youth.GetComponent<SeatOffer>();
            if (offer == null) offer = youth.gameObject.AddComponent<SeatOffer>();
            offer.Bind(this);
            offer.enabled = true;

            float waited = 0f;
            while (waited < 9f && !_playerAsked)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            bool byPlayer = _playerAsked;
            if (offer != null) offer.enabled = false;
            if (_yielded || youth == null || elder == null) yield break;
            _yielded = true;

            _youth.SetSitting(false);
            KidExpression.On(youth.gameObject).Play(SocialMood.Proud, 4f);
            youth.GetComponent<CharacterAnimationController>()?.TriggerBow();
            Vector3 aside = seat + youth.forward * 0.85f + youth.right * -0.75f;
            aside.y = seat.y;
            yield return MoveTo(youth, aside, 0.9f);
            if (elder == null) yield break;
            yield return MoveTo(elder, seat, 0.85f);
            if (elder == null) yield break;
            elder.rotation = facing;
            _elder.SetSitting(true);
            KidExpression.On(elder.gameObject).Play(SocialMood.Happy, 4f);
            if (byPlayer)
            {
                SocialStanding.AddReputation(12, "seat");
                SocialStanding.AddAffinity("station_elder", 12);
                SocialStanding.AddMoodlet("kind", SocialMood.Proud, 0.85f, 2.2f);
                HudFeed.Post(Loc.T("Bạn nhường ghế. Cụ cảm ơn.", "You gave up the seat. They thank you.", "せきを ゆずりました。ありがとうございます。"), HudFeed.Kind.Reward, 4f, "seat-yield");
            }
            else
            {
                HudFeed.Post(Loc.T("Một bạn trẻ đứng dậy nhường ghế cho cụ.", "A younger passenger stands and offers the seat.", "わかい ひとが せきを ゆずりました。"), HudFeed.Kind.Info, 4f, "seat-yield");
            }
        }

        private IEnumerator AlightBeat()
        {
            while (_station != null && !_station.IsAtStop) yield return null;
            if (_alighter == null || !_alighter.gameObject.activeInHierarchy) yield break;
            Transform body = _alighter.transform;
            _alighter.SetSitting(false);
            KidExpression.On(body.gameObject).Play(SocialMood.Neutral, 2f);
            Vector3 door = body.position + body.forward * 1.15f + body.right * 1.4f;
            door.y = body.position.y;
            yield return MoveTo(body, door, 1.05f);
            if (_alighter != null) _alighter.gameObject.SetActive(false);
        }

        private void SpawnPlatformWalkers()
        {
            if (_station == null || _station.PlatformSpawn == null || _station.CarriageInterior == null) return;
            if (transform.Find("PlatformPassenger_0") != null) return;
            var template = _station.CarriageInterior.GetComponentInChildren<SeatedPassenger>(true);
            if (template == null) return;
            Transform visual = template.transform.Find("Visual");
            if (visual == null) return;
            Transform origin = _station.PlatformSpawn;
            Vector3 axis = origin.right;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f) axis = Vector3.right;
            axis.Normalize();
            for (int i = 0; i < 2; i++)
            {
                var walker = CreateWalker(visual.gameObject, origin.position + axis * (i == 0 ? -3.2f : 2.6f), origin.rotation, i);
                StartCoroutine(Pace(walker.transform, origin, axis, i == 0));
            }
        }

        private GameObject CreateWalker(GameObject visualPrefab, Vector3 position, Quaternion rotation, int index)
        {
            var root = new GameObject("PlatformPassenger_" + index);
            root.transform.SetParent(transform, true);
            root.transform.SetPositionAndRotation(position, rotation);
            var visual = Instantiate(visualPrefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            var animation = root.AddComponent<CharacterAnimationController>();
            animation.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            KidExpression.On(root).Play(index == 0 ? SocialMood.Happy : SocialMood.Neutral, 1.5f);
            return root;
        }

        private IEnumerator Pace(Transform body, Transform origin, Vector3 axis, bool boards)
        {
            bool toPositive = true;
            bool boarded = false;
            while (body != null)
            {
                if (boards && !boarded && _station != null && _station.IsTrainBoarding && !_station.IsOnboard)
                {
                    Vector3 forward = origin.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                    Vector3 door = origin.position + forward.normalized * 1.5f;
                    door.y = origin.position.y;
                    yield return MoveTo(body, door, 1.15f);
                    if (body != null) body.gameObject.SetActive(false);
                    boarded = true;
                    yield break;
                }
                Vector3 target = origin.position + axis * (toPositive ? 4.6f : -4.6f);
                target.y = origin.position.y;
                yield return MoveTo(body, target, 0.95f);
                toPositive = !toPositive;
                yield return new WaitForSeconds(0.7f);
            }
        }

        private static IEnumerator MoveTo(Transform body, Vector3 target, float speed)
        {
            if (body == null) yield break;
            var animation = body.GetComponent<CharacterAnimationController>();
            target.y = body.position.y;
            float deadline = Time.time + 12f;
            while (body != null && (body.position - target).sqrMagnitude > 0.012f && Time.time < deadline)
            {
                Vector3 flat = target - body.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.0004f)
                    body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(flat), 10f * Time.deltaTime);
                body.position = Vector3.MoveTowards(body.position, target, speed * Time.deltaTime);
                animation?.SetSpeed(speed);
                yield return null;
            }
            animation?.SetSpeed(0f);
        }

        private static bool LooksElder(SeatedPassenger passenger) =>
            passenger != null && (passenger.IsAmbientElder || LooksNamed(passenger, "Guide") || LooksNamed(passenger, "grandma") || LooksNamed(passenger, "sota") || LooksNamed(passenger, "elder"));

        private static bool LooksNamed(SeatedPassenger passenger, string token)
        {
            if (passenger == null || string.IsNullOrEmpty(token)) return false;
            if (passenger.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var joints = passenger.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < joints.Length; i++)
                if (joints[i].name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }

    /// <summary>F on the younger rider, before they stand on their own, counts as the player giving up the seat.</summary>
    public sealed class SeatOffer : MonoBehaviour, IInteractable
    {
        private StationCarriageLife _life;

        public void Bind(StationCarriageLife life) => _life = life;

        private void Awake()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = SeatAnchor.TalkCenter;
            box.size = SeatAnchor.TalkSize;
        }

        public string GetPromptJa() => "せきを ゆずる";
        public string GetpromptEn() => Loc.T("Nhường ghế cho cụ", "Give up the seat", "せきを ゆずる");
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (!isActiveAndEnabled) return;
            enabled = false;
            _life?.PlayerYields();
        }
    }
}
