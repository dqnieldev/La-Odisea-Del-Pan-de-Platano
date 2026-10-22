using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Odisea.Core
{
    /// <summary>
    /// Atajos para escenas de prueba y demos: reiniciar (R / Start), salir (Esc / Select)
    /// y reinicio automático cuando el jugador pierde todas sus vidas.
    /// No va en los niveles finales; ahí lo hará la pantalla de Game Over.
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
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            bool restart = (keyboard != null && keyboard.rKey.wasPressedThisFrame) ||
                           (gamepad != null && gamepad.startButton.wasPressedThisFrame);
            bool quit = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                        (gamepad != null && gamepad.selectButton.wasPressedThisFrame);

            if (restart)
                Restart();
            else if (quit)
                Quit();
        }

        private void OnPlayerDied() => Invoke(nameof(Restart), restartDelayOnDeath);

        private void Restart()
        {
            if (restarting)
                return;
            restarting = true;

            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // En el Editor la escena puede no estar en Build Settings; se carga por ruta.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
