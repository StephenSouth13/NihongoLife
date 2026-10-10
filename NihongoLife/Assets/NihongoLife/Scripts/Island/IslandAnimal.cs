using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// A farm animal that wanders inside its pen (walk/idle clips via the Animator "Speed" parameter), turns to the
    /// player when talked to, and can be fed island produce or petted. Names and phrases follow the target language.
    /// </summary>
    public sealed class IslandAnimal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string animalId = "cow";
        [SerializeField] private Vector3 penCenter;
        [SerializeField] private float penRadius = 4f;
        [SerializeField] private float walkSpeed = 0.9f;

        private Animator _animator;
        private Vector3 _target;
        private float _waitUntil;
        private float _busyUntil;
        private Transform _lookAt;

        public string AnimalId => animalId;
        public IslandAnimalDef Def => IslandCatalog.Load().Animal(animalId);

        public void Configure(string id, Vector3 center, float radius) { animalId = id; penCenter = center; penRadius = radius; }

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _target = transform.position;
            _waitUntil = Time.time + UnityEngine.Random.Range(0.5f, 3f);
        }

        private void Update()
        {
            if (Time.time < _busyUntil)
            {
                SetSpeed(0f);
                if (_lookAt != null) Face(_lookAt.position, 4f);
                return;
            }
            Vector3 to = _target - transform.position; to.y = 0f;
            if (to.magnitude < 0.25f)
            {
                SetSpeed(0f);
                if (Time.time >= _waitUntil)
                {
                    Vector2 r = UnityEngine.Random.insideUnitCircle * penRadius;
                    _target = new Vector3(penCenter.x + r.x, transform.position.y, penCenter.z + r.y);
                    _waitUntil = Time.time + UnityEngine.Random.Range(3f, 8f);
                }
                return;
            }
            Face(_target, 2.5f);
            transform.position += transform.forward * walkSpeed * Time.deltaTime;
            SetSpeed(1f);
        }

        private void Face(Vector3 point, float speed)
        {
            Vector3 d = point - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), speed * Time.deltaTime);
        }

        private void SetSpeed(float s)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null) _animator.SetFloat("Speed", s, 0.15f, Time.deltaTime);
        }

        public void Hold(Transform player, float seconds)
        {
            _lookAt = player;
            _busyUntil = Time.time + seconds;
        }

        public string Feed(out string food)
        {
            food = null;
            var def = Def;
            if (def.foods == null || def.foods.Length == 0) return "Bé này không ăn nông sản — hãy vuốt ve nhé.";
            foreach (string f in def.foods)
                if (IslandEconomy.Owned(f) > 0) { food = f; break; }
            if (food == null) return $"Cần {string.Join(" hoặc ", def.foods.Select(f => IslandCatalog.Load().Crop(f)?.word.vi ?? f))} để cho ăn — thu hoạch ở ruộng nhé.";
            PlayerInventory.Instance.RemoveItem(food);
            if (_animator != null) _animator.SetTrigger("Eat");
            var record = IslandState.Record; record.fed++;
            Meet();
            IslandState.Save();
            return null;
        }

        public void Pet()
        {
            if (_animator != null) _animator.SetTrigger("Jump");
            Meet();
        }

        public void Meet()
        {
            var record = IslandState.Record;
            if (!record.animalsMet.Contains(animalId)) { record.animalsMet.Add(animalId); IslandState.Save(); }
            IslandState.Discover("animal:" + animalId);
            IslandAchievements.Check();
        }

        public string GetPromptJa() => IslandLanguage.Primary(Def?.word);
        public string GetpromptEn() => $"Làm quen với {Def?.word.vi}";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            Hold(player.transform, 6f);
            Meet();
            IslandUI.OpenAnimal(this);
        }
    }
}
