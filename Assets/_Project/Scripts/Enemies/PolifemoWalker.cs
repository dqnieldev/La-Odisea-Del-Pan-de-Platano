using System;
using System.Collections;
using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Polifemo en el nivel 2: cuando el pan ya está horneado sale de un lado y camina despacio hacia el horno, con pisadas
    /// pesadas (la cámara tiembla y sale polvo de sus botas). Cada tanto se detiene y le avienta una olla de barro a
    /// Ulises, en arco. Cada golpe de Ulises (rodillo o masa) lo hace retroceder un poco y lo detiene un instante; si llega
    /// al horno, se quema el pan. Al acabarse su vida cae y aparece el premio (el pan horneado, la meta). Daña a Ulises al
    /// tocarlo. El controlador trae los estados Idle, Walk, Throw, Hurt y Fall (se ponen con Animator.Play); el dibujo
    /// mira a la derecha y se voltea con <see cref="visual"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PolifemoWalker : MonoBehaviour
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
        [Tooltip("No avienta si Ulises está más cerca que esto (ahí ya lo pisa).")]
        [SerializeField, Min(0f)] private float minThrowDistance = 2.5f;

        [Header("Sonidos")]
        [SerializeField] private SoundEffect fallSound = new();
        [SerializeField] private SoundEffect hurtSound = new();
        [Tooltip("No repite el quejido antes de esto (el sonido es largo).")]
        [SerializeField, Min(0f)] private float hurtSoundCooldown = 2.5f;
        [SerializeField] private SoundEffect laughSound = new();
        [SerializeField] private SoundEffect throwSound = new();

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int ThrowState = Animator.StringToHash("Throw");
        private static readonly int HurtState = Animator.StringToHash("Hurt");
        private static readonly int FallState = Animator.StringToHash("Fall");

        private Rigidbody2D body;
        private float pauseTimer;
        private float throwTimer;
        private float stepTimer;
        private float lastHurtSound = -100f;
        private bool throwing;
        private bool walking;
        private bool done;
        private int facing = 1;

        public bool Defeated { get; private set; }
        public Health Health => health;
        public event Action Fell;

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
            if (potPrefab != null && throwTimer <= 0f && CanThrow())
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
            body.MovePosition(body.position + new Vector2(Mathf.Sign(dx) * speed * Time.fixedDeltaTime, 0f));
            if (stepTime <= 0f)
                return;
            // Pisa fuerte al inicio de la caminata y luego cada stepTime (los cuadros en que la bota toca el piso).
            stepTimer -= Time.fixedDeltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer += stepTime;
                Stomp();
            }
        }

        private bool CanThrow() =>
            player != null && !player.IsDead && Mathf.Abs(player.transform.position.x - body.position.x) >= minThrowDistance;

        private IEnumerator Throw()
        {
            throwing = true;
            walking = false;
            // Se voltea hacia Ulises para aventarle.
            Face(player.transform.position.x - body.position.x);
            Play(ThrowState);
            yield return new WaitForSeconds(throwRelease);
            if (!Defeated && player != null)
            {
                var from = body.position + new Vector2(throwHand.x * facing, throwHand.y);
                Vector2 to = player.transform.position;
                Vector2 delta = to - from;
                float time = Mathf.Lerp(flightTime.x, flightTime.y, Mathf.InverseLerp(2f, 14f, Mathf.Abs(delta.x)));
                ThrownItem pot = Instantiate(potPrefab, from, Quaternion.identity);
                pot.Throw(new Vector2(delta.x / time, delta.y / time + 0.5f * pot.Gravity * time));
                throwSound.Play();
            }
            yield return new WaitForSeconds(Mathf.Max(0f, throwDuration - throwRelease));
            throwTimer = UnityEngine.Random.Range(throwInterval.x, throwInterval.y);
            throwing = false;
        }

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
            StopAllCoroutines();
            if (contact != null)
                contact.enabled = false;
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
