using System;
using System.Collections;
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
    /// Al llegar desde el logo, la pantalla empieza oscura y se aclara cuando el video ya está corriendo.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private VideoPlayer video;
        [Tooltip("Cortina oscura encima de todo; se desvanece al empezar el video.")]
        [SerializeField] private CanvasGroup fadeCover;
        [Tooltip("Segundos que tarda en aclararse la pantalla.")]
        [SerializeField, Min(0.01f)] private float fadeInDuration = 1.5f;
        [Tooltip("Segundos al inicio en los que se ignoran las teclas, para no saltarse la pantalla por accidente.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.8f;

        private IDisposable anyButtonListener;
        private float shownAt;
        private bool leaving;

        private void Start()
        {
            shownAt = Time.unscaledTime;
            if (fadeCover != null)
                fadeCover.alpha = 1f;

            if (video != null)
            {
                // Se prepara antes de reproducir para que no haya un cuadro negro al inicio.
                video.isLooping = true;
                video.prepareCompleted += OnVideoPrepared;
                video.Prepare();
            }
            StartCoroutine(FadeInWhenVideoRuns());
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

        // Unity pausa el video cuando la ventana pierde el foco (Alt+Tab) y no lo reanuda por sí solo.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && video != null && video.isPrepared && !video.isPlaying)
                video.Play();
        }

        private IEnumerator FadeInWhenVideoRuns()
        {
            if (fadeCover == null)
                yield break;

            // Espera a que el video ya muestre cuadros (máximo 2 s, por si no hay video o tarda en cargar).
            for (float waited = 0f; waited < 2f; waited += Time.unscaledDeltaTime)
            {
                if (video == null || (video.isPlaying && video.frame > 0))
                    break;
                yield return null;
            }

            for (float t = 0f; t < fadeInDuration; t += Time.unscaledDeltaTime)
            {
                float p = t / fadeInDuration;
                fadeCover.alpha = 1f - p * p * (3f - 2f * p); // Suave al inicio y al final.
                yield return null;
            }
            fadeCover.alpha = 0f;
            fadeCover.blocksRaycasts = false;
        }

        private void OnAnyButtonPressed(InputControl control)
        {
            if (leaving || Time.unscaledTime - shownAt < inputDelay)
                return;

            leaving = true;
            UiAudio.Play(UiSound.Click);
            SceneFlow.Load(SceneFlow.MainMenu);
        }
    }
}
