using System;
using System.Collections;
using Odisea.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Odisea.UI
{
    /// <summary>
    /// Pantalla del logo antes del título: el logo aparece lentamente sobre fondo oscuro, se queda un momento,
    /// se desvanece y pasa a la pantalla de título. Cualquier tecla o botón la salta.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logo;

        [Header("Tiempos (segundos)")]
        [Tooltip("Pantalla oscura antes de que empiece a aparecer el logo.")]
        [SerializeField, Min(0f)] private float startDelay = 0.6f;
        [SerializeField, Min(0.01f)] private float fadeIn = 1.6f;
        [SerializeField, Min(0f)] private float hold = 2f;
        [SerializeField, Min(0.01f)] private float fadeOut = 1.2f;

        [Header("Movimiento")]
        [Tooltip("Escala inicial del logo; crece hasta 1 mientras dura la pantalla para que no se sienta estático.")]
        [SerializeField, Range(0.8f, 1f)] private float startScale = 0.94f;

        private IDisposable anyButtonListener;
        private bool skipping;
        private bool leaving;

        private void OnEnable() => anyButtonListener = InputSystem.onAnyButtonPress.Call(_ => skipping = true);

        private void OnDisable()
        {
            anyButtonListener?.Dispose();
            anyButtonListener = null;
        }

        private IEnumerator Start()
        {
            logo.alpha = 0f;
            float total = startDelay + fadeIn + hold + fadeOut;
            float elapsed = 0f;

            // Aparecer y quedarse; si se presiona algo, se pasa directo al desvanecido.
            while (elapsed < startDelay + fadeIn + hold && !skipping)
            {
                elapsed += Time.unscaledDeltaTime;
                float appear = Mathf.Clamp01((elapsed - startDelay) / fadeIn);
                logo.alpha = appear * appear * (3f - 2f * appear); // Suavizado al inicio y al final.
                SetScale(elapsed / total);
                yield return null;
            }

            float from = logo.alpha;
            float duration = skipping ? Mathf.Min(fadeOut, 0.4f) : fadeOut;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                elapsed += Time.unscaledDeltaTime;
                logo.alpha = Mathf.Lerp(from, 0f, t / duration);
                SetScale(elapsed / total);
                yield return null;
            }
            logo.alpha = 0f;

            GoToTitle();
        }

        private void SetScale(float progress) =>
            logo.transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, Mathf.Clamp01(progress));

        private void GoToTitle()
        {
            if (leaving)
                return;
            leaving = true;
            // Sin iris: el logo ya se desvaneció sobre el mismo café con el que arranca el título.
            SceneFlow.LoadWithoutTransition(SceneFlow.Title);
        }
    }
}
