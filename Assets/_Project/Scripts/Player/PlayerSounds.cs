using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Efectos de sonido de Ulises: rodillazo, cuando el rodillo le da a algo (y el remate que lo derrota), lanzar la
    /// bolita, saltar, doble salto, aterrizar, pasos, recibir daño y morir.
    /// Los que no tengan sonido asignado simplemente no suenan.
    /// </summary>
    public class PlayerSounds : MonoBehaviour
    {
        private static readonly int RunStateId = Animator.StringToHash("Run");

        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Health health;
        [SerializeField] private PlayerController controller;
        [Tooltip("Animator del dibujo de Ulises: los pasos siguen a la animación de correr.")]
        [SerializeField] private Animator animator;

        [Tooltip("Al golpear con el rodillo (el \"fiu\" del movimiento).")]
        [SerializeField] private SoundEffect attack = new();
        [Tooltip("Cuando el rodillo le da a un enemigo (el \"¡pum!\").")]
        [SerializeField] private SoundEffect attackHit = new();
        [Tooltip("El golpe que derrota al enemigo; si no hay, suena el normal.")]
        [SerializeField] private SoundEffect finisher = new();
        [SerializeField] private SoundEffect throwDough = new();
        [SerializeField] private SoundEffect jump = new();
        [Tooltip("El doble salto (la vuelta en el aire); si no hay, suena el salto normal.")]
        [SerializeField] private SoundEffect doubleJump = new();
        [SerializeField] private SoundEffect land = new();
        [Tooltip("Solo suena al caer con al menos esta velocidad (no en escalones ni al bajar un borde).")]
        [SerializeField, Min(0f)] private float landMinFallSpeed = 6f;
        [SerializeField] private SoundEffect hurt = new();
        [SerializeField] private SoundEffect death = new();

        [Header("Pasos")]
        [Tooltip("Apágalo si los pasos estorban.")]
        [SerializeField] private bool footstepsEnabled = true;
        [SerializeField] private SoundEffect footstep = new();
        [Tooltip("En qué momento de la animación de correr pisa (0-1): donde el cuerpo baja más.")]
        [SerializeField] private float[] footstepMoments = { 0.36f, 0.82f };

        private float fallSpeed;
        private float lastRunTime = -1f;

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.Attacked += attack.Play;
                combat.AttackHit += OnAttackHit;
                combat.Thrown += throwDough.Play;
            }
            if (controller != null)
            {
                controller.Jumped += jump.Play;
                controller.DoubleJumped += OnDoubleJumped;
                controller.Landed += OnLanded;
            }
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.Attacked -= attack.Play;
                combat.AttackHit -= OnAttackHit;
                combat.Thrown -= throwDough.Play;
            }
            if (controller != null)
            {
                controller.Jumped -= jump.Play;
                controller.DoubleJumped -= OnDoubleJumped;
                controller.Landed -= OnLanded;
            }
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (controller == null)
                return;
            // La caída más rápida desde que despegó: decide si el aterrizaje suena.
            if (!controller.IsGrounded)
                fallSpeed = Mathf.Max(fallSpeed, -controller.Velocity.y);
            Footsteps();
        }

        // Un paso cada vez que la animación de correr pasa por un momento de pisada (sigue su ritmo, también con el café).
        private void Footsteps()
        {
            AnimatorStateInfo state = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            if (!footstepsEnabled || animator == null || !controller.IsGrounded || state.shortNameHash != RunStateId)
            {
                lastRunTime = -1f;
                return;
            }
            float now = state.normalizedTime;
            if (lastRunTime >= 0f && now > lastRunTime)
            {
                foreach (float moment in footstepMoments)
                {
                    // ¿Cruzó la pisada de alguna vuelta entre el cuadro anterior y este?
                    if (Mathf.Floor(now - moment) > Mathf.Floor(lastRunTime - moment))
                    {
                        footstep.Play();
                        break;
                    }
                }
            }
            lastRunTime = now;
        }

        private void OnAttackHit() => (combat.LastHitDefeated && finisher.HasClips ? finisher : attackHit).Play();

        private void OnDoubleJumped() => (doubleJump.HasClips ? doubleJump : jump).Play();

        private void OnLanded()
        {
            if (fallSpeed >= landMinFallSpeed)
                land.Play();
            fallSpeed = 0f;
        }

        // El golpe que lo mata suena a muerte, no a daño (si no hay sonido de muerte, se usa el de daño).
        private void OnDamaged(DamageInfo _)
        {
            if (!health.IsDead)
                hurt.Play();
        }

        private void OnDied() => (death.HasClips ? death : hurt).Play();
    }
}
