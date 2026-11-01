using System.Collections.Generic;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Fuego vivo en la boca del horno, encima del dibujo: lenguas de fuego que suben y bajan, un resplandor que
    /// parpadea, chispitas que saltan y un brillo cálido que respira sobre el cuarto. Todo con un punto difuso; el
    /// objeto va en el centro de la base del fuego (sobre la leña).
    /// </summary>
    public class OvenFire : MonoBehaviour
    {
        [Tooltip("Punto blanco difuso (se tiñe).")]
        [SerializeField] private Sprite softDot;
        [Tooltip("Ancho y alto de la boca del horno (unidades).")]
        [SerializeField] private Vector2 mouthSize = new(0.8f, 0.6f);
        [SerializeField] private int sortingOrder = -15;

        [Header("Llamas")]
        [SerializeField, Min(0)] private int flameCount = 7;
        [SerializeField] private Color flameColor = new(1f, 0.72f, 0.22f, 0.75f);
        [SerializeField] private Color flameCore = new(1f, 0.93f, 0.6f, 0.85f);

        [Header("Resplandor")]
        [SerializeField] private Color glowColor = new(1f, 0.5f, 0.12f, 0.5f);
        [Tooltip("Brillo cálido sobre el cuarto, alrededor del horno.")]
        [SerializeField] private Color roomGlowColor = new(1f, 0.6f, 0.25f, 0.13f);
        [SerializeField, Min(0f)] private float roomGlowSize = 5f;

        [Header("Chispas")]
        [SerializeField, Min(0f)] private float sparksPerSecond = 5f;
        [SerializeField] private Color sparkColor = new(1f, 0.85f, 0.45f, 1f);

        private sealed class Spark
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Age, Life, Size;
        }

        private readonly List<(SpriteRenderer outer, SpriteRenderer core, float x, float seed)> flames = new();
        private readonly List<Spark> sparks = new();
        private readonly Stack<SpriteRenderer> pool = new();
        private SpriteRenderer glow, roomGlow;
        private float sparkTimer;

        private void Start()
        {
            if (softDot == null)
            {
                enabled = false;
                return;
            }
            roomGlow = Dot("Brillo del cuarto", sortingOrder - 1, roomGlowColor);
            glow = Dot("Resplandor", sortingOrder, glowColor);
            for (int i = 0; i < flameCount; i++)
            {
                // Más altas al centro, como una fogata.
                float x = flameCount > 1 ? Mathf.Lerp(-0.35f, 0.35f, i / (flameCount - 1f)) * mouthSize.x : 0f;
                flames.Add((Dot("Llama", sortingOrder + 1, flameColor), Dot("Corazón de la llama", sortingOrder + 2, flameCore), x,
                    Random.Range(0f, 100f)));
            }
        }

        private void Update()
        {
            float t = Time.time;
            // Parpadeo general: ruido lento más un temblor rápido.
            float flicker = 0.75f + 0.18f * Mathf.PerlinNoise(t * 1.7f, 0f) + 0.07f * Mathf.PerlinNoise(t * 9f, 3f);

            Place(glow, new Vector2(0f, mouthSize.y * 0.45f), mouthSize * (2.1f + 0.25f * flicker), glowColor, flicker);
            Place(roomGlow, new Vector2(0f, mouthSize.y * 0.3f), Vector2.one * roomGlowSize * (0.95f + 0.08f * flicker),
                roomGlowColor, flicker);

            foreach ((SpriteRenderer outer, SpriteRenderer core, float x, float seed) in flames)
            {
                float center = 1f - Mathf.Abs(x) / (mouthSize.x * 0.5f) * 0.6f;
                float height = mouthSize.y * (0.5f + 0.5f * Mathf.PerlinNoise(t * 3.2f, seed)) * center;
                float width = mouthSize.x * 0.34f;
                float sway = (Mathf.PerlinNoise(t * 2.5f, seed + 7f) - 0.5f) * 0.06f;
                Place(outer, new Vector2(x + sway, height * 0.5f), new Vector2(width, height), flameColor, 1f);
                Place(core, new Vector2(x + sway * 0.6f, height * 0.32f), new Vector2(width * 0.5f, height * 0.6f), flameCore, 1f);
            }

            UpdateSparks(Time.deltaTime);
        }

        private void UpdateSparks(float dt)
        {
            sparkTimer -= dt;
            if (sparksPerSecond > 0f && sparkTimer <= 0f)
            {
                sparkTimer = Random.Range(0.4f, 1.6f) / sparksPerSecond;
                SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : Dot("Chispa", sortingOrder + 3, sparkColor);
                renderer.gameObject.SetActive(true);
                renderer.transform.localPosition = new Vector3(Random.Range(-0.3f, 0.3f) * mouthSize.x, mouthSize.y * 0.2f, 0f);
                sparks.Add(new Spark
                {
                    Renderer = renderer,
                    Velocity = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.5f, 0.9f)),
                    Life = Random.Range(0.6f, 1.2f),
                    Size = Random.Range(0.04f, 0.08f),
                });
            }
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = sparks[i];
                spark.Age += dt;
                float p = spark.Age / spark.Life;
                if (p >= 1f)
                {
                    spark.Renderer.gameObject.SetActive(false);
                    pool.Push(spark.Renderer);
                    sparks.RemoveAt(i);
                    continue;
                }
                // Suben, se frenan y titilan mientras se apagan.
                spark.Velocity.y -= 0.35f * dt;
                spark.Renderer.transform.localPosition += (Vector3)(spark.Velocity * dt);
                spark.Renderer.transform.localScale = Vector3.one * spark.Size * (1f - p * 0.5f);
                float twinkle = 0.6f + 0.4f * Mathf.Sin((spark.Age + i) * 30f);
                spark.Renderer.color = new Color(sparkColor.r, sparkColor.g, sparkColor.b, sparkColor.a * (1f - p) * twinkle);
            }
        }

        private static void Place(SpriteRenderer renderer, Vector2 position, Vector2 size, Color color, float alpha)
        {
            renderer.transform.localPosition = position;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            renderer.color = new Color(color.r, color.g, color.b, color.a * alpha);
        }

        private SpriteRenderer Dot(string objectName, int order, Color color)
        {
            var renderer = new GameObject(objectName).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sprite = softDot;
            renderer.sortingOrder = order;
            renderer.color = color;
            return renderer;
        }
    }
}
