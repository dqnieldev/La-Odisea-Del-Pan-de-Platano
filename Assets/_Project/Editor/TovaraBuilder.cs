using System.Linq;
using Odisea.Core;
using Odisea.Enemies;
using Odisea.Level;
using Odisea.Player;
using UnityEditor;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Nivel 3, La Tovara (Acto III): run and gun por los manglares de izquierda a derecha. Ulises salta entre la
    /// orilla, pangas que flotan, tablones sobre raíces y piedras, venciendo a secuaces, ratas, cocodrilos y jejenes,
    /// hasta la entrada del festival. Si cae al agua pierde (Game Over y "Reintentar" empieza de nuevo). La cámara lo
    /// sigue sin regresar. Sin su arte, todo es de color.
    /// <list type="bullet">
    /// <item>Tramo 1 (muelle y orilla, x 0-56): orillas con huecos de agua y una panga lejos de la tierra.</item>
    /// <item>Tramo 2 (manglar, x 56-131): tablones con cocodrilos, piedras, pangas y jejenes.</item>
    /// <item>Tramo 3 (raíces altas, x 131-205): un salto doble a una isla con enemigos, tablones que suben.</item>
    /// <item>Tramo 4 (canal final, x 205-292): tres pangas angostas bajo jejenes, más enemigos y un último salto largo.</item>
    /// <item>En cada tramo sale un ítem de ayuda (pescado, hoja o café) en un lugar al azar.</item>
    /// </list>
    /// </summary>
    public static class TovaraBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Tovara.unity";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Player/UlisesTovaraConfig.asset";
        private const string BaseConfigPath = "Assets/_Project/ScriptableObjects/Player/UlisesConfig.asset";
        private const string MusicPath = "Assets/_Project/Audio/Music/musica_nivel3";
        private const string FallbackMusicPath = "Assets/_Project/Audio/Music/musica_tutorial";
        private const string GnatsPrefabPath = "Assets/_Project/Prefabs/Enemigos/Horno/Jejenes.prefab";
        private const string RatSoundFolder = "Assets/_Project/Audio/SFX/Raton";

        // El agua: su cara de arriba (lo que se ve) y dónde empieza a hundir a Ulises (un poco más abajo).
        private const float WaterTop = -0.45f;
        private const float WaterKill = -1.1f;
        private const float WaterBottom = -9f;
        private const float LevelStart = -16f;
        private const float LevelEnd = 292f;
        private const float GoalX = 278f;
        // La cámara a una altura fija (el nivel corre a lo ancho) y hasta dónde llega.
        private const float CameraY = 3.4f;
        private const float CameraMinX = -2f;
        private const float CameraMaxX = GoalX - 4f;
        private const int ShadowOrder = 4;
        private const float FairFollow = 0.5f;

        private static readonly Color Sky = new(0.98f, 0.80f, 0.58f);
        private static readonly Color Mud = new(0.42f, 0.31f, 0.20f);
        private static readonly Color Grass = new(0.36f, 0.50f, 0.25f);
        private static readonly Color Stone = new(0.52f, 0.52f, 0.50f);
        private static readonly Color Water = new(0.20f, 0.42f, 0.40f);
        private static readonly Color BoatHull = new(0.22f, 0.40f, 0.62f);
        private static readonly Color BoatTrim = new(0.94f, 0.92f, 0.86f);
        private static readonly Color FarMangrove = new(0.30f, 0.42f, 0.32f);
        private static readonly Color NearMangrove = new(0.20f, 0.32f, 0.22f);

        // ----- El recorrido (x de izquierda a derecha) -----
        // Ulises alcanza ~6.5 de distancia con un salto y ~10.5 con doble salto (config de La Tovara). Casi todos los
        // huecos miden 3.5-5.5 (hay que correr y saltar a tiempo); algunos piden el doble salto. Los enemigos esperan
        // donde se cae, para que un salto mal medido cueste un golpe.
        // Tierra firme: desde, hasta, altura de la cara de arriba.
        private static readonly (float from, float to, float top)[] Shores =
        {
            (LevelStart, 16f, 0f), (20f, 32f, 0f), (45f, 56f, 0f),
            (153f, 166f, 0.3f),
            (210f, 222f, 0.2f), (266f, LevelEnd, 0f),
        };
        // Pangas que flotan: centro y ancho.
        private static readonly (float x, float width)[] Boats =
        {
            (38.5f, 3f), (84.5f, 3f), (92f, 3f), (129.5f, 3f),
            (171.5f, 3f), (203f, 3f),
            (227.5f, 2.5f), (234.5f, 2.5f), (241.5f, 2.5f),
        };
        // Tablones sobre raíces (de un sentido): centro, ancho, altura.
        private static readonly (float x, float width, float y)[] Planks =
        {
            (62.5f, 6f, 1.4f), (102f, 8f, 2.0f), (119.5f, 7f, 1.4f),
            (143f, 5f, 2.2f), (179f, 5f, 2.0f), (187.5f, 5f, 3.0f),
            (256f, 7f, 1.6f),
        };
        // Piedras: centro, ancho, altura de la cara de arriba.
        private static readonly (float x, float width, float top)[] Stones =
        {
            (71f, 2f, 0.8f), (77f, 2f, 1.6f), (111f, 2f, 1.0f),
            (136f, 2f, 0.6f), (196f, 2f, 1.0f),
            (247.5f, 2f, 1.2f),
        };

        // Enemigos: secuaces (x, altura del piso), ratas, cocodrilos y jejenes (x, altura en el aire).
        private static readonly (float x, float y)[] Henchmen =
        {
            (14.5f, 0f), (53f, 0f), (120f, 1.4f), (143f, 2.2f), (160f, 0.3f), (221f, 0.2f), (256f, 1.6f),
        };
        private static readonly (float x, float y)[] Rats =
        {
            (22f, 0f), (30f, 0f), (47f, 0f), (155f, 0.3f), (164f, 0.3f), (212f, 0.2f), (269f, 0f),
        };
        private static readonly (float x, float y)[] Crocodiles =
        {
            (63f, 1.4f), (102f, 2.0f), (179f, 2.0f), (217f, 0.2f),
        };
        private static readonly (float x, float y)[] Gnats =
        {
            (40f, 4.5f), (90f, 4.5f), (186f, 5.5f), (200f, 4.5f),
            (226f, 5f), (234.5f, 4.5f), (243f, 5f), (262f, 4f),
        };

        // Ítems de ayuda: en cada tramo sale uno, en uno de sus dos lugares y de un tipo al azar (pescado, hoja o
        // café). Algunos lugares cuestan: en el aire sobre un salto o junto a un cocodrilo.
        private static readonly (float x, float y)[][] HelperSpots =
        {
            new[] { (26f, 1f), (50f, 1f) },
            new[] { (77f, 2.6f), (92f, 1.2f) },
            new[] { (149f, 3.6f), (187.5f, 4f) },
            new[] { (216f, 1.2f), (247.5f, 2.2f) },
        };
        private static readonly PickupKind[] HelperKinds = { PickupKind.Heal, PickupKind.Shield, PickupKind.Speed };

        [MenuItem("Odisea/Niveles/Crear La Tovara")]
        public static void Build()
        {
            PlayerSandboxBuilder.LevelContext level = PlayerSandboxBuilder.BeginLevel(ScenePath);
            Transform root = level.Root;
            Sprite square = level.Square;
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            PlayerSandboxBuilder.LevelArt art = PlayerSandboxBuilder.LevelArt.Load();

            // ----- El camino -----
            foreach ((float from, float to, float top) in Shores)
                CreateShore(root, square, from, to, top, level.GroundLayer);
            for (int i = 0; i < Boats.Length; i++)
                CreateBoat(root, square, Boats[i].x, Boats[i].width, i + 1, level.GroundLayer);
            for (int i = 0; i < Planks.Length; i++)
                PlayerSandboxBuilder.CreatePlatform($"Tablón {i + 1}", root, square, art, new Vector2(Planks[i].x, Planks[i].y - 0.2f),
                    Planks[i].width, level.GroundLayer);
            for (int i = 0; i < Stones.Length; i++)
                CreateStone(root, square, Stones[i].x, Stones[i].width, Stones[i].top, i + 1, level.GroundLayer);
            CreateWater(root, square, level);
            CreateBackground(root, square, circle);

            // ----- Ulises: la masa sale más seguido y más rápida en este nivel -----
            level.Config = EnsureConfig();
            GameObject player = PlayerSandboxBuilder.SetUpPlayer(level, new Vector2(-6f, 1f), "La Tovara",
                GameSoundsBuilder.LoadMusic(MusicPath) != null ? MusicPath : FallbackMusicPath, showControls: false, background: Sky);
            PlayerSandboxBuilder.AddPlayerShadow(player, level, ShadowOrder);
            if (Object.FindAnyObjectByType<LevelScore>() is { } score)
                PlayerSandboxBuilder.SetSerialized(score, "parTime", 140f);
            SetUpCamera(level);

            // ----- Enemigos: se encienden al acercarse la cámara -----
            var enemies = new GameObject("Enemigos").transform;
            enemies.SetParent(root, false);
            enemies.gameObject.AddComponent<ActivateOnView>();
            for (int i = 0; i < Henchmen.Length; i++)
                MercadoBuilder.CreateHenchman(enemies, square, circle, new Vector2(Henchmen[i].x, Henchmen[i].y), i + 1, male: i % 2 == 0, level);
            for (int i = 0; i < Rats.Length; i++)
                CreateRat(enemies, square, circle, new Vector2(Rats[i].x, Rats[i].y), i + 1, level);
            foreach ((float x, float y) in Crocodiles)
                PlayerSandboxBuilder.CreateCrocodile(enemies, square, new Vector2(x, y), level.EnemyLayer, level.GroundLayer, level.PlayerLayer,
                    ShadowOrder);
            var gnats = AssetDatabase.LoadAssetAtPath<GameObject>(GnatsPrefabPath);
            if (gnats != null)
                for (int i = 0; i < Gnats.Length; i++)
                {
                    var swarm = (GameObject)PrefabUtility.InstantiatePrefab(gnats, enemies);
                    swarm.name = $"Jejenes {i + 1}";
                    swarm.transform.position = new Vector3(Gnats[i].x, Gnats[i].y, 0f);
                }
            // Los enemigos ordenados de izquierda a derecha (así se van encendiendo).
            foreach (Transform child in enemies.Cast<Transform>().OrderBy(t => t.position.x).ToArray())
                child.SetAsLastSibling();

            // ----- Ítems de ayuda al azar -----
            for (int i = 0; i < HelperSpots.Length; i++)
            {
                var helpers = new System.Collections.Generic.List<Object>();
                foreach ((float x, float y) in HelperSpots[i])
                    foreach (PickupKind kind in HelperKinds)
                        helpers.Add(PlayerSandboxBuilder.CreatePickup(level, kind, new Vector2(x, y)).GetComponent<Pickup>());
                var spawner = new GameObject($"Ítem de ayuda al azar {i + 1}").AddComponent<RandomPickups>();
                spawner.transform.SetParent(root, false);
                PlayerSandboxBuilder.SetSerialized(spawner, "candidates", helpers.ToArray());
                PlayerSandboxBuilder.SetSerialized(spawner, "keep", 1);
            }

            // ----- La meta: la entrada del festival -----
            CreateFestivalGate(root, square, circle, level.PlayerLayer);

            MenuScenesBuilder.AddToBuild(ScenePath);
            PlayerSandboxBuilder.EndLevel(level);
            Debug.Log("[Odisea] La Tovara creada.");
        }

        // Copia de la configuración de Ulises con la masa más seguida y más rápida (run and gun).
        private static PlayerConfig EnsureConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath) == null)
                AssetDatabase.CopyAsset(BaseConfigPath, ConfigPath);
            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
            config.projectileCooldown = 0.35f;
            config.projectileSpeed = 9f;
            EditorUtility.SetDirty(config);
            return config;
        }

        // La cámara a una altura fija, sigue a Ulises hacia la derecha sin regresar y se adelanta un poco para ver lo
        // que viene; una pared en la orilla izquierda de la pantalla no deja salirse.
        private static void SetUpCamera(PlayerSandboxBuilder.LevelContext level)
        {
            var follow = Object.FindAnyObjectByType<CameraFollow2D>();
            PlayerSandboxBuilder.SetSerialized(follow, "useBounds", true);
            PlayerSandboxBuilder.SetSerialized(follow, "minPosition", new Vector2(CameraMinX, CameraY));
            PlayerSandboxBuilder.SetSerialized(follow, "maxPosition", new Vector2(CameraMaxX, CameraY));
            PlayerSandboxBuilder.SetSerialized(follow, "noBacktrack", true);
            PlayerSandboxBuilder.SetSerialized(follow, "lookAhead", 3f);
            follow.transform.position = new Vector3(CameraMinX, CameraY, -10f);

            var wall = new GameObject("Orilla de la pantalla") { layer = level.GroundLayer };
            wall.transform.SetParent(level.Root, false);
            // Ya en la orilla izquierda de la primera vista (16:9), lejos de donde aparece Ulises.
            wall.transform.position = new Vector3(CameraMinX - 7f * 16f / 9f - 0.5f, CameraY, 0f);
            wall.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 40f);
            PlayerSandboxBuilder.SetSerialized(wall.AddComponent<ScreenEdgeWall>(), "follow", follow);
        }

        // Orilla de tierra: lodo hasta el fondo del agua con pasto arriba.
        private static void CreateShore(Transform root, Sprite square, float from, float to, float top, int layer)
        {
            float height = top - WaterBottom;
            GameObject shore = PlayerSandboxBuilder.CreateBlock($"Orilla {from:0}-{to:0}", root, square,
                new Vector2((from + to) / 2f, top - height / 2f), new Vector2(to - from, height), Mud, layer);
            shore.GetComponent<SpriteRenderer>().sortingOrder = 0;
            var grass = new GameObject("Pasto").AddComponent<SpriteRenderer>();
            grass.transform.SetParent(root, false);
            grass.transform.position = new Vector3((from + to) / 2f, top - 0.15f, 0f);
            grass.transform.localScale = new Vector3(to - from, 0.3f, 1f);
            grass.sprite = square;
            grass.color = Grass;
            grass.sortingOrder = 1;
        }

        // Panga: lancha que flota y se mece; se pisa su cubierta (de un sentido, para subir saltando desde el agua no,
        // pero sí para no atorarse con la orilla).
        private static void CreateBoat(Transform root, Sprite square, float x, float width, int number, int layer)
        {
            var boat = new GameObject($"Panga {number}") { layer = layer };
            boat.transform.SetParent(root, false);
            boat.transform.position = new Vector3(x, -0.2f, 0f);
            boat.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var deck = boat.AddComponent<BoxCollider2D>();
            deck.size = new Vector2(width, 0.4f);
            PlayerSandboxBuilder.MakeOneWay(boat);
            boat.AddComponent<BobbingPlatform>();
            SpriteRenderer Part(string name, Vector2 position, Vector2 size, Color color, int order)
            {
                var part = new GameObject(name) { layer = layer }.AddComponent<SpriteRenderer>();
                part.transform.SetParent(boat.transform, false);
                part.transform.localPosition = position;
                part.transform.localScale = new Vector3(size.x, size.y, 1f);
                part.sprite = square;
                part.color = color;
                part.sortingOrder = order;
                return part;
            }
            // Encima del agua: la panga flota, no está hundida.
            Part("Casco", new Vector2(0f, -0.35f), new Vector2(width, 0.7f), BoatHull, 6);
            Part("Borda", new Vector2(0f, 0.12f), new Vector2(width + 0.1f, 0.16f), BoatTrim, 7);
        }

        private static void CreateStone(Transform root, Sprite square, float x, float width, float top, int number, int layer)
        {
            float height = top - WaterBottom;
            GameObject stone = PlayerSandboxBuilder.CreateBlock($"Piedra {number}", root, square,
                new Vector2(x, top - height / 2f), new Vector2(width, height), Stone, layer);
            stone.GetComponent<SpriteRenderer>().sortingOrder = 0;
        }

        // El agua a lo largo de todo el nivel: se ve encima del pie de la orilla y las piedras; un poco más abajo de su
        // cara empieza a hundir a Ulises.
        private static void CreateWater(Transform root, Sprite square, PlayerSandboxBuilder.LevelContext level)
        {
            float width = LevelEnd - LevelStart + 40f;
            float centerX = (LevelStart + LevelEnd) / 2f;
            var water = new GameObject("Agua").AddComponent<SpriteRenderer>();
            water.transform.SetParent(root, false);
            water.transform.position = new Vector3(centerX, (WaterTop + WaterBottom) / 2f, 0f);
            water.transform.localScale = new Vector3(width, WaterTop - WaterBottom, 1f);
            water.sprite = square;
            water.color = Water;
            water.sortingOrder = 5;

            var zone = new GameObject("Agua (hunde)");
            zone.transform.SetParent(root, false);
            zone.transform.position = new Vector3(centerX, (WaterKill + WaterBottom) / 2f, 0f);
            var trigger = zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(width, WaterKill - WaterBottom);
            var waterZone = zone.AddComponent<WaterZone>();
            var serialized = new SerializedObject(waterZone);
            serialized.FindProperty("playerLayers").intValue = 1 << level.PlayerLayer;
            serialized.FindProperty("enemyLayers").intValue = 1 << level.EnemyLayer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Fondo de color en capas (se mueven más lento que la cámara): el manglar lejano y el cercano. Al final, las
        // carpas de la feria.
        private static void CreateBackground(Transform root, Sprite square, Sprite circle)
        {
            Transform far = Layer(root, "Manglar lejano", 0.85f, 1f);
            Transform near = Layer(root, "Manglar cercano", 0.55f, 1f);
            for (float x = LevelStart; x < LevelEnd + 40f; x += 9f)
            {
                Shape(far, circle, new Vector2(x, 2.6f + Mathf.Sin(x) * 0.4f), new Vector2(11f, 4.2f), FarMangrove, -30);
                Shape(near, circle, new Vector2(x * 1.3f, 1.6f + Mathf.Cos(x) * 0.5f), new Vector2(8f, 5f), NearMangrove, -20);
            }
            // La feria al fondo, al final del canal.
            // A medio paso de la cámara: entra a la pantalla solo en el último tramo.
            Transform fair = Layer(root, "Feria", FairFollow, 1f);
            Color[] tents = { new(0.86f, 0.24f, 0.30f), new(0.98f, 0.72f, 0.18f), new(0.25f, 0.55f, 0.78f), new(0.55f, 0.30f, 0.62f) };
            // Quedan detrás de la meta cuando la cámara llega al final.
            float fairX = CameraMaxX - (CameraMaxX - CameraMinX) * FairFollow - 5.5f;
            for (int i = 0; i < 6; i++)
            {
                float x = fairX + i * 2.2f;
                Shape(fair, square, new Vector2(x, 3.6f), new Vector2(1.8f, 1.4f), tents[i % tents.Length], -25);
                var roof = Shape(fair, square, new Vector2(x, 4.6f), new Vector2(1.3f, 1.3f), tents[(i + 1) % tents.Length], -25);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
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

        private static SpriteRenderer Shape(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            var shape = new GameObject("Forma").AddComponent<SpriteRenderer>();
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = new Vector3(size.x, size.y, 1f);
            shape.sprite = sprite;
            shape.color = color;
            shape.sortingOrder = order;
            return shape;
        }

        // Rata de La Tovara: corre por su tramo de orilla (PatrolEnemy, como el cocodrilo), persigue a Ulises al verlo y
        // muerde. Cae de un golpe. Sin su dibujo, una figura café.
        private static void CreateRat(Transform parent, Sprite square, Sprite circle, Vector2 feet, int number,
            PlayerSandboxBuilder.LevelContext level)
        {
            var rat = new GameObject($"Rata {number}") { layer = level.EnemyLayer };
            rat.transform.SetParent(parent, false);
            rat.transform.position = feet;
            rat.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var collider = rat.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1f, 0.6f);
            collider.offset = new Vector2(0f, 0.3f);
            var visual = new GameObject("Visual") { layer = level.EnemyLayer };
            visual.transform.SetParent(rat.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            renderer.sortingOrder = 7;
            Animator animator = null;
            float death = 0f;
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.RatControllerPath);
            if (controller != null)
            {
                renderer.sprite = ArtImportBuilder.LoadStrip(ArtImportBuilder.RatWalkPath).FirstOrDefault();
                animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.RatClipPath("Die")) is { } die)
                    death = die.length;
            }
            else
            {
                renderer.sprite = circle;
                renderer.color = new Color(0.45f, 0.33f, 0.25f);
                visual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                visual.transform.localScale = new Vector3(1f, 0.55f, 1f);
            }

            var health = rat.AddComponent<Health>();
            PlayerSandboxBuilder.SetSerialized(health, "maxHealth", 1);
            PlayerSandboxBuilder.SetSerialized(health, "invulnerabilityTime", 0.2f);
            PlayerSandboxBuilder.SetSerialized(health, "destroyOnDeath", false);
            PlayerSandboxBuilder.SetSerialized(rat.AddComponent<Hazard>(), "targetLayers", 1 << level.PlayerLayer);

            var patrol = rat.AddComponent<PatrolEnemy>();
            PlayerSandboxBuilder.SetSerialized(patrol, "groundLayers", 1 << level.GroundLayer);
            PlayerSandboxBuilder.SetSerialized(patrol, "speed", 2.8f);
            PlayerSandboxBuilder.SetSerialized(patrol, "chaseSpeed", 5f);
            PlayerSandboxBuilder.SetSerialized(patrol, "sightDistance", 7f);
            PlayerSandboxBuilder.SetSerialized(patrol, "health", health);
            PlayerSandboxBuilder.SetSerialized(patrol, "biteTargets", 1 << level.PlayerLayer);
            PlayerSandboxBuilder.SetSerialized(patrol, "biteOffset", new Vector2(0.6f, 0.3f));
            PlayerSandboxBuilder.SetSerialized(patrol, "biteSize", new Vector2(0.7f, 0.6f));
            if (animator != null)
                PlayerSandboxBuilder.SetSerialized(patrol, "animator", animator);
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.RatClipPath("Hurt")) is { } hurt)
                PlayerSandboxBuilder.SetSerialized(patrol, "hurtStun", hurt.length);

            var sounds = rat.AddComponent<PatrolEnemySounds>();
            PlayerSandboxBuilder.SetSerialized(sounds, "enemy", patrol);
            PlayerSandboxBuilder.SetSerialized(sounds, "health", health);
            GameSoundsBuilder.SetSound(sounds, "biteSnap", GameSoundsBuilder.Load(RatSoundFolder, "raton_golpe"));
            GameSoundsBuilder.SetSound(sounds, "death", GameSoundsBuilder.Load(RatSoundFolder, "raton_muerte"));

            var feedback = rat.AddComponent<DamageFeedback>();
            PlayerSandboxBuilder.SetSerialized(feedback, "health", health);
            PlayerSandboxBuilder.SetSerialized(feedback, "visual", visual.transform);
            PlayerSandboxBuilder.SetSerialized(feedback, "renderers", new Object[] { renderer });
            PlayerSandboxBuilder.SetSerialized(feedback, "shrinkAndDestroyOnDeath", true);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashColor", Color.white);
            PlayerSandboxBuilder.SetSerialized(feedback, "flashDuration", 0.15f);
            if (death > 0f)
                PlayerSandboxBuilder.SetSerialized(feedback, "deathAnimationTime", death);
            PlayerSandboxBuilder.AddGroundShadow(rat, level.GroundLayer, new Vector2(1.25f, 0.32f), Vector2.zero, health, 3f, ShadowOrder);
        }

        // La entrada del festival: un arco de colores con papel picado. Al cruzarlo, la tarjeta de resultados, la
        // historieta del final y los créditos.
        private static void CreateFestivalGate(Transform root, Sprite square, Sprite circle, int playerLayer)
        {
            var gate = new GameObject("Meta (entrada del festival)");
            gate.transform.SetParent(root, false);
            gate.transform.position = new Vector3(GoalX, 2f, 0f);
            var area = gate.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(2.5f, 4f);
            Shape(gate.transform, square, new Vector2(-1.8f, 0.4f), new Vector2(0.5f, 4.8f), new Color(0.55f, 0.32f, 0.16f), 2);
            Shape(gate.transform, square, new Vector2(1.8f, 0.4f), new Vector2(0.5f, 4.8f), new Color(0.55f, 0.32f, 0.16f), 2);
            Shape(gate.transform, square, new Vector2(0f, 2.9f), new Vector2(4.4f, 0.6f), new Color(0.86f, 0.24f, 0.30f), 3);
            Color[] flags = { new(0.98f, 0.72f, 0.18f), new(0.25f, 0.55f, 0.78f), new(0.40f, 0.70f, 0.30f), new(0.86f, 0.24f, 0.30f) };
            for (int i = 0; i < 6; i++)
                Shape(gate.transform, square, new Vector2(-1.6f + i * 0.64f, 2.3f), new Vector2(0.45f, 0.5f), flags[i % flags.Length], 3);
            var component = gate.AddComponent<LevelGoal>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "nextScene", SceneFlow.Credits);
            PlayerSandboxBuilder.SetSerialized(component, "completesLocation", GameProgress.Tovara);
            PlayerSandboxBuilder.SetSerialized(component, "alsoCompletes", GameProgress.Festival);
            PlayerSandboxBuilder.SetSerialized(component, "outroStory", SceneFlow.TovaraOutroStory);
            PlayerSandboxBuilder.SetSerialized(component, "completeScreen", MenuScenesBuilder.CreateLevelCompleteScreen(
                "¡Llegaste al festival!", "Ulises cruzó los manglares y el pan llegó a tiempo."));
        }
    }
}
