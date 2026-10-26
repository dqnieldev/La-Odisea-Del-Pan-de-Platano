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
        [Tooltip("Opcional: volumen de la música.")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicValue;
        [Tooltip("Opcional: volumen de los efectos (golpes, mordidas, sonidos de los menús).")]
        [SerializeField] private Slider effectsSlider;
        [SerializeField] private TMP_Text effectsValue;

        [Header("Pantalla")]
        [SerializeField] private OptionSelector windowModeSelector;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private TMP_Text brightnessValue;
        [SerializeField] private OptionSelector frameRateSelector;
        [SerializeField] private Toggle showFpsToggle;

        [Header("Controles")]
        [Tooltip("Solo se ve al jugar con mando (la oculta ControlsDiagram).")]
        [SerializeField] private Toggle vibrationToggle;

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
            if (musicSlider != null)
            {
                musicSlider.minValue = 0f;
                musicSlider.maxValue = 100f;
                musicSlider.wholeNumbers = false;
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }
            if (effectsSlider != null)
            {
                effectsSlider.minValue = 0f;
                effectsSlider.maxValue = 100f;
                effectsSlider.wholeNumbers = false;
                effectsSlider.onValueChanged.AddListener(OnEffectsChanged);
            }

            // Mismo orden que el enum WindowMode.
            windowModeSelector.SetOptions(new[] { "Completa", "Sin bordes", "Ventana" });
            windowModeSelector.Changed += index => GameSettings.WindowMode = (WindowMode)index;
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 100f;
            brightnessSlider.wholeNumbers = false;
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            frameRateSelector.SetOptions(Array.ConvertAll(GameSettings.FrameRateOptions, fps => fps > 0 ? fps.ToString() : "Sin límite"));
            frameRateSelector.Changed += index => GameSettings.FrameRateLimit = GameSettings.FrameRateOptions[index];
            showFpsToggle.onValueChanged.AddListener(value => GameSettings.ShowFps = value);
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);

            audioTab.onClick.AddListener(() => ShowSection(Section.Audio));
            screenTab.onClick.AddListener(() => ShowSection(Section.Screen));
            controlsTab.onClick.AddListener(() => ShowSection(Section.Controls));
            backButton.onClick.AddListener(Close);
            if (restartButton != null)
                restartButton.onClick.AddListener(() => Leave(() => SceneFlow.ReloadCurrent()));
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
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.MusicVolume * 100f));
                musicValue.text = $"{musicSlider.value:0}%";
            }
            if (effectsSlider != null)
            {
                effectsSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.EffectsVolume * 100f));
                effectsValue.text = $"{effectsSlider.value:0}%";
            }
            windowModeSelector.SetIndexWithoutNotify((int)GameSettings.WindowMode);
            brightnessSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.Brightness * 100f));
            brightnessValue.text = $"{brightnessSlider.value:0}%";
            int frameRate = Array.IndexOf(GameSettings.FrameRateOptions, GameSettings.FrameRateLimit);
            frameRateSelector.SetIndexWithoutNotify(frameRate >= 0 ? frameRate : 1);
            showFpsToggle.SetIsOnWithoutNotify(GameSettings.ShowFps);
            vibrationToggle.SetIsOnWithoutNotify(GameSettings.Vibration);

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

            // Si lo seleccionado se ocultó (p. ej. Vibración al cambiar de mando a teclado), vuelve al índice.
            GameObject focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (focused != null && !focused.activeInHierarchy)
                Select(TabFor(current).gameObject);

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

        /// <summary>RB / E: pasa a los ajustes de la sección abierta (la portada no tiene; Controles solo con mando).</summary>
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
            Section.Screen => windowModeSelector,
            Section.Controls => vibrationToggle.gameObject.activeInHierarchy ? vibrationToggle : null,
            _ => null,
        };

        /// <summary>Lleva la selección al primer ajuste de la sección; si no tiene (Controles con teclado), se queda en el índice.</summary>
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

        private void OnBrightnessChanged(float value)
        {
            GameSettings.Brightness = value / 100f;
            brightnessValue.text = $"{value:0}%";
            UiAudio.Play(UiSound.Hover);
        }

        private void OnVibrationChanged(bool on)
        {
            GameSettings.Vibration = on;
            // Al activarla, un zumbido corto para que se sienta; al desactivarla, se corta lo que esté vibrando.
            if (on)
                GamepadRumble.Play(0.25f, 0.6f, 0.15f);
            else
                GamepadRumble.Stop();
        }

        private void OnEffectsChanged(float value)
        {
            GameSettings.EffectsVolume = value / 100f;
            effectsValue.text = $"{value:0}%";
            // Suena ya con el volumen nuevo de efectos, para que se escuche el nivel elegido.
            UiAudio.Play(UiSound.Hover);
        }

        private void OnMusicChanged(float value)
        {
            GameSettings.MusicVolume = value / 100f;
            musicValue.text = $"{value:0}%";
            UiAudio.Play(UiSound.Hover);
        }
    }
}
