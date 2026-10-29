using System.IO;
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
    /// Mapa de San Blas visto desde arriba: Ulises camina entre los cuatro lugares (Panadería de Don Lupe = tutorial,
    /// El Mercado, El Horno y La Tovara), unidos por caminos, con el mar abajo y el manglar a la derecha.
    /// Se arma por piezas: Art/Map/Mapa.png es la base (suelo, caminos, mar), Art/Map/Lugares/&lt;Lugar&gt;.png cada
    /// edificio y Art/Map/Detalles/&lt;detalle&gt;.png lo que adorna (palmeras, árboles...). Lo que falte queda en graybox;
    /// los choques y las zonas de entrada no cambian.
    /// </summary>
    public static class MapBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mapa.unity";
        private const string MapArtPath = "Assets/_Project/Art/Map/Mapa.png";
        // Cada lugar con su dibujo (Panaderia.png, Mercado.png, Horno.png, Tovara.png) y los detalles sueltos.
        private const string PlacesArtFolder = "Assets/_Project/Art/Map/Lugares";
        private const string DetailsArtFolder = "Assets/_Project/Art/Map/Detalles";
        // Copias recortadas a lo dibujado (los PNG vienen con mucho margen transparente).
        private const string TrimmedFolder = "Assets/_Project/Art/Map/Recortes";
        // Encima de todo lo del suelo: nombres y banderitas.
        private const int LabelOrder = 2000;
        private const string MapMusicPath = "Assets/_Project/Audio/Music/musica_mapa";

        // El mapa mide Width × Height unidades, centrado en el origen.
        private const float Width = 64f, Height = 36f;
        private const float CameraSize = 7f;
        // El dibujo del mapa trae un marco de papel alrededor: ni Ulises ni la cámara llegan ahí.
        private const float FrameMargin = 1.4f;

        // Orilla del mar del dibujo (de izquierda a derecha): abajo de esta línea es agua.
        private static readonly Vector2[] Coast =
        {
            new(-32f, -7.6f), new(-26f, -7.7f), new(-18f, -8.3f), new(-9f, -9.0f), new(0f, -10.0f),
            new(9f, -11.3f), new(17f, -12.6f), new(24f, -13.8f), new(32f, -14.8f),
        };

        private static readonly Color Sand = new(0.93f, 0.85f, 0.68f);
        private static readonly Color Path = new(0.86f, 0.74f, 0.55f);
        private static readonly Color Grass = new(0.66f, 0.76f, 0.47f);
        private static readonly Color Sea = new(0.48f, 0.72f, 0.80f);
        private static readonly Color Mangrove = new(0.31f, 0.47f, 0.30f);

        private readonly struct Place
        {
            public readonly string Id, Name, Subtitle, Scene, Story, Requires, RequiresName, ArtName;
            public readonly Vector2 Position;
            public readonly Color Wall, Roof;

            public Place(string id, string name, string subtitle, string scene, string story, string requires, string requiresName,
                Vector2 position, Color wall, Color roof)
            {
                Id = id; Name = name; Subtitle = subtitle; Scene = scene; Story = story;
                Requires = requires; RequiresName = requiresName; Position = position; Wall = wall; Roof = roof;
                ArtName = id == GameProgress.Tutorial ? "Panaderia" : char.ToUpperInvariant(id[0]) + id.Substring(1);
            }
        }

        // En orden de la historia; el camino los une en este orden.
        private static readonly Place[] Places =
        {
            // Cada uno en el centro de su claro del dibujo del mapa.
            new(GameProgress.Tutorial, "Panadería de Don Lupe", "Aprende el oficio con Don Lupe", SceneFlow.Tutorial, "", "", "",
                new Vector2(-23.3f, -2.0f), new Color(0.95f, 0.80f, 0.55f), new Color(0.70f, 0.33f, 0.20f)),
            new(GameProgress.Mercado, "El Mercado", "Acto I · La cueva del mercado", "Mercado", "Mercado", GameProgress.Tutorial,
                "el tutorial", new Vector2(-2.8f, 11.1f), new Color(0.98f, 0.70f, 0.40f), new Color(0.80f, 0.25f, 0.20f)),
            new(GameProgress.Horno, "El Horno", "Acto II · El asedio a la panadería", "Horno", "Horno", GameProgress.Mercado,
                "El Mercado", new Vector2(1.8f, -6.5f), new Color(0.85f, 0.62f, 0.45f), new Color(0.55f, 0.30f, 0.20f)),
            new(GameProgress.Tovara, "La Tovara", "Acto III · Carrera por los manglares", "Tovara", "Tovara", GameProgress.Horno,
                "El Horno", new Vector2(23.7f, -0.2f), new Color(0.55f, 0.70f, 0.45f), new Color(0.30f, 0.45f, 0.28f)),
        };

        // Cada lugar ocupa su claro (≈ 7 × 6): el dibujo se apoya abajo del claro, el edificio choca en la parte de
        // atrás y la entrada queda enfrente de la puerta.
        private static readonly Vector2 BuildingSize = new(5f, 3.4f);
        private const float PlaceArtWidth = 6.6f;
        private const float ArtBaseDrop = 2.7f;
        private static readonly Vector2 BuildingCollider = new(4.2f, 2.2f);
        private const float BuildingColliderY = 0.25f;
        private static readonly Vector2 EntranceSize = new(4.6f, 1.8f);

        // Detalles del mapa: nombre del archivo en Art/Map/Detalles, ancho en el mapa y dónde va cada uno (su base).
        // Si falta el dibujo, no se pone (no hay graybox de detalles).
        private static readonly (string file, float width, Vector2[] spots)[] Details =
        {
            ("palmera", 2.6f, new[] { new Vector2(-29f, -11.5f), new Vector2(-15f, -12f), new Vector2(-1f, -11.6f), new Vector2(13f, -12f) }),
            ("arbol", 3.2f, new[] { new Vector2(-27f, 8f), new Vector2(-17f, 12f), new Vector2(2f, 12.5f), new Vector2(-1f, 8f) }),
            ("mangle", 3.6f, new[] { new Vector2(17f, 13f), new Vector2(27f, 12f), new Vector2(28f, 0f), new Vector2(17f, -1f) }),
            ("roca", 1.3f, new[] { new Vector2(-12f, -1f), new Vector2(12f, 1.5f), new Vector2(-25f, 2f) }),
            ("lancha", 3.2f, new[] { new Vector2(-8f, -16.6f), new Vector2(19f, -16.4f) }),
            ("casita", 3.4f, new[] { new Vector2(-14f, -9f), new Vector2(0f, 1f) }),
        };
        // La zona de entrada queda frente a la puerta, en la parte de abajo del claro.
        private const float EntranceDrop = 1.9f;

        [MenuItem("Odisea/Niveles/Crear mapa")]
        public static void Build()
        {
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Transform root = level.Root;
            root.name = "Mapa";
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            Sprite mapArt = LoadMapSprite(MapArtPath, maxSize: 8192);
            bool graybox = mapArt == null;

            // ----- Suelo -----
            if (graybox)
            {
                Rect("Arena", root, square, Vector2.zero, new Vector2(Width, Height), Sand, -30);
                Rect("Pasto (norte)", root, square, new Vector2(-12f, 12f), new Vector2(30f, 9f), Grass, -29);
                Rect("Manglar", root, square, new Vector2(24f, 6f), new Vector2(16f, 22f), Mangrove, -29);
                Rect("Mar", root, square, new Vector2(0f, -Height / 2f + 2.5f), new Vector2(Width, 5f), Sea, -28);
                for (int i = 0; i + 1 < Places.Length; i++)
                    PathBetween(root, square, Entrance(Places[i]), Entrance(Places[i + 1]));
            }
            else
            {
                var art = new GameObject("Dibujo del mapa").AddComponent<SpriteRenderer>();
                art.transform.SetParent(root, false);
                art.sprite = mapArt;
                art.transform.localScale = Vector3.one * (Width / mapArt.bounds.size.x);
                art.sortingOrder = -30;
            }

            // ----- Bordes: la orilla del mar y el marco del dibujo -----
            int ground = level.GroundLayer;
            var coast = new GameObject("Orilla del mar") { layer = ground };
            coast.transform.SetParent(root, false);
            coast.AddComponent<EdgeCollider2D>().points = graybox
                ? new[] { new Vector2(-Width / 2f, -Height / 2f + 5f), new Vector2(Width / 2f, -Height / 2f + 5f) }
                : Coast;
            float inner = FrameMargin;
            Wall("Borde norte", root, new Vector2(0f, Height / 2f - inner + 0.5f), new Vector2(Width + 2f, 1f), ground);
            Wall("Borde oeste", root, new Vector2(-Width / 2f + inner - 0.5f, 0f), new Vector2(1f, Height + 2f), ground);
            Wall("Borde este", root, new Vector2(Width / 2f - inner + 0.5f, 0f), new Vector2(1f, Height + 2f), ground);

            // ----- HUD: la ventanita de cada lugar -----
            LocationPanel panel = CreateLocationPanel();

            // ----- Lugares y detalles -----
            foreach (Place place in Places)
                CreateLocation(place, root, square, circle, level, panel);
            CreateDetails(root);

            // ----- Ulises -----
            // La primera vez aparece en el camino, junto a la panadería (fuera de su zona de entrada).
            MapWalker walker = CreateWalker(level, Entrance(Places[0]) + new Vector2(5.4f, -0.3f));
            PlayerSandboxBuilder.SetSerialized(panel, "walker", walker);

            // ----- Cámara, música y pausa -----
            var camera = GameObject.Find("Main Camera");
            var cam = camera.GetComponent<Camera>();
            cam.orthographicSize = CameraSize;
            cam.backgroundColor = PlayerSandboxBuilder.Cafe;
            camera.transform.position = new Vector3(walker.transform.position.x, walker.transform.position.y, -10f);
            var follow = camera.AddComponent<CameraFollow2D>();
            follow.Target = walker.transform;
            float halfWidth = CameraSize * 16f / 9f;
            PlayerSandboxBuilder.SetSerialized(follow, "offset", new Vector2(0f, 0.5f));
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition",
                new Vector2(-Width / 2f + FrameMargin + halfWidth, -Height / 2f + FrameMargin + CameraSize));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition",
                new Vector2(Width / 2f - FrameMargin - halfWidth, Height / 2f - FrameMargin - CameraSize));

            string music = File.Exists(MapMusicPath + ".mp3") || File.Exists(MapMusicPath + ".ogg") || File.Exists(MapMusicPath + ".wav")
                ? MapMusicPath : MenuScenesBuilder.MenuMusicPath;
            GameSoundsBuilder.CreateMusic("Música del mapa", music);

            var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/MenuPausa.prefab");
            if (pausePrefab != null)
            {
                var pause = (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab);
                PlayerSandboxBuilder.SetSerialized(pause.GetComponent<PauseMenu>(), "levelName", "San Blas");
            }

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Mapa creado en {ScenePath}{(graybox ? " (graybox: falta Art/Map/Mapa.png)" : "")}.");
        }

        private static Vector2 Entrance(Place place) => place.Position + Vector2.down * EntranceDrop;

        // Edificio (choca), letrero con el nombre, banderita de terminado y la zona de entrada frente a la puerta.
        private static void CreateLocation(Place place, Transform root, Sprite square, Sprite circle,
            PlayerSandboxBuilder.LevelContext level, LocationPanel panel)
        {
            var go = new GameObject(place.Name);
            go.transform.SetParent(root, false);
            go.transform.position = place.Position;

            // El edificio se ordena por la línea de su puerta (el frente del choque), como Ulises: si Ulises está más
            // abajo pasa delante; si está detrás del edificio, queda tapado.
            float frontY = BuildingColliderY - BuildingCollider.y / 2f;
            int order = MapWalker.SortingOrder(place.Position.y + frontY);
            var tinted = new System.Collections.Generic.List<Object>();
            float top;
            Sprite art = LoadTrimmedSprite(PlacesArtFolder, place.ArtName);
            if (art != null)
            {
                // Dibujo del lugar apoyado en la parte de abajo de su claro.
                var drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(go.transform, false);
                float scale = PlaceArtWidth / art.bounds.size.x;
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, -ArtBaseDrop - art.bounds.min.y * scale, 0f);
                drawing.sprite = art;
                drawing.sortingOrder = order;
                tinted.Add(drawing);
                top = -ArtBaseDrop + art.bounds.size.y * scale;
            }
            else
            {
                Vector2 center = new(0f, frontY + BuildingSize.y / 2f);
                tinted.Add(Rect("Pared", go.transform, square, center, BuildingSize, place.Wall, order));
                tinted.Add(Rect("Techo", go.transform, square, center + new Vector2(0f, BuildingSize.y / 2f + 0.4f),
                    new Vector2(BuildingSize.x + 0.6f, 1.2f), place.Roof, order + 1));
                tinted.Add(Rect("Puerta", go.transform, square, new Vector2(0f, frontY + 0.8f), new Vector2(1.2f, 1.6f),
                    PlayerSandboxBuilder.Cafe, order + 1));
                top = center.y + BuildingSize.y / 2f + 1f;
            }
            Wall("Choque", go.transform, place.Position + new Vector2(0f, BuildingColliderY), BuildingCollider, level.GroundLayer);

            // Nombre sobre el techo.
            var label = new GameObject("Letrero", typeof(RectTransform), typeof(TextMeshPro));
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, top + 0.6f, 0f);
            ((RectTransform)label.transform).sizeDelta = new Vector2(9f, 1.4f);
            var text = label.GetComponent<TextMeshPro>();
            text.text = place.Name;
            text.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            text.fontSize = 6f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = PlayerSandboxBuilder.Cafe;
            text.sortingOrder = LabelOrder;

            // Banderita amarilla de "terminado".
            var flag = new GameObject("Terminado");
            flag.transform.SetParent(go.transform, false);
            // Al lado del edificio, para no tapar el nombre.
            flag.transform.localPosition = new Vector3(PlaceArtWidth / 2f - 0.2f, frontY - 0.4f, 0f);
            Rect("Asta", flag.transform, square, new Vector2(0f, 0f), new Vector2(0.12f, 1.4f), PlayerSandboxBuilder.Cafe, LabelOrder);
            Rect("Bandera", flag.transform, circle, new Vector2(0.35f, 0.45f), new Vector2(0.7f, 0.5f),
                new Color(0.96f, 0.72f, 0.19f), LabelOrder + 1);

            // Zona de entrada (frente a la puerta) y dónde aparece Ulises al volver.
            var entrance = new GameObject("Entrada");
            entrance.transform.SetParent(go.transform, false);
            entrance.transform.position = Entrance(place);
            entrance.AddComponent<BoxCollider2D>().size = EntranceSize;
            var location = entrance.AddComponent<MapLocation>();
            PlayerSandboxBuilder.SetSerialized(location, "id", place.Id);
            PlayerSandboxBuilder.SetSerialized(location, "displayName", place.Name);
            PlayerSandboxBuilder.SetSerialized(location, "subtitle", place.Subtitle);
            PlayerSandboxBuilder.SetSerialized(location, "sceneName", place.Scene);
            PlayerSandboxBuilder.SetSerialized(location, "introStory", place.Story);
            PlayerSandboxBuilder.SetSerialized(location, "requires", place.Requires);
            PlayerSandboxBuilder.SetSerialized(location, "requiresName", place.RequiresName);
            PlayerSandboxBuilder.SetSerialized(location, "playerLayers", 1 << level.PlayerLayer);
            MenuScenesBuilder.SetField(location, "arrival", entrance.transform);
            MenuScenesBuilder.SetField(location, "panel", panel);
            MenuScenesBuilder.SetField(location, "doneMark", flag);
            PlayerSandboxBuilder.SetSerialized(location, "tinted", tinted.ToArray());
        }

        // Palmeras, árboles, mangles, rocas, lanchas y casitas: cada uno con su base en su lugar, ordenados por altura.
        private static void CreateDetails(Transform root)
        {
            var parent = new GameObject("Detalles").transform;
            parent.SetParent(root, false);
            foreach ((string file, float width, Vector2[] spots) in Details)
            {
                Sprite sprite = LoadTrimmedSprite(DetailsArtFolder, file);
                if (sprite == null)
                    continue;
                float scale = width / sprite.bounds.size.x;
                for (int i = 0; i < spots.Length; i++)
                {
                    var detail = new GameObject($"{file} {i + 1}").AddComponent<SpriteRenderer>();
                    detail.transform.SetParent(parent, false);
                    detail.transform.localScale = new Vector3(i % 2 == 0 ? scale : -scale, scale, 1f); // Volteados para variar.
                    detail.transform.position = new Vector3(spots[i].x, spots[i].y - sprite.bounds.min.y * scale, 0f);
                    detail.sprite = sprite;
                    detail.sortingOrder = MapWalker.SortingOrder(spots[i].y);
                }
            }
        }

        // Sprite de un PNG del mapa (se configura como sprite la primera vez); null si no existe.
        // maxSize: el mapa base es grande (~5500 px) y con el tope normal de Unity (2048) se vería borroso de cerca.
        private static Sprite LoadMapSprite(string path, int maxSize = 2048)
        {
            if (!File.Exists(path))
                return null;
            Sprite sprite = MenuScenesBuilder.LoadUiSprite(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
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
            string target = $"{TrimmedFolder}/{System.IO.Path.GetFileName(folder)}_{name}.png";
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

        private static MapWalker CreateWalker(PlayerSandboxBuilder.LevelContext level, Vector2 spawn)
        {
            var player = new GameObject("Ulises (mapa)") { layer = level.PlayerLayer };
            player.transform.position = spawn;
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // Solo los pies chocan: puede pasar frente a los edificios sin atorarse con la cabeza.
            var feet = player.AddComponent<CapsuleCollider2D>();
            feet.direction = CapsuleDirection2D.Horizontal;
            feet.size = new Vector2(0.8f, 0.45f);
            feet.offset = new Vector2(0f, 0.2f);
            var input = player.AddComponent<PlayerInputReader>();
            MenuScenesBuilder.SetField(input, "actions", level.InputActions);

            // El dibujo es Ulises de lado (pivote en los pies) un poco más chico, mientras no haya arte desde arriba.
            var visual = new GameObject("Visual") { layer = level.PlayerLayer };
            visual.transform.SetParent(player.transform, false);
            visual.transform.localScale = Vector3.one * 0.8f;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            renderer.sprite = level.Square;
            Animator animator = null;
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.UlisesControllerPath);
            if (controller != null)
            {
                animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                visual.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                visual.transform.localScale = new Vector3(0.8f, 1.6f, 1f);
            }

            var walker = player.AddComponent<MapWalker>();
            MenuScenesBuilder.SetField(walker, "visual", visual.transform);
            MenuScenesBuilder.SetField(walker, "visualRenderer", renderer);
            if (animator != null)
                MenuScenesBuilder.SetField(walker, "animator", animator);
            GameSoundsBuilder.SetSound(walker, "footstep", GameSoundsBuilder.Load(GameSoundsBuilder.UlisesFolder, "paso"), 0.25f);
            return walker;
        }

        // Abajo al centro: nombre del lugar, frase, estado y "Entrar" con el botón del dispositivo actual.
        private static LocationPanel CreateLocationPanel()
        {
            Transform canvas = MenuScenesBuilder.CreateCanvas("HUD del mapa");
            var root = new GameObject("Ventanita del lugar", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 190f);
            rootRect.sizeDelta = new Vector2(860f, 270f);

            RectTransform card = MenuScenesBuilder.CreateCard("Tarjeta", rootRect, UiAssetsBuilder.EnsureRoundedSprite(),
                new Vector2(860f, 270f), MenuScenesBuilder.Crema);
            TMP_Text title = MenuScenesBuilder.CreateText("Nombre", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), 60,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, 75f), rectSize: new Vector2(800f, 80f));
            TMP_Text subtitle = MenuScenesBuilder.CreateText("Frase", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 30,
                MenuScenesBuilder.TintaClara, position: new Vector2(0f, 15f), rectSize: new Vector2(800f, 44f));
            TMP_Text status = MenuScenesBuilder.CreateText("Estado", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 30,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, -40f), rectSize: new Vector2(800f, 44f));

            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            var prompt = new GameObject("Entrar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var promptRect = (RectTransform)prompt.transform;
            promptRect.SetParent(card, false);
            promptRect.anchoredPosition = new Vector2(0f, -92f);
            promptRect.sizeDelta = new Vector2(400f, 60f);
            var layout = prompt.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Image icon = MenuScenesBuilder.CreatePromptIcon(promptRect);
            icon.GetComponent<LayoutElement>().preferredWidth = icon.GetComponent<LayoutElement>().preferredHeight = 56f;
            MenuScenesBuilder.CreatePromptLabel(promptRect, font, "Entrar").fontSize = 40;
            var bar = prompt.AddComponent<ButtonPromptBar>();
            MenuScenesBuilder.SetField(bar, "confirmIcon", icon);
            MenuScenesBuilder.SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_q", "kb_e");
            MenuScenesBuilder.SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_lb", "xbox_rb");
            MenuScenesBuilder.SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_l1", "ps_r1");

            var panel = root.AddComponent<LocationPanel>();
            MenuScenesBuilder.SetField(panel, "group", root.GetComponent<CanvasGroup>());
            MenuScenesBuilder.SetField(panel, "card", card);
            MenuScenesBuilder.SetField(panel, "title", title);
            MenuScenesBuilder.SetField(panel, "subtitle", subtitle);
            MenuScenesBuilder.SetField(panel, "status", status);
            MenuScenesBuilder.SetField(panel, "enterPrompt", prompt);
            return panel;
        }

        // Camino de tierra entre dos puntos: un tramo horizontal y uno vertical, como calles.
        private static void PathBetween(Transform root, Sprite square, Vector2 from, Vector2 to)
        {
            const float width = 2.2f;
            var corner = new Vector2(to.x, from.y);
            Rect("Camino", root, square, (from + corner) / 2f, new Vector2(Mathf.Abs(corner.x - from.x) + width, width), Path, -27);
            Rect("Camino", root, square, (corner + to) / 2f, new Vector2(width, Mathf.Abs(to.y - corner.y) + width), Path, -27);
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

        private static void Wall(string name, Transform parent, Vector2 worldCenter, Vector2 size, int layer)
        {
            var wall = new GameObject(name) { layer = layer };
            wall.transform.SetParent(parent, false);
            wall.transform.position = worldCenter;
            wall.AddComponent<BoxCollider2D>().size = size;
        }
    }
}
