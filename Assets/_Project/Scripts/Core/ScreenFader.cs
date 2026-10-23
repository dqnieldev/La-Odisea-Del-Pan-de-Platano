using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Odisea.Core
{
    /// <summary>
    /// Fundido a color café entre escenas. Se crea solo la primera vez que se usa
    /// y sobrevive a los cambios de escena. Lo usa <see cref="SceneFlow"/>.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private const float FadeDuration = 0.3f;
        // Café Panadería del design system.
        private static readonly Color FadeColor = new(0.169f, 0.118f, 0.086f);

        private static ScreenFader instance;
        private Image overlay;

        public bool IsBusy { get; private set; }

        public static ScreenFader Instance
        {
            get
            {
                if (instance == null)
                    instance = Create();
                return instance;
            }
        }

        public void FadeAndLoad(string sceneName)
        {
            if (IsBusy)
                return;
            StartCoroutine(FadeAndLoadRoutine(sceneName));
        }

        private static ScreenFader Create()
        {
            var root = new GameObject("Fundido de pantalla");
            DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Encima de cualquier otra UI.

            var overlayObject = new GameObject("Cortina", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)overlayObject.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var fader = root.AddComponent<ScreenFader>();
            fader.overlay = overlayObject.GetComponent<Image>();
            fader.overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, 0f);
            fader.overlay.raycastTarget = false;
            return fader;
        }

        private IEnumerator FadeAndLoadRoutine(string sceneName)
        {
            IsBusy = true;
            // Bloquea clics mientras la pantalla está oscura.
            overlay.raycastTarget = true;

            yield return Fade(0f, 1f);

            // Un menú de pausa pudo dejar el tiempo detenido.
            Time.timeScale = 1f;
            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
            while (!loading.isDone)
                yield return null;

            yield return Fade(1f, 0f);

            overlay.raycastTarget = false;
            IsBusy = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / FadeDuration));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            Color c = FadeColor;
            c.a = alpha;
            overlay.color = c;
        }
    }
}
