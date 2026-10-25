using System;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Ayuda de botones en la esquina del libro ("Seleccionar", "Atrás", "Página").
    /// Cambia los íconos según se juegue con teclado, mando de Xbox o de PlayStation.
    /// Íconos: Kenney Input Prompts (CC0), en Art/UI/Botones.
    /// </summary>
    public class ButtonPromptBar : MonoBehaviour
    {
        [Serializable]
        public struct PromptSprites
        {
            public Sprite confirm;
            public Sprite back;
            public Sprite pageLeft;
            public Sprite pageRight;
        }

        [SerializeField] private Image confirmIcon;
        [SerializeField] private Image backIcon;
        [SerializeField] private Image pageLeftIcon;
        [SerializeField] private Image pageRightIcon;

        [SerializeField] private PromptSprites keyboard;
        [SerializeField] private PromptSprites xbox;
        [SerializeField] private PromptSprites playStation;

        private void OnEnable()
        {
            InputDeviceTracker.Changed += Apply;
            Apply(InputDeviceTracker.Current);
        }

        private void OnDisable() => InputDeviceTracker.Changed -= Apply;

        private void Apply(InputScheme scheme)
        {
            PromptSprites set = scheme switch
            {
                InputScheme.Xbox => xbox,
                InputScheme.PlayStation => playStation,
                _ => keyboard,
            };
            // Solo Seleccionar es obligatorio: el pie del menú principal no tiene Atrás ni Página.
            confirmIcon.sprite = set.confirm;
            if (backIcon != null)
                backIcon.sprite = set.back;
            if (pageLeftIcon != null)
                pageLeftIcon.sprite = set.pageLeft;
            if (pageRightIcon != null)
                pageRightIcon.sprite = set.pageRight;
        }
    }
}
