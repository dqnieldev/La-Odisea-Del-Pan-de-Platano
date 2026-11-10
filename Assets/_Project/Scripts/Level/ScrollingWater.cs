using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Agua que corre: desliza una tira repetida (SpriteRenderer en modo Tiled) hacia un lado sin parar y la mece un
    /// poco hacia arriba y abajo. Al avanzar una repetición completa vuelve al inicio, así nunca se ve el corte. Con dos
    /// capas corriendo en sentidos contrarios (una semitransparente encima) el agua parece moverse de verdad.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ScrollingWater : MonoBehaviour
    {
        [Tooltip("Unidades por segundo (negativo = hacia la izquierda).")]
        [SerializeField] private float speed = 0.3f;
        [Tooltip("Cuánto sube y baja (unidades).")]
        [SerializeField, Min(0f)] private float bob = 0.04f;
        [SerializeField, Min(0.1f)] private float bobPeriod = 3f;
        [Tooltip("Desfase del vaivén, para que dos capas no suban juntas.")]
        [SerializeField] private float bobPhase;

        private Vector3 start;
        private float tile;

        private void Awake()
        {
            start = transform.localPosition;
            var drawing = GetComponent<SpriteRenderer>();
            // Lo que mide una repetición en el mundo.
            tile = drawing.sprite != null ? drawing.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x) : 0f;
        }

        private void Update()
        {
            if (tile <= 0f)
                return;
            float t = Time.time;
            float x = Mathf.Repeat(speed * t, tile);
            float y = Mathf.Sin(t * 2f * Mathf.PI / bobPeriod + bobPhase) * bob;
            transform.localPosition = start + new Vector3(x, y, 0f);
        }
    }
}
