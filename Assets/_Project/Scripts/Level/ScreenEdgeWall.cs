using Odisea.Core;
using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Pared invisible pegada a la orilla izquierda de lo que se ve: con la cámara que no regresa
    /// (<see cref="CameraFollow2D"/> con noBacktrack), Ulises no puede salirse de la pantalla hacia atrás.
    /// Va en un objeto con su BoxCollider2D (alto) y un Rigidbody2D cinemático.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ScreenEdgeWall : MonoBehaviour
    {
        [SerializeField] private CameraFollow2D follow;
        [Tooltip("Medio ancho del collider: la pared queda justo afuera de la orilla.")]
        [SerializeField, Min(0f)] private float halfWidth = 0.5f;

        private Rigidbody2D body;
        private bool placed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            if (follow == null && Camera.main != null)
                follow = Camera.main.GetComponent<CameraFollow2D>();
        }

        private void FixedUpdate()
        {
            if (follow == null)
                return;
            var target = new Vector2(follow.LeftEdge - halfWidth, follow.transform.position.y);
            // La primera vez (y si la cámara saltó lejos) aparece directo en su lugar: si se deslizara hasta ahí,
            // arrastraría a Ulises fuera de la pantalla. Después sí se mueve, para empujarlo si se queda atrás.
            if (!placed || Mathf.Abs(body.position.x - target.x) > 1f)
            {
                body.position = target;
                transform.position = target;
                placed = true;
                return;
            }
            body.MovePosition(target);
        }
    }
}
