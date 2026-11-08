using System.Collections;
using Odisea.Core;
using Odisea.Level;
using TMPro;
using UnityEngine;

namespace Odisea.UI
{
    /// <summary>
    /// Letrero al empezar el nivel ("¡Protege el horno!"): entra de golpe con un rebote, se queda un momento y se
    /// desvanece subiendo. A la vez hace parpadear lo que hay que cuidar (el horno), para que se vea de qué habla.
    /// </summary>
    public class LevelIntroBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [Tooltip("Opcional: el horno que parpadea mientras está el letrero.")]
        [SerializeField] private Oven oven;
        [Tooltip("Espera después de que se abre la pantalla.")]
        [SerializeField, Min(0f)] private float delay = 0.4f;
        [SerializeField, Min(0.01f)] private float popDuration = 0.35f;
        [SerializeField, Min(0f)] private float holdDuration = 2.2f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;
        [SerializeField] private SoundEffect sound = new();

        private void Start()
        {
            group.alpha = 0f;
            StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            // Que no pase mientras el iris de llegada sigue cerrado.
            while (SceneFlow.IsLoading)
                yield return null;
            yield return new WaitForSeconds(delay);
            if (oven != null)
                oven.Highlight(popDuration + holdDuration + fadeDuration);
            sound.Play();
            Vector2 home = content.anchoredPosition;
            for (float t = 0f; t < 1f; t += Time.deltaTime / popDuration)
            {
                group.alpha = Mathf.Clamp01(t * 2f);
                // Llega grande y rebota hasta su tamaño.
                float scale = Mathf.LerpUnclamped(1.6f, 1f, EaseOutBack(t));
                content.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            group.alpha = 1f;
            content.localScale = Vector3.one;
            yield return new WaitForSeconds(holdDuration);
            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
            {
                group.alpha = 1f - t;
                content.anchoredPosition = home + new Vector2(0f, 60f * t);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
