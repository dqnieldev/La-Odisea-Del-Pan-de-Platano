using System;
using System.Collections;
using System.Collections.Generic;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Enemigo que camina de un lado a otro sobre el suelo o una plataforma (el cocodrilo).
    /// Da la vuelta al llegar a una pared o a la orilla, así nunca se cae. El dibujo debe mirar a la derecha.
    /// El daño por contacto lo pone un <see cref="Hazard"/> en el mismo objeto.
    /// Opcional: si ve a Ulises enfrente en su piso, camina más rápido hacia él (chaseSpeed).
    /// Opcional: si ve a Ulises cerca y enfrente, se detiene y muerde (trigger "Bite" del Animator);
    /// el daño de la mordida llega en el cuadro en que cierra el hocico. Si le pegan por la espalda,
    /// se voltea y muerde de inmediato (contraataque).
    /// Si el Animator tiene los triggers "Hurt" y "Die": al recibir un golpe de frente se queja y se detiene un momento
    /// (si estaba mordiendo, el golpe le corta la mordida), y al morir hace su animación de muerte.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PatrolEnemy : MonoBehaviour
    {
        private static readonly int BiteId = Animator.StringToHash("Bite");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DieId = Animator.StringToHash("Die");

        [SerializeField, Min(0f)] private float speed = 1.5f;
        [Tooltip("Capas de suelo y paredes (para no caerse de la orilla ni atravesar paredes).")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Qué tan adelante de su centro revisa si hay suelo o pared.")]
        [SerializeField, Min(0f)] private float lookAhead = 0.8f;
        [Tooltip("1 = empieza caminando a la derecha, -1 = a la izquierda.")]
        [SerializeField] private int startDirection = -1;
        [Tooltip("Opcional: al morir deja de caminar.")]
        [SerializeField] private Health health;
        [Tooltip("Segundos que se queda quieto quejándose al recibir un golpe.")]
        [SerializeField, Min(0f)] private float hurtStun = 0.6f;

        [Header("Persecución (opcional)")]
        [Tooltip("Velocidad cuando ve a Ulises enfrente en su mismo piso (0 = no lo persigue). Usa las capas de la mordida.")]
        [SerializeField, Min(0f)] private float chaseSpeed;
        [Tooltip("Hasta qué distancia lo ve hacia adelante.")]
        [SerializeField, Min(0f)] private float sightDistance = 6f;
        [Tooltip("Alto de la zona donde lo ve, desde los pies (más o menos un piso).")]
        [SerializeField, Min(0f)] private float sightHeight = 1.8f;

        [Header("Mordida (opcional)")]
        [Tooltip("Sin capas de objetivo no muerde: solo camina y daña al tocarlo.")]
        [SerializeField] private LayerMask biteTargets;
        [SerializeField] private Animator animator;
        [Tooltip("Centro de la zona de mordida, desde los pies y hacia adelante (x se voltea con la dirección).")]
        [SerializeField] private Vector2 biteOffset = new(1f, 0.4f);
        [SerializeField] private Vector2 biteSize = new(1f, 0.8f);
        [Tooltip("Cuánto avanza al lanzarse a morder (mientras abre el hocico), para que la mordida sí alcance.")]
        [SerializeField, Min(0f)] private float lungeDistance = 0.6f;
        [Tooltip("Segundos desde que empieza a morder hasta que cierra el hocico (ahí hace daño).")]
        [SerializeField, Min(0f)] private float biteHitDelay = 0.25f;
        [Tooltip("Lo que dura la mordida completa; mientras, no camina.")]
        [SerializeField, Min(0f)] private float biteDuration = 0.5f;
        [Tooltip("Espera entre una mordida y la siguiente.")]
        [SerializeField, Min(0f)] private float biteCooldown = 1.2f;
        [SerializeField, Min(1)] private int biteDamage = 1;
        [Tooltip("Si le pegan por la espalda se voltea y muerde al instante, aunque esté esperando entre mordidas.")]
        [SerializeField] private bool counterBiteFromBehind = true;

        /// <summary>Empieza a morder (abre el hocico).</summary>
        public event Action BiteStarted;
        /// <summary>Cierra el hocico (el momento en que la mordida hace daño).</summary>
        public event Action BiteSnapped;
        /// <summary>La mordida le hizo daño a Ulises (no la esquivó ni la detuvo el escudo).</summary>
        public event Action BiteHit;

        private readonly List<Collider2D> hits = new();
        private readonly HashSet<IDamageable> bitten = new();
        private Rigidbody2D body;
        private int direction;
        private bool biting;
        private Coroutine biteRoutine;
        private float cooldownTimer;
        private float stunTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            direction = startDirection >= 0 ? 1 : -1;
            ApplyFacing();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            // La muerte la maneja OnDied; por la espalda contraataca en vez de quejarse.
            if (health.IsDead || TryCounterBite(info))
                return;
            // Pegarle primero le corta la mordida (y no hace daño): premia al que se adelanta.
            if (biting)
                CancelBite();
            stunTimer = hurtStun;
            SetAnimationSpeed(1f);
            SetTrigger(HurtId);
        }

        // Golpe por la espalda (el atacante o la bolita están del lado contrario al que mira): se voltea y muerde.
        private bool TryCounterBite(DamageInfo info)
        {
            if (!counterBiteFromBehind || biting || biteTargets.value == 0)
                return false;
            float side = info.SourcePosition.x - body.position.x;
            if (Mathf.Approximately(side, 0f) || Mathf.Sign(side) == direction)
                return false;
            direction = -direction;
            ApplyFacing();
            biteRoutine = StartCoroutine(Bite());
            return true;
        }

        private void CancelBite()
        {
            if (biteRoutine != null)
                StopCoroutine(biteRoutine);
            biteRoutine = null;
            biting = false;
            cooldownTimer = biteCooldown;
            var contactDamage = GetComponent<Hazard>();
            if (contactDamage != null)
                contactDamage.enabled = true;
            if (animator != null)
                animator.ResetTrigger(BiteId);
        }

        private void OnDied()
        {
            StopAllCoroutines();
            biting = false;
            if (animator == null)
                return;
            animator.speed = 1f;
            // Sin esto, el "Hurt" del mismo golpe quedaría pendiente y cortaría la animación de muerte.
            animator.ResetTrigger(HurtId);
            animator.ResetTrigger(BiteId);
            SetTrigger(DieId);
        }

        private void SetTrigger(int id)
        {
            if (animator == null)
                return;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == id)
                {
                    animator.SetTrigger(id);
                    return;
                }
            }
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead)
                return;

            cooldownTimer -= Time.fixedDeltaTime;
            stunTimer -= Time.fixedDeltaTime;
            if (biting || stunTimer > 0f)
            {
                SetAnimationSpeed(1f);
                return;
            }
            if (CanBite() && TargetInBiteZone())
            {
                biteRoutine = StartCoroutine(Bite());
                return;
            }

            Vector2 position = body.position;
            // Pared adelante (a media altura) u orilla (no hay suelo un poco adelante y abajo): da la vuelta.
            bool wallAhead = Physics2D.Raycast(position + Vector2.up * 0.4f, Vector2.right * direction, lookAhead, groundLayers);
            bool groundAhead = Physics2D.Raycast(position + new Vector2(direction * lookAhead, 0.1f), Vector2.down, 0.4f, groundLayers);
            if (wallAhead || !groundAhead)
            {
                direction = -direction;
                ApplyFacing();
            }
            // Si lo ve enfrente, se le va encima más rápido (y el caminar se acelera igual).
            float currentSpeed = chaseSpeed > 0f && TargetInSight() ? chaseSpeed : speed;
            SetAnimationSpeed(speed > 0f ? currentSpeed / speed : 1f);
            body.MovePosition(position + Vector2.right * (direction * currentSpeed * Time.fixedDeltaTime));
        }

        private bool TargetInSight()
        {
            if (biteTargets.value == 0)
                return false;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = biteTargets, useTriggers = true };
            Vector2 center = body.position + new Vector2(direction * sightDistance / 2f, 0.1f + sightHeight / 2f);
            return Physics2D.OverlapBox(center, new Vector2(sightDistance, sightHeight), 0f, filter, hits) > 0;
        }

        private void SetAnimationSpeed(float value)
        {
            if (animator != null)
                animator.speed = value;
        }

        private bool CanBite() => biteTargets.value != 0 && cooldownTimer <= 0f;

        private Vector2 BiteCenter => body.position + new Vector2(biteOffset.x * direction, biteOffset.y);

        private bool TargetInBiteZone()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = biteTargets, useTriggers = true };
            return Physics2D.OverlapBox(BiteCenter, biteSize, 0f, filter, hits) > 0;
        }

        private IEnumerator Bite()
        {
            biting = true;
            SetAnimationSpeed(1f);
            // Mientras muerde, tocarlo no daña: el daño es solo al cerrar el hocico (así se puede esquivar).
            var contactDamage = GetComponent<Hazard>();
            if (contactDamage != null)
                contactDamage.enabled = false;
            if (animator != null)
                animator.SetTrigger(BiteId);
            BiteStarted?.Invoke();

            // Se lanza hacia adelante mientras abre el hocico (sin pasar paredes ni caerse de la orilla).
            float lungeSpeed = biteHitDelay > 0f ? lungeDistance / biteHitDelay : 0f;
            for (float t = 0f; t < biteHitDelay; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                Vector2 position = body.position;
                bool wallAhead = Physics2D.Raycast(position + Vector2.up * 0.4f, Vector2.right * direction, lookAhead, groundLayers);
                bool groundAhead = Physics2D.Raycast(position + new Vector2(direction * lookAhead, 0.1f), Vector2.down, 0.4f, groundLayers);
                if (!wallAhead && groundAhead)
                    body.MovePosition(position + Vector2.right * (direction * lungeSpeed * Time.fixedDeltaTime));
            }
            if (health == null || !health.IsDead)
            {
                BiteSnapped?.Invoke();
                // Si Ulises sigue en la zona al cerrar el hocico, recibe la mordida (una vez por mordida).
                bitten.Clear();
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = biteTargets, useTriggers = true };
                Physics2D.OverlapBox(BiteCenter, biteSize, 0f, filter, hits);
                foreach (Collider2D hit in hits)
                {
                    var target = hit.GetComponentInParent<IDamageable>();
                    if (target != null && bitten.Add(target) && target.TakeDamage(new DamageInfo(biteDamage, transform.position, gameObject)))
                        BiteHit?.Invoke();
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0f, biteDuration - biteHitDelay));
            if (contactDamage != null)
                contactDamage.enabled = true;
            biting = false;
            biteRoutine = null;
            cooldownTimer = biteCooldown;
        }

        private void ApplyFacing()
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            int dir = Application.isPlaying ? direction : (startDirection >= 0 ? 1 : -1);
            Vector3 position = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(position + Vector3.up * 0.4f, position + new Vector3(dir * lookAhead, 0.4f));
            Gizmos.DrawLine(position + new Vector3(dir * lookAhead, 0.1f), position + new Vector3(dir * lookAhead, -0.3f));
            if (biteTargets.value != 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(position + new Vector3(biteOffset.x * dir, biteOffset.y), biteSize);
            }
        }
    }
}
