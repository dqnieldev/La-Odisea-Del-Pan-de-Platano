using System.Collections;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Enemigo del nivel 2 que camina por el piso hacia el horno (ratas y cocodrilos): al llegar se queda mordiéndolo
    /// cada <see cref="attackInterval"/> segundos hasta que Ulises lo vence. Al recibir un golpe se detiene un momento
    /// y retrocede un poco. Daña a Ulises al tocarlo (su <see cref="Hazard"/>). Animator opcional con los triggers
    /// Bite, Hurt y Die (los del cocodrilo); el dibujo mira a la derecha y se voltea con la escala del objeto.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class OvenRaider : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Hazard contact;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float speed = 2f;
        [Tooltip("Cuánta vida le quita al horno cada mordida.")]
        [SerializeField, Min(1)] private int ovenDamage = 1;
        [SerializeField, Min(0.1f)] private float attackInterval = 1.4f;
        [Tooltip("Lo que tarda en dar la primera mordida al llegar al horno (para que se note a qué llegó).")]
        [SerializeField, Min(0f)] private float firstAttackDelay = 0.2f;
        [Tooltip("Del inicio de la mordida al momento en que le pega al horno.")]
        [SerializeField, Min(0f)] private float attackHitDelay = 0.25f;
        [Tooltip("Dónde se detiene, además del medio ancho del horno (el hocico llega antes que el cuerpo).")]
        [SerializeField] private float reach = 0.4f;
        [SerializeField, Min(0f)] private float hurtStun = 0.4f;
        [SerializeField, Min(0f)] private float knockback = 0.6f;
        [Tooltip("Muerde una sola vez y huye por donde vino (las ratas); si no, se queda mordiendo (los cocodrilos).")]
        [SerializeField] private bool fleeAfterAttack;
        [SerializeField, Min(0f)] private float fleeDistance = 14f;
        [SerializeField] private SoundEffect attackSound = new();

        private static readonly int BiteId = Animator.StringToHash("Bite");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int WalkId = Animator.StringToHash("Walk");

        private Rigidbody2D body;
        private Oven oven;
        private float stunTimer;
        private float attackTimer;
        private bool dead;
        private bool arrived;
        private bool fleeing;
        private float fleeFrom;

        /// <summary>El horno al que va (lo pone quien lo hace aparecer).</summary>
        public void Setup(Oven target) => oven = target;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        private void OnEnable()
        {
            if (health == null)
                return;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health == null)
                return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Start()
        {
            if (oven == null)
                oven = FindAnyObjectByType<Oven>();
            attackTimer = firstAttackDelay;
        }

        private void FixedUpdate()
        {
            if (dead || oven == null || oven.Burned)
                return;
            float dx = oven.transform.position.x - body.position.x;
            if (fleeing)
            {
                // Huye alejándose del horno y desaparece fuera de la pantalla.
                Face(-dx);
                Walk();
                body.MovePosition(body.position + new Vector2(-Mathf.Sign(dx) * speed * 1.3f * Time.fixedDeltaTime, 0f));
                if (Mathf.Abs(body.position.x - fleeFrom) > fleeDistance)
                    Destroy(gameObject);
                return;
            }
            Face(dx);
            if (stunTimer > 0f)
            {
                stunTimer -= Time.fixedDeltaTime;
                return;
            }
            float stop = oven.HalfWidth + reach;
            if (Mathf.Abs(dx) > stop)
            {
                float step = Mathf.Min(speed * Time.fixedDeltaTime, Mathf.Abs(dx) - stop);
                body.MovePosition(body.position + new Vector2(Mathf.Sign(dx) * step, 0f));
                Walk();
                return;
            }
            if (!arrived)
            {
                // Al llegar muerde casi de inmediato; luego, cada attackInterval.
                arrived = true;
                attackTimer = Mathf.Min(attackTimer, firstAttackDelay);
            }
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer > 0f)
            {
                Rest();
                return;
            }
            attackTimer = attackInterval;
            StartCoroutine(Attack());
        }

        private IEnumerator Attack()
        {
            SetTrigger(BiteId);
            attackSound.Play();
            yield return new WaitForSeconds(attackHitDelay);
            if (dead || stunTimer > 0f || oven == null)
                yield break;
            oven.Hit(ovenDamage);
            if (!fleeAfterAttack)
                yield break;
            yield return new WaitForSeconds(0.2f);
            fleeing = true;
            fleeFrom = body.position.x;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (health.IsDead)
                return;
            stunTimer = hurtStun;
            attackTimer = Mathf.Max(attackTimer, hurtStun + 0.2f);
            SetTrigger(HurtId);
            // Retrocede, alejándose de quien le pegó.
            float away = Mathf.Sign(body.position.x - info.SourcePosition.x);
            if (Mathf.Approximately(away, 0f))
                away = -transform.localScale.x;
            body.position += new Vector2(away * knockback, 0f);
        }

        private void OnDied()
        {
            dead = true;
            StopAllCoroutines();
            if (contact != null)
                contact.enabled = false;
            SetTrigger(DieId);
        }

        private void Face(float dx)
        {
            if (Mathf.Abs(dx) < 0.05f)
                return;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dx);
            transform.localScale = scale;
        }

        private void SetTrigger(int id)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.speed = 1f;
                animator.SetTrigger(id);
            }
        }

        // Entre mordidas no camina en su lugar: se queda quieto en el primer cuadro de la caminata (no hay animación de
        // parado). En cuanto se mueve, lo golpean o muerde, la animación sigue.
        private void Rest()
        {
            if (animator == null || animator.runtimeAnimatorController == null || animator.speed == 0f ||
                animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).shortNameHash != WalkId)
                return;
            animator.Play(WalkId, 0, 0f);
            animator.speed = 0f;
        }

        private void Walk()
        {
            if (animator != null)
                animator.speed = 1f;
        }
    }
}
