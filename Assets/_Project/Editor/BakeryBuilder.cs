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
    /// Crea la Panadería de Don Lupe: un cuarto visto de lado (mostrador, horno de piedra, repisas con pan) donde
    /// Don Lupe espera a Ulises y platica un poco. La puerta de la derecha da a la playa (el tutorial) y la de la
    /// izquierda sale al mapa (solo después del tutorial). Mientras no haya arte (Art/Environment/Panaderia/Fondo.png,
    /// Art/Characters/DonLupe/DonLupe_Reposo.png) todo es de rectángulos de color.
    /// Menú: Odisea > Niveles > Crear panadería.
    /// </summary>
    public static class BakeryBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Panaderia.unity";
        private const string BackgroundArtPath = "Assets/_Project/Art/Environment/Panaderia/Fondo.png";
        private const string DonLupeArtPath = "Assets/_Project/Art/Characters/DonLupe/DonLupe_Reposo.png";
        private const string MapMusicPath = "Assets/_Project/Audio/Music/musica_mapa";

        // El cuarto: de LeftEdge a RightEdge, con el piso en y = 0 y el techo en Ceiling.
        private const float LeftEdge = -15f;
        private const float RightEdge = 15f;
        private const float Ceiling = 10f;
        private const float ExitDoorX = -12.5f;
        private const float BeachDoorX = 12.5f;
        private const float DonLupeX = -2.6f;

        private const int BackOrder = -20, FurnitureOrder = -10, NpcOrder = 8, FrontOrder = 30;

        // Colores de boceto del interior (cálidos, de horno).
        private static readonly Color Plaster = new(0.929f, 0.827f, 0.643f);
        private static readonly Color PlasterDark = new(0.851f, 0.729f, 0.529f);
        private static readonly Color Wood = new(0.545f, 0.353f, 0.169f);
        private static readonly Color WoodDark = new(0.396f, 0.247f, 0.122f);
        private static readonly Color WoodLight = new(0.690f, 0.482f, 0.282f);
        private static readonly Color Stone = new(0.718f, 0.631f, 0.533f);
        private static readonly Color StoneDark = new(0.541f, 0.463f, 0.384f);
        private static readonly Color Ember = new(0.973f, 0.537f, 0.180f);
        private static readonly Color Sky = new(0.643f, 0.835f, 0.878f);
        private static readonly Color Sea = new(0.184f, 0.561f, 0.627f);
        private static readonly Color Bread = new(0.851f, 0.584f, 0.271f);
        private static readonly Color BreadDark = new(0.659f, 0.400f, 0.180f);
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

            // ----- Piso, paredes y techo (lo que se pisa) -----
            float width = RightEdge - LeftEdge;
            GameObject floor = PlayerSandboxBuilder.CreateBlock("Piso", root, square, new Vector2(0f, -3f), new Vector2(width + 4f, 6f),
                Wood, ground);
            floor.GetComponent<SpriteRenderer>().sortingOrder = -5;
            Rect("Orilla del piso", root, square, new Vector2(0f, -0.12f), new Vector2(width + 4f, 0.24f), WoodDark, -4);
            for (float x = LeftEdge + 1.5f; x < RightEdge; x += 3f)
                Rect("Junta", root, square, new Vector2(x, -1.5f), new Vector2(0.08f, 2.8f), WoodDark, -4);
            PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(LeftEdge - 0.5f, 5f), new Vector2(1f, 20f),
                WoodDark, ground);
            PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(RightEdge + 0.5f, 5f), new Vector2(1f, 20f),
                WoodDark, ground);

            // ----- Cuarto: el dibujo del fondo o, mientras no exista, el boceto de color -----
            var art = File.Exists(BackgroundArtPath) ? MenuScenesBuilder.LoadUiSprite(BackgroundArtPath) : null;
            if (art != null)
            {
                var drawing = new GameObject("Fondo (dibujo)").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(root, false);
                drawing.sprite = art;
                float scale = (width + 2f) / art.bounds.size.x;
                drawing.transform.localScale = Vector3.one * scale;
                drawing.transform.localPosition = new Vector3(0f, -art.bounds.min.y * scale - 1f, 0f);
                drawing.sortingOrder = BackOrder;
            }
            else
            {
                CreateRoom(root, square, circle);
            }

            // ----- Puertas -----
            NpcDialogue donLupe = CreateDonLupe(root, square, circle, level.PlayerLayer);
            CreateExitDoor(root, square, donLupe, level.PlayerLayer);
            CreateBeachDoor(root, square, level.PlayerLayer);

            // ----- Ulises, HUD, música, pausa y cámara -----
            string music = new[] { ".ogg", ".mp3", ".wav" }.Any(ext => File.Exists(MapMusicPath + ext))
                ? MapMusicPath : MenuScenesBuilder.MenuMusicPath;
            PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(ExitDoorX + 2.5f, 1f), "Panadería de Don Lupe", music,
                showControls: false, background: WoodDark);
            // La canción del mapa sigue sin cortarse al ir y venir entre el mapa y la panadería (las veces que sea).
            SceneMusic sceneMusic = Object.FindAnyObjectByType<SceneMusic>();
            if (sceneMusic != null)
                SetStrings(sceneMusic, "continueInto", SceneFlow.Map, SceneFlow.Bakery);
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            if (pause != null)
                PlayerSandboxBuilder.SetSerialized(pause, "allowRestart", false);

            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(LeftEdge + 12.4f, 5.4f));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(RightEdge - 12.4f, 5.4f));
            Camera.main.transform.position = new Vector3(LeftEdge + 12.4f, 5.4f, -10f);

            CreateDialogueBox();

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Panadería creada en {ScenePath}.");
        }

        // ---------- Cuarto (boceto) ----------

        private static void CreateRoom(Transform root, Sprite square, Sprite circle)
        {
            var room = new GameObject("Cuarto (boceto)").transform;
            room.SetParent(root, false);
            float width = RightEdge - LeftEdge;

            // Pared de yeso, lambrín de madera abajo y vigas en el techo.
            Rect("Pared", room, square, new Vector2(0f, Ceiling / 2f), new Vector2(width + 2f, Ceiling + 2f), Plaster, BackOrder);
            Rect("Lambrín", room, square, new Vector2(0f, 1.1f), new Vector2(width + 2f, 2.2f), WoodLight, BackOrder + 1);
            Rect("Moldura", room, square, new Vector2(0f, 2.25f), new Vector2(width + 2f, 0.18f), WoodDark, BackOrder + 2);
            for (float x = LeftEdge + 2f; x < RightEdge; x += 2f)
                Rect("Tabla del lambrín", room, square, new Vector2(x, 1.1f), new Vector2(0.06f, 2.1f), Wood, BackOrder + 2);
            Rect("Techo", room, square, new Vector2(0f, Ceiling + 0.6f), new Vector2(width + 2f, 1.4f), WoodDark, BackOrder + 3);
            for (float x = LeftEdge + 3f; x < RightEdge; x += 6f)
                Rect("Viga", room, square, new Vector2(x, Ceiling - 0.35f), new Vector2(0.6f, 0.7f), Wood, BackOrder + 4);
            // Cenefa de azulejos bajo el techo.
            for (float x = LeftEdge + 0.5f; x < RightEdge; x += 1f)
                Rect("Azulejo", room, square, new Vector2(x, Ceiling - 1.05f), new Vector2(0.7f, 0.7f),
                    Mathf.RoundToInt(x - LeftEdge) % 2 == 0 ? PlasterDark : new Color(0.278f, 0.471f, 0.576f, 0.55f), BackOrder + 1);

            // Ventanas con el mar y su haz de luz.
            CreateWindow(room, square, new Vector2(-3.4f, 6.2f));
            CreateWindow(room, square, new Vector2(4.6f, 6.2f));

            // Repisas con pan, entre la salida y Don Lupe.
            for (int i = 0; i < 2; i++)
            {
                float y = 3.6f + i * 1.8f;
                Rect("Repisa", room, square, new Vector2(-7.6f, y), new Vector2(3.8f, 0.22f), WoodDark, FurnitureOrder);
                Rect("Ménsula", room, square, new Vector2(-9f, y - 0.35f), new Vector2(0.18f, 0.6f), WoodDark, FurnitureOrder);
                Rect("Ménsula", room, square, new Vector2(-6.2f, y - 0.35f), new Vector2(0.18f, 0.6f), WoodDark, FurnitureOrder);
                for (int b = 0; b < 4; b++)
                    Loaf(room, circle, new Vector2(-8.9f + b * 0.88f, y + 0.3f), 0.78f, FurnitureOrder + 1);
            }

            // Mostrador con panes y un letrerito.
            Rect("Mostrador", room, square, new Vector2(1.5f, 1.05f), new Vector2(6f, 2.1f), Wood, FurnitureOrder);
            Rect("Tablero", room, square, new Vector2(1.5f, 2.2f), new Vector2(6.6f, 0.28f), WoodDark, FurnitureOrder + 1);
            for (float x = -0.6f; x < 4f; x += 2.1f)
                Rect("Panel", room, square, new Vector2(x, 1f), new Vector2(1.6f, 1.4f), WoodLight, FurnitureOrder + 1);
            Loaf(room, circle, new Vector2(0.2f, 2.6f), 1.2f, FurnitureOrder + 2);
            Loaf(room, circle, new Vector2(1.5f, 2.62f), 1.3f, FurnitureOrder + 2);
            Loaf(room, circle, new Vector2(2.8f, 2.6f), 1.1f, FurnitureOrder + 2);

            // Horno de piedra con su boca encendida y la chimenea al techo.
            Rect("Chimenea", room, square, new Vector2(8f, 7.6f), new Vector2(1.6f, 5f), StoneDark, FurnitureOrder);
            Rect("Base del horno", room, square, new Vector2(8f, 0.7f), new Vector2(5.6f, 1.4f), StoneDark, FurnitureOrder + 1);
            Rect("Cúpula", room, circle, new Vector2(8f, 2.4f), new Vector2(5f, 4.4f), Stone, FurnitureOrder);
            Rect("Boca", room, circle, new Vector2(8f, 1.9f), new Vector2(2.2f, 1.8f), Cafe, FurnitureOrder + 2);
            Rect("Brasas", room, circle, new Vector2(8f, 1.6f), new Vector2(1.6f, 0.9f), Ember, FurnitureOrder + 3);
            Rect("Resplandor", room, circle, new Vector2(8f, 1.7f), new Vector2(3.2f, 2.4f), new Color(Ember.r, Ember.g, 0.3f, 0.25f),
                FurnitureOrder + 4);
            Rect("Leña", room, square, new Vector2(10.4f, 0.25f), new Vector2(0.9f, 0.5f), Wood, FurnitureOrder + 2);

            // Costales de harina junto a la salida.
            Rect("Costal", room, circle, new Vector2(-6.8f, 0.7f), new Vector2(1.4f, 1.5f), new Color(0.90f, 0.86f, 0.76f), FurnitureOrder);
            Rect("Costal", room, circle, new Vector2(-5.7f, 0.55f), new Vector2(1.2f, 1.2f), new Color(0.86f, 0.82f, 0.70f), FurnitureOrder + 1);
        }

        private static void CreateWindow(Transform room, Sprite square, Vector2 center)
        {
            var size = new Vector2(3f, 2.4f);
            Rect("Marco", room, square, center, size + new Vector2(0.4f, 0.4f), WoodDark, BackOrder + 5);
            Rect("Cielo", room, square, center + new Vector2(0f, 0.25f), new Vector2(size.x, size.y - 0.5f), Sky, BackOrder + 6);
            Rect("Mar", room, square, center + new Vector2(0f, -0.95f), new Vector2(size.x, 0.5f), Sea, BackOrder + 6);
            Rect("Cruz", room, square, center, new Vector2(0.14f, size.y), WoodDark, BackOrder + 7);
            Rect("Cruz", room, square, center, new Vector2(size.x, 0.14f), WoodDark, BackOrder + 7);
            Rect("Alféizar", room, square, center - new Vector2(0f, size.y / 2f + 0.25f), new Vector2(size.x + 0.9f, 0.2f), WoodDark,
                BackOrder + 7);
            // Haz de luz inclinado hacia el piso.
            SpriteRenderer light = Rect("Luz", room, square, center + new Vector2(1f, -3.2f), new Vector2(2.2f, 6f),
                new Color(1f, 0.95f, 0.75f, 0.09f), FurnitureOrder + 5);
            light.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        }

        // Pan de plátano: óvalo dorado con su raya tostada.
        private static void Loaf(Transform parent, Sprite circle, Vector2 center, float width, int order)
        {
            Rect("Pan", parent, circle, center, new Vector2(width, width * 0.55f), Bread, order);
            Rect("Raya", parent, circle, center + new Vector2(0f, 0.06f), new Vector2(width * 0.6f, width * 0.12f), BreadDark, order + 1);
        }

        // ---------- Don Lupe ----------

        private static NpcDialogue CreateDonLupe(Transform root, Sprite square, Sprite circle, int playerLayer)
        {
            var npc = new GameObject("Don Lupe");
            npc.transform.SetParent(root, false);
            npc.transform.position = new Vector3(DonLupeX, 0f, 0f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(npc.transform, false);

            Sprite[] frames = File.Exists(DonLupeArtPath) ? ArtImportBuilder.LoadStrip(DonLupeArtPath) : new Sprite[0];
            float height;
            if (frames.Length > 0)
            {
                // Arte real: los pies en el pivote, de la estatura de un señor junto a Ulises.
                var drawing = new GameObject("Dibujo").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(visual, false);
                drawing.sprite = frames[0];
                height = 2.7f;
                drawing.transform.localScale = Vector3.one * (height / frames[0].bounds.size.y);
                drawing.sortingOrder = NpcOrder;
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
            AddBubble(npc.transform, dialogue, height + 0.8f);

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
        private static void CreateExitDoor(Transform root, Sprite square, NpcDialogue keeper, int playerLayer)
        {
            GameObject door = DoorFrame("Salida al mapa", root, square, ExitDoorX);
            Rect("Hoja", door.transform, square, new Vector2(0f, 1.9f), new Vector2(2.2f, 3.8f), Wood, FurnitureOrder + 1);
            Rect("Tablero", door.transform, square, new Vector2(0f, 2.8f), new Vector2(1.6f, 1.2f), WoodLight, FurnitureOrder + 2);
            Rect("Tablero", door.transform, square, new Vector2(0f, 1.1f), new Vector2(1.6f, 1.4f), WoodLight, FurnitureOrder + 2);
            Rect("Perilla", door.transform, PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath),
                new Vector2(0.75f, 1.9f), new Vector2(0.24f, 0.24f), Platano, FurnitureOrder + 3);
            CreateNamePlate(door.transform, "Al mapa", new Vector2(0f, 4.75f), 0.75f);

            var component = door.AddComponent<SceneDoor>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "targetScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "requiresDone", GameProgress.Tutorial);
            MenuScenesBuilder.SetField(component, "keeper", keeper);
            SetStrings(component, "lockedLines", ExitLockedLines);
            PlayerSandboxBuilder.SetSerialized(component, "mapLocation", GameProgress.Tutorial);
            GameSoundsBuilder.SetSound(component, "openSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.7f);
            AddBubble(door.transform, component, 5.9f);
        }

        // Puerta a la playa, a la derecha: abierta, con el mar y la luz del muelle.
        private static void CreateBeachDoor(Transform root, Sprite square, int playerLayer)
        {
            GameObject door = DoorFrame("Puerta a la playa", root, square, BeachDoorX);
            Rect("Cielo", door.transform, square, new Vector2(0f, 2.3f), new Vector2(2.2f, 3f), Sky, FurnitureOrder + 1);
            Rect("Mar", door.transform, square, new Vector2(0f, 1.1f), new Vector2(2.2f, 0.7f), Sea, FurnitureOrder + 2);
            Rect("Arena", door.transform, square, new Vector2(0f, 0.38f), new Vector2(2.2f, 0.76f), TutorialBuilder.Sand,
                FurnitureOrder + 2);
            Rect("Hoja abierta", door.transform, square, new Vector2(-1.45f, 1.9f), new Vector2(0.5f, 3.8f), Wood, FurnitureOrder + 3);
            // Luz que entra y cae en el piso.
            Rect("Luz", door.transform, square, new Vector2(-1.6f, 0.03f), new Vector2(3.6f, 0.08f), new Color(1f, 0.95f, 0.75f, 0.5f),
                FrontOrder);
            Rect("Brillo", door.transform, PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath),
                new Vector2(0f, 2f), new Vector2(3.4f, 4.4f), new Color(1f, 0.95f, 0.75f, 0.12f), FurnitureOrder + 4);
            CreateNamePlate(door.transform, "A la playa", new Vector2(0f, 4.75f), 0.75f);

            var component = door.AddComponent<SceneDoor>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "targetScene", SceneFlow.Tutorial);
            GameSoundsBuilder.SetSound(component, "openSound", new[] { UiAssetsBuilder.LoadUiSound("ui_click") }, 0.7f);
            AddBubble(door.transform, component, 5.9f);
        }

        private static GameObject DoorFrame(string name, Transform root, Sprite square, float x)
        {
            var door = new GameObject(name);
            door.transform.SetParent(root, false);
            door.transform.position = new Vector3(x, 0f, 0f);
            Rect("Marco", door.transform, square, new Vector2(0f, 2.05f), new Vector2(2.8f, 4.1f), WoodDark, FurnitureOrder);
            var area = door.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2.4f, 3.5f);
            area.offset = new Vector2(0f, 1.75f);
            return door;
        }

        // ---------- Globito de "interactuar" y letreros ----------

        private static void AddBubble(Transform parent, Interactable target, float height)
        {
            var bubble = new GameObject("Globito").transform;
            bubble.SetParent(parent, false);
            bubble.localPosition = new Vector3(0f, height, 0f);
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            Plate("Borde", bubble, rounded, new Vector2(1.1f, 1.1f), Cafe, FrontOrder);
            Plate("Fondo", bubble, rounded, new Vector2(0.96f, 0.96f), Crema, FrontOrder + 1);
            var icon = new GameObject("Botón").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(bubble, false);
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

        // Tarjetita crema con borde café y el nombre (como los del mapa).
        private static void CreateNamePlate(Transform parent, string text, Vector2 position, float height)
        {
            var plate = new GameObject("Letrero").transform;
            plate.SetParent(parent, false);
            plate.localPosition = position;
            Sprite rounded = UiAssetsBuilder.EnsureRoundedSprite();
            var size = new Vector2(text.Length * height * 0.36f + height, height);
            Plate("Borde", plate, rounded, size + new Vector2(0.1f, 0.1f), Cafe, FrontOrder - 3);
            Plate("Fondo", plate, rounded, size, Crema, FrontOrder - 2);

            var label = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshPro));
            label.transform.SetParent(plate, false);
            ((RectTransform)label.transform).sizeDelta = size;
            var tmp = label.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            tmp.fontSize = height * 5.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Cafe;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.sortingOrder = FrontOrder - 1;
        }

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
