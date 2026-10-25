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
        [Tooltip("0 = no gira: el dibujo se voltea según la dirección (para arte que ya trae el movimiento dibujado).")]
        [SerializeField] private float spinSpeed = 540f;
        [Tooltip("Opcional: efecto que aparece donde choca con un enemigo o una pared (p. ej. la masa que se embarra).")]
        [SerializeField] private GameObject impactPrefab;

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
            // Sin giro, el dibujo (dibujado hacia la derecha) mira hacia donde va.
            if (visual != null && Mathf.Approximately(spinSpeed, 0f) && velocity.x < 0f)
            {
                Vector3 scale = visual.localScale;
                scale.x = -Mathf.Abs(scale.x);
                visual.localScale = scale;
            }
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (visual != null && !Mathf.Approximately(spinSpeed, 0f))
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
                Impact();
                return;
            }

            // Atraviesa plataformas de un sentido y zonas trigger (escaleras); se rompe contra lo sólido.
            if ((obstacleLayers.value & layerBit) != 0 && !other.isTrigger && !other.usedByEffector)
                Impact();
        }

        private void Impact()
        {
            if (impactPrefab != null)
            {
                GameObject effect = Instantiate(impactPrefab, transform.position, Quaternion.identity);
                // El efecto también va dibujado hacia la derecha: se voltea si el proyectil iba a la izquierda.
                if (body.linearVelocity.x < 0f)
                {
                    Vector3 scale = effect.transform.localScale;
                    scale.x = -Mathf.Abs(scale.x);
                    effect.transform.localScale = scale;
                }
            }
            Destroy(gameObject);
        }
    }
}
