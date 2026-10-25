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

        // Píldora café con el número en crema: se lee igual sobre fondos claros y oscuros.
        private static TextMeshProUGUI CreateFpsText(Transform parent)
        {
            var pill = new GameObject("FPS", typeof(RectTransform), typeof(Image));
            var pillRect = (RectTransform)pill.transform;
            pillRect.SetParent(parent, false);
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(1f, 1f);
            pillRect.anchoredPosition = new Vector2(-20f, -16f);
            pillRect.sizeDelta = new Vector2(150f, 46f);
            var background = pill.GetComponent<Image>();
            background.sprite = CreatePillSprite();
            background.type = Image.Type.Sliced;
            // Casi opaca: en espacio lineal un café al 78 % sobre blanco se ve gris.
            background.color = new Color(0.169f, 0.118f, 0.086f, 0.92f);
            background.raycastTarget = false;

            var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(pillRect, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = 26;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.984f, 0.973f, 0.949f);
            text.raycastTarget = false;
            return text;
        }

        // Rectángulo redondeado blanco hecho en código (esta capa no depende de assets del proyecto).
        private static Sprite CreatePillSprite()
        {
            const int size = 48;
            const float radius = 23f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance + 0.5f)));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
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
            fpsText.transform.parent.gameObject.SetActive(show);
            frames = 0;
            elapsed = 0f;
            fpsText.text = "";
        }

        private void Update()
        {
            if (fpsText == null || !fpsText.gameObject.activeInHierarchy)
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
