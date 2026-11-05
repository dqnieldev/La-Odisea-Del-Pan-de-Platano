using System.Linq;
using Odisea.Core;
using Odisea.Enemies;
using Odisea.Level;
using UnityEditor;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Nivel 1, El Mercado (Odisea > Niveles > Crear Mercado): nivel vertical sacado de estructura_nivel_1.jpg. Son dos
    /// torres de repisas con un hueco al centro; los pisos completos están en el suelo, a la mitad y arriba.
    /// Las repisas son firmes (no se atraviesan desde abajo): se sube por las escaleras (seguro y lento) o por las
    /// orillas del hueco con doble salto (rápido; si falla, cae varios pisos). Los ingredientes salen en lugares al azar
    /// cada vez que se juega (algunos escondidos o sobre el hueco) y la meta está en la cima (el racimo de plátano). No
    /// hay puntos de control: al morir se empieza desde abajo.
    /// Sin el arte del nivel, tablones, vigas y escaleras quedan de color.
    /// </summary>
    public static class MercadoBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Mercado.unity";
        private const string MusicPath = "Assets/_Project/Audio/Music/musica_nivel1";

        // Medidas del nivel (unidades). El suelo está en y = 0 y cada piso queda FloorGap más arriba.
        private const float FloorGap = 4f;
        private const int TopFloor = 10;
        private const float HalfWidth = 13.5f;
        // Vigas (columnas de las dos torres), como en el boceto.
        private static readonly float[] PostXs = { -11.2f, -4.8f, 5f, 11.1f };

        private static readonly Color Backdrop = new(0.93f, 0.87f, 0.76f);
        private static readonly Color Floor = new(0.545f, 0.353f, 0.169f);
        private static readonly Color Wall = new(0.36f, 0.24f, 0.15f);
        private static readonly Color BackWall = new(0.86f, 0.78f, 0.64f);

        // Repisas de cada piso (de dónde a dónde en x); los pisos 5 y 10 cruzan de lado a lado. Las orillas del hueco se
        // alternan (una más adentro, la de arriba más afuera) para que se pueda subir saltando junto a ellas.
        private static readonly (int floor, float from, float to)[] Shelves =
        {
            (1, -13f, -3f), (1, 3f, 13f),
            (2, -13f, -4.5f), (2, 4.5f, 13f),
            (3, -13f, -2.5f), (3, 3f, 13f),
            (4, -13f, -3.5f), (4, 2.5f, 13f),
            (5, -13f, 13f),
            (6, -13f, -3f), (6, 2.5f, 13f),
            (7, -13f, -4.5f), (7, 4.5f, 13f),
            (8, -13f, -2.5f), (8, 3f, 13f),
            (9, -13f, -4f), (9, 4f, 13f),
            (10, -13f, 13f),
        };
        // En los pisos completos, el tramo sobre el hueco se atraviesa desde abajo (la ruta por saltos sigue).
        private const float GapHalfWidth = 3f;
        // Ancho del tramo de paso encima de cada escalera.
        private const float HatchWidth = 1.6f;

        // Escaleras (x, del piso, al piso), entre las vigas para no quedar encimadas. Ruta: la torre izquierda hasta el piso 5, cruzar, y la derecha hasta arriba.
        // A cada torre le faltan escalones (hay que subir esos pisos con doble salto) y la izquierda de arriba no llega a
        // la cima: hay que saltar el hueco.
        private static readonly (float x, int from, int to)[] Ladders =
        {
            (-6.5f, 0, 1), (-6.5f, 1, 2), (-9f, 2, 3), (-6.5f, 3, 4), (-9f, 4, 5),
            (-6.5f, 5, 6), (-6.5f, 6, 7), (-9f, 8, 9),
            (7f, 0, 1), (7f, 1, 2), (9f, 3, 4), (7f, 4, 5),
            (7f, 5, 6), (9f, 6, 7), (7.5f, 7, 8), (9f, 8, 9), (7.5f, 9, 10),
        };

        // Lugares posibles de los ingredientes (x, piso, altura sobre el piso): en las orillas, sobre el hueco y en la torre
        // que no es la ruta. En cada partida salen IngredientCount de ellos, al azar.
        private static readonly (float x, int floor, float height)[] IngredientSpots =
        {
            (-12f, 1, 1f), (12f, 1, 1f), (0f, 2, 1.6f), (10f, 2, 1f), (-12f, 3, 1f), (12.2f, 3, 1f),
            (-3.8f, 4, 1f), (11.8f, 4, 1f), (0f, 5, 1f), (-12f, 5, 1f), (12f, 6, 1f), (-9f, 6, 1f),
            (0f, 7, 1.6f), (-12f, 7, 1f), (12f, 8, 1f), (-3.6f, 8, 1f), (-12f, 9, 1f), (11.8f, 9, 1f),
        };
        private const int IngredientCount = 8;

        // Secuaces de Circe (x, piso): uno cada uno o dos pisos, en las dos torres; se alternan cocinero y cocinera.
        private static readonly (float x, int floor)[] HenchmanSpots = { (-10f, 1), (10f, 3), (-10f, 4), (10.5f, 6), (10f, 8), (-8f, 9) };

        // Lugares posibles del ítem de ayuda (pescado, hoja o café, al azar): sale uno solo por partida.
        private static readonly (float x, int floor)[] HelperSpots = { (10f, 5), (-4f, 3), (11f, 7), (-6f, 8) };
        private static readonly PickupKind[] HelperKinds = { PickupKind.Heal, PickupKind.Shield, PickupKind.Speed };

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
            var floorBlock = PlayerSandboxBuilder.CreateBlock("Suelo", root, square, new Vector2(0f, -1f), new Vector2(HalfWidth * 2f, 2f), Floor, ground);
            // Con el arte de los puestos, el suelo es su empedrado: el bloque solo choca.
            bool stalls = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketFloorPath) != null;
            if (stalls)
                floorBlock.GetComponent<SpriteRenderer>().enabled = false;
            PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(-HalfWidth - 0.5f, top / 2f + 2f),
                new Vector2(1f, top + 8f), Wall, ground);
            PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(HalfWidth + 0.5f, top / 2f + 2f),
                new Vector2(1f, top + 8f), Wall, ground);
            CreateBackground(root, square, top, CameraMinY(stalls), top - 2f);

            // ----- Repisas, vigas y escaleras -----
            var art = PlayerSandboxBuilder.LevelArt.Load();
            foreach ((int floor, float from, float to) in Shelves)
            {
                bool full = to - from > 20f;
                string side = full ? "completo" : from < 0f ? "izquierda" : "derecha";
                // Pasos: encima de cada escalera que llega a este piso y, en los pisos completos, sobre el hueco. Las
                // orillas que dan al hueco terminan rotas; las de la pared, enteras.
                var passes = Ladders.Where(ladder => ladder.to == floor).Select(ladder => (ladder.x, HatchWidth / 2f)).ToList();
                if (full)
                    passes.Add((0f, GapHalfWidth));
                PlayerSandboxBuilder.CreateFirmPlatform($"Piso {floor} ({side})", root, square, art, from, to, Surface(floor), ground,
                    passes, brokenStart: !full && from > 0f, brokenEnd: !full && to < 0f);
            }
            if (art.Post != null)
            {
                foreach (float x in PostXs)
                    PlayerSandboxBuilder.CreatePost(root, art.Post, new Vector2(x, 0f), top - 0.4f, ground);
            }
            foreach ((float x, int from, int to) in Ladders)
                PlayerSandboxBuilder.CreateLadder($"Escalera {from}-{to}", root, square, art, x, Surface(from), Surface(to), level.LadderLayer);

            // ----- Ingredientes y el ítem de ayuda (en lugares al azar) -----
            var kinds = (PlayerSandboxBuilder.Ingredient[])System.Enum.GetValues(typeof(PlayerSandboxBuilder.Ingredient));
            var candidates = new Object[IngredientSpots.Length];
            for (int i = 0; i < IngredientSpots.Length; i++)
            {
                (float x, int floor, float height) = IngredientSpots[i];
                candidates[i] = PlayerSandboxBuilder.CreateIngredient(level, kinds[i % kinds.Length], new Vector2(x, Surface(floor) + height))
                    .GetComponent<Pickup>();
            }
            var spawner = new GameObject("Ingredientes al azar").AddComponent<RandomPickups>();
            PlayerSandboxBuilder.SetSerialized(spawner, "candidates", candidates);
            PlayerSandboxBuilder.SetSerialized(spawner, "keep", IngredientCount);
            // Ítem de ayuda: los tres tipos en cada lugar posible, y al jugar queda solo uno (tipo y lugar al azar).
            var helpers = new System.Collections.Generic.List<Object>();
            foreach ((float x, int floor) in HelperSpots)
            {
                foreach (PickupKind kind in HelperKinds)
                    helpers.Add(PlayerSandboxBuilder.CreatePickup(level, kind, new Vector2(x, Surface(floor) + 1f)).GetComponent<Pickup>());
            }
            var helperSpawner = new GameObject("Ítem de ayuda al azar").AddComponent<RandomPickups>();
            PlayerSandboxBuilder.SetSerialized(helperSpawner, "candidates", helpers.ToArray());
            PlayerSandboxBuilder.SetSerialized(helperSpawner, "keep", 1);

            // ----- Meta: el racimo de plátano en la cima -----
            CreateGoal(root, new Vector2(0f, top), level.PlayerLayer);

            GameObject player = PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(0f, 1f), "El Mercado", MusicPath,
                showControls: false, background: Backdrop);

            // ----- Enemigos: secuaces en las repisas y Madame Circe en el hueco -----
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            for (int i = 0; i < HenchmanSpots.Length; i++)
            {
                (float x, int floor) = HenchmanSpots[i];
                CreateHenchman(root, square, circle, new Vector2(x, Surface(floor)), i + 1, male: i % 2 == 0, level);
            }
            CreateCirce(root, square, circle, art, player.GetComponent<Health>(), top, level);
            // La cámara sigue hacia arriba sin asomarse fuera de las paredes ni debajo del suelo.
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            // Con los puestos, la cámara no baja más que el empedrado (tamaño 7: lo de abajo queda en y = -0.85).
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(-1f, CameraMinY(stalls)));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(1f, top - 2f));

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Mercado creado en {ScenePath}");
        }

        /// <summary>
        /// Fondo: la pared del mercado detrás de cada torre y los pisos lejanos en el hueco (los dos se repiten a lo
        /// alto), los puestos de la planta baja abajo y el techo con su tragaluz arriba. Sin el arte, la pared es un
        /// bloque de color.
        /// </summary>
        // Lo más abajo que baja la cámara: con los puestos, lo justo para no enseñar lo de abajo del empedrado.
        private static float CameraMinY(bool stalls) => stalls ? 6.15f : 5f;

        private static void CreateBackground(Transform root, Sprite square, float top, float cameraMinY, float cameraMaxY)
        {
            var parent = new GameObject("Fondo").transform;
            parent.SetParent(root, false);
            // Desde abajo del suelo hasta arriba de lo que alcanza a ver la cámara en la cima.
            const float bottom = -3f;
            float height = top + 12f - bottom;
            // El mercado entero en una imagen alta que sube con la cámara (trae su propio techo); si no está, una imagen
            // que se repite a lo alto, o la pared de cada torre y el hueco por separado.
            var tall = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketTallPath);
            if (tall != null)
            {
                var backdrop = Background("Mercado al fondo", parent, tall, new Vector2(0f, cameraMinY), null, -10);
                var scroll = backdrop.gameObject.AddComponent<TallBackdrop>();
                PlayerSandboxBuilder.SetSerialized(scroll, "cameraMinY", cameraMinY);
                PlayerSandboxBuilder.SetSerialized(scroll, "cameraMaxY", cameraMaxY);
            }
            else if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketBackPath) is { } back)
                Background("Pared del mercado", parent, back, new Vector2(0f, bottom + height / 2f),
                    new Vector2(ArtImportBuilder.MarketWideWidth, height), -10);
            else
                CreateWallsAndGap(parent, square, top, bottom, height);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketFloorPath) is { } floor)
                // A lo ancho de todo el mercado; la base de los cajones queda en el piso y el empedrado debajo.
                Background("Puestos", parent, floor, new Vector2(0f, -0.9f),
                    new Vector2(ArtImportBuilder.MarketWideWidth, floor.bounds.size.y), -9);
            if (tall == null && AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketRoofPath) is { } roof)
                // Arriba de lo que ve la cámara en la cima (sube hasta top - 2 con tamaño 7).
                Background("Techo", parent, roof, new Vector2(0f, top + 5.2f), null, -9);
        }

        // El fondo de antes: la pared de cada torre (o un bloque de color) y el hueco del centro.
        private static void CreateWallsAndGap(Transform parent, Sprite square, float top, float bottom, float height)
        {
            var wall = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketWallPath);
            foreach (float side in new[] { -1f, 1f })
            {
                string name = side < 0f ? "Pared izquierda" : "Pared derecha";
                if (wall != null)
                    Background(name, parent, wall, new Vector2(side * 8f, bottom + height / 2f),
                        new Vector2(ArtImportBuilder.MarketWallWidth, height), -10);
                else
                {
                    var back = PlayerSandboxBuilder.CreateBlock(name, parent, square, new Vector2(side * 8f, top / 2f),
                        new Vector2(10.5f, top), BackWall, 0);
                    Object.DestroyImmediate(back.GetComponent<BoxCollider2D>());
                    back.GetComponent<SpriteRenderer>().sortingOrder = -10;
                }
            }
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.MarketGapPath) is { } gap)
                Background("Hueco", parent, gap, new Vector2(0f, bottom + height / 2f), new Vector2(ArtImportBuilder.MarketGapWidth, height), -11);
        }

        // Una capa del fondo; con tamaño, el dibujo se repite (Tiled) para llenarlo.
        private static SpriteRenderer Background(string name, Transform parent, Sprite sprite, Vector2 position, Vector2? size, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.transform.position = position;
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (size is { } tiled)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = tiled;
            }
            return renderer;
        }

        // ---------- Enemigos: los secuaces de Circe y Madame Circe ----------

        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemigos";
        private static readonly Color ChefNavy = new(0.16f, 0.2f, 0.36f);
        private static readonly Color CircePurple = new(0.42f, 0.2f, 0.48f);
        private static readonly Color Skin = new(0.87f, 0.68f, 0.52f);
        private static readonly Color Peel = new(0.98f, 0.82f, 0.25f);
        private static readonly Color Crate = new(0.66f, 0.45f, 0.24f);

        /// <summary>
        /// Secuaz de Circe: cocinero con cuchara (male) o cocinera con batidor. Camina de orilla a orilla de su repisa
        /// (PatrolEnemy, como el cocodrilo), quita un corazón al tocarlo y, si tiene a Ulises enfrente, se detiene y le
        /// pega con el utensilio. Cae con 2 golpes y cuenta para el puntaje. Sin su arte, una figura azul marino.
        /// </summary>
        private static void CreateHenchman(Transform parent, Sprite square, Sprite circle, Vector2 feet, int number, bool male,
            PlayerSandboxBuilder.LevelContext level)
        {
            var henchman = new GameObject($"Secuaz de Circe {number} ({(male ? "cocinero" : "cocinera")})") { layer = level.EnemyLayer };
            henchman.transform.SetParent(parent, false);
            henchman.transform.position = feet;
            var body = henchman.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var collider = henchman.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.9f, 1.9f);
            collider.offset = new Vector2(0f, 0.95f);

            var visual = new GameObject("Visual") { layer = level.EnemyLayer };
            visual.transform.SetParent(henchman.transform, false);
            var flash = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.HenchmanControllerPath(male));
            SpriteRenderer[] renderers;
            Animator animator = null;
            if (controller != null)
            {
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(ArtImportBuilder.HenchmanWalkPath(male)).OfType<Sprite>()
                    .OrderBy(sprite => sprite.name.Length).ThenBy(sprite => sprite.name, System.StringComparer.Ordinal).FirstOrDefault();
                renderer.sharedMaterial = flash;
                renderer.sortingOrder = 6;
                animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                renderers = new[] { renderer };
            }
            else
            {
                renderers = new[]
                {
                    Part("Cuerpo", visual.transform, square, new Vector2(0f, 0.75f), new Vector2(0.8f, 1.5f), ChefNavy, 6, flash),
                    Part("Cabeza", visual.transform, circle, new Vector2(0.05f, 1.7f), new Vector2(0.5f, 0.5f), Skin, 7, flash),
                };
            }

            var health = henchman.AddComponent<Health>();
            PlayerSandboxBuilder.SetSerialized(health, "maxHealth", 2);
            PlayerSandboxBuilder.SetSerialized(health, "invulnerabilityTime", 0.2f);
            PlayerSandboxBuilder.SetSerialized(health, "destroyOnDeath", false); // Lo destruye DamageFeedback.
            var hazard = henchman.AddComponent<Hazard>();
            PlayerSandboxBuilder.SetSerialized(hazard, "targetLayers", 1 << level.PlayerLayer);
            var patrol = henchman.AddComponent<PatrolEnemy>();
            PlayerSandboxBuilder.SetSerialized(patrol, "groundLayers", 1 << level.GroundLayer);
            PlayerSandboxBuilder.SetSerialized(patrol, "speed", 2f);
            PlayerSandboxBuilder.SetSerialized(patrol, "health", health);
            PlayerSandboxBuilder.SetSerialized(patrol, "startDirection", number % 2 == 0 ? 1 : -1);
            if (animator != null)
            {
                // El ataque con el utensilio: alcanza un poco adelante, a la altura del pecho de Ulises.
                PlayerSandboxBuilder.SetSerialized(patrol, "animator", animator);
                PlayerSandboxBuilder.SetSerialized(patrol, "biteTargets", 1 << level.PlayerLayer);
                PlayerSandboxBuilder.SetSerialized(patrol, "biteOffset", new Vector2(0.9f, 0.9f));
                PlayerSandboxBuilder.SetSerialized(patrol, "biteSize", new Vector2(1.2f, 1.4f));
                PlayerSandboxBuilder.SetSerialized(patrol, "lungeDistance", 0.3f);
                PlayerSandboxBuilder.SetSerialized(patrol, "biteCooldown", 1.5f);
                PlayerSandboxBuilder.SetSerialized(patrol, "counterBiteFromBehind", false);
                // Si Ulises lo toca y eso le quita vida, se voltea y hace el golpe (que se vea que fue él).
                PlayerSandboxBuilder.SetSerialized(patrol, "swingOnContact", true);
                // Si ve a Ulises enfrente en su repisa, se le va encima más rápido.
                PlayerSandboxBuilder.SetSerialized(patrol, "chaseSpeed", 4.2f);
                var hurtClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.HenchmanClipPath(male, "Hurt"));
                if (hurtClip != null)
                    PlayerSandboxBuilder.SetSerialized(patrol, "hurtStun", hurtClip.length);
            }
            // Efectos de sonido (Audio/SFX/Secuaz): el golpe cuando lo suelta (no al echar el brazo: si Ulises lo
            // derriba antes, no suena) y la burla cuando le pega a Ulises.
            var sounds = henchman.AddComponent<PatrolEnemySounds>();
            PlayerSandboxBuilder.SetSerialized(sounds, "enemy", patrol);
            PlayerSandboxBuilder.SetSerialized(sounds, "health", health);
            PlayerSandboxBuilder.SetSerialized(sounds, "contact", hazard);
            GameSoundsBuilder.SetSound(sounds, "biteSnap", GameSoundsBuilder.Load(GameSoundsBuilder.HenchmanFolder, "secuaz_golpe"), 0.8f);
            GameSoundsBuilder.SetSound(sounds, "hitTarget", GameSoundsBuilder.Load(GameSoundsBuilder.HenchmanFolder, "secuaz_risa"), 1f);

            var feedback = henchman.AddComponent<DamageFeedback>();
            PlayerSandboxBuilder.SetSerialized(feedback, "health", health);
            PlayerSandboxBuilder.SetSerialized(feedback, "visual", visual.transform);
            PlayerSandboxBuilder.SetSerialized(feedback, "renderers", renderers.Cast<Object>().ToArray());
            PlayerSandboxBuilder.SetSerialized(feedback, "shrinkAndDestroyOnDeath", true);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashColor", Color.white);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashDuration", 0.2f);
            var deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.HenchmanClipPath(male, "Die"));
            if (deathClip != null)
                PlayerSandboxBuilder.SetSerialized(feedback, "deathAnimationTime", deathClip.length);
        }

        // Lo que miden las sogas sobre la tablita: alcanzan más allá de lo que se ve de la cima.
        private const float RopeLength = 20f;
        // Dónde queda la orilla de arriba de la tabla del columpio respecto a los pies de Circe (un poco arriba: así se
        // ve parada sobre la cara de la tabla).
        private const float SwingSurface = 0.08f;

        /// <summary>
        /// Madame Circe: flota en el hueco del centro, parada en una tablita colgada de dos cuerdas, a la altura de
        /// Ulises, y le avienta cajas y cáscaras (<see cref="CirceThrower"/>). Sin su dibujo, una figura morada.
        /// </summary>
        private static void CreateCirce(Transform parent, Sprite square, Sprite circle, PlayerSandboxBuilder.LevelArt art,
            Health player, float top, PlayerSandboxBuilder.LevelContext level)
        {
            var circe = new GameObject("Madame Circe");
            circe.transform.SetParent(parent, false);
            circe.transform.position = new Vector3(0f, 4f, 0f);

            // Tablita y cuerdas (no se mueven con el aviso ni se voltean). Con el dibujo del columpio, la tabla con sus
            // nudos; si no, un tablón del nivel.
            var swing = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.CirceSwingPath);
            float ropeBase = 0f, ropeSide = 0.8f;
            if (swing != null)
            {
                var plank = new GameObject("Columpio").AddComponent<SpriteRenderer>();
                plank.transform.SetParent(circe.transform, false);
                plank.transform.localPosition = new Vector3(0f, SwingSurface, 0f);
                plank.sprite = swing;
                plank.sortingOrder = 4;
                ropeBase = SwingSurface + swing.bounds.max.y;
                ropeSide = ArtImportBuilder.CirceSwingRopeSpacing / 2f;
            }
            else if (art.Plank != null)
            {
                var plank = new GameObject("Tablita").AddComponent<SpriteRenderer>();
                plank.transform.SetParent(circe.transform, false);
                plank.transform.localPosition = new Vector3(0f, -0.15f, 0f);
                plank.sprite = art.Plank;
                plank.drawMode = SpriteDrawMode.Tiled;
                plank.size = new Vector2(1.8f, art.Plank.bounds.size.y);
                plank.sortingOrder = 4;
            }
            // Las sogas suben fuera de la vista; con su dibujo se repite el trenzado, sin él una raya café.
            var rope = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.CirceRopePath);
            foreach (float side in new[] { -ropeSide, ropeSide })
            {
                if (rope != null)
                {
                    var cord = new GameObject("Cuerda").AddComponent<SpriteRenderer>();
                    cord.transform.SetParent(circe.transform, false);
                    cord.transform.localPosition = new Vector3(side, ropeBase + RopeLength / 2f, 0f);
                    cord.sprite = rope;
                    cord.drawMode = SpriteDrawMode.Tiled;
                    cord.size = new Vector2(rope.bounds.size.x, RopeLength);
                    cord.sortingOrder = 2;
                }
                else
                    Part("Cuerda", circe.transform, square, new Vector2(side, RopeLength / 2f), new Vector2(0.06f, RopeLength),
                        new Color(0.45f, 0.33f, 0.2f), 2, null);
            }

            var visual = new GameObject("Dibujo").transform;
            visual.SetParent(circe.transform, false);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.CirceControllerPath);
            Animator animator = null;
            var hand = new GameObject("Mano").transform;
            hand.SetParent(visual, false);
            if (controller != null)
            {
                var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(ArtImportBuilder.CirceIdlePath).OfType<Sprite>()
                    .OrderBy(sprite => sprite.name.Length).ThenBy(sprite => sprite.name, System.StringComparer.Ordinal).FirstOrDefault();
                renderer.sortingOrder = 5;
                animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                // Donde queda la mano al soltar (cuadro 5 de "lanzar"): adelante, a la altura del hombro.
                hand.localPosition = new Vector3(0.6f, 1.6f, 0f);
            }
            else
            {
                Part("Vestido", visual, square, new Vector2(0f, 0.75f), new Vector2(0.9f, 1.5f), CircePurple, 5, null);
                Part("Cabeza", visual, circle, new Vector2(0f, 1.75f), new Vector2(0.55f, 0.55f), Skin, 6, null);
                Part("Chongo", visual, circle, new Vector2(0f, 2.1f), new Vector2(0.4f, 0.32f), new Color(0.2f, 0.12f, 0.1f), 5, null);
                hand.localPosition = new Vector3(0f, 1.3f, 0f);
            }

            var thrower = circe.AddComponent<CirceThrower>();
            if (animator != null)
            {
                PlayerSandboxBuilder.SetSerialized(thrower, "animator", animator);
                // El aviso dura lo que tarda en llegar al cuadro en que suelta; y va un poco más abajo (mide más que la
                // figura de relleno) para que lo que avienta no choque con la repisa de arriba.
                PlayerSandboxBuilder.SetSerialized(thrower, "windUp", ArtImportBuilder.CirceReleaseFrame / ArtImportBuilder.CirceThrowFps);
                PlayerSandboxBuilder.SetSerialized(thrower, "heightAbovePlayer", -0.4f);
            }
            PlayerSandboxBuilder.SetSerialized(thrower, "player", player);
            PlayerSandboxBuilder.SetSerialized(thrower, "visual", visual);
            PlayerSandboxBuilder.SetSerialized(thrower, "hand", hand);
            PlayerSandboxBuilder.SetSerialized(thrower, "maxHeight", top);
            GameSoundsBuilder.SetSound(thrower, "laughSound", GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "circe_risa"), 0.8f);
            PlayerSandboxBuilder.SetSerialized(thrower, "cratePrefab",
                EnsureThrownPrefab("Caja", ThrownKind.Crate, square, Crate, new Vector2(0.6f, 0.6f), level));
            PlayerSandboxBuilder.SetSerialized(thrower, "peelPrefab",
                EnsureThrownPrefab("Cascara", ThrownKind.Peel, square, Peel, new Vector2(0.65f, 0.18f), level));
        }

        // Caja o cáscara como prefab (se instancia al aventarla). Con su arte, el dibujo de Art/Characters/Circe (la
        // caja rota o la cáscara en el piso como segundo dibujo); sin él, un cuadro de color.
        private static ThrownItem EnsureThrownPrefab(string name, ThrownKind kind, Sprite square, Color color, Vector2 size,
            PlayerSandboxBuilder.LevelContext level)
        {
            string path = $"{EnemyPrefabFolder}/{name}.prefab";
            if (!AssetDatabase.IsValidFolder(EnemyPrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Enemigos");
            bool crate = kind == ThrownKind.Crate;
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(crate ? ArtImportBuilder.CirceCratePath : ArtImportBuilder.CircePeelPath);
            var second = AssetDatabase.LoadAssetAtPath<Sprite>(crate ? ArtImportBuilder.CirceBrokenCratePath : ArtImportBuilder.CirceLandedPeelPath);
            if (art != null)
                // Un poco más chico que el dibujo, para que no pegue con las puntas; la cáscara, del ancho de la tirada.
                size = crate ? (Vector2)art.bounds.size * 0.85f : new Vector2((second != null ? second.bounds.size.x : art.bounds.size.x) * 0.8f, 0.2f);

            var root = new GameObject(name) { layer = level.EnemyLayer };
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            // La cáscara se apoya por abajo (su pivote es la base); la caja gira sobre su centro.
            box.size = size;
            box.offset = crate ? Vector2.zero : new Vector2(0f, size.y / 2f);
            var visual = new GameObject("Dibujo") { layer = level.EnemyLayer };
            visual.transform.SetParent(root.transform, false);
            if (!crate)
                visual.transform.localPosition = new Vector3(0f, art != null ? art.bounds.extents.y : size.y / 2f, 0f);
            if (art != null)
            {
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = art;
                renderer.sortingOrder = 10;
            }
            else
            {
                Part("Contorno", visual.transform, square, Vector2.zero, size + new Vector2(0.08f, 0.08f),
                    PlayerSandboxBuilder.Cafe, 9, null);
                Part("Relleno", visual.transform, square, Vector2.zero, size, color, 10, null);
            }
            var item = root.AddComponent<ThrownItem>();
            PlayerSandboxBuilder.SetSerialized(item, "kind", (int)kind);
            PlayerSandboxBuilder.SetSerialized(item, "visual", visual.transform);
            PlayerSandboxBuilder.SetSerialized(item, "playerLayers", 1 << level.PlayerLayer);
            PlayerSandboxBuilder.SetSerialized(item, "groundLayers", 1 << level.GroundLayer);
            if (second != null)
                PlayerSandboxBuilder.SetSerialized(item, crate ? "brokenSprite" : "landedSprite", second);
            GameSoundsBuilder.SetSound(item, crate ? "breakSound" : "slipSound",
                GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, crate ? "caja_golpe" : "resbalon"), 0.8f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ThrownItem>();
        }

        private static SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color,
            int order, Material material)
        {
            var part = new GameObject(name) { layer = parent.gameObject.layer }.AddComponent<SpriteRenderer>();
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(size.x, size.y, 1f);
            part.sprite = sprite;
            part.color = color;
            part.sortingOrder = order;
            if (material != null)
                part.sharedMaterial = material;
            return part;
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
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.GoldenBunchPath) is { } bunch)
            {
                var art = new GameObject("Racimo").AddComponent<SpriteRenderer>();
                art.transform.SetParent(goal.transform, false);
                art.sprite = bunch;
                art.sortingOrder = 5;
                glow.transform.localScale = Vector3.one * 3.2f;
            }
            else for (int i = 0; i < 3; i++)
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
