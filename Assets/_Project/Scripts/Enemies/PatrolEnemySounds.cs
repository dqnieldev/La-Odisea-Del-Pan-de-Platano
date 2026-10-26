using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Efectos de sonido de un enemigo que patrulla (el cocodrilo): abre el hocico, lo cierra (la mordida),
    /// recibe daño y muere. Los que no tengan sonido asignado simplemente no suenan; si falta el de muerte,
    /// el golpe final usa el de daño. La queja llega un instante después del golpe, para no taparse con él.
    /// </summary>
    public class PatrolEnemySounds : MonoBehaviour
    {
        [SerializeField] private PatrolEnemy enemy;
        [SerializeField] private Health health;

        [Tooltip("Opcional: al empezar la mordida (abre el hocico).")]
        [SerializeField] private SoundEffect biteStart = new();
        [Tooltip("Al cerrar el hocico: suena justo cuando la mordida hace daño.")]
        [SerializeField] private SoundEffect biteSnap = new();
        [SerializeField] private SoundEffect hurt = new();
        [SerializeField] private SoundEffect death = new();
        [Tooltip("Segundos entre el golpe (que suena con el arma) y la queja del enemigo.")]
        [SerializeField, Min(0f)] private float reactionDelay = 0.1f;

        private void OnEnable()
        {
            if (enemy != null)
            {
                enemy.BiteStarted += biteStart.Play;
                enemy.BiteSnapped += biteSnap.Play;
            }
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.BiteStarted -= biteStart.Play;
                enemy.BiteSnapped -= biteSnap.Play;
            }
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnDamaged(DamageInfo _)
        {
            if (!health.IsDead)
                hurt.PlayAfter(reactionDelay);
        }

        private void OnDied() => (death.HasClips ? death : hurt).PlayAfter(reactionDelay);
    }
}
