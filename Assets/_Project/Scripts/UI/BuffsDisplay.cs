using Odisea.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Debajo de los corazones: un ícono por cada ventaja activa de Ulises (escudo, velocidad) con un anillo que se
    /// vacía según el tiempo que queda. Sin ventajas no se ve nada. Cada casilla entra con un rebote y, cuando ya
    /// casi se acaba, late para avisar.
    /// </summary>
    public class BuffsDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerBuffs buffs;
        [SerializeField] private GameObject shieldSlot;
        [Tooltip("Anillo (Image tipo Filled, radial) que muestra el tiempo que le queda al escudo.")]
        [SerializeField] private Image shieldTime;
        [SerializeField] private GameObject speedSlot;
        [SerializeField] private Image speedTime;
        [SerializeField, Min(0.01f)] private float popDuration = 0.25f;
        [Tooltip("Parte del tiempo (0 a 1) que queda cuando la casilla empieza a latir.")]
        [SerializeField, Range(0f, 1f)] private float warnBelow = 0.25f;

        private float shieldShownAt, speedShownAt;

        private void Update()
        {
            if (buffs == null)
                return;
            Show(shieldSlot, shieldTime, buffs.HasShield, buffs.ShieldLeft, ref shieldShownAt);
            Show(speedSlot, speedTime, buffs.HasSpeed, buffs.SpeedLeft, ref speedShownAt);
        }

        private void Show(GameObject slot, Image time, bool active, float left, ref float shownAt)
        {
            if (slot.activeSelf != active)
            {
                slot.SetActive(active);
                shownAt = Time.unscaledTime;
            }
            if (!active)
                return;
            time.fillAmount = left;

            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / popDuration);
            float scale = p + Mathf.Sin(p * Mathf.PI) * 0.35f;
            if (left < warnBelow)
                scale *= 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f)) * 0.12f;
            slot.transform.localScale = Vector3.one * scale;
        }
    }
}
