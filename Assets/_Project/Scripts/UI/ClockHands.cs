using Odisea.Level;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Las manecillas del reloj del HUD: avanzan con el cronómetro del nivel desde como están en el dibujo. La larga da
    /// un brinquito cada segundo (una vuelta por minuto) y la corta gira despacio (una vuelta cada 12 minutos).
    /// </summary>
    public class ClockHands : MonoBehaviour
    {
        [SerializeField] private LevelScore score;
        [SerializeField] private RectTransform minuteHand;
        [SerializeField] private RectTransform hourHand;
        [Tooltip("Segundos del nivel que tarda la manecilla larga en dar la vuelta.")]
        [SerializeField, Min(1f)] private float minuteTurn = 60f;
        [Tooltip("Segundos del nivel que tarda la manecilla corta en dar la vuelta.")]
        [SerializeField, Min(1f)] private float hourTurn = 720f;
        [Tooltip("Lo que tarda el brinquito de la manecilla larga en cada segundo.")]
        [SerializeField, Min(0.01f)] private float tickDuration = 0.15f;

        private void Update()
        {
            if (score == null)
                return;
            float seconds = score.Seconds;
            float whole = Mathf.Floor(seconds);
            // Del segundo anterior a este, con un pequeño rebote al llegar.
            float ticks = Mathf.Max(0f, whole - 1f + EaseOutBack(Mathf.Clamp01((seconds - whole) / tickDuration)));
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, -360f * ticks / minuteTurn);
            hourHand.localRotation = Quaternion.Euler(0f, 0f, -360f * seconds / hourTurn);
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}
