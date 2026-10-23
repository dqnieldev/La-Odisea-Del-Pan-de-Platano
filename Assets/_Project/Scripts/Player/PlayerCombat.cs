using System;
using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Ataques de Ulises: golpe con el rodillo (cuerpo a cuerpo) y bolita de masa (proyectil lento).
    /// No se puede atacar mientras trepa una escalera.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Prefab de la bolita de masa.")]
        [SerializeField] private Projectile projectilePrefab;

        private PlayerController controller;
        private IDamageable self;
        private float attackCooldownTimer;
        private float throwCooldownTimer;
        private readonly HashSet<IDamageable> alreadyHit = new();

        public event Action Attacked;
        public event Action Thrown;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            self = GetComponent<IDamageable>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            attackCooldownTimer -= dt;
            throwCooldownTimer -= dt;

            if (controller.Config == null || input == null)
                return;

            // Se consumen siempre para que una pulsación hecha en la escalera no se dispare al bajar.
            bool attackPressed = input.ConsumeAttackPressed();
            bool throwPressed = input.ConsumeThrowPressed();
            if (!controller.HasControl || controller.IsClimbing)
                return;

            if (attackPressed && attackCooldownTimer <= 0f)
                Attack();
            else if (throwPressed && throwCooldownTimer <= 0f)
                Throw();
        }

        private void Attack()
        {
            PlayerConfig config = controller.Config;
            attackCooldownTimer = config.attackCooldown;
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

        private void Throw()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{name}: falta asignar el prefab del proyectil en PlayerCombat.", this);
                return;
            }

            PlayerConfig config = controller.Config;
            throwCooldownTimer = config.projectileCooldown;

            float direction = controller.FacingDirection;
            Vector2 offset = config.projectileSpawnOffset;
            offset.x *= direction;

            Projectile projectile = Instantiate(projectilePrefab, (Vector2)transform.position + offset, Quaternion.identity);
            projectile.Launch(new Vector2(direction * config.projectileSpeed, 0f), config.projectileDamage,
                config.projectileLifetime, config.attackLayers, config.groundLayers, gameObject);
            Thrown?.Invoke();
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
