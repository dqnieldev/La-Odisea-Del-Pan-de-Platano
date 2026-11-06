using UnityEngine;

namespace Odisea.Level
{
    /// <summary>Un ítem que cae del cielo: baja meciéndose hasta el piso y desaparece si nadie lo toma.</summary>
    public class FallingItem : MonoBehaviour
    {
        // Lo que queda flotando el ítem sobre el piso (como los que ya están puestos en el nivel).
        private const float RestHeight = 0.7f;
        private const float BlinkTime = 3f;

        private LayerMask ground;
        private float speed;
        private float lifetime;
        private float startX;
        private bool landed;
        private float timer;
        private Renderer[] renderers;

        public void Setup(LayerMask groundLayers, float fallSpeed, float life)
        {
            ground = groundLayers;
            speed = fallSpeed;
            lifetime = life;
        }

        private void Start()
        {
            startX = transform.position.x;
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            if (!landed)
            {
                float step = speed * Time.deltaTime;
                Vector2 position = transform.position;
                RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, RestHeight + step, ground);
                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    transform.position = new Vector3(position.x, hit.point.y + RestHeight, transform.position.z);
                    landed = true;
                    return;
                }
                // Se mece de lado a lado mientras cae.
                float sway = Mathf.Sin(Time.time * 2.5f) * 0.4f;
                transform.position = new Vector3(startX + sway, position.y - step, transform.position.z);
                return;
            }
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            if (lifetime - timer > BlinkTime)
                return;
            bool visible = Mathf.Repeat(timer * 8f, 1f) > 0.4f;
            foreach (Renderer part in renderers)
            {
                if (part != null)
                    part.enabled = visible;
            }
        }
    }
}
