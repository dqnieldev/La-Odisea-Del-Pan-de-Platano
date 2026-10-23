using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Odisea.Core
{
    /// <summary>
    /// Transición entre escenas tipo iris de caricatura: un círculo se cierra hacia el centro,
    /// la pantalla se queda un instante en café oscuro, se carga la escena y el círculo se abre.
    /// Se crea solo la primera vez que se usa y sobrevive a los cambios de escena. Lo usa <see cref="SceneFlow"/>.
    /// Si no encuentra el material del iris, hace un fundido simple.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private const string IrisMaterialPath = "Transitions/Iris";
        private const float CloseDuration = 0.7f;
        private const float HoldDuration = 0.18f;
        private const float OpenDuration = 0.7f;
        // Café Panadería del design system.
        private static readonly Color CurtainColor = new(0.169f, 0.118f, 0.086f);

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int CenterId = Shader.PropertyToID("_Center");

        private static ScreenFader instance;
        private Image overlay;
        private Material irisMaterial;

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

        /// <summary>
        /// Cierra el iris, carga la escena y lo vuelve a abrir.
        /// </summary>
        /// <param name="viewportCenter">Punto de la pantalla (0-1) hacia donde se cierra; por defecto el centro.
        /// Sirve para cerrar sobre Ulises al morir o al terminar un nivel.</param>
        public void FadeAndLoad(string sceneName, Vector2? viewportCenter = null)
        {
            if (IsBusy)
                return;
            StartCoroutine(FadeAndLoadRoutine(sceneName, viewportCenter ?? new Vector2(0.5f, 0.5f)));
        }

        private static ScreenFader Create()
        {
            var root = new GameObject("Transición de pantalla");
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
            fader.overlay.raycastTarget = false;

            var irisTemplate = Resources.Load<Material>(IrisMaterialPath);
            if (irisTemplate != null)
            {
                fader.irisMaterial = new Material(irisTemplate);
                fader.overlay.material = fader.irisMaterial;
                fader.overlay.color = Color.white; // El color lo pone el material.
            }
            else
            {
                Debug.LogWarning($"[Odisea] No se encontró Resources/{IrisMaterialPath}; se usará un fundido simple.");
            }

            fader.SetOpen(1f);
            fader.overlay.enabled = false; // Solo se dibuja durante una transición.
            return fader;
        }

        private IEnumerator FadeAndLoadRoutine(string sceneName, Vector2 center)
        {
            IsBusy = true;
            overlay.enabled = true;
            // Bloquea clics mientras la transición cubre la pantalla.
            overlay.raycastTarget = true;
            if (irisMaterial != null)
                irisMaterial.SetVector(CenterId, center);

            // Cerrar: empieza lento y acelera, como un iris que se cierra de golpe al final.
            yield return Animate(CloseDuration, p => SetOpen(1f - p * p * p));
            yield return new WaitForSecondsRealtime(HoldDuration);

            // Un menú de pausa pudo dejar el tiempo detenido.
            Time.timeScale = 1f;
            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
            while (!loading.isDone)
                yield return null;

            // Abrir siempre desde el centro: arranca rápido y frena al final.
            if (irisMaterial != null)
                irisMaterial.SetVector(CenterId, new Vector2(0.5f, 0.5f));
            yield return Animate(OpenDuration, p => SetOpen(1f - (1f - p) * (1f - p) * (1f - p)));

            overlay.raycastTarget = false;
            overlay.enabled = false;
            IsBusy = false;
        }

        private static IEnumerator Animate(float duration, System.Action<float> apply)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                apply(t / duration);
                yield return null;
            }
            apply(1f);
        }

        /// <summary>0 = pantalla cubierta, 1 = totalmente abierta.</summary>
        private void SetOpen(float openness)
        {
            if (irisMaterial != null)
            {
                Vector2 center = irisMaterial.GetVector(CenterId);
                irisMaterial.SetFloat(RadiusId, openness * MaxRadius(center));
            }
            else
            {
                Color c = CurtainColor;
                c.a = 1f - openness;
                overlay.color = c;
            }
        }

        /// <summary>Radio que deja ver toda la pantalla: la distancia del centro a la esquina más lejana.</summary>
        private static float MaxRadius(Vector2 center)
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float dx = Mathf.Max(center.x, 1f - center.x) * aspect;
            float dy = Mathf.Max(center.y, 1f - center.y);
            return Mathf.Sqrt(dx * dx + dy * dy) + 0.02f;
        }
    }
}
