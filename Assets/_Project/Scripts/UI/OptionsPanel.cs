using System;
using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Panel de Opciones: volumen general y pantalla completa (se guardan al cerrar).
    /// Esc o B / Círculo regresan, igual que el botón Volver.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button backButton;

        private Action closed;

        private void Awake()
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 100f;
            volumeSlider.wholeNumbers = true;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            fullscreenToggle.onValueChanged.AddListener(value => GameSettings.Fullscreen = value);
            backButton.onClick.AddListener(Close);
        }

        public void Open(Action onClosed)
        {
            closed = onClosed;
            gameObject.SetActive(true);

            volumeSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.MasterVolume * 100f));
            UpdateVolumeLabel(volumeSlider.value);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
            }
        }

        private void Update()
        {
            bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                          (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (cancel)
                Close();
        }

        private void Close()
        {
            GameSettings.Save();
            gameObject.SetActive(false);
            closed?.Invoke();
        }

        private void OnVolumeChanged(float value)
        {
            GameSettings.MasterVolume = value / 100f;
            UpdateVolumeLabel(value);
        }

        private void UpdateVolumeLabel(float value) => volumeValue.text = $"{value:0}%";
    }
}
