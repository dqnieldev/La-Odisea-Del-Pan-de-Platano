using System;
using System.Collections;
using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Recetario viejo con índice a la izquierda y la sección elegida a la derecha; al elegir otra sección
    /// la hoja da vuelta (<see cref="PageFlipper"/>). Se usa para Opciones en el menú principal y como menú
    /// de pausa dentro del juego (con Continuar, Reiniciar nivel, Ir al título y Salir al menú).
    /// Los botones de acción son opcionales: los que no estén asignados simplemente no existen en ese libro.
    /// Esc o B / Círculo: desde la página derecha regresa al índice; desde el índice cierra el libro.
    /// LB / RB (o Q / E): saltan entre el índice (izquierda) y los ajustes de la sección (derecha).
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private enum Section
        {
            Home,
            Audio,
            Screen,
            Controls,
        }

        [Header("Libro")]
        [SerializeField] private RectTransform book;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private PageFlipper pageFlipper;
        [Tooltip("Contenido de la página derecha; se desvanece mientras la hoja da vuelta.")]
        [SerializeField] private CanvasGroup rightPage;
        [Tooltip("Opcional: listón que marca en el índice la sección abierta.")]
        [SerializeField] private SectionRibbon ribbon;

        [Header("Índice (página izquierda)")]
        [SerializeField] private Button audioTab;
        [SerializeField] private Button screenTab;
        [SerializeField] private Button controlsTab;
        [Tooltip("Cierra el libro. En el menú principal dice Volver; en la pausa, Continuar.")]
        [SerializeField] private Button backButton;
        [Tooltip("Opcional (pausa): reinicia la escena actual.")]
        [SerializeField] private Button restartButton;
        [Tooltip("Opcional: regresa al inicio del juego (logo y luego el video del título).")]
        [SerializeField] private Button titleButton;
        [Tooltip("Opcional (pausa): sale al menú principal.")]
        [SerializeField] private Button exitToMenuButton;
        [Tooltip("Pausa: Start del mando también cierra el libro.")]
        [SerializeField] private bool closeWithStart;

        [Header("Secciones (página derecha)")]
        [Tooltip("Opcional: página que se ve al abrir el libro (pausa). Sin ella, el libro abre en Audio.")]
        [SerializeField] private GameObject homeSection;
        [SerializeField] private GameObject audioSection;
        [SerializeField] private GameObject screenSection;
        [SerializeField] private GameObject controlsSection;

        [Header("Ajustes")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Toggle fullscreenToggle;

        private const float OpenDuration = 0.28f;
        private const float CloseDuration = 0.16f;
        private const float ContentFade = 0.15f;

        private Action closed;
        private Section current;
        private bool closing;
        private Vector3 bookScale = Vector3.one;

        private void Awake()
        {
            bookScale = book.localScale;
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 100f;
            // Sin números enteros el teclado y el mando mueven el volumen de 10 en 10 (con enteros sería de 1 en 1).
            volumeSlider.wholeNumbers = false;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            fullscreenToggle.onValueChanged.AddListener(value => GameSettings.Fullscreen = value);

            audioTab.onClick.AddListener(() => ShowSection(Section.Audio));
            screenTab.onClick.AddListener(() => ShowSection(Section.Screen));
            controlsTab.onClick.AddListener(() => ShowSection(Section.Controls));
            backButton.onClick.AddListener(Close);
            if (restartButton != null)
                restartButton.onClick.AddListener(() => Leave(SceneFlow.ReloadCurrent));
            if (titleButton != null)
                titleButton.onClick.AddListener(() => Leave(() => SceneFlow.Load(SceneFlow.Splash)));
            if (exitToMenuButton != null)
                exitToMenuButton.onClick.AddListener(() => Leave(() => SceneFlow.Load(SceneFlow.MainMenu)));
        }

        public void Open(Action onClosed)
        {
            closed = onClosed;
            closing = false;
            gameObject.SetActive(true);

            volumeSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.MasterVolume * 100f));
            UpdateVolumeLabel(volumeSlider.value);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);

            SetSectionActive(homeSection != null ? Section.Home : Section.Audio, instantRibbon: true);
            rightPage.alpha = 1f;
            rightPage.interactable = true;
            Select(homeSection != null ? backButton.gameObject : audioTab.gameObject);
            StartCoroutine(AnimateOpen());
        }

        private void Update()
        {
            // Mientras abre (interactable en falso) se ignora: la misma tecla que abrió la pausa no debe cerrarla.
            if (closing || pageFlipper.IsFlipping || !panelGroup.interactable)
                return;

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool toIndex = (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame) ||
                           (keyboard != null && keyboard.qKey.wasPressedThisFrame);
            bool toSettings = (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame) ||
                              (keyboard != null && keyboard.eKey.wasPressedThisFrame);
            if (toIndex)
                GoToIndex();
            else if (toSettings)
                GoToSettings();

            bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                          (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
                          (closeWithStart && Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (!cancel)
                return;

            UiAudio.Play(UiSound.Back);
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(rightPage.transform))
                Select(TabFor(current).gameObject);
            else
                Close();
        }

        private void ShowSection(Section section)
        {
            if (closing || pageFlipper.IsFlipping)
                return;

            // Las opciones del índice no hacen su propio click: aquí se decide entre página o click.
            if (section == current)
            {
                UiAudio.Play(UiSound.Click);
                FocusSection();
                return;
            }

            UiAudio.Play(UiSound.Page);
            rightPage.interactable = false;
            StartCoroutine(FadeContent(0f));
            pageFlipper.Flip(
                onPageTurned: () => SetSectionActive(section),
                onFinished: () =>
                {
                    rightPage.interactable = true;
                    StartCoroutine(FadeContent(1f));
                    FocusSection();
                });
        }

        /// <summary>LB / Q: regresa a la página izquierda, a la sección abierta.</summary>
        private void GoToIndex()
        {
            if (IsOnRightPage())
            {
                UiAudio.Play(UiSound.Hover);
                Select(TabFor(current).gameObject);
            }
        }

        /// <summary>RB / E: pasa a los ajustes de la sección abierta (Controles y la portada no tienen).</summary>
        private void GoToSettings()
        {
            Selectable first = FirstSetting(current);
            if (first != null && !IsOnRightPage())
            {
                UiAudio.Play(UiSound.Hover);
                Select(first.gameObject);
            }
        }

        private bool IsOnRightPage()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.transform.IsChildOf(rightPage.transform);
        }

        private Selectable FirstSetting(Section section) => section switch
        {
            Section.Audio => volumeSlider,
            Section.Screen => fullscreenToggle,
            _ => null,
        };

        /// <summary>Lleva la selección al primer ajuste de la sección; Controles no tiene, se queda en el índice.</summary>
        private void FocusSection()
        {
            Selectable first = FirstSetting(current);
            if (current == Section.Home)
            {
                Select(backButton.gameObject);
                return;
            }
            Select(first != null ? first.gameObject : TabFor(current).gameObject);
        }

        private void SetSectionActive(Section section, bool instantRibbon = false)
        {
            current = section;
            if (ribbon != null)
            {
                if (section == Section.Home)
                    ribbon.Retract(instantRibbon);
                else
                    ribbon.PointTo((RectTransform)TabFor(section).transform, instantRibbon);
            }
            if (homeSection != null)
                homeSection.SetActive(section == Section.Home);
            audioSection.SetActive(section == Section.Audio);
            screenSection.SetActive(section == Section.Screen);
            controlsSection.SetActive(section == Section.Controls);
        }

        private Button TabFor(Section section) => section switch
        {
            Section.Audio => audioTab,
            Section.Screen => screenTab,
            Section.Controls => controlsTab,
            _ => backButton,
        };

        /// <summary>Sale del libro cambiando de escena: el iris cubre la pantalla, no hace falta animar el cierre.</summary>
        private void Leave(Action loadScene)
        {
            if (closing || pageFlipper.IsFlipping)
                return;
            closing = true;
            panelGroup.interactable = false;
            GameSettings.Save();
            loadScene();
        }

        private void Close()
        {
            if (closing)
                return;
            closing = true;
            GameSettings.Save();
            StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateOpen()
        {
            panelGroup.interactable = false;
            for (float t = 0f; t < OpenDuration; t += Time.unscaledDeltaTime)
            {
                float p = t / OpenDuration;
                panelGroup.alpha = Mathf.Clamp01(p * 2f);
                // Back ease-out: el libro llega un poco más grande y se asienta.
                float eased = 1f + 2.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f);
                book.localScale = bookScale * Mathf.LerpUnclamped(0.9f, 1f, eased);
                yield return null;
            }
            panelGroup.alpha = 1f;
            book.localScale = bookScale;
            panelGroup.interactable = true;
        }

        private IEnumerator AnimateClose()
        {
            panelGroup.interactable = false;
            for (float t = 0f; t < CloseDuration; t += Time.unscaledDeltaTime)
            {
                float p = t / CloseDuration;
                panelGroup.alpha = 1f - p;
                book.localScale = bookScale * Mathf.Lerp(1f, 0.94f, p);
                yield return null;
            }
            gameObject.SetActive(false);
            closed?.Invoke();
        }

        private IEnumerator FadeContent(float target)
        {
            float from = rightPage.alpha;
            for (float t = 0f; t < ContentFade; t += Time.unscaledDeltaTime)
            {
                rightPage.alpha = Mathf.Lerp(from, target, t / ContentFade);
                yield return null;
            }
            rightPage.alpha = target;
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current == null)
                return;
            UiAudio.MuteHoverBriefly();
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }

        private void OnVolumeChanged(float value)
        {
            GameSettings.MasterVolume = value / 100f;
            UpdateVolumeLabel(value);
            // Suena ya con el volumen nuevo, para que se escuche el nivel elegido.
            UiAudio.Play(UiSound.Hover);
        }

        private void UpdateVolumeLabel(float value) => volumeValue.text = $"{value:0}%";
    }
}
