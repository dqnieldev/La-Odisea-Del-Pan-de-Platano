using System;
using System.IO;
using System.Linq;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Arma la pantalla de inicio (video en loop + "Presiona cualquier botón") y el menú principal
    /// (Jugar, Tutorial, Opciones, Salir), y deja la lista de escenas del build en orden.
    /// El video y la imagen de fondo se toman de Docs/Referencias/Menu.
    /// Menú: Odisea > Menús > Crear pantalla de inicio y menú principal.
    /// </summary>
    public static class MenuScenesBuilder
    {
        private const string SourceFolder = "Docs/Referencias/Menu";
        private const string TitleScenePath = "Assets/_Project/Scenes/Menus/Title.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/Menus/MainMenu.unity";
        private const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox/PlayerSandbox.unity";
        private const string VideoFolder = "Assets/_Project/Video";
        private const string UiArtFolder = "Assets/_Project/Art/UI";

        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".webm", ".m4v" };
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".jfif" };

        // Design system.
        private static readonly Color Platano = new(0.961f, 0.718f, 0.192f);
        private static readonly Color PlatanoOscuro = new(0.878f, 0.635f, 0.118f);
        private static readonly Color Cafe = new(0.169f, 0.118f, 0.086f);
        private static readonly Color Crema = new(0.984f, 0.973f, 0.949f);
        private static readonly Color BeigeMasa = new(0.910f, 0.878f, 0.824f);

        [MenuItem("Odisea/Menús/Crear pantalla de inicio y menú principal")]
        public static void Build()
        {
            // Primero se importan y guardan los assets. Las referencias se vuelven a cargar después de crear
            // cada escena: NewScene descarga los assets en memoria y las referencias previas quedan inválidas.
            string videoPath = AssetPathOf(ImportUserFile<VideoClip>(VideoExtensions, VideoFolder, "IntroLoop"));
            string backgroundPath = AssetPathOf(ImportBackground());
            UiAssetsBuilder.EnsureRoundedSprite();
            UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            AssetDatabase.SaveAssets();

            BuildTitleScene(videoPath);
            BuildMainMenuScene(backgroundPath);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Odisea] Pantalla de inicio y menú principal creados.");
        }

        // ---------- Pantalla de inicio ----------

        private static void BuildTitleScene(string videoPath)
        {
            var scene = NewScene(TitleScenePath);
            var video = videoPath != null ? AssetDatabase.LoadAssetAtPath<VideoClip>(videoPath) : null;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            Camera camera = CreateCamera(Cafe);

            // El video se dibuja en el plano lejano de la cámara, cubriendo toda la pantalla.
            var audio = camera.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var player = camera.gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.renderMode = VideoRenderMode.CameraFarPlane;
            player.targetCamera = camera;
            player.aspectRatio = VideoAspectRatio.FitOutside;
            player.clip = video;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, audio);
            if (video == null)
                Debug.LogWarning($"[Odisea] No hay video en {SourceFolder}; la pantalla de inicio queda con fondo café.");

            Transform canvas = CreateCanvas("UI");

            // "Presiona cualquier botón para comenzar" sobre una píldora café, abajo al centro.
            var prompt = new GameObject("Presiona un botón", typeof(RectTransform), typeof(CanvasGroup), typeof(BlinkingGroup));
            var promptRect = (RectTransform)prompt.transform;
            promptRect.SetParent(canvas, false);
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 110f);
            promptRect.sizeDelta = new Vector2(980f, 96f);
            CreateImage("Fondo", promptRect, rounded, new Color(Cafe.r, Cafe.g, Cafe.b, 0.78f), Vector2.zero, Vector2.zero, stretch: true);
            CreateText("Texto", promptRect, "Presiona cualquier botón para comenzar", font, 46, Crema, stretch: true);

            var title = new GameObject("Pantalla de inicio").AddComponent<TitleScreen>();
            SetField(title, "video", player);

            EditorSceneManager.SaveScene(scene);
        }

        // ---------- Menú principal ----------

        private static void BuildMainMenuScene(string backgroundPath)
        {
            var scene = NewScene(MenuScenePath);
            var background = backgroundPath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath) : null;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            TMP_FontAsset titleFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            TMP_FontAsset bodyFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            CreateCamera(Cafe);
            CreateEventSystem();
            Transform canvas = CreateCanvas("UI");

            // Fondo: cubre la pantalla sin deformarse (recorta lo que sobre).
            var backgroundImage = CreateImage("Fondo", canvas, null, background != null ? Color.white : Crema,
                Vector2.zero, Vector2.zero, stretch: true);
            if (background != null)
            {
                backgroundImage.sprite = background;
                var fitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = background.rect.width / background.rect.height;
            }
            else
            {
                Debug.LogWarning($"[Odisea] No hay imagen en {SourceFolder}; el menú queda con fondo crema.");
            }

            // Botones en columna a la izquierda: la imagen de fondo deja ese lado oscuro y libre.
            var buttons = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var buttonsRect = (RectTransform)buttons.transform;
            buttonsRect.SetParent(canvas, false);
            buttonsRect.anchorMin = buttonsRect.anchorMax = new Vector2(0.2f, 0.42f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.sizeDelta = new Vector2(480f, 520f);
            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Button play = CreateMenuButton("Jugar", buttonsRect, rounded, titleFont, new Vector2(440f, 100f), 46);
            Button tutorial = CreateMenuButton("Tutorial", buttonsRect, rounded, titleFont, new Vector2(440f, 100f), 46);
            Button options = CreateMenuButton("Opciones", buttonsRect, rounded, titleFont, new Vector2(440f, 100f), 46);
            Button quit = CreateMenuButton("Salir", buttonsRect, rounded, titleFont, new Vector2(440f, 100f), 46);

            OptionsPanel optionsPanel = CreateOptionsPanel(canvas, rounded, titleFont, bodyFont);

            var menu = new GameObject("Menú principal").AddComponent<MainMenu>();
            SetField(menu, "playButton", play);
            SetField(menu, "tutorialButton", tutorial);
            SetField(menu, "optionsButton", options);
            SetField(menu, "quitButton", quit);
            SetField(menu, "buttonsPanel", buttons);
            SetField(menu, "optionsPanel", optionsPanel);

            EditorSceneManager.SaveScene(scene);
        }

        private static OptionsPanel CreateOptionsPanel(Transform canvas, Sprite rounded, TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            // Scrim café detrás del panel (design system).
            var root = new GameObject("Opciones", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            Stretch(rootRect);
            CreateImage("Scrim", rootRect, null, new Color(0.110f, 0.075f, 0.055f, 0.72f), Vector2.zero, Vector2.zero, stretch: true);

            RectTransform panel = CreateCard("Panel", rootRect, rounded, new Vector2(1000f, 760f), Crema);

            CreateText("Título", panel, "Opciones", titleFont, 64, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -80f), rectSize: new Vector2(800f, 90f));

            // Volumen.
            CreateText("Etiqueta volumen", panel, "Volumen general", bodyFont, 36, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(-240f, -190f), rectSize: new Vector2(380f, 60f), alignment: TextAlignmentOptions.Left);
            Slider volume = CreateSlider(panel, rounded, new Vector2(150f, -190f));
            TMP_Text volumeValue = CreateText("Valor volumen", panel, "100%", bodyFont, 34, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(390f, -190f), rectSize: new Vector2(120f, 60f), alignment: TextAlignmentOptions.Right);

            // Pantalla completa.
            CreateText("Etiqueta pantalla completa", panel, "Pantalla completa", bodyFont, 36, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(-240f, -280f), rectSize: new Vector2(380f, 60f), alignment: TextAlignmentOptions.Left);
            Toggle fullscreen = CreateToggle(panel, rounded, new Vector2(-10f, -280f));

            // Controles.
            CreateText("Título controles", panel, "CONTROLES", titleFont, 32, PlatanoOscuro, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -370f), rectSize: new Vector2(800f, 50f), alignment: TextAlignmentOptions.Left);
            CreateText("Tabla controles", panel,
                "<b>Moverse / escaleras</b>   A·D·W·S o flechas  ·  stick o cruceta\n" +
                "<b>Saltar</b> (2 veces = doble salto)   Espacio  ·  A / Cruz\n" +
                "<b>Golpear con el rodillo</b>   J o clic  ·  X / Cuadrado\n" +
                "<b>Lanzar masa</b>   K o clic derecho  ·  Y / Triángulo",
                bodyFont, 28, Cafe, anchor: new Vector2(0.5f, 1f), position: new Vector2(0f, -495f),
                rectSize: new Vector2(800f, 190f), alignment: TextAlignmentOptions.TopLeft);

            Button back = CreateMenuButton("Volver", panel, rounded, titleFont, new Vector2(300f, 84f), 38);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 50f);

            var options = root.AddComponent<OptionsPanel>();
            SetField(options, "volumeSlider", volume);
            SetField(options, "volumeValue", volumeValue);
            SetField(options, "fullscreenToggle", fullscreen);
            SetField(options, "backButton", back);
            return options;
        }

        // ---------- Piezas de UI ----------

        /// <summary>Botón del design system: sombra café desplazada, borde café y relleno plátano.</summary>
        private static Button CreateMenuButton(string label, RectTransform parent, Sprite rounded, TMP_FontAsset font, Vector2 size, float fontSize)
        {
            var root = new GameObject($"Botón {label}", typeof(RectTransform), typeof(Button));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;

            CreateImage("Sombra", rect, rounded, Cafe, new Vector2(0f, -6f), size, stretch: false);

            var face = new GameObject("Cara", typeof(RectTransform));
            var faceRect = (RectTransform)face.transform;
            faceRect.SetParent(rect, false);
            Stretch(faceRect);
            CreateImage("Borde", faceRect, rounded, Cafe, Vector2.zero, Vector2.zero, stretch: true);
            Image fill = CreateImage("Relleno", faceRect, rounded, Platano, Vector2.zero, Vector2.zero, stretch: true, inset: 4f);
            CreateText("Texto", faceRect, label, font, fontSize, Cafe, stretch: true);

            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            var feedback = root.AddComponent<MenuButtonFeedback>();
            SetField(feedback, "face", faceRect);
            SetField(feedback, "fill", fill);
            return button;
        }

        /// <summary>Tarjeta con sombra desplazada y borde café.</summary>
        private static RectTransform CreateCard(string name, RectTransform parent, Sprite rounded, Vector2 size, Color color)
        {
            var card = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)card.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            CreateImage("Sombra", rect, rounded, Cafe, new Vector2(0f, -8f), size, stretch: false);
            CreateImage("Borde", rect, rounded, Cafe, Vector2.zero, Vector2.zero, stretch: true);
            CreateImage("Fondo", rect, rounded, color, Vector2.zero, Vector2.zero, stretch: true, inset: 5f);
            return rect;
        }

        private static Slider CreateSlider(RectTransform parent, Sprite rounded, Vector2 position)
        {
            var root = new GameObject("Slider volumen", typeof(RectTransform), typeof(Slider));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(340f, 44f);

            Image track = CreateImage("Riel", rect, rounded, BeigeMasa, Vector2.zero, new Vector2(0f, 18f), stretch: false);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.pixelsPerUnitMultiplier = 3f;

            var fillArea = new GameObject("Área relleno", typeof(RectTransform));
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.SetParent(rect, false);
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0f, 18f);
            Image fill = CreateImage("Relleno", fillAreaRect, rounded, Platano, Vector2.zero, Vector2.zero, stretch: true);
            fill.pixelsPerUnitMultiplier = 3f;

            var handleArea = new GameObject("Área perilla", typeof(RectTransform));
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.SetParent(rect, false);
            Stretch(handleAreaRect);
            Image handle = CreateImage("Perilla", handleAreaRect, rounded, Color.white, Vector2.zero, new Vector2(44f, 44f), stretch: false);
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.pixelsPerUnitMultiplier = 1.1f; // Esquinas de 22 px en una perilla de 44 px = círculo.

            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.colors = SelectionColors();
            // El Slider estira la perilla a toda la altura (anclas y de 0 a 1); con alto extra 0 mide 44 × 44 y queda circular.
            handle.rectTransform.sizeDelta = new Vector2(44f, 0f);
            return slider;
        }

        private static Toggle CreateToggle(RectTransform parent, Sprite rounded, Vector2 position)
        {
            var root = new GameObject("Toggle pantalla completa", typeof(RectTransform), typeof(Toggle));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(60f, 60f);

            Image border = CreateImage("Borde", rect, rounded, Cafe, Vector2.zero, Vector2.zero, stretch: true);
            Image box = CreateImage("Caja", rect, rounded, Color.white, Vector2.zero, Vector2.zero, stretch: true, inset: 4f);
            Image check = CreateImage("Palomita", rect, rounded, Cafe, Vector2.zero, Vector2.zero, stretch: true, inset: 14f);
            border.pixelsPerUnitMultiplier = box.pixelsPerUnitMultiplier = 2f;
            check.pixelsPerUnitMultiplier = 4f;

            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.colors = SelectionColors(normal: BeigeMasa);
            return toggle;
        }

        /// <summary>Colores de selección: café en reposo, plátano al estar seleccionado (teclado/mando) o con el mouse encima.</summary>
        private static ColorBlock SelectionColors(Color? normal = null)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal ?? Cafe;
            colors.highlightedColor = Platano;
            colors.selectedColor = Platano;
            colors.pressedColor = PlatanoOscuro;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 position,
            Vector2 size, bool stretch, float inset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            if (stretch)
            {
                Stretch(rect);
                rect.offsetMin = new Vector2(inset, inset);
                rect.offsetMax = new Vector2(-inset, -inset);
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
                image.type = Image.Type.Sliced;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string content, TMP_FontAsset font, float fontSize,
            Color color, bool stretch = false, Vector2? anchor = null, Vector2? position = null, Vector2? rectSize = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            if (stretch)
            {
                Stretch(rect);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position ?? Vector2.zero;
                rect.sizeDelta = rectSize ?? new Vector2(400f, 80f);
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ---------- Escena, cámara, canvas ----------

        private static UnityEngine.SceneManagement.Scene NewScene(string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            return scene;
        }

        private static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static Transform CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        private static void CreateEventSystem()
        {
            // Teclado, mouse y mando navegan la UI con el nuevo Input System.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[] { TitleScenePath, MenuScenePath, SandboxScenePath }
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        // ---------- Archivos del usuario ----------

        private static T ImportUserFile<T>(string[] extensions, string destinationFolder, string destinationName) where T : UnityEngine.Object
        {
            string source = Directory.Exists(SourceFolder)
                ? Directory.GetFiles(SourceFolder).FirstOrDefault(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                : null;
            if (source == null)
                return null;

            EnsureFolder(destinationFolder);
            // .jfif es un JPEG que Unity no reconoce por su extensión; se copia como .jpg.
            string extension = Path.GetExtension(source).ToLowerInvariant();
            if (extension == ".jfif")
                extension = ".jpg";
            string destination = $"{destinationFolder}/{destinationName}{extension}";
            // Solo copia si el archivo cambió, para no reimportar el video cada vez.
            if (!File.Exists(destination) || new FileInfo(destination).Length != new FileInfo(source).Length)
            {
                File.Copy(source, destination, overwrite: true);
                AssetDatabase.ImportAsset(destination);
            }
            return AssetDatabase.LoadAssetAtPath<T>(destination);
        }

        private static Sprite ImportBackground()
        {
            var texture = ImportUserFile<Texture2D>(ImageExtensions, UiArtFolder, "FondoMenu");
            if (texture == null)
                return null;

            string path = AssetDatabase.GetAssetPath(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static string AssetPathOf(UnityEngine.Object asset) => asset != null ? AssetDatabase.GetAssetPath(asset) : null;

        private static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo '{field}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
