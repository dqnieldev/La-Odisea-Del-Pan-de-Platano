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

        [Header("Orilla del dibujo (opcional)")]
        [Tooltip("Lo que se ve nunca sale de este rectángulo (el fondo): al sacudirse, la cámara se acerca lo justo " +
                 "para no enseñar la orilla del dibujo.")]
        [SerializeField] private bool limitView;
        [SerializeField] private Vector2 viewMin;
        [SerializeField] private Vector2 viewMax;

        private Vector3 velocity;
        private Camera view;
        private float baseSize;
        // Sacudida (pisadas de Polifemo): se suma a la posición y se apaga sola.
        private static float shakeAmount, shakeTimer, shakeDuration;
        private Vector3 shakeOffset;

        /// <summary>Sacude la cámara un momento (amount en unidades, se apaga en duration segundos).</summary>
        public static void Shake(float amount, float duration)
        {
            if (amount < shakeAmount * (shakeTimer / Mathf.Max(0.01f, shakeDuration)))
                return;
            shakeAmount = amount;
            shakeDuration = shakeTimer = Mathf.Max(0.01f, duration);
        }

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        // Empieza ya encima del objetivo: si Ulises reaparece en un punto de control, no recorre todo el nivel.
        private void Start()
        {
            if (target != null)
                transform.position = Desired();
            view = GetComponent<Camera>();
            if (view != null)
                baseSize = view.orthographicSize;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            Vector3 position = transform.position - shakeOffset;
            position = Vector3.SmoothDamp(position, Desired(), ref velocity, smoothTime);
            shakeOffset = Vector3.zero;
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float strength = shakeAmount * Mathf.Clamp01(shakeTimer / shakeDuration);
                shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * strength;
            }
            if (limitView && view != null && view.orthographicSize > 0f)
                KeepInsideArt(position, shakeTimer > 0f ? shakeAmount * Mathf.Clamp01(shakeTimer / shakeDuration) : 0f);
            transform.position = position + shakeOffset;
        }

        // Con la cámara justo del tamaño del fondo, cualquier sacudida enseña la orilla: se acerca lo que haga falta
        // para la sacudida de ahora (se aleja sola al apagarse) y, por si acaso, la sacudida no pasa de la orilla.
        private void KeepInsideArt(Vector3 position, float strength)
        {
            float aspect = view.aspect;
            float marginX = Mathf.Min(position.x - baseSize * aspect - viewMin.x, viewMax.x - (position.x + baseSize * aspect));
            float marginY = Mathf.Min(position.y - baseSize - viewMin.y, viewMax.y - (position.y + baseSize));
            float zoom = Mathf.Max(0f, strength - marginY, (strength - marginX) / aspect);
            view.orthographicSize = baseSize - zoom;
            float halfHeight = view.orthographicSize, halfWidth = halfHeight * aspect;
            Vector3 shaken = position + shakeOffset;
            shaken.x = Mathf.Clamp(shaken.x, viewMin.x + halfWidth, Mathf.Max(viewMin.x + halfWidth, viewMax.x - halfWidth));
            shaken.y = Mathf.Clamp(shaken.y, viewMin.y + halfHeight, Mathf.Max(viewMin.y + halfHeight, viewMax.y - halfHeight));
            shakeOffset = shaken - position;
        }

        private Vector3 Desired()
        {
            Vector3 desired = target.position + (Vector3)offset;
            desired.z = transform.position.z;

            if (useBounds)
            {
                desired.x = Mathf.Clamp(desired.x, minPosition.x, maxPosition.x);
                desired.y = Mathf.Clamp(desired.y, minPosition.y, maxPosition.y);
            }
            return desired;
        }
    }
}
