using UnityEngine;

namespace Odisea.Core
{
    /// <summary>Cámara que sigue a un objetivo con suavizado. Opcionalmente se limita a un área.</summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new(0f, 1.5f);
        [Tooltip("Tiempo aproximado para alcanzar al objetivo. Menor = más pegada.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;

        [Header("Límites (opcional)")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 minPosition = new(-100f, -100f);
        [SerializeField] private Vector2 maxPosition = new(100f, 100f);

        private Vector3 velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desired = target.position + (Vector3)offset;
            desired.z = transform.position.z;

            if (useBounds)
            {
                desired.x = Mathf.Clamp(desired.x, minPosition.x, maxPosition.x);
                desired.y = Mathf.Clamp(desired.y, minPosition.y, maxPosition.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
