using System;
using System.Collections;
using Odisea.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Pantalla del logo antes del título: el logo aparece lentamente sobre fondo oscuro, se queda un momento,
    /// se desvanece y pasa a la pantalla de título. Cualquier tecla o botón la salta.
    /// Cuando el logo ya se ve completo, los brillitos sobre el pan chispean con un sonido y luego parpadean suave.
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

        [Header("Brillitos")]
        [Tooltip("Los brillitos sobre el pan, en el orden en que chispean (izquierda a derecha).")]
        [SerializeField] private Image[] sparkles;
        [Tooltip("Suena una vez, junto con el primer brillito.")]
        [SerializeField] private AudioSource sparkleSound;
        [Tooltip("Separación entre el chispazo de un brillito y el siguiente.")]
        [SerializeField, Min(0f)] private float sparkleStagger = 0.08f;
        [SerializeField, Min(0.05f)] private float sparklePopDuration = 0.35f;
        [Tooltip("Cuánto crece cada brillito al chispear.")]
        [SerializeField, Range(1f, 1.5f)] private float sparklePopScale = 1.15f;
        [Tooltip("Color al que destella el brillito en su punto más alto.")]
        [SerializeField] private Color sparkleFlashColor = new(1f, 0.95f, 0.78f);
        [Tooltip("Segundos (mínimo y máximo) entre un parpadeo y otro mientras el logo se queda en pantalla.")]
        [SerializeField] private Vector2 twinkleInterval = new(0.35f, 0.9f);
        [SerializeField, Min(0.05f)] private float twinkleDuration = 0.4f;
        [Tooltip("Qué tan transparente se pone un brillito al parpadear (1 = no parpadea).")]
        [SerializeField, Range(0f, 1f)] private float twinkleMinAlpha = 0.35f;

        private IDisposable anyButtonListener;
        private Color[] sparkleColors;
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
            sparkles ??= Array.Empty<Image>();
            sparkleColors = Array.ConvertAll(sparkles, s => s.color);
            float total = startDelay + fadeIn + hold + fadeOut;
            float elapsed = 0f;
            bool sparkled = false;

            // Aparecer y quedarse; si se presiona algo, se pasa directo al desvanecido.
            while (elapsed < startDelay + fadeIn + hold && !skipping)
            {
                elapsed += Time.unscaledDeltaTime;
                float appear = Mathf.Clamp01((elapsed - startDelay) / fadeIn);
                logo.alpha = appear * appear * (3f - 2f * appear); // Suavizado al inicio y al final.
                SetScale(elapsed / total);
                if (!sparkled && appear >= 1f)
                {
                    sparkled = true;
                    StartCoroutine(Sparkle());
                }
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

        // Chispazo en cadena con el sonido y, después, parpadeos sueltos hasta que se cambie de escena.
        private IEnumerator Sparkle()
        {
            if (sparkles.Length == 0)
                yield break;

            if (sparkleSound != null && sparkleSound.clip != null)
                sparkleSound.Play();
            for (int i = 0; i < sparkles.Length; i++)
            {
                StartCoroutine(Pop(i));
                yield return Wait(sparkleStagger);
            }
            yield return Wait(sparklePopDuration);

            int last = -1;
            while (true)
            {
                yield return Wait(UnityEngine.Random.Range(twinkleInterval.x, twinkleInterval.y));
                // Nunca el mismo dos veces seguidas, para que no se vea mecánico.
                int next = UnityEngine.Random.Range(0, sparkles.Length);
                if (next == last && sparkles.Length > 1)
                    next = (next + 1) % sparkles.Length;
                last = next;
                yield return Twinkle(next);
            }
        }

        private IEnumerator Pop(int index)
        {
            Transform sparkle = sparkles[index].transform;
            for (float t = 0f; t < sparklePopDuration; t += Time.unscaledDeltaTime)
            {
                float bump = Mathf.Sin(Mathf.PI * t / sparklePopDuration); // 0 → 1 → 0
                sparkle.localScale = Vector3.one * Mathf.Lerp(1f, sparklePopScale, bump);
                sparkles[index].color = Color.Lerp(sparkleColors[index], sparkleFlashColor, bump);
                yield return null;
            }
            sparkle.localScale = Vector3.one;
            sparkles[index].color = sparkleColors[index];
        }

        private IEnumerator Twinkle(int index)
        {
            Color baseColor = sparkleColors[index];
            for (float t = 0f; t < twinkleDuration; t += Time.unscaledDeltaTime)
            {
                float dip = Mathf.Sin(Mathf.PI * t / twinkleDuration);
                Color color = baseColor;
                color.a = baseColor.a * Mathf.Lerp(1f, twinkleMinAlpha, dip);
                sparkles[index].color = color;
                yield return null;
            }
            sparkles[index].color = baseColor;
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                yield return null;
        }

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
