using UnityEngine;

namespace Odisea.Level
{
    /// <summary>
    /// Un letrero que flota (el Tablero de Honor del festival): sube y baja despacito y se mece un poquito, como colgado.
    /// </summary>
    public class FloatingSign : MonoBehaviour
    {
        [Tooltip("Cuánto sube y baja (unidades).")]
        [SerializeField, Min(0f)] private float amplitude = 0.06f;
        [SerializeField, Min(0.01f)] private float period = 3.2f;
        [Tooltip("Cuánto se mece (grados).")]
        [SerializeField, Min(0f)] private float tilt = 0.6f;

        private Vector3 home;

        private void Awake() => home = transform.localPosition;

        private void Update()
        {
            float phase = Time.time * Mathf.PI * 2f / period;
            transform.localPosition = home + Vector3.up * (Mathf.Sin(phase) * amplitude);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * tilt);
        }
    }
}
