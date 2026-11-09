using System;
using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// El horno del nivel 2, al centro de la pantalla: tiene vida propia (no es un enemigo, por eso no usa
    /// <see cref="Health"/>, que el puntaje cuenta como enemigo). Los enemigos que lo alcanzan le pegan con
    /// <see cref="Hit"/>; al quedarse sin vida se quema el pan y Ulises pierde (Game Over).
    /// </summary>
    public class Oven : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 10;
        [Tooltip("Lo que tiembla y se pone rojo al recibir un golpe (el dibujo del horno).")]
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        [Tooltip("Boca del horno: hacia donde vuelan los jejenes.")]
        [SerializeField] private Transform mouth;
        [Tooltip("Medio ancho del horno: hasta donde llegan los enemigos que caminan.")]
        [SerializeField, Min(0f)] private float halfWidth = 1.3f;
        [Tooltip("La vida de Ulises: si se quema el pan, pierde.")]
        [SerializeField] private Health player;
        [SerializeField] private Color hitColor = new(1f, 0.45f, 0.35f);
        [Tooltip("Opcional: resplandor rojo que se prende al recibir un golpe (cuando el horno es parte del fondo).")]
        [SerializeField] private SpriteRenderer hitGlow;
        [SerializeField, Range(0f, 1f)] private float hitGlowAlpha = 0.55f;
        [Header("Estados")]
        [Tooltip("Opcional: el dibujo del horno y sus versiones de sano a quemado; cambia al bajar la vida (el último, al quemarse).")]
        [SerializeField] private SpriteRenderer stateRenderer;
        [SerializeField] private Sprite[] states = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float hitDuration = 0.25f;
        [Tooltip("Con una sola vida, el horno late de este color (a punto de quemarse).")]
        [SerializeField] private Color lastLifeColor = new(1f, 0.4f, 0.3f);
        [Tooltip("Al empezar el nivel, el horno parpadea de este color (\"¡Protege el horno!\").")]
        [SerializeField] private Color highlightColor = new(1f, 0.85f, 0.2f);
        [SerializeField, Min(0.1f)] private float highlightBlinks = 3f;
        [Tooltip("Opcional: resplandor amarillo encima del horno mientras parpadea.")]
        [SerializeField] private SpriteRenderer highlightGlow;
        [SerializeField, Range(0f, 1f)] private float highlightGlowAlpha = 0.6f;
        [SerializeField] private SoundEffect hitSound = new();

        private float hitTimer;
        private float highlightTimer, highlightDuration;
        private Vector3 rest;
        private Color[] baseColors;
        private Color stateBaseColor = Color.white;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool Burned { get; private set; }
        public float HalfWidth => halfWidth;
        public Vector2 Mouth => mouth != null ? (Vector2)mouth.position : (Vector2)transform.position + Vector2.up;

        /// <summary>(actual, máximo).</summary>
        public event Action<int, int> Changed;
        public event Action BurnedUp;

        private void Awake()
        {
            Current = maxHealth;
            if (visual != null)
                rest = visual.localPosition;
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
            if (stateRenderer != null)
                stateBaseColor = stateRenderer.color;
            if (highlightGlow != null)
                highlightGlow.color = new Color(highlightGlow.color.r, highlightGlow.color.g, highlightGlow.color.b, 0f);
        }

        private void Start() => Changed?.Invoke(Current, maxHealth);

        /// <summary>Parpadea de amarillo un rato para llamar la atención (al empezar el nivel).</summary>
        public void Highlight(float duration)
        {
            highlightDuration = highlightTimer = Mathf.Max(0.01f, duration);
        }

        /// <summary>Un enemigo le pegó al horno.</summary>
        public void Hit(int amount)
        {
            if (Burned || amount <= 0 || (player != null && player.IsDead))
                return;
            Current = Mathf.Max(0, Current - amount);
            hitTimer = hitDuration;
            hitSound.Play();
            Changed?.Invoke(Current, maxHealth);
            ShowState();
            if (Current == 0)
                Burn();
        }

        /// <summary>Se quemó el pan (sin vida, o Polifemo llegó al horno): Ulises pierde.</summary>
        public void Burn()
        {
            if (Burned)
                return;
            Burned = true;
            Current = 0;
            ShowState();
            Changed?.Invoke(Current, maxHealth);
            BurnedUp?.Invoke();
            if (player != null)
                player.Kill();
        }

        // No hay barra de vida: el dibujo la muestra. Cada vida perdida pasa a una versión más dañada (las que sobran
        // repiten la más dañada, que entonces late en rojo con la última vida); quemado al llegar a 0.
        private void ShowState()
        {
            if (stateRenderer == null || states.Length == 0)
                return;
            int last = states.Length - 1;
            int lost = maxHealth - Current;
            int state = Current <= 0 ? last
                : Mathf.Clamp(Mathf.CeilToInt((float)lost / Mathf.Max(1, maxHealth - 1) * (last - 1)), 0, last - 1);
            if (stateRenderer.sprite == states[state])
                return;
            stateRenderer.sprite = states[state];
            // Al cambiar de versión, una sacudida más larga.
            hitTimer = hitDuration * 2f;
        }

        private void Update()
        {
            if (hitTimer <= 0f && highlightTimer > 0f)
            {
                // Al empezar: parpadeos amarillos que se van apagando al final.
                highlightTimer -= Time.deltaTime;
                float elapsed = highlightDuration - highlightTimer;
                float blink = Mathf.Abs(Mathf.Sin(elapsed / highlightDuration * highlightBlinks * Mathf.PI));
                float amount = highlightTimer > 0f ? blink : 0f;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].color = Color.Lerp(baseColors[i], highlightColor, amount * 0.75f);
                }
                // El dibujo del horno se entinta y se prende el resplandor encima.
                if (stateRenderer != null)
                    stateRenderer.color = Color.Lerp(stateBaseColor, highlightColor, amount * 0.45f);
                if (highlightGlow != null)
                    highlightGlow.color = new Color(highlightGlow.color.r, highlightGlow.color.g, highlightGlow.color.b,
                        highlightGlowAlpha * amount);
                return;
            }
            if (hitTimer <= 0f)
            {
                // Última vida: late en rojo, con su resplandor.
                float pulse = Current == 1 && !Burned ? Mathf.Abs(Mathf.Sin(Time.time * 4f)) : 0f;
                if (hitGlow != null)
                    hitGlow.color = new Color(hitGlow.color.r, hitGlow.color.g, hitGlow.color.b, hitGlowAlpha * 0.7f * pulse);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].color = Color.Lerp(baseColors[i], lastLifeColor * baseColors[i], pulse);
                }
                return;
            }
            hitTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(hitTimer / hitDuration);
            if (hitGlow != null)
                hitGlow.color = new Color(hitGlow.color.r, hitGlow.color.g, hitGlow.color.b, hitGlowAlpha * t);
            if (visual != null)
                visual.localPosition = rest + new Vector3(Mathf.Sin(Time.time * 70f) * 0.08f * t, 0f, 0f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = Color.Lerp(baseColors[i], hitColor * baseColors[i], t);
            }
        }
    }
}
