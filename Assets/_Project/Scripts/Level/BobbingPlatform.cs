using Odisea.Core;
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
        [Tooltip("Quién la hace sonar al caer encima (Ulises).")]
        [SerializeField] private LayerMask riders;
        [Tooltip("Suena cuando alguien de riders cae sobre la panga (no al caminar en ella).")]
        [SerializeField] private SoundEffect landSound = new();
        [Tooltip("Qué tan rápido tiene que caer (u/s) para que suene.")]
        [SerializeField, Min(0f)] private float landSpeed = 2f;

        private Rigidbody2D body;
        private float lastLand = -1f;
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
            // Con velocidad (no MovePosition) para que quien la pisa sepa que el piso sube: si no, Ulises parecía ir en
            // el aire mientras la panga subía y el salto desde ella gastaba el doble salto.
            float dt = Time.fixedDeltaTime;
            float t = (Time.time + dt) * 2f * Mathf.PI / period + phase;
            Vector2 target = start + Vector2.up * (Mathf.Sin(t) * height);
            body.linearVelocity = (target - body.position) / dt;
            body.angularVelocity = Mathf.DeltaAngle(body.rotation, Mathf.Sin(t * 0.5f) * tilt) / dt;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if ((riders.value & (1 << collision.gameObject.layer)) == 0 || Time.time - lastLand < 0.25f)
                return;
            // Solo si cae desde arriba con algo de fuerza.
            Rigidbody2D rider = collision.rigidbody;
            if (rider == null || rider.position.y < body.position.y || Mathf.Abs(collision.relativeVelocity.y) < landSpeed)
                return;
            lastLand = Time.time;
            landSound.Play();
        }
    }
}
