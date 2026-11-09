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

        /// <summary>Un corazón más (el nuevo ya lleno), sin pasar de <paramref name="limit"/>. Devuelve si se pudo.</summary>
        public bool IncreaseMax(int amount, int limit)
        {
            if (IsDead || amount <= 0 || maxHealth >= limit)
                return false;
            int added = Mathf.Min(amount, limit - maxHealth);
            maxHealth += added;
            Current += added;
            HealthChanged?.Invoke(Current, maxHealth);
            return true;
        }

        /// <summary>Se acaba la vida de golpe (sin escudo ni invulnerabilidad): p. ej. se quemó el pan del horno.</summary>
        public void Kill()
        {
            if (IsDead)
                return;
            Current = 0;
            HealthChanged?.Invoke(Current, maxHealth);
            Died?.Invoke();
            if (destroyOnDeath)
                Destroy(gameObject);
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            invulnerabilityTimer = 0f;
            HealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
