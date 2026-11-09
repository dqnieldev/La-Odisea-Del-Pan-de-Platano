using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// El agua del manglar: si Ulises la toca, se hunde y pierde (sale el Game Over y "Reintentar" empieza el nivel de
    /// nuevo). Los enemigos que caen al agua se van sin sumar al puntaje. Va en un trigger a lo largo del nivel.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterZone : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField] private SoundEffect splashSound = new();

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            int bit = 1 << other.gameObject.layer;
            if ((playerLayers.value & bit) == 0 && (enemyLayers.value & bit) == 0)
                return;
            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.IsDead)
                return;
            splashSound.Play();
            if ((playerLayers.value & bit) != 0)
            {
                // Se hunde: deja de moverse y cae despacio mientras sale el Game Over.
                if (other.attachedRigidbody != null)
                {
                    other.attachedRigidbody.linearVelocity = Vector2.zero;
                    other.attachedRigidbody.gravityScale = 0.3f;
                }
                health.Kill();
                return;
            }
            if (LevelScore.Current != null)
                LevelScore.Current.Discount(health);
            health.Kill();
        }
    }
}
