using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Efectos de sonido de un enemigo que patrulla (el cocodrilo): abre el hocico, lo cierra (la mordida),
    /// recibe daño y muere. Los que no tengan sonido asignado simplemente no suenan.
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
                health.Died += death.Play;
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
                health.Died -= death.Play;
            }
        }

        private void OnDamaged(DamageInfo _)
        {
            if (!health.IsDead)
                hurt.Play();
        }
    }
}
