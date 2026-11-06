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
        [SerializeField, Min(0.01f)] private float hitDuration = 0.25f;
        [SerializeField] private SoundEffect hitSound = new();

        private float hitTimer;
        private Vector3 rest;
        private Color[] baseColors;

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
        }

        private void Start() => Changed?.Invoke(Current, maxHealth);

        /// <summary>Un enemigo le pegó al horno.</summary>
        public void Hit(int amount)
        {
            if (Burned || amount <= 0 || (player != null && player.IsDead))
                return;
            Current = Mathf.Max(0, Current - amount);
            hitTimer = hitDuration;
            hitSound.Play();
            Changed?.Invoke(Current, maxHealth);
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
            Changed?.Invoke(Current, maxHealth);
            BurnedUp?.Invoke();
            if (player != null)
                player.Kill();
        }

        private void Update()
        {
            if (hitTimer <= 0f)
                return;
            hitTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(hitTimer / hitDuration);
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
