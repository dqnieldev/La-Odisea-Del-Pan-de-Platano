using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Odisea.Core;
using Odisea.Enemies;
using Odisea.Level;
using Odisea.Player;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Crea la escena Sandbox/PlayerSandbox para probar a Ulises: suelo, plataformas a distintas alturas,
    /// escaleras, un peligro, un muñeco para golpear y un cocodrilo. Usa el arte importado con
    /// Odisea > Arte > Importar arte de la sandbox; lo que no exista se dibuja con cuadros de colores.
    /// Menú: Odisea > Sandbox > Crear escena de prueba del jugador.
    /// </summary>
    public static class PlayerSandboxBuilder
    {
        private const string BaseScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/PlayerSandbox.unity";
        internal const string SpritePath = "Assets/_Project/Art/Placeholders/Cuadro.png";
        private const string HeartFullPath = "Assets/_Project/Art/Placeholders/CorazonLleno.png";

        // Corazones del HUD dibujados en código (lisos, con contorno café); se rehacen si cambia HeartTextureWidth.
        private const int HeartTextureWidth = 256;
        private const int HeartTextureHeight = 236;
        // Viñeta roja de la última vida y anillo dorado al curar.
        private const string VignettePath = "Assets/_Project/Art/Placeholders/Vineta.png";
        private const string RingPath = "Assets/_Project/Art/Placeholders/Anillo.png";
        private const string MaterialPath = "Assets/_Project/Settings/SinFriccion.physicsMaterial2D";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Player/UlisesConfig.asset";
        private const string InputPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Colores del design system (Docs/Referencias/Paleta).
        private static readonly Color Platano = new(0.961f, 0.718f, 0.192f);
        internal static readonly Color Cafe = new(0.169f, 0.118f, 0.086f);
        internal static readonly Color Crema = new(0.984f, 0.973f, 0.949f);
        private static readonly Color Manglar = new(0.180f, 0.353f, 0.267f);
        private static readonly Color Chile = new(0.784f, 0.263f, 0.169f);
        private static readonly Color Mar = new(0.184f, 0.561f, 0.627f);
        private static readonly Color Madera = new(0.545f, 0.353f, 0.169f);

        [MenuItem("Odisea/Sandbox/Crear escena de prueba del jugador")]
        public static void Build()
        {
            LevelContext level = BeginLevel(ScenePath);
            Sprite square = level.Square;
            int groundLayer = level.GroundLayer, playerLayer = level.PlayerLayer, enemyLayer = level.EnemyLayer,
                ladderLayer = level.LadderLayer;

            CreateBlock("Suelo", level.Root, square, new Vector2(10f, -1f), new Vector2(60f, 2f), Manglar, groundLayer);
            CreateBlock("Pared izquierda", level.Root, square, new Vector2(-20.5f, 5f), new Vector2(1f, 14f), Manglar, groundLayer);
            CreateBlock("Pared derecha", level.Root, square, new Vector2(40.5f, 5f), new Vector2(1f, 14f), Manglar, groundLayer);
            // Madera = plataformas de un sentido: se suben saltando desde abajo o por escalera, y no se puede bajar de ellas
            // más que por escalera o cayendo por la orilla. Café = bloques sólidos.
            var art = LevelArt.Load();
            // Las vigas se saltan donde hay escalera, para no quedar una encima de la otra.
            art.LadderXs = new[] { 5.5f, -13f, -15.5f };
            CreatePlatform("Plataforma baja", level.Root, square, art, new Vector2(4f, 2f), 4f, groundLayer);
            CreatePlatform("Plataforma media", level.Root, square, art, new Vector2(10f, 4.5f), 3f, groundLayer);
            CreatePlatform("Plataforma doble salto", level.Root, square, art, new Vector2(16f, 8f), 3f, groundLayer, posts: false);
            CreatePlatform("Repisa 1", level.Root, square, art, new Vector2(-14f, 2.5f), 4f, groundLayer);
            CreatePlatform("Repisa 2", level.Root, square, art, new Vector2(-14f, 5f), 4f, groundLayer, posts: false);
            CreatePlatform("Repisa 3", level.Root, square, art, new Vector2(-16f, 7.5f), 4f, groundLayer, posts: false);
            CreateBlock("Escalón", level.Root, square, new Vector2(-8f, 0.75f), new Vector2(3f, 1.5f), Cafe, groundLayer);

            // Escaleras: abajo = suelo donde empiezan, arriba = superficie de la plataforma donde terminan.
            CreateLadder("Escalera a plataforma baja", level.Root, square, art, 5.5f, 0f, 2.2f, ladderLayer);
            CreateLadder("Escalera de la torre", level.Root, square, art, -13f, 0f, 5.2f, ladderLayer);
            CreateLadder("Escalera a repisa 3", level.Root, square, art, -15.5f, 5.2f, 7.7f, ladderLayer);

            CreateHazard("Peligro (resta 1 vida)", level, new Vector2(24f, 0.25f), new Vector2(3f, 0.5f));
            // Ítems en fila antes del peligro, para probarlos con calma.
            CreatePickup(level, PickupKind.Heal, new Vector2(14f, 1f));
            CreatePickup(level, PickupKind.Shield, new Vector2(16.5f, 1f));
            CreatePickup(level, PickupKind.Speed, new Vector2(19f, 1f));
            // Ingredientes del puntaje: uno en el suelo y otro arriba de la torre.
            CreateIngredient(level, Ingredient.Canela, new Vector2(7f, 1f));
            CreateIngredient(level, Ingredient.Platano, new Vector2(-15.5f, 8.7f));
            CreateDummy(level.Root, square, enemyLayer, new Vector2(-4f, 0.75f));
            CreateCrocodile(level.Root, square, new Vector2(30f, 0f), enemyLayer, groundLayer, playerLayer);

            SetUpPlayer(level, new Vector2(0f, 1f), "Escena de prueba", LevelMusicPath, showControls: true);
            EndLevel(level);
            Debug.Log($"[Odisea] Escena de prueba creada en {ScenePath}");
        }

        /// <summary>Lo que comparten los generadores de niveles: capas, assets base y la escena abierta.</summary>
        internal sealed class LevelContext
        {
            public UnityEngine.SceneManagement.Scene Scene;
            public string ScenePath;
            public Transform Root;
            public int GroundLayer, PlayerLayer, EnemyLayer, LadderLayer;
            public Sprite Square;
            public PhysicsMaterial2D NoFriction;
            public PlayerConfig Config;
            public InputActionAsset InputActions;
            public Sprite FullHeart;
            public Projectile DoughBall;
        }

        /// <summary>
        /// Prepara una escena de nivel: capas, assets base (placeholders, material sin fricción, configuración de
        /// Ulises, bolita de masa) y la escena vacía (solo cámara y luz), conservando su GUID si ya existía.
        /// </summary>
        internal static LevelContext BeginLevel(string scenePath)
        {
            int groundLayer = EnsureLayer("Ground", 6);
            int playerLayer = EnsureLayer("Player", 7);
            int enemyLayer = EnsureLayer("Enemy", 8);
            int ladderLayer = EnsureLayer("Ladder", 9);

            EnsurePlaceholderSprite();
            EnsureHeartSprite(HeartFullPath, Chile, full: true);
            EnsureVignetteSprite();
            EnsureRingSprite();
            EnsureNoFrictionMaterial();
            EnsureFlashMaterial();
            EnsureSwooshSprite();
            TutorialBuilder.EnsureCircleSprite();
            GameSoundsBuilder.EnsureFolders();
            EnsurePlayerConfig(groundLayer, enemyLayer, ladderLayer);
            // Guardar antes de abrir la escena: OpenScene descarga los assets nuevos que no están en disco
            // y las referencias a ellos quedarían vacías.
            AssetDatabase.SaveAssets();

            EnsureFolder(Path.GetDirectoryName(scenePath));
            if (File.Exists(scenePath))
            {
                // Se reemplaza solo el contenido para conservar el .meta (y su GUID, al que apunta Build Settings).
                File.Copy(BaseScenePath, scenePath, overwrite: true);
                AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
            }
            else if (!AssetDatabase.CopyAsset(BaseScenePath, scenePath))
                throw new InvalidOperationException($"No se pudo copiar {BaseScenePath}.");

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ClearSceneExcept(scene, "Main Camera", "Global Light 2D");

            var context = new LevelContext
            {
                Scene = scene,
                ScenePath = scenePath,
                Root = new GameObject("Nivel").transform,
                GroundLayer = groundLayer,
                PlayerLayer = playerLayer,
                EnemyLayer = enemyLayer,
                LadderLayer = ladderLayer,
                Square = LoadRequired<Sprite>(SpritePath),
                NoFriction = LoadRequired<PhysicsMaterial2D>(MaterialPath),
                Config = LoadRequired<PlayerConfig>(ConfigPath),
                InputActions = LoadRequired<InputActionAsset>(InputPath),
                FullHeart = LoadRequired<Sprite>(HeartFullPath),
            };
            context.DoughBall = EnsureProjectilePrefab(context.Square, playerLayer);
            return context;
        }

        /// <summary>
        /// Ulises con su bolita de masa, el HUD, la música del nivel (se apaga al morir), el reinicio al morir,
        /// el menú de pausa y la cámara que lo sigue. Con <paramref name="scored"/>, el puntaje del nivel y su HUD (el
        /// tutorial y la panadería no llevan).
        /// </summary>
        internal static GameObject SetUpPlayer(LevelContext level, Vector2 spawn, string levelName, string musicPath,
            bool showControls, Color? background = null, bool scored = true)
        {
            GameObject player = CreatePlayer(level.Square, level.NoFriction, level.Config, level.InputActions, level.PlayerLayer);
            player.transform.position = spawn;
            SetSerialized(player.GetComponent<PlayerCombat>(), "projectilePrefab", level.DoughBall);
            // Puntaje del nivel (ingredientes, tiempo, enemigos, sin daño) y su HUD arriba a la derecha.
            LevelScore score = null;
            if (scored)
            {
                score = new GameObject("Puntaje").AddComponent<LevelScore>();
                SetSerialized(score, "player", player.GetComponent<Health>());
            }
            CreateHud(player.GetComponent<Health>(), player.GetComponent<PlayerBuffs>(), level.FullHeart,
                showControls, score);

            // Música de fondo; se apaga rápido al morir Ulises para que se oiga su sonido de muerte.
            SceneMusic music = GameSoundsBuilder.CreateMusic("Música del nivel", musicPath);
            if (music != null)
                SetSerialized(music, "fadeOnDeath", player.GetComponent<Health>());

            // Al quedarse sin corazones: Game Over (Reintentar, Reiniciar nivel o salir).
            MenuScenesBuilder.CreateGameOverScreen(player.GetComponent<Health>(), spawn);

            // Menú de pausa (Esc / Start). Lo genera Odisea > Menús > Crear menú de pausa.
            var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/MenuPausa.prefab");
            if (pausePrefab != null)
            {
                var pause = (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab);
                var serialized = new SerializedObject(pause.GetComponent<PauseMenu>());
                serialized.FindProperty("levelName").stringValue = levelName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[Odisea] Falta el prefab del menú de pausa; genéralo con Odisea > Menús > Crear menú de pausa.");
            }

            var camera = GameObject.Find("Main Camera");
            camera.transform.position = new Vector3(spawn.x, spawn.y + 1f, -10f);
            var cam = camera.GetComponent<Camera>();
            cam.orthographicSize = 7f;
            cam.backgroundColor = background ?? Crema;
            var follow = camera.AddComponent<CameraFollow2D>();
            follow.Target = player.transform;
            return player;
        }

        internal static void EndLevel(LevelContext level)
        {
            EditorSceneManager.MarkSceneDirty(level.Scene);
            EditorSceneManager.SaveScene(level.Scene);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Bloque rojo que resta una vida al tocarlo.</summary>
        internal static GameObject CreateHazard(string name, LevelContext level, Vector2 center, Vector2 size)
        {
            GameObject hazard = CreateBlock(name, level.Root, level.Square, center, size, Chile, level.GroundLayer);
            SetSerialized(hazard.AddComponent<Hazard>(), "targetLayers", 1 << level.PlayerLayer);
            return hazard;
        }

        /// <summary>Punto de entrada para Unity en modo batch (-executeMethod).</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static GameObject CreatePlayer(Sprite square, PhysicsMaterial2D material, PlayerConfig config,
            InputActionAsset inputActions, int playerLayer)
        {
            var player = new GameObject("Ulises") { layer = playerLayer };
            player.transform.position = new Vector3(0f, 1f, 0f);

            var body = player.AddComponent<Rigidbody2D>();
            body.sharedMaterial = material;
            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);

            // El sprite va en un hijo para poder cambiar el placeholder por el arte real sin tocar la física.
            var visual = new GameObject("Visual") { layer = playerLayer };
            visual.transform.SetParent(player.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            var renderers = new System.Collections.Generic.List<UnityEngine.Object> { renderer };
            var ulisesController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.UlisesControllerPath);
            Animator spriteAnimator = null;
            if (ulisesController != null)
            {
                // Arte real: el pivote de cada cuadro está en los pies, así que el visual va en la base del collider,
                // un poco hundido: la física deja ~1.5 cm entre el collider y el suelo, y así el zapato pisa el contorno.
                visual.transform.localPosition = new Vector3(0f, -0.8f - FeetSink, 0f);
                renderer.sprite = FirstSprite("Assets/_Project/Art/Characters/Ulises/Ulises_Idle.png");
                spriteAnimator = visual.AddComponent<Animator>();
                spriteAnimator.runtimeAnimatorController = ulisesController;
            }
            else
            {
                visual.transform.localScale = new Vector3(0.8f, 1.6f, 1f);
                renderer.sprite = square;
                renderer.color = Platano;

                // "Nariz" para ver hacia dónde mira mientras no hay sprite real.
                var nose = new GameObject("Frente") { layer = playerLayer };
                nose.transform.SetParent(player.transform, false);
                nose.transform.localPosition = new Vector3(0.35f, 0.4f, 0f);
                nose.transform.localScale = new Vector3(0.3f, 0.2f, 1f);
                var noseRenderer = nose.AddComponent<SpriteRenderer>();
                noseRenderer.sprite = square;
                noseRenderer.color = Cafe;
                noseRenderer.sortingOrder = 11;
                renderers.Add(noseRenderer);
            }

            var groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(player.transform, false);
            groundCheck.localPosition = new Vector3(0f, -0.8f, 0f);

            var input = player.AddComponent<PlayerInputReader>();
            SetSerialized(input, "actions", inputActions);

            var health = player.AddComponent<Health>();
            SetSerialized(health, "maxHealth", 3);
            SetSerialized(health, "invulnerabilityTime", 1.2f);

            var controller = player.AddComponent<PlayerController>();
            SetSerialized(controller, "config", config);
            SetSerialized(controller, "input", input);
            SetSerialized(controller, "groundCheck", groundCheck);
            SetSerialized(controller, "health", health);

            var combat = player.AddComponent<PlayerCombat>();
            SetSerialized(combat, "input", input);

            var animator = player.AddComponent<PlayerAnimator>();
            SetSerialized(animator, "controller", controller);
            SetSerialized(animator, "combat", combat);
            SetSerialized(animator, "health", health);
            SetSerialized(animator, "spriteRenderer", renderer);
            if (spriteAnimator != null)
                SetSerialized(animator, "animator", spriteAnimator);

            var feedback = player.AddComponent<DamageFeedback>();
            SetSerialized(feedback, "health", health);
            SetSerialized(feedback, "visual", visual.transform);
            SetSerialized(feedback, "renderers", renderers.ToArray());
            SetSerialized(feedback, "flashColor", Chile);
            SetSerialized(feedback, "rumble", true);

            // Efectos de sonido (Audio/SFX/Ulises); los que falten no suenan.
            var sounds = player.AddComponent<PlayerSounds>();
            SetSerialized(sounds, "combat", combat);
            SetSerialized(sounds, "health", health);
            SetSerialized(sounds, "controller", controller);
            if (spriteAnimator != null)
                SetSerialized(sounds, "animator", spriteAnimator);
            string ulises = GameSoundsBuilder.UlisesFolder;
            GameSoundsBuilder.SetSound(sounds, "attack", GameSoundsBuilder.Load(ulises, "golpe_rodillo"));
            // Los impactos de las armas suenan en cada acierto: más bajos, para no tapar la queja del enemigo.
            // Con varias variantes se emparejan a la más bajita: se compensa con volumen completo, para no perderse
            // con el "fiu" del rodillo.
            AudioClip[] hits = GameSoundsBuilder.Load(ulises, "golpe_acierta");
            GameSoundsBuilder.SetSound(sounds, "attackHit", hits, hits.Length > 1 ? 1f : ImpactVolume);
            GameSoundsBuilder.SetSound(sounds, "finisher", GameSoundsBuilder.Load(ulises, "golpe_remate"));
            GameSoundsBuilder.SetSound(sounds, "jump", GameSoundsBuilder.Load(ulises, "salto"), 0.7f);
            GameSoundsBuilder.SetSound(sounds, "doubleJump", GameSoundsBuilder.Load(ulises, "doble_salto"), 0.7f);
            GameSoundsBuilder.SetSound(sounds, "land", GameSoundsBuilder.Load(ulises, "aterrizar"), 0.5f);
            // Los pasos suenan muy seguido: bajitos, para que acompañen sin cansar.
            GameSoundsBuilder.SetSound(sounds, "footstep", GameSoundsBuilder.Load(ulises, "paso"), 0.3f);
            GameSoundsBuilder.SetSound(sounds, "throwDough", GameSoundsBuilder.Load(ulises, "lanzar_masa"));
            GameSoundsBuilder.SetSound(sounds, "hurt", GameSoundsBuilder.Load(ulises, "dano"));
            GameSoundsBuilder.SetSound(sounds, "death", GameSoundsBuilder.Load(ulises, "muerte"));

            CreateAttackVisual(player, combat, square, config, playerLayer, showBar: ulisesController == null);
            CreateBuffs(player, controller, playerLayer);
            return player;
        }

        // ---------- Ítems ----------

        // Lo que dan los ítems (se ajusta aquí y se vuelve a generar la escena).
        private const float ShieldSeconds = 10f;
        private const float SpeedMultiplier = 1.5f;
        private const float SpeedSeconds = 6f;
        private const string UlisesArtFolder = "Assets/_Project/Art/Characters/Ulises";
        // Mientras no exista Audio/SFX/Items/recoger, al tomar un ítem suena el brillo del logo.
        private const string PickupFallbackSound = "Assets/_Project/Audio/SFX/UI/logo_brillo.mp3";

        private static readonly Color FishColor = new(0.902f, 0.494f, 0.180f);
        private static readonly Color LeafColor = new(0.353f, 0.604f, 0.259f);
        private static readonly Color CoffeeColor = new(0.420f, 0.259f, 0.141f);

        private static (string name, string artPath, Color color, string label) ItemInfo(PickupKind kind) => kind switch
        {
            PickupKind.Heal => ("Pescado zarandeado", ArtImportBuilder.FishPath, FishColor, "+1 vida"),
            PickupKind.Shield => ("Hoja de plátano", ArtImportBuilder.LeafPath, LeafColor, "¡Escudo!"),
            PickupKind.Ingredient => IngredientInfo(Ingredient.Platano),
            _ => ("Café de olla", ArtImportBuilder.CoffeePath, CoffeeColor, "¡Rápido!"),
        };

        private static string ItemSoundName(PickupKind kind) => kind switch
        {
            PickupKind.Heal => "pescado",
            PickupKind.Shield => "hoja",
            PickupKind.Ingredient => "ingrediente",
            _ => "cafe",
        };

        // El arte del ítem (Art/Items) o, mientras no exista, un círculo de su color.
        private static (Sprite sprite, Color color, bool isArt) ItemSprite(PickupKind kind)
        {
            (_, string artPath, Color color, _) = ItemInfo(kind);
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            return art != null ? (art, Color.white, true) : (LoadRequired<Sprite>(TutorialBuilder.CirclePath), color, false);
        }

        /// <summary>
        /// Ítem que flota sobre el suelo con un brillo detrás: pescado zarandeado (cura 1 vida), hoja de plátano
        /// (escudo) o café de olla (velocidad). Lo recoge Ulises al tocarlo.
        /// </summary>
        internal static GameObject CreatePickup(LevelContext level, PickupKind kind, Vector2 center)
        {
            (string name, _, _, string label) = ItemInfo(kind);
            var item = new GameObject(name);
            item.transform.SetParent(level.Root, false);
            item.transform.position = center;
            item.AddComponent<CircleCollider2D>().radius = 0.55f;

            var visual = new GameObject("Dibujo").transform;
            visual.SetParent(item.transform, false);
            var glow = new GameObject("Brillo").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(visual, false);
            glow.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            glow.sprite = LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            glow.color = new Color(1f, 0.93f, 0.6f, 0.55f);
            glow.sortingOrder = 4;
            var drawing = new GameObject("Ítem").AddComponent<SpriteRenderer>();
            drawing.transform.SetParent(visual, false);
            (Sprite sprite, Color color, bool isArt) = ItemSprite(kind);
            drawing.sprite = sprite;
            drawing.color = color;
            drawing.sortingOrder = 5;
            if (!isArt)
                drawing.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            var pickup = item.AddComponent<Pickup>();
            SetSerialized(pickup, "kind", (int)kind);
            SetSerialized(pickup, "amount", kind == PickupKind.Speed ? SpeedMultiplier : 1f);
            SetSerialized(pickup, "duration", kind == PickupKind.Shield ? ShieldSeconds : SpeedSeconds);
            SetSerialized(pickup, "playerLayers", 1 << level.PlayerLayer);
            SetSerialized(pickup, "visual", visual);
            SetSerialized(pickup, "label", label);
            SetSerialized(pickup, "font", UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold));
            // Cada ítem con su sonido (recoger_pescado, recoger_hoja, recoger_cafe); si falta, "recoger" y luego el brillo.
            AudioClip[] sound = GameSoundsBuilder.Load(GameSoundsBuilder.ItemsFolder, $"recoger_{ItemSoundName(kind)}");
            if (sound.Length == 0)
                sound = GameSoundsBuilder.Load(GameSoundsBuilder.ItemsFolder, "recoger");
            if (sound.Length == 0 && AssetDatabase.LoadAssetAtPath<AudioClip>(PickupFallbackSound) is { } fallback)
                sound = new[] { fallback };
            GameSoundsBuilder.SetSound(pickup, "sound", sound, 0.6f);
            return item;
        }

        // ---------- Ingredientes del pan de plátano (puntaje) ----------

        internal enum Ingredient
        {
            Canela,
            Azucar,
            Harina,
            Platano,
        }

        private static (string name, string artPath, Color color, string label) IngredientInfo(Ingredient ingredient) => ingredient switch
        {
            Ingredient.Canela => ("Canela", ArtImportBuilder.IngredientsFolder + "/Canela.png", new Color(0.627f, 0.369f, 0.204f), "+Canela"),
            Ingredient.Azucar => ("Azúcar de caña", ArtImportBuilder.IngredientsFolder + "/Azucar.png", new Color(0.851f, 0.596f, 0.322f), "+Azúcar"),
            Ingredient.Harina => ("Harina", ArtImportBuilder.IngredientsFolder + "/Harina.png", new Color(0.957f, 0.941f, 0.902f), "+Harina"),
            _ => ("Plátano", ArtImportBuilder.IngredientsFolder + "/Platano.png", new Color(0.961f, 0.718f, 0.192f), "+Plátano"),
        };

        private static (Sprite sprite, Color color, bool isArt) IngredientSprite(Ingredient ingredient)
        {
            (_, string artPath, Color color, _) = IngredientInfo(ingredient);
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            return art != null ? (art, Color.white, true) : (LoadRequired<Sprite>(TutorialBuilder.CirclePath), color, false);
        }

        /// <summary>
        /// Ingrediente del pan de plátano (canela, azúcar de caña, harina o plátano): flota como los demás ítems y
        /// suma al puntaje del nivel. Sin su dibujo (Art/Items/Ingredientes), un círculo de su color con contorno.
        /// </summary>
        internal static GameObject CreateIngredient(LevelContext level, Ingredient ingredient, Vector2 center)
        {
            GameObject item = CreatePickup(level, PickupKind.Ingredient, center);
            (string name, _, _, string label) = IngredientInfo(ingredient);
            item.name = $"Ingrediente {name}";
            SetSerialized(item.GetComponent<Pickup>(), "label", label);
            var drawing = item.transform.Find("Dibujo/Ítem").GetComponent<SpriteRenderer>();
            (Sprite sprite, Color color, bool isArt) = IngredientSprite(ingredient);
            drawing.sprite = sprite;
            drawing.color = color;
            drawing.transform.localScale = isArt ? Vector3.one : new Vector3(0.6f, 0.6f, 1f);
            if (!isArt)
            {
                var outline = new GameObject("Contorno").AddComponent<SpriteRenderer>();
                outline.transform.SetParent(drawing.transform.parent, false);
                outline.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                outline.sprite = sprite;
                outline.color = Cafe;
                outline.sortingOrder = 4;
            }
            return item;
        }

        /// <summary>
        /// Bandeja café arriba a la derecha: el ingrediente con "recogidos/total" y el cronómetro. Se acomoda sola
        /// al ancho (sin ingredientes en el nivel queda solo el reloj).
        /// </summary>
        private static void CreateScoreHud(Transform canvas, LevelScore score)
        {
            TMP_FontAsset font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            var tray = new GameObject("Puntaje", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            var rect = (RectTransform)tray.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-48f, -48f);
            rect.sizeDelta = new Vector2(300f, 76f);
            var background = tray.GetComponent<Image>();
            background.sprite = UiAssetsBuilder.EnsureRoundedSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(Cafe.r, Cafe.g, Cafe.b, 0.85f);
            background.raycastTarget = false;
            var layout = tray.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 26, 8, 8);
            layout.spacing = 30f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            tray.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            (Sprite sprite, Color color, _) = IngredientSprite(Ingredient.Platano);
            (GameObject ingredients, Image icon, TMP_Text count) = CreateHudEntry(rect, "Ingredientes", sprite, color, font, "0/0", 90f);
            (_, Image clock, TMP_Text time) = CreateHudEntry(rect, "Tiempo", UiAssetsBuilder.EnsureRingSprite(), Crema, font, "0:00", 100f);
            clock.rectTransform.localScale = Vector3.one * 0.75f;

            var hud = tray.AddComponent<ScoreHud>();
            SetSerialized(hud, "score", score);
            SetSerialized(hud, "ingredientsGroup", ingredients);
            SetSerialized(hud, "ingredientsIcon", icon.rectTransform);
            SetSerialized(hud, "ingredientsText", count);
            SetSerialized(hud, "timeText", time);
        }

        // Ícono y texto de la bandeja del puntaje. El texto tiene ancho fijo para que la bandeja no baile cada segundo.
        private static (GameObject entry, Image icon, TMP_Text text) CreateHudEntry(RectTransform tray, string name, Sprite sprite,
            Color color, TMP_FontAsset font, string sample, float textWidth)
        {
            var entry = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)entry.transform;
            rect.SetParent(tray, false);
            var layout = entry.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var iconObject = new GameObject("Ícono", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(rect, false);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.color = color;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var element = iconObject.GetComponent<LayoutElement>();
            element.preferredWidth = element.preferredHeight = 48f;

            var textObject = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(rect, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = sample;
            text.font = font;
            text.fontSize = 44f;
            text.color = Crema;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            textObject.GetComponent<LayoutElement>().preferredWidth = textWidth;
            return (entry, icon, text);
        }

        // Escudo: hojitas que giran alrededor de Ulises (ShieldLeaves). Café: siluetas, polvo y vapor (SpeedTrail).
        private static void CreateBuffs(GameObject player, PlayerController controller, int layer)
        {
            Sprite circle = LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            var buffs = player.AddComponent<PlayerBuffs>();
            SetSerialized(buffs, "controller", controller);
            SetSerialized(player.GetComponent<PlayerAnimator>(), "buffs", buffs);

            (Sprite leafSprite, Color leafColor, bool leafIsArt) = ItemSprite(PickupKind.Shield);
            var leaves = new UnityEngine.Object[3];
            for (int i = 0; i < leaves.Length; i++)
            {
                var leaf = new GameObject($"Hojita {i + 1}") { layer = layer }.AddComponent<SpriteRenderer>();
                leaf.transform.SetParent(player.transform, false);
                leaf.sprite = leafSprite;
                leaf.color = leafColor;
                leaves[i] = leaf;
            }
            var shield = player.AddComponent<ShieldLeaves>();
            SetSerialized(shield, "buffs", buffs);
            SetSerialized(shield, "leaves", leaves);
            // Sin el dibujo de la hoja, el círculo de color es más grande que el dibujo: se achica.
            SetSerialized(shield, "leafSize", leafIsArt ? 0.6f : 0.35f);

            var trail = player.AddComponent<SpeedTrail>();
            SetSerialized(trail, "buffs", buffs);
            SetSerialized(trail, "controller", controller);
            SetSerialized(trail, "body", player.transform.Find("Visual").GetComponent<SpriteRenderer>());
            SetSerialized(trail, "puff", circle);

            // Tomar café: si Ulises no tiene su animación dibujada (Ulises_Drink), un jarrito que se empina frente a él.
            if (!File.Exists($"{UlisesArtFolder}/Ulises_Drink.anim"))
            {
                var mug = new GameObject("Jarrito") { layer = layer }.AddComponent<SpriteRenderer>();
                mug.transform.SetParent(player.transform, false);
                (Sprite sprite, Color color, bool isArt) = ItemSprite(PickupKind.Speed);
                mug.sprite = sprite;
                mug.color = color;
                // Sin el dibujo del café, un óvalo café (alargado, para que se note cuando se empina).
                mug.transform.localScale = isArt ? new Vector3(0.6f, 0.6f, 1f) : new Vector3(0.28f, 0.4f, 1f);
                mug.sortingOrder = 12;
                mug.enabled = false;
                SetSerialized(buffs, "drinkProp", mug);
            }
            GameSoundsBuilder.SetSound(buffs, "shieldBreak", GameSoundsBuilder.Load(GameSoundsBuilder.ItemsFolder, "romper_escudo"));
        }

        /// <summary>Debajo de los corazones: una casilla por ventaja activa (escudo, café), con un anillo que se vacía.</summary>
        private static void CreateBuffsDisplay(Transform canvas, PlayerBuffs buffs)
        {
            var row = new GameObject("Ventajas", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)row.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(48f, -130f);
            rect.sizeDelta = new Vector2(300f, 72f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var display = row.AddComponent<BuffsDisplay>();
            SetSerialized(display, "buffs", buffs);
            (GameObject shieldSlot, Image shieldTime) = CreateBuffSlot(rect, PickupKind.Shield);
            (GameObject speedSlot, Image speedTime) = CreateBuffSlot(rect, PickupKind.Speed);
            SetSerialized(display, "shieldSlot", shieldSlot);
            SetSerialized(display, "shieldTime", shieldTime);
            SetSerialized(display, "speedSlot", speedSlot);
            SetSerialized(display, "speedTime", speedTime);
        }

        private static (GameObject slot, Image time) CreateBuffSlot(RectTransform row, PickupKind kind)
        {
            Sprite circle = LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            var slot = new GameObject(ItemInfo(kind).name, typeof(RectTransform));
            var rect = (RectTransform)slot.transform;
            rect.SetParent(row, false);
            rect.sizeDelta = new Vector2(72f, 72f);

            UiCircle("Fondo", rect, circle, new Color(Cafe.r, Cafe.g, Cafe.b, 0.85f), 72f);
            Image time = UiCircle("Tiempo", rect, circle, Platano, 72f);
            time.type = Image.Type.Filled;
            time.fillMethod = Image.FillMethod.Radial360;
            time.fillOrigin = (int)Image.Origin360.Top;
            time.fillClockwise = false;
            UiCircle("Centro", rect, circle, Cafe, 58f);
            (Sprite sprite, Color color, bool isArt) = ItemSprite(kind);
            Image icon = UiCircle("Ícono", rect, sprite, color, isArt ? 44f : 32f);
            icon.preserveAspect = true;

            slot.SetActive(false);
            return (slot, time);
        }

        private static Image UiCircle(string name, RectTransform parent, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateAttackVisual(GameObject player, PlayerCombat combat, Sprite square, PlayerConfig config, int layer,
            bool showBar)
        {
            // Rodillo: el pivote es la mano; la barra sale hacia adelante y gira hacia abajo.
            var pivot = new GameObject("Rodillo") { layer = layer };
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0.25f, 0.15f, 0f);

            var bar = new GameObject("Barra") { layer = layer };
            bar.transform.SetParent(pivot.transform, false);
            bar.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            bar.transform.localScale = new Vector3(1f, 0.22f, 1f);
            var barRenderer = bar.AddComponent<SpriteRenderer>();
            barRenderer.enabled = showBar;
            barRenderer.sprite = square;
            barRenderer.color = Madera;
            barRenderer.sortingOrder = 12;

            // Estela: media luna del tamaño exacto del área que daña el golpe (PlayerConfig.attackOffset / attackSize).
            // Su pivote está a la izquierda (lado de Ulises) para que crezca hacia adelante; su orilla de enfrente
            // marca hasta dónde llega el golpe.
            var trail = new GameObject("Estela") { layer = layer };
            trail.transform.SetParent(player.transform, false);
            trail.transform.localPosition = new Vector3(config.attackOffset.x - config.attackSize.x / 2f, config.attackOffset.y, 0f);
            trail.transform.localScale = new Vector3(config.attackSize.x, config.attackSize.y, 1f);
            var trailRenderer = trail.AddComponent<SpriteRenderer>();
            trailRenderer.sprite = LoadRequired<Sprite>(SwooshPath);
            trailRenderer.color = Color.white; // Los colores vienen en la textura; el alfa lo anima PlayerAttackVisual.
            trailRenderer.sortingOrder = 9;

            var attackVisual = player.AddComponent<PlayerAttackVisual>();
            SetSerialized(attackVisual, "combat", combat);
            SetSerialized(attackVisual, "swingPivot", pivot.transform);
            SetSerialized(attackVisual, "trail", trailRenderer);
        }

        internal static GameObject CreateDummy(Transform parent, Sprite square, int enemyLayer, Vector2 center)
        {
            var dummy = new GameObject("Muñeco de práctica") { layer = enemyLayer };
            dummy.transform.SetParent(parent, false);
            dummy.transform.position = center;
            var collider = dummy.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1.5f);
            collider.isTrigger = true;

            var visual = new GameObject("Visual") { layer = enemyLayer };
            visual.transform.SetParent(dummy.transform, false);
            visual.transform.localScale = new Vector3(1f, 1.5f, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = Mar;
            renderer.sortingOrder = 5;

            var health = dummy.AddComponent<Health>();
            SetSerialized(health, "maxHealth", 3);
            SetSerialized(health, "invulnerabilityTime", 0.2f);
            SetSerialized(health, "destroyOnDeath", false); // Lo destruye DamageFeedback tras la animación.

            var feedback = dummy.AddComponent<DamageFeedback>();
            SetSerialized(feedback, "health", health);
            SetSerialized(feedback, "visual", visual.transform);
            SetSerialized(feedback, "renderers", new UnityEngine.Object[] { renderer });
            SetSerialized(feedback, "shrinkAndDestroyOnDeath", true);
            return dummy;
        }

        private static void CreateHud(Health playerHealth, PlayerBuffs buffs, Sprite fullHeart, bool showControls, LevelScore score)
        {
            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Design system: HUD anclado a 48 px de los bordes, vidas arriba a la izquierda.
            var lives = new GameObject("Vidas", typeof(RectTransform));
            var rect = (RectTransform)lives.transform;
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(48f, -48f);
            rect.sizeDelta = new Vector2(300f, 80f);

            // Orillas rojas de la última vida: detrás de todo el HUD.
            var vignette = new GameObject("Última vida", typeof(RectTransform), typeof(Image));
            var vignetteRect = (RectTransform)vignette.transform;
            vignetteRect.SetParent(canvasObject.transform, false);
            vignetteRect.SetAsFirstSibling();
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = vignetteRect.offsetMax = Vector2.zero;
            var vignetteImage = vignette.GetComponent<Image>();
            vignetteImage.sprite = LoadRequired<Sprite>(VignettePath);
            vignetteImage.color = new Color(Chile.r, Chile.g * 0.6f, Chile.b * 0.6f, 0f);
            vignetteImage.raycastTarget = false;
            vignetteImage.enabled = false;

            var hearts = lives.AddComponent<HeartsDisplay>();
            SetSerialized(hearts, "health", playerHealth);
            SetSerialized(hearts, "fullHeart", fullHeart);
            SetSerialized(hearts, "shardSprite", LoadRequired<Sprite>(TutorialBuilder.CirclePath));
            SetSerialized(hearts, "ringSprite", LoadRequired<Sprite>(RingPath));
            SetSerialized(hearts, "lowHealthVignette", vignetteImage);
            var heartSettings = new SerializedObject(hearts);
            heartSettings.FindProperty("heartSize").vector2Value = new Vector2(70f, 64f);
            heartSettings.FindProperty("spacing").floatValue = 10f;
            heartSettings.FindProperty("shardColor").colorValue = Chile;
            heartSettings.FindProperty("ringColor").colorValue = Platano;
            heartSettings.ApplyModifiedPropertiesWithoutUndo();
            CreateBuffsDisplay(canvasObject.transform, buffs);
            if (score != null)
                CreateScoreHud(canvasObject.transform, score);

            if (showControls)
                CreateControlsPanel(canvasObject.transform);
        }

        private static void CreateControlsPanel(Transform canvas)
        {
            // Bandeja café (design system), arriba a la derecha. Más opaca que el 78% para leerse sobre fondo crema.
            var panel = new GameObject("Controles", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-48f, -150f); // Debajo del puntaje.
            rect.sizeDelta = new Vector2(660f, 330f);
            var background = panel.GetComponent<Image>();
            background.color = new Color(Cafe.r, Cafe.g, Cafe.b, 0.92f);
            background.raycastTarget = false;

            var textObject = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(panel.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 20f);
            textRect.offsetMax = new Vector2(-24f, -20f);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.lineSpacing = 1.15f;
            text.color = Crema;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.text =
                "<b><color=#F5B731>CONTROLES</color></b>\n" +
                "Moverse:  A / D  ·  flechas  ·  stick / cruceta\n" +
                "Saltar:  Espacio  ·  A / Cruz   (2 veces = doble salto)\n" +
                "Escaleras:  arriba / abajo  (saltar para soltarte)\n" +
                "Golpear:  J  ·  clic  ·  X / Cuadrado\n" +
                "Lanzar masa:  K  ·  clic derecho  ·  Y / Triángulo\n" +
                "Pausa y reiniciar:  Esc  ·  Start\n" +
                "<color=#E8E0D2>Rojo = peligro · Azul = muñeco (3 golpes) · Cocodrilo (2 golpes)</color>";
        }

        internal static void CreateLadder(string name, Transform parent, Sprite square, LevelArt art, float x, float bottom, float top, int layer)
        {
            float height = top - bottom;
            var ladder = new GameObject(name) { layer = layer };
            ladder.transform.SetParent(parent, false);
            ladder.transform.position = new Vector3(x, bottom + height / 2f, 0f);
            var area = ladder.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(0.9f, height);
            ladder.AddComponent<Odisea.Level.Ladder>();

            if (art.Ladder != null)
            {
                // El dibujo es más angosto que el área para trepar: se escala parejo hasta ese ancho y se repite a lo alto.
                float scale = area.size.x / art.Ladder.bounds.size.x;
                var drawing = new GameObject("Dibujo") { layer = layer };
                drawing.transform.SetParent(ladder.transform, false);
                drawing.transform.localScale = new Vector3(scale, scale, 1f);
                var ladderRenderer = drawing.AddComponent<SpriteRenderer>();
                ladderRenderer.sprite = art.Ladder;
                ladderRenderer.drawMode = SpriteDrawMode.Tiled;
                ladderRenderer.tileMode = SpriteTileMode.Continuous;
                ladderRenderer.size = new Vector2(art.Ladder.bounds.size.x, height / scale);
                ladderRenderer.sortingOrder = 1; // Detrás de Ulises y de las plataformas.
                return;
            }

            var wood = new Color(0.71f, 0.50f, 0.28f);
            foreach (float side in new[] { -0.38f, 0.38f })
                CreateLadderPiece("Larguero", ladder.transform, square, new Vector2(side, 0f), new Vector2(0.12f, height), wood);
            for (float y = -height / 2f + 0.3f; y < height / 2f; y += 0.45f)
                CreateLadderPiece("Peldaño", ladder.transform, square, new Vector2(0f, y), new Vector2(0.76f, 0.08f), wood);
        }

        private static void CreateLadderPiece(string name, Transform parent, Sprite square, Vector2 position, Vector2 size, Color color)
        {
            var piece = new GameObject(name) { layer = parent.gameObject.layer };
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = 1; // Detrás de Ulises y de las plataformas.
        }

        private static Projectile EnsureProjectilePrefab(Sprite square, int layer)
        {
            const string path = "Assets/_Project/Prefabs/Player/BolitaDeMasa.prefab";
            EnsureFolder(Path.GetDirectoryName(path));

            var root = new GameObject("BolitaDeMasa") { layer = layer };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.isTrigger = true;

            var visual = new GameObject("Visual") { layer = layer };
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 11;
            var projectile = root.AddComponent<Projectile>();
            SetSerialized(projectile, "visual", visual.transform);

            Sprite[] flying = ArtImportBuilder.LoadStrip(ArtImportBuilder.DoughFlyPath);
            Sprite[] splat = ArtImportBuilder.LoadStrip(ArtImportBuilder.DoughImpactPath);
            if (flying.Length > 0)
            {
                // Arte: la bolita ya trae el giro y las rayitas dibujados; no gira, se voltea según la dirección.
                renderer.sprite = flying[0];
                var flipbook = visual.AddComponent<SpriteFlipbook>();
                SetSerialized(flipbook, "target", renderer);
                SetSerialized(flipbook, "frames", flying);
                SetSerialized(flipbook, "framesPerSecond", 14f);
                SetSerialized(projectile, "spinSpeed", 0f);
                if (splat.Length > 0)
                    SetSerialized(projectile, "impactPrefab", EnsureImpactPrefab(splat, layer));
            }
            else
            {
                visual.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
                renderer.sprite = square;
                renderer.color = new Color(0.86f, 0.66f, 0.38f); // Masa tostada.
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return LoadRequired<GameObject>(path).GetComponent<Projectile>();
        }

        /// <summary>Sprites del nivel 1 (los que no existan quedan en null y se usan cuadros de colores).</summary>
        internal sealed class LevelArt
        {
            public Sprite Plank;
            public Sprite BrokenPlank;
            /// <summary>Tablón roto por la izquierda (BrokenPlank está roto por la derecha).</summary>
            public Sprite BrokenPlank2;
            /// <summary>Tablón roto de los dos lados.</summary>
            public Sprite BrokenBoth;
            public Sprite Post;
            public Sprite Ladder;
            public float[] LadderXs = Array.Empty<float>();

            /// <summary>Piezas de una carpeta de nivel (por defecto la del nivel 1); faltantes = null.</summary>
            public static LevelArt Load(string folder = null)
            {
                folder ??= Path.GetDirectoryName(ArtImportBuilder.PlankPath).Replace('\\', '/');
                return new LevelArt
                {
                    Plank = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/Tablon.png"),
                    BrokenPlank = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/TablonRoto1.png"),
                    BrokenPlank2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/TablonRoto2.png"),
                    BrokenBoth = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/TablonRotoAmbos.png"),
                    Post = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/Viga.png"),
                    Ladder = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/Escalera.png"),
                };
            }
        }

        private const float PlatformThickness = 0.4f;
        // Velocidad del cocodrilo; si cambia, ajustar también ArtImportBuilder.CrocodileWalkFps (las patas).
        private const float CrocodileSpeed = 2.6f;
        private const string LevelMusicPath = "Assets/_Project/Audio/Music/musica_nivel1";
        // Volumen de los impactos de las armas (rodillo que acierta, bolita que choca).
        private const float ImpactVolume = 0.55f;
        // Cuánto se dibuja a Ulises por debajo de su collider (margen de contacto de la física + grosor del contorno).
        private const float FeetSink = 0.03f;

        /// <summary>
        /// Plataforma de un sentido. Con arte: tablón repetido a lo ancho (las cortas usan el tablón roto)
        /// y, si se pide, vigas que la sostienen desde el suelo. Sin arte: el cuadro café de siempre.
        /// </summary>
        internal static GameObject CreatePlatform(string name, Transform parent, Sprite square, LevelArt art, Vector2 center,
            float width, int layer, bool posts = true)
        {
            Sprite plank = width <= 3f && art.BrokenPlank != null ? art.BrokenPlank : art.Plank;
            if (plank == null)
            {
                GameObject block = CreateBlock(name, parent, square, center, new Vector2(width, PlatformThickness), Madera, layer);
                MakeOneWay(block);
                return block;
            }

            var platform = new GameObject(name) { layer = layer };
            platform.transform.SetParent(parent, false);
            platform.transform.position = center;
            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, PlatformThickness);
            MakeOneWay(platform);

            // La cara de arriba del tablón coincide con la del collider (donde pisa Ulises).
            float plankHeight = plank.bounds.size.y;
            var drawing = new GameObject("Tablón") { layer = layer };
            drawing.transform.SetParent(platform.transform, false);
            // El PNG trae un borde vacío arriba: se sube eso para que la madera dibujada quede justo donde se pisa.
            float emptyTop = ArtImportBuilder.Padding / plank.pixelsPerUnit;
            drawing.transform.localPosition = new Vector3(0f, PlatformThickness / 2f - plankHeight / 2f + emptyTop, 0f);
            var renderer = drawing.AddComponent<SpriteRenderer>();
            renderer.sprite = plank;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(width + 0.2f, plankHeight);
            renderer.sortingOrder = 3;

            if (!posts || art.Post == null)
                return platform;
            float bottom = center.y - PlatformThickness / 2f;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = center.x + side * (width / 2f - 0.35f);
                if (art.LadderXs.All(ladderX => Mathf.Abs(ladderX - x) > 0.9f))
                    CreatePost(platform.transform, art.Post, new Vector2(x, 0f), bottom, layer);
            }
            return platform;
        }

        // Hueco que se deja entre las dos puntas rotas de un paso (se asoma lo que hay abajo, como la escalera).
        private const float CrackHalfWidth = 0.3f;

        /// <summary>
        /// Plataforma firme (no se atraviesa desde abajo) de x = from a x = to, con la cara de arriba en surface. Solo los
        /// pasos (centro y medio ancho) son de un sentido: ahí se cruza saltando desde abajo y las escaleras los
        /// atraviesan al trepar. Se dibuja por tramos con los tablones rotos para que se note el paso: se parte encima de
        /// cada uno, y las orillas marcadas con brokenStart / brokenEnd terminan astilladas. Sin los dibujos de tablones
        /// rotos se queda el tablón entero. Con posts, vigas desde el suelo como en CreatePlatform.
        /// </summary>
        internal static GameObject CreateFirmPlatform(string name, Transform parent, Sprite square, LevelArt art, float from, float to,
            float surface, int layer, IEnumerable<(float x, float halfWidth)> passes, bool brokenStart = false, bool brokenEnd = false,
            bool posts = false)
        {
            GameObject platform = CreatePlatform(name, parent, square, art, new Vector2((from + to) / 2f, surface - PlatformThickness / 2f),
                to - from, layer, posts);
            List<(float x, float halfWidth)> sorted = passes.Where(pass => pass.x > from && pass.x < to).OrderBy(pass => pass.x).ToList();

            // Collider: tramos sólidos y pasos de un sentido.
            var box = platform.GetComponent<BoxCollider2D>();
            float y = box.bounds.center.y;
            UnityEngine.Object.DestroyImmediate(platform.GetComponent<PlatformEffector2D>());
            UnityEngine.Object.DestroyImmediate(box);
            float cursor = from;
            foreach ((float x, float halfWidth) in sorted)
            {
                float start = Mathf.Max(from, x - halfWidth), end = Mathf.Min(to, x + halfWidth);
                PlatformPiece(platform, "Tramo firme", cursor, start, y, layer, oneWay: false);
                PlatformPiece(platform, "Paso", start, end, y, layer, oneWay: true);
                cursor = end;
            }
            PlatformPiece(platform, "Tramo firme", cursor, to, y, layer, oneWay: false);

            // Dibujo: partido en cada paso.
            Transform whole = platform.transform.Find("Tablón");
            if (whole == null || art.BrokenPlank == null || art.BrokenPlank2 == null)
                return platform;
            UnityEngine.Object.DestroyImmediate(whole.gameObject);
            float segmentStart = from;
            bool brokenSegmentStart = brokenStart;
            foreach ((float x, _) in sorted)
            {
                DrawPlankSegment(platform, art, segmentStart, x - CrackHalfWidth, brokenSegmentStart, brokenEnd: true);
                segmentStart = x + CrackHalfWidth;
                brokenSegmentStart = true;
            }
            DrawPlankSegment(platform, art, segmentStart, to, brokenSegmentStart, brokenEnd);
            return platform;
        }

        private static void DrawPlankSegment(GameObject platform, LevelArt art, float from, float to, bool brokenStart, bool brokenEnd)
        {
            // Las puntas enteras se pasan un poco del collider, como el tablón de siempre; las rotas acaban justo ahí.
            if (!brokenStart)
                from -= 0.1f;
            if (!brokenEnd)
                to += 0.1f;
            Sprite sprite = brokenStart && brokenEnd && art.BrokenBoth != null ? art.BrokenBoth
                : brokenEnd ? art.BrokenPlank : brokenStart ? art.BrokenPlank2 : art.Plank;
            // Un tablón repetido a lo ancho: sus puntas (los bordes del sprite) no se estiran. La cara de arriba coincide
            // con la del collider, como en CreatePlatform.
            var drawing = new GameObject("Tablón") { layer = platform.layer };
            drawing.transform.SetParent(platform.transform, false);
            float height = sprite.bounds.size.y;
            float emptyTop = ArtImportBuilder.Padding / sprite.pixelsPerUnit;
            drawing.transform.position = new Vector3((from + to) / 2f,
                platform.transform.position.y + PlatformThickness / 2f - height / 2f + emptyTop, 0f);
            var renderer = drawing.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(to - from, height);
            renderer.sortingOrder = 3;
        }

        // Un tramo de collider de la plataforma, de x = from a x = to (en el mundo).
        private static void PlatformPiece(GameObject platform, string name, float from, float to, float y, int layer, bool oneWay)
        {
            if (to - from < 0.05f)
                return;
            var piece = new GameObject(name) { layer = layer };
            piece.transform.SetParent(platform.transform, false);
            piece.transform.position = new Vector3((from + to) / 2f, y, 0f);
            // Si la plataforma está escalada (sin arte, el cuadro de color), el tramo se mide en el mundo de todos modos.
            Vector3 scale = platform.transform.lossyScale;
            piece.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            piece.AddComponent<BoxCollider2D>().size = new Vector2(to - from, PlatformThickness);
            if (oneWay)
                MakeOneWay(piece);
        }

        // Viga decorativa del suelo (y = ground) hasta debajo de la plataforma; no tiene collider.
        internal static void CreatePost(Transform parent, Sprite post, Vector2 foot, float top, int layer)
        {
            float height = top - foot.y;
            if (height < 0.5f)
                return;
            var drawing = new GameObject("Viga") { layer = layer };
            drawing.transform.SetParent(parent, true);
            drawing.transform.position = new Vector3(foot.x, foot.y + height / 2f, 0f);
            var renderer = drawing.AddComponent<SpriteRenderer>();
            renderer.sprite = post;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(post.bounds.size.x * 0.8f, height);
            renderer.sortingOrder = 0;
        }

        /// <summary>Cocodrilo que patrulla: 2 golpes, daña a Ulises al tocarlo y da la vuelta en paredes y orillas.</summary>
        internal static void CreateCrocodile(Transform parent, Sprite square, Vector2 feet, int enemyLayer, int groundLayer, int playerLayer)
        {
            var crocodile = new GameObject("Cocodrilo") { layer = enemyLayer };
            crocodile.transform.SetParent(parent, false);
            crocodile.transform.position = feet;
            var body = crocodile.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var collider = crocodile.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.5f, 0.75f);
            collider.offset = new Vector2(0f, 0.4f);

            var visual = new GameObject("Visual") { layer = enemyLayer };
            visual.transform.SetParent(crocodile.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 6;
            // Material que permite que se ilumine de blanco al recibir un golpe (DamageFeedback).
            renderer.sharedMaterial = LoadRequired<Material>(FlashMaterialPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.CrocodileControllerPath);
            Animator crocodileAnimator = null;
            if (controller != null)
            {
                renderer.sprite = FirstSprite("Assets/_Project/Art/Characters/Enemigos/Cocodrilo/Cocodrilo_Walk.png");
                crocodileAnimator = visual.AddComponent<Animator>();
                crocodileAnimator.runtimeAnimatorController = controller;
            }
            else
            {
                visual.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                visual.transform.localScale = new Vector3(1.5f, 0.75f, 1f);
                renderer.sprite = square;
                renderer.color = Manglar;
            }

            var health = crocodile.AddComponent<Health>();
            SetSerialized(health, "maxHealth", 2);
            SetSerialized(health, "invulnerabilityTime", 0.2f);
            SetSerialized(health, "destroyOnDeath", false); // Lo destruye DamageFeedback tras la animación.

            var hazard = crocodile.AddComponent<Hazard>();
            SetSerialized(hazard, "targetLayers", 1 << playerLayer);

            var patrol = crocodile.AddComponent<PatrolEnemy>();
            SetSerialized(patrol, "groundLayers", 1 << groundLayer);
            SetSerialized(patrol, "speed", CrocodileSpeed);
            SetSerialized(patrol, "health", health);
            // Se queda quieto lo que dura su animación de daño, para que se vea completa.
            var hurtClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Enemigos/Cocodrilo/Cocodrilo_Hurt.anim");
            if (hurtClip != null)
                SetSerialized(patrol, "hurtStun", hurtClip.length);
            // Muerde a Ulises cuando lo tiene cerca y enfrente (además del daño al tocarlo).
            SetSerialized(patrol, "biteTargets", 1 << playerLayer);
            if (crocodileAnimator != null)
                SetSerialized(patrol, "animator", crocodileAnimator);

            // Efectos de sonido (Audio/SFX/Cocodrilo); los que falten no suenan.
            var sounds = crocodile.AddComponent<PatrolEnemySounds>();
            SetSerialized(sounds, "enemy", patrol);
            SetSerialized(sounds, "health", health);
            string folder = GameSoundsBuilder.CrocodileFolder;
            GameSoundsBuilder.SetSound(sounds, "biteStart", GameSoundsBuilder.Load(folder, "mordida_abre"));
            GameSoundsBuilder.SetSound(sounds, "biteSnap", GameSoundsBuilder.Load(folder, "mordida"));
            GameSoundsBuilder.SetSound(sounds, "hurt", GameSoundsBuilder.Load(folder, "dano"));
            GameSoundsBuilder.SetSound(sounds, "death", GameSoundsBuilder.Load(folder, "muerte"));

            var feedback = crocodile.AddComponent<DamageFeedback>();
            SetSerialized(feedback, "health", health);
            SetSerialized(feedback, "visual", visual.transform);
            SetSerialized(feedback, "renderers", new UnityEngine.Object[] { renderer });
            SetSerialized(feedback, "shrinkAndDestroyOnDeath", true);
            SetSerialized(feedback, "flashColor", Color.white);
            SetSerialized(feedback, "flashDuration", 0.2f);
            // Con animación de muerte: se deja ver completa (y un momento boca arriba) antes de desvanecerse.
            var deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Enemigos/Cocodrilo/Cocodrilo_Die.anim");
            if (deathClip != null)
                SetSerialized(feedback, "deathAnimationTime", deathClip.length);
        }

        // Primer cuadro de una tira (Nombre_0) para que la escena se vea bien aun antes de darle Play.
        private static Sprite FirstSprite(string texturePath) =>
            AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>()
                .OrderBy(s => s.name.Length).ThenBy(s => s.name, StringComparer.Ordinal).FirstOrDefault();

        // Embarrón de masa: se reproduce una vez donde chocó la bolita, se desvanece y se destruye solo.
        private static GameObject EnsureImpactPrefab(Sprite[] frames, int layer)
        {
            const string path = "Assets/_Project/Prefabs/Player/ImpactoMasa.prefab";
            var root = new GameObject("ImpactoMasa") { layer = layer };
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 11;
            var flipbook = root.AddComponent<SpriteFlipbook>();
            SetSerialized(flipbook, "target", renderer);
            SetSerialized(flipbook, "frames", frames);
            SetSerialized(flipbook, "framesPerSecond", 14f);
            SetSerialized(flipbook, "loop", false);
            SetSerialized(flipbook, "fadeOutDuration", 0.25f);
            SetSerialized(flipbook, "destroyWhenDone", true);
            var sound = root.AddComponent<SoundOnStart>();
            GameSoundsBuilder.SetSound(sound, "sound", GameSoundsBuilder.Load(GameSoundsBuilder.UlisesFolder, "masa_impacto"), ImpactVolume);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return LoadRequired<GameObject>(path);
        }

        internal static void MakeOneWay(GameObject platform)
        {
            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            platform.GetComponent<BoxCollider2D>().usedByEffector = true;
        }

        internal static GameObject CreateBlock(string name, Transform parent, Sprite sprite, Vector2 position,
            Vector2 size, Color color, int layer)
        {
            var block = new GameObject(name) { layer = layer };
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            block.AddComponent<BoxCollider2D>();
            return block;
        }

        private static void ClearSceneExcept(UnityEngine.SceneManagement.Scene scene, params string[] keep)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (Array.IndexOf(keep, root.name) < 0)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        internal static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"No se encontró {path}.");
            return asset;
        }

        internal static void SetSerialized(UnityEngine.Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo '{field}'.");

            if (value is UnityEngine.Object[] references)
            {
                property.arraySize = references.Length;
                for (int i = 0; i < references.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            if (value is UnityEngine.Object reference)
            {
                property.objectReferenceValue = reference;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (new SerializedObject(target).FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException($"No se pudo asignar '{field}' en {target.GetType().Name}.");
                return;
            }

            SetSerializedValue(serialized, property, field, value);
        }

        private static void SetSerializedValue(SerializedObject serialized, SerializedProperty property, string field, object value)
        {
            switch (value)
            {
                case int i: // También cubre LayerMask.
                    property.intValue = i;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case Color color:
                    property.colorValue = color;
                    break;
                case Vector2 vector:
                    property.vector2Value = vector;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                default:
                    throw new ArgumentException($"Tipo no soportado para '{field}'.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int EnsureLayer(string layerName, int preferredIndex)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    return i;
            }

            int index = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredIndex).stringValue)
                ? preferredIndex
                : FirstEmptyUserLayer(layers);
            layers.GetArrayElementAtIndex(index).stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return index;
        }

        private static int FirstEmptyUserLayer(SerializedProperty layers)
        {
            for (int i = 6; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    return i;
            }
            throw new InvalidOperationException("No hay capas libres en Tags and Layers.");
        }

        private static void EnsurePlaceholderSprite()
        {
            if (!File.Exists(SpritePath))
            {
                EnsureFolder(Path.GetDirectoryName(SpritePath));
                var texture = new Texture2D(4, 4);
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 4;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Corazón liso (con antialiasing) de contorno café grueso. Lleno: relleno de color, sombra abajo a la
        /// derecha y brillo crema arriba a la izquierda. Vacío: hueco translúcido con el mismo contorno.
        /// </summary>
        private static void EnsureHeartSprite(string path, Color fill, bool full)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null && existing.width == HeartTextureWidth)
                return;

            const int samples = 4;
            Color shadow = new(fill.r * 0.72f, fill.g * 0.62f, fill.b * 0.62f, fill.a);
            var texture = new Texture2D(HeartTextureWidth, HeartTextureHeight, TextureFormat.RGBA32, false);
            for (int py = 0; py < HeartTextureHeight; py++)
            {
                for (int px = 0; px < HeartTextureWidth; px++)
                {
                    float outline = 0f, inner = 0f, dark = 0f, shine = 0f;
                    for (int sy = 0; sy < samples; sy++)
                    for (int sx = 0; sx < samples; sx++)
                    {
                        // Del píxel al plano del corazón: x de -1.25 a 1.25, y de -1.1 a 1.35.
                        float u = ((px + (sx + 0.5f) / samples) / HeartTextureWidth * 2f - 1f) * 1.25f;
                        float v = (py + (sy + 0.5f) / samples) / HeartTextureHeight * 2.45f - 1.1f;
                        if (!InHeart(u, v, 1f, 0f, 0f))
                            continue;
                        outline++;
                        if (!InHeart(u, v, 0.8f, 0f, 0.1f))
                            continue;
                        inner++;
                        // La sombra: lo del relleno que no tapa una copia corrida arriba a la izquierda.
                        if (!InHeart(u, v, 0.8f, -0.12f, 0.24f))
                            dark++;
                        float du = (u + 0.48f) / 0.22f, dv = (v - 0.62f) / 0.14f;
                        if (du * du + dv * dv <= 1f)
                            shine++;
                    }
                    float total = samples * samples;
                    float ring = (outline - inner) / total, fillCoverage = inner / total;
                    Color body = full ? Color.Lerp(fill, shadow, inner > 0f ? dark / inner : 0f) : fill;
                    if (full && inner > 0f)
                        body = Color.Lerp(body, Crema, shine / inner);
                    // Mezcla premultiplicada del aro café y el relleno (que en el vacío es translúcido).
                    float alpha = ring + fillCoverage * body.a;
                    Color color = alpha > 0f ? (Cafe * ring + body * (fillCoverage * body.a)) / alpha : Color.clear;
                    color.a = alpha;
                    texture.SetPixel(px, py, color);
                }
            }
            WriteSpriteTexture(path, texture);
        }

        // Dentro del corazón (x² + y² − 1)³ − x²y³ ≤ 0, escalado y movido.
        private static bool InHeart(float u, float v, float scale, float offsetX, float offsetY)
        {
            float x = (u - offsetX) / scale, y = (v - offsetY) / scale;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        }

        /// <summary>Blanco transparente al centro y opaco en las orillas (se tiñe en el Image).</summary>
        private static void EnsureVignetteSprite()
        {
            if (File.Exists(VignettePath))
                return;
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f, y = (py + 0.5f) / size * 2f - 1f;
                    // Distancia "cuadrada redondeada": pega a las orillas sin oscurecer de más las esquinas.
                    float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 4f) + Mathf.Pow(Mathf.Abs(y), 4f), 0.25f);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1.02f, d));
                    texture.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
                }
            }
            WriteSpriteTexture(VignettePath, texture);
        }

        /// <summary>Aro blanco suave (se tiñe en el Image).</summary>
        private static void EnsureRingSprite()
        {
            if (File.Exists(RingPath))
                return;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float d = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f);
                    texture.SetPixel(px, py, new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha)));
                }
            }
            WriteSpriteTexture(RingPath, texture);
        }

        private static void WriteSpriteTexture(string path, Texture2D texture)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private const string FlashMaterialPath = "Assets/_Project/Art/Shaders/SpriteFlash.mat";
        private const string SwooshPath = "Assets/_Project/Art/VFX/EstelaGolpe.png";

        /// <summary>
        /// Media luna para la estela del golpe (1 × 1 unidad, pivote a la izquierda): relleno amarillo plátano que se
        /// desvanece hacia Ulises y una línea café en la orilla de enfrente (hasta dónde llega el golpe), para que
        /// se lea sobre fondos claros y oscuros.
        /// </summary>
        private static void EnsureSwooshSprite()
        {
            if (File.Exists(SwooshPath))
                return;
            EnsureFolder(Path.GetDirectoryName(SwooshPath));
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size, y = (py + 0.5f) / size - 0.5f;
                    // Afuera: media elipse que toca la orilla derecha. Adentro: otra elipse corrida a la izquierda
                    // y más alta, que "muerde" la primera y deja la media luna.
                    float outer = 1f - Mathf.Sqrt(x * x + (y / 0.5f) * (y / 0.5f));
                    float innerX = (x + 0.35f) / 1.05f;
                    float inner = Mathf.Sqrt(innerX * innerX + (y / 0.62f) * (y / 0.62f)) - 1f;
                    float mask = Mathf.Clamp01(outer / 0.015f) * Mathf.Clamp01(inner / 0.05f);
                    // Línea de la orilla: los primeros ~4 % hacia adentro desde el borde de afuera.
                    float rim = 1f - Mathf.Clamp01((outer - 0.03f) / 0.015f);
                    float fillAlpha = Mathf.Lerp(0.1f, 0.55f, x * x);
                    Color pixel = Color.Lerp(Platano, Cafe, rim);
                    pixel.a = mask * Mathf.Lerp(fillAlpha, 0.9f, rim);
                    texture.SetPixel(px, py, pixel);
                }
            }
            File.WriteAllBytes(SwooshPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SwooshPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SwooshPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // Material de sprite con destello (shader Odisea/Sprites/Flash) para los enemigos.
        private static void EnsureFlashMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath) != null)
                return;
            Shader shader = Shader.Find("Odisea/Sprites/Flash")
                ?? throw new InvalidOperationException("No se encontró el shader Odisea/Sprites/Flash.");
            AssetDatabase.CreateAsset(new Material(shader) { name = "SpriteFlash" }, FlashMaterialPath);
        }

        private static void EnsureNoFrictionMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath) != null)
                return;

            EnsureFolder(Path.GetDirectoryName(MaterialPath));
            // Sin fricción para que Ulises no se quede pegado a las paredes al saltar contra ellas.
            var material = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        private static void EnsurePlayerConfig(int groundLayer, int enemyLayer, int ladderLayer)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
            if (config == null)
            {
                EnsureFolder(Path.GetDirectoryName(ConfigPath));
                config = ScriptableObject.CreateInstance<PlayerConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            if (config.groundLayers.value == 0)
                config.groundLayers = 1 << groundLayer;
            if (config.attackLayers.value == 0)
                config.attackLayers = 1 << enemyLayer;
            if (config.ladderLayers.value == 0)
                config.ladderLayers = 1 << ladderLayer;
            EditorUtility.SetDirty(config);
        }

        internal static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
