using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Lo que suelta la garza (<see cref="HeronBomber"/>): un pescado podrido que cae con su propia gravedad (aventado
    /// hacia donde va Ulises), gira un poco y hace daño si le pega (Hazard con destroyOnHit). En el piso o en el agua,
    /// justo donde va a caer, una sombra crece mientras se acerca. Se deshace al llegar al piso o al agua.
    /// </summary>
    public class HeronDrop : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Altura del agua: si no hay piso debajo, cae hasta aquí.")]
        [SerializeField] private float waterHeight = -0.45f;
        [SerializeField, Min(0f)] private float gravity = 20f;
        [SerializeField] private float spin = 220f;
        [Tooltip("La sombra de aviso (hijo, se mueve sola al piso).")]
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private Vector2 shadowSize = new(1.5f, 0.38f);
        [SerializeField] private Transform visual;

        private Vector2 velocity;
        private float startHeight;

        public float Gravity => gravity;

        public void Launch(Vector2 initialVelocity)
        {
            velocity = initialVelocity;
            startHeight = Mathf.Max(1f, transform.position.y - FloorAt(transform.position.x));
            if (shadow != null)
                shadow.transform.SetParent(null, true);
        }

        private float FloorAt(float x)
        {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, transform.position.y), Vector2.down, 30f, groundLayers);
            return hit.collider != null ? Mathf.Max(hit.point.y, waterHeight) : waterHeight;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            velocity += Vector2.down * gravity * dt;
            transform.position += (Vector3)(velocity * dt);
            if (visual != null)
                visual.Rotate(0f, 0f, spin * dt);

            float floorHere = FloorAt(transform.position.x);
            if (shadow != null)
            {
                // Dónde va a caer: con su velocidad de ahora, cuánto le falta para llegar al piso.
                float height = Mathf.Max(0f, transform.position.y - floorHere);
                float down = -velocity.y;
                float time = gravity > 0f ? (-down + Mathf.Sqrt(Mathf.Max(0f, down * down + 2f * gravity * height))) / gravity : 0f;
                float landX = transform.position.x + velocity.x * time;
                float floor = FloorAt(landX);
                float closeness = 1f - Mathf.Clamp01((transform.position.y - floor) / startHeight);
                shadow.transform.position = new Vector3(landX, floor + 0.05f, 0f);
                shadow.transform.localScale = new Vector3(shadowSize.x * (0.5f + 0.5f * closeness),
                    shadowSize.y * (0.5f + 0.5f * closeness), 1f);
                Color color = shadow.color;
                color.a = 0.35f + 0.45f * closeness;
                shadow.color = color;
            }
            if (transform.position.y <= floorHere + 0.1f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (shadow != null)
                Destroy(shadow.gameObject);
        }
    }
}
