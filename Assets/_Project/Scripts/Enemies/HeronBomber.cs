using Odisea.Core;
using Odisea.Player;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// La garza de La Tovara: acompaña a Ulises por todo el nivel volando alto (fuera del alcance de sus saltos), como
    /// Madame Circe en el Mercado: no se le puede vencer. Entra a los pocos segundos y, cada tanto, vuela hacia donde
    /// Ulises va a estar, se detiene sacudiendo las alas (aviso) y suelta un pescado podrido (<see cref="HeronDrop"/>),
    /// cuya sombra en el piso avisa dónde cae. Entre tirada y tirada se pasea por la pantalla. Deja de tirar si Ulises
    /// muere y se va volando al llegar al final del nivel.
    /// </summary>
    public class HeronBomber : MonoBehaviour
    {
        [Tooltip("El dibujo: se voltea hacia donde vuela y aletea.")]
        [SerializeField] private Transform visual;
        [Tooltip("Opcional: alas del dibujo de relleno (se estiran al aletear).")]
        [SerializeField] private Transform[] wings = new Transform[0];
        [Tooltip("Con su dibujo: el SpriteRenderer y los cuadros del aleteo.")]
        [SerializeField] private SpriteRenderer drawing;
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;
        [Tooltip("Hacia dónde mira el dibujo sin voltear (el de relleno mira a la izquierda).")]
        [SerializeField] private bool drawingFacesRight;
        [Tooltip("Molde del pescado (apagado); se copia en cada tirada.")]
        [SerializeField] private HeronDrop dropTemplate;

        [Header("Vuelo")]
        [Tooltip("Qué tan arriba del centro de la cámara vuela.")]
        [SerializeField] private float heightAboveView = 4.2f;
        [Tooltip("Velocidad (respecto a la pantalla) al ir hacia Ulises o al pasearse.")]
        [SerializeField, Min(0.1f)] private float speed = 7f;
        [SerializeField, Min(0f)] private float bob = 0.3f;
        [Tooltip("Segundos antes de entrar al empezar el nivel.")]
        [SerializeField, Min(0f)] private float startDelay = 3f;
        [Tooltip("Al pasar Ulises de esta x (cerca del festival) se va.")]
        [SerializeField] private float leaveX = 1000f;

        [Header("Tirar")]
        [Tooltip("Segundos entre tiradas (al azar entre los dos).")]
        [SerializeField] private Vector2 interval = new(3f, 5f);
        [Tooltip("Lo que se detiene en el aire antes de soltar el pescado (aviso).")]
        [SerializeField, Min(0f)] private float warnTime = 0.5f;
        [Tooltip("Lo que tarda el pescado en llegar al piso (para adelantarse a Ulises).")]
        [SerializeField, Min(0f)] private float fallTime = 1f;
        [SerializeField] private SoundEffect squawk = new();
        [SerializeField] private SoundEffect dropSound = new();

        private enum State { Waiting, Wander, Hunt, Warning, Leaving }

        private PlayerController player;
        private Health playerHealth;
        private Camera view;
        private State state = State.Waiting;
        private float timer;
        private float nextDrop;
        private float lastViewX;
        private float wanderX;
        private float huntX;
        private int facing = -1;
        private float flap;
        private Vector3 visualScale;

        private void Awake()
        {
            if (visual != null)
                visualScale = visual.localScale;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            playerHealth = player != null ? player.GetComponent<Health>() : null;
            view = Camera.main;
            if (view != null)
                lastViewX = view.transform.position.x;
            // Fuera de la vista, por la derecha, hasta que entra.
            SetVisible(false);
        }

        private float HalfWidth => view.orthographicSize * view.aspect;

        private void Update()
        {
            if (view == null)
                return;
            float dt = Time.deltaTime;
            timer += dt;
            flap += dt * (state == State.Warning ? 24f : 11f);

            // Acompaña a la cámara: su x es relativa a la pantalla.
            float viewX = view.transform.position.x;
            Vector3 position = transform.position;
            position.x += viewX - lastViewX;
            lastViewX = viewX;
            float height = view.transform.position.y + heightAboveView + Mathf.Sin(Time.time * 2f) * bob;

            bool playerGone = playerHealth != null && playerHealth.IsDead;
            if (state != State.Leaving && state != State.Waiting && player != null && player.transform.position.x > leaveX)
                state = State.Leaving;

            switch (state)
            {
                case State.Waiting:
                    if (timer >= startDelay)
                    {
                        position = new Vector3(viewX + HalfWidth + 2f, height, 0f);
                        SetVisible(true);
                        Wander(viewX);
                    }
                    break;
                case State.Wander:
                    position.x = Approach(position.x, wanderX + viewX, dt);
                    position.y = height;
                    if (Mathf.Abs(position.x - (wanderX + viewX)) < 0.3f)
                        wanderX = Random.Range(-HalfWidth + 2f, HalfWidth - 2f);
                    if (Time.time >= nextDrop && !playerGone)
                        state = State.Hunt;
                    break;
                case State.Hunt:
                    if (player == null || playerGone)
                    {
                        Wander(viewX);
                        break;
                    }
                    // Hacia donde va a estar Ulises cuando el pescado llegue al piso (sin salirse de la pantalla).
                    huntX = player.transform.position.x + player.Velocity.x * (warnTime + fallTime);
                    huntX = Mathf.Clamp(huntX, viewX - HalfWidth + 1f, viewX + HalfWidth - 1f);
                    position.x = Approach(position.x, huntX, dt * 1.3f);
                    position.y = height;
                    if (Mathf.Abs(position.x - huntX) < 0.4f)
                    {
                        state = State.Warning;
                        timer = 0f;
                        squawk.Play();
                    }
                    break;
                case State.Warning:
                    position.y = height;
                    if (timer >= warnTime)
                    {
                        Drop(position);
                        Wander(viewX);
                    }
                    break;
                case State.Leaving:
                    position += new Vector3(-speed * 0.5f, speed * 0.7f, 0f) * dt;
                    facing = -1;
                    if (position.y > view.transform.position.y + view.orthographicSize + 3f)
                        gameObject.SetActive(false);
                    break;
            }
            transform.position = position;
            Animate();
        }

        private void Wander(float viewX)
        {
            state = State.Wander;
            wanderX = Random.Range(-HalfWidth + 2f, HalfWidth - 2f);
            nextDrop = Time.time + Random.Range(interval.x, interval.y);
        }

        // Se acerca a la x deseada sin pasar de su velocidad, y voltea hacia donde va.
        private float Approach(float x, float target, float dt)
        {
            float step = Mathf.Clamp(target - x, -speed * dt, speed * dt);
            if (Mathf.Abs(step) > 0.001f)
                facing = step < 0f ? -1 : 1;
            return x + step;
        }

        private void Drop(Vector3 position)
        {
            if (dropTemplate == null)
                return;
            HeronDrop drop = Instantiate(dropTemplate, position + Vector3.down * 0.4f, Quaternion.identity);
            drop.gameObject.SetActive(true);
            drop.Launch(Vector2.zero);
            dropSound.Play();
        }

        private void SetVisible(bool visible)
        {
            if (visual != null)
                visual.gameObject.SetActive(visible);
        }

        private void Animate()
        {
            if (visual == null)
                return;
            // Se voltea según hacia dónde vuela (y hacia dónde mira el dibujo).
            float wobble = state == State.Warning ? Mathf.Sin(flap * 1.3f) * 0.06f : 0f;
            int flip = drawingFacesRight ? facing : -facing;
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * flip, visualScale.y, visualScale.z);
            if (drawing != null && frames.Length > 0)
            {
                // Aletea con sus cuadros; al avisar, más rápido.
                float rate = framesPerSecond * (state == State.Warning ? 1.8f : 1f);
                drawing.sprite = frames[(int)(Time.time * rate) % frames.Length];
            }
            visual.localPosition = new Vector3(wobble, 0f, 0f);
            float wing = Mathf.Sin(flap);
            foreach (Transform w in wings)
            {
                if (w == null)
                    continue;
                Vector3 s = w.localScale;
                w.localScale = new Vector3(s.x, Mathf.Abs(s.x) * 0.45f * (0.35f + 0.65f * Mathf.Abs(wing)), s.z);
                w.localPosition = new Vector3(w.localPosition.x, 0.15f + wing * 0.18f, 0f);
            }
        }
    }
}
