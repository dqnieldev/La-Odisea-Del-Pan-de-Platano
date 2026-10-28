using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Capa del fondo con paralaje: sigue a la cámara solo en parte, así lo lejano se mueve lento y lo cercano rápido.
    /// follow = 1: pegada a la cámara (cielo, el sol que acompaña a Ulises); 0: quieta en el mundo (como el suelo);
    /// entre 0 y 1: más lejos mientras más alto; negativo: primer plano, pasa más rápido que el escenario.
    /// </summary>
    [DefaultExecutionOrder(100)] // Después de la cámara (CameraFollow2D se mueve en LateUpdate).
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Cuánto acompaña a la cámara en horizontal (1 = pegada, 0 = quieta en el mundo).")]
        [SerializeField, Range(-1f, 1f)] private float followX = 0.5f;
        [Tooltip("Cuánto acompaña a la cámara en vertical (al subir escaleras o saltar).")]
        [SerializeField, Range(-1f, 1f)] private float followY = 0.5f;

        private Transform view;
        private Vector3 startPosition;
        private Vector3 viewStart;

        // En Awake, antes de cualquier Start: la cámara aún está donde la dejó la escena. Si luego salta a Ulises
        // (al reaparecer en un punto de control), ese salto cuenta como movimiento y la capa lo acompaña.
        private void Awake()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                enabled = false;
                return;
            }
            view = main.transform;
            startPosition = transform.position;
            viewStart = view.position;
        }

        private void LateUpdate()
        {
            Vector3 moved = view.position - viewStart;
            transform.position = startPosition + new Vector3(moved.x * followX, moved.y * followY, 0f);
        }
    }
}
