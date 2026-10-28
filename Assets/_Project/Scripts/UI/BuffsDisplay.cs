using Odisea.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Debajo de los corazones: un ícono por cada ventaja activa de Ulises (escudo, velocidad) con un anillo que se
    /// vacía según el tiempo que queda. Sin ventajas no se ve nada.
    /// </summary>
    public class BuffsDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerBuffs buffs;
        [SerializeField] private GameObject shieldSlot;
        [Tooltip("Anillo (Image tipo Filled, radial) que muestra el tiempo que le queda al escudo.")]
        [SerializeField] private Image shieldTime;
        [SerializeField] private GameObject speedSlot;
        [SerializeField] private Image speedTime;

        private void Update()
        {
            if (buffs == null)
                return;
            Show(shieldSlot, shieldTime, buffs.HasShield, buffs.ShieldLeft);
            Show(speedSlot, speedTime, buffs.HasSpeed, buffs.SpeedLeft);
        }

        private static void Show(GameObject slot, Image time, bool active, float left)
        {
            if (slot.activeSelf != active)
                slot.SetActive(active);
            if (active)
                time.fillAmount = left;
        }
    }
}
