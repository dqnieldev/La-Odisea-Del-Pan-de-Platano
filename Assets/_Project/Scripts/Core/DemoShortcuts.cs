using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.Core
{
    /// <summary>
    /// Atajos para escenas de prueba: reiniciar con R y reinicio automático cuando el jugador pierde
    /// todas sus vidas. Esc y Start son del menú de pausa. En los niveles finales esto lo hará el Game Over.
    /// </summary>
    public class DemoShortcuts : MonoBehaviour
    {
        [Tooltip("Vida del jugador. Si se acaba, la escena se reinicia sola.")]
        [SerializeField] private Health playerHealth;
        [SerializeField, Min(0f)] private float restartDelayOnDeath = 1.5f;

        private bool restarting;

        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.Died -= OnPlayerDied;
        }

        private void Update()
        {
            // En pausa (tiempo detenido) la R no reinicia.
            bool paused = Time.timeScale == 0f;
            if (!paused && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
        }

        private void OnPlayerDied() => Invoke(nameof(Restart), restartDelayOnDeath);

        private void Restart()
        {
            if (restarting)
                return;
            restarting = true;
            SceneFlow.ReloadCurrent();
        }
    }
}
