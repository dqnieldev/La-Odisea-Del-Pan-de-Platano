using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Hace que un grupo de UI (texto y su fondo) aparezca y desaparezca suavemente,
    /// como el "Presiona cualquier botón para comenzar" de la pantalla de inicio.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BlinkingGroup : MonoBehaviour
    {
        [Tooltip("Segundos de un ciclo completo (visible → tenue → visible).")]
        [SerializeField, Min(0.1f)] private float period = 1.4f;
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.2f;

        private CanvasGroup group;

        private void Awake() => group = GetComponent<CanvasGroup>();

        private void Update()
        {
            float wave = (Mathf.Cos(Time.unscaledTime / period * Mathf.PI * 2f) + 1f) * 0.5f;
            group.alpha = Mathf.Lerp(minAlpha, 1f, wave);
        }
    }
}
