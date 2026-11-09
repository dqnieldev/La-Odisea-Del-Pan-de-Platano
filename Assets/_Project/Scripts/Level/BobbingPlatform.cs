using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Plataforma que flota en el agua (las pangas): sube y baja suave y se mece un poco. Se mueve con un Rigidbody2D
    /// cinemático para que Ulises viaje encima sin resbalar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BobbingPlatform : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float height = 0.15f;
        [SerializeField, Min(0.1f)] private float period = 2.4f;
        [Tooltip("Grados que se mece de lado a lado (0 = no se inclina).")]
        [SerializeField, Min(0f)] private float tilt = 1.5f;

        private Rigidbody2D body;
        private Vector2 start;
        private float phase;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            start = body.position;
            // Cada una a su ritmo, para que no suban todas juntas.
            phase = (start.x * 0.37f) % (Mathf.PI * 2f);
        }

        private void FixedUpdate()
        {
            float t = Time.time * 2f * Mathf.PI / period + phase;
            body.MovePosition(start + Vector2.up * (Mathf.Sin(t) * height));
            body.MoveRotation(Mathf.Sin(t * 0.5f) * tilt);
        }
    }
}
