using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Sección Controles del recetario: muestra el dibujo del mando que se está usando (Xbox o PlayStation)
    /// con líneas de cada botón a su acción, o la tabla de teclas si se juega con teclado y mouse.
    /// Cambia sola si la persona agarra otro dispositivo con la página abierta.
    /// </summary>
    public class ControlsDiagram : MonoBehaviour
    {
        [SerializeField] private GameObject keyboard;
        [SerializeField] private GameObject xbox;
        [SerializeField] private GameObject playStation;
        [Tooltip("Opcional: lo que solo tiene sentido con mando (la casilla de Vibración).")]
        [SerializeField] private GameObject gamepadOnly;

        private void OnEnable()
        {
            Show(InputDeviceTracker.Current);
            InputDeviceTracker.Changed += Show;
        }

        private void OnDisable() => InputDeviceTracker.Changed -= Show;

        private void Show(InputScheme scheme)
        {
            if (keyboard != null)
                keyboard.SetActive(scheme == InputScheme.Keyboard);
            if (xbox != null)
                xbox.SetActive(scheme == InputScheme.Xbox);
            if (playStation != null)
                playStation.SetActive(scheme == InputScheme.PlayStation);
            if (gamepadOnly != null)
                gamepadOnly.SetActive(scheme != InputScheme.Keyboard);
        }
    }
}
