using System.Collections.Generic;
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
    /// Crea la escena del Tutorial en graybox: la playa del muelle de San Blas en estilo boceto (tablas y escalera
    /// del nivel 1, obstáculos de arena) y fondo por capas con paralaje (cielo,
    /// mar y arena) con el arte de Art/Environment/Tutorial/Fondo
    /// (o placeholders de color si todavía no se importa).
    /// Cada zona enseña una cosa con su letrero: moverse, saltar, doble salto, tablas, escaleras, golpear,
    /// lanzar masa y el cocodrilo; al final, la meta.
    /// Menú: Odisea > Niveles > Crear tutorial.
    /// </summary>
    public static class TutorialBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Tutorial.unity";
        private const string MusicFolder = "Assets/_Project/Audio/Music/";
        private const string CirclePath = "Assets/_Project/Art/Placeholders/Circulo.png";

        // Paleta de boceto (placeholders): papel, arena, tinta y fondos lavados.
        private static readonly Color Paper = new(0.937f, 0.898f, 0.800f);
        private static readonly Color Sand = new(0.882f, 0.792f, 0.600f);
        private static readonly Color SandDark = new(0.780f, 0.678f, 0.490f);
        private static readonly Color Sun = new(0.976f, 0.878f, 0.549f);
        private static readonly Color Sea = new(0.760f, 0.812f, 0.796f);
        private static readonly Color Far = new(0.690f, 0.729f, 0.698f);
        private static readonly Color Shore = new(0.741f, 0.706f, 0.627f);
        private static readonly Color Grass = new(0.420f, 0.396f, 0.310f);
        // Color de la parte de abajo de la arena dibujada (lo que asome debajo de ella).
        private static readonly Color SandBottom = new(0.638f, 0.537f, 0.401f);

        // Límites del nivel (x) y alto de la arena.
        private const float LeftEdge = -10f;
        private const float RightEdge = 124f;

        [MenuItem("Odisea/Niveles/Crear tutorial")]
        public static void Build()
        {
            EnsureCircleSprite();
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(CirclePath);
            TMP_FontAsset titleFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitBold);
            TMP_FontAsset bodyFont = UiAssetsBuilder.LoadFont(UiAssetsBuilder.RubikMedium);
            Transform root = level.Root;
            int ground = level.GroundLayer;

            // ----- Suelo y paredes -----
            float width = RightEdge - LeftEdge;
            GameObject sand = PlayerSandboxBuilder.CreateBlock("Arena", root, square,
                new Vector2((LeftEdge + RightEdge) / 2f, -3f), new Vector2(width, 6f), Sand, ground);
            sand.GetComponent<SpriteRenderer>().sortingOrder = -8; // Debajo de todo: solo se ve si falta el dibujo de la arena.
            PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(LeftEdge - 0.5f, 7f), new Vector2(1f, 20f), SandDark, ground);
            PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(RightEdge + 0.5f, 7f), new Vector2(1f, 20f), SandDark, ground);

            // Tablas, escalera y vigas (de tutorial_tablas.jpg; las de solo contorno se perdían con el paisaje).
            var art = PlayerSandboxBuilder.LevelArt.Load(ArtImportBuilder.TutorialFolder);
            art.LadderXs = new[] { 54f };

            // 1. Moverse.
            CreateSign(root, square, titleFont, bodyFont, 3f, "MOVERSE",
                Prompt.Of(new[] { "kb_a", "kb_d", "/", "kb_arrows" }, new[] { "xbox_stick_h" }, new[] { "ps_stick_h" }));

            // 2. Saltar: una caja que se sube con un salto (Ulises salta 3.2).
            CreateSign(root, square, titleFont, bodyFont, 9f, "SALTAR",
                Prompt.Of(new[] { "kb_space_icon" }, new[] { "xbox_a" }, new[] { "ps_cross" }));
            // Cubo de arena hecho con cubeta.
            CreateSandBlock("Caja (salto)", root, square, ArtImportBuilder.CubePath, 13.5f, new Vector2(2.1f, ArtImportBuilder.CubeHeight), ground);

            // 3. Doble salto: un muro que solo se alcanza saltando otra vez en el aire (3.2 + 2.4).
            CreateSign(root, square, titleFont, bodyFont, 19.5f, "DOBLE SALTO",
                Prompt.Of(new[] { "kb_space_icon", "x2" }, new[] { "xbox_a", "x2" }, new[] { "ps_cross", "x2" }));
            // Torre de arena; la banderita de arriba es adorno (no se pisa).
            CreateSandBlock("Muro (doble salto)", root, square, ArtImportBuilder.TowerPath, 25f, new Vector2(2.4f, ArtImportBuilder.TowerHeight), ground);

            // 4. Tablas de un sentido: escalera de tablas que se cruzan desde abajo.
            CreateSign(root, square, titleFont, bodyFont, 31f, "TABLAS", Prompt.Text("Se cruzan saltando desde abajo"));
            PlayerSandboxBuilder.CreatePlatform("Tabla 1", root, square, art, new Vector2(35f, 2.4f), 3f, ground);
            PlayerSandboxBuilder.CreatePlatform("Tabla 2", root, square, art, new Vector2(39f, 4.8f), 3f, ground);
            PlayerSandboxBuilder.CreatePlatform("Tabla 3", root, square, art, new Vector2(43.5f, 7.2f), 4f, ground);

            // 5. Escaleras: sube a un tablón alto.
            CreateSign(root, square, titleFont, bodyFont, 50f, "ESCALERAS",
                Prompt.Of(new[] { "kb_w", "kb_s" }, new[] { "xbox_stick_v" }, new[] { "ps_stick_v" }));
            PlayerSandboxBuilder.CreateLadder("Escalera", root, square, art, 54f, 0f, 6.2f, level.LadderLayer);
            PlayerSandboxBuilder.CreatePlatform("Tablón de la escalera", root, square, art, new Vector2(57.5f, 6f), 8f, ground);

            // 6. Golpear con el rodillo.
            CreateSign(root, square, titleFont, bodyFont, 64f, "GOLPEAR",
                Prompt.Of(new[] { "kb_j", "/", "mouse_left" }, new[] { "xbox_x" }, new[] { "ps_square" }));
            DressDummy(PlayerSandboxBuilder.CreateDummy(root, square, level.EnemyLayer, new Vector2(68.5f, 0.75f)));

            // 7. Lanzar masa: el muñeco está del otro lado de unas espinas.
            CreateSign(root, square, titleFont, bodyFont, 74f, "LANZAR MASA",
                Prompt.Of(new[] { "kb_k", "/", "mouse_right" }, new[] { "xbox_y" }, new[] { "ps_triangle" }));
            DressUrchins(PlayerSandboxBuilder.CreateHazard("Espinas", level, new Vector2(80.5f, 0.25f), new Vector2(4f, 0.5f)));
            DressDummy(PlayerSandboxBuilder.CreateDummy(root, square, level.EnemyLayer, new Vector2(85f, 0.75f)));

            // 8. Cocodrilo: camina entre dos bordillos (da la vuelta en ellos); Ulises los salta.
            CreateSign(root, square, titleFont, bodyFont, 90.5f, "¡COCODRILO!", Prompt.Text("Pégale de frente con el rodillo"));
            CreateSandBlock("Bordillo 1", root, square, ArtImportBuilder.CurbPath, 94f, new Vector2(1f, ArtImportBuilder.CurbHeight), ground);
            CreateSandBlock("Bordillo 2", root, square, ArtImportBuilder.CurbPath, 110f, new Vector2(1f, ArtImportBuilder.CurbHeight), ground);
            PlayerSandboxBuilder.CreateCrocodile(root, square, new Vector2(104f, 0f), level.EnemyLayer, ground, level.PlayerLayer);

            // 9. Meta.
            CreateSign(root, square, titleFont, bodyFont, 115f, "¡LISTO!", Prompt.Text("Ya conoces lo básico"));
            CreateGoal(root, square, new Vector2(120f, 2f), level.PlayerLayer);

            // ----- Fondo por capas: el arte del tutorial, o placeholders de color si aún no se importa -----
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.PaperPath) != null)
                CreateArtBackground(root, sand.GetComponent<SpriteRenderer>());
            else
                CreateBackground(square, circle);

            PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(0f, 1f), "Tutorial", MusicPath(),
                showControls: false, background: Paper);
            // La cámara no se asoma más allá de las paredes ni por debajo de la arena.
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(LeftEdge + 12.5f, 2.5f));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(RightEdge - 12.5f, 12f));

            PlayerSandboxBuilder.EndLevel(level);
            AddToBuildSettings();
            Debug.Log($"[Odisea] Tutorial creado en {ScenePath}");
        }

        // Música del tutorial; mientras no exista, la del nivel 1.
        private static string MusicPath()
        {
            string[] extensions = { ".ogg", ".mp3", ".wav" };
            string tutorial = MusicFolder + "musica_tutorial";
            return extensions.Any(ext => File.Exists(tutorial + ext)) ? tutorial : MusicFolder + "musica_nivel1";
        }

        // ---------- Letreros ----------

        // Alto máximo de los íconos de botones en el letrero y separación entre piezas de la fila.
        private const float MaxIconSize = 0.8f;
        // Lo que va detrás de la orilla de la arena (su base queda tapada, como enterrada): obstáculos y letreros.
        private const int BehindSandOrder = -6;
        private const float RowGap = 0.08f;

        /// <summary>
        /// Qué muestra un letrero debajo del título: una fila de botones por dispositivo (nombres de íconos de
        /// Art/UI/Botones; lo que no sea ícono, como "/" o "x2", va como texto) o una frase igual para todos.
        /// </summary>
        private sealed class Prompt
        {
            public string[] Keyboard, Xbox, PlayStation;
            public string Hint;

            public static Prompt Of(string[] keyboard, string[] xbox, string[] playStation) =>
                new() { Keyboard = keyboard, Xbox = xbox, PlayStation = playStation };

            public static Prompt Text(string hint) => new() { Hint = hint };
        }

        /// <summary>
        /// Letrero de madera clavado en la arena con el nombre de la acción y sus botones. Los botones cambian
        /// solos según se juegue con teclado, mando de Xbox o de PlayStation (igual que en Controles).
        /// </summary>
        private static void CreateSign(Transform parent, Sprite square, TMP_FontAsset titleFont, TMP_FontAsset bodyFont,
            float x, string title, Prompt prompt)
        {
            var sign = new GameObject($"Letrero {title}");
            sign.transform.SetParent(parent, false);
            sign.transform.position = new Vector3(x, 0f, 0f);

            // Cara del letrero (donde va lo escrito): la del dibujo, o la tabla de color si aún no se importa.
            Vector2 face, faceSize;
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.SignPath);
            if (art != null)
            {
                // El pivote del dibujo es el centro de su cara; su base de arena, hundida tras la orilla.
                float ppu = art.pixelsPerUnit;
                face = new Vector2(0f, art.pivot.y / ppu - ArtImportBuilder.ObstacleSink);
                faceSize = new Vector2(art.rect.width - art.border.x - art.border.z, art.rect.height - art.border.y - art.border.w) / ppu;
                var board = new GameObject("Tabla").AddComponent<SpriteRenderer>();
                board.transform.SetParent(sign.transform, false);
                board.transform.localPosition = face;
                board.sprite = art;
                board.sortingOrder = BehindSandOrder;
            }
            else
            {
                face = new Vector2(0f, 2.55f);
                faceSize = new Vector2(3.44f, 1.34f);
                Quad("Poste", sign.transform, square, new Vector2(0f, 0.9f), new Vector2(0.18f, 1.8f), PlayerSandboxBuilder.Cafe, 1);
                Quad("Borde", sign.transform, square, face, faceSize + new Vector2(0.16f, 0.16f), PlayerSandboxBuilder.Cafe, 1);
                Quad("Tabla", sign.transform, square, face, faceSize, PlayerSandboxBuilder.Crema, 2);
            }

            // Título en la mitad de arriba de la cara; botones (o la frase) en la de abajo.
            float lineWidth = faceSize.x * 0.9f;
            Text("Título", sign.transform, title, titleFont, face + new Vector2(0f, faceSize.y * 0.22f),
                new Vector2(lineWidth, faceSize.y * 0.42f), 6f);
            Vector2 row = face - new Vector2(0f, faceSize.y * 0.22f);
            if (prompt.Hint != null)
            {
                Text("Ayuda", sign.transform, prompt.Hint, bodyFont, row, new Vector2(lineWidth, faceSize.y * 0.36f), 3.6f);
                return;
            }

            float iconSize = Mathf.Min(MaxIconSize, faceSize.y * 0.52f);
            var switcher = sign.AddComponent<ControlsDiagram>();
            PlayerSandboxBuilder.SetSerialized(switcher, "keyboard",
                PromptRow("Teclado", sign.transform, prompt.Keyboard, bodyFont, row, iconSize));
            PlayerSandboxBuilder.SetSerialized(switcher, "xbox", PromptRow("Xbox", sign.transform, prompt.Xbox, bodyFont, row, iconSize));
            PlayerSandboxBuilder.SetSerialized(switcher, "playStation",
                PromptRow("PlayStation", sign.transform, prompt.PlayStation, bodyFont, row, iconSize));
        }

        // Fila centrada de íconos (teñidos de tinta café, como en Controles) y textos cortos.
        private static GameObject PromptRow(string name, Transform parent, string[] tokens, TMP_FontAsset font, Vector2 center,
            float iconSize)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.transform.localPosition = center;

            var pieces = new List<(Transform piece, float width)>();
            foreach (string token in tokens)
            {
                string path = $"{MenuScenesBuilder.UiFolder}/Botones/{token}.png";
                Sprite icon = File.Exists(path) ? MenuScenesBuilder.LoadUiSprite(path) : null;
                if (icon != null)
                {
                    var renderer = new GameObject(token).AddComponent<SpriteRenderer>();
                    renderer.transform.SetParent(row.transform, false);
                    renderer.sprite = icon;
                    renderer.color = PlayerSandboxBuilder.Cafe;
                    renderer.sortingOrder = 3;
                    float scale = iconSize / icon.bounds.size.y;
                    renderer.transform.localScale = new Vector3(scale, scale, 1f);
                    pieces.Add((renderer.transform, icon.bounds.size.x * scale));
                }
                else
                {
                    float width = 0.18f + 0.2f * token.Length;
                    TextMeshPro text = Text(token, row.transform, token, font, Vector2.zero, new Vector2(width, iconSize), 4f);
                    pieces.Add((text.transform, width));
                }
            }

            float total = pieces.Sum(p => p.width) + RowGap * (pieces.Count - 1);
            float left = -total / 2f;
            foreach ((Transform piece, float width) in pieces)
            {
                piece.localPosition = new Vector3(left + width / 2f, 0f, 0f);
                left += width + RowGap;
            }
            return row;
        }

        private static TextMeshPro Text(string name, Transform parent, string content, TMP_FontAsset font, Vector2 position,
            Vector2 size, float maxSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshPro>();
            text.text = content;
            text.font = font;
            text.color = PlayerSandboxBuilder.Cafe;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 1f;
            text.fontSizeMax = maxSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.sortingOrder = 3;
            return text;
        }

        private static SpriteRenderer Quad(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size,
            Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        // ---------- Obstáculos de arena ----------

        /// <summary>
        /// Bloque de arena dibujado sobre el suelo en x: el collider (del ancho del cuerpo del dibujo, sin la arena
        /// de la base) llega justo a su parte de arriba plana. Sin el arte importado, el bloque de color.
        /// </summary>
        private static void CreateSandBlock(string name, Transform parent, Sprite square, string artPath, float x,
            Vector2 size, int ground)
        {
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (art == null)
            {
                PlayerSandboxBuilder.CreateBlock(name, parent, square, new Vector2(x, size.y / 2f), size, SandDark, ground);
                return;
            }
            var block = new GameObject(name) { layer = ground };
            block.transform.SetParent(parent, false);
            block.transform.position = new Vector3(x, 0f, 0f);
            var box = block.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = new Vector2(0f, size.y / 2f);
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.sortingOrder = BehindSandOrder;
        }

        // ---------- Muñeco de práctica y erizos ----------

        /// <summary>
        /// Muñeco de práctica dibujado (si ya se importó), de la altura de Ulises y mirando hacia donde él llega:
        /// quieto, golpeado al recibir un golpe y noqueado al morir. Va delante de la orilla de la arena, con una
        /// sombra a sus pies para que no se pierda con el paisaje.
        /// </summary>
        private static void DressDummy(GameObject dummy)
        {
            Sprite[] frames = ArtImportBuilder.LoadStrip(ArtImportBuilder.DummyPath);
            if (frames.Length == 0)
                return;
            // El collider crece con el dibujo, con la base en el suelo (el centro del muñeco está a medio alto).
            var box = dummy.GetComponent<BoxCollider2D>();
            float ground = -box.size.y / 2f;
            box.size = new Vector2(1.1f, ArtImportBuilder.DummyHeight);
            box.offset = new Vector2(0f, ground + ArtImportBuilder.DummyHeight / 2f);

            Transform visual = dummy.transform.Find("Visual");
            visual.localScale = Vector3.one;
            visual.localPosition = new Vector3(0f, ground, 0f);
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.color = Color.white;
            renderer.flipX = true;
            renderer.sortingOrder = -3; // Delante de la orilla de la arena y de su sombra.

            var shadow = new GameObject("Sombra").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(dummy.transform, false);
            shadow.transform.localPosition = new Vector3(0f, ground + 0.03f, 0f);
            shadow.transform.localScale = new Vector3(1.5f, 0.28f, 1f);
            shadow.sprite = PlayerSandboxBuilder.LoadRequired<Sprite>(CirclePath);
            shadow.color = new Color(0.24f, 0.16f, 0.08f, 0.3f);
            shadow.sortingOrder = -4;

            var reaction = dummy.AddComponent<HitReactionSprites>();
            PlayerSandboxBuilder.SetSerialized(reaction, "health", dummy.GetComponent<Health>());
            PlayerSandboxBuilder.SetSerialized(reaction, "target", renderer);
            PlayerSandboxBuilder.SetSerialized(reaction, "idle", frames[0]);
            PlayerSandboxBuilder.SetSerialized(reaction, "hit", frames[Mathf.Min(1, frames.Length - 1)]);
            PlayerSandboxBuilder.SetSerialized(reaction, "knockedOut", frames[^1]);
            PlayerSandboxBuilder.SetSerialized(reaction, "faceLeft", true);
        }

        /// <summary>Erizos de mar sobre la franja que hace daño (el bloque rojo deja de pintarse).</summary>
        private static void DressUrchins(GameObject hazard)
        {
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.UrchinsPath);
            if (art == null)
                return;
            hazard.GetComponent<SpriteRenderer>().enabled = false;
            var urchins = new GameObject("Erizos").AddComponent<SpriteRenderer>();
            urchins.transform.SetParent(hazard.transform.parent, false);
            urchins.transform.position = new Vector3(hazard.transform.position.x, 0f, 0f);
            urchins.sprite = art;
            urchins.sortingOrder = -4; // Delante de la orilla de la arena: tienen que verse bien porque hacen daño.
        }

        // ---------- Meta ----------

        private static void CreateGoal(Transform parent, Sprite square, Vector2 center, int playerLayer)
        {
            var goal = new GameObject("Meta");
            goal.transform.SetParent(parent, false);
            goal.transform.position = center;
            var area = goal.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2f, 4f);
            var component = goal.AddComponent<LevelGoal>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            // Banderín placeholder.
            Quad("Asta", goal.transform, square, new Vector2(0f, 0f), new Vector2(0.15f, 4f), PlayerSandboxBuilder.Cafe, 2);
            Quad("Banderín", goal.transform, square, new Vector2(0.65f, 1.5f), new Vector2(1.2f, 0.8f), Sun, 2);
        }

        // ---------- Fondo por capas ----------

        /// <summary>
        /// Capas con paralaje, todavía de color. Al llegar el arte, cada una se cambia por su imagen
        /// (cielo, sol, horizonte, muelle y palmeras, primer plano); la arena es el suelo del nivel.
        /// </summary>
        private static void CreateBackground(Sprite square, Sprite circle)
        {
            var background = new GameObject("Fondo").transform;

            // Sol: acompaña a Ulises (pegado a la cámara), arriba a la derecha.
            Transform sun = Layer(background, "Capa sol", 1f, 1f);
            Quad("Sol", sun, circle, new Vector2(7f, 7.5f), new Vector2(3f, 3f), Sun, -50);

            // Horizonte: el mar y unas islas a lo lejos, se mueven muy poco.
            Transform horizon = Layer(background, "Capa horizonte", 0.9f, 0.7f);
            Quad("Mar", horizon, square, new Vector2(20f, 1.4f), new Vector2(90f, 2.6f), Sea, -40);
            Quad("Isla 1", horizon, circle, new Vector2(-3f, 2.6f), new Vector2(5f, 1.6f), Far, -41);
            Quad("Isla 2", horizon, circle, new Vector2(16f, 2.7f), new Vector2(7f, 2f), Far, -41);
            Quad("Cerro", horizon, circle, new Vector2(34f, 3f), new Vector2(9f, 3.2f), Far, -41);

            // Orilla: el muelle y palmeras, a media velocidad.
            Transform shore = Layer(background, "Capa muelle y palmeras", 0.5f, 0.35f);
            for (float x = 22f; x <= 44f; x += 2f)
                Quad("Pilote", shore, square, new Vector2(x, 1.4f), new Vector2(0.3f, 2.4f), Shore, -31);
            Quad("Muelle", shore, square, new Vector2(33f, 2.7f), new Vector2(24f, 0.35f), Shore, -30);
            foreach (float x in new[] { -6f, 4f, 12f, 52f, 60f, 68f })
            {
                Quad("Tronco", shore, square, new Vector2(x, 1.7f), new Vector2(0.35f, 3.4f), Shore, -31);
                Quad("Palmera", shore, circle, new Vector2(x, 3.5f), new Vector2(2.6f, 1.2f), Shore, -30);
            }

            // Primer plano: matas de pasto que pasan delante de Ulises, más rápido que el escenario.
            Transform front = Layer(background, "Capa primer plano", -0.3f, 0f);
            foreach (float x in new[] { 6f, 27f, 47f, 71f, 88f, 101f, 117f })
                // Hundido en la arena: solo se asoma la punta (centrado más abajo se veía como un hoyo).
                Quad("Pasto", front, circle, new Vector2(x, -0.55f), new Vector2(1.6f, 0.9f), Grass, 20);
        }

        /// <summary>
        /// Fondo con el arte (Art/Environment/Tutorial/Fondo): el papel pegado a la cámara, el horizonte casi
        /// quieto y la arena como suelo.
        /// </summary>
        private static void CreateArtBackground(Transform root, SpriteRenderer sandBlock)
        {
            var background = new GameObject("Fondo").transform;

            // Papel: cubre toda la vista y va con la cámara.
            Transform paper = Layer(background, "Capa papel", 1f, 1f);
            Piece("Papel", paper, ArtImportBuilder.PaperPath, new Vector2(2.5f, 2.5f), -60);

            // Horizonte: el mar, la isla del faro y el cerro con el fuerte; la línea del mar queda sobre la arena.
            Transform horizon = Layer(background, "Capa horizonte", 0.9f, 0.7f);
            Piece("Horizonte", horizon, ArtImportBuilder.HorizonPath, new Vector2(8f, 1f), -40);

            // Arena: tiras una junto a otra (con conchas de vez en cuando) sobre el bloque del suelo.
            Sprite[] sands =
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.SandPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.Sand2Path),
            };
            if (sands[0] != null)
            {
                var ground = new GameObject("Arena (dibujo)").transform;
                ground.SetParent(root, false);
                float step = sands[0].bounds.size.x;
                int index = 0;
                for (float x = LeftEdge - 2f; x - step / 2f < RightEdge + 2f; x += step, index++)
                {
                    Sprite sprite = index % 3 == 1 && sands[1] != null ? sands[1] : sands[0];
                    var tile = new GameObject($"Arena {index}").AddComponent<SpriteRenderer>();
                    tile.transform.SetParent(ground, false);
                    // Un pelito encimadas para que no se vea la costura.
                    tile.transform.localPosition = new Vector3(x, 0f, 0f);
                    tile.transform.localScale = new Vector3((step + 0.02f) / sprite.bounds.size.x, 1f, 1f);
                    tile.sprite = sprite;
                    tile.sortingOrder = -5;
                }
                // El bloque del suelo (solo collider) no se pinta: asomaba como una raya café por las ondas de la
                // orilla. Debajo de la arena dibujada va un relleno de su color, lejos de la orilla.
                sandBlock.enabled = false;
                Transform block = sandBlock.transform;
                Quad("Relleno de arena", root, sandBlock.sprite, new Vector2(block.position.x, -3.6f),
                    new Vector2(block.localScale.x, 4.8f), SandBottom, -8);
            }
        }

        private static SpriteRenderer Piece(string name, Transform parent, string path, Vector2 position, int order)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[Odisea] Falta {path}; corre Odisea > Arte > Importar arte de la sandbox.");
                return null;
            }
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.transform.localPosition = position;
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Transform Layer(Transform parent, string name, float followX, float followY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var parallax = go.AddComponent<ParallaxLayer>();
            PlayerSandboxBuilder.SetSerialized(parallax, "followX", followX);
            PlayerSandboxBuilder.SetSerialized(parallax, "followY", followY);
            return go.transform;
        }

        // ---------- Utilidades ----------

        // Círculo blanco de placeholder (sol, islas, copas de palmera).
        private static void EnsureCircleSprite()
        {
            if (File.Exists(CirclePath))
                return;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
                }
            }
            PlayerSandboxBuilder.EnsureFolder(Path.GetDirectoryName(CirclePath));
            File.WriteAllBytes(CirclePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(CirclePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(CirclePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.SaveAndReimport();
        }

        private static void AddToBuildSettings()
        {
            if (EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                return;
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        }
    }
}
