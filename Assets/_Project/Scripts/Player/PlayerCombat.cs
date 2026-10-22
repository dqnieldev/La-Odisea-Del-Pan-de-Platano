using System;
using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>Golpe con el rodillo: daña lo que esté en un área frente a Ulises.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        private PlayerController controller;
        private IDamageable self;
        private float cooldownTimer;
        private readonly HashSet<IDamageable> alreadyHit = new();

        public event Action Attacked;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            self = GetComponent<IDamageable>();
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            if (input.AttackPressed && cooldownTimer <= 0f && controller.HasControl)
                Attack();
        }

        private void Attack()
        {
            PlayerConfig config = controller.Config;
            cooldownTimer = config.attackCooldown;
            Attacked?.Invoke();

            alreadyHit.Clear();
            Collider2D[] hits = Physics2D.OverlapBoxAll(AttackCenter(config), config.attackSize, 0f, config.attackLayers);
            foreach (Collider2D hit in hits)
            {
                var target = hit.GetComponentInParent<IDamageable>();
                // Un enemigo con varios colliders solo recibe un golpe por ataque, y Ulises no se golpea a sí mismo.
                if (target == null || ReferenceEquals(target, self) || !alreadyHit.Add(target))
                    continue;

                target.TakeDamage(new DamageInfo(config.attackDamage, transform.position, gameObject));
            }
        }

        private Vector2 AttackCenter(PlayerConfig config)
        {
            Vector2 offset = config.attackOffset;
            // La escala x se invierte al voltear a Ulises, así sirve también fuera de Play mode.
            offset.x *= Mathf.Sign(transform.localScale.x);
            return (Vector2)transform.position + offset;
        }

        private void OnDrawGizmosSelected()
        {
            var playerController = GetComponent<PlayerController>();
            if (playerController == null || playerController.Config == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(AttackCenter(playerController.Config), playerController.Config.attackSize);
        }
    }
}
