using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Lo que suelta la garza (<see cref="HeronBomber"/>): un pescado podrido que cae con su propia gravedad, gira un
    /// poco y hace daño si le pega a Ulises (Hazard con destroyOnHit). Debajo, en el piso o en el agua, una sombra crece
    /// mientras cae para avisar dónde va a caer. Se deshace al llegar al piso o al agua.
    /// </summary>
    public class HeronDrop : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Altura del agua: si no hay piso debajo, cae hasta aquí.")]
        [SerializeField] private float waterHeight = -0.45f;
        [SerializeField, Min(0f)] private float gravity = 14f;
        [SerializeField] private float spin = 220f;
        [Tooltip("La sombra de aviso (hijo, se mueve sola al piso).")]
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private Vector2 shadowSize = new(1f, 0.25f);
        [SerializeField] private Transform visual;

        private Vector2 velocity;
        private float startHeight;

        public void Launch(Vector2 initialVelocity)
        {
            velocity = initialVelocity;
            startHeight = Mathf.Max(1f, transform.position.y - Floor());
            if (shadow != null)
                shadow.transform.SetParent(null, true);
        }

        private float Floor()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 30f, groundLayers);
            return hit.collider != null ? Mathf.Max(hit.point.y, waterHeight) : waterHeight;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            velocity += Vector2.down * gravity * dt;
            transform.position += (Vector3)(velocity * dt);
            if (visual != null)
                visual.Rotate(0f, 0f, spin * dt);

            float floor = Floor();
            if (shadow != null)
            {
                float closeness = 1f - Mathf.Clamp01((transform.position.y - floor) / startHeight);
                shadow.transform.position = new Vector3(transform.position.x, floor + 0.05f, 0f);
                shadow.transform.localScale = new Vector3(shadowSize.x * (0.4f + 0.6f * closeness), shadowSize.y * (0.4f + 0.6f * closeness), 1f);
                Color color = shadow.color;
                color.a = 0.25f + 0.4f * closeness;
                shadow.color = color;
            }
            if (transform.position.y <= floor + 0.1f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (shadow != null)
                Destroy(shadow.gameObject);
        }
    }
}
