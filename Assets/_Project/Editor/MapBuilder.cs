using System.IO;
using System.Linq;
using Odisea.Core;
using Odisea.Map;
using Odisea.Player;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Mapa de San Blas, de lado (como el tutorial): Ulises camina por la costa con el mismo control de los niveles
    /// y los cuatro lugares están en fila: Panadería de Don Lupe (tutorial), El Mercado, El Horno y La Tovara.
    /// Cada uno tiene su dibujo (Art/Map/Lugares/&lt;Lugar&gt;.png, o un edificio de colores si falta), su letrero y su
    /// puerta: al pararse enfrente sale la ventanita del lugar y con arriba se entra.
    /// </summary>
    public static class MapBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mapa.unity";
        private const string PlacesArtFolder = "Assets/_Project/Art/Map/Lugares";
        // Copias recortadas a lo dibujado (los PNG vienen con mucho margen transparente).
        private const string TrimmedFolder = "Assets/_Project/Art/Map/Recortes";
        private const string MapMusicPath = "Assets/_Project/Audio/Music/musica_mapa";

        private const float LeftEdge = -12f;
        private const float RightEdge = 104f;
        // Ancho del dibujo de cada lugar y cuánto se hunde su base tras la orilla de la arena.
        private const float PlaceArtWidth = 8f;
        private const float PlaceArtSink = 0.6f;
        // El letrero va a la izquierda de cada lugar; la puerta (zona de entrada) frente al edificio.
        private const float SignOffset = -6.5f;
        private static readonly Vector2 DoorSize = new(5f, 3f);
        private const int FlagOrder = 30;

        private readonly struct Place
        {
            public readonly string Id, Name, Subtitle, Scene, Story, Requires, RequiresName, SignTitle, SignHint, ArtName;
            public readonly float X;
            public readonly Color Wall, Roof;

            public Place(string id, string name, string subtitle, string scene, string story, string requires, string requiresName,
                string signTitle, string signHint, float x, Color wall, Color roof)
            {
                Id = id; Name = name; Subtitle = subtitle; Scene = scene; Story = story; Requires = requires;
                RequiresName = requiresName; SignTitle = signTitle; SignHint = signHint; X = x; Wall = wall; Roof = roof;
                ArtName = id == GameProgress.Tutorial ? "Panaderia" : char.ToUpperInvariant(id[0]) + id.Substring(1);
            }
        }

        // En orden de la historia, de izquierda a derecha.
        private static readonly Place[] Places =
        {
            new(GameProgress.Tutorial, "Panadería de Don Lupe", "Aprende el oficio con Don Lupe", SceneFlow.Tutorial, "", "", "",
                "PANADERÍA", "De Don Lupe", 0f, new Color(0.95f, 0.80f, 0.55f), new Color(0.70f, 0.33f, 0.20f)),
            new(GameProgress.Mercado, "El Mercado", "Acto I · La cueva del mercado", "Mercado", "Mercado", GameProgress.Tutorial,
                "el tutorial", "EL MERCADO", "Acto I", 30f, new Color(0.98f, 0.70f, 0.40f), new Color(0.80f, 0.25f, 0.20f)),
            new(GameProgress.Horno, "El Horno", "Acto II · El asedio a la panadería", "Horno", "Horno", GameProgress.Mercado,
                "El Mercado", "EL HORNO", "Acto II", 60f, new Color(0.85f, 0.62f, 0.45f), new Color(0.55f, 0.30f, 0.20f)),
            new(GameProgress.Tovara, "La Tovara", "Acto III · Carrera por los manglares", "Tovara", "Tovara", GameProgress.Horno,
                "El Horno", "LA TOVARA", "Acto III", 90f, new Color(0.55f, 0.70f, 0.45f), new Color(0.30f, 0.45f, 0.28f)),
        };

        [MenuItem("Odisea/Niveles/Crear mapa")]
        public static void Build()
        {
            TutorialBuilder.EnsureCircleSprite();
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Transform root = level.Root;
            root.name = "Mapa";
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            int ground = level.GroundLayer;

            // ----- Suelo y paredes (como el tutorial) -----
            GameObject sand = PlayerSandboxBuilder.CreateBlock("Arena", root, square,
                new Vector2((LeftEdge + RightEdge) / 2f, -3f), new Vector2(RightEdge - LeftEdge, 6f), TutorialBuilder.Sand, ground);
            sand.GetComponent<SpriteRenderer>().sortingOrder = -8;
            PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(LeftEdge - 0.5f, 7f), new Vector2(1f, 20f),
                TutorialBuilder.SandDark, ground);
            PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(RightEdge + 0.5f, 7f), new Vector2(1f, 20f),
                TutorialBuilder.SandDark, ground);

            // ----- Lugares -----
            LocationPanel panel = CreateLocationPanel();
            TMP_FontAsset titleFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            TMP_FontAsset bodyFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            foreach (Place place in Places)
                CreateLocation(place, root, square, circle, level, panel, titleFont, bodyFont);

            // ----- Fondo: el del tutorial (papel, horizonte y arena dibujada) -----
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.PaperPath) != null)
                TutorialBuilder.CreateArtBackground(root, sand.GetComponent<SpriteRenderer>(), LeftEdge, RightEdge);
            else
                TutorialBuilder.CreateBackground(square, circle);

            // ----- Ulises: el mismo de los niveles; aparece frente al último lugar del que salió -----
            string[] extensions = { ".ogg", ".mp3", ".wav" };
            string music = extensions.Any(ext => File.Exists(MapMusicPath + ext)) ? MapMusicPath : MenuScenesBuilder.MenuMusicPath;
            GameObject player = PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(Places[0].X + 5f, 1f), "San Blas", music,
                showControls: false, background: TutorialBuilder.Paper);
            player.AddComponent<MapArrival>();
            // En el mapa no hay peligros: sin vidas ni ventajas en pantalla.
            var hud = GameObject.Find("HUD");
            if (hud != null)
                Object.DestroyImmediate(hud);
            MenuScenesBuilder.SetField(panel, "input", player.GetComponent<PlayerInputReader>());

            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(LeftEdge + 12.5f, 2.5f));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(RightEdge - 12.5f, 12f));

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Mapa creado en {ScenePath}.");
        }

        // Edificio (detrás de Ulises), su letrero de madera a la izquierda, banderita de terminado y la puerta.
        private static void CreateLocation(Place place, Transform root, Sprite square, Sprite circle,
            PlayerSandboxBuilder.LevelContext level, LocationPanel panel, TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            var go = new GameObject(place.Name);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(place.X, 0f, 0f);

            var tinted = new System.Collections.Generic.List<Object>();
            Sprite art = LoadTrimmedSprite(PlacesArtFolder, place.ArtName);
            if (art != null)
            {
                // Su base de tierra queda hundida tras la orilla de la arena, como los letreros.
                var drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(go.transform, false);
                float scale = PlaceArtWidth / art.bounds.size.x;
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, -PlaceArtSink - art.bounds.min.y * scale, 0f);
                drawing.sprite = art;
                drawing.sortingOrder = TutorialBuilder.BehindSandOrder;
                tinted.Add(drawing);
            }
            else
            {
                const int order = TutorialBuilder.BehindSandOrder;
                tinted.Add(Rect("Pared", go.transform, square, new Vector2(0f, 1.7f), new Vector2(5f, 3.4f), place.Wall, order));
                tinted.Add(Rect("Techo", go.transform, square, new Vector2(0f, 3.8f), new Vector2(5.6f, 1.2f), place.Roof, order + 1));
                tinted.Add(Rect("Puerta", go.transform, square, new Vector2(0f, 0.8f), new Vector2(1.2f, 1.6f),
                    PlayerSandboxBuilder.Cafe, order + 1));
            }

            float signX = place.X + SignOffset;
            TutorialBuilder.CreateSign(root, square, titleFont, bodyFont, signX, place.SignTitle,
                TutorialBuilder.Prompt.Text(place.SignHint));

            // Banderita amarilla de "terminado", clavada junto al letrero.
            var flag = new GameObject("Terminado");
            flag.transform.SetParent(go.transform, false);
            flag.transform.position = new Vector3(signX + 2.3f, 0f, 0f);
            Rect("Asta", flag.transform, square, new Vector2(0f, 1f), new Vector2(0.12f, 2f), PlayerSandboxBuilder.Cafe, FlagOrder);
            Rect("Bandera", flag.transform, circle, new Vector2(0.4f, 1.75f), new Vector2(0.8f, 0.55f),
                new Color(0.96f, 0.72f, 0.19f), FlagOrder + 1);

            // Puerta: al pararse enfrente sale la ventanita; ahí aparece Ulises al volver.
            var door = new GameObject("Puerta (entrada)");
            door.transform.SetParent(go.transform, false);
            door.transform.localPosition = new Vector3(0f, DoorSize.y / 2f, 0f);
            door.AddComponent<BoxCollider2D>().size = DoorSize;
            var arrival = new GameObject("Llegada").transform;
            arrival.SetParent(go.transform, false);
            arrival.localPosition = new Vector3(0f, 1f, 0f);

            var location = door.AddComponent<MapLocation>();
            PlayerSandboxBuilder.SetSerialized(location, "id", place.Id);
            PlayerSandboxBuilder.SetSerialized(location, "displayName", place.Name);
            PlayerSandboxBuilder.SetSerialized(location, "subtitle", place.Subtitle);
            PlayerSandboxBuilder.SetSerialized(location, "sceneName", place.Scene);
            PlayerSandboxBuilder.SetSerialized(location, "introStory", place.Story);
            PlayerSandboxBuilder.SetSerialized(location, "requires", place.Requires);
            PlayerSandboxBuilder.SetSerialized(location, "requiresName", place.RequiresName);
            PlayerSandboxBuilder.SetSerialized(location, "playerLayers", 1 << level.PlayerLayer);
            MenuScenesBuilder.SetField(location, "arrival", arrival);
            MenuScenesBuilder.SetField(location, "panel", panel);
            MenuScenesBuilder.SetField(location, "doneMark", flag);
            PlayerSandboxBuilder.SetSerialized(location, "tinted", tinted.ToArray());
        }

        /// <summary>
        /// Sprite de folder/name.png recortado a lo dibujado: los generadores dejan mucho margen transparente y así
        /// el tamaño en el mapa corresponde al dibujo. La copia recortada va en Art/Map/Recortes y se rehace si el
        /// original cambia.
        /// </summary>
        private static Sprite LoadTrimmedSprite(string folder, string name)
        {
            string source = $"{folder}/{name}.png";
            if (!File.Exists(source))
                return null;
            string target = $"{TrimmedFolder}/{Path.GetFileName(folder)}_{name}.png";
            if (!File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(source));
                Color32[] pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height, minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a <= 10)
                        continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < 0)
                {
                    Object.DestroyImmediate(texture);
                    return null;
                }
                int cw = maxX - minX + 1, ch = maxY - minY + 1;
                var cropped = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                cropped.SetPixels(texture.GetPixels(minX, minY, cw, ch));
                cropped.Apply();
                PlayerSandboxBuilder.EnsureFolder(TrimmedFolder);
                File.WriteAllBytes(target, cropped.EncodeToPNG());
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(cropped);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            }
            return MenuScenesBuilder.LoadUiSprite(target);
        }

        // Arriba al centro: nombre del lugar, frase, estado y "Entrar" con arriba (W / stick / cruceta).
        private static LocationPanel CreateLocationPanel()
        {
            Transform canvas = MenuScenesBuilder.CreateCanvas("HUD del mapa");
            var root = new GameObject("Ventanita del lugar", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -170f);
            rootRect.sizeDelta = new Vector2(860f, 250f);

            RectTransform card = MenuScenesBuilder.CreateCard("Tarjeta", rootRect, UiAssetsBuilder.EnsureRoundedSprite(),
                new Vector2(860f, 250f), MenuScenesBuilder.Crema);
            TMP_Text title = MenuScenesBuilder.CreateText("Nombre", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), 56,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, 70f), rectSize: new Vector2(800f, 76f));
            TMP_Text subtitle = MenuScenesBuilder.CreateText("Frase", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 30,
                MenuScenesBuilder.TintaClara, position: new Vector2(0f, 15f), rectSize: new Vector2(800f, 44f));
            TMP_Text status = MenuScenesBuilder.CreateText("Estado", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 30,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, -35f), rectSize: new Vector2(800f, 44f));

            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            var prompt = new GameObject("Entrar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var promptRect = (RectTransform)prompt.transform;
            promptRect.SetParent(card, false);
            promptRect.anchoredPosition = new Vector2(0f, -85f);
            promptRect.sizeDelta = new Vector2(400f, 56f);
            var layout = prompt.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Image icon = MenuScenesBuilder.CreatePromptIcon(promptRect);
            icon.GetComponent<LayoutElement>().preferredWidth = icon.GetComponent<LayoutElement>().preferredHeight = 52f;
            MenuScenesBuilder.CreatePromptLabel(promptRect, font, "Entrar").fontSize = 38;
            var bar = prompt.AddComponent<ButtonPromptBar>();
            MenuScenesBuilder.SetField(bar, "confirmIcon", icon);
            // Se entra con arriba, como a una puerta (A / Espacio es saltar).
            MenuScenesBuilder.SetPromptSet(bar, "keyboard", "kb_w", "kb_esc", "kb_q", "kb_e");
            MenuScenesBuilder.SetPromptSet(bar, "xbox", "xbox_stick_v", "xbox_b", "xbox_lb", "xbox_rb");
            MenuScenesBuilder.SetPromptSet(bar, "playStation", "ps_stick_v", "ps_circle", "ps_l1", "ps_r1");

            var panel = root.AddComponent<LocationPanel>();
            MenuScenesBuilder.SetField(panel, "group", root.GetComponent<CanvasGroup>());
            MenuScenesBuilder.SetField(panel, "card", card);
            MenuScenesBuilder.SetField(panel, "title", title);
            MenuScenesBuilder.SetField(panel, "subtitle", subtitle);
            MenuScenesBuilder.SetField(panel, "status", status);
            MenuScenesBuilder.SetField(panel, "enterPrompt", prompt);
            return panel;
        }

        private static SpriteRenderer Rect(string name, Transform parent, Sprite sprite, Vector2 localPosition, Vector2 size,
            Color color, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
