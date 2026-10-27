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
            {
                // Reiniciar a mano es desde el principio; morir, desde el último punto de control.
                Checkpoints.Clear();
                Restart();
            }
        }

        private void OnPlayerDied() => Invoke(nameof(RestartOnPlayer), restartDelayOnDeath);

        // Al morir, el iris se cierra sobre Ulises (el centro de su dibujo: tirado en el suelo, ahí queda),
        // esté donde esté en la pantalla.
        private void RestartOnPlayer()
        {
            Camera view = Camera.main;
            if (view == null || playerHealth == null)
            {
                Restart();
                return;
            }
            var drawing = playerHealth.GetComponentInChildren<SpriteRenderer>();
            Vector3 target = drawing != null ? drawing.bounds.center : playerHealth.transform.position;
            Vector3 point = view.WorldToViewportPoint(target);
            Restart(new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y)));
        }

        private void Restart() => Restart(null);

        private void Restart(Vector2? irisCenter)
        {
            if (restarting)
                return;
            restarting = true;
            SceneFlow.ReloadCurrent(irisCenter);
        }
    }
}
