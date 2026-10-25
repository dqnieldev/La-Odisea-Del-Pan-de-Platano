using Odisea.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Menú principal: Jugar, Tutorial, Opciones y Salir (Salir pregunta antes de cerrar el juego).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Contenedor de los botones; se oculta mientras Opciones está abierto.")]
        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private OptionsPanel optionsPanel;
        [Tooltip("Opcional: nombre, versión y ayuda de botones abajo; se oculta con Opciones (el libro tiene su propia ayuda).")]
        [SerializeField] private GameObject footer;

        [Header("Confirmar salida")]
        [Tooltip("\"¿Salir del juego?\" con No y Sí; ocupa el lugar de los botones.")]
        [SerializeField] private GameObject quitConfirm;
        [SerializeField] private Button quitNoButton;
        [SerializeField] private Button quitYesButton;

        private GameObject lastSelected;

        private void Start()
        {
            playButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.Play));
            tutorialButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.Tutorial));
            optionsButton.onClick.AddListener(OpenOptions);
            quitButton.onClick.AddListener(AskToQuit);
            quitNoButton.onClick.AddListener(CancelQuit);
            quitYesButton.onClick.AddListener(SceneFlow.Quit);

            optionsPanel.gameObject.SetActive(false);
            quitConfirm.SetActive(false);
            Select(playButton.gameObject);
        }

        private void Update()
        {
            // Esc o B / Círculo en la pregunta de salir equivale a "No".
            if (quitConfirm.activeSelf && CancelPressed())
            {
                UiAudio.Play(UiSound.Back);
                CancelQuit();
                return;
            }

            // Si un clic en el vacío quita la selección, el teclado y el mando no deben quedarse sin botón activo.
            EventSystem events = EventSystem.current;
            if (events == null)
                return;
            if (events.currentSelectedGameObject == null && lastSelected != null && lastSelected.activeInHierarchy)
            {
                UiAudio.MuteHoverBriefly();
                events.SetSelectedGameObject(lastSelected);
            }
            else if (events.currentSelectedGameObject != null)
                lastSelected = events.currentSelectedGameObject;
        }

        private static bool CancelPressed() =>
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        private void OpenOptions()
        {
            SetMenuVisible(false);
            optionsPanel.Open(onClosed: () =>
            {
                SetMenuVisible(true);
                Select(optionsButton.gameObject);
            });
        }

        private void AskToQuit()
        {
            buttonsPanel.SetActive(false);
            quitConfirm.SetActive(true);
            // "No" de entrada, para que un doble clic o un botón de más no cierre el juego.
            Select(quitNoButton.gameObject);
        }

        private void CancelQuit()
        {
            quitConfirm.SetActive(false);
            buttonsPanel.SetActive(true);
            Select(quitButton.gameObject);
        }

        private void SetMenuVisible(bool visible)
        {
            buttonsPanel.SetActive(visible);
            if (footer != null)
                footer.SetActive(visible);
        }

        private void Select(GameObject target)
        {
            lastSelected = target;
            // Seleccionar por código (al abrir el menú o al volver de Opciones) no debe sonar a hover.
            UiAudio.MuteHoverBriefly();
            if (EventSystem.current != null)
            {
                // Se limpia primero para que OnSelect se dispare aunque ya estuviera seleccionado.
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(target);
            }
        }
    }
}
