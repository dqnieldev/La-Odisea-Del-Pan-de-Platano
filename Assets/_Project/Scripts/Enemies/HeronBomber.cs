using Odisea.Core;
using Odisea.Level;
using Odisea.Player;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Garza de La Tovara: entra por la orilla de la pantalla volando a media altura hacia Ulises. Cuando queda casi
    /// encima de donde él va a estar, se detiene en el aire un instante (aviso: sacude las alas) y suelta un pescado
    /// podrido (<see cref="HeronDrop"/>). Al salir de la vista da la vuelta para otra pasada; después de
    /// <see cref="passes"/> se va volando hacia arriba (si se escapa, no cuenta en el puntaje). Se le pega con la masa
    /// saltando; tocarla hace daño (Hazard).
    /// </summary>
    public class HeronBomber : MonoBehaviour
    {
        [SerializeField] private Health health;
        [Tooltip("El dibujo: se voltea hacia donde vuela y aletea.")]
        [SerializeField] private Transform visual;
        [Tooltip("Opcional: alas del dibujo de relleno (se estiran al aletear).")]
        [SerializeField] private Transform[] wings = new Transform[0];
        [Tooltip("Molde del pescado (apagado); se copia en cada tirada.")]
        [SerializeField] private HeronDrop dropTemplate;

        [Header("Vuelo")]
        [Tooltip("Altura del vuelo (y del mundo).")]
        [SerializeField] private float flyHeight = 5f;
        [SerializeField, Min(0.1f)] private float speed = 4.5f;
        [SerializeField, Min(0f)] private float bob = 0.25f;
        [SerializeField, Min(1)] private int passes = 2;

        [Header("Tirar")]
        [Tooltip("Cuánto adelanta a Ulises (segundos de su carrera) para apuntar.")]
        [SerializeField, Min(0f)] private float lead = 0.35f;
        [Tooltip("Lo que se detiene en el aire antes de soltar el pescado (aviso).")]
        [SerializeField, Min(0f)] private float warnTime = 0.45f;
        [SerializeField] private SoundEffect squawk = new();
        [SerializeField] private SoundEffect dropSound = new();

        private enum State { Flying, Warning, Leaving }

        private PlayerController player;
        private Camera view;
        private State state;
        private int direction = -1;
        private int passesDone;
        private bool droppedThisPass;
        private float stateTime;
        private float flap;
        private Vector3 visualScale;

        private float homeX;

        private void Awake()
        {
            if (visual != null)
                visualScale = visual.localScale;
            homeX = transform.position.x;
        }

        // Al encenderse (cuando la cámara llega a su lugar) aparece en la orilla derecha de la pantalla. Al cargar la
        // escena se enciende un instante antes de que ActivateOnView la apague: ahí no se mueve.
        private void OnEnable()
        {
            player = FindAnyObjectByType<PlayerController>();
            view = Camera.main;
            if (view == null)
                return;
            float half = view.orthographicSize * view.aspect;
            float rightEdge = view.transform.position.x + half;
            if (rightEdge < homeX - 4f)
                return;
            transform.position = new Vector3(rightEdge + 1f, flyHeight, 0f);
            direction = -1;
            state = State.Flying;
            passesDone = 0;
            droppedThisPass = false;
        }

        private void Update()
        {
            if (health != null && health.IsDead)
                return;
            float dt = Time.deltaTime;
            stateTime += dt;
            flap += dt * (state == State.Warning ? 22f : 11f);

            switch (state)
            {
                case State.Flying:
                    Fly(dt);
                    break;
                case State.Warning:
                    // Se queda en el aire sacudiendo las alas y suelta el pescado.
                    if (stateTime >= warnTime)
                    {
                        Drop();
                        state = State.Flying;
                        stateTime = 0f;
                    }
                    break;
                case State.Leaving:
                    transform.position += new Vector3(direction * speed * 0.6f, speed * 0.8f, 0f) * dt;
                    if (view == null || transform.position.y > view.transform.position.y + view.orthographicSize + 2f)
                    {
                        // Se escapó: no cuenta como derrotada ni para el máximo.
                        if (LevelScore.Current != null && health != null)
                            LevelScore.Current.Discount(health);
                        gameObject.SetActive(false);
                    }
                    break;
            }
            Animate();
        }

        private void Fly(float dt)
        {
            Vector3 position = transform.position;
            position.x += direction * speed * dt;
            position.y = flyHeight + Mathf.Sin(Time.time * 2.2f) * bob;
            transform.position = position;

            if (player != null && !droppedThisPass)
            {
                float target = player.transform.position.x + player.Velocity.x * (lead + warnTime);
                // Cuando el punto donde va a estar Ulises le queda debajo (o un poquito adelante), avisa.
                if (Mathf.Abs(position.x - target) < 0.6f || (position.x - target) * direction > 0f &&
                    Mathf.Abs(position.x - target) < 2f)
                {
                    droppedThisPass = true;
                    state = State.Warning;
                    stateTime = 0f;
                    squawk.Play();
                    return;
                }
            }

            // Fuera de la vista (por el lado hacia donde va): da la vuelta o se va.
            if (view == null)
                return;
            float half = view.orthographicSize * view.aspect;
            float edge = view.transform.position.x + direction * (half + 1.5f);
            if ((position.x - edge) * direction > 0f)
            {
                passesDone++;
                if (passesDone >= passes)
                {
                    state = State.Leaving;
                    return;
                }
                direction = -direction;
                droppedThisPass = false;
            }
        }

        private void Drop()
        {
            if (dropTemplate == null)
                return;
            HeronDrop drop = Instantiate(dropTemplate, transform.position + Vector3.down * 0.4f, Quaternion.identity);
            drop.gameObject.SetActive(true);
            drop.Launch(new Vector2(direction * speed * 0.25f, 0f));
            dropSound.Play();
        }

        private void Animate()
        {
            if (visual == null)
                return;
            float wobble = state == State.Warning ? Mathf.Sin(flap * 1.3f) * 0.06f : 0f;
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * -direction, visualScale.y, visualScale.z);
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
