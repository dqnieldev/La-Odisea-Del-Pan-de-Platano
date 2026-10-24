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
    /// Opciones como un recetario viejo: índice en la página izquierda (Audio, Pantalla, Controles, Volver)
    /// y la sección elegida en la derecha. Al elegir otra sección la hoja da vuelta (<see cref="PageFlipper"/>).
    /// Esc o B / Círculo: desde la página derecha regresa al índice; desde el índice cierra el libro.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        private enum Section
        {
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

        [Header("Índice (página izquierda)")]
        [SerializeField] private Button audioTab;
        [SerializeField] private Button screenTab;
        [SerializeField] private Button controlsTab;
        [SerializeField] private Button backButton;

        [Header("Secciones (página derecha)")]
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

        private void Awake()
        {
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
        }

        public void Open(Action onClosed)
        {
            closed = onClosed;
            closing = false;
            gameObject.SetActive(true);

            volumeSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.MasterVolume * 100f));
            UpdateVolumeLabel(volumeSlider.value);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);

            SetSectionActive(Section.Audio);
            rightPage.alpha = 1f;
            rightPage.interactable = true;
            Select(audioTab.gameObject);
            StartCoroutine(AnimateOpen());
        }

        private void Update()
        {
            if (closing || pageFlipper.IsFlipping)
                return;

            bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                          (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
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

            if (section == current)
            {
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

        /// <summary>Lleva la selección al primer ajuste de la sección; Controles no tiene, se queda en el índice.</summary>
        private void FocusSection()
        {
            Selectable first = current switch
            {
                Section.Audio => volumeSlider,
                Section.Screen => fullscreenToggle,
                _ => null,
            };
            Select(first != null ? first.gameObject : TabFor(current).gameObject);
        }

        private void SetSectionActive(Section section)
        {
            current = section;
            audioSection.SetActive(section == Section.Audio);
            screenSection.SetActive(section == Section.Screen);
            controlsSection.SetActive(section == Section.Controls);
        }

        private Button TabFor(Section section) => section switch
        {
            Section.Audio => audioTab,
            Section.Screen => screenTab,
            _ => controlsTab,
        };

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
                book.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, eased);
                yield return null;
            }
            panelGroup.alpha = 1f;
            book.localScale = Vector3.one;
            panelGroup.interactable = true;
        }

        private IEnumerator AnimateClose()
        {
            panelGroup.interactable = false;
            for (float t = 0f; t < CloseDuration; t += Time.unscaledDeltaTime)
            {
                float p = t / CloseDuration;
                panelGroup.alpha = 1f - p;
                book.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, p);
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
