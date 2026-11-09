using Odisea.Core;
using Odisea.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// Menú de pausa dentro de un nivel: Esc o Start abren el recetario (más chico, con el juego detrás),
    /// detienen el tiempo y desactivan los controles de Ulises hasta que se cierre.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private OptionsPanel book;
        [Tooltip("Texto de la página de portada donde va el nombre del nivel.")]
        [SerializeField] private TMP_Text levelTitle;
        [Tooltip("Nombre que se muestra en la portada. Vacío = el nombre de la escena.")]
        [SerializeField] private string levelName;
        [Tooltip("Muestra \"Reiniciar nivel\" (en el mapa no tiene sentido).")]
        [SerializeField] private bool allowRestart = true;

        private PlayerInputReader[] disabledInputs = System.Array.Empty<PlayerInputReader>();

        public bool IsPaused { get; private set; }

        private void Start()
        {
            if (levelTitle != null)
                levelTitle.text = string.IsNullOrWhiteSpace(levelName)
                    ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                    : levelName;
            if (!allowRestart)
                book.HideRestart();
            if (!CanGoToMap())
                book.HideMap();
            book.gameObject.SetActive(false);
        }

        // "Volver al mapa" solo si ya se llegó a él (tutorial terminado), no estando en él y no en el tutorial que se
        // juega desde el menú principal (ese regresa al menú).
        private static bool CanGoToMap() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneFlow.Map &&
            !SceneFlow.TutorialFromMenu && GameProgress.IsDone(GameProgress.Tutorial);

        private void Update()
        {
            // Con el tiempo ya detenido por otra pantalla (nivel completado) o con Ulises muerto no se abre encima.
            if (IsPaused || SceneFlow.IsLoading || Time.timeScale == 0f || GameOverScreen.IsActive)
                return;

            bool pausePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                                (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (pausePressed)
                Pause();
        }

        private void OnDestroy()
        {
            // Si la escena se cambia desde la pausa, el tiempo no debe quedarse detenido.
            if (IsPaused)
            {
                Time.timeScale = 1f;
                GamePause.Set(false);
            }
        }

        public void Pause()
        {
            if (IsPaused)
                return;
            IsPaused = true;
            Time.timeScale = 0f;

            // Sin esto, un salto o golpe presionado en el menú se ejecutaría al continuar.
            disabledInputs = FindObjectsByType<PlayerInputReader>(FindObjectsInactive.Exclude);
            foreach (PlayerInputReader input in disabledInputs)
                input.enabled = false;

            UiAudio.Play(UiSound.Click);
            GamePause.Set(true);
            book.Open(onClosed: Resume);
        }

        private void Resume()
        {
            foreach (PlayerInputReader input in disabledInputs)
            {
                if (input != null)
                    input.enabled = true;
            }
            disabledInputs = System.Array.Empty<PlayerInputReader>();
            Time.timeScale = 1f;
            IsPaused = false;
            GamePause.Set(false);
        }
    }
}
