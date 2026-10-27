using System;
using Odisea.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Odisea.UI
{
    /// <summary>
    /// Tarjeta de "¡Nivel completado!" que sale al llegar a la meta: detiene el juego, aparece con un rebote y
    /// espera a que se pulse Aceptar (Enter, Espacio, clic, A / Cruz o Start) para seguir. La abre <see cref="Level.LevelGoal"/>.
    /// </summary>
    public class LevelCompleteScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.35f;
        [Tooltip("Segundos antes de aceptar el botón, para que un salto que se venía pulsando no la salte.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.6f;

        private Action onContinue;
        private float shownTime = -1f;

        public bool IsShowing => shownTime >= 0f;

        private void Awake() => gameObject.SetActive(false);

        public void Show(Action continueAction)
        {
            if (IsShowing)
                return;
            onContinue = continueAction;
            gameObject.SetActive(true);
            shownTime = Time.unscaledTime;
            Time.timeScale = 0f; // El iris del cambio de escena lo vuelve a 1.
            group.alpha = 0f;
            UiAudio.Play(UiSound.Click);
        }

        private void Update()
        {
            if (!IsShowing)
                return;
            float elapsed = Time.unscaledTime - shownTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);
            group.alpha = t;
            // Crece un poco de más y se asienta.
            float scale = 0.8f + 0.2f * t + Mathf.Sin(t * Mathf.PI) * 0.08f;
            card.localScale = new Vector3(scale, scale, 1f);

            if (onContinue == null || elapsed < inputDelay || !ContinuePressed())
                return;
            UiAudio.Play(UiSound.Click);
            Action action = onContinue;
            onContinue = null;
            action();
        }

        private static bool ContinuePressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame)) ||
                   (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame)) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        private void OnDestroy()
        {
            if (IsShowing)
                Time.timeScale = 1f;
        }
    }
}
