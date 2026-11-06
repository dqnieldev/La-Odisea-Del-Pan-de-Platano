using System.Collections.Generic;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Humo que sale de la chimenea del horno: bocanadas redondas que suben meciéndose, crecen y se desvanecen.
    /// Con el horno dañado sale más seguido, más espeso y más oscuro; quemado, una columna negra. El objeto va en la
    /// boca de la chimenea.
    /// </summary>
    public class ChimneySmoke : MonoBehaviour
    {
        [Tooltip("Círculo blanco (se tiñe): cada bocanada es una bolita de humo.")]
        [SerializeField] private Sprite softDot;
        [Tooltip("Opcional: el horno, para que el humo cambie con su vida.")]
        [SerializeField] private Oven oven;
        [SerializeField] private int sortingOrder = -16;
        [Tooltip("Bocanadas por segundo con el horno sano y cuando ya se quemó.")]
        [SerializeField] private Vector2 rate = new(5f, 12f);
        [SerializeField, Min(0.1f)] private float lifetime = 3.2f;
        [SerializeField, Min(0f)] private float riseSpeed = 1.1f;
        [Tooltip("Hacia dónde se lo lleva el aire (por segundo).")]
        [SerializeField] private float drift = 0.7f;
        [Tooltip("Tamaño al salir y al desvanecerse.")]
        [SerializeField] private Vector2 size = new(0.35f, 1.3f);
        [Tooltip("Ancho de la boca de la chimenea (de dónde salen).")]
        [SerializeField, Min(0f)] private float mouthWidth = 0.6f;
        [SerializeField] private Color cleanSmoke = new(0.93f, 0.91f, 0.88f, 0.75f);
        [SerializeField] private Color darkSmoke = new(0.16f, 0.14f, 0.13f, 0.9f);

        private sealed class Puff
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Life;
            public float Sway;
            public Color Color;
            public float Size;
        }

        private readonly List<Puff> active = new();
        private readonly Stack<SpriteRenderer> pool = new();
        private float timer;

        private float Damage => oven == null ? 0f : oven.Burned ? 1f : 1f - (float)oven.Current / Mathf.Max(1, oven.Max);

        private void Update()
        {
            float damage = Damage;
            timer -= Time.deltaTime;
            if (softDot != null && timer <= 0f)
            {
                timer = 1f / Mathf.Lerp(rate.x, rate.y, damage);
                Spawn(damage);
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Puff puff = active[i];
                puff.Age += Time.deltaTime;
                float t = puff.Age / puff.Life;
                if (t >= 1f)
                {
                    puff.Renderer.gameObject.SetActive(false);
                    pool.Push(puff.Renderer);
                    active.RemoveAt(i);
                    continue;
                }
                Transform shape = puff.Renderer.transform;
                // Sube cada vez más despacio, el aire lo lleva de lado y se mece.
                float rise = riseSpeed * puff.Age * (1f - 0.35f * t);
                float side = drift * puff.Age + Mathf.Sin(puff.Age * 2f + puff.Sway) * 0.15f;
                shape.localPosition = new Vector3(puff.Sway * 0.05f + side, rise, 0f);
                shape.localScale = Vector3.one * Mathf.Lerp(size.x, size.y, Mathf.Sqrt(t)) * puff.Size;
                Color color = puff.Color;
                // Aparece rápido y se desvanece despacio.
                color.a *= Mathf.Clamp01(t * 6f) * (1f - t);
                puff.Renderer.color = color;
            }
        }

        private void Spawn(float damage)
        {
            SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : CreateRenderer();
            renderer.gameObject.SetActive(true);
            active.Add(new Puff
            {
                Renderer = renderer,
                Life = lifetime * Random.Range(0.8f, 1.2f),
                Sway = Random.Range(-mouthWidth, mouthWidth) * 10f,
                Color = Color.Lerp(cleanSmoke, darkSmoke, damage * Random.Range(0.8f, 1f)),
                Size = Mathf.Lerp(1f, 1.5f, damage) * Random.Range(0.85f, 1.15f),
            });
        }

        private SpriteRenderer CreateRenderer()
        {
            var renderer = new GameObject("Bocanada").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sprite = softDot;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
