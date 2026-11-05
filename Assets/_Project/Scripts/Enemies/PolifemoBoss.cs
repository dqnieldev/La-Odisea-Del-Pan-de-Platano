using System;
using System.Collections;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Polifemo, el jefe del Mercado. Espera en la cima y, cuando Ulises llega a su piso, empieza la pelea: camina hacia
    /// él, le da puñetazos de cerca (a veces uno potente, que avisa y se lanza hacia adelante) y le avienta plátanos de
    /// lejos. Los golpes de Ulises no le hacen nada (<see cref="IDamageBlocker"/>) hasta que la nube de un costal de harina
    /// lo marea (<see cref="Daze"/>): tose, se queda mareado unos segundos y ahí cada golpe le quita vida. La vida va en
    /// rondas: al perder la de una ronda se recompone, más rápido y más enojado. Al acabarse la última cae y aparece el
    /// premio (el racimo de plátano, la meta).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PolifemoBoss : MonoBehaviour, IDamageBlocker
    {
        [Header("Partes")]
        [SerializeField] private Health health;
        [Tooltip("Daño por tocarlo; se apaga mientras está mareado o tirado.")]
        [SerializeField] private Hazard contact;
        [SerializeField] private Animator animator;
        [Tooltip("Dibujo: se voltea hacia donde mira (dibujado hacia la derecha).")]
        [SerializeField] private Transform visual;
        [SerializeField] private Transform player;
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private LayerMask groundLayers;

        [Header("Arena")]
        [Tooltip("La pelea empieza cuando los pies de Ulises llegan a esta altura (el piso de la cima).")]
        [SerializeField] private float startHeight;
        [SerializeField] private float minX = -11.5f;
        [SerializeField] private float maxX = 11.5f;
        [Tooltip("Lo que aparece al vencerlo (el racimo). Se activa con un saltito.")]
        [SerializeField] private GameObject reward;
        [SerializeField, Min(0f)] private float rewardDelay = 1.2f;

        [Header("Caminar")]
        [SerializeField, Min(0f)] private float walkSpeed = 2f;
        [Tooltip("Cuánto más rápido camina en cada ronda nueva.")]
        [SerializeField, Min(0f)] private float speedPerRound = 0.6f;
        [Tooltip("Pausa después de cada ataque.")]
        [SerializeField, Min(0f)] private float restAfterAttack = 0.7f;

        [Header("Puñetazo")]
        [SerializeField, Min(0f)] private float punchRange = 2.2f;
        [Tooltip("Se queda quieto este rato antes de soltar el golpe (el aviso).")]
        [SerializeField, Min(0f)] private float punchWarning = 0.3f;
        [Tooltip("Del inicio de la animación al cuadro en que pega.")]
        [SerializeField, Min(0f)] private float punchHitTime = 0.17f;
        [SerializeField, Min(0f)] private float punchDuration = 0.45f;
        [SerializeField] private Vector2 punchOffset = new(1.5f, 1.9f);
        [SerializeField] private Vector2 punchSize = new(1.6f, 1.4f);

        [Header("Golpe potente")]
        [SerializeField, Range(0f, 1f)] private float powerChance = 0.35f;
        [SerializeField, Min(0f)] private float powerWarning = 0.6f;
        [SerializeField, Min(0f)] private float powerHitTime = 0.29f;
        [SerializeField, Min(0f)] private float powerDuration = 0.7f;
        [Tooltip("Lo que avanza mientras echa el golpe potente.")]
        [SerializeField, Min(0f)] private float powerLunge = 2.5f;
        [SerializeField] private Vector2 powerSize = new(2.2f, 1.8f);

        [Header("Plátanos")]
        [SerializeField] private Projectile bananaPrefab;
        [Tooltip("Avienta plátanos si Ulises está al menos a esta distancia.")]
        [SerializeField, Min(0f)] private float throwMinDistance = 5f;
        [SerializeField, Min(0f)] private float throwCooldown = 3f;
        [SerializeField, Min(0f)] private float throwReleaseTime = 0.25f;
        [SerializeField, Min(0f)] private float throwDuration = 0.55f;
        [Tooltip("De dónde sale el plátano (mirando a la derecha): a la altura del pecho de Ulises, para que lo salte.")]
        [SerializeField] private Vector2 throwHand = new(1.3f, 1.1f);
        [SerializeField, Min(0f)] private float bananaSpeed = 7f;

        [Header("Mareo")]
        [SerializeField, Min(0f)] private float coughDuration = 0.7f;
        [SerializeField, Min(0f)] private float dizzyDuration = 5f;
        [SerializeField, Min(0f)] private float hurtDuration = 0.25f;
        [Tooltip("Golpes que aguanta en cada ronda (la vida total son rondas × esto). Si el mareo se acaba antes, lo que " +
                 "le falte de la ronda se lo quitan en el siguiente.")]
        [SerializeField, Min(1)] private int hitsPerRound = 3;
        [Tooltip("Al recomponerse se queda un momento parado antes de seguir (enojado).")]
        [SerializeField, Min(0f)] private float recoverPause = 0.6f;

        [Header("Sonidos")]
        [SerializeField] private SoundEffect punchSound = new();
        [SerializeField] private SoundEffect throwSound = new();
        [SerializeField] private SoundEffect dizzySound = new();
        [SerializeField] private SoundEffect fallSound = new();

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int PunchState = Animator.StringToHash("Punch");
        private static readonly int PowerState = Animator.StringToHash("Power");
        private static readonly int ThrowState = Animator.StringToHash("Throw");
        private static readonly int CoughState = Animator.StringToHash("Cough");
        private static readonly int DizzyState = Animator.StringToHash("Dizzy");
        private static readonly int HurtState = Animator.StringToHash("Hurt");
        private static readonly int FallState = Animator.StringToHash("Fall");

        private Rigidbody2D body;
        private Health playerHealth;
        private int facing = -1;
        private int round;
        private int roundHits;
        private float throwTimer;
        private bool dazed;
        private bool hurting;
        private int currentState = -1;
        private Coroutine routine;

        public bool Started { get; private set; }
        public bool Defeated { get; private set; }
        /// <summary>Empezó la pelea (aparece la barra de vida).</summary>
        public event Action FightStarted;
        /// <summary>Se recompuso del mareo: empieza otra ronda (los costales se rellenan).</summary>
        public event Action RoundStarted;
        public event Action Fell;
        public Health Health => health;
        public int HitsPerRound => hitsPerRound;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            if (player != null)
                playerHealth = player.GetComponentInParent<Health>();
            if (reward != null)
                reward.SetActive(false);
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

        // Si el puntaje lo quitó al empezar (ya lo había vencido antes de reintentar), el premio sale de una vez.
        private void OnDestroy()
        {
            if (!Defeated && reward != null && Time.timeSinceLevelLoad < 0.5f && gameObject.scene.isLoaded)
                reward.SetActive(true);
        }

        private void Update()
        {
            throwTimer -= Time.deltaTime;
            if (Started || player == null || player.position.y < startHeight - 0.05f)
                return;
            Started = true;
            FightStarted?.Invoke();
            routine = StartCoroutine(Fight(firstPause: 0.8f));
        }

        // ---------- Pelea ----------

        private IEnumerator Fight(float firstPause)
        {
            Play(IdleState);
            Face(player.position.x - body.position.x);
            yield return new WaitForSeconds(firstPause);
            throwTimer = throwCooldown * 0.5f;
            while (true)
            {
                if (playerHealth != null && playerHealth.IsDead)
                {
                    Play(IdleState);
                    yield break;
                }
                float offset = player.position.x - body.position.x;
                float distance = Mathf.Abs(offset);
                bool sameFloor = Mathf.Abs(player.position.y - body.position.y) < 1.5f;
                Face(offset);
                if (distance <= punchRange && sameFloor)
                {
                    yield return UnityEngine.Random.value < powerChance ? Power() : Punch();
                    yield return Rest();
                }
                else if (distance >= throwMinDistance && throwTimer <= 0f && bananaPrefab != null)
                {
                    yield return Throw();
                    yield return Rest();
                }
                else
                {
                    Play(WalkState);
                    float step = Mathf.Sign(offset) * CurrentSpeed * Time.deltaTime;
                    // Si no está en su piso (brincando encima de él), camina hasta quedar abajo y espera.
                    if (distance > 0.3f)
                        MoveTo(body.position.x + step);
                    else
                        Play(IdleState);
                    yield return null;
                }
            }
        }

        private float CurrentSpeed => walkSpeed + speedPerRound * round;

        private IEnumerator Rest()
        {
            Play(IdleState);
            yield return new WaitForSeconds(Mathf.Max(0.2f, restAfterAttack - 0.15f * round));
        }

        private IEnumerator Punch()
        {
            Play(IdleState);
            yield return new WaitForSeconds(punchWarning);
            Play(PunchState, restart: true);
            punchSound.Play();
            yield return new WaitForSeconds(punchHitTime);
            HitBox(punchOffset, punchSize);
            yield return new WaitForSeconds(Mathf.Max(0f, punchDuration - punchHitTime));
        }

        // El aviso: tiembla en su lugar; luego se lanza hacia adelante con el puño.
        private IEnumerator Power()
        {
            Play(IdleState);
            float timer = 0f;
            while (timer < powerWarning)
            {
                timer += Time.deltaTime;
                visual.localPosition = new Vector3(Mathf.Sin(timer * 70f) * 0.06f, 0f, 0f);
                yield return null;
            }
            visual.localPosition = Vector3.zero;
            Play(PowerState, restart: true);
            punchSound.Play();
            float start = body.position.x;
            timer = 0f;
            bool hit = false;
            while (timer < powerDuration)
            {
                timer += Time.deltaTime;
                MoveTo(start + facing * powerLunge * Mathf.Clamp01(timer / powerHitTime));
                if (!hit && timer >= powerHitTime)
                {
                    hit = true;
                    HitBox(punchOffset, powerSize);
                }
                yield return null;
            }
        }

        private IEnumerator Throw()
        {
            throwTimer = throwCooldown;
            Play(ThrowState, restart: true);
            yield return new WaitForSeconds(throwReleaseTime);
            var hand = new Vector2(throwHand.x * facing, throwHand.y);
            Projectile banana = Instantiate(bananaPrefab, body.position + hand, Quaternion.identity);
            banana.Launch(new Vector2(facing * bananaSpeed, 0f), 1, 5f, playerLayers, groundLayers, gameObject);
            throwSound.Play();
            yield return new WaitForSeconds(Mathf.Max(0f, throwDuration - throwReleaseTime));
        }

        private void HitBox(Vector2 offset, Vector2 size)
        {
            var center = body.position + new Vector2(offset.x * facing, offset.y);
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f, playerLayers))
            {
                // Solo Ulises (lo que tiene vida): sus bolitas de masa están en la misma capa.
                if (hit.GetComponentInParent<IDamageable>() is { } target && hit.GetComponentInParent<Health>() != null)
                {
                    target.TakeDamage(new DamageInfo(1, body.position, gameObject));
                    break;
                }
            }
        }

        // ---------- Mareo y rondas ----------

        /// <summary>La nube de harina lo alcanzó: tose y se marea. Lo llama el costal.</summary>
        public void Daze()
        {
            if (!Started || Defeated || dazed)
                return;
            if (routine != null)
                StopCoroutine(routine);
            visual.localPosition = Vector3.zero;
            routine = StartCoroutine(Dizzy());
        }

        private IEnumerator Dizzy()
        {
            dazed = true;
            roundHits = 0;
            SetContact(false);
            Play(CoughState, restart: true);
            dizzySound.Play();
            yield return new WaitForSeconds(coughDuration);
            Play(DizzyState);
            float timer = 0f;
            while (timer < dizzyDuration && !RoundLost && !Defeated)
            {
                timer += Time.deltaTime;
                if (!hurting)
                    Play(DizzyState);
                yield return null;
            }
            if (Defeated)
                yield break;
            // Se recompone: si perdió la vida de la ronda, la que sigue va más rápido.
            if (RoundLost)
                round++;
            dazed = false;
            hurting = false;
            SetContact(true);
            RoundStarted?.Invoke();
            routine = StartCoroutine(Fight(recoverPause));
        }

        // Ya le quitaron en este mareo lo que le quedaba de la ronda (su vida llegó a un múltiplo de hitsPerRound).
        private bool RoundLost => roundHits > 0 && health.Current % hitsPerRound == 0;

        public bool TryBlock(DamageInfo info) => !dazed || hurting || RoundLost || Defeated;

        private void OnDamaged(DamageInfo info)
        {
            roundHits++;
            if (!health.IsDead)
                StartCoroutine(Hurt());
        }

        private IEnumerator Hurt()
        {
            hurting = true;
            Play(HurtState, restart: true);
            yield return new WaitForSeconds(hurtDuration);
            hurting = false;
            if (dazed)
                Play(DizzyState);
        }

        private void OnDied()
        {
            Defeated = true;
            dazed = false;
            StopAllCoroutines();
            visual.localPosition = Vector3.zero;
            SetContact(false);
            Play(FallState, restart: true);
            fallSound.Play();
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

        // ---------- Ayudantes ----------

        private void SetContact(bool on)
        {
            if (contact != null)
                contact.enabled = on;
        }

        private void MoveTo(float x) => body.MovePosition(new Vector2(Mathf.Clamp(x, minX, maxX), body.position.y));

        private void Face(float offset)
        {
            if (Mathf.Abs(offset) < 0.1f)
                return;
            int side = offset > 0f ? 1 : -1;
            if (side == facing)
                return;
            facing = side;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            visual.localScale = scale;
        }

        private void Play(int state, bool restart = false)
        {
            if (animator == null || (state == currentState && !restart))
                return;
            currentState = state;
            animator.Play(state, 0, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector2 position = transform.position;
            Gizmos.DrawWireCube(position + new Vector2(punchOffset.x * facing, punchOffset.y), punchSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(minX, startHeight), new Vector3(maxX, startHeight));
        }
    }
}
