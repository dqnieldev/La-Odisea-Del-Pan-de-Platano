using UnityEngine;

namespace Odisea.Core
{
    /// <summary>
    /// Partícula de un momento hecha con un sprite: se mueve, gira, crece y se desvanece, y luego se destruye.
    /// La usan las siluetas y el polvo del café y las hojitas del escudo cuando se rompe.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FadingSprite : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector2 velocity;
        private float gravity, spin, growth, wobble, lifetime, age, startAlpha;
        private Vector3 startScale;

        /// <summary>Crea la partícula con el sprite y el color dados (el alfa del color es el inicial).</summary>
        public static FadingSprite Spawn(Sprite sprite, Vector3 position, Color color, int sortingOrder, float lifetime)
        {
            var go = new GameObject("Partícula");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            var particle = go.AddComponent<FadingSprite>();
            particle.spriteRenderer = renderer;
            particle.lifetime = Mathf.Max(0.01f, lifetime);
            particle.startAlpha = color.a;
            return particle;
        }

        /// <summary>Velocidad inicial, gravedad (unidades/s²) y giro (grados/s).</summary>
        public FadingSprite Move(Vector2 initialVelocity, float gravityAcceleration = 0f, float spinSpeed = 0f)
        {
            velocity = initialVelocity;
            gravity = gravityAcceleration;
            spin = spinSpeed;
            return this;
        }

        /// <summary>Cuánto crece en toda su vida (0.5 = termina 50 % más grande) y vaivén lateral (unidades).</summary>
        public FadingSprite Grow(float amount, float sideWobble = 0f)
        {
            growth = amount;
            wobble = sideWobble;
            return this;
        }

        private void Start() => startScale = transform.localScale;

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            velocity.y -= gravity * Time.deltaTime;
            Vector3 step = velocity * Time.deltaTime;
            if (wobble > 0f)
                step.x += Mathf.Cos(age * 9f) * wobble * 9f * Time.deltaTime;
            transform.position += step;
            transform.Rotate(0f, 0f, spin * Time.deltaTime);
            transform.localScale = startScale * (1f + growth * t);
            Color color = spriteRenderer.color;
            color.a = startAlpha * (1f - t * t);
            spriteRenderer.color = color;
        }
    }
}
