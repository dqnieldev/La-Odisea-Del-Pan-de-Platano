using System.IO;
using System.Linq;
using Odisea.Core;
using Odisea.Map;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Mapa de San Blas como el de Mario: el dibujo completo en pantalla (Art/Map/Mapa.png, con los cinco lugares y
    /// sus bases de piedra ya dibujados) y la ficha de Ulises que camina por los caminos y el muelle
    /// (<see cref="OverworldMap"/>). Los lugares que todavía no se abren se ven en blanco y negro: una copia en gris
    /// del dibujo con la silueta del recorte de cada lugar (Art/Map/Recortes), que al abrirse se borra como pintura
    /// que se extiende desde la base. Las coordenadas se midieron en el dibujo (píxeles de 1935 × 1080) y la escala y
    /// posición de cada recorte se encontraron comparándolo con el mapa.
    /// </summary>
    public static class MapBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mapa.unity";
        private const string MapArtPath = "Assets/_Project/Art/Map/Mapa.png";
        private const string CutoutsFolder = "Assets/_Project/Art/Map/Recortes";
        // Lo que este generador hace a partir del mapa y los recortes (grises y la base dorada).
        private const string GeneratedFolder = "Assets/_Project/Art/Map/Generados";
        private const string ReachedCutout = "Lugar_alcanzado";
        private const string MapMusicPath = "Assets/_Project/Audio/Music/musica_mapa";
        // Punto blanco difuso del ambiente (humo, polvo, destellos, sombras de nubes) y el aro del lugar elegido.
        private const string SoftDotPath = "Assets/_Project/Art/Placeholders/Difuso.png";
        private const string RingPath = "Assets/_Project/Art/Placeholders/Anillo.png";
        private const string UnlockSoundPath = "Assets/_Project/Audio/SFX/UI/logo_brillo.mp3";

        // El mapa mide Width de ancho (el alto sale de la proporción del dibujo) y llena la pantalla.
        private const float Width = 64f;
        private static readonly Vector2 Reference = new(1935f, 1080f);
        private const int PlaceOrder = 10, LabelOrder = 40, TokenOrder = 100;
        // Ancho de la base de piedra en el dibujo (px): la dorada de "alcanzado" se pone encima a ese tamaño.
        private const float PadWidth = 94f;

        // Ambiente medido en el dibujo (px): agua donde brillan los destellos y la punta de la chimenea de la panadería.
        private static readonly Rect[] SeaAreas =
        {
            new(320f, 965f, 780f, 70f),
            new(1150f, 730f, 760f, 300f),
            new(30f, 600f, 70f, 380f),
        };
        private static readonly Vector2 Chimney = new(235f, 418f);

        private readonly struct Place
        {
            public readonly string Id, Name, Subtitle, Scene, Story, Requires, RequiresName, Cutout;
            // Stand: centro de la base de piedra (ahí se para Ulises). Click: área (x, y, w, h).
            public readonly Vector2 Stand, CutoutOrigin;
            public readonly Rect Click;
            public readonly float CutoutScale;

            public Place(string id, string name, string subtitle, string scene, string story, string requires, string requiresName,
                Vector2 stand, Rect click, string cutout, float cutoutScale, Vector2 cutoutOrigin)
            {
                Id = id; Name = name; Subtitle = subtitle; Scene = scene; Story = story; Requires = requires;
                RequiresName = requiresName; Stand = stand; Click = click;
                Cutout = cutout; CutoutScale = cutoutScale; CutoutOrigin = cutoutOrigin;
            }
        }

        // En orden de la historia. El recorte va a CutoutScale de su tamaño, con su esquina de arriba a la izquierda en
        // CutoutOrigin (px del mapa).
        private static readonly Place[] Places =
        {
            new(GameProgress.Tutorial, "Panadería de Don Lupe", "Habla con Don Lupe", SceneFlow.Bakery, "", "", "",
                new Vector2(452f, 693f), new Rect(130f, 300f, 410f, 440f),
                "Lugares_Panaderia", 0.495f, new Vector2(-149.1f, 243.4f)),
            new(GameProgress.Mercado, "El Mercado", "Acto I · La cueva del mercado", "Mercado", "Mercado", GameProgress.Tutorial,
                "el tutorial", new Vector2(796f, 400f), new Rect(570f, 10f, 590f, 430f),
                "Lugares_mercado", 0.585f, new Vector2(297.5f, -98.7f)),
            new(GameProgress.Horno, "El Horno", "Acto II · El asedio a la panadería", "Horno", "Horno", GameProgress.Mercado,
                "El Mercado", new Vector2(889f, 870f), new Rect(840f, 560f, 310f, 350f),
                "Lugares_horno", 0.465f, new Vector2(552.4f, 453f)),
            new(GameProgress.Tovara, "La Tovara", "Acto III · Carrera por los manglares", "Tovara", "Tovara", GameProgress.Horno,
                "El Horno", new Vector2(1380f, 565f), new Rect(1165f, 120f, 455f, 500f),
                "Lugares_tovara", 0.865f, new Vector2(474.3f, -89.8f)),
            new(GameProgress.Festival, "El Festival", "Final · El Festival del Pan de Plátano", "Festival", "Festival",
                GameProgress.Tovara, "La Tovara", new Vector2(1760f, 557f),
                new Rect(1630f, 190f, 305f, 500f), "Lugares_festival", 0.63f, new Vector2(1167.5f, 100f)),
        };

        // Tramos entre lugares, con puntos que siguen los caminos de tierra y el muelle (nodo = índice del lugar).
        private static readonly (int from, int to, Vector2[] points)[] Paths =
        {
            (0, 1, new Vector2[] { new(520f, 650f), new(590f, 585f), new(650f, 505f), new(705f, 445f), new(750f, 415f) }),
            (1, 2, new Vector2[] { new(830f, 470f), new(810f, 560f), new(780f, 650f), new(770f, 740f), new(800f, 815f), new(850f, 855f) }),
            (2, 3, new Vector2[]
            {
                new(970f, 885f), new(1060f, 885f), new(1130f, 860f), new(1235f, 820f), new(1355f, 770f), new(1415f, 715f),
                new(1405f, 655f), new(1360f, 610f),
            }),
            (3, 4, new Vector2[] { new(1430f, 595f), new(1500f, 640f), new(1585f, 650f), new(1660f, 620f), new(1725f, 595f) }),
        };

        private static float height = Width * 9f / 16f;

        // Píxel del dibujo (en la referencia de 1935 × 1080) → unidades del mundo, con el mapa centrado en el origen.
        private static Vector2 P(Vector2 pixel) =>
            new((pixel.x / Reference.x - 0.5f) * Width, (0.5f - pixel.y / Reference.y) * height);

        [MenuItem("Odisea/Niveles/Crear mapa")]
        public static void Build()
        {
            TutorialBuilder.EnsureCircleSprite();
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Transform root = level.Root;
            root.name = "Mapa";
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);

            // ----- El dibujo del mapa, completo en pantalla -----
            Sprite mapArt = LoadMapSprite(MapArtPath);
            if (mapArt != null)
            {
                height = Width * mapArt.bounds.size.y / mapArt.bounds.size.x;
                var art = new GameObject("Dibujo del mapa").AddComponent<SpriteRenderer>();
                art.transform.SetParent(root, false);
                art.sprite = mapArt;
                art.transform.localScale = Vector3.one * (Width / mapArt.bounds.size.x);
                art.sortingOrder = -30;
            }
            else
            {
                height = Width * 9f / 16f;
                Rect("Arena", root, square, Vector2.zero, new Vector2(Width, height), TutorialBuilder.Sand, -30);
                Debug.LogWarning($"[Odisea] Falta {MapArtPath}; el mapa queda de un color.");
            }

            // ----- Lugares y caminos -----
            LocationPanel panel = CreateLocationPanel();
            Sprite reached = EnsureReachedPad();
            var locations = Places.Select((place, i) => CreateLocation(place, i, root, square, circle, reached)).ToArray();
            var map = new GameObject("Mapa (control)").AddComponent<OverworldMap>();
            var serialized = new SerializedObject(map);
            SerializedProperty nodes = serialized.FindProperty("nodes");
            nodes.arraySize = Places.Length;
            for (int i = 0; i < Places.Length; i++)
            {
                nodes.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value = P(Places[i].Stand);
                nodes.GetArrayElementAtIndex(i).FindPropertyRelative("location").objectReferenceValue = locations[i];
            }
            SerializedProperty edges = serialized.FindProperty("edges");
            edges.arraySize = Paths.Length;
            for (int i = 0; i < Paths.Length; i++)
            {
                SerializedProperty edge = edges.GetArrayElementAtIndex(i);
                edge.FindPropertyRelative("from").intValue = Paths[i].from;
                edge.FindPropertyRelative("to").intValue = Paths[i].to;
                SerializedProperty points = edge.FindPropertyRelative("points");
                points.arraySize = Paths[i].points.Length;
                for (int p = 0; p < Paths[i].points.Length; p++)
                    points.GetArrayElementAtIndex(p).vector2Value = P(Paths[i].points[p]);
            }
            SerializedProperty order = serialized.FindProperty("locationOrder");
            order.arraySize = Places.Length;
            for (int i = 0; i < Places.Length; i++)
                order.GetArrayElementAtIndex(i).intValue = i;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // ----- La ficha de Ulises -----
            (Transform token, Transform visual, Animator animator) = CreateToken(level, P(Places[0].Stand));
            MenuScenesBuilder.SetField(map, "token", token);
            MenuScenesBuilder.SetField(map, "tokenVisual", visual);
            if (animator != null)
                MenuScenesBuilder.SetField(map, "tokenAnimator", animator);
            // Ulises chibi (ulises_mapa.png importado con Odisea > Arte > Importar arte de la sandbox).
            Sprite[] idle = ArtImportBuilder.LoadStrip(ArtImportBuilder.MapIdlePath);
            Sprite[] walk = ArtImportBuilder.LoadStrip(ArtImportBuilder.MapWalkPath);
            if (idle.Length > 0 && walk.Length > 0)
            {
                MenuScenesBuilder.SetField(map, "tokenRenderer", visual.GetComponent<SpriteRenderer>());
                PlayerSandboxBuilder.SetSerialized(map, "idleFrames", idle.Cast<Object>().ToArray());
                PlayerSandboxBuilder.SetSerialized(map, "walkFrames", walk.Cast<Object>().ToArray());
            }
            MenuScenesBuilder.SetField(map, "panel", panel);
            GameSoundsBuilder.SetSound(map, "footstep", GameSoundsBuilder.Load(GameSoundsBuilder.UlisesFolder, "paso"), 0.25f);

            // ----- Efectos: flecha y anillo del lugar elegido, ambiente -----
            MenuScenesBuilder.SetField(map, "marker", CreateMarker(root, square));
            var ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
            if (ring != null)
            {
                SpriteRenderer highlight = Rect("Anillo del lugar", root, ring, Vector2.zero, ring.bounds.size, Platano, PlaceOrder - 1);
                MenuScenesBuilder.SetField(map, "highlight", highlight);
            }
            MenuScenesBuilder.SetField(map, "ambience", CreateAmbience(square));
            var unlock = AssetDatabase.LoadAssetAtPath<AudioClip>(UnlockSoundPath);
            if (unlock != null)
                GameSoundsBuilder.SetSound(map, "unlockSound", new[] { unlock }, 0.6f);

            // ----- Cámara fija con todo el mapa, música y pausa -----
            var camera = GameObject.Find("Main Camera");
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camera.GetComponent<Camera>();
            cam.orthographicSize = height / 2f;
            cam.backgroundColor = PlayerSandboxBuilder.Cafe;

            // Con música propia del mapa, esa; si no, la del menú, que sigue sin cortarse desde el menú.
            string[] extensions = { ".ogg", ".mp3", ".wav" };
            if (extensions.Any(ext => File.Exists(MapMusicPath + ext)))
                GameSoundsBuilder.CreateMusic("Música del mapa", MapMusicPath);
            else
                MenuScenesBuilder.CreateMenuMusic();
            var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/MenuPausa.prefab");
            if (pausePrefab != null)
            {
                var pause = (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab);
                PlayerSandboxBuilder.SetSerialized(pause.GetComponent<PauseMenu>(), "levelName", "San Blas");
                // En el mapa no hay nivel que reiniciar.
                PlayerSandboxBuilder.SetSerialized(pause.GetComponent<PauseMenu>(), "allowRestart", false);
            }

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Mapa creado en {ScenePath}.");
        }

        // Lugar sobre su base de piedra: banderita de terminado, base dorada de "alcanzado", su copia en gris
        // (mientras no se abra) y el área de clic.
        private static MapLocation CreateLocation(Place place, int index, Transform root, Sprite square, Sprite circle, Sprite reached)
        {
            Vector2 stand = P(place.Stand);
            var go = new GameObject(place.Name);
            go.transform.SetParent(root, false);
            go.transform.position = stand;

            // Banderita amarilla de "terminado", junto a la base.
            var flag = new GameObject("Terminado");
            flag.transform.SetParent(go.transform, false);
            flag.transform.localPosition = new Vector3(1.8f, 0f, 0f);
            Rect("Asta", flag.transform, square, new Vector2(0f, 0.7f), new Vector2(0.14f, 1.4f), PlayerSandboxBuilder.Cafe, LabelOrder);
            Rect("Bandera", flag.transform, circle, new Vector2(0.4f, 1.2f), new Vector2(0.8f, 0.55f),
                new Color(0.96f, 0.72f, 0.19f), LabelOrder + 1);

            GameObject pad = null;
            if (reached != null)
            {
                SpriteRenderer gold = Rect("Base dorada", go.transform, reached, Vector2.zero,
                    reached.bounds.size * (PadWidth / Reference.x * Width / reached.bounds.size.x), Color.white, PlaceOrder + 8);
                pad = gold.gameObject;
            }

            // Copia en gris del lugar (cada una en su propio orden, para que la máscara del revelado solo la toque a ella).
            SpriteRenderer gray = null;
            (Sprite graySprite, Vector2 grayCenter) = EnsureGrayOverlay(place);
            if (graySprite != null)
            {
                gray = new GameObject("En gris").AddComponent<SpriteRenderer>();
                gray.transform.SetParent(go.transform, false);
                gray.transform.position = P(grayCenter);
                gray.sprite = graySprite;
                gray.sortingOrder = PlaceOrder + 1 + index;
            }

            var area = go.AddComponent<BoxCollider2D>();
            Vector2 clickMin = P(new Vector2(place.Click.xMin, place.Click.yMax)), clickMax = P(new Vector2(place.Click.xMax, place.Click.yMin));
            area.size = clickMax - clickMin;
            area.offset = (clickMin + clickMax) / 2f - stand;
            var location = go.AddComponent<MapLocation>();
            PlayerSandboxBuilder.SetSerialized(location, "id", place.Id);
            PlayerSandboxBuilder.SetSerialized(location, "displayName", place.Name);
            PlayerSandboxBuilder.SetSerialized(location, "subtitle", place.Subtitle);
            PlayerSandboxBuilder.SetSerialized(location, "sceneName", place.Scene);
            PlayerSandboxBuilder.SetSerialized(location, "introStory", place.Story);
            PlayerSandboxBuilder.SetSerialized(location, "requires", place.Requires);
            PlayerSandboxBuilder.SetSerialized(location, "requiresName", place.RequiresName);
            MenuScenesBuilder.SetField(location, "doneMark", flag);
            if (pad != null)
                MenuScenesBuilder.SetField(location, "reachedMark", pad);
            if (gray != null)
            {
                MenuScenesBuilder.SetField(location, "grayOverlay", gray);
                MenuScenesBuilder.SetField(location, "revealShape", circle);
                var ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
                if (ring != null)
                    MenuScenesBuilder.SetField(location, "revealRing", ring);
            }
            return location;
        }

        // Ulises a tamaño de ficha, con su sombra; usa sus animaciones de reposo y de correr.
        private static (Transform, Transform, Animator) CreateToken(PlayerSandboxBuilder.LevelContext level, Vector2 position)
        {
            var token = new GameObject("Ulises (ficha)").transform;
            token.position = position;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            Rect("Sombra", token, circle, new Vector2(0f, 0.05f), new Vector2(2.2f, 0.6f), new Color(0f, 0f, 0f, 0.3f), TokenOrder - 1);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(token, false);
            var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = TokenOrder;
            Animator animator = null;
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.UlisesControllerPath);
            Sprite[] chibi = ArtImportBuilder.LoadStrip(ArtImportBuilder.MapIdlePath);
            if (chibi.Length > 0)
            {
                // Ulises chibi: del alto de la puerta de un edificio.
                renderer.sprite = chibi[0];
                visual.localScale = Vector3.one * (3.4f / chibi[0].bounds.size.y);
            }
            else if (controller != null)
            {
                // Más grande que en los niveles: es la ficha del tablero y tiene que verse en el mapa completo.
                visual.localScale = Vector3.one * 2f;
                animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                renderer.sprite = level.Square;
                renderer.color = new Color(0.169f, 0.118f, 0.086f);
                visual.localPosition = new Vector3(0f, 0.8f, 0f);
                visual.localScale = new Vector3(0.8f, 1.6f, 1f);
            }
            return (token, visual, animator);
        }

        // ---------- Efectos ----------

        private static readonly Color Platano = new(0.961f, 0.718f, 0.192f);

        // Flecha amarilla con orilla café que apunta hacia abajo (dos rombos estirados).
        private static Transform CreateMarker(Transform root, Sprite square)
        {
            var marker = new GameObject("Flecha del lugar").transform;
            marker.SetParent(root, false);
            SpriteRenderer edge = Rect("Orilla", marker, square, Vector2.zero, new Vector2(1.05f, 1.05f), PlayerSandboxBuilder.Cafe,
                LabelOrder + 5);
            SpriteRenderer fill = Rect("Relleno", marker, square, new Vector2(0f, 0.05f), new Vector2(0.75f, 0.75f), Platano,
                LabelOrder + 6);
            edge.transform.localRotation = fill.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            marker.localScale = new Vector3(1.3f, 1.8f, 1f);
            return marker;
        }

        private static MapAmbience CreateAmbience(Sprite square)
        {
            Sprite softDot = EnsureSoftDot();
            var ambience = new GameObject("Ambiente").AddComponent<MapAmbience>();
            MenuScenesBuilder.SetField(ambience, "softDot", softDot);
            MenuScenesBuilder.SetField(ambience, "square", square);
            var serialized = new SerializedObject(ambience);
            serialized.FindProperty("bounds").rectValue = new UnityEngine.Rect(-Width / 2f, -height / 2f, Width, height);
            SerializedProperty sea = serialized.FindProperty("seaAreas");
            sea.arraySize = SeaAreas.Length;
            for (int i = 0; i < SeaAreas.Length; i++)
            {
                Vector2 min = P(new Vector2(SeaAreas[i].xMin, SeaAreas[i].yMax));
                Vector2 max = P(new Vector2(SeaAreas[i].xMax, SeaAreas[i].yMin));
                sea.GetArrayElementAtIndex(i).rectValue = UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
            serialized.FindProperty("chimney").vector2Value = P(Chimney);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return ambience;
        }

        /// <summary>Punto blanco con orilla difusa, de 1 unidad de ancho.</summary>
        private static Sprite EnsureSoftDot()
        {
            if (!File.Exists(SoftDotPath))
            {
                const int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha)));
                }
                File.WriteAllBytes(SoftDotPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SoftDotPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(SoftDotPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath);
        }

        // ---------- HUD ----------

        // Abajo a la izquierda: nombre del lugar, frase, estado y "Entrar". Abajo a la derecha, como en las demás
        // pantallas, la ayuda de botones: [← →] Elegir  [Aceptar] Entrar.
        private static LocationPanel CreateLocationPanel()
        {
            Transform canvas = MenuScenesBuilder.CreateCanvas("HUD del mapa");
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();

            CreatePromptBar(canvas, font, rounded);

            var root = new GameObject("Ventanita del lugar", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            // Abajo a la izquierda, sobre el mar (al centro tapaba El Horno).
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.anchoredPosition = new Vector2(450f, 150f);
            rootRect.sizeDelta = new Vector2(800f, 240f);

            RectTransform card = MenuScenesBuilder.CreateCard("Tarjeta", rootRect, rounded, new Vector2(800f, 240f), MenuScenesBuilder.Crema);
            TMP_Text title = MenuScenesBuilder.CreateText("Nombre", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold), 54,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, 68f), rectSize: new Vector2(780f, 72f));
            TMP_Text subtitle = MenuScenesBuilder.CreateText("Frase", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 28,
                MenuScenesBuilder.TintaClara, position: new Vector2(0f, 16f), rectSize: new Vector2(780f, 40f));
            TMP_Text status = MenuScenesBuilder.CreateText("Estado", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 28,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, -30f), rectSize: new Vector2(780f, 40f));

            var prompt = new GameObject("Entrar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var promptRect = (RectTransform)prompt.transform;
            promptRect.SetParent(card, false);
            promptRect.anchoredPosition = new Vector2(0f, -80f);
            promptRect.sizeDelta = new Vector2(400f, 54f);
            var layout = prompt.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Image icon = MenuScenesBuilder.CreatePromptIcon(promptRect);
            icon.GetComponent<LayoutElement>().preferredWidth = icon.GetComponent<LayoutElement>().preferredHeight = 50f;
            MenuScenesBuilder.CreatePromptLabel(promptRect, font, "Entrar").fontSize = 36;
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

        // Pastilla café translúcida (se lee sobre cualquier parte del dibujo) con los íconos y textos en crema.
        private static void CreatePromptBar(Transform canvas, TMP_FontAsset font, Sprite rounded)
        {
            var go = new GameObject("Ayuda de botones", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 36f);
            var background = go.GetComponent<Image>();
            background.sprite = rounded;
            background.type = Image.Type.Sliced;
            background.color = new Color(PlayerSandboxBuilder.Cafe.r, PlayerSandboxBuilder.Cafe.g, PlayerSandboxBuilder.Cafe.b, 0.75f);
            background.raycastTarget = false;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 26, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image choose = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Elegir"));
            MenuScenesBuilder.CreatePromptSpacer(rect);
            Image enter = MenuScenesBuilder.CreatePromptIcon(rect);
            Light(MenuScenesBuilder.CreatePromptLabel(rect, font, "Entrar"));
            choose.color = enter.color = PlayerSandboxBuilder.Crema;
            // Las flechas son más anchas que un botón.
            choose.GetComponent<LayoutElement>().preferredWidth = 64f;

            var bar = go.AddComponent<ButtonPromptBar>();
            MenuScenesBuilder.SetField(bar, "confirmIcon", enter);
            MenuScenesBuilder.SetField(bar, "pageLeftIcon", choose);
            MenuScenesBuilder.SetPromptSet(bar, "keyboard", "kb_enter", "kb_esc", "kb_arrows", "kb_e");
            MenuScenesBuilder.SetPromptSet(bar, "xbox", "xbox_a", "xbox_b", "xbox_stick_h", "xbox_rb");
            MenuScenesBuilder.SetPromptSet(bar, "playStation", "ps_cross", "ps_circle", "ps_stick_h", "ps_r1");
        }

        private static void Light(TMP_Text label)
        {
            label.color = PlayerSandboxBuilder.Crema;
            label.fontSize = 30;
        }

        // ---------- Sprites ----------

        // El dibujo del mapa es grande (~5500 px): se importa sin el tope normal de 2048 y comprimido en alta calidad.
        private static Sprite LoadMapSprite(string path)
        {
            if (!File.Exists(path))
                return null;
            Sprite sprite = MenuScenesBuilder.LoadUiSprite(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.maxTextureSize != 8192 || importer.textureCompression != TextureImporterCompression.CompressedHQ))
            {
                importer.maxTextureSize = 8192;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
        }

        /// <summary>
        /// Copia en gris del dibujo del mapa con la silueta del recorte del lugar (un poco engordada, para cubrir sus
        /// orillas), y su centro en px del mapa. Se rehace si cambian el mapa o el recorte.
        /// </summary>
        private static (Sprite, Vector2) EnsureGrayOverlay(Place place)
        {
            string cutoutPath = $"{CutoutsFolder}/{place.Cutout}.png";
            string target = $"{GeneratedFolder}/{place.Cutout}_gris.png";
            if (!File.Exists(cutoutPath) || !File.Exists(MapArtPath))
                return (null, Vector2.zero);

            Texture2D cutout = ReadTexture(cutoutPath);
            float s = place.CutoutScale;
            Vector2 o = place.CutoutOrigin;
            // Lo dibujado del recorte (en px de arriba hacia abajo) llevado al mapa.
            RectInt drawn = OpaqueBounds(cutout);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(o.x + drawn.xMin * s) - 4);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(o.y + drawn.yMin * s) - 4);
            int x1 = Mathf.Min((int)Reference.x, Mathf.CeilToInt(o.x + drawn.xMax * s) + 4);
            int y1 = Mathf.Min((int)Reference.y, Mathf.CeilToInt(o.y + drawn.yMax * s) + 4);
            var center = new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f);

            bool stale = !File.Exists(target) || File.GetLastWriteTimeUtc(MapArtPath) > File.GetLastWriteTimeUtc(target) ||
                         File.GetLastWriteTimeUtc(cutoutPath) > File.GetLastWriteTimeUtc(target);
            if (stale)
            {
                Texture2D map = ReadTexture(MapArtPath);
                int w = x1 - x0, h = y1 - y0;
                var pixels = new Color32[w * h];
                for (int my = y0; my < y1; my++)
                for (int mx = x0; mx < x1; mx++)
                {
                    // Silueta: lo más opaco del recorte alrededor del punto (2 px del mapa a cada lado).
                    float alpha = 0f;
                    for (int k = 0; k < 5; k++)
                    {
                        int dx = k == 1 ? -2 : k == 2 ? 2 : 0, dy = k == 3 ? -2 : k == 4 ? 2 : 0;
                        float u = (mx + 0.5f + dx - o.x) / s, v = (my + 0.5f + dy - o.y) / s;
                        if (u >= 0f && v >= 0f && u < cutout.width && v < cutout.height)
                            alpha = Mathf.Max(alpha, cutout.GetPixelBilinear(u / cutout.width, 1f - v / cutout.height).a);
                    }
                    Color32 c = map.GetPixel(mx, map.height - 1 - my);
                    // Gris de foto vieja: luminancia, un poco cálida y apagada.
                    float g = (0.3f * c.r + 0.59f * c.g + 0.11f * c.b) / 255f * 0.92f;
                    pixels[(y1 - 1 - my) * w + (mx - x0)] = new Color(g * 1.03f, g, g * 0.95f, alpha);
                }
                var output = new Texture2D(w, h, TextureFormat.RGBA32, false);
                output.SetPixels32(pixels);
                PlayerSandboxBuilder.EnsureFolder(GeneratedFolder);
                File.WriteAllBytes(target, output.EncodeToPNG());
                Object.DestroyImmediate(output);
                Object.DestroyImmediate(map);
                ImportPixelSprite(target, Reference.x / Width);
            }
            Object.DestroyImmediate(cutout);
            return (AssetDatabase.LoadAssetAtPath<Sprite>(target), center);
        }

        /// <summary>Base dorada de "lugar alcanzado": el recorte, recortado a lo dibujado.</summary>
        private static Sprite EnsureReachedPad()
        {
            string source = $"{CutoutsFolder}/{ReachedCutout}.png";
            string target = $"{GeneratedFolder}/Base_dorada.png";
            if (!File.Exists(source))
                return null;
            if (!File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target))
            {
                Texture2D texture = ReadTexture(source);
                RectInt drawn = OpaqueBounds(texture);
                // OpaqueBounds está de arriba hacia abajo; GetPixels, de abajo hacia arriba.
                var cropped = new Texture2D(drawn.width, drawn.height, TextureFormat.RGBA32, false);
                cropped.SetPixels(texture.GetPixels(drawn.xMin, texture.height - drawn.yMax, drawn.width, drawn.height));
                cropped.Apply();
                PlayerSandboxBuilder.EnsureFolder(GeneratedFolder);
                File.WriteAllBytes(target, cropped.EncodeToPNG());
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(cropped);
                ImportPixelSprite(target, 100f);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }

        private static Texture2D ReadTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        // Lo dibujado (alfa > 10), en px de arriba hacia abajo.
        private static RectInt OpaqueBounds(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int w = texture.width, h = texture.height, minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a <= 10)
                    continue;
                int top = h - 1 - y;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, top); maxY = Mathf.Max(maxY, top);
            }
            return maxX < 0 ? new RectInt(0, 0, w, h) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void ImportPixelSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
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
