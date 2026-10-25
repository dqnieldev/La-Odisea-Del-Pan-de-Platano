using Odisea.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.UI
{
    /// <summary>
    /// Capa que vive todo el juego encima de cualquier escena: aplica el Brillo de Opciones a toda la imagen
    /// y muestra el contador de FPS si está activado. Se crea sola al arrancar.
    /// </summary>
    public class ScreenOverlay : MonoBehaviour
    {
        private const string BrightnessMaterialPath = "UI/Brillo";
        // Cuánto aclara u oscurece el brillo en sus extremos (0 → ×0.6, 1 → ×1.4).
        private const float BrightnessRange = 0.4f;
        // Cada cuánto se actualiza el número de FPS, para que se pueda leer.
        private const float FpsRefresh = 0.5f;

        private Image brightness;
        private TextMeshProUGUI fpsText;
        private int frames;
        private float elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var root = new GameObject("Capa de pantalla (brillo y FPS)");
            DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // Encima de la UI del juego y debajo de la transición de escenas.
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var overlay = root.AddComponent<ScreenOverlay>();
            overlay.brightness = CreateBrightnessLayer(root.transform);
            overlay.fpsText = CreateFpsText(root.transform);
        }

        private static Image CreateBrightnessLayer(Transform parent)
        {
            var go = new GameObject("Brillo", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.material = Resources.Load<Material>(BrightnessMaterialPath);
            if (image.material == null || image.material == image.defaultMaterial)
            {
                Debug.LogWarning($"[Odisea] No se encontró Resources/{BrightnessMaterialPath}; el brillo no tendrá efecto.");
                go.SetActive(false);
            }
            return image;
        }

        private static TextMeshProUGUI CreateFpsText(Transform parent)
        {
            var go = new GameObject("FPS", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -18f);
            rect.sizeDelta = new Vector2(220f, 50f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = 30;
            text.alignment = TextAlignmentOptions.TopRight;
            text.color = new Color(0.98f, 0.97f, 0.95f, 0.9f);
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color32(43, 30, 22, 255);
            text.raycastTarget = false;
            return text;
        }

        private void OnEnable()
        {
            GameSettings.BrightnessChanged += ApplyBrightness;
            GameSettings.ShowFpsChanged += ApplyShowFps;
        }

        // En Start y no en OnEnable: OnEnable corre al agregar el componente, antes de que Create asigne las capas.
        private void Start()
        {
            ApplyBrightness(GameSettings.Brightness);
            ApplyShowFps(GameSettings.ShowFps);
        }

        private void OnDisable()
        {
            GameSettings.BrightnessChanged -= ApplyBrightness;
            GameSettings.ShowFpsChanged -= ApplyShowFps;
        }

        private void ApplyBrightness(float value)
        {
            if (brightness == null)
                return;
            // El shader multiplica por 2 × color: 0.5 es neutro.
            float factor = 1f + (value - 0.5f) * 2f * BrightnessRange;
            float gray = factor * 0.5f;
            // El proyecto trabaja en espacio lineal: Unity convierte el color de la UI antes de mezclar,
            // así que se pasa a gamma para que llegue al shader exactamente como factor / 2.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                gray = Mathf.LinearToGammaSpace(gray);
            brightness.color = new Color(gray, gray, gray, 1f);
            // En el punto neutro no se dibuja nada (ahorra un pase a pantalla completa).
            brightness.enabled = Mathf.Abs(value - 0.5f) > 0.005f;
        }

        private void ApplyShowFps(bool show)
        {
            if (fpsText == null)
                return;
            fpsText.gameObject.SetActive(show);
            frames = 0;
            elapsed = 0f;
            fpsText.text = "";
        }

        private void Update()
        {
            if (fpsText == null || !fpsText.gameObject.activeSelf)
                return;
            frames++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < FpsRefresh)
                return;
            fpsText.text = $"{Mathf.RoundToInt(frames / elapsed)} FPS";
            frames = 0;
            elapsed = 0f;
        }
    }
}
