using System.IO;
using System.Linq;
using Odisea.Core;
using Odisea.Level;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Crea la Panadería de Don Lupe: el cuarto completo en una sola pantalla, como la casa del abuelo en Cuphead
    /// (Art/Environment/Panaderia/Fondo.png, con su viñeta), y Don Lupe junto al horno, apoyado en su bastón, que
    /// platica un poco. La puerta de la derecha da a la playa (el tutorial) y la de la izquierda sale al mapa (solo
    /// después del tutorial). Sin el dibujo, el cuarto y Don Lupe son de rectángulos de color.
    /// Menú: Odisea > Niveles > Crear panadería.
    /// </summary>
    public static class BakeryBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Panaderia.unity";
        private const string BackgroundArtPath = "Assets/_Project/Art/Environment/Panaderia/Fondo.png";
        // Música propia de la panadería; sin ella, la del mapa.
        private const string BakeryMusicPath = "Assets/_Project/Audio/Music/musica_panaderia";
        private const string MapMusicPath = "Assets/_Project/Audio/Music/musica_mapa";

        // El cuarto mide RoomHeight de alto (la cámara lo muestra completo) y el piso donde se camina queda en y = 0:
        // en el dibujo, esa línea está a FloorLine del alto, contando desde arriba. Las puertas y Don Lupe están
        // medidos en el dibujo.
        private const float RoomHeight = 8f;
        private const float FloorLine = 0.853f;
        private const float RoomWidth = RoomHeight * 2752f / 1536f;
        private const float LeftEdge = -RoomWidth / 2f + 0.5f;
        private const float RightEdge = RoomWidth / 2f - 0.5f;
        private const float ExitDoorX = -6.2f;
        private const float BeachDoorX = 6.2f;
        private const float DonLupeX = 2.5f;
        private const float DoorTop = 4.5f;
        private static float CameraY => (FloorLine - 0.5f) * RoomHeight;

        // Boca del horno en el dibujo (px de 2752 × 1536): centro de la leña y tamaño de la boca.
        private static readonly Vector2 OvenFireBase = new(2090f, 890f);
        private static readonly Vector2 OvenMouthSize = new(160f, 120f);
        private const string SoftDotPath = "Assets/_Project/Art/Placeholders/Difuso.png";

        private const int BackOrder = -20, FurnitureOrder = -10, NpcOrder = 8, FrontOrder = 30;

        // Colores de boceto del interior, mientras no esté el dibujo.
        private static readonly Color Plaster = new(0.929f, 0.827f, 0.643f);
        private static readonly Color Wood = new(0.545f, 0.353f, 0.169f);
        private static readonly Color WoodDark = new(0.396f, 0.247f, 0.122f);
        private static readonly Color WoodLight = new(0.690f, 0.482f, 0.282f);
        private static readonly Color Sky = new(0.643f, 0.835f, 0.878f);
        private static readonly Color Sea = new(0.184f, 0.561f, 0.627f);
        private static readonly Color Platano = new(0.961f, 0.718f, 0.192f);

        // Don Lupe (de Docs/Referencias/Personajes/Don Lupe): camisa azul, mandil café, boina gris, barba blanca.
        private static readonly Color Shirt = new(0.384f, 0.471f, 0.584f);
        private static readonly Color Apron = new(0.329f, 0.231f, 0.169f);
        private static readonly Color Pants = new(0.420f, 0.298f, 0.200f);
        private static readonly Color Beret = new(0.380f, 0.361f, 0.341f);
        private static readonly Color Skin = new(0.933f, 0.722f, 0.620f);
        private static readonly Color Beard = new(0.945f, 0.941f, 0.925f);

        // Lo que dice Don Lupe, según el avance de la partida (se usa la última que ya se cumplió).
        private static readonly (string requires, string[] lines)[] DonLupeLines =
        {
            ("", new[]
            {
                "¡Ahí estás, muchacho! El festival no espera a nadie.",
                "Antes de irte, baja a la playa y demuéstrame que no se te olvidó lo básico.",
                "La puerta de la derecha da al muelle. ¡Anda!",
            }),
            (GameProgress.Tutorial, new[]
            {
                "Nada mal, nada mal… Todavía tienes buenas manos.",
                "Ahora ve al Mercado: ahí está el mejor plátano macho de San Blas.",
                "Y cuídate de los ayudantes de Circe. No juegan limpio.",
            }),
            (GameProgress.Mercado, new[]
            {
                "¡Ese plátano huele a gloria! Sigue así, muchacho.",
                "El horno y la panadería te necesitan. No te rindas.",
            }),
            (GameProgress.Horno, new[]
            {
                "¡Mira nada más qué corteza! Ni mi abuelo sacaba un pan así de dorado.",
                "Y quién lo diría: hasta Polifemo terminó pidiendo otra rebanada.",
                "Pero la feria ya empezó y la tormenta cerró el camino de la costa.",
                "Hazle caso al gigante: por los manglares de La Tovara llegas más rápido. ¡Y cuida esa canasta!",
            }),
        };

        private static readonly string[] ExitLockedLines =
        {
            "¿A dónde tan rápido? Primero, la playa.",
            "La puerta del muelle es la de la derecha.",
        };

        [MenuItem("Odisea/Niveles/Crear panadería")]
        public static void Build()
        {
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Transform root = level.Root;
            root.name = "Panadería";
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            int ground = level.GroundLayer;

            // ----- Cuarto: el dibujo completo en pantalla o, mientras no exista, una pared y un piso de color -----
            var art = File.Exists(BackgroundArtPath) ? MenuScenesBuilder.LoadUiSprite(BackgroundArtPath) : null;
            if (art != null)
            {
                var drawing = new GameObject("Fondo (dibujo)").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(root, false);
                drawing.sprite = art;
                drawing.transform.localScale = Vector3.one * (RoomHeight / art.bounds.size.y);
                drawing.transform.localPosition = new Vector3(0f, CameraY, 0f);
                drawing.sortingOrder = BackOrder;
                CreateOvenFire(root);
            }
            else
            {
                Rect("Pared", root, square, new Vector2(0f, CameraY), new Vector2(RoomWidth, RoomHeight), Plaster, BackOrder);
                Rect("Lambrín", root, square, new Vector2(0f, 1f), new Vector2(RoomWidth, 2f), WoodLight, BackOrder + 1);
            }

            // ----- Lo que se pisa: piso y paredes (invisibles sobre el dibujo) -----
            GameObject floor = PlayerSandboxBuilder.CreateBlock("Piso", root, square, new Vector2(0f, -3f), new Vector2(RoomWidth + 4f, 6f),
                Wood, ground);
            floor.GetComponent<SpriteRenderer>().sortingOrder = -5;
            foreach (float x in new[] { LeftEdge - 0.5f, RightEdge + 0.5f })
            {
                GameObject wall = PlayerSandboxBuilder.CreateBlock(x < 0f ? "Pared izquierda" : "Pared derecha", root, square,
                    new Vector2(x, 5f), new Vector2(1f, 20f), WoodDark, ground);
                wall.GetComponent<SpriteRenderer>().enabled = art == null;
            }
            floor.GetComponent<SpriteRenderer>().enabled = art == null;

            // ----- Puertas -----
            NpcDialogue donLupe = CreateDonLupe(root, square, circle, level.PlayerLayer);
            // Sombra en el piso, delante de los muebles y detrás de los personajes.
            PlayerSandboxBuilder.AddGroundShadow(donLupe.gameObject, ground, new Vector2(1.6f, 0.4f), Vector2.zero, null, 3f, FurnitureOrder + 2);
            CreateExitDoor(root, square, donLupe, level.PlayerLayer, drawn: art == null);
            CreateBeachDoor(root, square, level.PlayerLayer, drawn: art == null);

            // ----- Ulises, HUD, música, pausa y cámara -----
            string music = new[] { BakeryMusicPath, MapMusicPath, MenuScenesBuilder.MenuMusicPath }
                .First(path => new[] { ".ogg", ".mp3", ".wav" }.Any(ext => File.Exists(path + ext)));
            GameObject player = PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(ExitDoorX + 1.6f, 1f), "Panadería de Don Lupe",
                music, showControls: false, background: Color.black, scored: false);
            PlayerSandboxBuilder.AddPlayerShadow(player, level, FurnitureOrder + 2);
            // Aquí no hay peligro: sin corazones en pantalla y sin golpear ni lanzar masa (solo caminar, saltar y platicar).
            Object.DestroyImmediate(GameObject.Find("HUD"));
            Object.DestroyImmediate(GameObject.Find("Game Over"));
            player.GetComponent<Odisea.Player.PlayerCombat>().enabled = false;
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            if (pause != null)
                PlayerSandboxBuilder.SetSerialized(pause, "allowRestart", false);

            // Cámara fija con todo el cuarto (no sigue a Ulises).
            Camera.main.orthographicSize = RoomHeight / 2f;
            Camera.main.transform.position = new Vector3(0f, CameraY, -10f);
            Object.DestroyImmediate(Camera.main.GetComponent<CameraFollow2D>());

            CreateDialogueBox();

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Panadería creada en {ScenePath}.");
        }

        // ---------- Horno ----------

        // Fuego vivo encima del dibujo del horno: llamas, resplandor, chispas y el brillo cálido del cuarto.
        private static void CreateOvenFire(Transform root)
        {
            var softDot = AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath);
            if (softDot == null)
                return;
            // Del píxel del dibujo al mundo (el dibujo está centrado en x = 0 y su orilla de arriba en CameraY + RoomHeight / 2).
            float scale = RoomHeight / 1536f;
            var fire = new GameObject("Fuego del horno").AddComponent<OvenFire>();
            fire.transform.SetParent(root, false);
            fire.transform.position = new Vector3((OvenFireBase.x - 2752f / 2f) * scale, CameraY + RoomHeight / 2f - OvenFireBase.y * scale, 0f);
            MenuScenesBuilder.SetField(fire, "softDot", softDot);
            var settings = new SerializedObject(fire);
            settings.FindProperty("mouthSize").vector2Value = OvenMouthSize * scale;
            settings.FindProperty("sortingOrder").intValue = BackOrder + 5;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Don Lupe ----------

        private static NpcDialogue CreateDonLupe(Transform root, Sprite square, Sprite circle, int playerLayer)
        {
            var npc = new GameObject("Don Lupe");
            npc.transform.SetParent(root, false);
            npc.transform.position = new Vector3(DonLupeX, 0f, 0f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(npc.transform, false);

            Sprite[] frames = File.Exists(ArtImportBuilder.DonLupeIdlePath)
                ? ArtImportBuilder.LoadStrip(ArtImportBuilder.DonLupeIdlePath) : new Sprite[0];
            Sprite[] talk = File.Exists(ArtImportBuilder.DonLupeTalkPath)
                ? ArtImportBuilder.LoadStrip(ArtImportBuilder.DonLupeTalkPath) : new Sprite[0];
            SpriteRenderer animated = null;
            float height;
            if (frames.Length > 0)
            {
                // Arte real (Odisea > Arte > Importar a Don Lupe): ya viene a su estatura, con los pies en el pivote.
                // NpcDialogue lo anima: respira en reposo y mueve la boca mientras habla.
                var drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(visual, false);
                drawing.sprite = frames[0];
                drawing.sortingOrder = NpcOrder;
                animated = drawing;
                height = frames[0].bounds.size.y;
            }
            else
            {
                height = 2.7f;
                // De perfil, mirando a la derecha: zapatos, pantalón, camisa con panza, mandil, brazo, cabeza, barba y boina.
                Rect("Zapato", visual, circle, new Vector2(0.15f, 0.13f), new Vector2(0.85f, 0.32f), WoodDark, NpcOrder);
                Rect("Pantalón", visual, square, new Vector2(0f, 0.6f), new Vector2(0.6f, 0.85f), Pants, NpcOrder);
                Rect("Camisa", visual, square, new Vector2(-0.05f, 1.5f), new Vector2(0.95f, 1.05f), Shirt, NpcOrder + 1);
                Rect("Panza", visual, circle, new Vector2(0.15f, 1.35f), new Vector2(1.05f, 1.05f), Shirt, NpcOrder + 1);
                Rect("Mandil", visual, square, new Vector2(0.42f, 1.2f), new Vector2(0.4f, 1.35f), Apron, NpcOrder + 2);
                Rect("Cinta", visual, square, new Vector2(0.05f, 1.25f), new Vector2(0.95f, 0.1f), Apron, NpcOrder + 2);
                Rect("Brazo", visual, square, new Vector2(-0.12f, 1.45f), new Vector2(0.32f, 0.8f), Darker(Shirt, 0.85f), NpcOrder + 3);
                Rect("Guante", visual, circle, new Vector2(-0.1f, 1.02f), new Vector2(0.4f, 0.4f), Beard, NpcOrder + 3);
                Rect("Cabeza", visual, circle, new Vector2(0.05f, 2.3f), new Vector2(0.85f, 0.85f), Skin, NpcOrder + 1);
                Rect("Pelo", visual, circle, new Vector2(-0.28f, 2.28f), new Vector2(0.35f, 0.5f), Beard, NpcOrder + 2);
                Rect("Nariz", visual, circle, new Vector2(0.5f, 2.28f), new Vector2(0.32f, 0.26f), Skin, NpcOrder + 2);
                Rect("Barba", visual, circle, new Vector2(0.2f, 2.0f), new Vector2(0.75f, 0.58f), Beard, NpcOrder + 2);
                Rect("Bigote", visual, circle, new Vector2(0.42f, 2.13f), new Vector2(0.42f, 0.16f), Beard, NpcOrder + 3);
                Rect("Ojo", visual, circle, new Vector2(0.3f, 2.42f), new Vector2(0.1f, 0.15f), Cafe, NpcOrder + 3);
                Rect("Boina", visual, circle, new Vector2(-0.05f, 2.7f), new Vector2(1.1f, 0.48f), Beret, NpcOrder + 3);
                Rect("Banda", visual, square, new Vector2(0f, 2.57f), new Vector2(0.88f, 0.12f), Darker(Beret, 0.8f), NpcOrder + 4);
            }

            var area = npc.AddComponent<BoxCollider2D>();
            area.size = new Vector2(3.2f, 3f);
            area.offset = new Vector2(0f, 1.5f);
            var dialogue = npc.AddComponent<NpcDialogue>();
            PlayerSandboxBuilder.SetSerialized(dialogue, "playerLayers", 1 << playerLayer);
            MenuScenesBuilder.SetField(dialogue, "visual", visual);
            if (animated != null)
            {
                MenuScenesBuilder.SetField(dialogue, "drawing", animated);
                PlayerSandboxBuilder.SetSerialized(dialogue, "idleFrames", frames.Cast<Object>().ToArray());
                PlayerSandboxBuilder.SetSerialized(dialogue, "talkFrames", talk.Cast<Object>().ToArray());
            }
            AddBubble(npc.transform, dialogue, new Vector2(0f, height + 0.5f), "Hablar");

            var serialized = new SerializedObject(dialogue);
            SerializedProperty conversations = serialized.FindProperty("conversations");
            conversations.arraySize = DonLupeLines.Length;
            for (int i = 0; i < DonLupeLines.Length; i++)
            {
                SerializedProperty conversation = conversations.GetArrayElementAtIndex(i);
                conversation.FindPropertyRelative("requiresDone").stringValue = DonLupeLines[i].requires;
                SerializedProperty lines = conversation.FindPropertyRelative("lines");
                lines.arraySize = DonLupeLines[i].lines.Length;
                for (int l = 0; l < DonLupeLines[i].lines.Length; l++)
                    lines.GetArrayElementAtIndex(l).stringValue = DonLupeLines[i].lines[l];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return dialogue;
        }

        // ---------- Puertas ----------

        // Salida al mapa, a la izquierda: puerta de madera cerrada. Solo abre después del tutorial.
        private static void CreateExitDoor(Transform root, Sprite square, NpcDialogue keeper, int playerLayer, bool drawn)
        {
            GameObject door = DoorFrame("Salida al mapa", root, square, ExitDoorX, drawn);
            if (drawn)
            {
                Rect("Hoja", door.transform, square, new Vector2(0f, 1.9f), new Vector2(2.2f, 3.8f), Wood, FurnitureOrder + 1);
                Rect("Perilla", door.transform, PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath),
                    new Vector2(0.75f, 1.9f), new Vector2(0.24f, 0.24f), Platano, FurnitureOrder + 3);
            }

            var component = door.AddComponent<SceneDoor>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "targetScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "requiresDone", GameProgress.Tutorial);
            MenuScenesBuilder.SetField(component, "keeper", keeper);
            SetStrings(component, "lockedLines", ExitLockedLines);
            PlayerSandboxBuilder.SetSerialized(component, "mapLocation", GameProgress.Tutorial);
            GameSoundsBuilder.SetSound(component, "openSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.7f);
            AddBubble(door.transform, component, new Vector2(0.7f, DoorTop - 0.5f), "Ir al mapa");
        }

        // Puerta a la playa, a la derecha: abierta, con el mar y la luz del muelle.
        private static void CreateBeachDoor(Transform root, Sprite square, int playerLayer, bool drawn)
        {
            GameObject door = DoorFrame("Puerta a la playa", root, square, BeachDoorX, drawn);
            if (drawn)
            {
                Rect("Cielo", door.transform, square, new Vector2(0f, 2.3f), new Vector2(2.2f, 3f), Sky, FurnitureOrder + 1);
                Rect("Mar", door.transform, square, new Vector2(0f, 1.1f), new Vector2(2.2f, 0.7f), Sea, FurnitureOrder + 2);
            }

            var component = door.AddComponent<SceneDoor>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "targetScene", SceneFlow.Tutorial);
            GameSoundsBuilder.SetSound(component, "openSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.7f);
            AddBubble(door.transform, component, new Vector2(-0.8f, DoorTop - 0.5f), "Ir a la playa");
        }

        private static GameObject DoorFrame(string name, Transform root, Sprite square, float x, bool drawn)
        {
            var door = new GameObject(name);
            door.transform.SetParent(root, false);
            door.transform.position = new Vector3(x, 0f, 0f);
            if (drawn)
                Rect("Marco", door.transform, square, new Vector2(0f, 2.05f), new Vector2(2.8f, 4.1f), WoodDark, FurnitureOrder);
            var area = door.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2.4f, 3.5f);
            area.offset = new Vector2(0f, 1.75f);
            return door;
        }

        // ---------- Globito de "interactuar" ----------

        // Tarjetita crema con borde café: el botón (cambia según el control) y lo que hace, p. ej. "[E] Ir a la playa".
        private static void AddBubble(Transform parent, Interactable target, Vector2 position, string action)
        {
            var bubble = new GameObject("Globito").transform;
            bubble.SetParent(parent, false);
            bubble.localPosition = position;
            bubble.localScale = Vector3.one * 0.6f;

            var label = new GameObject("Acción", typeof(RectTransform), typeof(TextMeshPro));
            label.transform.SetParent(bubble, false);
            var text = label.GetComponent<TextMeshPro>();
            text.text = action;
            text.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            text.fontSize = 6f;
            text.color = Cafe;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.sortingOrder = FrontOrder + 2;
            float textWidth = text.GetPreferredValues(action).x;
            const float iconSlot = 1.1f, padding = 0.3f;
            float width = iconSlot + textWidth + padding;
            ((RectTransform)label.transform).sizeDelta = new Vector2(textWidth + 0.1f, 1f);
            label.transform.localPosition = new Vector3(-width / 2f + iconSlot + textWidth / 2f, 0f, 0f);

            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            Plate("Borde", bubble, rounded, new Vector2(width + 0.14f, 1.1f), Cafe, FrontOrder);
            Plate("Fondo", bubble, rounded, new Vector2(width, 0.96f), Crema, FrontOrder + 1);
            var icon = new GameObject("Botón").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(bubble, false);
            icon.transform.localPosition = new Vector3(-width / 2f + iconSlot / 2f + 0.05f, 0f, 0f);
            icon.sortingOrder = FrontOrder + 2;
            // Los íconos de Kenney son blancos: café, como en la ayuda de botones de los menús.
            icon.color = Cafe;

            Sprite keyboard = PromptIcon("kb_e"), xbox = PromptIcon("xbox_b"), playStation = PromptIcon("ps_circle");
            icon.sprite = keyboard;
            if (keyboard != null)
                icon.transform.localScale = Vector3.one * (0.75f / keyboard.bounds.size.y);
            MenuScenesBuilder.SetField(target, "bubble", bubble);
            MenuScenesBuilder.SetField(target, "bubbleIcon", icon);
            MenuScenesBuilder.SetField(target, "keyboardIcon", keyboard);
            MenuScenesBuilder.SetField(target, "xboxIcon", xbox);
            MenuScenesBuilder.SetField(target, "playStationIcon", playStation);
        }

        private static Sprite PromptIcon(string name) => MenuScenesBuilder.LoadUiSprite($"{MenuScenesBuilder.UiFolder}/Botones/{name}.png");

        private static void Plate(string name, Transform parent, Sprite sprite, Vector2 size, Color color, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            // El sprite redondeado es de UI (en píxeles): se achica para que sus esquinas queden proporcionadas.
            const float cornerScale = 0.2f;
            renderer.transform.localScale = Vector3.one * cornerScale;
            renderer.size = size / cornerScale;
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        // ---------- Cuadro de diálogo ----------

        // Abajo al centro: tarjeta crema, cinta amarilla con el nombre y el texto; la flechita café de "sigue".
        private static void CreateDialogueBox()
        {
            Transform canvas = MenuScenesBuilder.CreateCanvas("Diálogo");
            canvas.GetComponent<Canvas>().sortingOrder = 20;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();

            var root = new GameObject("Cuadro de diálogo", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 150f);
            rootRect.sizeDelta = new Vector2(1400f, 210f);
            root.GetComponent<CanvasGroup>().blocksRaycasts = false;

            RectTransform card = MenuScenesBuilder.CreateCard("Tarjeta", rootRect, rounded, new Vector2(1400f, 210f), MenuScenesBuilder.Crema);
            RectTransform ribbon = MenuScenesBuilder.CreateCard("Cinta", card, rounded, new Vector2(320f, 66f), MenuScenesBuilder.Platano);
            ribbon.anchoredPosition = new Vector2(-500f, 105f);
            TMP_Text speaker = MenuScenesBuilder.CreateText("Nombre", ribbon, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold),
                38, MenuScenesBuilder.Cafe, stretch: true);
            TMP_Text body = MenuScenesBuilder.CreateText("Texto", card, "", UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium), 40,
                MenuScenesBuilder.Cafe, position: new Vector2(0f, -14f), rectSize: new Vector2(1280f, 140f),
                alignment: TextAlignmentOptions.TopLeft);
            Image mark = MenuScenesBuilder.CreateImage("Sigue", card, null, MenuScenesBuilder.PlatanoOscuro, new Vector2(640f, -75f),
                new Vector2(26f, 26f), stretch: false);
            mark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var box = root.AddComponent<DialogueBox>();
            MenuScenesBuilder.SetField(box, "group", root.GetComponent<CanvasGroup>());
            MenuScenesBuilder.SetField(box, "card", card);
            MenuScenesBuilder.SetField(box, "speakerName", speaker);
            MenuScenesBuilder.SetField(box, "body", body);
            MenuScenesBuilder.SetField(box, "continueMark", mark.rectTransform);
            GameSoundsBuilder.SetSound(box, "blip", new[] { UiAssetsBuilder.LoadUiSound("ui_hover") }, 0.25f);
            GameSoundsBuilder.SetSound(box, "advance", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.5f);
        }

        // ---------- Utilidades ----------

        private static readonly Color Cafe = PlayerSandboxBuilder.Cafe;
        private static readonly Color Crema = PlayerSandboxBuilder.Crema;

        private static Color Darker(Color color, float factor) => new(color.r * factor, color.g * factor, color.b * factor, color.a);

        private static void SetStrings(Object target, string field, params string[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
