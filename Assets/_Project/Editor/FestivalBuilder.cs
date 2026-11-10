using System.IO;
using System.Linq;
using Odisea.Core;
using Odisea.Level;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// El Festival (después de La Tovara, desde el mapa): la plaza de San Blas de noche, para caminar y platicar como en la
    /// panadería. Ulises habla con Don Lupe, Polifemo y Madame Circe, ve sus puntajes en el Tablero de Honor y, por la
    /// puerta del escenario, pasa a los créditos (que regresan al menú). A la izquierda, la salida al mapa. Con solo
    /// llegar, el Festival cuenta como visitado. Cámara fija con toda la plaza.
    /// Menú: Odisea > Niveles > Crear festival.
    /// </summary>
    public static class FestivalBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Festival.unity";
        private const string BackgroundArtPath = "Assets/_Project/Art/Environment/Festival/Fondo.png";
        private const string BoardArtPath = "Assets/_Project/Art/Environment/Festival/Tablero.png";
        private const string BoardShadowPath = "Assets/_Project/Art/Environment/Festival/Tablero_Sombra.png";
        private const string FestivalMusicPath = "Assets/_Project/Audio/Music/musica_festival";
        // En la plaza la música baja (poco a poco, si viene de la historieta) para que se oigan los personajes.
        public const float MusicVolume = 0.45f;
        private const string BakeryMusicPath = "Assets/_Project/Audio/Music/musica_panaderia";
        private const string SoftDotPath = "Assets/_Project/Art/Placeholders/Difuso.png";
        private const string NightSkyPath = "Assets/_Project/Art/Environment/Tovara/Cielo.png";
        private const string TownPath = "Assets/_Project/Art/Environment/Tovara/Festival.png";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // La plaza es más ancha que la pantalla: el dibujo mide ArtHeight de alto (y RoomWidth de ancho) y la cámara, de
        // ViewHeight, sigue a Ulises de lado a lado. El piso (y = 0) está a FloorLine del alto del dibujo, desde arriba;
        // la orilla de abajo de la vista coincide con la del dibujo.
        private const float ArtHeight = 10.5f;
        private const float ViewHeight = 8f;
        private const float FloorLine = 0.9f;
        private const float RoomWidth = ArtHeight * 5504f / 3072f;
        private const float LeftEdge = -RoomWidth / 2f + 0.5f;
        private const float RightEdge = RoomWidth / 2f - 0.5f;
        private static float ArtCenterY => (FloorLine - 0.5f) * ArtHeight;
        private static float CameraY => ViewHeight / 2f - (1f - FloorLine) * ArtHeight;

        // Bien separados (cada uno con su espacio para platicar, lejos de las puertas). La puerta del escenario cae en el
        // escenario del dibujo (a la derecha).
        private const float MapDoorX = -8.3f;
        private const float DonLupeX = -4.6f;
        private const float BoardX = -1.7f;
        private const float PolifemoX = 1.7f;
        private const float CirceX = 4.9f;
        private const float StageDoorX = 8.3f;

        private const int BackOrder = BakeryBuilder.BackOrder;
        private const int FurnitureOrder = BakeryBuilder.FurnitureOrder;
        private const int NpcOrder = BakeryBuilder.NpcOrder;

        private static readonly Color Cafe = PlayerSandboxBuilder.Cafe;
        private static readonly Color Crema = PlayerSandboxBuilder.Crema;
        private static readonly Color Wood = new(0.545f, 0.353f, 0.169f);
        private static readonly Color WoodDark = new(0.396f, 0.247f, 0.122f);
        private static readonly Color Plaza = new(0.42f, 0.36f, 0.33f);
        private static readonly Color Curtain = new(0.72f, 0.16f, 0.2f);
        private static readonly Color[] TentColors =
        {
            new(0.86f, 0.24f, 0.30f), new(0.98f, 0.72f, 0.18f), new(0.25f, 0.55f, 0.78f), new(0.55f, 0.30f, 0.62f),
            new(0.40f, 0.70f, 0.30f),
        };

        private static readonly (string requires, string[] lines)[] DonLupeLines =
        {
            ("", new[]
            {
                "¡Lo lograste, muchacho! Todo San Blas habla de tu pan de plátano.",
                "Mira el Tablero de Honor: ahí quedó cada hazaña de tu viaje.",
                "Y cuando quieras, ahí está la panadería. Siempre habrá masa para ti.",
            }),
        };

        private static readonly string[] PolifemoLines =
        {
            "¡Ulises! Ya me comí tres rebanadas… bueno, cinco.",
            "Perdón por lo del horno. Desde que probé tu pan, mis ollas solo sirven para cocinar.",
            "Si vuelves a la panadería, guárdame la orilla, que es la mejor parte.",
        };

        private static readonly string[] CirceLines =
        {
            "Hmph. No vayas a creer que vine por el pan.",
            "…Aunque debo admitir que la corteza está perfecta. Casi tanto como las mías.",
            "El próximo año el concurso es mío. ¡Ya verás!",
        };

        [MenuItem("Odisea/Niveles/Crear festival")]
        public static void Build()
        {
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Transform root = level.Root;
            root.name = "Festival";
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            int ground = level.GroundLayer;

            // ----- La plaza: el dibujo completo o, mientras no exista, un fondo de relleno -----
            var art = File.Exists(BackgroundArtPath) ? MenuScenesBuilder.LoadUiSprite(BackgroundArtPath) : null;
            if (art != null)
            {
                var drawing = new GameObject("Fondo (dibujo)").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(root, false);
                drawing.sprite = art;
                drawing.transform.localScale = Vector3.one * (ArtHeight / art.bounds.size.y);
                drawing.transform.localPosition = new Vector3(0f, ArtCenterY, 0f);
                drawing.sortingOrder = BackOrder;
            }
            else
            {
                CreatePlaceholderFair(root, square);
            }

            // ----- Lo que se pisa (invisible sobre el dibujo) -----
            GameObject floor = PlayerSandboxBuilder.CreateBlock("Piso", root, square, new Vector2(0f, -3f), new Vector2(RoomWidth + 4f, 6f),
                Plaza, ground);
            floor.GetComponent<SpriteRenderer>().sortingOrder = -5;
            floor.GetComponent<SpriteRenderer>().enabled = art == null;
            foreach (float x in new[] { LeftEdge - 0.5f, RightEdge + 0.5f })
            {
                GameObject wall = PlayerSandboxBuilder.CreateBlock(x < 0f ? "Orilla izquierda" : "Orilla derecha", root, square,
                    new Vector2(x, 5f), new Vector2(1f, 20f), WoodDark, ground);
                wall.GetComponent<SpriteRenderer>().enabled = false;
            }

            // ----- Personajes, tablero y puertas -----
            NpcDialogue donLupe = BakeryBuilder.CreateDonLupe(root, square, circle, level.PlayerLayer, DonLupeX, DonLupeLines);
            PlayerSandboxBuilder.AddGroundShadow(donLupe.gameObject, ground, new Vector2(1.6f, 0.4f), Vector2.zero, null, 3f, FurnitureOrder + 2);
            // Polifemo contento (respira con su plato y se ríe al hablar); sin ese dibujo, un cuadro de su caminata.
            Sprite[] happy = Enumerable.Range(0, 8)
                .Select(i => $"{ArtImportBuilder.PolifemoFolder}/Feliz/Polifemo_Feliz_{i}.png")
                .Where(File.Exists).Select(path => MenuScenesBuilder.LoadUiSprite(path)).Where(sprite => sprite != null).ToArray();
            if (happy.Length == 8)
                CreateNpc(root, "Polifemo", PolifemoX, happy.Take(4).ToArray(), -1, happy.Skip(4).ToArray(), 3.4f, PolifemoLines,
                    level.PlayerLayer, ground);
            else
                CreateNpc(root, "Polifemo", PolifemoX, ArtImportBuilder.LoadStrip($"{ArtImportBuilder.PolifemoFolder}/Polifemo_HD_Caminar.png"),
                    1, new Sprite[0], 3.4f, PolifemoLines, level.PlayerLayer, ground);
            Sprite[] circeIdle = ArtImportBuilder.LoadStrip(ArtImportBuilder.CirceIdlePath);
            Sprite[] circeLaugh = ArtImportBuilder.LoadStrip($"{ArtImportBuilder.CirceFolder}/Circe_Reir.png");
            NpcDialogue circe = CreateNpc(root, "Madame Circe", CirceX, circeIdle, -1, circeLaugh, 2.7f, CirceLines, level.PlayerLayer,
                ground);
            // Se ríe al empezar a platicar.
            GameSoundsBuilder.SetSound(circe, "talkSound", GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "circe_risa"), 0.7f);
            CreateScoreboard(root, square, ground);
            CreateMapDoor(root, square, level.PlayerLayer, drawn: art == null);
            CreateStageDoor(root, square, circle, level.PlayerLayer, drawn: art == null);

            // Con solo llegar, el Festival cuenta como visitado (en el mapa ya no sale como nuevo).
            var visited = new GameObject("Festival visitado").AddComponent<CompleteOnStart>();
            visited.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(visited, "location", GameProgress.Festival);

            // ----- Ulises, música, pausa y cámara (sin peligro: sin corazones ni golpes) -----
            string music = new[] { FestivalMusicPath, BakeryMusicPath, MenuScenesBuilder.MenuMusicPath }
                .First(path => new[] { ".ogg", ".mp3", ".wav" }.Any(ext => File.Exists(path + ext)));
            GameObject player = PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(MapDoorX + 1.9f, 1f), "El Festival",
                music, showControls: false, background: Color.black, scored: false);
            PlayerSandboxBuilder.AddPlayerShadow(player, level, FurnitureOrder + 2);
            if (music == FestivalMusicPath && Object.FindAnyObjectByType<SceneMusic>() is { } festivalMusic)
                PlayerSandboxBuilder.SetSerialized(festivalMusic, "volume", MusicVolume);
            Object.DestroyImmediate(GameObject.Find("HUD"));
            Object.DestroyImmediate(GameObject.Find("Game Over"));
            player.GetComponent<Odisea.Player.PlayerCombat>().enabled = false;
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            if (pause != null)
                PlayerSandboxBuilder.SetSerialized(pause, "allowRestart", false);

            // La cámara sigue a Ulises de lado a lado sin salirse del dibujo (a una altura fija).
            Camera.main.orthographicSize = ViewHeight / 2f;
            float halfView = ViewHeight / 2f * 16f / 9f;
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(-RoomWidth / 2f + halfView, CameraY));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(RoomWidth / 2f - halfView, CameraY));
            Camera.main.transform.position = new Vector3(-RoomWidth / 2f + halfView, CameraY, -10f);

            BakeryBuilder.CreateDialogueBox();

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Festival creado en {ScenePath}.");
        }

        // ---------- Fondo de relleno: cielo de noche, el pueblo, carpas y luces de feria ----------

        private static void CreatePlaceholderFair(Transform root, Sprite square)
        {
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(NightSkyPath) is { } sky)
            {
                var drawing = new GameObject("Cielo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(root, false);
                drawing.sprite = sky;
                float scale = Mathf.Max(RoomWidth / sky.bounds.size.x, ArtHeight / sky.bounds.size.y);
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, ArtCenterY, 0f);
                drawing.sortingOrder = BackOrder;
                if (unlit != null)
                    drawing.sharedMaterial = unlit;
            }
            if (AssetDatabase.LoadAssetAtPath<Sprite>(TownPath) is { } town)
            {
                var drawing = new GameObject("Pueblo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(root, false);
                drawing.sprite = town;
                float scale = RoomWidth * 1.05f / town.bounds.size.x;
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, town.bounds.size.y * scale * 0.2f, 0f);
                drawing.sortingOrder = BackOrder + 1;
            }
            // Carpas de colores al fondo de la plaza.
            for (int i = 0; i < 6; i++)
            {
                float x = LeftEdge + 1.2f + i * (RoomWidth - 2.4f) / 5f;
                Color color = TentColors[i % TentColors.Length];
                BakeryBuilder.Rect($"Carpa {i + 1}", root, square, new Vector2(x, 1.1f), new Vector2(2f, 2.2f), color, BackOrder + 3);
                // Techo de dos aguas: un rombo detrás de la carpa (solo asoma su mitad de arriba).
                var roof = BakeryBuilder.Rect($"Techo {i + 1}", root, square, new Vector2(x, 2.2f), new Vector2(1.45f, 1.45f),
                    TentColors[(i + 2) % TentColors.Length], BackOrder + 2);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                BakeryBuilder.Rect($"Mostrador {i + 1}", root, square, new Vector2(x, 0.45f), new Vector2(2.1f, 0.9f), Wood, BackOrder + 5);
            }
            // Luces de feria colgadas de lado a lado.
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } dot)
            {
                Color[] bulbs = { new(1f, 0.85f, 0.45f), new(1f, 0.55f, 0.4f), new(0.6f, 0.85f, 1f), new(0.8f, 1f, 0.55f) };
                for (int i = 0; i <= 28; i++)
                {
                    float t = i / 28f;
                    float x = Mathf.Lerp(LeftEdge, RightEdge, t);
                    float y = 5.6f - Mathf.Sin(t * Mathf.PI * 3f) * Mathf.Sin(t * Mathf.PI * 3f) * 0.7f;
                    var bulb = BakeryBuilder.Rect($"Foco {i + 1}", root, dot, new Vector2(x, y), new Vector2(0.45f, 0.45f),
                        bulbs[i % bulbs.Length], BackOrder + 6);
                    if (unlit != null)
                        bulb.sharedMaterial = unlit;
                }
            }
        }

        // ---------- Personajes ----------

        // Un personaje para platicar, con su dibujo a cierta estatura (los pies en el piso). frame: cuadro fijo de la tira
        // (-1: anima toda la tira en reposo); talk: cuadros mientras habla.
        private static NpcDialogue CreateNpc(Transform root, string speaker, float x, Sprite[] strip, int frame, Sprite[] talk, float height,
            string[] lines, int playerLayer, int ground)
        {
            var npc = new GameObject(speaker);
            npc.transform.SetParent(root, false);
            npc.transform.position = new Vector3(x, 0f, 0f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(npc.transform, false);

            SpriteRenderer drawing = null;
            Sprite first = strip.Length > 0 ? strip[Mathf.Clamp(frame, 0, strip.Length - 1)] : null;
            if (first != null)
            {
                drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(visual, false);
                drawing.sprite = first;
                drawing.sortingOrder = NpcOrder;
                float scale = height / first.bounds.size.y;
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, -first.bounds.min.y * scale, 0f);
            }

            var area = npc.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2.2f, 3f);
            area.offset = new Vector2(0f, 1.5f);
            var dialogue = npc.AddComponent<NpcDialogue>();
            PlayerSandboxBuilder.SetSerialized(dialogue, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(dialogue, "speakerName", speaker);
            MenuScenesBuilder.SetField(dialogue, "visual", visual);
            if (drawing != null)
            {
                MenuScenesBuilder.SetField(dialogue, "drawing", drawing);
                Sprite[] idle = frame >= 0 ? new[] { first } : strip;
                PlayerSandboxBuilder.SetSerialized(dialogue, "idleFrames", idle.Cast<Object>().ToArray());
                PlayerSandboxBuilder.SetSerialized(dialogue, "talkFrames", (talk.Length > 0 ? talk : idle).Cast<Object>().ToArray());
            }
            BakeryBuilder.AddBubble(npc.transform, dialogue, new Vector2(0f, height + 0.5f), "Hablar");

            var serialized = new SerializedObject(dialogue);
            SerializedProperty conversations = serialized.FindProperty("conversations");
            conversations.arraySize = 1;
            SerializedProperty conversation = conversations.GetArrayElementAtIndex(0);
            conversation.FindPropertyRelative("requiresDone").stringValue = "";
            SerializedProperty said = conversation.FindPropertyRelative("lines");
            said.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
                said.GetArrayElementAtIndex(i).stringValue = lines[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PlayerSandboxBuilder.AddGroundShadow(npc, ground, new Vector2(1.6f, 0.4f), Vector2.zero, null, 3f, FurnitureOrder + 2);
            return dialogue;
        }

        // ---------- Tablero de Honor ----------

        // Un tablero de madera sobre dos postes con el mejor puntaje y rango de cada nivel (FestivalScoreboard).
        private static void CreateScoreboard(Transform root, Sprite square, int ground)
        {
            var board = new GameObject("Tablero de Honor").transform;
            board.SetParent(root, false);
            board.position = new Vector3(BoardX, 0f, 0f);
            Sprite art = File.Exists(BoardArtPath) ? MenuScenesBuilder.LoadUiSprite(BoardArtPath) : null;
            if (art != null)
            {
                CreateDrawnScoreboard(board, art, ground);
                return;
            }
            const float width = 3.8f, height = 2.5f, centerY = 3.55f;
            foreach (float side in new[] { -1f, 1f })
                BakeryBuilder.Rect("Poste", board, square, new Vector2(side * (width / 2f - 0.3f), centerY / 2f), new Vector2(0.22f, centerY),
                    WoodDark, FurnitureOrder);
            BakeryBuilder.Rect("Marco", board, square, new Vector2(0f, centerY), new Vector2(width, height), WoodDark, FurnitureOrder + 1);
            BakeryBuilder.Rect("Tabla", board, square, new Vector2(0f, centerY), new Vector2(width - 0.25f, height - 0.25f), Crema,
                FurnitureOrder + 2);
            BakeryBuilder.Rect("Cinta", board, square, new Vector2(0f, centerY + height / 2f - 0.05f), new Vector2(width - 0.6f, 0.45f),
                new Color(0.86f, 0.24f, 0.30f), FurnitureOrder + 3);

            TMP_FontAsset title = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            TMP_FontAsset body = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            TextMeshPro Text(string name, string text, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize,
                TextAlignmentOptions alignment, Color color)
            {
                var label = new GameObject(name).AddComponent<TextMeshPro>();
                label.transform.SetParent(board, false);
                label.transform.localPosition = position;
                label.rectTransform.sizeDelta = size;
                label.font = font;
                label.text = text;
                label.fontSize = fontSize;
                label.alignment = alignment;
                label.color = color;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.sortingOrder = FurnitureOrder + 4;
                return label;
            }
            float top = centerY + height / 2f;
            Text("Título", "TABLERO DE HONOR", title, new Vector2(0f, top - 0.05f), new Vector2(width, 0.45f), 2.6f,
                TextAlignmentOptions.Center, Crema);
            var listY = new Vector2(0f, centerY - 0.1f);
            TextMeshPro names = Text("Niveles", "", body, listY + new Vector2(-0.55f, 0f), new Vector2(1.9f, 1.2f), 2.3f,
                TextAlignmentOptions.Left, Cafe);
            TextMeshPro points = Text("Puntos", "", body, listY + new Vector2(0.75f, 0f), new Vector2(1.1f, 1.2f), 2.3f,
                TextAlignmentOptions.Right, Cafe);
            TextMeshPro ranks = Text("Rangos", "", title, listY + new Vector2(1.45f, 0f), new Vector2(0.4f, 1.2f), 2.4f,
                TextAlignmentOptions.Center, new Color(0.78f, 0.45f, 0.08f));
            TextMeshPro total = Text("Total", "", title, new Vector2(0f, centerY - height / 2f + 0.32f), new Vector2(width - 0.4f, 0.4f), 2.4f,
                TextAlignmentOptions.Center, Cafe);
            var scoreboard = board.gameObject.AddComponent<FestivalScoreboard>();
            MenuScenesBuilder.SetField(scoreboard, "names", names);
            MenuScenesBuilder.SetField(scoreboard, "points", points);
            MenuScenesBuilder.SetField(scoreboard, "ranks", ranks);
            MenuScenesBuilder.SetField(scoreboard, "total", total);
        }

        // El tablero dibujado (listón arriba, tres tiras de papel y una placa abajo), sin patas: flota arriba, entre Don
        // Lupe y Polifemo, sobre el paisaje y sin tapar a nadie. Cada texto va en su lugar del dibujo; las medidas son
        // fracciones del dibujo original con patas (x desde la izquierda, y desde arriba): el recorte llega a CutLine.
        private static void CreateDrawnScoreboard(Transform board, Sprite art, int ground)
        {
            const float width = 5f, centerY = 4.55f, CutLine = 0.824f;
            board.position = new Vector3(BoardX, centerY, 0f);
            float height = width * art.bounds.size.y / art.bounds.size.x;
            var drawing = new GameObject("Tablero (dibujo)").AddComponent<SpriteRenderer>();
            drawing.transform.SetParent(board, false);
            drawing.sprite = art;
            drawing.transform.localScale = Vector3.one * (width / art.bounds.size.x);
            drawing.sortingOrder = FurnitureOrder;
            // Con la luz de la plaza: un poco más cálido y apagado, para que no se vea pegado encima del dibujo.
            drawing.color = new Color(0.88f, 0.8f, 0.72f);
            // Se mece apenas, como colgado.
            var sway = drawing.gameObject.AddComponent<FloatingSign>();
            PlayerSandboxBuilder.SetSerialized(sway, "amplitude", 0.06f);

            // Sombra difuminada del tablero sobre lo de atrás (su silueta borrosa, con el mismo tamaño de pixel), un poco
            // corrida hacia abajo.
            if (File.Exists(BoardShadowPath) && MenuScenesBuilder.LoadUiSprite(BoardShadowPath) is { } blur)
            {
                var cast = new GameObject("Sombra del tablero").AddComponent<SpriteRenderer>();
                cast.transform.SetParent(drawing.transform, false);
                cast.sprite = blur;
                cast.transform.localScale = Vector3.one * (art.pixelsPerUnit / blur.pixelsPerUnit);
                cast.transform.localPosition = new Vector3(0.1f, -0.18f, 0f) / drawing.transform.localScale.x;
                cast.color = new Color(1f, 1f, 1f, 0.7f);
                cast.sortingOrder = FurnitureOrder - 2;
            }

            // Resplandor cálido detrás (como los lugares del mapa) y la luz del farolito de la izquierda.
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } dot)
            {
                SpriteRenderer halo = BakeryBuilder.Rect("Resplandor", board, dot, Vector2.zero,
                    new Vector2(width * 1.45f, height * 1.6f), new Color(1f, 0.72f, 0.38f, 0.45f), FurnitureOrder - 3);
                SpriteRenderer lamp = BakeryBuilder.Rect("Luz del farolito", drawing.transform, dot,
                    new Vector2((0.036f - 0.5f) * width, (0.5f - 0.42f / CutLine) * height) / drawing.transform.localScale.x,
                    new Vector2(1.3f, 1.3f) / drawing.transform.localScale.x, new Color(1f, 0.78f, 0.4f, 0.55f), FurnitureOrder + 1);
                if (unlit != null)
                    halo.sharedMaterial = lamp.sharedMaterial = unlit;
            }

            Vector3 At(float fx, float fy) => new((fx - 0.5f) * width, (0.5f - fy / CutLine) * height, 0f);
            TMP_FontAsset titleFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RyeRegular);
            TMP_FontAsset bold = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            TMP_FontAsset body = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            Color ink = new(0.30f, 0.17f, 0.08f);
            Color gold = new(0.80f, 0.42f, 0.05f);
            TextMeshPro Text(string name, string text, TMP_FontAsset font, Vector3 position, float w, float h,
                TextAlignmentOptions alignment, Color color)
            {
                var label = new GameObject(name).AddComponent<TextMeshPro>();
                // En el dibujo, para que se mezan juntos.
                label.transform.SetParent(drawing.transform, false);
                label.transform.localPosition = position / drawing.transform.localScale.x;
                label.transform.localScale = Vector3.one / drawing.transform.localScale.x;
                label.rectTransform.sizeDelta = new Vector2(w * width, h / CutLine * height);
                label.font = font;
                label.text = text;
                label.enableAutoSizing = true;
                label.fontSizeMin = 0.5f;
                label.fontSizeMax = 10f;
                label.alignment = alignment;
                label.color = color;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.margin = Vector4.zero;
                label.sortingOrder = FurnitureOrder + 1;
                return label;
            }

            // El título en el listón (la parte de enfrente, del 30 % al 70 % del ancho).
            Text("Título", "TABLERO DE HONOR", titleFont, At(0.5f, 0.095f), 0.38f, 0.075f, TextAlignmentOptions.Center, ink);
            // Las tres tiras: nombre a la izquierda, puntos y rango a la derecha.
            float[] rows = { 0.335f, 0.46f, 0.585f };
            var names = new TMP_Text[rows.Length];
            var points = new TMP_Text[rows.Length];
            var ranks = new TMP_Text[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                names[i] = Text($"Nivel {i + 1}", "", bold, At(0.37f, rows[i]), 0.3f, 0.058f, TextAlignmentOptions.Left, ink);
                points[i] = Text($"Puntos {i + 1}", "", body, At(0.63f, rows[i]), 0.18f, 0.058f, TextAlignmentOptions.Right, ink);
                ranks[i] = Text($"Rango {i + 1}", "", titleFont, At(0.765f, rows[i]), 0.06f, 0.085f, TextAlignmentOptions.Center, gold);
            }
            TextMeshPro total = Text("Total", "", bold, At(0.5f, 0.7f), 0.22f, 0.05f, TextAlignmentOptions.Center, ink);

            var scoreboard = board.gameObject.AddComponent<FestivalScoreboard>();
            MenuScenesBuilder.SetField(scoreboard, "total", total);
            PlayerSandboxBuilder.SetSerialized(scoreboard, "rowNames", names.Cast<Object>().ToArray());
            PlayerSandboxBuilder.SetSerialized(scoreboard, "rowPoints", points.Cast<Object>().ToArray());
            PlayerSandboxBuilder.SetSerialized(scoreboard, "rowRanks", ranks.Cast<Object>().ToArray());
        }

        // ---------- Puertas ----------

        // A la izquierda, de vuelta al mapa.
        private static void CreateMapDoor(Transform root, Sprite square, int playerLayer, bool drawn)
        {
            GameObject door = BakeryBuilder.DoorFrame("Salida al mapa", root, square, MapDoorX, drawn);
            if (drawn)
                BakeryBuilder.Rect("Hoja", door.transform, square, new Vector2(0f, 1.9f), new Vector2(2.2f, 3.8f), Wood, FurnitureOrder + 1);
            var component = door.AddComponent<SceneDoor>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "targetScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "mapLocation", GameProgress.Festival);
            GameSoundsBuilder.SetSound(component, "openSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.7f);
            BakeryBuilder.AddBubble(door.transform, component, new Vector2(0.7f, 4f), "Ir al mapa");
        }

        // A la derecha, la puerta del escenario (telón rojo): lleva a los créditos, que regresan al menú.
        private static void CreateStageDoor(Transform root, Sprite square, Sprite circle, int playerLayer, bool drawn)
        {
            GameObject door = BakeryBuilder.DoorFrame("Puerta del escenario", root, square, StageDoorX, drawn);
            if (drawn)
            {
                BakeryBuilder.Rect("Telón izquierdo", door.transform, square, new Vector2(-0.55f, 1.9f), new Vector2(1.1f, 3.8f), Curtain,
                    FurnitureOrder + 1);
                BakeryBuilder.Rect("Telón derecho", door.transform, square, new Vector2(0.55f, 1.9f), new Vector2(1.1f, 3.8f),
                    new Color(Curtain.r * 0.85f, Curtain.g * 0.85f, Curtain.b * 0.85f), FurnitureOrder + 1);
                BakeryBuilder.Rect("Estrella", door.transform, circle, new Vector2(0f, 4.35f), new Vector2(0.5f, 0.5f),
                    new Color(0.98f, 0.8f, 0.25f), FurnitureOrder + 2);
            }
            var component = door.AddComponent<SceneDoor>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "targetScene", SceneFlow.Credits);
            PlayerSandboxBuilder.SetSerialized(component, "mapLocation", GameProgress.Festival);
            GameSoundsBuilder.SetSound(component, "openSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.7f);
            BakeryBuilder.AddBubble(door.transform, component, new Vector2(-0.8f, 4f), "Créditos");
        }
    }
}
