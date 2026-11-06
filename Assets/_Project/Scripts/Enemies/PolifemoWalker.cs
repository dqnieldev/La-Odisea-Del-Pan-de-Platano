using System;
using System.Collections;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Polifemo en el nivel 2: cuando el pan ya está horneado sale de un lado y camina despacio hacia el horno, con pisadas
    /// pesadas (la cámara tiembla y sale polvo de sus botas). Cada tanto se detiene y avienta ollas de barro en arco: a
    /// Ulises o al horno, y a veces dos seguidas. Cuando Ulises lo ataca de lejos, de vez en cuando se cubre y la masa le
    /// rebota (el rodillo sí le pega). A media vida se enoja: camina más rápido y avienta más seguido. Cada golpe de Ulises
    /// lo hace retroceder un poco y lo detiene un instante; si llega al horno, se quema el pan. Al acabarse su vida cae y
    /// aparece el premio (el pan horneado, la meta). Daña a Ulises al tocarlo. El controlador trae los estados Idle, Walk,
    /// Throw, Hurt, Guard y Fall (se ponen con Animator.Play); el dibujo mira a la derecha y se voltea con
    /// <see cref="visual"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PolifemoWalker : MonoBehaviour, IDamageBlocker
    {
        [SerializeField] private Health health;
        [SerializeField] private Hazard contact;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private Oven oven;
        [Tooltip("La vida de Ulises: a dónde avienta y para burlarse cuando le atina.")]
        [SerializeField] private Health player;
        [SerializeField, Min(0f)] private float speed = 0.8f;
        [Tooltip("Lo que retrocede con cada golpe.")]
        [SerializeField, Min(0f)] private float pushBack = 0.6f;
        [Tooltip("Lo que se queda quieto después de un golpe.")]
        [SerializeField, Min(0f)] private float hurtPause = 0.45f;
        [Tooltip("Hasta dónde llega (además del medio ancho del horno) antes de quemarlo.")]
        [SerializeField] private float reach = 0.7f;
        [Tooltip("Lo que aparece al vencerlo (el pan horneado: la meta). Se activa con un saltito.")]
        [SerializeField] private GameObject reward;
        [SerializeField, Min(0f)] private float rewardDelay = 1.2f;

        [Header("Pisadas")]
        [Tooltip("Cada cuánto pisa fuerte mientras camina (0 = sin pisadas).")]
        [SerializeField, Min(0f)] private float stepTime;
        [SerializeField, Min(0f)] private float shake = 0.18f;
        [Tooltip("Polvo que sale de la bota al pisar (un punto difuso).")]
        [SerializeField] private Sprite dust;
        [SerializeField] private Color dustColor = new(0.78f, 0.66f, 0.5f, 0.7f);
        [SerializeField, Min(0f)] private float footWidth = 1.2f;

        [Header("Ollas")]
        [Tooltip("Opcional: lo que avienta (sin esto no avienta).")]
        [SerializeField] private ThrownItem potPrefab;
        [Tooltip("Segundos entre una olla y otra (al azar entre los dos).")]
        [SerializeField] private Vector2 throwInterval = new(4f, 6f);
        [Tooltip("Del inicio de la animación al momento en que la suelta, y lo que dura toda.")]
        [SerializeField, Min(0f)] private float throwRelease = 0.45f;
        [SerializeField, Min(0f)] private float throwDuration = 0.8f;
        [Tooltip("De dónde sale la olla (mirando a la derecha): arriba de su cabeza.")]
        [SerializeField] private Vector2 throwHand = new(0.6f, 5.8f);
        [Tooltip("Lo que tarda la olla en llegar (según la distancia, entre los dos).")]
        [SerializeField] private Vector2 flightTime = new(0.8f, 1.3f);
        [Tooltip("No avienta a Ulises si está más cerca que esto (ahí ya lo pisa).")]
        [SerializeField, Min(0f)] private float minThrowDistance = 2.5f;
        [Tooltip("Probabilidad de aventar dos ollas seguidas.")]
        [SerializeField, Range(0f, 1f)] private float doubleThrowChance = 0.3f;
        [Tooltip("Probabilidad de que la olla vaya al horno en lugar de a Ulises (se estrella en su boca y le quita vida).")]
        [SerializeField, Range(0f, 1f)] private float ovenThrowChance = 0.3f;
        [SerializeField, Min(0)] private int ovenPotDamage = 1;

        [Header("Se cubre")]
        [Tooltip("Cada cuánto se cubre cuando Ulises le pega de lejos (al azar entre los dos).")]
        [SerializeField] private Vector2 guardInterval = new(5f, 8f);
        [SerializeField, Min(0f)] private float guardDuration = 1.6f;
        [Tooltip("Golpes de lejos (masa) seguidos que lo hacen cubrirse aunque no le toque todavía.")]
        [SerializeField, Min(1)] private int guardAfterRangedHits = 4;
        [Tooltip("Lo que sale donde rebota la masa.")]
        [SerializeField] private Color blockColor = new(1f, 0.97f, 0.85f, 0.9f);

        [Header("Furia (a media vida)")]
        [SerializeField, Min(1f)] private float rageSpeedMultiplier = 1.5f;
        [SerializeField] private Vector2 rageThrowInterval = new(2.5f, 3.5f);
        [SerializeField, Range(0f, 1f)] private float rageDoubleThrowChance = 0.55f;
        [SerializeField, Range(0f, 1f)] private float rageOvenThrowChance = 0.4f;
        [Tooltip("El dibujo se tiñe de este color mientras está enojado.")]
        [SerializeField] private Color rageTint = new(1f, 0.62f, 0.55f);

        [Header("Sonidos")]
        [SerializeField] private SoundEffect fallSound = new();
        [SerializeField] private SoundEffect hurtSound = new();
        [Tooltip("No repite el quejido antes de esto (el sonido es largo).")]
        [SerializeField, Min(0f)] private float hurtSoundCooldown = 2.5f;
        [SerializeField] private SoundEffect laughSound = new();
        [SerializeField] private SoundEffect throwSound = new();
        [SerializeField] private SoundEffect blockSound = new();

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int ThrowState = Animator.StringToHash("Throw");
        private static readonly int HurtState = Animator.StringToHash("Hurt");
        private static readonly int GuardState = Animator.StringToHash("Guard");
        private static readonly int FallState = Animator.StringToHash("Fall");

        private Rigidbody2D body;
        private float pauseTimer;
        private float throwTimer;
        private float guardTimer;
        private float guarding;
        private float stepTimer;
        private float lastHurtSound = -100f;
        private int rangedHits;
        private bool throwing;
        private bool walking;
        private bool done;
        private int facing = 1;

        public bool Defeated { get; private set; }
        /// <summary>A media vida se enoja (y así se queda).</summary>
        public bool Enraged { get; private set; }
        public bool Guarding => guarding > 0f;
        public Health Health => health;
        public event Action Fell;
        public event Action Raged;

        private float Speed => Enraged ? speed * rageSpeedMultiplier : speed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            if (reward != null)
                reward.SetActive(false);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
            if (player != null)
                player.Damaged += OnPlayerDamaged;
            throwTimer = UnityEngine.Random.Range(throwInterval.x, throwInterval.y) * 0.6f;
            guardTimer = UnityEngine.Random.Range(guardInterval.x, guardInterval.y);
            walking = false;
            // Sale riéndose.
            laughSound.Play();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
            if (player != null)
                player.Damaged -= OnPlayerDamaged;
        }

        private void FixedUpdate()
        {
            if (done || oven == null)
                return;
            if (oven.Burned)
            {
                Stop(IdleState);
                return;
            }
            float dx = oven.transform.position.x - body.position.x;
            if (throwing)
                return;
            if (guarding > 0f)
            {
                guarding -= Time.fixedDeltaTime;
                if (guarding <= 0f)
                    walking = false;
                return;
            }
            Face(dx);
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.fixedDeltaTime;
                return;
            }
            if (Mathf.Abs(dx) <= oven.HalfWidth + reach)
            {
                Stop(IdleState);
                oven.Burn();
                return;
            }
            throwTimer -= Time.fixedDeltaTime;
            guardTimer -= Time.fixedDeltaTime;
            if (potPrefab != null && throwTimer <= 0f && player != null && !player.IsDead)
            {
                StartCoroutine(Throw());
                return;
            }
            if (!walking)
            {
                walking = true;
                stepTimer = 0f;
                Play(WalkState);
            }
            body.MovePosition(body.position + new Vector2(Mathf.Sign(dx) * Speed * Time.fixedDeltaTime, 0f));
            if (stepTime <= 0f)
                return;
            // Pisa fuerte al inicio de la caminata y luego cada stepTime (los cuadros en que la bota toca el piso).
            stepTimer -= Time.fixedDeltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer += Enraged ? stepTime / rageSpeedMultiplier : stepTime;
                Stomp();
            }
        }

        // A Ulises (si no lo tiene encima) o al horno; a veces dos seguidas.
        private IEnumerator Throw()
        {
            throwing = true;
            walking = false;
            int count = UnityEngine.Random.value < (Enraged ? rageDoubleThrowChance : doubleThrowChance) ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                bool atOven = UnityEngine.Random.value < (Enraged ? rageOvenThrowChance : ovenThrowChance) ||
                              Mathf.Abs(player.transform.position.x - body.position.x) < minThrowDistance;
                Vector2 target = atOven ? oven.Mouth : (Vector2)player.transform.position;
                Face(target.x - body.position.x);
                Play(ThrowState);
                yield return new WaitForSeconds(throwRelease);
                if (Defeated || player == null)
                    break;
                var from = body.position + new Vector2(throwHand.x * facing, throwHand.y);
                Vector2 delta = target - from;
                float time = Mathf.Lerp(flightTime.x, flightTime.y, Mathf.InverseLerp(2f, 14f, Mathf.Abs(delta.x)));
                ThrownItem pot = Instantiate(potPrefab, from, Quaternion.identity);
                pot.Throw(new Vector2(delta.x / time, delta.y / time + 0.5f * pot.Gravity * time));
                if (atOven)
                    pot.HitOvenAfter(oven, time, ovenPotDamage);
                throwSound.Play();
                yield return new WaitForSeconds(Mathf.Max(0f, throwDuration - throwRelease) * (i + 1 < count ? 0.5f : 1f));
            }
            Vector2 interval = Enraged ? rageThrowInterval : throwInterval;
            throwTimer = UnityEngine.Random.Range(interval.x, interval.y);
            throwing = false;
        }

        // Se cubre: se detiene con los brazos al frente y la masa le rebota.
        private void StartGuard()
        {
            guarding = guardDuration;
            guardTimer = UnityEngine.Random.Range(guardInterval.x, guardInterval.y);
            rangedHits = 0;
            walking = false;
            if (player != null)
                Face(player.transform.position.x - body.position.x);
            Play(GuardState);
        }

        /// <summary>Cubierto, la masa (que llega de lejos) no le hace nada; el rodillo (de cerca) sí.</summary>
        public bool TryBlock(DamageInfo info)
        {
            if (Defeated || !IsRanged(info))
                return false;
            if (guarding <= 0f)
            {
                // Lo atacan de lejos: si le toca o ya van varias seguidas, se cubre (ese golpe sí entra).
                rangedHits++;
                if (!throwing && (guardTimer <= 0f || rangedHits >= guardAfterRangedHits))
                    StartCoroutine(GuardSoon());
                return false;
            }
            blockSound.Play();
            if (dust != null)
                FadingSprite.Spawn(dust, info.SourcePosition, blockColor, 12, 0.3f).Grow(2.2f);
            return true;
        }

        // Se cubre justo después del golpe que lo hizo decidirse (no en medio de su reacción).
        private IEnumerator GuardSoon()
        {
            yield return new WaitForSeconds(0.2f);
            if (!Defeated && !throwing && guarding <= 0f)
            {
                pauseTimer = 0f;
                StartGuard();
            }
        }

        // La masa llega de lejos: el golpe sale de donde está la bolita, no de donde está Ulises.
        private static bool IsRanged(DamageInfo info) =>
            info.Source != null && ((Vector2)info.Source.transform.position - info.SourcePosition).sqrMagnitude > 1f;

        private void Stomp()
        {
            CameraFollow2D.Shake(shake, 0.25f);
            if (dust == null)
                return;
            Vector3 feet = transform.position;
            for (int i = 0; i < 3; i++)
            {
                float side = (i - 1) * footWidth * 0.5f;
                FadingSprite.Spawn(dust, feet + new Vector3(side, 0.15f, 0f), dustColor, 9, 0.6f)
                    .Move(new Vector2(side * 1.5f, 0.6f))
                    .Grow(1.8f);
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            if (health.IsDead || oven == null)
                return;
            if (!IsRanged(info))
                rangedHits = 0;
            // Retrocede, alejándose del horno, y suelta lo que estuviera aventando.
            float away = -Mathf.Sign(oven.transform.position.x - body.position.x);
            body.position += new Vector2(away * pushBack, 0f);
            pauseTimer = hurtPause;
            if (throwing)
            {
                StopAllCoroutines();
                throwing = false;
                throwTimer = UnityEngine.Random.Range(throwInterval.x, throwInterval.y);
            }
            walking = false;
            Play(HurtState);
            if (Time.time - lastHurtSound >= hurtSoundCooldown)
            {
                lastHurtSound = Time.time;
                hurtSound.Play();
            }
            if (!Enraged && health.Current <= health.Max / 2)
                Enrage();
        }

        // A media vida: se ríe, la cámara tiembla y se tiñe de rojo; desde ahora camina y avienta más rápido.
        private void Enrage()
        {
            Enraged = true;
            laughSound.Play();
            CameraFollow2D.Shake(shake * 2.5f, 0.6f);
            throwTimer = Mathf.Min(throwTimer, rageThrowInterval.x);
            if (animator != null)
                animator.speed = rageSpeedMultiplier;
            // Con DamageFeedback, el destello de cada golpe regresa a este color.
            if (TryGetComponent(out DamageFeedback feedback))
                feedback.Tint(rageTint);
            else if (visual != null)
            {
                foreach (SpriteRenderer renderer in visual.GetComponentsInChildren<SpriteRenderer>())
                    renderer.color = rageTint;
            }
            Raged?.Invoke();
        }

        // Le atinó una olla a Ulises: se burla.
        private void OnPlayerDamaged(DamageInfo info)
        {
            if (!Defeated && info.Source != null && info.Source.GetComponentInParent<ThrownItem>() != null)
                laughSound.Play();
        }

        private void OnDied()
        {
            Defeated = true;
            done = true;
            guarding = 0f;
            StopAllCoroutines();
            if (contact != null)
                contact.enabled = false;
            if (animator != null)
                animator.speed = 1f;
            Play(FallState);
            fallSound.Play();
            CameraFollow2D.Shake(shake * 2f, 0.5f);
            Fell?.Invoke();
            if (reward != null)
                StartCoroutine(ShowReward());
        }

        private IEnumerator ShowReward()
        {
            yield return new WaitForSeconds(rewardDelay);
            reward.SetActive(true);
            Transform shown = reward.transform;
            Vector3 scale = shown.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                // Crece con un rebote.
                float s = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                shown.localScale = scale * s;
                yield return null;
            }
            shown.localScale = scale;
        }

        private void Stop(int state)
        {
            done = true;
            walking = false;
            Play(state);
        }

        private void Face(float dx)
        {
            if (visual == null || Mathf.Abs(dx) < 0.05f)
                return;
            facing = dx > 0f ? 1 : -1;
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            visual.localScale = scale;
        }

        private void Play(int state)
        {
            if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled)
                animator.Play(state, 0, 0f);
        }
    }
}
