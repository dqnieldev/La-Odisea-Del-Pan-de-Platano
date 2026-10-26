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
    /// Opcional: si ve a Ulises cerca y enfrente, se detiene y muerde (trigger "Bite" del Animator);
    /// el daño de la mordida llega en el cuadro en que cierra el hocico.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PatrolEnemy : MonoBehaviour
    {
        private static readonly int BiteId = Animator.StringToHash("Bite");

        [SerializeField, Min(0f)] private float speed = 1.5f;
        [Tooltip("Capas de suelo y paredes (para no caerse de la orilla ni atravesar paredes).")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Qué tan adelante de su centro revisa si hay suelo o pared.")]
        [SerializeField, Min(0f)] private float lookAhead = 0.8f;
        [Tooltip("1 = empieza caminando a la derecha, -1 = a la izquierda.")]
        [SerializeField] private int startDirection = -1;
        [Tooltip("Opcional: al morir deja de caminar.")]
        [SerializeField] private Health health;

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

        private readonly List<Collider2D> hits = new();
        private readonly HashSet<IDamageable> bitten = new();
        private Rigidbody2D body;
        private int direction;
        private bool biting;
        private float cooldownTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            direction = startDirection >= 0 ? 1 : -1;
            ApplyFacing();
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead)
                return;

            cooldownTimer -= Time.fixedDeltaTime;
            if (biting)
                return;
            if (CanBite() && TargetInBiteZone())
            {
                StartCoroutine(Bite());
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
            body.MovePosition(position + Vector2.right * (direction * speed * Time.fixedDeltaTime));
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
            if (animator != null)
                animator.SetTrigger(BiteId);

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
                // Si Ulises sigue en la zona al cerrar el hocico, recibe la mordida (una vez por mordida).
                bitten.Clear();
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = biteTargets, useTriggers = true };
                Physics2D.OverlapBox(BiteCenter, biteSize, 0f, filter, hits);
                foreach (Collider2D hit in hits)
                {
                    var target = hit.GetComponentInParent<IDamageable>();
                    if (target != null && bitten.Add(target))
                        target.TakeDamage(new DamageInfo(biteDamage, transform.position, gameObject));
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0f, biteDuration - biteHitDelay));
            biting = false;
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
