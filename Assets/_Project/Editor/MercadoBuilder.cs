using Odisea.Core;
using Odisea.Level;
using UnityEditor;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Nivel 1, El Mercado (Odisea > Niveles > Crear Mercado): nivel vertical sacado de estructura_nivel_1.jpg. Son dos
    /// torres de repisas con un hueco al centro; los pisos completos están en el suelo, a la mitad y arriba.
    /// Las repisas son firmes (no se atraviesan desde abajo): se sube por las escaleras (seguro y lento) o por las
    /// orillas del hueco con doble salto (rápido; si falla, cae varios pisos). Hay ingredientes repartidos (algunos
    /// escondidos o sobre el hueco) y la meta en la cima (el racimo de plátano). No hay puntos de control: al morir
    /// se empieza desde abajo.
    /// Sin el arte del nivel, tablones, vigas y escaleras quedan de color.
    /// </summary>
    public static class MercadoBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mercado.unity";
        private const string MusicPath = "Assets/_Project/Audio/Music/musica_nivel1";

        // Medidas del nivel (unidades). El suelo está en y = 0 y cada piso queda FloorGap más arriba.
        private const float FloorGap = 4f;
        private const int TopFloor = 7;
        private const float HalfWidth = 13.5f;
        // Vigas (columnas de las dos torres), como en el boceto.
        private static readonly float[] PostXs = { -11.2f, -4.8f, 5f, 11.1f };

        private static readonly Color Backdrop = new(0.93f, 0.87f, 0.76f);
        private static readonly Color Floor = new(0.545f, 0.353f, 0.169f);
        private static readonly Color Wall = new(0.36f, 0.24f, 0.15f);
        private static readonly Color BackWall = new(0.86f, 0.78f, 0.64f);

        // Repisas de cada piso (de dónde a dónde en x); los pisos 4 y 7 cruzan de lado a lado. Las orillas del hueco se
        // alternan (una más adentro, la de arriba más afuera) para que se pueda subir saltando junto a ellas.
        private static readonly (int floor, float from, float to)[] Shelves =
        {
            (1, -13f, -3f), (1, 3f, 13f),
            (2, -13f, -4.5f), (2, 4.5f, 13f),
            (3, -13f, -2.5f), (3, 3f, 13f),
            (4, -13f, 13f),
            (5, -13f, -3f), (5, 2.5f, 13f),
            (6, -13f, -4.5f), (6, 4.5f, 13f),
            (7, -13f, 13f),
        };
        // En los pisos completos, el tramo sobre el hueco se atraviesa desde abajo (la ruta por saltos sigue).
        private const float GapHalfWidth = 3f;
        // Ancho del tramo de paso encima de cada escalera.
        private const float HatchWidth = 1.6f;

        // Escaleras (x, del piso, al piso). Ruta: la torre izquierda hasta el piso 4, cruzar, y la derecha hasta arriba.
        // La derecha de abajo pide un doble salto (del 2 al 3) y la izquierda de arriba no llega a la cima: hay que
        // saltar el hueco.
        private static readonly (float x, int from, int to)[] Ladders =
        {
            (-6.5f, 0, 1), (-6.5f, 1, 2), (-6.5f, 2, 3), (-11f, 3, 4), (-6.5f, 4, 5), (-6.5f, 5, 6),
            (7f, 0, 1), (7f, 1, 2), (11f, 3, 4), (5.5f, 4, 5), (11f, 5, 6), (6f, 6, 7),
        };

        // Ingredientes: en las orillas, sobre el hueco y en la torre que no es la ruta.
        private static readonly (PlayerSandboxBuilder.Ingredient kind, Vector2 position)[] Ingredients =
        {
            (PlayerSandboxBuilder.Ingredient.Canela, new Vector2(-12f, Surface(1) + 1f)),
            (PlayerSandboxBuilder.Ingredient.Azucar, new Vector2(12f, Surface(2) + 1f)),
            (PlayerSandboxBuilder.Ingredient.Harina, new Vector2(0f, Surface(3) + 1.6f)),
            (PlayerSandboxBuilder.Ingredient.Platano, new Vector2(12.2f, Surface(3) + 1f)),
            (PlayerSandboxBuilder.Ingredient.Canela, new Vector2(0f, Surface(4) + 1f)),
            (PlayerSandboxBuilder.Ingredient.Harina, new Vector2(9f, Surface(5) + 1f)),
            (PlayerSandboxBuilder.Ingredient.Platano, new Vector2(-3.6f, Surface(5) + 1f)),
            (PlayerSandboxBuilder.Ingredient.Azucar, new Vector2(-12f, Surface(6) + 1f)),
        };

        private static float Surface(int floor) => floor * FloorGap;

        [MenuItem("Odisea/Niveles/Crear Mercado")]
        public static void Build()
        {
            TutorialBuilder.EnsureCircleSprite();
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Sprite square = level.Square;
            Transform root = level.Root;
            int ground = level.GroundLayer;
            float top = Surface(TopFloor);

            // ----- Suelo, paredes y el fondo de las torres -----
            PlayerSandboxBuilder.CreateBlock("Suelo", root, square, new Vector2(0f, -1f), new Vector2(HalfWidth * 2f, 2f), Floor, ground);
            PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(-HalfWidth - 0.5f, top / 2f + 2f),
                new Vector2(1f, top + 8f), Wall, ground);
            PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(HalfWidth + 0.5f, top / 2f + 2f),
                new Vector2(1f, top + 8f), Wall, ground);
            foreach (float side in new[] { -1f, 1f })
            {
                var back = PlayerSandboxBuilder.CreateBlock(side < 0f ? "Fondo torre izquierda" : "Fondo torre derecha", root, square,
                    new Vector2(side * 8f, top / 2f), new Vector2(10.5f, top), BackWall, 0);
                Object.DestroyImmediate(back.GetComponent<BoxCollider2D>());
                back.GetComponent<SpriteRenderer>().sortingOrder = -10;
            }

            // ----- Repisas, vigas y escaleras -----
            var art = PlayerSandboxBuilder.LevelArt.Load();
            foreach ((int floor, float from, float to) in Shelves)
            {
                float width = to - from;
                // La superficie (donde pisa Ulises) queda en el piso; el collider mide PlatformThickness (0.4).
                string side = width > 20f ? "completo" : from < 0f ? "izquierda" : "derecha";
                GameObject shelf = PlayerSandboxBuilder.CreatePlatform($"Piso {floor} ({side})", root, square, art,
                    new Vector2((from + to) / 2f, Surface(floor) - 0.2f), width, ground, posts: false);
                MakeFirm(shelf, floor, from, to, width > 20f, ground);
                DrawBrokenEnds(shelf, art, floor, from, to, width > 20f);
            }
            if (art.Post != null)
            {
                foreach (float x in PostXs)
                    PlayerSandboxBuilder.CreatePost(root, art.Post, new Vector2(x, 0f), top - 0.4f, ground);
            }
            foreach ((float x, int from, int to) in Ladders)
                PlayerSandboxBuilder.CreateLadder($"Escalera {from}-{to}", root, square, art, x, Surface(from), Surface(to), level.LadderLayer);

            // ----- Ingredientes y un pescado a la mitad -----
            foreach ((PlayerSandboxBuilder.Ingredient kind, Vector2 position) in Ingredients)
                PlayerSandboxBuilder.CreateIngredient(level, kind, position);
            PlayerSandboxBuilder.CreatePickup(level, PickupKind.Heal, new Vector2(8f, Surface(4) + 1f));

            // ----- Meta: el racimo de plátano en la cima -----
            CreateGoal(root, new Vector2(0f, top), level.PlayerLayer);

            PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(0f, 1f), "El Mercado", MusicPath,
                showControls: false, background: Backdrop);
            // La cámara sigue hacia arriba sin asomarse fuera de las paredes ni debajo del suelo.
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(-1f, 5f));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(1f, top - 2f));

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Mercado creado en {ScenePath}");
        }

        /// <summary>
        /// Repisa firme: se cambia su collider de un sentido por tramos sólidos y deja de un sentido solo los pasos (encima
        /// de cada escalera que llega a este piso y, en los pisos completos, sobre el hueco). Las escaleras atraviesan
        /// esos pasos al trepar.
        /// </summary>
        private static void MakeFirm(GameObject shelf, int floor, float from, float to, bool full, int layer)
        {
            var openings = new System.Collections.Generic.List<(float from, float to)>();
            foreach ((float x, _, int ladderTo) in Ladders)
            {
                if (ladderTo == floor && x > from && x < to)
                    openings.Add((x - HatchWidth / 2f, x + HatchWidth / 2f));
            }
            if (full)
                openings.Add((-GapHalfWidth, GapHalfWidth));
            openings.Sort((a, b) => a.from.CompareTo(b.from));

            var box = shelf.GetComponent<BoxCollider2D>();
            float y = box.bounds.center.y, thickness = box.bounds.size.y;
            Object.DestroyImmediate(shelf.GetComponent<PlatformEffector2D>());
            Object.DestroyImmediate(box);

            float cursor = from;
            foreach ((float start, float end) in openings)
            {
                Piece(shelf, "Tramo firme", cursor, start, y, thickness, layer, oneWay: false);
                Piece(shelf, start > -GapHalfWidth - 0.01f && end < GapHalfWidth + 0.01f ? "Paso (hueco)" : "Paso (escalera)",
                    start, end, y, thickness, layer, oneWay: true);
                cursor = end;
            }
            Piece(shelf, "Tramo firme", cursor, to, y, thickness, layer, oneWay: false);
        }

        // Hueco que se deja entre las dos puntas rotas de un paso (se asoma la escalera).
        private const float CrackHalfWidth = 0.3f;

        /// <summary>
        /// Dibujo de la repisa por tramos para que se note por dónde se pasa: se parte (con las puntas astilladas de
        /// TablonRoto1, TablonRoto2 y TablonRotoAmbos) encima de cada escalera y a la mitad del hueco en los pisos completos, y las
        /// orillas que dan al hueco terminan rotas, como en el boceto. Sin esos dibujos se queda el tablón entero.
        /// </summary>
        private static void DrawBrokenEnds(GameObject shelf, PlayerSandboxBuilder.LevelArt art, int floor, float from, float to, bool full)
        {
            Transform whole = shelf.transform.Find("Tablón");
            if (whole == null || art.BrokenPlank == null || art.BrokenPlank2 == null)
                return;
            Object.DestroyImmediate(whole.gameObject);

            var cracks = new System.Collections.Generic.List<float>();
            foreach ((float x, _, int ladderTo) in Ladders)
            {
                if (ladderTo == floor && x > from && x < to)
                    cracks.Add(x);
            }
            if (full)
                cracks.Add(0f);
            cracks.Sort();

            // Las orillas del hueco van rotas; las de la pared, enteras.
            float start = from;
            bool brokenStart = !full && from > 0f;
            foreach (float crack in cracks)
            {
                DrawSegment(shelf, art, start, crack - CrackHalfWidth, brokenStart, brokenEnd: true);
                start = crack + CrackHalfWidth;
                brokenStart = true;
            }
            DrawSegment(shelf, art, start, to, brokenStart, brokenEnd: !full && to < 0f);
        }

        private static void DrawSegment(GameObject shelf, PlayerSandboxBuilder.LevelArt art, float from, float to, bool brokenStart,
            bool brokenEnd)
        {
            // Las puntas enteras se pasan un poco del collider, como el tablón de siempre; las rotas acaban justo ahí.
            if (!brokenStart)
                from -= 0.1f;
            if (!brokenEnd)
                to += 0.1f;
            Sprite sprite = brokenStart && brokenEnd && art.BrokenBoth != null ? art.BrokenBoth
                : brokenEnd ? art.BrokenPlank : brokenStart ? art.BrokenPlank2 : art.Plank;
            PlankPiece(shelf, sprite, from, to);
        }

        // Un tablón de x = from a x = to (en el mundo), repetido a lo ancho: sus puntas (los bordes del sprite) no se
        // estiran. La cara de arriba coincide con la del collider, como en PlayerSandboxBuilder.CreatePlatform.
        private static void PlankPiece(GameObject shelf, Sprite sprite, float from, float to)
        {
            var drawing = new GameObject("Tablón") { layer = shelf.layer };
            drawing.transform.SetParent(shelf.transform, false);
            float height = sprite.bounds.size.y;
            float emptyTop = ArtImportBuilder.Padding / sprite.pixelsPerUnit;
            drawing.transform.position = new Vector3((from + to) / 2f, shelf.transform.position.y + 0.2f - height / 2f + emptyTop, 0f);
            var renderer = drawing.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(to - from, height);
            renderer.sortingOrder = 3;
        }

        // Un tramo de collider de la repisa, de x = from a x = to (en el mundo).
        private static void Piece(GameObject shelf, string name, float from, float to, float y, float thickness, int layer, bool oneWay)
        {
            if (to - from < 0.05f)
                return;
            var piece = new GameObject(name) { layer = layer };
            piece.transform.SetParent(shelf.transform, false);
            piece.transform.position = new Vector3((from + to) / 2f, y, 0f);
            // Si la repisa está escalada (sin arte, el cuadro de color), el tramo se mide en el mundo de todos modos.
            Vector3 scale = shelf.transform.lossyScale;
            piece.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            var collider = piece.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(to - from, thickness);
            if (oneWay)
                PlayerSandboxBuilder.MakeOneWay(piece);
        }

        /// <summary>Meta en la cima: el racimo de plátano dorado (círculos de color mientras llega su dibujo).</summary>
        private static void CreateGoal(Transform parent, Vector2 floor, int playerLayer)
        {
            var goal = new GameObject("Meta (racimo de plátano)");
            goal.transform.SetParent(parent, false);
            goal.transform.position = floor + new Vector2(0f, 1.5f);
            var area = goal.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2f, 3f);

            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            var glow = new GameObject("Brillo").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(goal.transform, false);
            glow.transform.localScale = Vector3.one * 2.6f;
            glow.sprite = circle;
            glow.color = new Color(1f, 0.93f, 0.6f, 0.6f);
            glow.sortingOrder = 4;
            for (int i = 0; i < 3; i++)
            {
                var banana = new GameObject($"Plátano {i + 1}").AddComponent<SpriteRenderer>();
                banana.transform.SetParent(goal.transform, false);
                banana.transform.localPosition = new Vector3((i - 1) * 0.45f, (i == 1 ? 0.2f : 0f), 0f);
                banana.transform.localScale = new Vector3(0.45f, 1.1f, 1f);
                banana.transform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 18f);
                banana.sprite = circle;
                banana.color = new Color(0.961f, 0.718f, 0.192f);
                banana.sortingOrder = 5;
            }

            var component = goal.AddComponent<LevelGoal>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "nextScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "completesLocation", GameProgress.Mercado);
            PlayerSandboxBuilder.SetSerialized(component, "completeScreen", MenuScenesBuilder.CreateLevelCompleteScreen(
                "¡Mercado completado!", "Ulises llegó a la cima con el racimo de plátano."));
        }
    }
}
