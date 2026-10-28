using System;
using System.Collections.Generic;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Player
{
    /// <summary>
    /// Movimiento de plataformas de Ulises: correr, saltar, doble salto, coyote time,
    /// buffer de salto, salto variable, trepar escaleras y retroceso al recibir daño.
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

        private Ladder currentLadder;
        // Plataformas que se atraviesan al trepar. Se vuelven sólidas cuando Ulises ya no está dentro de ellas.
        private readonly List<Collider2D> ignoredPlatforms = new();
        private readonly List<Collider2D> groundHits = new();
        private ContactFilter2D groundFilter;
        // Distancia del centro a los pies. Los pies se calculan desde el Rigidbody y no desde el Transform,
        // porque con interpolación el Transform va un poco atrasado y causaba que agarrara la escalera de nuevo.
        private Vector2 feetOffset;

        public PlayerConfig Config => config;
        public bool IsGrounded { get; private set; }
        public bool IsClimbing => currentLadder != null;
        /// <summary>1 = mirando a la derecha, -1 = a la izquierda.</summary>
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;
        /// <summary>Multiplicador externo de velocidad: lodo (&lt; 1), café de olla (&gt; 1).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public bool HasControl => controlLockTimer <= 0f && (health == null || !health.IsDead);
        private Vector2 FeetPosition => body.position + feetOffset;

        public event Action Jumped;
        public event Action DoubleJumped;
        public event Action Landed;
        public event Action ClimbStarted;
        public event Action ClimbEnded;

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

            feetOffset = groundCheck.position - transform.position;
            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(config.groundLayers);
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

            // Se consume siempre para que un salto pulsado durante el aturdimiento no se dispare después.
            bool jumpPressed = input.ConsumeJumpPressed();
            if (!HasControl)
                return;

            if (IsClimbing)
            {
                // Saltar suelta la escalera.
                if (jumpPressed)
                {
                    StopClimbing();
                    Jump(config.jumpHeight);
                    Jumped?.Invoke();
                }
                return;
            }

            if (TryStartClimbing())
                return;

            if (jumpPressed)
                jumpBufferTimer = config.jumpBufferTime;

            TryJump();
            UpdateFacing(input.Move.x);
        }

        private void FixedUpdate()
        {
            RestoreIgnoredPlatforms();

            if (IsClimbing)
            {
                Climb(Time.fixedDeltaTime);
                return;
            }

            UpdateGrounded();

            if (HasControl)
                MoveHorizontally(Time.fixedDeltaTime);

            ApplyGravity();
        }

        /// <summary>Empuja a Ulises y le quita el control un momento (daño, rebote en enemigos).</summary>
        public void ApplyKnockback(Vector2 velocity, float lockTime)
        {
            if (IsClimbing)
                StopClimbing();
            body.linearVelocity = velocity;
            controlLockTimer = lockTime;
        }

        /// <summary>Se detiene un momento sin poder moverse (tomar café). En el aire o en la escalera no frena.</summary>
        public void HoldStill(float seconds)
        {
            if (IsGrounded)
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            controlLockTimer = Mathf.Max(controlLockTimer, seconds);
        }

        // ---------- Escaleras ----------

        private bool TryStartClimbing()
        {
            float vertical = input.Move.y;
            if (Mathf.Abs(vertical) < config.climbInputThreshold)
                return false;

            Vector2 feet = FeetPosition;
            Ladder ladder;
            if (vertical > 0f)
            {
                // Subir: la escalera tiene que estar frente a Ulises y continuar por encima de sus pies.
                ladder = FindLadder(body.position) ?? FindLadder(feet + Vector2.up * 0.1f);
                if (ladder == null || feet.y >= ladder.TopY - 0.05f)
                    return false;
            }
            else
            {
                // Bajar: Ulises está parado arriba de la escalera (o ya a media escalera).
                ladder = FindLadder(feet + Vector2.down * 0.15f);
                if (ladder == null || feet.y <= ladder.BottomY + 0.05f)
                    return false;
            }

            StartClimbing(ladder);
            return true;
        }

        private Ladder FindLadder(Vector2 point)
        {
            Collider2D hit = Physics2D.OverlapPoint(point, config.ladderLayers);
            return hit != null ? hit.GetComponent<Ladder>() : null;
        }

        private void StartClimbing(Ladder ladder)
        {
            currentLadder = ladder;
            IsGrounded = false;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;

            foreach (Collider2D platform in ladder.PassThroughPlatforms)
            {
                if (platform == null)
                    continue;
                Physics2D.IgnoreCollision(bodyCollider, platform, true);
                if (!ignoredPlatforms.Contains(platform))
                    ignoredPlatforms.Add(platform);
            }
            ClimbStarted?.Invoke();
        }

        private void StopClimbing()
        {
            if (!IsClimbing)
                return;
            currentLadder = null;
            body.gravityScale = config.gravityScale;
            extraJumpsLeft = config.extraJumps;
            ClimbEnded?.Invoke();
        }

        private void Climb(float dt)
        {
            if (currentLadder == null || !HasControl)
            {
                StopClimbing();
                return;
            }

            // Si algo lo sacó de la escalera (empujón, teletransporte, plataforma móvil), la suelta.
            Vector2 feet = FeetPosition;
            bool offLadder = Mathf.Abs(body.position.x - currentLadder.CenterX) > 1f ||
                             feet.y > currentLadder.TopY + 0.5f || body.position.y < currentLadder.BottomY - 0.5f;
            if (offLadder)
            {
                StopClimbing();
                return;
            }

            float vertical = input.Move.y;
            float vy = Mathf.Abs(vertical) < 0.2f ? 0f : vertical * config.climbSpeed;

            // Se centra en la escalera para que no quede chueco.
            float targetX = Mathf.MoveTowards(body.position.x, currentLadder.CenterX, config.ladderSnapSpeed * dt);
            float vx = (targetX - body.position.x) / dt;
            body.linearVelocity = new Vector2(vx, vy);

            if (vy > 0f && feet.y >= currentLadder.TopY)
            {
                // Llegó arriba: queda parado sobre la plataforma.
                body.position = new Vector2(body.position.x, currentLadder.TopY - feetOffset.y + 0.02f);
                body.linearVelocity = Vector2.zero;
                StopClimbing();
            }
            else if (vy < 0f && HasSolidGroundBelow())
            {
                // Llegó abajo (al suelo o a una plataforma que no atraviesa esta escalera).
                StopClimbing();
            }
        }

        private void RestoreIgnoredPlatforms()
        {
            if (IsClimbing)
                return;

            float feetY = FeetPosition.y;
            for (int i = ignoredPlatforms.Count - 1; i >= 0; i--)
            {
                Collider2D platform = ignoredPlatforms[i];
                // Vuelve a ser sólida si Ulises ya salió de ella o si sus pies quedaron sobre la superficie
                // (Physics2D.Distance marca contacto con separaciones de ~1 cm, por eso no basta solo con eso).
                bool standingOnTop = platform != null && feetY >= platform.bounds.max.y - 0.05f;
                if (platform != null && !standingOnTop && Physics2D.Distance(bodyCollider, platform).isOverlapped)
                    continue;
                if (platform != null)
                    Physics2D.IgnoreCollision(bodyCollider, platform, false);
                ignoredPlatforms.RemoveAt(i);
            }
        }

        // ---------- Suelo y salto ----------

        private void UpdateGrounded()
        {
            bool wasGrounded = IsGrounded;
            // Al despegar el sensor sigue tocando el suelo un instante; solo cuenta si no va subiendo.
            IsGrounded = HasSolidGroundBelow() && body.linearVelocity.y <= 0.01f;

            if (IsGrounded)
            {
                coyoteTimer = config.coyoteTime;
                extraJumpsLeft = config.extraJumps;
                if (!wasGrounded)
                    Landed?.Invoke();
            }
        }

        private bool HasSolidGroundBelow()
        {
            Physics2D.OverlapBox(FeetPosition, config.groundCheckSize, 0f, groundFilter, groundHits);
            foreach (Collider2D hit in groundHits)
            {
                // Una plataforma que se está atravesando no cuenta como suelo.
                if (!ignoredPlatforms.Contains(hit))
                    return true;
            }
            return false;
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
            StopClimbing();
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
