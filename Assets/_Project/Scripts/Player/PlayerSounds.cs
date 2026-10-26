using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Efectos de sonido de Ulises: rodillazo, cuando el rodillo le da a algo, lanzar la bolita, recibir daño y morir.
    /// Los que no tengan sonido asignado simplemente no suenan.
    /// </summary>
    public class PlayerSounds : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Health health;

        [Tooltip("Al golpear con el rodillo (el \"fiu\" del movimiento).")]
        [SerializeField] private SoundEffect attack = new();
        [Tooltip("Cuando el rodillo le da a un enemigo (el \"¡pum!\").")]
        [SerializeField] private SoundEffect attackHit = new();
        [SerializeField] private SoundEffect throwDough = new();
        [SerializeField] private SoundEffect hurt = new();
        [SerializeField] private SoundEffect death = new();

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.Attacked += attack.Play;
                combat.AttackHit += attackHit.Play;
                combat.Thrown += throwDough.Play;
            }
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += death.Play;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.Attacked -= attack.Play;
                combat.AttackHit -= attackHit.Play;
                combat.Thrown -= throwDough.Play;
            }
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= death.Play;
            }
        }

        // El golpe que lo mata suena a muerte, no a daño.
        private void OnDamaged(DamageInfo _)
        {
            if (!health.IsDead)
                hurt.Play();
        }
    }
}
