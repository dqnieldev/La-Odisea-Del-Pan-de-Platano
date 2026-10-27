using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Cambia el dibujo según los golpes, sin Animator: quieto, "golpeado" un momento al recibir daño (se voltea
    /// para inclinarse lejos de quien pegó) y "noqueado" al morir. Lo usa el muñeco de práctica; convive con
    /// <see cref="DamageFeedback"/> (destello, aplastón y encogerse al morir).
    /// </summary>
    public class HitReactionSprites : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite idle;
        [Tooltip("Dibujado como si le pegaran desde la derecha (se inclina a la izquierda).")]
        [SerializeField] private Sprite hit;
        [SerializeField] private Sprite knockedOut;
        [Tooltip("Segundos que dura el dibujo de golpeado antes de volver a quieto.")]
        [SerializeField, Min(0f)] private float hitDuration = 0.35f;
        [Tooltip("Quieto, mira a la izquierda (hacia donde llega Ulises) en vez de como está dibujado.")]
        [SerializeField] private bool faceLeft;

        private float hitTimer;

        private void Awake() => target.flipX = faceLeft;

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Update()
        {
            if (hitTimer <= 0f)
                return;
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                target.sprite = idle;
                target.flipX = faceLeft;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            target.flipX = info.SourcePosition.x < transform.position.x;
            if (health.IsDead)
                return; // Died pone el dibujo de noqueado.
            target.sprite = hit;
            hitTimer = hitDuration;
        }

        private void OnDied()
        {
            hitTimer = 0f;
            if (knockedOut != null)
                target.sprite = knockedOut;
        }
    }
}
