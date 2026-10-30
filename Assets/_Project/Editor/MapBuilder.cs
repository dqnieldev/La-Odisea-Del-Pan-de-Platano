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
    /// Mapa de San Blas como el de Mario: el dibujo completo en pantalla (Art/Map/Mapa.png, visto desde arriba), los
    /// cuatro lugares sobre sus claros (Art/Map/Lugares/&lt;Lugar&gt;.png) y la ficha de Ulises que camina por los
    /// caminos dibujados (<see cref="OverworldMap"/>). Las coordenadas de claros y caminos se midieron en el dibujo
    /// (en píxeles de una imagen de 1376 × 768; sirven para cualquier tamaño con la misma proporción).
    /// </summary>
    public static class MapBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mapa.unity";
        private const string MapArtPath = "Assets/_Project/Art/Map/Mapa.png";
        private const string PlacesArtFolder = "Assets/_Project/Art/Map/Lugares";
        // Copias recortadas a lo dibujado (los PNG vienen con mucho margen transparente).
        private const string TrimmedFolder = "Assets/_Project/Art/Map/Recortes";
        private const string MapMusicPath = "Assets/_Project/Audio/Music/musica_mapa";
        // Punto blanco difuso del ambiente (humo, polvo, destellos, sombras de nubes) y el aro del lugar elegido.
        private const string SoftDotPath = "Assets/_Project/Art/Placeholders/Difuso.png";
        private const string RingPath = "Assets/_Project/Art/Placeholders/Anillo.png";
        private const string UnlockSoundPath = "Assets/_Project/Audio/SFX/UI/logo_brillo.mp3";

        // El mapa mide Width de ancho (el alto sale de la proporción del dibujo) y llena la pantalla.
        private const float Width = 64f;
        private static readonly Vector2 Reference = new(1376f, 768f);
        private const float PlaceArtWidth = 6.6f;
        // Base del dibujo de cada lugar: la orilla de abajo de su claro.
        private const float PlaceBaseDrop = 2.7f;
        private static readonly Vector2 ClickArea = new(7f, 6.2f);
        private const int PlaceOrder = 10, LabelOrder = 40, TokenOrder = 100;
        // Puntitos de los caminos: cada cuánto y qué tan lejos de los lugares empiezan.
        private const float DotSpacing = 0.95f, DotMargin = 1.1f;

        // Ambiente medido en el dibujo (px de la referencia): agua donde brillan los destellos y la punta de la
        // chimenea de la panadería.
        private static readonly Rect[] SeaAreas =
        {
            new(610f, 610f, 520f, 140f),
            new(1120f, 680f, 130f, 70f),
            new(20f, 575f, 560f, 30f),
        };
        private static readonly Vector2 Chimney = new(137f, 372f);

        private readonly struct Place
        {
            public readonly string Id, Name, Subtitle, Scene, Story, Requires, RequiresName, ArtName;
            public readonly Vector2 Clearing, Stand;
            public readonly Color Wall, Roof;

            public Place(string id, string name, string subtitle, string scene, string story, string requires, string requiresName,
                Vector2 clearing, Vector2 stand, Color wall, Color roof)
            {
                Id = id; Name = name; Subtitle = subtitle; Scene = scene; Story = story; Requires = requires;
                RequiresName = requiresName; Clearing = clearing; Stand = stand; Wall = wall; Roof = roof;
                ArtName = id == GameProgress.Tutorial ? "Panaderia" : char.ToUpperInvariant(id[0]) + id.Substring(1);
            }
        }

        // En orden de la historia. Clearing: centro del claro; Stand: dónde se para Ulises (frente a la puerta).
        private static readonly Place[] Places =
        {
            new(GameProgress.Tutorial, "Panadería de Don Lupe", "Habla con Don Lupe", SceneFlow.Bakery, "", "", "",
                new Vector2(187f, 427f), new Vector2(187f, 470f), new Color(0.95f, 0.80f, 0.55f), new Color(0.70f, 0.33f, 0.20f)),
            new(GameProgress.Mercado, "El Mercado", "Acto I · La cueva del mercado", "Mercado", "Mercado", GameProgress.Tutorial,
                "el tutorial", new Vector2(627f, 148f), new Vector2(627f, 195f), new Color(0.98f, 0.70f, 0.40f), new Color(0.80f, 0.25f, 0.20f)),
            new(GameProgress.Horno, "El Horno", "Acto II · El asedio a la panadería", "Horno", "Horno", GameProgress.Mercado,
                "El Mercado", new Vector2(727f, 522f), new Vector2(727f, 565f), new Color(0.85f, 0.62f, 0.45f), new Color(0.55f, 0.30f, 0.20f)),
            new(GameProgress.Tovara, "La Tovara", "Acto III · Carrera por los manglares", "Tovara", "Tovara", GameProgress.Horno,
                "El Horno", new Vector2(1199f, 388f), new Vector2(1199f, 430f), new Color(0.55f, 0.70f, 0.45f), new Color(0.30f, 0.45f, 0.28f)),
        };

        // Cruce de caminos (nodo 4) y los tramos entre nodos, con puntos que siguen los caminos dibujados.
        // Nodos: 0 Panadería, 1 Mercado, 2 Horno, 3 Tovara, 4 cruce.
        private static readonly Vector2 Junction = new(560f, 468f);
        private static readonly (int from, int to, Vector2[] points)[] Paths =
        {
            (0, 4, new Vector2[] { new(265f, 440f), new(340f, 438f), new(420f, 448f), new(490f, 465f) }),
            (4, 1, new Vector2[] { new(565f, 420f), new(600f, 375f), new(650f, 335f), new(690f, 300f), new(700f, 250f), new(670f, 215f) }),
            (4, 2, new Vector2[] { new(610f, 490f), new(660f, 525f), new(700f, 560f) }),
            (2, 3, new Vector2[] { new(800f, 540f), new(870f, 505f), new(960f, 497f), new(1050f, 478f), new(1110f, 450f), new(1150f, 430f) }),
        };

        private static float height = Width * 9f / 16f;

        // Píxel del dibujo (en la referencia de 1376 × 768) → unidades del mundo, con el mapa centrado en el origen.
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
            var locations = Places.Select(place => CreateLocation(place, root, square, circle)).ToArray();
            var map = new GameObject("Mapa (control)").AddComponent<OverworldMap>();
            var serialized = new SerializedObject(map);
            SerializedProperty nodes = serialized.FindProperty("nodes");
            nodes.arraySize = Places.Length + 1;
            for (int i = 0; i < Places.Length; i++)
            {
                nodes.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value = P(Places[i].Stand);
                nodes.GetArrayElementAtIndex(i).FindPropertyRelative("location").objectReferenceValue = locations[i];
            }
            nodes.GetArrayElementAtIndex(Places.Length).FindPropertyRelative("position").vector2Value = P(Junction);
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

            // ----- Efectos: puntitos de los caminos, flecha y anillo del lugar elegido, ambiente -----
            CreatePathDots(map, root, circle);
            MenuScenesBuilder.SetField(map, "marker", CreateMarker(root, square));
            var ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
            if (ring != null)
            {
                SpriteRenderer highlight = Rect("Anillo del lugar", root, ring, Vector2.zero, ring.bounds.size, Platano, PlaceOrder - 1);
                MenuScenesBuilder.SetField(map, "highlight", highlight);
            }
            MenuScenesBuilder.SetField(map, "ambience", CreateAmbience(square));
            GameSoundsBuilder.SetSound(map, "dotSound", new[] { UiAssetsBuilder.LoadUiSound("ui_hover") }, 0.25f);
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
            {
                // Sigue sin cortarse al ir y venir entre el mapa y la panadería (que tiene la misma canción).
                SceneMusic music = GameSoundsBuilder.CreateMusic("Música del mapa", MapMusicPath);
                var musicSettings = new SerializedObject(music);
                SerializedProperty scenes = musicSettings.FindProperty("continueInto");
                scenes.arraySize = 2;
                scenes.GetArrayElementAtIndex(0).stringValue = SceneFlow.Bakery;
                scenes.GetArrayElementAtIndex(1).stringValue = SceneFlow.Map;
                musicSettings.ApplyModifiedPropertiesWithoutUndo();
            }
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

        // Edificio sobre su claro, letrero con el nombre, banderita de terminado y el área de clic.
        private static MapLocation CreateLocation(Place place, Transform root, Sprite square, Sprite circle)
        {
            Vector2 center = P(place.Clearing);
            var go = new GameObject(place.Name);
            go.transform.SetParent(root, false);
            go.transform.position = center;

            var tinted = new System.Collections.Generic.List<Object>();
            float top;
            Sprite art = LoadTrimmedSprite(PlacesArtFolder, place.ArtName);
            if (art != null)
            {
                var drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(go.transform, false);
                float scale = PlaceArtWidth / art.bounds.size.x;
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, -PlaceBaseDrop - art.bounds.min.y * scale, 0f);
                drawing.sprite = art;
                drawing.sortingOrder = PlaceOrder;
                tinted.Add(drawing);
                top = -PlaceBaseDrop + art.bounds.size.y * scale;
            }
            else
            {
                tinted.Add(Rect("Pared", go.transform, square, new Vector2(0f, -0.6f), new Vector2(4.4f, 3f), place.Wall, PlaceOrder));
                tinted.Add(Rect("Techo", go.transform, square, new Vector2(0f, 1.3f), new Vector2(5f, 1.1f), place.Roof, PlaceOrder + 1));
                tinted.Add(Rect("Puerta", go.transform, square, new Vector2(0f, -1.4f), new Vector2(1f, 1.4f),
                    PlayerSandboxBuilder.Cafe, PlaceOrder + 1));
                top = 1.9f;
            }

            CreateNamePlate(go.transform, place.Name, new Vector2(0f, top + 0.5f));

            // Banderita amarilla de "terminado", a un lado del edificio.
            var flag = new GameObject("Terminado");
            flag.transform.SetParent(go.transform, false);
            flag.transform.localPosition = new Vector3(PlaceArtWidth / 2f - 0.3f, -PlaceBaseDrop + 1.2f, 0f);
            Rect("Asta", flag.transform, square, new Vector2(0f, 0.7f), new Vector2(0.14f, 1.4f), PlayerSandboxBuilder.Cafe, LabelOrder);
            Rect("Bandera", flag.transform, circle, new Vector2(0.4f, 1.2f), new Vector2(0.8f, 0.55f),
                new Color(0.96f, 0.72f, 0.19f), LabelOrder + 1);

            go.AddComponent<BoxCollider2D>().size = ClickArea;
            var location = go.AddComponent<MapLocation>();
            PlayerSandboxBuilder.SetSerialized(location, "id", place.Id);
            PlayerSandboxBuilder.SetSerialized(location, "displayName", place.Name);
            PlayerSandboxBuilder.SetSerialized(location, "subtitle", place.Subtitle);
            PlayerSandboxBuilder.SetSerialized(location, "sceneName", place.Scene);
            PlayerSandboxBuilder.SetSerialized(location, "introStory", place.Story);
            PlayerSandboxBuilder.SetSerialized(location, "requires", place.Requires);
            PlayerSandboxBuilder.SetSerialized(location, "requiresName", place.RequiresName);
            MenuScenesBuilder.SetField(location, "doneMark", flag);
            PlayerSandboxBuilder.SetSerialized(location, "tinted", tinted.ToArray());
            return location;
        }

        // Tarjetita crema con borde café y el nombre: se lee bien sobre el dibujo.
        private static void CreateNamePlate(Transform parent, string name, Vector2 position)
        {
            var plate = new GameObject("Nombre").transform;
            plate.SetParent(parent, false);
            plate.localPosition = position;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            var size = new Vector2(name.Length * 0.62f + 1.4f, 1.6f);
            Plate("Borde", plate, rounded, size + new Vector2(0.16f, 0.16f), PlayerSandboxBuilder.Cafe, LabelOrder);
            Plate("Fondo", plate, rounded, size, PlayerSandboxBuilder.Crema, LabelOrder + 1);

            var label = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshPro));
            label.transform.SetParent(plate, false);
            ((RectTransform)label.transform).sizeDelta = size;
            var text = label.GetComponent<TextMeshPro>();
            text.text = name;
            text.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            text.fontSize = 9f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = PlayerSandboxBuilder.Cafe;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.sortingOrder = LabelOrder + 2;
        }

        private static void Plate(string name, Transform parent, Sprite sprite, Vector2 size, Color color, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            // El sprite redondeado es de UI (en píxeles): se achica para que sus esquinas queden proporcionadas.
            const float cornerScale = 0.25f;
            renderer.transform.localScale = Vector3.one * cornerScale;
            renderer.size = size / cornerScale;
            renderer.color = color;
            renderer.sortingOrder = order;
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

        // Puntitos crema con orilla café a lo largo de cada tramo (OverworldMap muestra solo los de tramos abiertos).
        private static void CreatePathDots(OverworldMap map, Transform root, Sprite circle)
        {
            var serialized = new SerializedObject(map);
            SerializedProperty edges = serialized.FindProperty("edges");
            for (int i = 0; i < Paths.Length; i++)
            {
                var line = new System.Collections.Generic.List<Vector2> { Node(Paths[i].from) };
                line.AddRange(Paths[i].points.Select(P));
                line.Add(Node(Paths[i].to));
                float length = 0f;
                for (int k = 1; k < line.Count; k++)
                    length += Vector2.Distance(line[k - 1], line[k]);

                var group = new GameObject($"Camino {i + 1}").transform;
                group.SetParent(root, false);
                for (float d = DotMargin; d <= length - DotMargin; d += DotSpacing)
                {
                    var dot = new GameObject("Puntito").transform;
                    dot.SetParent(group, false);
                    dot.position = AlongLine(line, d);
                    Rect("Orilla", dot, circle, Vector2.zero, new Vector2(0.42f, 0.42f), PlayerSandboxBuilder.Cafe, PlaceOrder - 2);
                    Rect("Punto", dot, circle, Vector2.zero, new Vector2(0.28f, 0.28f), PlayerSandboxBuilder.Crema, PlaceOrder - 1);
                }
                edges.GetArrayElementAtIndex(i).FindPropertyRelative("dots").objectReferenceValue = group;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector2 Node(int index) => index < Places.Length ? P(Places[index].Stand) : P(Junction);

        private static Vector2 AlongLine(System.Collections.Generic.List<Vector2> line, float distance)
        {
            for (int k = 1; k < line.Count; k++)
            {
                float segment = Vector2.Distance(line[k - 1], line[k]);
                if (distance <= segment)
                    return Vector2.Lerp(line[k - 1], line[k], segment > 0f ? distance / segment : 0f);
                distance -= segment;
            }
            return line[line.Count - 1];
        }

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

        // Abajo: nombre del lugar, frase, estado y "Entrar". Arriba: cómo elegir lugar.
        private static LocationPanel CreateLocationPanel()
        {
            Transform canvas = MenuScenesBuilder.CreateCanvas("HUD del mapa");
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();

            var hint = new GameObject("Ayuda", typeof(RectTransform));
            var hintRect = (RectTransform)hint.transform;
            hintRect.SetParent(canvas, false);
            // Arriba a la izquierda, sobre las colinas (al centro taparía el nombre del mercado).
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 1f);
            hintRect.anchoredPosition = new Vector2(330f, -60f);
            RectTransform hintCard = MenuScenesBuilder.CreateCard("Tarjeta", hintRect, rounded, new Vector2(560f, 64f), MenuScenesBuilder.Crema);
            MenuScenesBuilder.CreateText("Texto", hintCard, "Flechas o clic: elegir lugar", font, 28, MenuScenesBuilder.Cafe, stretch: true);

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
