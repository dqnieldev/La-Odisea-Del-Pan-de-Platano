using Odisea.Level;
using Odisea.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// El Tablero de Honor en grande, encima de todo (se abre desde el tótem del festival): mientras está abierto el juego
    /// se detiene y Ulises no se mueve; se cierra con el botón "Cerrar", Esc, Enter, Espacio, E o B/Círculo del control.
    /// Los puntajes los pone <see cref="FestivalScoreboard"/> al abrirse.
    /// </summary>
    public class ScoreboardModal : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private FestivalScoreboard scoreboard;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.25f;
        [Tooltip("Tiempo antes de aceptar cerrar (para que la tecla que lo abrió no lo cierre).")]
        [SerializeField, Min(0f)] private float inputDelay = 0.25f;

        private PlayerInputReader reader;
        private float shownTime = -1f;

        public static bool IsOpen { get; private set; }

        private void Awake() => gameObject.SetActive(false);

        public void Show(PlayerInputReader player)
        {
            if (IsOpen)
                return;
            reader = player;
            if (reader != null)
                reader.InputLocked = true;
            if (scoreboard != null)
                scoreboard.Refresh();
            gameObject.SetActive(true);
            group.alpha = 0f;
            shownTime = Time.unscaledTime;
            Time.timeScale = 0f;
            IsOpen = true;
            UiAudio.Play(UiSound.Page);
        }

        /// <summary>Lo llama el botón "Cerrar".</summary>
        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            Time.timeScale = 1f;
            if (reader != null)
            {
                reader.ConsumeInteractPressed();
                reader.InputLocked = false;
            }
            UiAudio.Play(UiSound.Back);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            float elapsed = Time.unscaledTime - shownTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            group.alpha = t;
            // Crece un poco de más y se asienta.
            float scale = 0.85f + 0.15f * t + Mathf.Sin(t * Mathf.PI) * 0.05f;
            card.localScale = new Vector3(scale, scale, 1f);
            if (elapsed >= inputDelay && ClosePressed())
                Close();
        }

        private bool ClosePressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool interact = reader != null && reader.ConsumeInteractPressed();
            return interact ||
                   (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame ||
                                         keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                   (gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame ||
                                        gamepad.startButton.wasPressedThisFrame));
        }

        private void OnDestroy()
        {
            // Si se sale de la escena con el tablero abierto, el tiempo no se queda detenido.
            if (IsOpen)
            {
                IsOpen = false;
                Time.timeScale = 1f;
            }
        }
    }
}
