using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Odisea.Core
{
    /// <summary>
    /// Transición entre escenas tipo iris de caricatura: un círculo se cierra hacia el centro,
    /// la pantalla se queda un instante en café oscuro, se carga la escena y el círculo se abre.
    /// Se crea solo la primera vez que se usa y sobrevive a los cambios de escena. Lo usa <see cref="SceneFlow"/>.
    /// Duraciones, color y curvas se ajustan en el asset Resources/Transitions/TransitionSettings.
    /// Si no encuentra el material del iris, hace un fundido simple. Mientras la pantalla está cubierta, abajo a la
    /// derecha camina Ulises con un "Cargando…".
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private const string IrisMaterialPath = "Transitions/Iris";

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

        private static ScreenFader instance;
        private Image overlay;
        private Material irisMaterial;
        private TransitionSettings settings;
        private CanvasGroup loadingSign;
        private Image loadingImage;
        private TMP_Text loadingText;

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
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var overlayObject = new GameObject("Cortina", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)overlayObject.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var fader = root.AddComponent<ScreenFader>();
            fader.settings = Resources.Load<TransitionSettings>(TransitionSettings.ResourcePath);
            if (fader.settings == null)
            {
                Debug.LogWarning($"[Odisea] No se encontró Resources/{TransitionSettings.ResourcePath}; se usan valores por defecto.");
                fader.settings = ScriptableObject.CreateInstance<TransitionSettings>();
            }
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

            fader.CreateLoadingSign(root.transform);
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
            // Se aplican en cada transición para que los cambios del Inspector se vean sin reiniciar.
            if (irisMaterial != null)
            {
                irisMaterial.SetVector(CenterId, center);
                irisMaterial.SetColor(ColorId, settings.color);
                irisMaterial.SetFloat(SoftnessId, settings.edgeSoftness);
            }

            yield return Animate(settings.closeDuration, p => SetOpen(1f - settings.closeCurve.Evaluate(p)));
            float shownAt = Time.unscaledTime;
            yield return ShowLoadingSign(true);
            yield return new WaitForSecondsRealtime(settings.holdDuration);

            // Un menú de pausa pudo dejar el tiempo detenido.
            Time.timeScale = 1f;
            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
            while (!loading.isDone)
                yield return null;
            while (Time.unscaledTime - shownAt < settings.minLoadingTime)
                yield return null;
            yield return ShowLoadingSign(false);

            // Abrir siempre desde el centro.
            if (irisMaterial != null)
                irisMaterial.SetVector(CenterId, new Vector2(0.5f, 0.5f));
            yield return Animate(settings.openDuration, p => SetOpen(settings.openCurve.Evaluate(p)));

            overlay.raycastTarget = false;
            overlay.enabled = false;
            IsBusy = false;
        }

        // ---------- Signo de carga ----------

        private const float LoadingFadeTime = 0.15f;

        private void CreateLoadingSign(Transform parent)
        {
            if (settings.loadingFrames == null || settings.loadingFrames.Length == 0 || settings.loadingFrames[0] == null)
                return;
            var sign = new GameObject("Cargando", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)sign.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-48f, 36f);
            rect.sizeDelta = new Vector2(420f, settings.loadingHeight);
            loadingSign = sign.GetComponent<CanvasGroup>();
            loadingSign.blocksRaycasts = false;
            loadingSign.alpha = 0f;

            Sprite first = settings.loadingFrames[0];
            float height = settings.loadingHeight;
            var ulises = new GameObject("Ulises", typeof(RectTransform), typeof(Image));
            var ulisesRect = (RectTransform)ulises.transform;
            ulisesRect.SetParent(rect, false);
            ulisesRect.anchorMin = ulisesRect.anchorMax = ulisesRect.pivot = new Vector2(1f, 0f);
            ulisesRect.anchoredPosition = Vector2.zero;
            ulisesRect.sizeDelta = new Vector2(height * first.rect.width / first.rect.height, height);
            loadingImage = ulises.GetComponent<Image>();
            loadingImage.sprite = first;
            loadingImage.preserveAspect = true;
            loadingImage.raycastTarget = false;

            if (settings.loadingFont == null)
                return;
            var label = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(1f, 0f);
            labelRect.anchoredPosition = new Vector2(-ulisesRect.sizeDelta.x + 4f, height * 0.16f);
            labelRect.sizeDelta = new Vector2(300f, 50f);
            loadingText = label.GetComponent<TextMeshProUGUI>();
            loadingText.font = settings.loadingFont;
            loadingText.fontSize = 34f;
            loadingText.color = settings.loadingTextColor;
            loadingText.alignment = TextAlignmentOptions.BottomLeft;
            loadingText.raycastTarget = false;
            loadingText.text = "Cargando";
            // Ancho fijo con los tres puntos, para que el texto no brinque al cambiar de puntos.
            labelRect.sizeDelta = new Vector2(loadingText.GetPreferredValues("Cargando...").x + 4f, 50f);
        }

        private IEnumerator ShowLoadingSign(bool show)
        {
            if (loadingSign == null)
                yield break;
            float from = loadingSign.alpha, to = show ? 1f : 0f;
            for (float t = 0f; t < LoadingFadeTime; t += Time.unscaledDeltaTime)
            {
                loadingSign.alpha = Mathf.Lerp(from, to, t / LoadingFadeTime);
                yield return null;
            }
            loadingSign.alpha = to;
        }

        // Ulises camina en su lugar y los puntitos de "Cargando…" van apareciendo (con tiempo sin escala: la pausa
        // pudo dejar el tiempo detenido).
        private void Update()
        {
            if (loadingSign == null || loadingSign.alpha <= 0f)
                return;
            Sprite[] frames = settings.loadingFrames;
            loadingImage.sprite = frames[(int)(Time.unscaledTime * settings.loadingFps) % frames.Length];
            if (loadingText != null)
                loadingText.text = "Cargando" + new string('.', (int)(Time.unscaledTime * 3f) % 4);
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
                Color c = settings.color;
                c.a *= 1f - openness;
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
