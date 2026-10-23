using System;
using Odisea.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Video;

namespace Odisea.UI
{
    /// <summary>
    /// Pantalla de inicio: reproduce el video en loop y pasa al menú principal
    /// al presionar cualquier tecla, botón del mouse o del mando.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private VideoPlayer video;
        [Tooltip("Segundos al inicio en los que se ignoran las teclas, para no saltarse la pantalla por accidente.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.5f;

        private IDisposable anyButtonListener;
        private float shownAt;
        private bool leaving;

        private void Start()
        {
            shownAt = Time.unscaledTime;
            if (video != null)
            {
                // Se prepara antes de reproducir para que no haya un cuadro negro al inicio.
                video.isLooping = true;
                video.prepareCompleted += OnVideoPrepared;
                video.Prepare();
            }
        }

        private void OnEnable()
        {
            anyButtonListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }

        private void OnDisable()
        {
            anyButtonListener?.Dispose();
            anyButtonListener = null;
            if (video != null)
                video.prepareCompleted -= OnVideoPrepared;
        }

        private void OnVideoPrepared(VideoPlayer player) => player.Play();

        private void OnAnyButtonPressed(InputControl control)
        {
            if (leaving || Time.unscaledTime - shownAt < inputDelay)
                return;

            leaving = true;
            SceneFlow.Load(SceneFlow.MainMenu);
        }
    }
}
