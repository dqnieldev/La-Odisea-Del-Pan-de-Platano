using Odisea.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>Menú principal: Jugar, Tutorial, Opciones y Salir.</summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Contenedor de los botones; se oculta mientras Opciones está abierto.")]
        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private OptionsPanel optionsPanel;

        private GameObject lastSelected;

        private void Start()
        {
            playButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.Play));
            tutorialButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.Tutorial));
            optionsButton.onClick.AddListener(OpenOptions);
            quitButton.onClick.AddListener(SceneFlow.Quit);

            optionsPanel.gameObject.SetActive(false);
            Select(playButton.gameObject);
        }

        private void Update()
        {
            // Si un clic en el vacío quita la selección, el teclado y el mando no deben quedarse sin botón activo.
            EventSystem events = EventSystem.current;
            if (events == null)
                return;
            if (events.currentSelectedGameObject == null && lastSelected != null && lastSelected.activeInHierarchy)
                events.SetSelectedGameObject(lastSelected);
            else if (events.currentSelectedGameObject != null)
                lastSelected = events.currentSelectedGameObject;
        }

        private void OpenOptions()
        {
            buttonsPanel.SetActive(false);
            optionsPanel.Open(onClosed: () =>
            {
                buttonsPanel.SetActive(true);
                Select(optionsButton.gameObject);
            });
        }

        private void Select(GameObject target)
        {
            lastSelected = target;
            if (EventSystem.current != null)
            {
                // Se limpia primero para que OnSelect se dispare aunque ya estuviera seleccionado.
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(target);
            }
        }
    }
}
