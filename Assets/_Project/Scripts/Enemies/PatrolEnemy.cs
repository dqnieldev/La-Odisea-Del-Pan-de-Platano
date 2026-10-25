using Odisea.Core;
using UnityEngine;

namespace Odisea.Enemies
{
    /// <summary>
    /// Enemigo que camina de un lado a otro sobre el suelo o una plataforma (el cocodrilo).
    /// Da la vuelta al llegar a una pared o a la orilla, así nunca se cae. El dibujo debe mirar a la derecha.
    /// El daño por contacto lo pone un <see cref="Hazard"/> en el mismo objeto.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PatrolEnemy : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 1.5f;
        [Tooltip("Capas de suelo y paredes (para no caerse de la orilla ni atravesar paredes).")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Qué tan adelante de su centro revisa si hay suelo o pared.")]
        [SerializeField, Min(0f)] private float lookAhead = 0.8f;
        [Tooltip("1 = empieza caminando a la derecha, -1 = a la izquierda.")]
        [SerializeField] private int startDirection = -1;
        [Tooltip("Opcional: al morir deja de caminar.")]
        [SerializeField] private Health health;

        private Rigidbody2D body;
        private int direction;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            direction = startDirection >= 0 ? 1 : -1;
            ApplyFacing();
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead)
                return;

            Vector2 position = body.position;
            // Pared adelante (a media altura) u orilla (no hay suelo un poco adelante y abajo): da la vuelta.
            bool wallAhead = Physics2D.Raycast(position + Vector2.up * 0.4f, Vector2.right * direction, lookAhead, groundLayers);
            bool groundAhead = Physics2D.Raycast(position + new Vector2(direction * lookAhead, 0.1f), Vector2.down, 0.4f, groundLayers);
            if (wallAhead || !groundAhead)
            {
                direction = -direction;
                ApplyFacing();
            }
            body.MovePosition(position + Vector2.right * (direction * speed * Time.fixedDeltaTime));
        }

        private void ApplyFacing()
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            int dir = Application.isPlaying ? direction : (startDirection >= 0 ? 1 : -1);
            Vector3 position = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(position + Vector3.up * 0.4f, position + new Vector3(dir * lookAhead, 0.4f));
            Gizmos.DrawLine(position + new Vector3(dir * lookAhead, 0.1f), position + new Vector3(dir * lookAhead, -0.3f));
        }
    }
}
