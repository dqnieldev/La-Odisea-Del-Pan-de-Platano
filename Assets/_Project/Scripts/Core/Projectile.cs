using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Proyectil en línea recta: daña al primer objetivo que toca y desaparece al chocar
    /// con una pared o al acabarse su tiempo. Lo usan la bolita de masa de Ulises y,
    /// más adelante, los proyectiles de Polifemo y Circe.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Hijo con el sprite; gira mientras vuela.")]
        [SerializeField] private Transform visual;
        [SerializeField] private float spinSpeed = 540f;

        private Rigidbody2D body;
        private int damage;
        private LayerMask targetLayers;
        private LayerMask obstacleLayers;
        private GameObject owner;
        private float lifeTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            GetComponent<Collider2D>().isTrigger = true;
        }

        /// <summary>Dispara el proyectil. Hay que llamarlo justo después de instanciarlo.</summary>
        public void Launch(Vector2 velocity, int damageAmount, float lifetime,
            LayerMask targets, LayerMask obstacles, GameObject shooter)
        {
            body.linearVelocity = velocity;
            damage = damageAmount;
            lifeTimer = lifetime;
            targetLayers = targets;
            obstacleLayers = obstacles;
            owner = shooter;
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (visual != null)
                visual.Rotate(0f, 0f, -spinSpeed * Mathf.Sign(body.linearVelocity.x) * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.transform.IsChildOf(owner.transform))
                return;

            int layerBit = 1 << other.gameObject.layer;
            if ((targetLayers.value & layerBit) != 0)
            {
                var target = other.GetComponentInParent<IDamageable>();
                if (target == null)
                    return;
                target.TakeDamage(new DamageInfo(damage, transform.position, owner));
                Destroy(gameObject);
                return;
            }

            // Atraviesa plataformas de un sentido y zonas trigger (escaleras); se rompe contra lo sólido.
            if ((obstacleLayers.value & layerBit) != 0 && !other.isTrigger && !other.usedByEffector)
                Destroy(gameObject);
        }
    }
}
