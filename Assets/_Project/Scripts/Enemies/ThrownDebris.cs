using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Los pedazos de la caja de Circe al romperse: el dibujo de la caja rota aparece donde chocó, se abre un poco,
    /// sigue hacia donde iba, cae y se desvanece.
    /// </summary>
    public class ThrownDebris : MonoBehaviour
    {
        private const float Lifetime = 0.45f;

        private SpriteRenderer drawing;
        private Vector2 velocity;
        private float age;

        public static void Spawn(Sprite sprite, Vector3 position, int sortingOrder, float direction)
        {
            var debris = new GameObject("Caja rota").AddComponent<ThrownDebris>();
            debris.transform.position = position;
            debris.drawing = debris.gameObject.AddComponent<SpriteRenderer>();
            debris.drawing.sprite = sprite;
            debris.drawing.sortingOrder = sortingOrder;
            debris.velocity = new Vector2(direction * 1.5f, 2f);
            debris.transform.localScale = Vector3.one * 0.8f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / Lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            velocity.y -= 14f * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.15f, Mathf.Sqrt(t));
            Color color = drawing.color;
            color.a = 1f - t * t;
            drawing.color = color;
        }
    }
}
