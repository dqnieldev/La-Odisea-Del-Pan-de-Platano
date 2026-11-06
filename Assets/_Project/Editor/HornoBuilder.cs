using System.IO;
using System.Linq;
using Odisea.Core;
using Odisea.Enemies;
using Odisea.Level;
using Odisea.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Nivel 2, El Horno (Odisea > Niveles > Crear Horno): defensa del horno, en una sola pantalla con la cámara fija.
    /// El horno de barro está al centro y se hornea el pan (<see cref="BakeTimer"/>); por los lados llegan ratas,
    /// cocodrilos y jejenes en oleadas (<see cref="WaveSpawner"/>) y Ulises los vence antes de que se acaben la vida del
    /// horno (<see cref="Oven"/>). Con el pan listo sale Polifemo caminando hacia el horno (<see cref="PolifemoWalker"/>);
    /// al vencerlo aparece el pan horneado, la meta. Sin el arte del patio, el fondo y el horno son de color.
    /// </summary>
    public static class HornoBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Horno.unity";
        private const string MusicPath = "Assets/_Project/Audio/Music/musica_nivel2";
        private const string FallbackMusicPath = "Assets/_Project/Audio/Music/musica_panaderia";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Enemigos/Horno";
        private const string SoftDotPath = "Assets/_Project/Art/Placeholders/Difuso.png";
        private const string CrocodileArtFolder = "Assets/_Project/Art/Characters/Enemigos/Cocodrilo";

        // Medidas: el piso en y = 0, la pantalla de -HalfWidth a HalfWidth y la cámara fija.
        private const float HalfWidth = 12.5f;
        // Con el dibujo del patio la cámara baja un poco para que la imagen llene la pantalla (ver HornoBackgroundPpu).
        private static Vector2 CameraPosition => Patio != null ? new Vector2(0f, 4.5f) : new Vector2(0f, 5f);
        // El horno: medio ancho (hasta donde llegan los que caminan) y la boca (a donde vuelan los jejenes).
        // El horno del dibujo es grande (casi tres veces Ulises): medio ancho de su base y centro de la boca.
        // El horno recortado se dibuja un poco más chico que en el patio original (y todo lo suyo con él).
        private static float OvenScale => OvenStates.Length == ArtImportBuilder.HornoStates ? 0.8f : 1f;
        private static float OvenHalfWidth => (Patio != null ? 3f : 1.7f) * OvenScale;
        // La boca en el dibujo (sin escalar) y ya en el juego.
        private static Vector2 OvenMouthDrawn => Patio != null ? new Vector2(0f, 3.6f) : new Vector2(0f, 1.1f);
        private static Vector2 OvenMouth => OvenMouthDrawn * OvenScale;
        private static Sprite Patio => AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoBackgroundPath);
        // El horno recortado en sus versiones (de sano a quemado) y su delineado; sin ellos, el horno es parte del patio.
        private static Sprite[] OvenStates => Enumerable.Range(0, ArtImportBuilder.HornoStates)
            .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoStatePath(i))).Where(s => s != null).ToArray();
        private const int OvenHealth = 10;
        private const float BakeSeconds = 100f;
        private const int PolifemoHealth = 10;

        private static readonly Color Sky = new(0.98f, 0.84f, 0.64f);
        private static readonly Color Adobe = new(0.86f, 0.66f, 0.47f);
        private static readonly Color Dirt = new(0.55f, 0.38f, 0.24f);
        private static readonly Color Clay = new(0.76f, 0.44f, 0.27f);
        private static readonly Color ClayDark = new(0.55f, 0.29f, 0.17f);
        private static readonly Color Soot = new(0.17f, 0.09f, 0.06f);
        private static readonly Color RatBrown = new(0.45f, 0.33f, 0.25f);
        private static readonly Color Bread = new(0.82f, 0.55f, 0.24f);
        private static readonly Color OvenBar = new(0.93f, 0.45f, 0.16f);
        private static readonly Color BossBar = new(0.82f, 0.24f, 0.16f);
        private static readonly Color BossTrail = new(0.98f, 0.85f, 0.45f);

        [MenuItem("Odisea/Niveles/Crear Horno")]
        public static void Build()
        {
            TutorialBuilder.EnsureCircleSprite();
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            Transform root = level.Root;
            int ground = level.GroundLayer;

            // ----- Patio: piso, paredes, fondo y las repisas -----
            var blocks = new[]
            {
                PlayerSandboxBuilder.CreateBlock("Suelo", root, square, new Vector2(0f, -1.5f), new Vector2(HalfWidth * 2f + 2f, 3f), Dirt, ground),
                PlayerSandboxBuilder.CreateBlock("Pared izquierda", root, square, new Vector2(-HalfWidth - 0.5f, 6f), new Vector2(1f, 16f), Adobe, ground),
                PlayerSandboxBuilder.CreateBlock("Pared derecha", root, square, new Vector2(HalfWidth + 0.5f, 6f), new Vector2(1f, 16f), Adobe, ground),
            };
            if (Patio is { } patio)
            {
                // El patio con su horno: el pivote del dibujo es el centro del horno a la altura del piso.
                foreach (GameObject block in blocks)
                    block.GetComponent<SpriteRenderer>().enabled = false;
                var backdrop = new GameObject("Patio").AddComponent<SpriteRenderer>();
                backdrop.transform.SetParent(root, false);
                backdrop.sprite = patio;
                backdrop.sortingOrder = -20;
            }
            else
            {
                var back = PlayerSandboxBuilder.CreateBlock("Barda del patio", root, square, new Vector2(0f, 2.5f), new Vector2(HalfWidth * 2f, 5f), Adobe, 0);
                Object.DestroyImmediate(back.GetComponent<BoxCollider2D>());
                back.GetComponent<SpriteRenderer>().sortingOrder = -10;
            }

            GameObject player = PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(-3f, 1f), "El Horno",
                GameSoundsBuilder.LoadMusic(MusicPath) != null ? MusicPath : FallbackMusicPath, showControls: false, background: Sky);
            var playerHealth = player.GetComponent<Health>();

            // ----- El horno y el horneado -----
            Oven oven = CreateOven(root, square, circle, playerHealth);
            var bake = new GameObject("Horneado").AddComponent<BakeTimer>();
            bake.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(bake, "duration", BakeSeconds);
            PlayerSandboxBuilder.SetSerialized(bake, "player", playerHealth);

            // ----- La meta: el pan horneado (aparece al vencer a Polifemo) -----
            GameObject goal = CreateGoal(root, circle, level.PlayerLayer);

            // ----- Enemigos -----
            GameObject rat = EnsureRat(level, square, circle);
            GameObject crocodile = EnsureCrocodile(level, square);
            GameObject gnats = EnsureGnats(level, circle);
            PolifemoWalker boss = CreatePolifemo(root, level, oven, goal);
            var spawner = new GameObject("Oleadas").AddComponent<WaveSpawner>();
            spawner.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(spawner, "oven", oven);
            PlayerSandboxBuilder.SetSerialized(spawner, "bake", bake);
            PlayerSandboxBuilder.SetSerialized(spawner, "groundX", HalfWidth - 0.3f);
            PlayerSandboxBuilder.SetSerialized(spawner, "airHeights", new Vector2(2.2f, 4f));
            PlayerSandboxBuilder.SetSerialized(spawner, "boss", boss);
            PlayerSandboxBuilder.SetSerialized(spawner, "bossHelpers", new Object[] { rat, gnats });
            SetWaves(spawner, rat, crocodile, gnats);

            // ----- Ítems de ayuda que caen del cielo cada tanto (pescado, hoja o café) -----
            var dropper = new GameObject("Ítems del cielo").AddComponent<ItemDropper>();
            dropper.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(dropper, "items", EnsureHelperItems(level));
            PlayerSandboxBuilder.SetSerialized(dropper, "minX", -HalfWidth + 1.5f);
            PlayerSandboxBuilder.SetSerialized(dropper, "maxX", HalfWidth - 1.5f);
            PlayerSandboxBuilder.SetSerialized(dropper, "spawnY", CameraPosition.y + 8f);
            PlayerSandboxBuilder.SetSerialized(dropper, "groundLayers", 1 << ground);
            PlayerSandboxBuilder.SetSerialized(dropper, "player", playerHealth);
            PlayerSandboxBuilder.SetSerialized(dropper, "oven", oven);

            // ----- Cámara fija, Game Over por el horneado, puntaje e interfaz -----
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            Camera.main.transform.position = new Vector3(CameraPosition.x, CameraPosition.y, -10f);
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", CameraPosition);
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", CameraPosition);
            var gameOver = Object.FindAnyObjectByType<GameOverScreen>(FindObjectsInactive.Include);
            if (gameOver != null)
                PlayerSandboxBuilder.SetSerialized(gameOver, "bake", bake);
            var score = Object.FindAnyObjectByType<LevelScore>();
            if (score != null)
                // El horneado dura BakeSeconds: el tiempo cuenta lo que tarda en vencer a Polifemo después.
                PlayerSandboxBuilder.SetSerialized(score, "parTime", BakeSeconds + 40f);
            CreateOvenHud(oven, bake, square, circle);
            CreateBossBar(boss.Health, square);

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Horno creado en {ScenePath}");
        }

        // Ratas desde el principio; cocodrilos desde el segundo tramo; jejenes en el tercero (el horneado dura 100 s).
        private static void SetWaves(WaveSpawner spawner, GameObject rat, GameObject crocodile, GameObject gnats)
        {
            var waves = new (float time, GameObject prefab, int side, int count, float interval)[]
            {
                (2f, rat, 0, 4, 3.5f), (17f, rat, 0, 4, 3f),
                (30f, crocodile, 1, 1, 0f), (34f, rat, -1, 3, 2.5f), (41f, crocodile, -1, 1, 0f), (46f, rat, 0, 4, 2.5f),
                (60f, gnats, 0, 4, 3f), (64f, crocodile, 0, 2, 7f), (70f, rat, 0, 6, 2f), (82f, gnats, 0, 4, 2.5f),
                (86f, crocodile, 0, 2, 5f),
            };
            var serialized = new SerializedObject(spawner);
            SerializedProperty list = serialized.FindProperty("waves");
            list.arraySize = waves.Length;
            for (int i = 0; i < waves.Length; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("time").floatValue = waves[i].time;
                item.FindPropertyRelative("prefab").objectReferenceValue = waves[i].prefab;
                item.FindPropertyRelative("side").intValue = waves[i].side;
                item.FindPropertyRelative("count").intValue = waves[i].count;
                item.FindPropertyRelative("interval").floatValue = waves[i].interval;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- El horno ----------

        // Horno de barro de color (una cúpula sobre su base, la boca oscura y la chimenea) con el fuego vivo en la boca.
        private static Oven CreateOven(Transform root, Sprite square, Sprite circle, Health player)
        {
            var oven = new GameObject("Horno");
            oven.transform.SetParent(root, false);
            var visual = new GameObject("Dibujo").transform;
            visual.SetParent(oven.transform, false);
            visual.localScale = Vector3.one * OvenScale;
            bool drawn = Patio != null;
            var shapes = new SpriteRenderer[0];
            if (!drawn)
            {
                shapes = new[]
                {
                    Part("Cúpula", visual, circle, new Vector2(0f, 1.4f), new Vector2(3.6f, 3.2f), Clay, -6),
                    Part("Base", visual, square, new Vector2(0f, 0.3f), new Vector2(3.8f, 0.6f), ClayDark, -5),
                };
                Part("Chimenea", visual, square, new Vector2(0.9f, 3f), new Vector2(0.35f, 0.7f), ClayDark, -7);
                Part("Boca", visual, circle, OvenMouthDrawn, new Vector2(1.3f, 1f), Soot, -4);
            }
            var mouth = new GameObject("Boca (a donde vuelan los jejenes)").transform;
            mouth.SetParent(oven.transform, false);
            mouth.localPosition = OvenMouth;

            if (AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } softDot)
            {
                var fire = new GameObject("Fuego del horno").AddComponent<OvenFire>();
                fire.transform.SetParent(visual, false);
                // La leña va abajo de la boca; en el dibujo la boca es mucho más grande.
                fire.transform.localPosition = OvenMouthDrawn - new Vector2(0f, drawn ? 1.05f : 0.35f);
                MenuScenesBuilder.SetField(fire, "softDot", softDot);
                var settings = new SerializedObject(fire);
                settings.FindProperty("mouthSize").vector2Value = drawn ? new Vector2(2f, 1.4f) : new Vector2(1f, 0.7f);
                settings.FindProperty("sortingOrder").intValue = -3;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            var component = oven.AddComponent<Oven>();
            PlayerSandboxBuilder.SetSerialized(component, "maxHealth", OvenHealth);
            PlayerSandboxBuilder.SetSerialized(component, "visual", visual);
            PlayerSandboxBuilder.SetSerialized(component, "renderers", shapes.Cast<Object>().ToArray());
            Sprite[] states = OvenStates;
            if (drawn && states.Length == ArtImportBuilder.HornoStates)
            {
                // El horno es su propio dibujo encima del patio sin horno: cambia de versión con la vida.
                var drawing = new GameObject("Horno (dibujo)").AddComponent<SpriteRenderer>();
                drawing.transform.SetParent(visual, false);
                drawing.sprite = states[0];
                drawing.sortingOrder = -18;
                PlayerSandboxBuilder.SetSerialized(component, "stateRenderer", drawing);
                PlayerSandboxBuilder.SetSerialized(component, "states", states.Cast<Object>().ToArray());
            }
            if (drawn && AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } glowDot)
            {
                // El horno es parte del fondo: en vez de ponerse rojo, se prende un resplandor rojo encima.
                SpriteRenderer glow = Part("Golpe", visual, glowDot, new Vector2(0f, 2.6f), new Vector2(8f, 7f), new Color(1f, 0.2f, 0.1f, 0f), -2);
                PlayerSandboxBuilder.SetSerialized(component, "hitGlow", glow);
            }
            PlayerSandboxBuilder.SetSerialized(component, "mouth", mouth);
            PlayerSandboxBuilder.SetSerialized(component, "halfWidth", OvenHalfWidth);
            PlayerSandboxBuilder.SetSerialized(component, "player", player);
            GameSoundsBuilder.SetSound(component, "hitSound", GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "caja_golpe"), 0.5f);
            return component;
        }

        // El pan de plátano recién horneado, frente al horno: la meta (apagada hasta vencer a Polifemo).
        private static GameObject CreateGoal(Transform root, Sprite circle, int playerLayer)
        {
            var goal = new GameObject("Meta (pan horneado)");
            goal.transform.SetParent(root, false);
            goal.transform.position = new Vector3(0f, 0.8f, 0f);
            var area = goal.AddComponent<BoxCollider2D>();
            area.size = new Vector2(2f, 2f);
            var glow = Part("Brillo", goal.transform, circle, Vector2.zero, new Vector2(2.4f, 2.4f), new Color(1f, 0.93f, 0.6f, 0.6f), 4);
            glow.name = "Brillo";
            Part("Pan", goal.transform, circle, Vector2.zero, new Vector2(1.6f, 0.8f), Bread, 5);
            Part("Corteza", goal.transform, circle, new Vector2(0f, 0.12f), new Vector2(1.3f, 0.45f), new Color(0.66f, 0.38f, 0.14f), 6);
            var component = goal.AddComponent<LevelGoal>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "nextScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "completesLocation", GameProgress.Horno);
            PlayerSandboxBuilder.SetSerialized(component, "completeScreen", MenuScenesBuilder.CreateLevelCompleteScreen(
                "¡Pan horneado!", "Ulises defendió el horno y Polifemo se fue con las manos vacías."));
            goal.SetActive(false);
            return goal;
        }

        // Los tres ítems de ayuda como prefabs (los mismos que se ponen en los niveles), para hacerlos caer.
        private static Object[] EnsureHelperItems(PlayerSandboxBuilder.LevelContext level)
        {
            EnsurePrefabFolder();
            return new[] { PickupKind.Heal, PickupKind.Shield, PickupKind.Speed }.Select(kind =>
            {
                GameObject item = PlayerSandboxBuilder.CreatePickup(level, kind, Vector2.zero);
                item.transform.SetParent(null);
                string path = $"{PrefabFolder}/Item {kind}.prefab";
                PrefabUtility.SaveAsPrefabAsset(item, path);
                Object.DestroyImmediate(item);
                return (Object)AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }).ToArray();
        }

        // ---------- Enemigos ----------

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Enemigos"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Enemigos");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Enemigos", "Horno");
        }

        private static GameObject SavePrefab(GameObject root)
        {
            EnsurePrefabFolder();
            string path = $"{PrefabFolder}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Base común de los que caminan: cuerpo cinemático, colisionador, vida, daño al tocar, el destello y el que lo
        // hace caminar hacia el horno.
        private static (GameObject root, Transform visual, Health health, Hazard hazard, OvenRaider raider) Walker(
            string name, PlayerSandboxBuilder.LevelContext level, Vector2 size, int life)
        {
            var root = new GameObject(name) { layer = level.EnemyLayer };
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            box.offset = new Vector2(0f, size.y / 2f);
            var visual = new GameObject("Visual") { layer = level.EnemyLayer }.transform;
            visual.SetParent(root.transform, false);
            var health = root.AddComponent<Health>();
            PlayerSandboxBuilder.SetSerialized(health, "maxHealth", life);
            PlayerSandboxBuilder.SetSerialized(health, "invulnerabilityTime", 0.2f);
            PlayerSandboxBuilder.SetSerialized(health, "destroyOnDeath", false);
            var hazard = root.AddComponent<Hazard>();
            PlayerSandboxBuilder.SetSerialized(hazard, "targetLayers", 1 << level.PlayerLayer);
            var raider = root.AddComponent<OvenRaider>();
            PlayerSandboxBuilder.SetSerialized(raider, "health", health);
            PlayerSandboxBuilder.SetSerialized(raider, "contact", hazard);
            return (root, visual, health, hazard, raider);
        }

        private static void Feedback(GameObject root, Health health, Transform visual, SpriteRenderer[] renderers, float deathTime)
        {
            var feedback = root.AddComponent<DamageFeedback>();
            PlayerSandboxBuilder.SetSerialized(feedback, "health", health);
            PlayerSandboxBuilder.SetSerialized(feedback, "visual", visual);
            PlayerSandboxBuilder.SetSerialized(feedback, "renderers", renderers.Cast<Object>().ToArray());
            PlayerSandboxBuilder.SetSerialized(feedback, "shrinkAndDestroyOnDeath", true);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashColor", Color.white);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashDuration", 0.15f);
            if (deathTime > 0f)
                PlayerSandboxBuilder.SetSerialized(feedback, "deathAnimationTime", deathTime);
        }

        // Rata: rápida, de un golpe; muerde el horno una vez y huye. Sin su dibujo, una figura café (cuerpo, cabeza, oreja y cola).
        private static GameObject EnsureRat(PlayerSandboxBuilder.LevelContext level, Sprite square, Sprite circle)
        {
            (GameObject root, Transform visual, Health health, _, OvenRaider raider) = Walker("Rata", level, new Vector2(1f, 0.6f), 1);
            var flash = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.RatControllerPath);
            if (controller != null)
            {
                var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = ArtImportBuilder.LoadStrip(ArtImportBuilder.RatWalkPath).FirstOrDefault();
                renderer.sharedMaterial = flash;
                renderer.sortingOrder = 7;
                var animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                PlayerSandboxBuilder.SetSerialized(raider, "animator", animator);
                PlayerSandboxBuilder.SetSerialized(raider, "speed", 3.2f);
                PlayerSandboxBuilder.SetSerialized(raider, "ovenDamage", 1);
                PlayerSandboxBuilder.SetSerialized(raider, "attackInterval", 0.6f);
                PlayerSandboxBuilder.SetSerialized(raider, "attackHitDelay", 0.2f);
                PlayerSandboxBuilder.SetSerialized(raider, "fleeAfterAttack", true);
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.RatClipPath("Hurt")) is { } hurt)
                    PlayerSandboxBuilder.SetSerialized(raider, "hurtStun", hurt.length);
                float death = AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.RatClipPath("Die")) is { } die ? die.length : 0f;
                Feedback(root, health, visual, new[] { renderer }, death);
                return SavePrefab(root);
            }
            var renderers = new[]
            {
                Part("Cuerpo", visual, circle, new Vector2(-0.05f, 0.25f), new Vector2(0.85f, 0.45f), RatBrown, 6, flash),
                Part("Cabeza", visual, circle, new Vector2(0.4f, 0.3f), new Vector2(0.4f, 0.32f), RatBrown, 7, flash),
                Part("Oreja", visual, circle, new Vector2(0.35f, 0.5f), new Vector2(0.18f, 0.2f), new Color(0.85f, 0.6f, 0.6f), 8, flash),
                Part("Cola", visual, square, new Vector2(-0.6f, 0.2f), new Vector2(0.45f, 0.05f), new Color(0.85f, 0.6f, 0.6f), 5, flash),
            };
            PlayerSandboxBuilder.SetSerialized(raider, "speed", 3.2f);
            PlayerSandboxBuilder.SetSerialized(raider, "ovenDamage", 1);
            PlayerSandboxBuilder.SetSerialized(raider, "attackInterval", 0.6f);
            // Muerde una vez y huye: el peligro es que se cuelen muchas.
            PlayerSandboxBuilder.SetSerialized(raider, "fleeAfterAttack", true);
            Feedback(root, health, visual, renderers, 0f);
            return SavePrefab(root);
        }

        // Cocodrilo: lento, aguanta 3 golpes y le quita 2 al horno por mordida. Con su arte (el del tutorial).
        private static GameObject EnsureCrocodile(PlayerSandboxBuilder.LevelContext level, Sprite square)
        {
            (GameObject root, Transform visual, Health health, _, OvenRaider raider) = Walker("Cocodrilo", level, new Vector2(1.5f, 0.75f), 3);
            var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 6;
            renderer.sharedMaterial = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.CrocodileControllerPath);
            float deathTime = 0f;
            if (controller != null)
            {
                renderer.sprite = ArtImportBuilder.LoadStrip($"{CrocodileArtFolder}/Cocodrilo_Walk.png").FirstOrDefault();
                var animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                PlayerSandboxBuilder.SetSerialized(raider, "animator", animator);
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CrocodileArtFolder}/Cocodrilo_Die.anim") is { } die)
                    deathTime = die.length;
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CrocodileArtFolder}/Cocodrilo_Hurt.anim") is { } hurt)
                    PlayerSandboxBuilder.SetSerialized(raider, "hurtStun", hurt.length);
            }
            else
            {
                visual.localPosition = new Vector3(0f, 0.4f, 0f);
                visual.localScale = new Vector3(1.5f, 0.75f, 1f);
                renderer.sprite = square;
                renderer.color = new Color(0.3f, 0.45f, 0.25f);
            }
            PlayerSandboxBuilder.SetSerialized(raider, "speed", 1.4f);
            PlayerSandboxBuilder.SetSerialized(raider, "ovenDamage", 2);
            PlayerSandboxBuilder.SetSerialized(raider, "attackInterval", 2.6f);
            PlayerSandboxBuilder.SetSerialized(raider, "attackHitDelay", 0.3f);
            PlayerSandboxBuilder.SetSerialized(raider, "reach", 0.9f);
            GameSoundsBuilder.SetSound(raider, "attackSound", GameSoundsBuilder.Load(GameSoundsBuilder.CrocodileFolder, "mordida"), 0.7f);
            Feedback(root, health, visual, new[] { renderer }, deathTime);
            return SavePrefab(root);
        }

        // Jejenes: un enjambre que vuela a la boca del horno; se deshace de un golpe. Sin su dibujo, una nube gris.
        private static GameObject EnsureGnats(PlayerSandboxBuilder.LevelContext level, Sprite circle)
        {
            var root = new GameObject("Jejenes") { layer = level.EnemyLayer };
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var area = root.AddComponent<CircleCollider2D>();
            area.isTrigger = true;
            area.radius = 0.65f;
            var visual = new GameObject("Visual") { layer = level.EnemyLayer }.transform;
            visual.SetParent(root.transform, false);
            var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 9;
            renderer.sharedMaterial = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            Sprite[] frames = Enumerable.Range(0, ArtImportBuilder.GnatFrames)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.GnatFramePath(i))).Where(s => s != null).ToArray();
            if (frames.Length > 0)
            {
                renderer.sprite = frames[0];
                var flipbook = visual.gameObject.AddComponent<SpriteFlipbook>();
                PlayerSandboxBuilder.SetSerialized(flipbook, "target", renderer);
                PlayerSandboxBuilder.SetSerialized(flipbook, "frames", frames.Cast<Object>().ToArray());
                PlayerSandboxBuilder.SetSerialized(flipbook, "framesPerSecond", 8f);
                PlayerSandboxBuilder.SetSerialized(flipbook, "loop", true);
            }
            else
            {
                renderer.sprite = circle;
                renderer.color = new Color(0.3f, 0.3f, 0.32f, 0.85f);
                visual.localScale = new Vector3(1.3f, 1f, 1f);
            }
            var health = root.AddComponent<Health>();
            PlayerSandboxBuilder.SetSerialized(health, "maxHealth", 1);
            PlayerSandboxBuilder.SetSerialized(health, "destroyOnDeath", false);
            var hazard = root.AddComponent<Hazard>();
            PlayerSandboxBuilder.SetSerialized(hazard, "targetLayers", 1 << level.PlayerLayer);
            var swarm = root.AddComponent<GnatSwarm>();
            PlayerSandboxBuilder.SetSerialized(swarm, "health", health);
            PlayerSandboxBuilder.SetSerialized(swarm, "speed", 2.2f);
            Feedback(root, health, visual, new[] { renderer }, 0f);
            return SavePrefab(root);
        }

        // Polifemo: aparece al terminar el horneado (lo activan las oleadas) y camina hacia el horno.
        private static PolifemoWalker CreatePolifemo(Transform root, PlayerSandboxBuilder.LevelContext level, Oven oven, GameObject goal)
        {
            var boss = new GameObject("Polifemo") { layer = level.EnemyLayer };
            boss.transform.SetParent(root, false);
            boss.transform.position = new Vector3(HalfWidth - 0.3f, 0f, 0f);
            boss.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var box = boss.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.6f, 3f);
            box.offset = new Vector2(0f, 1.5f);

            var visual = new GameObject("Dibujo") { layer = level.EnemyLayer };
            visual.transform.SetParent(boss.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            renderer.sortingOrder = 6;
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.PolifemoControllerPath);
            Animator animator = null;
            if (controller != null)
            {
                renderer.sprite = ArtImportBuilder.LoadStrip(ArtImportBuilder.PolifemoStandingPath)[ArtImportBuilder.PolifemoStandingFrame];
                animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                renderer.sprite = level.Square;
                renderer.color = new Color(0.3f, 0.4f, 0.6f);
                visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                visual.transform.localScale = new Vector3(1.6f, 3f, 1f);
            }

            var health = boss.AddComponent<Health>();
            PlayerSandboxBuilder.SetSerialized(health, "maxHealth", PolifemoHealth);
            PlayerSandboxBuilder.SetSerialized(health, "invulnerabilityTime", 0.3f);
            PlayerSandboxBuilder.SetSerialized(health, "destroyOnDeath", false);
            var hazard = boss.AddComponent<Hazard>();
            PlayerSandboxBuilder.SetSerialized(hazard, "targetLayers", 1 << level.PlayerLayer);
            var feedback = boss.AddComponent<DamageFeedback>();
            PlayerSandboxBuilder.SetSerialized(feedback, "health", health);
            PlayerSandboxBuilder.SetSerialized(feedback, "visual", visual.transform);
            PlayerSandboxBuilder.SetSerialized(feedback, "renderers", new Object[] { renderer });
            PlayerSandboxBuilder.SetSerialized(feedback, "flashColor", Color.white);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashDuration", 0.15f);

            var walker = boss.AddComponent<PolifemoWalker>();
            PlayerSandboxBuilder.SetSerialized(walker, "health", health);
            PlayerSandboxBuilder.SetSerialized(walker, "contact", hazard);
            PlayerSandboxBuilder.SetSerialized(walker, "animator", animator);
            PlayerSandboxBuilder.SetSerialized(walker, "visual", visual.transform);
            PlayerSandboxBuilder.SetSerialized(walker, "oven", oven);
            PlayerSandboxBuilder.SetSerialized(walker, "reward", goal);
            GameSoundsBuilder.SetSound(walker, "fallSound", GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "caja_golpe"), 0.9f);
            return walker;
        }

        // ---------- Interfaz ----------

        private static Canvas OverlayCanvas(string name, int order)
        {
            var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // Barra con marco café (redondeado) y relleno horizontal.
        private static Image Bar(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Sprite square, Color color,
            out Image trail, Color? trailColor = null)
        {
            RectTransform frame = Box(name, parent, anchor, position, size);
            var back = frame.gameObject.AddComponent<Image>();
            back.sprite = UiAssetsBuilder.EnsureRoundedSprite();
            back.type = Image.Type.Sliced;
            back.color = PlayerSandboxBuilder.Cafe;
            back.raycastTarget = false;
            Image Fill(string fillName, Color fillColor)
            {
                var rect = (RectTransform)new GameObject(fillName, typeof(RectTransform), typeof(Image)).transform;
                rect.SetParent(frame, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(5f, 5f);
                rect.offsetMax = new Vector2(-5f, -5f);
                var image = rect.GetComponent<Image>();
                image.sprite = square;
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.color = fillColor;
                image.raycastTarget = false;
                return image;
            }
            trail = trailColor is { } t ? Fill("Lo que perdió", t) : null;
            return Fill("Relleno", color);
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string text, float fontSize)
        {
            var label = Box(name, parent, anchor, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
            label.fontSize = fontSize;
            label.characterSpacing = 4f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = PlayerSandboxBuilder.Crema;
            label.outlineWidth = 0.25f;
            label.outlineColor = PlayerSandboxBuilder.Cafe;
            label.raycastTarget = false;
            return label;
        }

        // La vida del horno debajo de los corazones (con su ícono) y la barra del horneado arriba al centro.
        private static void CreateOvenHud(Oven oven, BakeTimer bake, Sprite square, Sprite circle)
        {
            Canvas canvas = OverlayCanvas("HUD del horno", -1);
            var corner = new Vector2(0f, 1f);
            RectTransform icon = Box("Ícono del horno", canvas.transform, corner, new Vector2(48f, -138f), new Vector2(56f, 56f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            var iconArt = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoIconPath);
            iconImage.sprite = iconArt != null ? iconArt : circle;
            iconImage.color = iconArt != null ? Color.white : Clay;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            Image ovenFill = Bar("Vida del horno", canvas.transform, corner, new Vector2(116f, -146f), new Vector2(260f, 30f), square,
                OvenBar, out _);
            ovenFill.fillAmount = 1f;

            var top = new Vector2(0.5f, 1f);
            RectTransform bakeRoot = Box("Horneado", canvas.transform, top, new Vector2(0f, -40f), new Vector2(560f, 74f));
            var group = bakeRoot.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            Label("Nombre", bakeRoot, top, Vector2.zero, new Vector2(560f, 40f), "HORNEANDO", 30f);
            Image bakeFill = Bar("Barra", bakeRoot, top, new Vector2(0f, -42f), new Vector2(560f, 30f), square, Bread, out _);
            bakeFill.fillAmount = 0f;

            var hud = canvas.gameObject.AddComponent<OvenHud>();
            PlayerSandboxBuilder.SetSerialized(hud, "oven", oven);
            PlayerSandboxBuilder.SetSerialized(hud, "bake", bake);
            PlayerSandboxBuilder.SetSerialized(hud, "ovenFill", ovenFill);
            PlayerSandboxBuilder.SetSerialized(hud, "ovenIcon", icon);
            PlayerSandboxBuilder.SetSerialized(hud, "bakeFill", bakeFill);
            PlayerSandboxBuilder.SetSerialized(hud, "bakeGroup", group);
        }

        // Barra de vida de Polifemo arriba al centro (donde estaba la del horneado, que ya se fue).
        private static void CreateBossBar(Health boss, Sprite square)
        {
            Canvas canvas = OverlayCanvas("HUD de Polifemo", -1);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            var top = new Vector2(0.5f, 1f);
            RectTransform bar = Box("Barra", canvas.transform, top, new Vector2(0f, -40f), new Vector2(640f, 74f));
            Label("Nombre", bar, top, Vector2.zero, new Vector2(640f, 40f), "POLIFEMO", 32f);
            Image fill = Bar("Vida", bar, top, new Vector2(0f, -42f), new Vector2(640f, 30f), square, BossBar, out Image trail, BossTrail);
            var component = canvas.gameObject.AddComponent<BossHealthBar>();
            PlayerSandboxBuilder.SetSerialized(component, "boss", boss);
            PlayerSandboxBuilder.SetSerialized(component, "group", group);
            PlayerSandboxBuilder.SetSerialized(component, "fill", fill);
            PlayerSandboxBuilder.SetSerialized(component, "trail", trail);
        }

        private static SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color,
            int order, Material material = null)
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
    }
}
