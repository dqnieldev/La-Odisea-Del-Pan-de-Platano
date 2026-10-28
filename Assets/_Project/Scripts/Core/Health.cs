using System;
using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Vida genérica con invulnerabilidad temporal tras recibir daño.
    /// La usan Ulises (vidas del HUD) y los enemigos.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [Tooltip("Segundos sin poder recibir daño después de un golpe.")]
        [SerializeField, Min(0f)] private float invulnerabilityTime = 1f;
        [Tooltip("Destruye el GameObject al morir (útil para enemigos).")]
        [SerializeField] private bool destroyOnDeath;

        private float invulnerabilityTimer;
        private IDamageBlocker blocker;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;

        /// <summary>(actual, máximo). Lo escucha el HUD.</summary>
        public event Action<int, int> HealthChanged;
        public event Action<DamageInfo> Damaged;
        public event Action Died;
        /// <summary>Un escudo detuvo el golpe: no se perdió vida.</summary>
        public event Action<DamageInfo> Blocked;

        private void Awake()
        {
            Current = maxHealth;
            blocker = GetComponent<IDamageBlocker>();
        }

        private void Update()
        {
            if (invulnerabilityTimer > 0f)
                invulnerabilityTimer -= Time.deltaTime;
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || IsInvulnerable || info.Amount <= 0)
                return false;

            // El escudo se gasta en este golpe; la invulnerabilidad breve evita que el siguiente toque llegue enseguida.
            if (blocker != null && blocker.TryBlock(info))
            {
                invulnerabilityTimer = invulnerabilityTime;
                Blocked?.Invoke(info);
                return false;
            }

            Current = Mathf.Max(0, Current - info.Amount);
            invulnerabilityTimer = invulnerabilityTime;

            Damaged?.Invoke(info);
            HealthChanged?.Invoke(Current, maxHealth);

            if (IsDead)
            {
                Died?.Invoke();
                if (destroyOnDeath)
                    Destroy(gameObject);
            }
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return;

            Current = Mathf.Min(maxHealth, Current + amount);
            HealthChanged?.Invoke(Current, maxHealth);
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            invulnerabilityTimer = 0f;
            HealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
