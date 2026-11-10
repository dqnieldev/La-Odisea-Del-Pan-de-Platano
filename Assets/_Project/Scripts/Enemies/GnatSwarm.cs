using Odisea.Core;
using Odisea.Level;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Enjambre de jejenes del nivel 2: va hacia la boca del horno a ratos, sin un camino fijo. Zumba un momento casi en
    /// su lugar y luego se lanza rápido un tramo, subiendo o bajando a otra altura; ya cerca del horno se lanza directo a
    /// la boca. Si llega, le pica la masa (le quita vida al horno) y se dispersa. Aguanta varios golpes: con cada uno se
    /// achica (se deshace parte del enjambre) y se dispersa hacia arriba un instante. Daña a Ulises al tocarlo.
    /// </summary>
    public class GnatSwarm : MonoBehaviour
    {
        [SerializeField] private Health health;
        [Tooltip("Velocidad al lanzarse.")]
        [SerializeField, Min(0f)] private float speed = 6.5f;
        [Tooltip("Velocidad mientras zumba (casi en su lugar, acercándose poco a poco).")]
        [SerializeField, Min(0f)] private float hoverSpeed = 0.8f;
        [SerializeField] private Vector2 hoverTime = new(0.2f, 0.45f);
        [Tooltip("Lo que avanza hacia el horno en cada arrancón.")]
        [SerializeField] private Vector2 dashLength = new(1.8f, 3.2f);
        [Tooltip("Cuánto puede subir o bajar en cada arrancón.")]
        [SerializeField, Min(0f)] private float dashRise = 2.2f;
        [Tooltip("Alturas entre las que vuela (mundo).")]
        [SerializeField] private Vector2 heightLimits = new(1.4f, 5.2f);
        [Tooltip("A esta distancia del horno ya se lanza directo a la boca.")]
        [SerializeField, Min(0f)] private float finalDash = 3.5f;
        [Tooltip("El temblor del zumbido.")]
        [SerializeField, Min(0f)] private float buzz = 0.1f;
        [SerializeField, Min(1)] private int ovenDamage = 1;
        [Tooltip("Sin horno en el nivel (La Tovara): a quién persigue. Vacío = Ulises.")]
        [SerializeField] private Transform target;

        [Header("Al recibir un golpe (sin morir)")]
        [Tooltip("Tamaño que le queda (se deshace parte del enjambre).")]
        [SerializeField, Range(0.3f, 1f)] private float hurtShrink = 0.72f;
        [SerializeField, Min(0f)] private float scatterHeight = 1.3f;
        [SerializeField, Min(0.01f)] private float scatterTime = 0.35f;
        [SerializeField] private SoundEffect stingSound = new();

        private Oven oven;
        private Vector2 position;
        private Vector2 dashTarget;
        private bool dashing;
        private float phaseTimer;
        private float size = 1f;
        private float baseScale = 1f;
        private float scatterTimer;
        private Vector2 scatterVelocity;
        private float seed;
        private bool dead;

        public void Setup(Oven target) => oven = target;

        private void OnEnable()
        {
            if (health == null)
                return;
            health.Died += OnDied;
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health == null)
                return;
            health.Died -= OnDied;
            health.Damaged -= OnDamaged;
        }

        private void Start()
        {
            if (oven == null)
                oven = FindAnyObjectByType<Oven>();
            if (oven == null && target == null)
                target = FindAnyObjectByType<Odisea.Player.PlayerController>()?.transform;
            position = transform.position;
            baseScale = Mathf.Abs(transform.localScale.x);
            seed = Random.value * 100f;
            // Entra a la pantalla con un arrancón.
            StartDash();
        }

        private void Update()
        {
            if (dead || (oven != null ? oven.Burned : target == null))
                return;
            Vector2 mouth = Goal();
            Vector2 limits = Limits();
            float dt = Time.deltaTime;

            if (scatterTimer > 0f)
            {
                // Golpeado: se dispersa hacia arriba (y lejos de quien le pegó), frenándose.
                scatterTimer -= dt;
                position += scatterVelocity * (dt * Mathf.Clamp01(scatterTimer / scatterTime) * 2f);
                position.y = Mathf.Clamp(position.y, limits.x, limits.y);
                if (scatterTimer <= 0f)
                    StartHover();
            }
            else if (dashing)
            {
                position = Vector2.MoveTowards(position, dashTarget, speed * dt);
                phaseTimer -= dt;
                if ((position - dashTarget).sqrMagnitude < 0.0004f || phaseTimer <= 0f)
                    StartHover();
            }
            else
            {
                position = Vector2.MoveTowards(position, mouth, hoverSpeed * dt);
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                    StartDash();
            }

            // El zumbido: un temblor irregular encima del movimiento.
            float t = Time.time * 13f + seed;
            Vector2 jitter = new(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f);
            transform.position = new Vector3(position.x + jitter.x * buzz * 2f, position.y + jitter.y * buzz * 2f, transform.position.z);
            float facing = Mathf.Sign(mouth.x - position.x);
            if (Mathf.Approximately(facing, 0f))
                facing = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(baseScale * size * facing, baseScale * size, 1f);

            if (oven == null)
            {
                // Persiguiendo a Ulises: al alcanzarlo le pica (su Hazard) y se aparta un momento antes de volver.
                if (scatterTimer <= 0f && (position - mouth).sqrMagnitude < 0.36f)
                {
                    float away = Mathf.Sign(position.x - mouth.x);
                    if (Mathf.Approximately(away, 0f))
                        away = 1f;
                    scatterVelocity = new Vector2(away * 1.6f, scatterHeight) / scatterTime;
                    scatterTimer = scatterTime;
                    dashing = false;
                }
                return;
            }
            if ((position - mouth).sqrMagnitude > 0.09f)
                return;
            // Llegó: pica la masa y se dispersa.
            dead = true;
            stingSound.Play();
            oven.Hit(ovenDamage);
            Destroy(gameObject);
        }

        private void StartHover()
        {
            dashing = false;
            phaseTimer = Random.Range(hoverTime.x, hoverTime.y);
        }

        // Un tramo rápido hacia el horno, a otra altura; ya cerca, directo a la boca.
        private void StartDash()
        {
            dashing = true;
            Vector2 mouth = Goal();
            Vector2 limits = Limits();
            float remaining = Mathf.Abs(mouth.x - position.x);
            if (remaining <= finalDash)
                dashTarget = mouth;
            else
            {
                float step = Mathf.Min(Random.Range(dashLength.x, dashLength.y), remaining - finalDash * 0.5f);
                float y = position.y + Random.Range(-dashRise, dashRise);
                // Si casi no cambió de altura, que el cambio se note.
                if (Mathf.Abs(y - position.y) < dashRise * 0.4f)
                    y = position.y + Mathf.Sign(y - position.y + 0.001f) * dashRise * 0.6f;
                dashTarget = new Vector2(position.x + Mathf.Sign(mouth.x - position.x) * step,
                    Mathf.Clamp(y, limits.x, limits.y));
            }
            phaseTimer = Vector2.Distance(position, dashTarget) / Mathf.Max(0.01f, speed) + 0.1f;
        }

        // A dónde va: la boca del horno o, sin horno, un poco arriba de Ulises.
        private Vector2 Goal()
        {
            if (oven != null)
                return oven.Mouth;
            return target != null ? (Vector2)target.position + Vector2.up * 0.4f : position;
        }

        // Alturas entre las que vuela: fijas con horno; persiguiendo a Ulises, desde su altura hasta un poco arriba.
        private Vector2 Limits() => oven != null || target == null
            ? heightLimits
            : new Vector2(target.position.y - 0.5f, target.position.y + 4.5f);

        private void OnDamaged(DamageInfo info)
        {
            if (dead || health.IsDead)
                return;
            size *= hurtShrink;
            float away = Mathf.Sign(position.x - info.SourcePosition.x);
            if (Mathf.Approximately(away, 0f))
                away = 1f;
            scatterVelocity = new Vector2(away * 0.8f, scatterHeight) / scatterTime;
            scatterTimer = scatterTime;
            dashing = false;
        }

        private void OnDied() => dead = true;
    }
}
