using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Fondo de un nivel vertical hecho con una sola imagen alta: sube con la cámara más despacio que el escenario,
    /// de modo que con la cámara hasta abajo se ve la orilla de abajo de la imagen y con la cámara hasta arriba se ve
    /// la de arriba. Así una imagen con perspectiva cubre todo el nivel sin repetirse y se siente lejana.
    /// </summary>
    [DefaultExecutionOrder(100)] // Después de la cámara (CameraFollow2D se mueve en LateUpdate).
    [RequireComponent(typeof(SpriteRenderer))]
    public class TallBackdrop : MonoBehaviour
    {
        [Tooltip("Altura de la cámara al empezar el nivel (su límite de abajo).")]
        [SerializeField] private float cameraMinY;
        [Tooltip("Altura de la cámara en la cima (su límite de arriba).")]
        [SerializeField] private float cameraMaxY = 40f;
        [Tooltip("Cuánto acompaña a la cámara en horizontal (1 = pegada, 0 = quieta en el mundo).")]
        [SerializeField, Range(0f, 1f)] private float followX;

        private Camera view;
        private float halfHeight;

        private void Awake()
        {
            view = Camera.main;
            if (view == null)
            {
                enabled = false;
                return;
            }
            halfHeight = GetComponent<SpriteRenderer>().bounds.extents.y;
        }

        private void LateUpdate()
        {
            Vector3 camera = view.transform.position;
            float t = Mathf.InverseLerp(cameraMinY, cameraMaxY, camera.y);
            float viewHalf = view.orthographicSize;
            // Abajo: la orilla de abajo de la imagen con la de la vista; arriba, la de arriba con la de arriba.
            float offset = Mathf.Lerp(halfHeight - viewHalf, viewHalf - halfHeight, t);
            transform.position = new Vector3(camera.x * followX, camera.y + offset, transform.position.z);
        }
    }
}
