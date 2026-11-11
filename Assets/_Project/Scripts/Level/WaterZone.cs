using Odisea.Core;
using Odisea.Player;
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
        [Tooltip("Cuánto antes de que acabe el chapuzón empieza el sonido de muerte de Ulises (0 = justo al terminar).")]
        [SerializeField, Min(0f)] private float deathSoundOverlap = 0.15f;

        // Lo que dura el chapuzón más largo (hay de dónde escoger si tiene variantes).
        private float SplashLength()
        {
            float length = 0f;
            if (splashSound.clips != null)
                foreach (AudioClip clip in splashSound.clips)
                    if (clip != null)
                        length = Mathf.Max(length, clip.length);
            return length;
        }

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
                // Primero el chapuzón y, al acabar, el sonido de muerte (si no, se encimarían).
                PlayerSounds sounds = health.GetComponentInChildren<PlayerSounds>();
                if (sounds == null)
                    sounds = health.GetComponentInParent<PlayerSounds>();
                if (sounds != null)
                    sounds.DelayNextDeath(SplashLength() - deathSoundOverlap);
                health.Kill();
                return;
            }
            if (LevelScore.Current != null)
                LevelScore.Current.Discount(health);
            health.Kill();
        }
    }
}
