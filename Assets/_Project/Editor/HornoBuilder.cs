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
        // Sonidos del nivel (Audio/SFX/...): el horno al recibir un golpe, la rata (pasitos y mordida) y los jejenes.
        private const string OvenSoundFolder = "Assets/_Project/Audio/SFX/Horno";
        private const string RatSoundFolder = "Assets/_Project/Audio/SFX/Raton";
        private const string GnatSoundFolder = "Assets/_Project/Audio/SFX/Jejenes";

        // Medidas: el piso en y = 0, la pantalla de -HalfWidth a HalfWidth y la cámara fija.
        private const float HalfWidth = 12.5f;
        // Con el dibujo del patio la cámara baja un poco para que la imagen llene la pantalla (ver HornoBackgroundPpu).
        private static Vector2 CameraPosition => Patio != null ? new Vector2(0f, 4.5f) : new Vector2(0f, 5f);
        // El horno: medio ancho (hasta donde llegan los que caminan) y la boca (a donde vuelan los jejenes).
        // El horno del dibujo es grande (casi tres veces Ulises): medio ancho de su base y centro de la boca.
        // El horno recortado se dibuja un poco más chico que en el patio original (y todo lo suyo con él).
        private static bool Cutout => OvenStates.Length == ArtImportBuilder.HornoStates;
        private static float OvenScale => Cutout ? 0.8f : 1f;
        // Medidas del horno en unidades del dibujo (antes de escalar): el recortado es más grande que el del patio.
        // Medio ancho de la base, la boca, la base del fuego y su tamaño, y la punta de la chimenea.
        private static float OvenHalfWidth => (Cutout ? 4.4f : Patio != null ? 3f : 1.7f) * OvenScale;
        // La boca en el dibujo (sin escalar) y ya en el juego.
        private static Vector2 OvenMouthDrawn => Cutout ? new Vector2(-0.34f, 3.7f) : Patio != null ? new Vector2(0f, 3.6f) : new Vector2(0f, 1.1f);
        private static Vector2 FireBase => Cutout ? new Vector2(-0.34f, 2.15f) : OvenMouthDrawn - new Vector2(0f, Patio != null ? 1.05f : 0.35f);
        private static Vector2 FireSize => Cutout ? new Vector2(2.6f, 1.6f) : Patio != null ? new Vector2(2f, 1.4f) : new Vector2(1f, 0.7f);
        private static float ChimneyTop => Cutout ? 9.15f : Patio != null ? 7.47f : 3.4f;
        private static Vector2 OvenMouth => OvenMouthDrawn * OvenScale;
        private static Sprite Patio => AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoBackgroundPath);
        // El horno recortado en sus versiones (de sano a quemado) y su delineado; sin ellos, el horno es parte del patio.
        private static Sprite[] OvenStates => Enumerable.Range(0, ArtImportBuilder.HornoStates)
            .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoStatePath(i))).Where(s => s != null).ToArray();
        private const int OvenHealth = 5;
        private const float BakeSeconds = 100f;
        private const int PolifemoHealth = 10;
        // Tamaño de Polifemo respecto a su dibujo. Más chico, sus pasos son más cortos: camina más lento (la velocidad
        // va con el tamaño para que los pies no resbalen) y llega más tarde al horno.
        private const float PolifemoScale = 0.8f;
        private const float PolifemoSpeed = 0.8f;

        private static readonly Color Sky = new(0.98f, 0.84f, 0.64f);
        private static readonly Color Adobe = new(0.86f, 0.66f, 0.47f);
        private static readonly Color Dirt = new(0.55f, 0.38f, 0.24f);
        private static readonly Color Clay = new(0.76f, 0.44f, 0.27f);
        private static readonly Color ClayDark = new(0.55f, 0.29f, 0.17f);
        private static readonly Color Soot = new(0.17f, 0.09f, 0.06f);
        private static readonly Color RatBrown = new(0.45f, 0.33f, 0.25f);
        private static readonly Color Bread = new(0.82f, 0.55f, 0.24f);
        private static readonly Color BossBar = new(0.82f, 0.24f, 0.16f);
        private static readonly Color BossTrail = new(0.98f, 0.85f, 0.45f);
        private static readonly Color IntroYellow = new(1f, 0.84f, 0.25f);

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
            // Sombra en el piso: que se vea que Ulises pisa el patio.
            PlayerSandboxBuilder.AddPlayerShadow(player, level, ShadowOrder);

            // ----- El horno y el horneado -----
            Oven oven = CreateOven(root, square, circle, playerHealth);
            CreateOvenAlert(oven, level.EnemyLayer);
            var bake = new GameObject("Horneado").AddComponent<BakeTimer>();
            bake.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(bake, "duration", BakeSeconds);
            PlayerSandboxBuilder.SetSerialized(bake, "player", playerHealth);
            CreateNightfall(root, bake, oven);

            // ----- La meta: el pan horneado (aparece al vencer a Polifemo) -----
            GameObject goal = CreateGoal(root, circle, level.PlayerLayer);

            // ----- Enemigos -----
            GameObject rat = EnsureRat(level, square, circle);
            GameObject crocodile = EnsureCrocodile(level, square);
            GameObject gnats = EnsureGnats(level, circle);
            PolifemoWalker boss = CreatePolifemo(root, level, oven, goal, playerHealth);
            var spawner = new GameObject("Oleadas").AddComponent<WaveSpawner>();
            spawner.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(spawner, "oven", oven);
            PlayerSandboxBuilder.SetSerialized(spawner, "bake", bake);
            PlayerSandboxBuilder.SetSerialized(spawner, "groundX", HalfWidth - 0.3f);
            PlayerSandboxBuilder.SetSerialized(spawner, "airHeights", new Vector2(2.2f, 4f));
            PlayerSandboxBuilder.SetSerialized(spawner, "boss", boss);
            // Con Polifemo salen más jejenes que ratas (2 de cada 3): obligan a saltar sin que se cuelen al horno tan
            // fácil como las ratas mientras peleas con él.
            PlayerSandboxBuilder.SetSerialized(spawner, "bossHelpers", new Object[] { rat, gnats, gnats });
            PlayerSandboxBuilder.SetSerialized(spawner, "rageHelperInterval", 2.2f);
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
            // La cámara casi del tamaño del dibujo del patio: al temblar (las pisadas de Polifemo) no debe enseñar su
            // orilla.
            if (root.Find("Patio") is { } patioArt && patioArt.TryGetComponent(out SpriteRenderer patioRenderer))
            {
                PlayerSandboxBuilder.SetSerialized(follow, "limitView", true);
                PlayerSandboxBuilder.SetSerialized(follow, "viewMin", (Vector2)patioRenderer.bounds.min);
                PlayerSandboxBuilder.SetSerialized(follow, "viewMax", (Vector2)patioRenderer.bounds.max);
            }
            var gameOver = Object.FindAnyObjectByType<GameOverScreen>(FindObjectsInactive.Include);
            if (gameOver != null)
            {
                PlayerSandboxBuilder.SetSerialized(gameOver, "bake", bake);
                // La barra por fases: el horneado y luego Polifemo.
                PlayerSandboxBuilder.SetSerialized(gameOver, "boss", boss.Health);
            }
            var score = Object.FindAnyObjectByType<LevelScore>();
            if (score != null)
            {
                // El horneado dura BakeSeconds: el tiempo cuenta lo que tarda en vencer a Polifemo después.
                PlayerSandboxBuilder.SetSerialized(score, "parTime", BakeSeconds + 40f);
                // Entre más vidas le queden al horno, más puntos.
                PlayerSandboxBuilder.SetSerialized(score, "oven", oven);
            }
            CreateOvenHud(oven, bake, square, circle);
            CreateBossBar(boss.Health, square);
            CreateIntroBanner(oven);

            PlayerSandboxBuilder.EndLevel(level);
            MenuScenesBuilder.AddToBuild(ScenePath);
            Debug.Log($"[Odisea] Horno creado en {ScenePath}");
        }

        // Los primeros 60 s, ratas seguido y un cocodrilo cada tanto desde los 10 s; luego entran los jejenes (el horneado
        // dura 100 s).
        private static void SetWaves(WaveSpawner spawner, GameObject rat, GameObject crocodile, GameObject gnats)
        {
            var waves = new (float time, GameObject prefab, int side, int count, float interval)[]
            {
                (2f, rat, 0, 5, 2.5f), (10f, crocodile, 1, 1, 0f), (15f, rat, 0, 5, 2.2f), (22f, crocodile, -1, 1, 0f),
                (28f, rat, 0, 4, 2f), (32f, crocodile, 0, 2, 6f), (40f, rat, 0, 5, 2f), (46f, crocodile, -1, 1, 0f),
                (50f, rat, 0, 4, 1.8f), (54f, crocodile, 1, 1, 0f),
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
                // Las ratas pueden salir en ráfaga o en pinza; los jejenes solo en pinza; los cocodrilos, de uno en uno.
                item.FindPropertyRelative("bursts").boolValue = waves[i].prefab == rat;
                item.FindPropertyRelative("pincers").boolValue = waves[i].prefab == rat || waves[i].prefab == gnats;
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
                fire.transform.localPosition = FireBase;
                MenuScenesBuilder.SetField(fire, "softDot", softDot);
                var settings = new SerializedObject(fire);
                settings.FindProperty("mouthSize").vector2Value = FireSize;
                settings.FindProperty("sortingOrder").intValue = -3;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            var component = oven.AddComponent<Oven>();
            // Humo de la chimenea (con el recorte; en el horno de color, sobre su chimenea).
            if (AssetDatabase.LoadAssetAtPath<Sprite>(TutorialBuilder.CirclePath) is { } smokeDot)
            {
                var smoke = new GameObject("Humo de la chimenea").AddComponent<ChimneySmoke>();
                smoke.transform.SetParent(visual, false);
                smoke.transform.localPosition = Cutout ? new Vector2(-0.27f, ChimneyTop - 0.1f) : new Vector2(0.9f, 3.35f);
                PlayerSandboxBuilder.SetSerialized(smoke, "softDot", smokeDot);
                PlayerSandboxBuilder.SetSerialized(smoke, "oven", component);
            }
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
                drawing.sortingOrder = -17;
                PlayerSandboxBuilder.SetSerialized(component, "stateRenderer", drawing);
                PlayerSandboxBuilder.SetSerialized(component, "states", states.Cast<Object>().ToArray());
            }
            if (drawn && AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } glowDot)
            {
                // El horno es parte del fondo: en vez de ponerse rojo, se prende un resplandor rojo encima.
                SpriteRenderer glow = Part("Golpe", visual, glowDot, new Vector2(OvenMouthDrawn.x, OvenMouthDrawn.y - 0.6f), new Vector2(10f, 9f), new Color(1f, 0.2f, 0.1f, 0f), -2);
                PlayerSandboxBuilder.SetSerialized(component, "hitGlow", glow);
                // Al empezar el nivel, el horno parpadea con un resplandor amarillo ("¡Protege el horno!").
                SpriteRenderer highlight = Part("Resplandor de inicio", visual, glowDot, new Vector2(OvenMouthDrawn.x, OvenMouthDrawn.y + 0.2f),
                    new Vector2(12f, 11f), new Color(1f, 0.9f, 0.3f, 0f), -2);
                PlayerSandboxBuilder.SetSerialized(component, "highlightGlow", highlight);
            }
            PlayerSandboxBuilder.SetSerialized(component, "mouth", mouth);
            PlayerSandboxBuilder.SetSerialized(component, "halfWidth", OvenHalfWidth);
            PlayerSandboxBuilder.SetSerialized(component, "player", player);
            AudioClip[] ovenHit = GameSoundsBuilder.Load(OvenSoundFolder, "horno_golpe");
            GameSoundsBuilder.SetSound(component, "hitSound",
                ovenHit.Length > 0 ? ovenHit : GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "caja_golpe"), 0.7f);
            return component;
        }

        // Anochece con el horneado: la luz global de la escena se vuelve azul y baja, el patio de noche aparece encima del
        // de día y una luz cálida sale de la boca del horno.
        private static void CreateNightfall(Transform root, BakeTimer bake, Oven oven)
        {
            var nightfall = new GameObject("Anochecer").AddComponent<Nightfall>();
            nightfall.transform.SetParent(root, false);
            PlayerSandboxBuilder.SetSerialized(nightfall, "bake", bake);
            var global = Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None)
                .FirstOrDefault(light => light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
            if (global != null)
                PlayerSandboxBuilder.SetSerialized(nightfall, "globalLight", global);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoNightPath) is { } night)
            {
                var backdrop = new GameObject("Patio de noche").AddComponent<SpriteRenderer>();
                backdrop.transform.SetParent(root, false);
                backdrop.sprite = night;
                backdrop.sortingOrder = -19;
                backdrop.color = new Color(1f, 1f, 1f, 0f);
                PlayerSandboxBuilder.SetSerialized(nightfall, "nightBackdrop", backdrop);
            }
            var fireLight = new GameObject("Luz del horno").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            fireLight.transform.SetParent(oven.transform, false);
            fireLight.transform.localPosition = OvenMouth;
            fireLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            fireLight.color = new Color(1f, 0.62f, 0.28f);
            fireLight.pointLightInnerRadius = 2f;
            fireLight.pointLightOuterRadius = 12f;
            fireLight.intensity = 0f;
            PlayerSandboxBuilder.SetSerialized(nightfall, "ovenLight", fireLight);
            // Resplandor cálido alrededor del horno (detrás de su dibujo y sobre el patio), que se ve de noche.
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } glowDot)
            {
                var halo = new GameObject("Resplandor del horno").AddComponent<SpriteRenderer>();
                halo.transform.SetParent(oven.transform, false);
                halo.transform.localPosition = new Vector3(OvenMouth.x, OvenMouth.y * 0.8f, 0f);
                halo.transform.localScale = new Vector3(OvenHalfWidth * 4.2f, OvenHalfWidth * 3.2f, 1f);
                halo.sprite = glowDot;
                halo.color = new Color(1f, 0.6f, 0.25f, 0f);
                halo.sortingOrder = -18;
                PlayerSandboxBuilder.SetSerialized(nightfall, "ovenHalo", halo);
            }
        }

        // Signo "!" rojo de muestra: el aviso pone una copia encima de cada enemigo que esté muy cerca del horno.
        private static void CreateOvenAlert(Oven oven, int enemyLayer)
        {
            var alert = oven.gameObject.AddComponent<OvenAlert>();
            Sprite circle = PlayerSandboxBuilder.LoadRequired<Sprite>(TutorialBuilder.CirclePath);
            // Un círculo crema con orilla café y, encima, el "!" rojo grueso.
            Transform Mark(string name)
            {
                var root = new GameObject(name).transform;
                root.SetParent(oven.transform, false);
                root.localScale = Vector3.one * 0.75f;
                Part("Orilla", root, circle, Vector2.zero, new Vector2(1.45f, 1.45f), PlayerSandboxBuilder.Cafe, 24);
                Part("Fondo", root, circle, Vector2.zero, new Vector2(1.25f, 1.25f), new Color(1f, 0.96f, 0.86f), 25);
                var mark = new GameObject("Signo").AddComponent<TextMeshPro>();
                mark.transform.SetParent(root, false);
                mark.text = "!";
                mark.font = UiAssetsBuilder.LoadFont(UiAssetsBuilder.OutfitExtraBold);
                mark.fontSize = 13f;
                mark.fontStyle = FontStyles.Bold;
                mark.alignment = TextAlignmentOptions.Center;
                mark.color = new Color(0.9f, 0.1f, 0.08f);
                mark.rectTransform.sizeDelta = new Vector2(1.4f, 1.4f);
                mark.sortingOrder = 26;
                return root;
            }
            PlayerSandboxBuilder.SetSerialized(alert, "oven", oven);
            PlayerSandboxBuilder.SetSerialized(alert, "markTemplate", Mark("Alerta (muestra)"));
            PlayerSandboxBuilder.SetSerialized(alert, "enemyLayers", 1 << enemyLayer);
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
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.BananaBreadPath) is { } bread)
                Part("Pan", goal.transform, bread, Vector2.zero, Vector2.one, Color.white, 5);
            else
            {
                // Sin su dibujo, un óvalo café con su corteza.
                Part("Pan", goal.transform, circle, Vector2.zero, new Vector2(1.6f, 0.8f), Bread, 5);
                Part("Corteza", goal.transform, circle, new Vector2(0f, 0.12f), new Vector2(1.3f, 0.45f), new Color(0.66f, 0.38f, 0.14f), 6);
            }
            var component = goal.AddComponent<LevelGoal>();
            PlayerSandboxBuilder.SetSerialized(component, "playerLayers", 1 << playerLayer);
            PlayerSandboxBuilder.SetSerialized(component, "nextScene", SceneFlow.Map);
            PlayerSandboxBuilder.SetSerialized(component, "completesLocation", GameProgress.Horno);
            PlayerSandboxBuilder.SetSerialized(component, "outroStory", SceneFlow.HornoOutroStory);
            PlayerSandboxBuilder.SetSerialized(component, "completeScreen", MenuScenesBuilder.CreateLevelCompleteScreen(
                "¡Pan horneado!", "Ulises defendió el horno y el pan salió perfecto."));
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

        // Sonido en bucle mientras el enemigo se mueve (se apaga al morir).
        private static void AddLoop(GameObject root, AudioClip[] clips, Health health, float volume)
        {
            if (clips.Length == 0)
                return;
            var loop = root.AddComponent<LoopingSound>();
            PlayerSandboxBuilder.SetSerialized(loop, "clip", clips[0]);
            PlayerSandboxBuilder.SetSerialized(loop, "volume", volume);
            PlayerSandboxBuilder.SetSerialized(loop, "health", health);
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
                GameSoundsBuilder.SetSound(raider, "attackSound", GameSoundsBuilder.Load(RatSoundFolder, "raton_golpe"), 0.8f);
                AddLoop(root, GameSoundsBuilder.Load(RatSoundFolder, "raton"), health, 0.35f);
                Feedback(root, health, visual, new[] { renderer }, death);
                Shadow(root, level, new Vector2(1.25f, 0.32f), Vector2.zero, health, 3f);
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
            Shadow(root, level, new Vector2(1.25f, 0.32f), Vector2.zero, health, 3f);
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
            PlayerSandboxBuilder.SetSerialized(raider, "ovenDamage", 1);
            PlayerSandboxBuilder.SetSerialized(raider, "attackInterval", 2.6f);
            PlayerSandboxBuilder.SetSerialized(raider, "attackHitDelay", 0.3f);
            PlayerSandboxBuilder.SetSerialized(raider, "reach", 0.9f);
            GameSoundsBuilder.SetSound(raider, "attackSound", GameSoundsBuilder.Load(GameSoundsBuilder.CrocodileFolder, "mordida"), 0.7f);
            Feedback(root, health, visual, new[] { renderer }, deathTime);
            Shadow(root, level, new Vector2(2.1f, 0.42f), Vector2.zero, health, 3f);
            return SavePrefab(root);
        }

        // Jejenes: un enjambre que vuela a la boca del horno; se deshace de un golpe. Sin su dibujo, una nube gris.
        private static GameObject EnsureGnats(PlayerSandboxBuilder.LevelContext level, Sprite circle)
        {
            var root = new GameObject("Jejenes") { layer = level.EnemyLayer };
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var area = root.AddComponent<CircleCollider2D>();
            area.isTrigger = true;
            area.radius = 0.85f;
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
            AddLoop(root, GameSoundsBuilder.Load(GnatSoundFolder, "jejenes"), health, 0.45f);
            Feedback(root, health, visual, new[] { renderer }, 0f);
            // Vuelan: su sombra queda en el piso, chica y clara.
            Shadow(root, level, new Vector2(1.7f, 0.36f), Vector2.zero, health, 7f);
            return SavePrefab(root);
        }

        // Sombra ovalada en el piso debajo del personaje (sigue al piso al saltar o volar). Atrás de todos los
        // personajes y delante del patio.
        private static void Shadow(GameObject target, PlayerSandboxBuilder.LevelContext level, Vector2 size, Vector2 offset,
            Health health, float maxHeight) =>
            PlayerSandboxBuilder.AddGroundShadow(target, level.GroundLayer, size, offset, health, maxHeight, ShadowOrder);

        private const int ShadowOrder = -2;

        // Polifemo: aparece al terminar el horneado (lo activan las oleadas) y camina hacia el horno.
        private const string PolifemoSoundFolder = "Assets/_Project/Audio/SFX/Polifemo";

        private static PolifemoWalker CreatePolifemo(Transform root, PlayerSandboxBuilder.LevelContext level, Oven oven, GameObject goal,
            Health player)
        {
            bool hd = ArtImportBuilder.PolifemoHD &&
                      AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.PolifemoClipPath("Throw")) != null;
            // Con las hojas grandes es casi del tamaño del horno.
            float height = (hd ? ArtImportBuilder.PolifemoHDHeight : 3f) * PolifemoScale;
            var boss = new GameObject("Polifemo") { layer = level.EnemyLayer };
            boss.transform.SetParent(root, false);
            boss.transform.position = new Vector3(HalfWidth - 0.3f, 0f, 0f);
            boss.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var box = boss.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(height * 0.4f, height * 0.95f);
            box.offset = new Vector2(0f, height * 0.475f);

            var visual = new GameObject("Dibujo") { layer = level.EnemyLayer };
            visual.transform.SetParent(boss.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = PlayerSandboxBuilder.LoadRequired<Material>(PlayerSandboxBuilder.FlashMaterialPath);
            renderer.sortingOrder = 6;
            visual.transform.localScale = new Vector3(PolifemoScale, PolifemoScale, 1f);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ArtImportBuilder.PolifemoControllerPath);
            Animator animator = null;
            if (controller != null)
            {
                renderer.sprite = hd
                    ? ArtImportBuilder.LoadStrip($"{ArtImportBuilder.PolifemoFolder}/Polifemo_HD_Lanzar.png")[6]
                    : ArtImportBuilder.LoadStrip(ArtImportBuilder.PolifemoStandingPath)[ArtImportBuilder.PolifemoStandingFrame];
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
            // Su sombra se queda aunque caiga (ahí se queda tirado).
            Shadow(boss, level, new Vector2(height * 0.55f, height * 0.11f), Vector2.zero, null, 3f);
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
            PlayerSandboxBuilder.SetSerialized(walker, "player", player);
            PlayerSandboxBuilder.SetSerialized(walker, "speed", PolifemoSpeed * PolifemoScale);
            GameSoundsBuilder.SetSound(walker, "fallSound", GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "caja_golpe"), 0.9f);
            GameSoundsBuilder.SetSound(walker, "hurtSound", GameSoundsBuilder.Load(PolifemoSoundFolder, "polifemo_dano_recibido"), 0.9f);
            GameSoundsBuilder.SetSound(walker, "laughSound", GameSoundsBuilder.Load(PolifemoSoundFolder, "polifemo_risa"), 1f);
            // Los pasos suenan en bucle mientras camina.
            AddLoop(boss, GameSoundsBuilder.Load(PolifemoSoundFolder, "polifemo_pasos"), health, 0.9f);
            if (hd)
            {
                // Más grande: llega antes a la orilla del horno y la olla sale de arriba de su cabeza.
                PlayerSandboxBuilder.SetSerialized(walker, "reach", 1.4f * PolifemoScale);
                PlayerSandboxBuilder.SetSerialized(walker, "footWidth", 1.2f * PolifemoScale);
                PlayerSandboxBuilder.SetSerialized(walker, "pushBack", 0.8f);
                PlayerSandboxBuilder.SetSerialized(walker, "stepTime", ArtImportBuilder.PolifemoStepTime);
                if (AssetDatabase.LoadAssetAtPath<Sprite>(SoftDotPath) is { } dust)
                    PlayerSandboxBuilder.SetSerialized(walker, "dust", dust);
                PlayerSandboxBuilder.SetSerialized(walker, "throwRelease",
                    ArtImportBuilder.PolifemoHDReleaseFrame / ArtImportBuilder.PolifemoHDThrowFps);
                PlayerSandboxBuilder.SetSerialized(walker, "throwDuration",
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.PolifemoClipPath("Throw")).length);
                PlayerSandboxBuilder.SetSerialized(walker, "throwHand", new Vector2(0.7f, height * 0.95f));
                PlayerSandboxBuilder.SetSerialized(walker, "hurtPause",
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtImportBuilder.PolifemoClipPath("Hurt")).length);
                PlayerSandboxBuilder.SetSerialized(walker, "potPrefab", EnsurePotPrefab(level));
                GameSoundsBuilder.SetSound(walker, "throwSound", GameSoundsBuilder.Load(GameSoundsBuilder.HenchmanFolder, "secuaz_golpe"), 0.6f);
                // Cuando se cubre y la masa le rebota (polifemo_bloqueo si existe).
                AudioClip[] block = GameSoundsBuilder.Load(PolifemoSoundFolder, "polifemo_bloqueo");
                GameSoundsBuilder.SetSound(walker, "blockSound",
                    block.Length > 0 ? block : GameSoundsBuilder.Load(GameSoundsBuilder.HenchmanFolder, "secuaz_golpe"), 0.5f);
            }
            return walker;
        }

        // La olla de barro que avienta Polifemo: vuela en arco girando, quita un corazón y se rompe al chocar.
        private static ThrownItem EnsurePotPrefab(PlayerSandboxBuilder.LevelContext level)
        {
            var root = new GameObject("Olla de Polifemo") { layer = level.EnemyLayer };
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var circle = root.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.45f;
            var visual = new GameObject("Dibujo") { layer = level.EnemyLayer }.AddComponent<SpriteRenderer>();
            visual.transform.SetParent(root.transform, false);
            visual.sprite = AssetDatabase.LoadAllAssetsAtPath(ArtImportBuilder.PolifemoPotPath(0)).OfType<Sprite>().FirstOrDefault();
            visual.sortingOrder = 11;
            var item = root.AddComponent<ThrownItem>();
            PlayerSandboxBuilder.SetSerialized(item, "kind", (int)ThrownKind.Crate);
            PlayerSandboxBuilder.SetSerialized(item, "visual", visual.transform);
            PlayerSandboxBuilder.SetSerialized(item, "playerLayers", 1 << level.PlayerLayer);
            PlayerSandboxBuilder.SetSerialized(item, "groundLayers", 1 << level.GroundLayer);
            PlayerSandboxBuilder.SetSerialized(item, "spinSpeed", 300f);
            if (AssetDatabase.LoadAllAssetsAtPath(ArtImportBuilder.PolifemoPotBrokenPath).OfType<Sprite>().FirstOrDefault() is { } broken)
                PlayerSandboxBuilder.SetSerialized(item, "brokenSprite", broken);
            AudioClip[] potBreak = GameSoundsBuilder.Load(PolifemoSoundFolder, "olla_golpe");
            GameSoundsBuilder.SetSound(item, "breakSound",
                potBreak.Length > 0 ? potBreak : GameSoundsBuilder.Load(GameSoundsBuilder.CirceFolder, "caja_golpe"), 0.9f);
            return SavePrefab(root).GetComponent<ThrownItem>();
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


        // La vida del horno no lleva indicador: se ve en el mismo dibujo, que se va deteriorando. El horneado, en el
        // pizarrón del puntaje: un reloj de cocina con el pan en medio (sin su dibujo, un cronómetro que se va llenando).
        // El texto del pizarrón cuenta lo que falta.
        private static void CreateOvenHud(Oven oven, BakeTimer bake, Sprite square, Sprite circle)
        {
            var hudObject = new GameObject("Horneado");
            hudObject.transform.SetParent(oven.transform, false);
            var hud = hudObject.AddComponent<OvenHud>();
            PlayerSandboxBuilder.SetSerialized(hud, "bake", bake);
            if (!CreateKitchenClock(hud, out ScoreHud score))
            {
                Image bakeFill = CreateStopwatch(circle, square, out score);
                if (bakeFill != null)
                    PlayerSandboxBuilder.SetSerialized(hud, "bakeFill", bakeFill);
            }
            if (score != null)
                PlayerSandboxBuilder.SetSerialized(score, "bake", bake);
        }

        private static Image UiImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        // Ancho de la carátula del reloj de cocina en el pizarrón.
        private const float ClockSize = 104f;

        // En el pizarrón del puntaje, el reloj de pared se vuelve el reloj de cocina: solo su aguja, que da la vuelta en
        // lo que dura el horneado.
        private static bool CreateKitchenClock(OvenHud hud, out ScoreHud score)
        {
            score = Object.FindAnyObjectByType<ScoreHud>();
            var face = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoClockPath);
            var needleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtImportBuilder.HornoNeedlePath);
            Transform icon = score != null ? score.transform.Find("Tiempo/Ícono") : null;
            if (icon == null || face == null || needleSprite == null)
                return false;
            // El pizarrón un poco más grande en este nivel, para que quepa el reloj.
            var tray = (RectTransform)score.transform;
            tray.sizeDelta = new Vector2(tray.sizeDelta.x, 144f);
            if (tray.GetComponent<HorizontalLayoutGroup>() is { } layout)
                layout.padding = new RectOffset(44, 54, 14, 14);
            if (icon.GetComponent<ClockHands>() is { } hands)
                Object.DestroyImmediate(hands);
            for (int i = icon.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(icon.GetChild(i).gameObject);
            var faceImage = icon.GetComponent<Image>();
            faceImage.sprite = face;
            faceImage.color = Color.white;
            faceImage.preserveAspect = true;
            icon.localScale = Vector3.one;
            float height = ClockSize * face.rect.height / face.rect.width;
            if (icon.GetComponent<LayoutElement>() is { } element)
            {
                element.preferredWidth = ClockSize;
                element.preferredHeight = height;
            }
            // La aguja gira sobre el centro de la carátula: a medio ancho desde abajo.
            var dial = new Vector2(0.5f, 0f);
            var middle = new Vector2(0f, ClockSize / 2f);
            float needleHeight = ClockSize * 0.42f;
            Image needle = UiImage("Aguja", icon, needleSprite, dial, middle,
                new Vector2(needleHeight * needleSprite.rect.width / needleSprite.rect.height, needleHeight));
            needle.rectTransform.pivot = needleSprite.pivot / needleSprite.rect.size;
            needle.rectTransform.anchoredPosition = middle;
            PlayerSandboxBuilder.SetSerialized(hud, "needle", needle.rectTransform);
            // Al sonar tiembla toda la carátula.
            PlayerSandboxBuilder.SetSerialized(hud, "clock", (RectTransform)icon);
            GameSoundsBuilder.SetSound(hud, "ringSound", GameSoundsBuilder.Load(OvenSoundFolder, "horno_listo"), 0.8f);
            return true;
        }

        // Sin el dibujo del reloj de cocina, el reloj de pared se vuelve un cronómetro: un aro con su botón arriba y un
        // relleno que se va llenando como rebanadas, de arriba en el sentido del reloj.
        private static Image CreateStopwatch(Sprite circle, Sprite square, out ScoreHud score)
        {
            score = Object.FindAnyObjectByType<ScoreHud>();
            Transform icon = score != null ? score.transform.Find("Tiempo/Ícono") : null;
            if (icon == null)
                return null;
            // El pizarrón un poco más grande en este nivel, para que el cronómetro y el tiempo tengan aire.
            var tray = (RectTransform)score.transform;
            tray.sizeDelta = new Vector2(tray.sizeDelta.x, 108f);
            if (tray.GetComponent<HorizontalLayoutGroup>() is { } layout)
                layout.padding = new RectOffset(50, 54, 20, 20);
            if (icon.GetComponent<ClockHands>() is { } hands)
                Object.DestroyImmediate(hands);
            for (int i = icon.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(icon.GetChild(i).gameObject);
            var ring = icon.GetComponent<Image>();
            ring.sprite = UiAssetsBuilder.EnsureRingSprite();
            ring.color = PlayerSandboxBuilder.Crema;
            ring.preserveAspect = true;
            icon.localScale = Vector3.one * 1.1f;
            Image Child(string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
            {
                var rect = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
                rect.SetParent(icon, false);
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
                var image = rect.GetComponent<Image>();
                image.sprite = sprite;
                image.color = color;
                image.raycastTarget = false;
                return image;
            }
            // El botón de arriba y el relleno (un poco adentro del aro).
            Child("Botón", square, PlayerSandboxBuilder.Crema, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-6f, 0f), new Vector2(6f, 9f));
            Image fill = Child("Horneado", circle, Bread, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            return fill;
        }

        // Barra de vida de Polifemo arriba al centro (sale cuando el pan ya está horneado).
        private static void CreateBossBar(Health boss, Sprite square)
        {
            Canvas canvas = OverlayCanvas("HUD de Polifemo", -1);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            var top = new Vector2(0.5f, 1f);
            // Abajo al centro (arriba van el puntaje y la vida de Ulises).
            RectTransform bar = Box("Barra", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(640f, 74f));
            Label("Nombre", bar, top, Vector2.zero, new Vector2(640f, 40f), "POLIFEMO", 32f);
            Image fill = Bar("Vida", bar, top, new Vector2(0f, -42f), new Vector2(640f, 30f), square, BossBar, out Image trail, BossTrail);
            var component = canvas.gameObject.AddComponent<BossHealthBar>();
            PlayerSandboxBuilder.SetSerialized(component, "boss", boss);
            PlayerSandboxBuilder.SetSerialized(component, "group", group);
            PlayerSandboxBuilder.SetSerialized(component, "fill", fill);
            PlayerSandboxBuilder.SetSerialized(component, "trail", trail);
        }

        // Al empezar: "¡Protege el horno!" grande en medio de la pantalla (un poco arriba del horno) mientras el horno
        // parpadea de amarillo.
        private static void CreateIntroBanner(Oven oven)
        {
            Canvas canvas = OverlayCanvas("Letrero de inicio", 5);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            group.alpha = 0f;
            var middle = new Vector2(0.5f, 0.5f);
            // Arriba, entre los corazones y el reloj, sobre una franja café para que se lea encima del cielo.
            RectTransform content = Box("Letrero", canvas.transform, middle, new Vector2(0f, 330f), new Vector2(1160f, 200f));
            var band = content.gameObject.AddComponent<Image>();
            band.sprite = UiAssetsBuilder.EnsureRoundedSprite();
            band.type = Image.Type.Sliced;
            band.color = new Color(PlayerSandboxBuilder.Cafe.r, PlayerSandboxBuilder.Cafe.g, PlayerSandboxBuilder.Cafe.b, 0.82f);
            band.raycastTarget = false;
            TMP_Text title = Label("Título", content, middle, new Vector2(0f, 28f), new Vector2(1160f, 124f), "¡PROTEGE EL HORNO!", 88f);
            title.color = IntroYellow;
            title.outlineWidth = 0.3f;
            TMP_Text hint = Label("Ayuda", content, middle, new Vector2(0f, -58f), new Vector2(1160f, 54f),
                "Que nada lo toque hasta que el pan esté listo", 40f);
            hint.characterSpacing = 1f;
            var banner = canvas.gameObject.AddComponent<LevelIntroBanner>();
            PlayerSandboxBuilder.SetSerialized(banner, "group", group);
            PlayerSandboxBuilder.SetSerialized(banner, "content", content);
            PlayerSandboxBuilder.SetSerialized(banner, "oven", oven);
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
