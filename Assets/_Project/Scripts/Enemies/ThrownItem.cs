using Odisea.Core;
using Odisea.Player;
using UnityEngine;

namespace Odisea.Enemies
{
    public enum ThrownKind
    {
        /// <summary>Caja: quita un corazón si le pega a Ulises y se rompe contra lo que toque.</summary>
        Crate,
        /// <summary>Cáscara: cae en una repisa y se queda ahí; Ulises se resbala si la pisa.</summary>
        Peel,
    }

    /// <summary>
    /// Lo que avienta Madame Circe (<see cref="CirceThrower"/>): vuela en arco (con su propia gravedad) y atraviesa las
    /// partes rotas de las repisas cuando va de subida. La caja se rompe al chocar; la cáscara se queda donde cae un
    /// rato y hace resbalar a Ulises (lo empuja hacia donde iba y le quita el control un momento).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ThrownItem : MonoBehaviour
    {
        [SerializeField] private ThrownKind kind;
        [SerializeField] private Transform visual;
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0f)] private float gravity = 22f;
        [SerializeField] private float spinSpeed = 360f;
        [SerializeField, Min(1)] private int damage = 1;
        [Tooltip("Segundos que dura una cáscara en el piso antes de desaparecer.")]
        [SerializeField, Min(0f)] private float peelLifetime = 10f;
        [Tooltip("Velocidad del resbalón (x hacia donde iba Ulises, y un saltito).")]
        [SerializeField] private Vector2 slipVelocity = new(9f, 3f);
        [SerializeField, Min(0f)] private float slipControlLock = 0.6f;
        [Tooltip("Cáscara: su dibujo ya tirada en el piso (pivote abajo).")]
        [SerializeField] private Sprite landedSprite;
        [Tooltip("Caja: sus pedazos, que se ven un instante al romperse.")]
        [SerializeField] private Sprite brokenSprite;
        [SerializeField] private SoundEffect breakSound = new();
        [SerializeField] private SoundEffect slipSound = new();

        private Rigidbody2D body;
        private Vector2 velocity;
        private bool landed;
        private float lifeTimer = 8f;

        public ThrownKind Kind => kind;
        public float Gravity => gravity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            GetComponent<Collider2D>().isTrigger = true;
        }

        /// <summary>Lo avienta con esa velocidad inicial (luego la gravedad lo curva).</summary>
        public void Throw(Vector2 initialVelocity) => velocity = initialVelocity;

        private void FixedUpdate()
        {
            if (landed)
                return;
            velocity.y -= gravity * Time.fixedDeltaTime;
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            if (!landed && visual != null)
                visual.Rotate(0f, 0f, -spinSpeed * Mathf.Sign(velocity.x == 0f ? 1f : velocity.x) * Time.deltaTime);
            // La cáscara se desvanece el último segundo.
            if (landed && lifeTimer < 1f && visual != null && visual.TryGetComponent(out SpriteRenderer renderer))
            {
                Color color = renderer.color;
                color.a = lifeTimer;
                renderer.color = color;
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => Touch(other);

        // La cáscara en el piso también resbala a quien llega caminando encima.
        private void OnTriggerStay2D(Collider2D other)
        {
            if (landed)
                Touch(other);
        }

        private void Touch(Collider2D other)
        {
            int layerBit = 1 << other.gameObject.layer;
            if ((playerLayers.value & layerBit) != 0)
            {
                HitPlayer(other);
                return;
            }
            if (landed || (groundLayers.value & layerBit) == 0 || other.isTrigger)
                return;
            // De subida atraviesa las partes rotas (plataformas de un sentido), como Ulises.
            if (other.usedByEffector && velocity.y > 0f)
                return;

            float top = other.bounds.max.y;
            bool onTop = velocity.y <= 0f && transform.position.y >= top - 0.35f;
            if (kind == ThrownKind.Crate)
            {
                Break();
            }
            else if (onTop)
            {
                // La cáscara se queda sobre la repisa.
                landed = true;
                velocity = Vector2.zero;
                lifeTimer = peelLifetime;
                transform.position = new Vector3(transform.position.x, top, transform.position.z);
                if (visual != null)
                {
                    visual.localRotation = Quaternion.identity;
                    if (landedSprite != null && visual.TryGetComponent(out SpriteRenderer peel))
                    {
                        peel.sprite = landedSprite;
                        visual.localPosition = Vector3.zero;
                    }
                }
            }
            else if (!other.usedByEffector)
            {
                // Chocó de lado con una pared o una orilla: cae derecho.
                velocity.x = 0f;
            }
        }

        private void HitPlayer(Collider2D other)
        {
            if (kind == ThrownKind.Crate)
            {
                if (landed)
                    return;
                var target = other.GetComponentInParent<IDamageable>();
                if (target == null)
                    return;
                target.TakeDamage(new DamageInfo(damage, transform.position, gameObject));
                Break();
                return;
            }

            // Cáscara: solo resbala si está en el piso y Ulises va pisando.
            var controller = other.GetComponentInParent<PlayerController>();
            if (!landed || controller == null || !controller.IsGrounded || !controller.HasControl)
                return;
            float direction = Mathf.Abs(controller.Velocity.x) > 0.1f ? Mathf.Sign(controller.Velocity.x) : controller.FacingDirection;
            controller.Slip(new Vector2(direction * slipVelocity.x, slipVelocity.y), slipControlLock);
            slipSound.Play();
            Destroy(gameObject);
        }

        private void Break()
        {
            breakSound.Play();
            if (brokenSprite != null)
            {
                int order = visual != null && visual.TryGetComponent(out SpriteRenderer crate) ? crate.sortingOrder : 10;
                ThrownDebris.Spawn(brokenSprite, transform.position, order, Mathf.Sign(velocity.x == 0f ? 1f : velocity.x));
            }
            Destroy(gameObject);
        }
    }
}
