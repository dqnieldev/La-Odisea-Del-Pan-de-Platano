using System;
using System.IO;
using System.Linq;
using Odisea.Core;
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
        private const string SplashScenePath = "Assets/_Project/Scenes/Menus/Splash.unity";
        private const string TitleScenePath = "Assets/_Project/Scenes/Menus/Title.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/Menus/MainMenu.unity";
        private const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox/PlayerSandbox.unity";
        private const string VideoFolder = "Assets/_Project/Video";
        private const string UiArtFolder = "Assets/_Project/Art/UI";

        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".webm", ".m4v" };
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".jfif" };

        // Design system.
        internal static readonly Color Platano = new(0.961f, 0.718f, 0.192f);
        internal static readonly Color PlatanoOscuro = new(0.878f, 0.635f, 0.118f);
        internal static readonly Color Cafe = new(0.169f, 0.118f, 0.086f);
        internal static readonly Color Crema = new(0.984f, 0.973f, 0.949f);
        internal static readonly Color BeigeMasa = new(0.910f, 0.878f, 0.824f);
        // Tinte del fondo del menú: vuelve madera el rodillo dibujado en tonos fríos (#E2B07E).
        internal static readonly Color TinteMadera = new(0.886f, 0.690f, 0.494f);

        [MenuItem("Odisea/Menús/Crear pantalla de inicio y menú principal")]
        public static void Build()
        {
            // Primero se importan y guardan los assets. Las referencias se vuelven a cargar después de crear
            // cada escena: NewScene descarga los assets en memoria y las referencias previas quedan inválidas.
            // Cada archivo se busca por su nombre en Docs/Referencias/Menu.
            string videoPath = AssetPathOf(ImportUserFile<VideoClip>("Pantalla_inicio", VideoExtensions, VideoFolder, "IntroLoop"));
            string backgroundPath = AssetPathOf(ImportSprite("Background_inicio", "FondoMenu"));
            string bookPath = AssetPathOf(ImportSprite("recetario-libro-opciones", "LibroRecetas"));
            string pagePath = AssetPathOf(ImportUserFile<VideoClip>("animacion-menu", VideoExtensions, VideoFolder, "PasarPagina"));
            UiAssetsBuilder.EnsureRoundedSprite();
            UiAssetsBuilder.EnsureLumaKeyMaterial();
            UiAssetsBuilder.EnsureBrightnessMaterial();
            UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikRegular);
            foreach (string sound in new[] { "ui_hover", "ui_click", "ui_back", PageSoundName })
                UiAssetsBuilder.LoadUiSound(sound);
            AssetDatabase.SaveAssets();

            // El logo mide 3200 px; en pantalla se ve a ~560 px, así que 1024 basta y pesa mucho menos.
            string logoPath = AssetPathOf(ImportSprite("logo-juego", "LogoJuego", maxSize: 1024));
            // Piezas recortadas del logo para animar los brillitos: si existen, sustituyen al logo completo.
            if (LoadLocalSprite($"{LogoPartsFolder}/LogoSinBrillos.png", 1024) != null)
            {
                foreach (string side in SparkleNames)
                    LoadLocalSprite($"{LogoPartsFolder}/LogoBrillo{side}.png", 256);
            }
            UiAssetsBuilder.LoadUiSound(LogoSparkleSoundName);
            AssetDatabase.SaveAssets();

            BuildSplashScene(logoPath);
            BuildTitleScene(videoPath);
            BuildMainMenuScene(backgroundPath, bookPath, pagePath);
            BuildPausePrefab();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Odisea] Pantalla de inicio y menú principal creados.");
        }

        // ---------- Logo (antes del título) ----------

        private const string LogoPartsFolder = UiArtFolder + "/Logo";
        private const string LogoSparkleSoundName = "logo_brillo";
        private static readonly string[] SparkleNames = { "Izquierdo", "Centro", "Derecho" };
        // Dónde está cada brillito dentro del logo (anclas normalizadas, abajo-izquierda / arriba-derecha)
        // y el punto desde el que crece al chispear: el extremo que mira al pan.
        private static readonly (Vector2 min, Vector2 max, Vector2 pivot)[] SparkleLayout =
        {
            (new Vector2(0.6062f, 0.7569f), new Vector2(0.6759f, 0.8375f), new Vector2(0.85f, 0.1f)),
            (new Vector2(0.7409f, 0.7819f), new Vector2(0.7734f, 0.8750f), new Vector2(0.5f, 0.05f)),
            (new Vector2(0.8406f, 0.7588f), new Vector2(0.9100f, 0.8394f), new Vector2(0.15f, 0.1f)),
        };
        // Amarillo del logo; los brillitos son blancos para poder destellar por encima de este color.
        private static readonly Color LogoYellow = new(0.961f, 0.718f, 0.192f);

        private static void BuildSplashScene(string logoPath)
        {
            var scene = NewScene(SplashScenePath);
            var logo = logoPath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(logoPath) : null;
            var logoBase = AssetDatabase.LoadAssetAtPath<Sprite>($"{LogoPartsFolder}/LogoSinBrillos.png");
            Sprite[] sparkleSprites = Array.ConvertAll(SparkleNames,
                side => AssetDatabase.LoadAssetAtPath<Sprite>($"{LogoPartsFolder}/LogoBrillo{side}.png"));
            bool animatedSparkles = logoBase != null && sparkleSprites.All(s => s != null);
            // Mismo café que el fondo del logo (#2B1E16): las orillas del ícono se funden con la pantalla.
            CreateCamera(Cafe);
            Transform canvas = CreateCanvas("UI");

            var logoObject = new GameObject("Logo", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)logoObject.transform;
            rect.SetParent(canvas, false);
            rect.sizeDelta = new Vector2(560f, 560f);
            var image = logoObject.GetComponent<Image>();
            image.sprite = animatedSparkles ? logoBase : logo;
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (logo == null && !animatedSparkles)
                Debug.LogWarning($"[Odisea] No está logo-juego en {SourceFolder}; la pantalla del logo queda vacía.");

            var splash = new GameObject("Pantalla del logo").AddComponent<SplashScreen>();
            SetField(splash, "logo", logoObject.GetComponent<CanvasGroup>());

            if (animatedSparkles)
            {
                var serialized = new SerializedObject(splash);
                SerializedProperty list = serialized.FindProperty("sparkles");
                list.arraySize = SparkleNames.Length;
                for (int i = 0; i < SparkleNames.Length; i++)
                {
                    var sparkle = new GameObject($"Brillo {SparkleNames[i]}", typeof(RectTransform), typeof(Image));
                    var sparkleRect = (RectTransform)sparkle.transform;
                    sparkleRect.SetParent(rect, false);
                    (sparkleRect.anchorMin, sparkleRect.anchorMax, sparkleRect.pivot) = SparkleLayout[i];
                    sparkleRect.offsetMin = sparkleRect.offsetMax = Vector2.zero;
                    var sparkleImage = sparkle.GetComponent<Image>();
                    sparkleImage.sprite = sparkleSprites[i];
                    sparkleImage.color = LogoYellow;
                    sparkleImage.raycastTarget = false;
                    list.GetArrayElementAtIndex(i).objectReferenceValue = sparkleImage;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
                Debug.LogWarning($"[Odisea] Faltan las piezas del logo en {LogoPartsFolder}; los brillitos no se animan.");

            AudioClip sparkleClip = UiAssetsBuilder.LoadUiSound(LogoSparkleSoundName);
            if (sparkleClip != null)
            {
                var audio = splash.gameObject.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.clip = sparkleClip;
                SetField(splash, "sparkleSound", audio);
            }
            else
                Debug.LogWarning($"[Odisea] No está {LogoSparkleSoundName} en Audio/SFX/UI; los brillitos chispean sin sonido.");
            EditorSceneManager.SaveScene(scene);
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

            CreateUiAudio();
            // Cortina café encima de todo: la pantalla llega oscura desde el logo y se aclara al empezar el video.
            Image cover = CreateImage("Cortina de entrada", canvas, null, Cafe, Vector2.zero, Vector2.zero, stretch: true);
            var coverGroup = cover.gameObject.AddComponent<CanvasGroup>();
            coverGroup.interactable = false;

            var title = new GameObject("Pantalla de inicio").AddComponent<TitleScreen>();
            SetField(title, "video", player);
            SetField(title, "fadeCover", coverGroup);

            EditorSceneManager.SaveScene(scene);
        }

        // ---------- Menú principal ----------

        private static void BuildMainMenuScene(string backgroundPath, string bookPath, string pagePath)
        {
            var scene = NewScene(MenuScenePath);
            var background = backgroundPath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath) : null;
            var bookSprite = bookPath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(bookPath) : null;
            var pageClip = pagePath != null ? AssetDatabase.LoadAssetAtPath<VideoClip>(pagePath) : null;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            TMP_FontAsset titleFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            TMP_FontAsset bodyFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            CreateCamera(Cafe);
            CreateEventSystem();
            CreateUiAudio();
            CreateMenuMusic();
            Transform canvas = CreateCanvas("UI");

            // Fondo: cubre la pantalla sin deformarse (recorta lo que sobre).
            var backgroundImage = CreateImage("Fondo", canvas, null, background != null ? TinteMadera : Crema,
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

            // Menú minimalista estilo Cuphead: solo texto, del lado izquierdo y centrado verticalmente (zona negra del fondo).
            var buttons = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var buttonsRect = (RectTransform)buttons.transform;
            buttonsRect.SetParent(canvas, false);
            buttonsRect.anchorMin = buttonsRect.anchorMax = new Vector2(0.14f, 0.5f);
            buttonsRect.pivot = new Vector2(0.5f, 0.5f);
            buttonsRect.sizeDelta = new Vector2(600f, 410f);
            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            TMP_FontAsset menuFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            Button play = CreateTextMenuItem("JUGAR", buttonsRect, menuFont);
            Button tutorial = CreateTextMenuItem("TUTORIAL", buttonsRect, menuFont);
            Button options = CreateTextMenuItem("OPCIONES", buttonsRect, menuFont);
            Button credits = CreateTextMenuItem("CRÉDITOS", buttonsRect, menuFont);
            Button quit = CreateTextMenuItem("SALIR", buttonsRect, menuFont);

            // "¿Salir del juego?" en el mismo lugar que los botones; MainMenu la muestra al elegir SALIR.
            var confirm = new GameObject("Confirmar salida", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.SetParent(canvas, false);
            confirmRect.anchorMin = confirmRect.anchorMax = buttonsRect.anchorMin;
            confirmRect.sizeDelta = buttonsRect.sizeDelta;
            var confirmLayout = confirm.GetComponent<VerticalLayoutGroup>();
            confirmLayout.spacing = 6f;
            confirmLayout.childAlignment = TextAnchor.MiddleCenter;
            confirmLayout.childControlWidth = confirmLayout.childControlHeight = false;
            confirmLayout.childForceExpandWidth = confirmLayout.childForceExpandHeight = false;
            TMP_Text question = CreateText("Pregunta", confirmRect, "¿SALIR DEL JUEGO?", menuFont, 46,
                new Color(Crema.r, Crema.g, Crema.b, 0.75f), rectSize: new Vector2(600f, 80f));
            question.characterSpacing = 4f;
            question.textWrappingMode = TextWrappingModes.NoWrap;
            Button quitNo = CreateTextMenuItem("NO", confirmRect, menuFont);
            Button quitYes = CreateTextMenuItem("SÍ", confirmRect, menuFont);
            confirm.SetActive(false);

            GameObject footer = CreateMenuFooter(canvas, bodyFont);

            OptionsPanel optionsPanel = CreateRecipeBook(canvas, bookSprite, pageClip, rounded, titleFont, bodyFont, menuFont,
                BookMode.Options, out _);

            var menu = new GameObject("Menú principal").AddComponent<MainMenu>();
            SetField(menu, "playButton", play);
            SetField(menu, "tutorialButton", tutorial);
            SetField(menu, "optionsButton", options);
            SetField(menu, "creditsButton", credits);
            SetField(menu, "quitButton", quit);
            SetField(menu, "buttonsPanel", buttons);
            SetField(menu, "optionsPanel", optionsPanel);
            SetField(menu, "footer", footer);
            SetField(menu, "quitConfirm", confirm);
            SetField(menu, "quitNoButton", quitNo);
            SetField(menu, "quitYesButton", quitYes);

            EditorSceneManager.SaveScene(scene);
        }

        // ---------- Recetario: Opciones (menú principal) y Pausa (dentro del juego) ----------

        private const string PausePrefabPath = "Assets/_Project/Prefabs/UI/MenuPausa.prefab";

        // Medidas del libro (PNG y video de 1920×1080): centro y tamaño del área útil de cada página.
        private static readonly Vector2 LeftPageCenter = new(-405f, 10f);
        private static readonly Vector2 RightPageCenter = new(407f, 10f);
        private static readonly Vector2 PageSize = new(600f, 700f);
        internal static readonly Color TintaClara = new(0.45f, 0.36f, 0.27f);
        // Sonido de hoja que da vuelta; suena solo al cambiar de sección en el recetario.
        private const string PageSoundName = "ui_clickOptions";
        // El libro de pausa es más chico para que se vea el juego alrededor.
        private const float PauseBookScale = 0.72f;
        // Altura de cada renglón del índice de la pausa: Continuar, Reiniciar, Volver al mapa | Audio, Pantalla,
        // Controles | Ir al título, Salir al menú.
        private static readonly float[] PauseIndexY = { -235f, -290f, -345f, -420f, -475f, -530f, -605f, -660f };

        /// <summary>
        /// Agrega "Volver al mapa" al prefab de la pausa ya hecho, sin regenerarlo (regenerarlo cambia los ids internos y
        /// los niveles que lo usan pierden la referencia).
        /// </summary>
        [MenuItem("Odisea/Menús/Agregar \"Volver al mapa\" a la pausa")]
        public static void AddMapButtonToPausePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PausePrefabPath);
            try
            {
                OptionsPanel options = root.GetComponentInChildren<OptionsPanel>(true);
                var serialized = new SerializedObject(options);
                if (serialized.FindProperty("mapButton").objectReferenceValue != null)
                {
                    Debug.Log("[Odisea] La pausa ya tiene \"Volver al mapa\".");
                    return;
                }
                var restart = (Button)serialized.FindProperty("restartButton").objectReferenceValue;
                Transform index = restart.transform.parent;
                Button Item(string label) => index.Find($"Botón {label}").GetComponent<Button>();
                Button map = UnityEngine.Object.Instantiate(restart, index);
                map.name = "Botón VOLVER AL MAPA";
                // El texto y sus dos aberraciones de color.
                foreach (TMP_Text text in map.GetComponentsInChildren<TMP_Text>(true))
                    text.text = "VOLVER AL MAPA";
                map.transform.SetSiblingIndex(restart.transform.GetSiblingIndex() + 1);
                Button[] order =
                {
                    Item("CONTINUAR"), restart, map, Item("AUDIO"), Item("PANTALLA"), Item("CONTROLES"), Item("IR AL TÍTULO"),
                    Item("SALIR AL MENÚ"),
                };
                for (int i = 0; i < order.Length; i++)
                {
                    var rect = (RectTransform)order[i].transform;
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, PauseIndexY[i]);
                }
                LinkVertically(order);
                serialized.FindProperty("mapButton").objectReferenceValue = map;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
                Debug.Log("[Odisea] \"Volver al mapa\" agregado a la pausa.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private enum BookMode
        {
            Options,
            Pause,
        }

        /// <summary>
        /// Crea el menú de pausa como prefab (Assets/_Project/Prefabs/UI/MenuPausa.prefab) para ponerlo en cada nivel.
        /// Trae su propio canvas, EventSystem y sonidos de UI.
        /// </summary>
        [MenuItem("Odisea/Menús/Crear menú de pausa (prefab)")]
        public static void BuildPausePrefab()
        {
            string bookPath = AssetPathOf(ImportSprite("recetario-libro-opciones", "LibroRecetas"));
            string pagePath = AssetPathOf(ImportUserFile<VideoClip>("animacion-menu", VideoExtensions, VideoFolder, "PasarPagina"));
            var bookSprite = bookPath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(bookPath) : null;
            var pageClip = pagePath != null ? AssetDatabase.LoadAssetAtPath<VideoClip>(pagePath) : null;

            var root = new GameObject("Menú de pausa", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // Encima del HUD del nivel; debajo del iris de transición.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateEventSystem().transform.SetParent(root.transform, false);
            CreateUiAudio().transform.SetParent(root.transform, false);

            OptionsPanel book = CreateRecipeBook(root.transform, bookSprite, pageClip, UiAssetsBuilder.EnsureRoundedSprite(),
                UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium),
                UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold), BookMode.Pause, out TMP_Text levelTitle);

            var pause = root.AddComponent<PauseMenu>();
            SetField(pause, "book", book);
            SetField(pause, "levelTitle", levelTitle);

            EnsureFolder(Path.GetDirectoryName(PausePrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[Odisea] Menú de pausa creado en {PausePrefabPath}");
        }

        /// <summary>
        /// Game Over de un nivel: tarjeta "¡Se quemó el pan!" con la barra de qué tan lejos llegó Ulises (él encima, en
        /// chibi) y los botones Reintentar, Reiniciar nivel y salir. Empieza apagada; la muestra la muerte de Ulises.
        /// </summary>
        internal static GameOverScreen CreateGameOverScreen(Health player, Vector2 levelStart)
        {
            var root = new GameObject("Game Over", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600; // Encima del HUD y de la pausa; debajo del iris de transición.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var rootRect = (RectTransform)root.transform;

            var panel = new GameObject("Tarjeta y fondo", typeof(RectTransform), typeof(CanvasGroup));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(rootRect, false);
            Stretch(panelRect);
            CreateImage("Oscurecer", panelRect, null, new Color(Cafe.r, Cafe.g, Cafe.b, 0.8f), Vector2.zero, Vector2.zero,
                stretch: true).raycastTarget = true;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            RectTransform card = CreatePaperSheet("Hoja", panelRect, new Vector2(1000f, 800f));
            CreateText("Título", card, "¡SE QUEMÓ EL PAN!", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular), 78, Cafe,
                position: new Vector2(0f, 270f), rectSize: new Vector2(900f, 110f));
            CreateOrnamentRule(card, new Vector2(0f, 205f), 420f);

            // Qué tan lejos llegó: barra de plátano con Ulises encima y la meta al final.
            var progress = new GameObject("Avance", typeof(RectTransform));
            var progressRect = (RectTransform)progress.transform;
            progressRect.SetParent(card, false);
            TMP_Text label = CreateText("Texto", progressRect, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.ImFellEnglish), 40,
                TintaClara, position: new Vector2(0f, 40f), rectSize: new Vector2(900f, 50f));
            Image track = CreateImage("Barra", progressRect, null, Cafe, new Vector2(0f, 92f), new Vector2(680f, 30f),
                stretch: false);
            CreateImage("Fondo", track.rectTransform, null, BeigeMasa, Vector2.zero, Vector2.zero, stretch: true, inset: 3f);
            // El relleno necesita un sprite para llenarse por partes: el mismo papel, teñido de plátano.
            Sprite paper = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.PaperPath);
            Image fill = CreateImage("Relleno", track.rectTransform, paper != null ? paper : rounded, Platano, Vector2.zero,
                Vector2.zero, stretch: true, inset: 5f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            Image goalMark = CreateImage("Meta", track.rectTransform, AssetDatabase.LoadAssetAtPath<Sprite>(TutorialBuilder.CirclePath),
                Cafe, Vector2.zero, new Vector2(30f, 30f), stretch: false);
            goalMark.rectTransform.anchorMin = goalMark.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            Sprite[] chibi = ArtImportBuilder.LoadStrip(ArtImportBuilder.MapIdlePath);
            Image marker = CreateImage("Ulises", track.rectTransform, chibi.Length > 0 ? chibi[0] : null, Color.white,
                Vector2.zero, new Vector2(72f, 82f), stretch: false);
            marker.preserveAspect = true;
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0f, 1f);
            marker.rectTransform.pivot = new Vector2(0.5f, 0f);
            marker.rectTransform.anchoredPosition = new Vector2(0f, -4f);

            // Opciones como las del índice del recetario (Opciones y Pausa): tinta café sobre el papel.
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            Button retry = CreateIndexItem("REINTENTAR", card, font, 0f, 56f);
            Button restart = CreateIndexItem("REINICIAR NIVEL", card, font, 0f, 56f);
            Button exit = CreateIndexItem("VOLVER AL MAPA", card, font, 0f, 56f);
            float y = -60f;
            foreach (Button button in new[] { retry, restart, exit })
            {
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(640f, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(0f, y);
                y -= 95f;
                button.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            }

            var screen = root.AddComponent<GameOverScreen>();
            SetField(screen, "player", player);
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("levelStart").vector2Value = levelStart;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetField(screen, "panel", panel.GetComponent<CanvasGroup>());
            SetField(screen, "card", card);
            SetField(screen, "progressFill", fill);
            SetField(screen, "progressMarker", marker.rectTransform);
            SetField(screen, "progressLabel", label);
            SetField(screen, "progressGroup", progress);
            SetField(screen, "retryButton", retry);
            SetField(screen, "restartButton", restart);
            SetField(screen, "exitButton", exit);
            return screen;
        }

        /// <summary>
        /// Hoja de papel viejo (el papel del fondo del tutorial) un poco chueca, con sombra, doble marco de tinta y
        /// rombos en las esquinas. Si falta el papel, queda crema.
        /// </summary>
        private static RectTransform CreatePaperSheet(string name, RectTransform parent, Vector2 size)
        {
            var sheet = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)sheet.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            CreateImage("Sombra", rect, null, new Color(0f, 0f, 0f, 0.45f), new Vector2(12f, -14f), size, stretch: false);
            var paper = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.PaperPath);
            CreateImage("Papel", rect, paper, paper != null ? Color.white : Crema, Vector2.zero, Vector2.zero, stretch: true);
            var ink = new Color(Cafe.r, Cafe.g, Cafe.b, 0.85f);
            CreateFrame(rect, "Marco", 28f, 5f, ink);
            CreateFrame(rect, "Marco fino", 42f, 2f, new Color(Cafe.r, Cafe.g, Cafe.b, 0.6f));
            foreach (Vector2 corner in new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) })
            {
                Vector2 position = new(corner.x * (size.x / 2f - 30.5f), corner.y * (size.y / 2f - 30.5f));
                CreateDiamond(rect, position, 22f, Cafe);
            }
            return rect;
        }

        /// <summary>Recuadro de cuatro líneas a <paramref name="inset"/> del borde del padre.</summary>
        private static void CreateFrame(RectTransform parent, string name, float inset, float thickness, Color color)
        {
            var frame = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)frame.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            // Arriba, abajo, izquierda, derecha.
            (Vector2 min, Vector2 max)[] sides =
            {
                (new Vector2(0f, 1f), new Vector2(1f, 1f)), (new Vector2(0f, 0f), new Vector2(1f, 0f)),
                (new Vector2(0f, 0f), new Vector2(0f, 1f)), (new Vector2(1f, 0f), new Vector2(1f, 1f)),
            };
            foreach ((Vector2 min, Vector2 max) in sides)
            {
                Image line = CreateImage("Línea", rect, null, color, Vector2.zero, Vector2.zero, stretch: false);
                line.rectTransform.anchorMin = min;
                line.rectTransform.anchorMax = max;
                // Lo grueso hacia los lados y un poco de más a lo largo para cerrar las esquinas.
                line.rectTransform.sizeDelta = new Vector2(thickness, thickness);
                line.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private static void CreateDiamond(RectTransform parent, Vector2 position, float size, Color color)
        {
            Image diamond = CreateImage("Rombo", parent, null, color, position, new Vector2(size, size) / 1.414f, stretch: false);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>Raya de adorno bajo un título: dos líneas finas con un rombo en medio.</summary>
        private static void CreateOrnamentRule(RectTransform parent, Vector2 position, float width)
        {
            var color = new Color(Cafe.r, Cafe.g, Cafe.b, 0.7f);
            float half = width / 2f;
            CreateImage("Raya izquierda", parent, null, color, position + new Vector2(-half / 2f - 14f, 0f), new Vector2(half - 20f, 2.5f), stretch: false);
            CreateImage("Raya derecha", parent, null, color, position + new Vector2(half / 2f + 14f, 0f), new Vector2(half - 20f, 2.5f), stretch: false);
            CreateDiamond(parent, position, 18f, Cafe);
        }

        /// <summary>
        /// Tarjeta de nivel completado (se crea dentro de la escena del nivel; la abre la meta): la misma hoja de papel
        /// que el Game Over, con el título y, si el nivel lleva puntaje, los renglones (ingredientes, tiempo, enemigos,
        /// sin daño), el total y el sello del rango; sin puntaje, la frase. Abajo "Continuar" con el botón del
        /// dispositivo actual.
        /// </summary>
        internal static LevelCompleteScreen CreateLevelCompleteScreen(string title, string message)
        {
            var root = new GameObject("Nivel completado", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600; // Encima del HUD y de la pausa; debajo del iris de transición.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var rootRect = (RectTransform)root.transform;

            CreateImage("Oscurecer", rootRect, null, new Color(Cafe.r, Cafe.g, Cafe.b, 0.7f), Vector2.zero, Vector2.zero, stretch: true);
            RectTransform card = CreatePaperSheet("Hoja", rootRect, new Vector2(1000f, 800f));
            TMP_Text titleText = CreateText("Título", card, title.ToUpperInvariant(), UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular),
                70, Cafe, position: new Vector2(0f, 285f), rectSize: new Vector2(860f, 100f));
            // En un solo renglón: los títulos largos se achican.
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 40f;
            titleText.fontSizeMax = 70f;
            CreateOrnamentRule(card, new Vector2(0f, 225f), 420f);
            TMP_FontAsset inkFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.ImFellEnglish);
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            TMP_Text phrase = CreateText("Frase", card, message, inkFont, 42, TintaClara,
                position: new Vector2(0f, 40f), rectSize: new Vector2(860f, 200f));

            // ----- Puntaje: renglones a la izquierda, sello del rango a la derecha -----
            var score = new GameObject("Puntaje", typeof(RectTransform));
            var scoreRect = (RectTransform)score.transform;
            scoreRect.SetParent(card, false);
            Stretch(scoreRect);

            var list = new GameObject("Renglones", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var listRect = (RectTransform)list.transform;
            listRect.SetParent(scoreRect, false);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = new Vector2(-150f, 180f);
            listRect.sizeDelta = new Vector2(560f, 280f);
            var listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 6f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            string[] names = { "Ingredientes", "Tiempo", "Enemigos", "Sin daño" };
            var rows = new UnityEngine.Object[names.Length];
            var details = new UnityEngine.Object[names.Length];
            var points = new UnityEngine.Object[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var row = new GameObject(names[i], typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
                var rowRect = (RectTransform)row.transform;
                rowRect.SetParent(listRect, false);
                row.GetComponent<LayoutElement>().preferredHeight = 62f;
                CreateText("Nombre", rowRect, names[i], font, 38, Cafe, anchor: new Vector2(0f, 0.5f),
                    position: new Vector2(130f, 0f), rectSize: new Vector2(260f, 60f), alignment: TextAlignmentOptions.MidlineLeft);
                details[i] = CreateText("Detalle", rowRect, "", inkFont, 40, TintaClara, anchor: new Vector2(0f, 0.5f),
                    position: new Vector2(330f, 0f), rectSize: new Vector2(130f, 60f), alignment: TextAlignmentOptions.MidlineLeft);
                points[i] = CreateText("Puntos", rowRect, "", font, 38, Cafe, anchor: new Vector2(1f, 0.5f),
                    position: new Vector2(-75f, 0f), rectSize: new Vector2(150f, 60f), alignment: TextAlignmentOptions.MidlineRight);
                rows[i] = row.GetComponent<CanvasGroup>();
            }

            var total = new GameObject("Total", typeof(RectTransform), typeof(CanvasGroup));
            var totalRect = (RectTransform)total.transform;
            totalRect.SetParent(scoreRect, false);
            totalRect.anchoredPosition = new Vector2(-150f, -150f);
            totalRect.sizeDelta = new Vector2(560f, 90f);
            CreateImage("Raya", totalRect, null, new Color(Cafe.r, Cafe.g, Cafe.b, 0.7f), new Vector2(0f, 48f), new Vector2(560f, 3f), stretch: false);
            CreateText("Nombre", totalRect, "TOTAL", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular), 48, Cafe,
                anchor: new Vector2(0f, 0.5f), position: new Vector2(130f, 0f), rectSize: new Vector2(260f, 80f),
                alignment: TextAlignmentOptions.MidlineLeft);
            TMP_Text totalText = CreateText("Puntos", totalRect, "0", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), 58, Cafe,
                anchor: new Vector2(1f, 0.5f), position: new Vector2(-110f, 0f), rectSize: new Vector2(220f, 80f),
                alignment: TextAlignmentOptions.MidlineRight);

            // Sello de tinta roja: anillo doble con la letra del rango y la frase.
            var stamp = new GameObject("Sello", typeof(RectTransform));
            var stampRect = (RectTransform)stamp.transform;
            stampRect.SetParent(scoreRect, false);
            stampRect.anchoredPosition = new Vector2(285f, 30f);
            stampRect.sizeDelta = new Vector2(250f, 250f);
            stampRect.localRotation = Quaternion.Euler(0f, 0f, -12f);
            var ink = new Color(0.784f, 0.263f, 0.169f, 0.9f);
            Sprite ring = UiAssetsBuilder.EnsureRingSprite();
            CreateImage("Aro", stampRect, ring, ink, Vector2.zero, new Vector2(250f, 250f), stretch: false);
            CreateImage("Aro fino", stampRect, ring, ink, Vector2.zero, new Vector2(212f, 212f), stretch: false);
            TMP_Text letter = CreateText("Letra", stampRect, "S", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular), 150, ink,
                position: new Vector2(0f, 8f), rectSize: new Vector2(200f, 190f));
            TMP_Text rankPhrase = CreateText("Frase", stampRect, "", inkFont, 34, ink,
                position: new Vector2(-30f, -155f), rectSize: new Vector2(340f, 50f));
            rankPhrase.textWrappingMode = TextWrappingModes.NoWrap;
            // La frase va derecha aunque el sello esté chueco.
            rankPhrase.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            TMP_Text record = CreateText("Récord", scoreRect, "¡NUEVO RÉCORD!", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), 36,
                new Color(0.784f, 0.263f, 0.169f), position: new Vector2(285f, -205f), rectSize: new Vector2(340f, 60f));
            record.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            record.gameObject.AddComponent<BlinkingGroup>();

            // ----- Continuar -----
            var prompt = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var promptRect = (RectTransform)prompt.transform;
            promptRect.SetParent(card, false);
            promptRect.anchoredPosition = new Vector2(0f, -300f);
            promptRect.sizeDelta = new Vector2(500f, 64f);
            var layout = prompt.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Image icon = CreatePromptIcon(promptRect);
            icon.GetComponent<LayoutElement>().preferredWidth = icon.GetComponent<LayoutElement>().preferredHeight = 56f;
            CreatePromptLabel(promptRect, font, "Continuar").fontSize = 38;
            var bar = prompt.AddComponent<ButtonPromptBar>();
            SetField(bar, "confirmIcon", icon);
            SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_lb", "xbox_rb");
            SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_l1", "ps_r1");

            var screen = root.AddComponent<LevelCompleteScreen>();
            SetField(screen, "group", root.GetComponent<CanvasGroup>());
            SetField(screen, "card", card);
            SetField(screen, "message", phrase.gameObject);
            SetField(screen, "prompt", prompt);
            SetField(screen, "scoreGroup", score);
            SetField(screen, "totalGroup", total.GetComponent<CanvasGroup>());
            SetField(screen, "totalText", totalText);
            SetField(screen, "stamp", stampRect);
            SetField(screen, "rankLetter", letter);
            SetField(screen, "rankPhrase", rankPhrase);
            SetField(screen, "recordLabel", record.gameObject);
            var serialized = new SerializedObject(screen);
            SetArray(serialized.FindProperty("rows"), rows);
            SetArray(serialized.FindProperty("rowDetails"), details);
            SetArray(serialized.FindProperty("rowPoints"), points);
            // Letra del sello por rango (C, B, A, S): todas en tinta roja; la S en plátano oscuro, como de oro.
            SerializedProperty colors = serialized.FindProperty("rankColors");
            colors.arraySize = 4;
            for (int i = 0; i < 3; i++)
                colors.GetArrayElementAtIndex(i).colorValue = ink;
            colors.GetArrayElementAtIndex(3).colorValue = PlatanoOscuro;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        private static void SetArray(SerializedProperty array, UnityEngine.Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>
        /// Recetario: el libro (PNG), el índice a la izquierda, la sección a la derecha y encima la hoja que da
        /// vuelta (video con el negro quitado por el shader LumaKey). En modo Pausa es más chico, deja ver el juego
        /// y su índice tiene Continuar, Reiniciar nivel, Ir al título y Salir al menú.
        /// </summary>
        private static OptionsPanel CreateRecipeBook(Transform canvas, Sprite bookSprite, VideoClip pageClip, Sprite rounded,
            TMP_FontAsset titleFont, TMP_FontAsset bodyFont, TMP_FontAsset menuFont, BookMode mode, out TMP_Text levelTitle)
        {
            bool pause = mode == BookMode.Pause;
            var root = new GameObject(pause ? "Pausa (recetario)" : "Opciones (recetario)", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            Stretch(rootRect);
            CreateImage("Scrim", rootRect, null, new Color(0.110f, 0.075f, 0.055f, pause ? 0.45f : 0.8f),
                Vector2.zero, Vector2.zero, stretch: true);

            // El libro mide exactamente lo mismo que el video para que las páginas coincidan.
            var book = new GameObject("Libro", typeof(RectTransform));
            var bookRect = (RectTransform)book.transform;
            bookRect.SetParent(rootRect, false);
            bookRect.sizeDelta = new Vector2(1920f, 1080f);
            if (pause)
                bookRect.localScale = Vector3.one * PauseBookScale;
            Image bookImage = CreateImage("Imagen del libro", bookRect, bookSprite, bookSprite != null ? Color.white : Crema,
                Vector2.zero, Vector2.zero, stretch: true);
            if (bookSprite == null)
                Debug.LogWarning($"[Odisea] No está recetario-libro-opciones en {SourceFolder}; el recetario queda sin libro.");
            bookImage.preserveAspect = true;

            // ----- Página izquierda: índice -----
            RectTransform left = CreatePage("Página izquierda (índice)", bookRect, LeftPageCenter);
            CreateText("Título", left, pause ? "Pausa" : "Opciones", titleFont, 76, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -70f), rectSize: new Vector2(560f, 100f));
            CreateText("Subtítulo", left, "Recetario de Don Lupe", bodyFont, 30, TintaClara, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -135f), rectSize: new Vector2(560f, 50f));
            CreateDivider(left, -180f);

            Button audioTab, screenTab, controlsTab, titleTab, closeTab;
            Button restartTab = null, mapTab = null, exitTab = null;
            if (pause)
            {
                // Mismas posiciones que PausePrefabLayout (que acomoda un prefab ya hecho sin regenerarlo).
                const float size = 46f;
                closeTab = CreateIndexItem("CONTINUAR", left, menuFont, PauseIndexY[0], size);
                restartTab = CreateIndexItem("REINICIAR NIVEL", left, menuFont, PauseIndexY[1], size);
                mapTab = CreateIndexItem("VOLVER AL MAPA", left, menuFont, PauseIndexY[2], size);
                audioTab = CreateIndexItem("AUDIO", left, menuFont, PauseIndexY[3], size);
                screenTab = CreateIndexItem("PANTALLA", left, menuFont, PauseIndexY[4], size);
                controlsTab = CreateIndexItem("CONTROLES", left, menuFont, PauseIndexY[5], size);
                titleTab = CreateIndexItem("IR AL TÍTULO", left, menuFont, PauseIndexY[6], size);
                exitTab = CreateIndexItem("SALIR AL MENÚ", left, menuFont, PauseIndexY[7], size);
                LinkVertically(closeTab, restartTab, mapTab, audioTab, screenTab, controlsTab, titleTab, exitTab);
            }
            else
            {
                audioTab = CreateIndexItem("AUDIO", left, menuFont, -280f);
                screenTab = CreateIndexItem("PANTALLA", left, menuFont, -360f);
                controlsTab = CreateIndexItem("CONTROLES", left, menuFont, -440f);
                titleTab = CreateIndexItem("IR AL TÍTULO", left, menuFont, -565f);
                closeTab = CreateIndexItem("VOLVER", left, menuFont, -640f);
                SetEnum(closeTab.GetComponent<SelectableSounds>(), "pressSound", (int)UiSound.Back);
                LinkVertically(audioTab, screenTab, controlsTab, titleTab, closeTab);
            }
            // Las secciones suenan a página (lo decide OptionsPanel), no a click.
            foreach (Button tab in new[] { audioTab, screenTab, controlsTab })
                SetBool(tab.GetComponent<SelectableSounds>(), "playPressSound", false);

            // ----- Página derecha: secciones -----
            RectTransform right = CreatePage("Página derecha (sección)", bookRect, RightPageCenter);
            var rightGroup = right.gameObject.AddComponent<CanvasGroup>();

            levelTitle = null;
            RectTransform home = null;
            if (pause)
            {
                // Portada de la pausa: nombre del nivel (lo pone PauseMenu) y una frase de la panadería.
                home = CreateSection("Nivel", right, titleFont);
                levelTitle = home.Find("Título").GetComponent<TMP_Text>();
                CreateText("Frase", home, "La masa puede esperar un momento.\nEl horno no se va a enfriar.", bodyFont, 34, Cafe,
                    anchor: new Vector2(0.5f, 1f), position: new Vector2(0f, -260f), rectSize: new Vector2(540f, 140f));
                CreateHint(home, bodyFont, "Esc o Start para continuar");
            }

            RectTransform audio = CreateSection("Audio", right, titleFont);
            // Tres volúmenes (general, música y efectos): etiqueta arriba y barra con su porcentaje abajo.
            (Slider volume, TMP_Text volumeValue) = CreateVolumeRow(audio, rounded, bodyFont, "Volumen general", "100%", -180f);
            (Slider music, TMP_Text musicValue) = CreateVolumeRow(audio, rounded, bodyFont, "Música", "80%", -315f);
            (Slider effects, TMP_Text effectsValue) = CreateVolumeRow(audio, rounded, bodyFont, "Efectos", "100%", -450f);
            // Arriba/abajo cambian de volumen; izquierda/derecha ajustan el valor (no hay vecino a los lados).
            Selectable[] audioRows = { volume, music, effects };
            for (int i = 0; i < audioRows.Length; i++)
            {
                audioRows[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? audioRows[i - 1] : null,
                    selectOnDown = i < audioRows.Length - 1 ? audioRows[i + 1] : null,
                };
            }
            CreateHint(audio, bodyFont, "Arriba / abajo para elegir   ·   izquierda / derecha para ajustar");

            RectTransform screen = CreateSection("Pantalla", right, titleFont);
            // Una fila por ajuste: etiqueta a la izquierda (a ella apunta la manícula) y el control a la derecha.
            OptionSelector windowMode = CreateSelector("Selector modo de ventana", screen, rounded, bodyFont, ScreenRowY(0));
            AddScreenLabel(screen, bodyFont, "Modo de ventana", 0, windowMode);
            Slider brightness = CreateSlider(screen, rounded, new Vector2(130f, ScreenRowY(1)));
            brightness.gameObject.name = "Slider brillo";
            ((RectTransform)brightness.transform).sizeDelta = new Vector2(190f, 44f);
            TMP_Text brightnessValue = CreateText("Valor brillo", screen, "50%", bodyFont, 32, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(275f, ScreenRowY(1)), rectSize: new Vector2(80f, 60f), alignment: TextAlignmentOptions.Left);
            AddScreenLabel(screen, bodyFont, "Brillo", 1, brightness);
            OptionSelector frameRate = CreateSelector("Selector límite de FPS", screen, rounded, bodyFont, ScreenRowY(2));
            AddScreenLabel(screen, bodyFont, "Límite de FPS", 2, frameRate);
            Toggle showFps = CreateToggle(screen, rounded, new Vector2(63f, ScreenRowY(3)));
            showFps.gameObject.name = "Toggle mostrar FPS";
            AddScreenLabel(screen, bodyFont, "Mostrar FPS", 3, showFps);
            // Arriba / abajo recorren las filas; izquierda / derecha cambian el valor (selectores y brillo).
            Selectable[] screenRows = { windowMode, brightness, frameRate, showFps };
            for (int i = 0; i < screenRows.Length; i++)
            {
                screenRows[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? screenRows[i - 1] : null,
                    selectOnDown = i < screenRows.Length - 1 ? screenRows[i + 1] : null,
                };
            }
            CreateHint(screen, bodyFont, "Arriba / abajo para elegir   ·   izquierda / derecha para cambiar");

            RectTransform controls = CreateSection("Controles", right, titleFont);
            Toggle vibration = CreateControlsPage(controls, bodyFont, rounded);

            // ----- Listón (sección abierta), manícula (opción seleccionada) y ayuda de botones -----
            SectionRibbon ribbon = CreateRibbon(bookRect);
            CreatePointer(root, bookRect);
            CreatePromptBar(bookRect, bodyFont);

            // ----- Hoja que da vuelta (encima de todo lo demás del libro) -----
            var sheet = new GameObject("Hoja que da vuelta", typeof(RectTransform), typeof(RawImage));
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.SetParent(bookRect, false);
            Stretch(sheetRect);
            var sheetImage = sheet.GetComponent<RawImage>();
            sheetImage.material = UiAssetsBuilder.EnsureLumaKeyMaterial();
            sheetImage.raycastTarget = false;
            var flipper = sheet.AddComponent<PageFlipper>();
            sheet.GetComponent<VideoPlayer>().clip = pageClip;
            SetField(flipper, "display", sheetImage);
            if (pageClip == null)
                Debug.LogWarning($"[Odisea] No está animacion-menu en {SourceFolder}; las secciones cambiarán sin animación.");

            var options = root.AddComponent<OptionsPanel>();
            SetField(options, "book", bookRect);
            SetField(options, "panelGroup", root.GetComponent<CanvasGroup>());
            SetField(options, "pageFlipper", flipper);
            SetField(options, "rightPage", rightGroup);
            SetField(options, "ribbon", ribbon);
            SetField(options, "audioTab", audioTab);
            SetField(options, "screenTab", screenTab);
            SetField(options, "controlsTab", controlsTab);
            SetField(options, "backButton", closeTab);
            SetField(options, "titleButton", titleTab);
            if (pause)
            {
                SetField(options, "restartButton", restartTab);
                SetField(options, "mapButton", mapTab);
                SetField(options, "exitToMenuButton", exitTab);
                SetField(options, "homeSection", home.gameObject);
                SetBool(options, "closeWithStart", true);
            }
            SetField(options, "audioSection", audio.gameObject);
            SetField(options, "screenSection", screen.gameObject);
            SetField(options, "controlsSection", controls.gameObject);
            SetField(options, "volumeSlider", volume);
            SetField(options, "volumeValue", volumeValue);
            SetField(options, "musicSlider", music);
            SetField(options, "musicValue", musicValue);
            SetField(options, "effectsSlider", effects);
            SetField(options, "effectsValue", effectsValue);
            SetField(options, "windowModeSelector", windowMode);
            SetField(options, "brightnessSlider", brightness);
            SetField(options, "brightnessValue", brightnessValue);
            SetField(options, "frameRateSelector", frameRate);
            SetField(options, "showFpsToggle", showFps);
            SetField(options, "vibrationToggle", vibration);
            return options;
        }

        internal const string UiFolder = "Assets/_Project/Art/UI";

        /// <summary>
        /// Listón que cuelga del borde de arriba del libro, junto al margen izquierdo, y baja hasta la sección abierta.
        /// 9-slice: los bordes de arriba (dobladillo) y de abajo (corte en V) no se estiran.
        /// </summary>
        private static SectionRibbon CreateRibbon(RectTransform book)
        {
            const float width = 48f;
            Sprite sprite = ImportRibbonSprite();
            var go = new GameObject("Listón", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(book, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(-760f, 470f);
            rect.sizeDelta = new Vector2(width, 70f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = sprite.rect.width / width;
            }
            else
            {
                image.color = new Color(0.45f, 0.12f, 0.14f);
                Debug.LogWarning($"[Odisea] No está liston en {SourceFolder}; se usa un listón liso.");
            }

            var ribbon = go.AddComponent<SectionRibbon>();
            SetField(ribbon, "ribbon", rect);
            return ribbon;
        }

        private static void CreatePointer(GameObject root, RectTransform book)
        {
            var go = new GameObject("Manícula", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(book, false);
            rect.sizeDelta = new Vector2(88f, 55f);
            var image = go.GetComponent<Image>();
            image.sprite = LoadUiSprite($"{UiFolder}/manicula.png");
            image.preserveAspect = true;
            image.raycastTarget = false;
            go.GetComponent<CanvasGroup>().alpha = 0f;

            var pointer = root.AddComponent<SelectionPointer>();
            SetField(pointer, "pointer", rect);
            SetField(pointer, "area", book);
        }

        /// <summary>Ayuda de botones en la esquina inferior derecha del libro (íconos de Kenney, CC0, teñidos de café).</summary>
        private static void CreatePromptBar(RectTransform book, TMP_FontAsset font)
        {
            var go = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(book, false);
            rect.anchoredPosition = new Vector2(RightPageCenter.x, -335f);
            rect.sizeDelta = new Vector2(640f, 60f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Image confirm = CreatePromptIcon(rect);
            CreatePromptLabel(rect, font, "Seleccionar");
            CreatePromptSpacer(rect);
            Image back = CreatePromptIcon(rect);
            CreatePromptLabel(rect, font, "Atrás");
            CreatePromptSpacer(rect);
            Image left = CreatePromptIcon(rect);
            Image right = CreatePromptIcon(rect);
            CreatePromptLabel(rect, font, "Página");

            var bar = go.AddComponent<ButtonPromptBar>();
            SetField(bar, "confirmIcon", confirm);
            SetField(bar, "backIcon", back);
            SetField(bar, "pageLeftIcon", left);
            SetField(bar, "pageRightIcon", right);
            SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_lb", "xbox_rb");
            SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_l1", "ps_r1");
        }

        /// <summary>Pie del menú principal: nombre y versión a la izquierda (tenue) y "Seleccionar" a la derecha.</summary>
        private static GameObject CreateMenuFooter(Transform canvas, TMP_FontAsset font)
        {
            var footer = new GameObject("Pie", typeof(RectTransform));
            var rect = (RectTransform)footer.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 110f);

            Color faded = new(Crema.r, Crema.g, Crema.b, 0.45f);
            TMP_Text version = CreateText("Nombre y versión", rect, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikRegular), 26, faded,
                anchor: Vector2.zero, rectSize: new Vector2(700f, 50f), alignment: TextAlignmentOptions.BottomLeft);
            version.rectTransform.pivot = Vector2.zero;
            version.rectTransform.anchoredPosition = new Vector2(40f, 36f);
            version.gameObject.AddComponent<VersionLabel>();

            var prompt = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var promptRect = (RectTransform)prompt.transform;
            promptRect.SetParent(rect, false);
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(1f, 0f);
            promptRect.anchoredPosition = new Vector2(-40f, 28f);
            promptRect.sizeDelta = new Vector2(300f, 60f);
            var layout = prompt.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Image icon = CreatePromptIcon(promptRect);
            icon.color = new Color(Crema.r, Crema.g, Crema.b, 0.8f);
            TMP_Text label = CreatePromptLabel(promptRect, font, "Seleccionar");
            label.color = new Color(Crema.r, Crema.g, Crema.b, 0.8f);

            var bar = prompt.AddComponent<ButtonPromptBar>();
            SetField(bar, "confirmIcon", icon);
            SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_lb", "xbox_rb");
            SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_l1", "ps_r1");
            return footer;
        }

        internal static Image CreatePromptIcon(RectTransform bar)
        {
            var go = new GameObject("Ícono", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(bar, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = element.preferredHeight = 50f;
            var image = go.GetComponent<Image>();
            image.color = new Color(Cafe.r, Cafe.g, Cafe.b, 0.85f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        internal static TMP_Text CreatePromptLabel(RectTransform bar, TMP_FontAsset font, string text)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(bar, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font;
            label.fontSize = 28;
            label.color = Cafe;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        internal static void CreatePromptSpacer(RectTransform bar)
        {
            var go = new GameObject("Separación", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(bar, false);
            go.GetComponent<LayoutElement>().preferredWidth = 18f;
        }

        internal static void SetPromptSet(ButtonPromptBar bar, string field, string confirm, string back, string left, string right)
        {
            var serialized = new SerializedObject(bar);
            SerializedProperty set = serialized.FindProperty(field);
            set.FindPropertyRelative("confirm").objectReferenceValue = LoadUiSprite($"{UiFolder}/Botones/{confirm}.png");
            set.FindPropertyRelative("back").objectReferenceValue = LoadUiSprite($"{UiFolder}/Botones/{back}.png");
            set.FindPropertyRelative("pageLeft").objectReferenceValue = LoadUiSprite($"{UiFolder}/Botones/{left}.png");
            set.FindPropertyRelative("pageRight").objectReferenceValue = LoadUiSprite($"{UiFolder}/Botones/{right}.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Carga un PNG del proyecto como sprite de UI, configurando su importación la primera vez.</summary>
        internal static Sprite LoadUiSprite(string path, Vector4? border = null)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Odisea] No se encontró {path}.");
                return null;
            }

            // Unity a veces importa los PNG como "Multiple" y los parte en pedazos; aquí siempre es un solo sprite.
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           (border.HasValue && importer.spriteBorder != border.Value);
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                if (border.HasValue)
                {
                    importer.spriteBorder = border.Value;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                }
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Listón: se copia de Docs/Referencias/Menu y se marcan como bordes el dobladillo y la punta en V.</summary>
        private static Sprite ImportRibbonSprite()
        {
            var texture = ImportUserFile<Texture2D>("liston", ImageExtensions, UiArtFolder, "Liston");
            if (texture == null)
                return null;
            // Imagen de 200×1000: arriba 140 px (vacío + dobladillo) y abajo 200 px (corte en V) no se estiran.
            return LoadUiSprite(AssetDatabase.GetAssetPath(texture), new Vector4(0f, 200f, 0f, 140f));
        }

        private static RectTransform CreatePage(string name, RectTransform book, Vector2 center)
        {
            var page = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)page.transform;
            rect.SetParent(book, false);
            rect.anchoredPosition = center;
            rect.sizeDelta = PageSize;
            return rect;
        }

        /// <summary>Sección de la página derecha: título y línea; el contenido se agrega encima.</summary>
        private static RectTransform CreateSection(string title, RectTransform page, TMP_FontAsset titleFont)
        {
            var section = new GameObject($"Sección {title}", typeof(RectTransform));
            var rect = (RectTransform)section.transform;
            rect.SetParent(page, false);
            Stretch(rect);
            CreateText("Título", rect, title, titleFont, 68, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -70f), rectSize: new Vector2(560f, 100f));
            CreateDivider(rect, -125f);
            return rect;
        }

        private static void CreateDivider(RectTransform parent, float y)
        {
            Image line = CreateImage("Línea", parent, null, new Color(Cafe.r, Cafe.g, Cafe.b, 0.45f), Vector2.zero, new Vector2(300f, 3f), stretch: false);
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            line.rectTransform.anchoredPosition = new Vector2(0f, y);
        }

        // ---------- Sección Controles: dibujo del mando o tabla de teclas ----------

        // Dibujo del mando en la página (centro y tamaño, con la proporción de los PNG recortados de 1302 × 901).
        // Más chico que la página para dejar abajo la fila de Vibración.
        private static readonly Vector2 PadCenter = new(0f, -362f);
        private static readonly Vector2 PadSize = new(340f, 235f);
        // Altura a la que llegan las líneas: arriba del dibujo y debajo de él.
        private const float CalloutTop = -215f;
        private const float CalloutBottom = -505f;
        private const float VibrationRowY = -625f;
        private const string DoubleJumpNote = "\n<size=70%><color=#73594A>2 veces: doble salto</color></size>";

        /// <summary>Una acción del mando: su texto, en qué x va, si va arriba o abajo y a qué botón(es) apunta
        /// (posición normalizada dentro del dibujo, 0,0 = abajo-izquierda).</summary>
        private readonly struct Callout
        {
            public readonly string Label;
            public readonly float X;
            public readonly bool Top;
            public readonly Vector2[] Targets;

            public Callout(string label, float x, bool top, params Vector2[] targets)
            {
                Label = label;
                X = x;
                Top = top;
                Targets = targets;
            }
        }

        /// <summary>Arma la página y devuelve la casilla de Vibración (solo visible con mando).</summary>
        private static Toggle CreateControlsPage(RectTransform section, TMP_FontAsset font, Sprite rounded)
        {
            GameObject keyboard = CreateKeyboardTable(section, font);
            GameObject xbox = CreatePadDiagram("Mando Xbox", section, font, rounded, $"{UiFolder}/Mandos/MandoXbox.png",
                new Callout("Pausa", -165f, true, new Vector2(0.572f, 0.693f)),
                new Callout("Golpear", 10f, true, new Vector2(0.684f, 0.687f)),
                new Callout("Lanzar masa", 195f, true, new Vector2(0.757f, 0.784f)),
                new Callout("Moverse / escaleras", -150f, false, new Vector2(0.256f, 0.711f), new Vector2(0.382f, 0.462f)),
                new Callout("Saltar" + DoubleJumpNote, 175f, false, new Vector2(0.757f, 0.595f)));
            GameObject playStation = CreatePadDiagram("Mando PlayStation", section, font, rounded, $"{UiFolder}/Mandos/MandoPlayStation.png",
                new Callout("Golpear", -150f, true, new Vector2(0.707f, 0.693f)),
                new Callout("Pausa", 20f, true, new Vector2(0.700f, 0.839f)),
                new Callout("Lanzar masa", 195f, true, new Vector2(0.776f, 0.791f)),
                new Callout("Moverse / escaleras", -150f, false, new Vector2(0.230f, 0.689f), new Vector2(0.367f, 0.495f)),
                new Callout("Saltar" + DoubleJumpNote, 175f, false, new Vector2(0.776f, 0.589f)));
            xbox.SetActive(false);
            playStation.SetActive(false);

            var diagram = section.gameObject.AddComponent<ControlsDiagram>();
            SetField(diagram, "keyboard", keyboard);
            SetField(diagram, "xbox", xbox);
            SetField(diagram, "playStation", playStation);

            // Fila "Vibración ☐" centrada debajo del dibujo; la manícula apunta al texto.
            var row = new GameObject("Fila vibración", typeof(RectTransform));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(section, false);
            Stretch(rowRect);
            TMP_Text label = CreateText("Etiqueta vibración", rowRect, "Vibración", font, 34, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(-70f, VibrationRowY), rectSize: new Vector2(200f, 60f), alignment: TextAlignmentOptions.Right);
            Toggle vibration = CreateToggle(rowRect, rounded, new Vector2(70f, VibrationRowY));
            vibration.gameObject.name = "Toggle vibración";
            ((RectTransform)vibration.transform).sizeDelta = new Vector2(52f, 52f);
            vibration.navigation = new Navigation { mode = Navigation.Mode.None };
            SetField(vibration.gameObject.AddComponent<PointerTarget>(), "target", label.rectTransform);
            SetField(diagram, "gamepadOnly", row);
            return vibration;
        }

        private static GameObject CreatePadDiagram(string name, RectTransform section, TMP_FontAsset font, Sprite rounded,
            string spritePath, params Callout[] callouts)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(section, false);
            Stretch(rect);

            Image pad = CreateImage("Dibujo", rect, LoadUiSprite(spritePath), Color.white, Vector2.zero, PadSize, stretch: false);
            pad.rectTransform.anchorMin = pad.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            pad.rectTransform.anchoredPosition = PadCenter;
            pad.preserveAspect = true;

            // Primero todas las líneas, luego los puntos y al final los textos, para que nada tape a un texto.
            var points = new System.Collections.Generic.List<Vector2>();
            foreach (Callout callout in callouts)
            {
                var end = new Vector2(callout.X, callout.Top ? CalloutTop : CalloutBottom);
                foreach (Vector2 target in callout.Targets)
                {
                    Vector2 point = PadCenter + Vector2.Scale(target - new Vector2(0.5f, 0.5f), PadSize);
                    CreateCalloutLine(rect, point, end);
                    points.Add(point);
                }
            }
            foreach (Vector2 point in points)
            {
                CreateDot(rect, rounded, point, 22f, Crema);
                CreateDot(rect, rounded, point, 13f, Cafe);
            }
            foreach (Callout callout in callouts)
            {
                TMP_Text label = CreateText(callout.Label.Split('\n')[0], rect, $"<b>{callout.Label}</b>", font, 28, Cafe,
                    anchor: new Vector2(0.5f, 1f), rectSize: new Vector2(270f, 80f),
                    alignment: callout.Top ? TextAlignmentOptions.Bottom : TextAlignmentOptions.Top);
                label.rectTransform.pivot = new Vector2(0.5f, callout.Top ? 0f : 1f);
                label.rectTransform.anchoredPosition = new Vector2(callout.X, callout.Top ? CalloutTop + 4f : CalloutBottom - 4f);
            }
            return root;
        }

        private static void CreateCalloutLine(RectTransform parent, Vector2 from, Vector2 to)
        {
            Image line = CreateImage("Línea", parent, null, new Color(Cafe.r, Cafe.g, Cafe.b, 0.75f), Vector2.zero, Vector2.zero, stretch: false);
            RectTransform rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            Vector2 delta = to - from;
            rect.anchoredPosition = from;
            rect.sizeDelta = new Vector2(delta.magnitude, 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void CreateDot(RectTransform parent, Sprite rounded, Vector2 position, float size, Color color)
        {
            Image dot = CreateImage("Punto", parent, rounded, color, Vector2.zero, new Vector2(size, size), stretch: false);
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            dot.rectTransform.anchoredPosition = position;
            // Esquinas enormes que Unity recorta al tamaño del punto: queda un círculo.
            dot.pixelsPerUnitMultiplier = 0.05f;
        }

        /// <summary>Teclado y mouse: una fila por acción con las teclas dibujadas.</summary>
        private static GameObject CreateKeyboardTable(RectTransform section, TMP_FontAsset font)
        {
            var root = new GameObject("Teclado y mouse", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(section, false);
            Stretch(rect);

            (string label, string[] keys)[] rows =
            {
                ("Moverse / escaleras", new[] { "kb_w", "kb_a", "kb_s", "kb_d", "o", "kb_arrows" }),
                ("Saltar" + DoubleJumpNote, new[] { "kb_space" }),
                ("Golpear", new[] { "kb_j", "o", "mouse_left" }),
                ("Lanzar masa", new[] { "kb_k", "o", "mouse_right" }),
                ("Pausa", new[] { "kb_esc" }),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = -205f - i * 88f;
                CreateText(rows[i].label.Split('\n')[0], rect, $"<b>{rows[i].label}</b>", font, 30, Cafe,
                    anchor: new Vector2(0.5f, 1f), position: new Vector2(-148f, y), rectSize: new Vector2(265f, 80f),
                    alignment: TextAlignmentOptions.Right);

                var keys = new GameObject("Teclas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                var keysRect = (RectTransform)keys.transform;
                keysRect.SetParent(rect, false);
                keysRect.anchorMin = keysRect.anchorMax = new Vector2(0.5f, 1f);
                keysRect.pivot = new Vector2(0f, 0.5f);
                keysRect.anchoredPosition = new Vector2(5f, y);
                keysRect.sizeDelta = new Vector2(290f, 60f);
                var layout = keys.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 2f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                foreach (string key in rows[i].keys)
                {
                    if (key == "o")
                    {
                        CreatePromptLabel(keysRect, font, " o ");
                        continue;
                    }
                    Image icon = CreatePromptIcon(keysRect);
                    icon.sprite = LoadUiSprite($"{UiFolder}/Botones/{key}.png");
                    var element = icon.GetComponent<LayoutElement>();
                    element.preferredWidth = element.preferredHeight = 54f;
                }
            }
            return root;
        }

        private static void CreateHint(RectTransform section, TMP_FontAsset font, string text)
        {
            CreateText("Ayuda", section, text, font, 26, TintaClara, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -590f), rectSize: new Vector2(560f, 50f));
        }

        /// <summary>Opción del índice: mismo estilo que el menú principal, pero en tinta café sobre el papel.</summary>
        private static Button CreateIndexItem(string label, RectTransform page, TMP_FontAsset font, float y, float fontSize = 52f)
        {
            Button item = CreateTextMenuItem(label, page, font, fontSize: fontSize, size: new Vector2(520f, fontSize + 20f), ghostAlpha: 0.3f);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            var feedback = item.GetComponent<TextMenuItemFeedback>();
            SetColor(feedback, "normalColor", TintaClara);
            SetColor(feedback, "selectedColor", Cafe);
            SetField(item.gameObject.AddComponent<PointerTarget>(), "target", item.transform.Find("Texto"));
            return item;
        }

        /// <summary>Navegación explícita arriba/abajo en el índice, dando la vuelta de la última a la primera.</summary>
        private static void LinkVertically(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length],
                };
            }
        }

        // ---------- Piezas de UI ----------

        /// <summary>Botón del design system: sombra café desplazada, borde café y relleno plátano.</summary>
        internal static Button CreateMenuButton(string label, RectTransform parent, Sprite rounded, TMP_FontAsset font, Vector2 size, float fontSize)
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
            // Lo que recibe el clic del mouse (CreateImage los deja sin raycast).
            fill.raycastTarget = true;
            CreateText("Texto", faceRect, label, font, fontSize, Cafe, stretch: true);

            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            var feedback = root.AddComponent<MenuButtonFeedback>();
            SetField(feedback, "face", faceRect);
            SetField(feedback, "fill", fill);
            root.AddComponent<SelectableSounds>();
            return button;
        }

        /// <summary>
        /// Opción de menú solo de texto (estilo Cuphead): dos copias del texto en rojo y cian detrás,
        /// desplazadas, que se encienden al seleccionar.
        /// </summary>
        private static Button CreateTextMenuItem(string label, RectTransform parent, TMP_FontAsset font,
            float fontSize = 62f, Vector2? size = null, float ghostAlpha = 0.55f)
        {
            var root = new GameObject($"Botón {label}", typeof(RectTransform), typeof(Button));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size ?? new Vector2(560f, 80f);

            TMP_Text red = CreateMenuLabel("Aberración roja", rect, label, font, fontSize, new Vector2(3f, 0f), new Color(1f, 0.22f, 0.22f, ghostAlpha));
            TMP_Text cyan = CreateMenuLabel("Aberración cian", rect, label, font, fontSize, new Vector2(-3f, 0f), new Color(0.2f, 0.85f, 1f, ghostAlpha));
            TMP_Text main = CreateMenuLabel("Texto", rect, label, font, fontSize, Vector2.zero, Color.white);

            // Zona clicable invisible del tamaño de la opción (el texto no recibe clics).
            var hitArea = root.AddComponent<Image>();
            hitArea.color = Color.clear;

            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hitArea;
            button.navigation = new Navigation { mode = Navigation.Mode.Vertical };

            var feedback = root.AddComponent<TextMenuItemFeedback>();
            SetField(feedback, "label", main);
            var serialized = new SerializedObject(feedback);
            SerializedProperty ghosts = serialized.FindProperty("ghosts");
            ghosts.arraySize = 2;
            ghosts.GetArrayElementAtIndex(0).objectReferenceValue = red;
            ghosts.GetArrayElementAtIndex(1).objectReferenceValue = cyan;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<SelectableSounds>();
            return button;
        }

        private static TMP_Text CreateMenuLabel(string name, RectTransform parent, string label, TMP_FontAsset font, float fontSize,
            Vector2 offset, Color color)
        {
            TMP_Text text = CreateText(name, parent, label, font, fontSize, color, stretch: true);
            text.rectTransform.anchoredPosition = offset;
            text.characterSpacing = 4f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>Tarjeta con sombra desplazada y borde café.</summary>
        internal static RectTransform CreateCard(string name, RectTransform parent, Sprite rounded, Vector2 size, Color color)
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
            root.AddComponent<SelectableSounds>();
            // El Slider estira la perilla a toda la altura (anclas y de 0 a 1); con alto extra 0 mide 44 × 44 y queda circular.
            handle.rectTransform.sizeDelta = new Vector2(44f, 0f);
            return slider;
        }

        private static (Slider, TMP_Text) CreateVolumeRow(RectTransform section, Sprite rounded, TMP_FontAsset font, string label,
            string value, float y)
        {
            CreateText($"Etiqueta {label}", section, label, font, 40, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(0f, y), rectSize: new Vector2(540f, 60f));
            Slider slider = CreateSlider(section, rounded, new Vector2(-50f, y - 65f));
            slider.gameObject.name = $"Slider {label}";
            TMP_Text text = CreateText($"Valor {label}", section, value, font, 36, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(225f, y - 65f), rectSize: new Vector2(120f, 60f), alignment: TextAlignmentOptions.Left);
            return (slider, text);
        }

        // ---------- Sección Pantalla ----------

        private static float ScreenRowY(int row) => -195f - row * 78f;

        private static void AddScreenLabel(RectTransform section, TMP_FontAsset font, string text, int row, Selectable control)
        {
            TMP_Text label = CreateText($"Etiqueta {text}", section, text, font, 34, Cafe, anchor: new Vector2(0.5f, 1f),
                position: new Vector2(-110f, ScreenRowY(row)), rectSize: new Vector2(250f, 60f),
                alignment: TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 34f;
            SetField(control.gameObject.AddComponent<PointerTarget>(), "target", label.rectTransform);
        }

        /// <summary>Selector "‹ Valor ›" sobre una píldora beige; las opciones las pone OptionsPanel.</summary>
        private static OptionSelector CreateSelector(string name, RectTransform parent, Sprite rounded, TMP_FontAsset font, float y)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(155f, y);
            rect.sizeDelta = new Vector2(240f, 54f);
            var background = root.GetComponent<Image>();
            background.sprite = rounded;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1.5f;

            TMP_Text value = CreateText("Valor", rect, "", font, 30, Cafe, stretch: true);
            value.margin = new Vector4(36f, 0f, 36f, 0f);
            value.enableAutoSizing = true;
            value.fontSizeMin = 22f;
            value.fontSizeMax = 30f;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_FontAsset arrowFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            TMP_Text left = CreateText("Flecha izquierda", rect, "‹", arrowFont, 44, Cafe, anchor: new Vector2(0f, 0.5f),
                position: new Vector2(20f, 3f), rectSize: new Vector2(30f, 54f));
            TMP_Text right = CreateText("Flecha derecha", rect, "›", arrowFont, 44, Cafe, anchor: new Vector2(1f, 0.5f),
                position: new Vector2(-20f, 3f), rectSize: new Vector2(30f, 54f));

            var selector = root.AddComponent<OptionSelector>();
            selector.targetGraphic = background;
            selector.colors = SelectionColors(normal: BeigeMasa);
            SetField(selector, "valueText", value);
            SetField(selector, "leftArrow", left.rectTransform);
            SetField(selector, "rightArrow", right.rectTransform);
            root.AddComponent<SelectableSounds>();
            return selector;
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
            root.AddComponent<SelectableSounds>();
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

        internal static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 position,
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

        internal static TMP_Text CreateText(string name, Transform parent, string content, TMP_FontAsset font, float fontSize,
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

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ---------- Escena, cámara, canvas ----------

        internal static UnityEngine.SceneManagement.Scene NewScene(string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            return scene;
        }

        internal static Camera CreateCamera(Color background)
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

        internal static Transform CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        internal static GameObject CreateEventSystem()
        {
            // Teclado, mouse y mando navegan la UI con el nuevo Input System.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return go;
        }

        internal static GameObject CreateUiAudio()
        {
            var go = new GameObject("Sonidos de UI", typeof(AudioSource));
            var audio = go.AddComponent<UiAudio>();
            SetField(audio, "hover", UiAssetsBuilder.LoadUiSound("ui_hover"));
            SetField(audio, "click", UiAssetsBuilder.LoadUiSound("ui_click"));
            SetField(audio, "back", UiAssetsBuilder.LoadUiSound("ui_back"));
            SetField(audio, "page", UiAssetsBuilder.LoadUiSound(PageSoundName)); // Opcional.
            return go;
        }

        private static void SetBool(UnityEngine.Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(UnityEngine.Object target, string field, Color value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal const string MenuMusicPath = "Assets/_Project/Audio/Music/musica_menu";

        /// <summary>
        /// Música del menú principal (Audio/Music/musica_menu, cualquier formato de audio). Es la misma en el menú,
        /// las partidas, las historietas y el mapa: pasa entre ellas sin cortarse y solo cambia al entrar a un nivel.
        /// </summary>
        internal const string CreditsMusicPath = "Assets/_Project/Audio/Music/musica_creditos";

        internal static void CreateMenuMusic()
        {
            SceneMusic music = GameSoundsBuilder.CreateMusic("Música del menú", MenuMusicPath);
            if (music == null)
                return;
            var serialized = new SerializedObject(music);
            SerializedProperty scenes = serialized.FindProperty("continueInto");
            // Sigue sin cortarse en partidas (y en créditos si no tienen canción propia); la historia y el mapa tienen la
            // suya.
            string[] names = GameSoundsBuilder.LoadMusic(CreditsMusicPath) != null
                ? new[] { SceneFlow.MainMenu, SceneFlow.Slots }
                : new[] { SceneFlow.MainMenu, SceneFlow.Slots, SceneFlow.Credits };
            scenes.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                scenes.GetArrayElementAtIndex(i).stringValue = names[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBuildScenes()
        {
            // Los niveles se agregan si ya se generaron (el tutorial lo crea Odisea > Niveles > Crear tutorial).
            EditorBuildSettings.scenes = new[]
                {
                    SplashScenePath, TitleScenePath, MenuScenePath, SlotSelectBuilder.ScenePath, StoryBuilder.ScenePath,
                    MapBuilder.ScenePath, BakeryBuilder.ScenePath, CreditsBuilder.ScenePath,
                    TutorialBuilder.ScenePath, MercadoBuilder.ScenePath, SandboxScenePath,
                }
                .Where(File.Exists)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        /// <summary>Agrega una escena a Build Settings si no está (la generan los otros constructores).</summary>
        internal static void AddToBuild(string path)
        {
            if (EditorBuildSettings.scenes.Any(scene => scene.path == path))
                return;
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(path, true)).ToArray();
        }

        // ---------- Archivos del usuario ----------

        /// <summary>Copia un archivo de Docs/Referencias/Menu (buscado por nombre, sin extensión) al proyecto.</summary>
        private static T ImportUserFile<T>(string sourceName, string[] extensions, string destinationFolder, string destinationName)
            where T : UnityEngine.Object
        {
            string source = Directory.Exists(SourceFolder)
                ? Directory.GetFiles(SourceFolder).FirstOrDefault(f =>
                    string.Equals(Path.GetFileNameWithoutExtension(f), sourceName, StringComparison.OrdinalIgnoreCase) &&
                    extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
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

        private static Sprite ImportSprite(string sourceName, string destinationName, int maxSize = 4096)
        {
            var texture = ImportUserFile<Texture2D>(sourceName, ImageExtensions, UiArtFolder, destinationName);
            if (texture == null)
                return null;

            string path = AssetDatabase.GetAssetPath(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != maxSize)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Sprite que ya vive dentro de Assets (no se copia de Docs).
        private static Sprite LoadLocalSprite(string path, int maxSize)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;
            if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != maxSize)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static string AssetPathOf(UnityEngine.Object asset) => asset != null ? AssetDatabase.GetAssetPath(asset) : null;

        internal static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value)
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
