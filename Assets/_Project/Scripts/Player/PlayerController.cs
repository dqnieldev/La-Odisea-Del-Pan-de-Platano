using System;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Movimiento de plataformas de Ulises: correr, saltar, doble salto, coyote time,
    /// buffer de salto, salto variable y retroceso al recibir daño.
    /// Lo comparten el Tutorial, El Mercado y La Tovara.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Punto en los pies de Ulises para detectar el suelo.")]
        [SerializeField] private Transform groundCheck;
        [Tooltip("Opcional. Si existe, Ulises retrocede al recibir daño y deja de moverse al morir.")]
        [SerializeField] private Health health;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float controlLockTimer;
        private int extraJumpsLeft;

        // Plataforma que se está atravesando al bajar (abajo + saltar).
        private Collider2D groundCollider;
        private Collider2D droppingPlatform;
        private float dropTimer;

        public PlayerConfig Config => config;
        public bool IsGrounded { get; private set; }
        /// <summary>1 = mirando a la derecha, -1 = a la izquierda.</summary>
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;
        /// <summary>Multiplicador externo de velocidad: lodo (&lt; 1), café de olla (&gt; 1).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public bool HasControl => controlLockTimer <= 0f && (health == null || !health.IsDead);

        /// <summary>true si Ulises está parado en una plataforma que se puede atravesar hacia abajo.</summary>
        public bool IsOnDropThroughPlatform => IsGrounded && CanDropThrough(groundCollider);

        public event Action Jumped;
        public event Action DoubleJumped;
        public event Action Landed;
        public event Action DroppedThrough;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            if (config == null || input == null || groundCheck == null)
            {
                Debug.LogError($"{name}: PlayerController necesita Config, Input y Ground Check asignados en el Inspector.", this);
                enabled = false;
                return;
            }

            body.gravityScale = config.gravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            extraJumpsLeft = config.extraJumps;
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

        private void Update()
        {
            float dt = Time.deltaTime;
            coyoteTimer -= dt;
            jumpBufferTimer -= dt;
            controlLockTimer -= dt;
            UpdateDropThrough(dt);

            // Se consume siempre para que un salto pulsado durante el aturdimiento no se dispare después.
            bool jumpPressed = input.ConsumeJumpPressed();
            if (!HasControl)
                return;

            bool holdingDown = input.Move.y <= -config.dropInputThreshold;
            if (jumpPressed && holdingDown && IsOnDropThroughPlatform)
                DropThroughPlatform();
            else if (jumpPressed)
                jumpBufferTimer = config.jumpBufferTime;

            TryJump();
            UpdateFacing(input.Move.x);
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            if (HasControl)
                MoveHorizontally(Time.fixedDeltaTime);

            ApplyGravity();
        }

        /// <summary>Empuja a Ulises y le quita el control un momento (daño, rebote en enemigos).</summary>
        public void ApplyKnockback(Vector2 velocity, float lockTime)
        {
            body.linearVelocity = velocity;
            controlLockTimer = lockTime;
        }

        /// <summary>
        /// Baja de la plataforma actual atravesándola. Solo funciona en plataformas de un sentido
        /// (con PlatformEffector2D); el suelo normal no se puede atravesar.
        /// </summary>
        public bool DropThroughPlatform()
        {
            if (!IsOnDropThroughPlatform || bodyCollider == null)
                return false;

            droppingPlatform = groundCollider;
            dropTimer = config.dropThroughTime;
            Physics2D.IgnoreCollision(bodyCollider, droppingPlatform, true);

            IsGrounded = false;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            // Pequeño empujón hacia abajo para que no se quede "flotando" sobre la orilla.
            body.linearVelocity = new Vector2(body.linearVelocity.x, -2f);
            DroppedThrough?.Invoke();
            return true;
        }

        private void UpdateDropThrough(float dt)
        {
            if (droppingPlatform == null)
                return;

            dropTimer -= dt;
            if (dropTimer > 0f)
                return;

            // La plataforma es de un sentido: aunque Ulises siga dentro, no lo empuja hacia arriba.
            Physics2D.IgnoreCollision(bodyCollider, droppingPlatform, false);
            droppingPlatform = null;
        }

        private static bool CanDropThrough(Collider2D platform) =>
            platform != null && platform.usedByEffector && platform.GetComponent<PlatformEffector2D>() != null;

        private void UpdateGrounded()
        {
            bool wasGrounded = IsGrounded;
            groundCollider = Physics2D.OverlapBox(groundCheck.position, config.groundCheckSize, 0f, config.groundLayers);
            // Mientras atraviesa una plataforma, esa plataforma no cuenta como suelo.
            if (groundCollider != null && groundCollider == droppingPlatform)
                groundCollider = null;

            // Al despegar el sensor sigue tocando el suelo un instante; solo cuenta si no va subiendo.
            IsGrounded = groundCollider != null && body.linearVelocity.y <= 0.01f;

            if (IsGrounded)
            {
                coyoteTimer = config.coyoteTime;
                extraJumpsLeft = config.extraJumps;
                if (!wasGrounded)
                    Landed?.Invoke();
            }
        }

        private void TryJump()
        {
            if (jumpBufferTimer <= 0f)
                return;

            if (coyoteTimer > 0f)
            {
                Jump(config.jumpHeight);
                coyoteTimer = 0f;
                Jumped?.Invoke();
            }
            else if (extraJumpsLeft > 0)
            {
                extraJumpsLeft--;
                Jump(config.doubleJumpHeight);
                DoubleJumped?.Invoke();
            }
        }

        private void Jump(float height)
        {
            jumpBufferTimer = 0f;
            IsGrounded = false;
            body.linearVelocity = new Vector2(body.linearVelocity.x, config.JumpVelocityFor(height));
        }

        private void MoveHorizontally(float dt)
        {
            float target = input.Move.x * config.moveSpeed * SpeedMultiplier;
            float rate = Mathf.Abs(target) > 0.01f ? config.acceleration : config.deceleration;
            if (!IsGrounded)
                rate *= config.airControl;

            float x = Mathf.MoveTowards(body.linearVelocity.x, target, rate * dt);
            body.linearVelocity = new Vector2(x, body.linearVelocity.y);
        }

        private void ApplyGravity()
        {
            float vy = body.linearVelocity.y;
            float multiplier = 1f;
            if (vy < 0f)
                multiplier = config.fallGravityMultiplier;
            else if (vy > 0f && !input.JumpHeld)
                multiplier = config.jumpCutGravityMultiplier;

            body.gravityScale = config.gravityScale * multiplier;

            if (vy < -config.maxFallSpeed)
                body.linearVelocity = new Vector2(body.linearVelocity.x, -config.maxFallSpeed);
        }

        private void UpdateFacing(float moveX)
        {
            if (Mathf.Abs(moveX) < 0.01f)
                return;

            int direction = moveX > 0f ? 1 : -1;
            if (direction == FacingDirection)
                return;

            FacingDirection = direction;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void OnDamaged(DamageInfo info)
        {
            float away = transform.position.x >= info.SourcePosition.x ? 1f : -1f;
            ApplyKnockback(new Vector2(away * config.hurtKnockback.x, config.hurtKnockback.y), config.hurtControlLock);
        }

        private void OnDied()
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null || groundCheck == null)
                return;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, config.groundCheckSize);
        }
    }
}
