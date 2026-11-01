using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Odisea.Map
{
    /// <summary>
    /// Vida del mapa, todo hecho con un punto difuso: destellos en el mar, sombras de nubes que cruzan despacio,
    /// gaviotas que pasan de vez en cuando y humo de la chimenea de la panadería. También deja polvito
    /// (<see cref="Puff"/>) donde lo pidan, como los pasos de Ulises.
    /// </summary>
    public class MapAmbience : MonoBehaviour
    {
        [Tooltip("Punto blanco difuso (se tiñe).")]
        [SerializeField] private Sprite softDot;
        [Tooltip("Rectángulo blanco (para las alas de las gaviotas).")]
        [SerializeField] private Sprite square;
        [Tooltip("Todo el mapa, en unidades del mundo (por donde cruzan nubes y gaviotas).")]
        [SerializeField] private Rect bounds = new(-32f, -18f, 64f, 36f);
        [SerializeField] private int sortingOrder = 60;

        [Header("Mar")]
        [Tooltip("Zonas de agua donde brillan los destellos.")]
        [SerializeField] private Rect[] seaAreas = Array.Empty<Rect>();
        [SerializeField, Min(0)] private int sparkleCount = 30;
        [SerializeField] private Color sparkleColor = new(1f, 1f, 0.95f, 1f);

        [Header("Nubes")]
        [SerializeField, Min(0)] private int cloudCount = 3;
        [SerializeField] private Color cloudShadow = new(0.1f, 0.12f, 0.15f, 0.14f);
        [SerializeField, Min(0f)] private float cloudSpeed = 0.6f;

        [Header("Gaviotas")]
        [SerializeField] private Vector2 gullInterval = new(5f, 11f);
        [SerializeField] private Color gullColor = new(0.17f, 0.12f, 0.09f, 0.9f);
        [SerializeField, Min(0f)] private float gullSpeed = 4.5f;

        [Header("Humo")]
        [Tooltip("Punta de la chimenea de la panadería; sin chimenea (0, 0 y desactivado), no hay humo.")]
        [SerializeField] private bool smoke = true;
        [SerializeField] private Vector2 chimney;
        [SerializeField, Min(0.05f)] private float smokeInterval = 0.45f;
        [SerializeField] private Color smokeColor = new(0.96f, 0.95f, 0.92f, 0.8f);

        private sealed class Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Age, Life, StartSize, EndSize;
            public Color Color;
        }

        private sealed class Sparkle
        {
            public SpriteRenderer Renderer;
            public float Start, Life, Size;
        }

        private sealed class Gull
        {
            public Transform Body;
            public Transform LeftWing, RightWing;
            public float Speed, Phase;
        }

        private readonly List<Particle> particles = new();
        private readonly Stack<SpriteRenderer> pool = new();
        private readonly List<Sparkle> sparkles = new();
        private readonly List<Transform> clouds = new();
        private readonly List<Gull> gulls = new();
        private float nextGull, nextSmoke;

        private void Start()
        {
            if (softDot == null)
            {
                enabled = false;
                return;
            }
            if (seaAreas.Length > 0)
            {
                for (int i = 0; i < sparkleCount; i++)
                {
                    var sparkle = new Sparkle { Renderer = NewRenderer("Destello", softDot, sortingOrder) };
                    RestartSparkle(sparkle, Time.time - Random.Range(0f, 2f));
                    sparkles.Add(sparkle);
                }
            }
            for (int i = 0; i < cloudCount; i++)
            {
                // Una sombra de nube: tres manchas difusas juntas.
                var cloud = new GameObject("Sombra de nube").transform;
                cloud.SetParent(transform, false);
                for (int b = 0; b < 3; b++)
                {
                    // Debajo de los letreros: la sombra pasa sobre el dibujo y los edificios.
                    SpriteRenderer blob = NewRenderer("Mancha", softDot, sortingOrder - 25);
                    blob.transform.SetParent(cloud, false);
                    blob.transform.localPosition = new Vector3((b - 1) * 3.2f, (b % 2) * 1.1f, 0f);
                    blob.transform.localScale = Vector3.one * (b == 1 ? 9f : 7f);
                    blob.color = cloudShadow;
                }
                cloud.position = new Vector3(Mathf.Lerp(bounds.xMin, bounds.xMax, (i + 0.5f) / cloudCount),
                    Random.Range(bounds.yMin + 4f, bounds.yMax - 4f), 0f);
                clouds.Add(cloud);
            }
            nextGull = Time.time + Random.Range(1f, 3f);
        }

        private void Update()
        {
            float now = Time.time, dt = Time.deltaTime;
            foreach (Sparkle sparkle in sparkles)
                UpdateSparkle(sparkle, now);
            foreach (Transform cloud in clouds)
            {
                cloud.position += Vector3.right * (cloudSpeed * dt);
                if (cloud.position.x > bounds.xMax + 12f)
                    cloud.position = new Vector3(bounds.xMin - 12f, Random.Range(bounds.yMin + 4f, bounds.yMax - 4f), 0f);
            }
            UpdateGulls(now, dt);
            if (smoke && now >= nextSmoke)
            {
                nextSmoke = now + smokeInterval * Random.Range(0.8f, 1.2f);
                Emit(chimney + new Vector2(Random.Range(-0.1f, 0.1f), 0f), new Vector2(Random.Range(0.15f, 0.45f), 0.9f), 2.6f,
                    0.6f, 2.2f, smokeColor);
            }
            UpdateParticles(dt);
        }

        /// <summary>Polvito que se abre y se desvanece (pasos de Ulises en el camino).</summary>
        public void Puff(Vector2 position, Color color)
        {
            for (int i = 0; i < 2; i++)
            {
                var velocity = new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.1f, 0.5f));
                Emit(position + new Vector2(Random.Range(-0.15f, 0.15f), 0f), velocity, 0.45f, 0.35f, 0.9f, color);
            }
        }

        /// <summary>Estallido de chispas que salen en círculo (un lugar que se estrena).</summary>
        public void Burst(Vector2 position, Color color, int count = 24)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i + Random.Range(0f, 0.6f)) / count * Mathf.PI * 2f;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(2.5f, 5f);
                Emit(position, velocity, Random.Range(0.6f, 1f), Random.Range(0.35f, 0.6f), 0.1f, color);
            }
        }

        // ---------- Mar ----------

        private void RestartSparkle(Sparkle sparkle, float now)
        {
            Rect area = seaAreas[Random.Range(0, seaAreas.Length)];
            sparkle.Renderer.transform.position = new Vector3(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax), 0f);
            sparkle.Start = now + Random.Range(0f, 1.5f);
            sparkle.Life = Random.Range(0.6f, 1.3f);
            sparkle.Size = Random.Range(0.45f, 0.85f);
        }

        // Crece y se apaga como un reflejo del sol; luego aparece en otro lado.
        private void UpdateSparkle(Sparkle sparkle, float now)
        {
            float p = (now - sparkle.Start) / sparkle.Life;
            if (p >= 1f)
            {
                RestartSparkle(sparkle, now);
                p = 0f;
            }
            float shine = p <= 0f ? 0f : Mathf.Sin(p * Mathf.PI);
            sparkle.Renderer.transform.localScale = new Vector3(sparkle.Size * 1.8f, sparkle.Size * 0.7f, 1f) * shine;
            sparkle.Renderer.color = new Color(sparkleColor.r, sparkleColor.g, sparkleColor.b, sparkleColor.a * shine);
        }

        // ---------- Gaviotas ----------

        private void UpdateGulls(float now, float dt)
        {
            if (square != null && now >= nextGull)
            {
                nextGull = now + Random.Range(gullInterval.x, gullInterval.y);
                SpawnGull();
            }
            for (int i = gulls.Count - 1; i >= 0; i--)
            {
                Gull gull = gulls[i];
                gull.Body.position += new Vector3(gull.Speed * dt, Mathf.Sin(now * 1.3f + gull.Phase) * 0.3f * dt, 0f);
                // Aletea: las alas suben y bajan.
                float flap = Mathf.Sin(now * 9f + gull.Phase) * 22f;
                gull.LeftWing.localRotation = Quaternion.Euler(0f, 0f, -25f - flap);
                gull.RightWing.localRotation = Quaternion.Euler(0f, 0f, 25f + flap);
                bool gone = gull.Speed > 0f ? gull.Body.position.x > bounds.xMax + 2f : gull.Body.position.x < bounds.xMin - 2f;
                if (gone)
                {
                    Destroy(gull.Body.gameObject);
                    gulls.RemoveAt(i);
                }
            }
        }

        private void SpawnGull()
        {
            bool right = Random.value < 0.5f;
            var body = new GameObject("Gaviota").transform;
            body.SetParent(transform, false);
            body.position = new Vector3(right ? bounds.xMin - 1.5f : bounds.xMax + 1.5f,
                Random.Range(bounds.center.y, bounds.yMax - 2f), 0f);
            float scale = Random.Range(0.8f, 1.2f);
            body.localScale = Vector3.one * scale;
            gulls.Add(new Gull
            {
                Body = body,
                LeftWing = Wing(body, -1f),
                RightWing = Wing(body, 1f),
                Speed = (right ? 1f : -1f) * gullSpeed * Random.Range(0.8f, 1.2f),
                Phase = Random.Range(0f, 10f),
            });
        }

        // Ala: un trazo delgado con el pivote en el cuerpo.
        private Transform Wing(Transform body, float side)
        {
            var pivot = new GameObject(side < 0f ? "Ala izquierda" : "Ala derecha").transform;
            pivot.SetParent(body, false);
            SpriteRenderer stroke = NewRenderer("Trazo", square, sortingOrder + 5);
            stroke.transform.SetParent(pivot, false);
            stroke.transform.localPosition = new Vector3(side * 0.45f, 0f, 0f);
            stroke.transform.localScale = new Vector3(0.9f / square.bounds.size.x, 0.13f / square.bounds.size.y, 1f);
            stroke.color = gullColor;
            return pivot;
        }

        // ---------- Partículas (humo y polvo) ----------

        private void Emit(Vector2 position, Vector2 velocity, float life, float startSize, float endSize, Color color)
        {
            SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : NewRenderer("Partícula", softDot, sortingOrder + 2);
            renderer.gameObject.SetActive(true);
            renderer.transform.position = position;
            particles.Add(new Particle
            {
                Renderer = renderer, Velocity = velocity, Life = life, StartSize = startSize, EndSize = endSize, Color = color,
            });
        }

        private void UpdateParticles(float dt)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.Age += dt;
                float p = particle.Age / particle.Life;
                if (p >= 1f)
                {
                    particle.Renderer.gameObject.SetActive(false);
                    pool.Push(particle.Renderer);
                    particles.RemoveAt(i);
                    continue;
                }
                particle.Renderer.transform.position += (Vector3)(particle.Velocity * dt);
                particle.Renderer.transform.localScale = Vector3.one * Mathf.Lerp(particle.StartSize, particle.EndSize, p);
                // Entra rápido y se desvanece despacio.
                float alpha = Mathf.Min(1f, p * 6f) * (1f - p);
                particle.Renderer.color = new Color(particle.Color.r, particle.Color.g, particle.Color.b, particle.Color.a * alpha);
            }
        }

        private SpriteRenderer NewRenderer(string objectName, Sprite sprite, int order)
        {
            var renderer = new GameObject(objectName).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
